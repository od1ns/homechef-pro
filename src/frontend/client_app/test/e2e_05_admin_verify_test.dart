/// E2E-05 — Admin verifica el pago.
///
/// Test de integración contra la API real. No requiere dispositivo ni emulador
/// — corre directamente en el host con:
///
///   flutter test test/e2e_05_admin_verify_test.dart \
///     --dart-define=HCP_API_BASE=http://localhost:8080 \
///     --dart-define=ADMIN_PASSWORD="w+OC-9uhlJ0HRZ=bzcLsap5F"

import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

const _apiBase = String.fromEnvironment(
  'HCP_API_BASE',
  defaultValue: 'http://localhost:8080',
);
const _adminEmail = String.fromEnvironment(
  'ADMIN_EMAIL',
  defaultValue: 'admin@homechef.local',
);
const _adminPassword = String.fromEnvironment(
  'ADMIN_PASSWORD',
  defaultValue: 'demo1234',
);

Map<String, String> _bearer(String token) => {
      'Authorization': 'Bearer $token',
      'Content-Type': 'application/json',
    };

void main() {
  late String adminToken;
  late String orderId;
  late String orderToken;

  setUpAll(() async {
    // ── Login admin ──────────────────────────────────────────────────────
    final loginResp = await http.post(
      Uri.parse('$_apiBase/api/auth/login'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'email': _adminEmail, 'password': _adminPassword}),
    );
    expect(
      loginResp.statusCode, 200,
      reason: 'Admin login falló. Usa --dart-define=ADMIN_PASSWORD=...',
    );
    adminToken =
        (jsonDecode(loginResp.body) as Map<String, dynamic>)['accessToken']
            as String;

    // ── Crear orden de prueba ────────────────────────────────────────────
    final menuResp = await http.get(Uri.parse('$_apiBase/api/client/menu'));
    expect(menuResp.statusCode, 200, reason: 'Menú no disponible.');
    final menu = jsonDecode(menuResp.body) as List<dynamic>;
    expect(menu, isNotEmpty, reason: 'Menú vacío — aplica los seeds.');
    final dish = menu.first as Map<String, dynamic>;

    final orderResp = await http.post(
      Uri.parse('$_apiBase/api/client/orders'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'guestFullName': 'Test E2E-05',
        'guestPhone': '+58 414 0000405',
        'deliveryType': 'pickup',
        'items': [
          {'dishId': dish['id'], 'quantity': 1},
        ],
        'customerNotes': 'e2e-05-test',
      }),
    );
    expect(orderResp.statusCode, 201,
        reason: 'Fallo al crear orden: ${orderResp.body}');
    final created = jsonDecode(orderResp.body) as Map<String, dynamic>;
    orderId    = created['id']          as String;
    orderToken = created['accessToken'] as String;

    // ── Obtener totalUsd para el pago ────────────────────────────────────
    final detailResp = await http.get(
      Uri.parse('$_apiBase/api/client/orders/$orderId?token=$orderToken'),
    );
    final order = jsonDecode(detailResp.body) as Map<String, dynamic>;

    // ── Enviar pago (sin imagen — es opcional) ───────────────────────────
    final payResp = await http.post(
      Uri.parse('$_apiBase/api/client/orders/$orderId/payment'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'method': 'pago_movil',
        'amountUsd': order['totalUsd'],
        'paidCurrency': 'VES',
        'amountPaidCurrency': (order['totalUsd'] as num) * 42,
        'exchangeRateUsed': 42,
        'referenceNumber': 'E2E-REF-000405',
        'payerName': 'Tester E2E-05',
        'payerPhone': '04140000405',
      }),
    );
    expect(payResp.statusCode, 201,
        reason: 'Fallo al enviar pago: ${payResp.body}');
  });

  test('El pago aparece en pending y al verificarlo la orden pasa a paid',
      () async {
    // 1. Pago debe estar en la cola de pendientes.
    final pendingResp = await http.get(
      Uri.parse('$_apiBase/api/admin/payments/pending'),
      headers: _bearer(adminToken),
    );
    expect(pendingResp.statusCode, 200);
    final pending = jsonDecode(pendingResp.body) as List<dynamic>;
    final paymentsForOrder = pending
        .cast<Map<String, dynamic>>()
        .where((p) => p['orderId'] == orderId)
        .toList();
    expect(paymentsForOrder, isNotEmpty,
        reason: 'El pago no aparece en /api/admin/payments/pending.');
    final paymentId = paymentsForOrder.first['id'] as String;

    // 2. Admin verifica.
    final verifyResp = await http.post(
      Uri.parse('$_apiBase/api/admin/payments/$paymentId/verify'),
      headers: _bearer(adminToken),
    );
    expect(verifyResp.statusCode, 204,
        reason: 'Fallo al verificar pago $paymentId: ${verifyResp.body}');

    // 3. Orden debe estar en 'paid' (vista admin).
    final adminOrderResp = await http.get(
      Uri.parse('$_apiBase/api/admin/orders/$orderId'),
      headers: _bearer(adminToken),
    );
    expect(adminOrderResp.statusCode, 200);
    final adminOrder =
        jsonDecode(adminOrderResp.body) as Map<String, dynamic>;
    expect(adminOrder['status'], 'paid',
        reason: 'La orden debe estar en paid tras la verificación.');

    // 4. Cliente también ve 'paid'.
    final clientResp = await http.get(
      Uri.parse('$_apiBase/api/client/orders/$orderId?token=$orderToken'),
    );
    expect(clientResp.statusCode, 200);
    final clientOrder =
        jsonDecode(clientResp.body) as Map<String, dynamic>;
    expect(clientOrder['status'], 'paid',
        reason: 'El cliente debe ver status=paid.');
  });
}
