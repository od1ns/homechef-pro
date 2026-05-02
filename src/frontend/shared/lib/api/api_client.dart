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

/// HTTP client that talks to the HomeChef Pro REST API. Adds the JWT bearer
/// header automatically when [AuthStorage] has a token.
///
/// Cuando recibe un 401 (token expirado o invalidado), limpia el token guardado
/// y notifica a los suscriptores de [onUnauthorized] para que la app puede
/// redirigir al login. Asi evitamos pantallas blancas con "401 Unauthorized"
/// y forzamos un re-login limpio.
class ApiClient {
  final Uri baseUri;
  final AuthStorage auth;
  final http.Client _http;

  /// Listeners notificados cuando el server respondio 401. Cada listener es
  /// llamado una vez por evento; la lista no se limpia automaticamente, asi
  /// que asegurate de [removeUnauthorizedListener] cuando el widget se desmonta.
  final List<void Function()> _unauthorizedListeners = [];

  ApiClient({
    required this.baseUri,
    AuthStorage? auth,
    http.Client? client,
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

  /// Multipart upload — used for payment proof images.
  /// Sends a single file under [fieldName] plus optional text fields.
  Future<dynamic> postMultipart(
    String path, {
    required String fieldName,
    required String filename,
    required String contentType,
    required List<int> bytes,
    Map<String, String>? fields,
  }) async {
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
    final resp = await http.Response.fromStream(streamed);
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
    final resp = await http.Response.fromStream(streamed);
    return _decode(resp);
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
