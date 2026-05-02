import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:http_parser/http_parser.dart' show MediaType;

import 'auth_storage.dart';

class ApiException implements Exception {
  final int statusCode;
  final String message;
  final Map<String, dynamic>? body;

  ApiException(this.statusCode, this.message, [this.body]);

  @override
  String toString() => 'ApiException($statusCode): $message';
}

/// HTTP client que habla con la API REST de HomeChef Pro. Agrega el header
/// Bearer automaticamente cuando [AuthStorage] tiene un token guardado.
///
/// Cuando un endpoint responde 401 (token expirado), el cliente intenta hacer
/// refresh transparente con el refresh token guardado:
///   1. POST /api/auth/refresh con el refresh token plano.
///   2. Si responde 200, guarda el nuevo par y reintenta UNA vez la request.
///   3. Si el refresh tambien falla (401/expired/revocado), limpia el storage
///      y notifica a los listeners para que la app redirija al login.
class ApiClient {
  final Uri baseUri;
  final AuthStorage auth;
  final http.Client _http;

  /// Path del endpoint de refresh. Configurable por si la API cambia su base.
  final String refreshPath;

  /// Listeners notificados cuando el server respondio 401 Y el refresh fallo
  /// (sesion definitivamente perdida). Si el refresh tiene exito, los listeners
  /// NO son llamados — el usuario sigue trabajando sin enterarse.
  final List<void Function()> _unauthorizedListeners = [];

  /// Lock para evitar refresh concurrentes. Si llegan 5 requests simultaneos
  /// y los 5 reciben 401, todos esperan al mismo refresh en vez de disparar 5.
  Future<bool>? _refreshInFlight;

  ApiClient({
    required this.baseUri,
    AuthStorage? auth,
    http.Client? client,
    this.refreshPath = '/api/auth/refresh',
  })  : auth = auth ?? AuthStorage(),
        _http = client ?? http.Client();

  void addUnauthorizedListener(void Function() listener) {
    _unauthorizedListeners.add(listener);
  }

  void removeUnauthorizedListener(void Function() listener) {
    _unauthorizedListeners.remove(listener);
  }

  void _fireUnauthorized() {
    // Snapshot defensivo por si un listener modifica la lista mientras iteramos.
    for (final l in List.of(_unauthorizedListeners)) {
      try {
        l();
      } catch (_) {
        // Un listener que tira no debe cortar la cadena; si se queja hay un bug en la UI.
      }
    }
  }

  Future<dynamic> get(String path, {Map<String, String>? query}) =>
      _send('GET', path, query: query);

  Future<dynamic> post(String path, {Object? body, Map<String, String>? query}) =>
      _send('POST', path, body: body, query: query);

  Future<dynamic> patch(String path, {Object? body}) =>
      _send('PATCH', path, body: body);

  Future<dynamic> put(String path, {Object? body}) =>
      _send('PUT', path, body: body);

  Future<dynamic> delete(String path) => _send('DELETE', path);

  /// Multipart upload — comprobantes de pago, fotos, etc.
  Future<dynamic> postMultipart(
    String path, {
    required String fieldName,
    required String filename,
    required String contentType,
    required List<int> bytes,
    Map<String, String>? fields,
  }) async {
    Future<http.Response> doRequest() async {
      final uri = baseUri.replace(
        path: _join(baseUri.path, path),
        queryParameters: baseUri.queryParameters.isEmpty ? null : baseUri.queryParameters,
      );
      final request = http.MultipartRequest('POST', uri);
      final token = await auth.readToken();
      if (token != null && token.isNotEmpty) {
        request.headers['Authorization'] = 'Bearer $token';
      }
      request.headers['Accept'] = 'application/json';
      if (fields != null) request.fields.addAll(fields);

      final parts = contentType.split('/');
      request.files.add(http.MultipartFile.fromBytes(
        fieldName,
        bytes,
        filename: filename,
        contentType: MediaType(parts.first, parts.length > 1 ? parts[1] : 'octet-stream'),
      ));

      final streamed = await _http.send(request);
      return http.Response.fromStream(streamed);
    }

    var resp = await doRequest();
    if (resp.statusCode == 401 && await _tryRefresh()) {
      resp = await doRequest();
    }
    return _decode(resp);
  }

