/// E2E-05 — Admin verifica el pago.
///
/// Cubre el paso 5 del smoke-deep: el admin lista los pagos pendientes,
/// verifica el del cliente y la orden pasa a 'paid'.
///
/// Este test es de integración contra la API (no UI de admin_web, que es
/// una app separada). Usa integration_test para correr dentro del runner
/// unificado de flutter drive / flutter test.
///
/// Ejecutar:
///   flutter test integration_test/e2e_05_admin_verify_test.dart \
///     --dart-define=HCP_API_BASE=http://localhost:8080 \
///     --dart-define=ADMIN_EMAIL=admin@homechef.local \
///     --dart-define=ADMIN_PASSWORD=demo1234
///
/// Para staging (con la contraseña real del .env.staging.local):
///   flutter test integration_test/e2e_05_admin_verify_test.dart \
///     --dart-define=HCP_API_BASE=https://<tunnel>.trycloudflare.com \
///     --dart-define=ADMIN_EMAIL=admin@homechef.local \
///     --dart-define=ADMIN_PASSWORD="w+OC-9uhlJ0HRZ=bzcLsap5F"

library e2e_05;

import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:integration_test/integration_test.dart';

// ---------------------------------------------------------------------------
// Constantes configurables via --dart-define
// ---------------------------------------------------------------------------
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

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------
Map<String, String> _bearerHeaders(String token) => {
      'Authorization': 'Bearer $token',
      'Content-Type': 'application/json',
    };

Future<String> _adminLogin() async {
  final resp = await http.post(
    Uri.parse('$_apiBase/api/auth/login'),
    headers: {'Content-Type': 'application/json'},
    body: jsonEncode({'email': _adminEmail, 'password': _adminPassword}),
  );
  expect(
    resp.statusCode,
    200,
    reason:
        'Admin login falló (${resp.statusCode}). '
        '¿Contraseña correcta? Usa --dart-define=ADMIN_PASSWORD=...',
  );
  return (jsonDecode(resp.body) as Map<String, dynamic>)['accessToken'] as String;
}

Future<Map<String, dynamic>> _createOrderWithPayment(String adminToken) async {
  // 1. Menú
  final menuResp = await http.get(Uri.parse('$_apiBase/api/client/menu'));
  expect(menuResp.statusCode, 200, reason: 'Menú no disponible.');
  final menu = jsonDecode(menuResp.body) as List<dynamic>;
  expect(menu, isNotEmpty, reason: 'Menú vacío — aplica los seeds.');
  final dish = menu.first as Map<String, dynamic>;

  // 2. Crear orden
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
      'customerNotes': 'e2e-05-integration-test',
    }),
  );
  expect(orderResp.statusCode, 201, reason: 'Fallo al crear orden: ${orderResp.body}');
  final created = jsonDecode(orderResp.body) as Map<String, dynamic>;
  final orderId    = created['id']          as String;
  final orderToken = created['accessToken'] as String;

  // 3. Obtener orden completa
  final detailResp = await http.get(
    Uri.parse('$_apiBase/api/client/orders/$orderId?token=$orderToken'),
  );
  final order = jsonDecode(detailResp.body) as Map<String, dynamic>;

  // 4. Enviar pago (sin imagen — es opcional)
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
  expect(
    payResp.statusCode,
    200,
    reason: 'Fallo al enviar pago: ${payResp.body}',
  );

  return {'orderId': orderId, 'orderToken': orderToken};
}

// ---------------------------------------------------------------------------
// Test
// ---------------------------------------------------------------------------
void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  late String adminToken;
  late String orderId;
  late String orderToken;

  setUpAll(() async {
    adminToken = await _adminLogin();
    final result = await _createOrderWithPayment(adminToken);
    orderId    = result['orderId']!;
    orderToken = result['orderToken']!;
  });

  testWidgets(
    'E2E-05: Admin verifica el pago → orden pasa a paid',
    (tester) async {
      // ── 1. Verificar que el pago aparece en la cola de pendientes ──────
      final pendingResp = await http.get(
        Uri.parse('$_apiBase/api/admin/payments/pending'),
        headers: _bearerHeaders(adminToken),
      );
      expect(
        pendingResp.statusCode,
        200,
        reason: 'No se pudo obtener pagos pendientes.',
      );
      final pending = jsonDecode(pendingResp.body) as List<dynamic>;
      final paymentForOrder = pending
          .cast<Map<String, dynamic>>()
          .where((p) => p['orderId'] == orderId)
          .toList();
      expect(
        paymentForOrder,
        isNotEmpty,
        reason:
            'El pago de la orden $orderId no aparece en /api/admin/payments/pending.',
      );
      final paymentId = paymentForOrder.first['id'] as String;

      // ── 2. Admin verifica el pago ──────────────────────────────────────
      final verifyResp = await http.post(
        Uri.parse('$_apiBase/api/admin/payments/$paymentId/verify'),
        headers: _bearerHeaders(adminToken),
      );
      expect(
        verifyResp.statusCode,
        200,
        reason: 'Fallo al verificar el pago $paymentId: ${verifyResp.body}',
      );

      // ── 3. Verificar que la orden pasó a 'paid' ────────────────────────
      final orderResp = await http.get(
        Uri.parse('$_apiBase/api/admin/orders/$orderId'),
        headers: _bearerHeaders(adminToken),
      );
      expect(orderResp.statusCode, 200);
      final updatedOrder =
          jsonDecode(orderResp.body) as Map<String, dynamic>;
      expect(
        updatedOrder['status'],
        'paid',
        reason:
            'Tras verificar el pago, la orden debería estar en status=paid.',
      );

      // ── 4. El cliente también ve el nuevo estado ───────────────────────
      final clientResp = await http.get(
        Uri.parse('$_apiBase/api/client/orders/$orderId?token=$orderToken'),
      );
      expect(clientResp.statusCode, 200);
      final clientView =
          jsonDecode(clientResp.body) as Map<String, dynamic>;
      expect(
        clientView['status'],
        'paid',
        reason:
            'El endpoint del cliente debe reflejar status=paid tras la verificación.',
      );
    },
    timeout: const Timeout(Duration(minutes: 2)),
  );
}
