import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Almacena el JWT de acceso, el refresh token y la metadata del usuario de
/// forma segura. Usa keychain en iOS, KeyStore en Android, encrypted local
/// storage en web.
class AuthStorage {
  static const _kToken = 'hcp.access_token';
  static const _kUserId = 'hcp.user_id';
  static const _kEmail = 'hcp.email';
  static const _kRoles = 'hcp.roles';
  static const _kExpiresAt = 'hcp.expires_at';
  static const _kRefreshToken = 'hcp.refresh_token';
  static const _kRefreshExpiresAt = 'hcp.refresh_expires_at';

  final FlutterSecureStorage _store;
  AuthStorage([FlutterSecureStorage? store]) : _store = store ?? const FlutterSecureStorage();

  Future<void> save({
    required String token,
    required String userId,
    required String email,
    required List<String> roles,
    required DateTime expiresAt,
    String? refreshToken,
    DateTime? refreshExpiresAt,
  }) async {
    await _store.write(key: _kToken, value: token);
    await _store.write(key: _kUserId, value: userId);
    await _store.write(key: _kEmail, value: email);
    await _store.write(key: _kRoles, value: roles.join(','));
    await _store.write(key: _kExpiresAt, value: expiresAt.toIso8601String());
    if (refreshToken != null) {
      await _store.write(key: _kRefreshToken, value: refreshToken);
    }
    if (refreshExpiresAt != null) {
      await _store.write(key: _kRefreshExpiresAt, value: refreshExpiresAt.toIso8601String());
    }
  }

  Future<String?> readToken() async => _store.read(key: _kToken);
  Future<String?> readRefreshToken() async => _store.read(key: _kRefreshToken);

  /// Actualiza solo el access token + refresh token tras un refresh exitoso,
  /// preservando el resto de la metadata (userId, email, roles).
  Future<void> updateTokens({
    required String accessToken,
    required DateTime expiresAt,
    required String refreshToken,
    required DateTime refreshExpiresAt,
  }) async {
    await _store.write(key: _kToken, value: accessToken);
    await _store.write(key: _kExpiresAt, value: expiresAt.toIso8601String());
    await _store.write(key: _kRefreshToken, value: refreshToken);
    await _store.write(key: _kRefreshExpiresAt, value: refreshExpiresAt.toIso8601String());
  }

  Future<({String userId, String email, List<String> roles, DateTime expiresAt})?>
      readSession() async {
    final token = await _store.read(key: _kToken);
    if (token == null) return null;
    final userId = await _store.read(key: _kUserId) ?? '';
    final email = await _store.read(key: _kEmail) ?? '';
    final rolesCsv = await _store.read(key: _kRoles) ?? '';
    final expiresStr = await _store.read(key: _kExpiresAt);
    final expires = expiresStr != null
        ? DateTime.tryParse(expiresStr) ?? DateTime.now()
        : DateTime.now();
    return (
      userId: userId,
      email: email,
      roles: rolesCsv.isEmpty ? <String>[] : rolesCsv.split(','),
      expiresAt: expires,
    );
  }

  Future<void> clear() async {
    await _store.delete(key: _kToken);
    await _store.delete(key: _kUserId);
    await _store.delete(key: _kEmail);
    await _store.delete(key: _kRoles);
    await _store.delete(key: _kExpiresAt);
    await _store.delete(key: _kRefreshToken);
    await _store.delete(key: _kRefreshExpiresAt);
  }
}