  Future<dynamic> _send(
    String method,
    String path, {
    Object? body,
    Map<String, String>? query,
  }) async {
    final uri = baseUri.replace(
      path: _join(baseUri.path, path),
      queryParameters: {
        ...baseUri.queryParameters,
        if (query != null) ...query,
      },
    );

    Future<http.Response> doRequest() async {
      final headers = <String, String>{
        'Accept': 'application/json',
      };
      final token = await auth.readToken();
      if (token != null && token.isNotEmpty) {
        headers['Authorization'] = 'Bearer $token';
      }
      if (body != null) headers['Content-Type'] = 'application/json';

      final request = http.Request(method, uri)
        ..headers.addAll(headers)
        ..followRedirects = false;
      if (body != null) request.body = jsonEncode(body);

      final streamed = await _http.send(request);
      return http.Response.fromStream(streamed);
    }

    var resp = await doRequest();
    if (resp.statusCode == 401 && await _tryRefresh()) {
      // Reintentar UNA SOLA VEZ tras refresh exitoso.
      resp = await doRequest();
    }
    return _decode(resp);
  }

  /// Intenta refrescar el access token usando el refresh token guardado.
  /// Devuelve true si el refresh fue exitoso y el storage tiene un par nuevo.
  /// Si ya hay un refresh en vuelo, espera al mismo en vez de disparar otro.
  Future<bool> _tryRefresh() async {
    return _refreshInFlight ??= _doRefresh();
  }

  Future<bool> _doRefresh() async {
    try {
      final refresh = await auth.readRefreshToken();
      if (refresh == null || refresh.isEmpty) return false;

      final uri = baseUri.replace(path: _join(baseUri.path, refreshPath));
      final resp = await _http.post(
        uri,
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json',
        },
        body: jsonEncode({'refreshToken': refresh}),
      );

      if (resp.statusCode != 200) return false;
      final body = jsonDecode(resp.body) as Map<String, dynamic>;
      final newAccess = body['accessToken'] as String?;
      final newExp = body['expiresAt'] as String?;
      final newRefresh = body['refreshToken'] as String?;
      final newRefreshExp = body['refreshExpiresAt'] as String?;
      if (newAccess == null || newRefresh == null || newExp == null || newRefreshExp == null) {
        return false;
      }

      await auth.updateTokens(
        accessToken: newAccess,
        expiresAt: DateTime.parse(newExp),
        refreshToken: newRefresh,
        refreshExpiresAt: DateTime.parse(newRefreshExp),
      );
      return true;
    } catch (_) {
      return false;
    } finally {
      _refreshInFlight = null;
    }
  }

  static String _join(String basePath, String path) {
    if (path.startsWith('/')) {
      // Absolute path overrides basePath.
      final trimmedBase = basePath.endsWith('/')
          ? basePath.substring(0, basePath.length - 1)
          : basePath;
      return trimmedBase.isEmpty ? path : '$trimmedBase$path';
    }
    final base = basePath.endsWith('/') ? basePath : '$basePath/';
    return '$base$path';
  }

  dynamic _decode(http.Response r) {
    if (r.statusCode == 204) return null;

    final isJson =
        r.body.isNotEmpty && r.headers['content-type']?.contains('application/json') == true;
    final decoded = isJson ? jsonDecode(r.body) : null;

    if (r.statusCode >= 200 && r.statusCode < 300) {
      if (isJson) return decoded; // Map<String,dynamic> o List<dynamic>, segun el endpoint.
      return r.bodyBytes;
    }

    // 401: token expirado o invalido. Limpiamos el storage y notificamos a la
    // UI para que redirija al login. Aun asi seguimos lanzando ApiException
    // para que la pantalla activa pueda manejar el error puntual si quiere.
    if (r.statusCode == 401) {
      // fire-and-forget: no esperamos al clear del storage para fallar rapido.
      auth.clear();
      _fireUnauthorized();
    }

    // Error path: ProblemDetails devuelve un objeto JSON.
    final problem = decoded is Map<String, dynamic> ? decoded : null;
    final detail = problem?['detail']
        ?? problem?['title']
        ?? r.reasonPhrase
        ?? 'HTTP ${r.statusCode}';
    throw ApiException(r.statusCode, detail.toString(), problem);
  }
}
