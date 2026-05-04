import 'dart:convert';

import 'package:shared_preferences/shared_preferences.dart';

/// Persists the order IDs the customer has placed locally so the Orders tab can
/// keep tracking them across sessions even when the user is anonymous.
class LocalOrderStore {
  static const _key = 'hcp.local_orders.v1';

  Future<List<LocalOrderRef>> read() async {
    final prefs = await SharedPreferences.getInstance();
    final raw = prefs.getString(_key);
    if (raw == null || raw.isEmpty) return const [];
    final decoded = jsonDecode(raw) as List<dynamic>;
    return decoded
        .map((e) => LocalOrderRef.fromJson(e as Map<String, dynamic>))
        .toList(growable: false);
  }

  Future<void> add(LocalOrderRef ref) async {
    final list = (await read()).toList();
    list.removeWhere((r) => r.orderId == ref.orderId);
    list.insert(0, ref);
    await _write(list.take(50).toList()); // cap to last 50 orders
  }

  Future<void> remove(String orderId) async {
    final list = (await read()).where((r) => r.orderId != orderId).toList();
    await _write(list);
  }

  Future<void> _write(List<LocalOrderRef> list) async {
    final prefs = await SharedPreferences.getInstance();
    final encoded = jsonEncode(list.map((r) => r.toJson()).toList());
    await prefs.setString(_key, encoded);
  }
}

class LocalOrderRef {
  final String orderId;
  /// F-24: token anti-IDOR retornado por POST /api/client/orders. Necesario para
  /// hacer GET /api/client/orders/{id}?token=... en sesiones futuras.
  /// Backwards compat: orders viejos sin token tendran "" y el GET fallara con 404.
  final String accessToken;
  final String guestName;
  final DateTime placedAt;

  const LocalOrderRef({
    required this.orderId,
    required this.accessToken,
    required this.guestName,
    required this.placedAt,
  });

  Map<String, dynamic> toJson() => {
        'orderId': orderId,
        'accessToken': accessToken,
        'guestName': guestName,
        'placedAt': placedAt.toIso8601String(),
      };

  factory LocalOrderRef.fromJson(Map<String, dynamic> j) => LocalOrderRef(
        orderId: j['orderId'] as String,
        accessToken: j['accessToken'] as String? ?? '',  // backwards compat
        guestName: j['guestName'] as String? ?? '',
        placedAt: DateTime.parse(j['placedAt'] as String),
      );
}
