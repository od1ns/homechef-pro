/// E2E-04 — Cliente sube comprobante y envía pago.
///
/// Cubre el paso 4 del smoke-deep: Maria abre PaymentScreen, completa
/// el formulario y envía el pago sin imagen (opcional en el backend).
///
/// Ejecutar:
///   flutter test integration_test/e2e_04_payment_test.dart \
///     --dart-define=HCP_API_BASE=http://localhost:8080
///
/// Con flutter drive (dispositivo/emulador conectado):
///   flutter drive \
///     --driver=test_driver/integration_test.dart \
///     --target=integration_test/e2e_04_payment_test.dart \
///     --dart-define=HCP_API_BASE=http://localhost:8080

library e2e_04;

import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:homechef_shared/homechef_shared.dart';
import 'package:http/http.dart' as http;
import 'package:integration_test/integration_test.dart';

import 'package:homechef_client/app_state.dart';
import 'package:homechef_client/screens/payment_screen.dart';

// ---------------------------------------------------------------------------
// Constantes configurables via --dart-define
// ---------------------------------------------------------------------------
const _apiBase = String.fromEnvironment(
  'HCP_API_BASE',
  defaultValue: 'http://localhost:8080',
);

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  // Estado compartido entre setUpAll y el test.
  late String orderId;
  late String orderToken;
  late Order testOrder;

  // ---------------------------------------------------------------------------
  // Preparación: crea una orden real via REST antes del test de UI.
  // ---------------------------------------------------------------------------
  setUpAll(() async {
    // 1. Obtener menú público (sin auth).
    final menuResp = await http.get(Uri.parse('$_apiBase/api/client/menu'));
    expect(
      menuResp.statusCode,
      200,
      reason: 'El menú no respondió 200. ¿Está el backend corriendo?',
    );
    final menu = jsonDecode(menuResp.body) as List<dynamic>;
    expect(menu, isNotEmpty, reason: 'Menú vacío — aplica los seeds primero.');
    final dish = menu.first as Map<String, dynamic>;

    // 2. Crear orden de prueba como guest.
    final orderResp = await http.post(
      Uri.parse('$_apiBase/api/client/orders'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'guestFullName': 'Test E2E-04',
        'guestPhone': '+58 414 0000404',
        'deliveryType': 'pickup',
        'items': [
          {'dishId': dish['id'], 'quantity': 1},
        ],
        'customerNotes': 'e2e-04-integration-test',
      }),
    );
    expect(
      orderResp.statusCode,
      201,
      reason: 'No se pudo crear la orden: ${orderResp.body}',
    );
    final created = jsonDecode(orderResp.body) as Map<String, dynamic>;
    orderId    = created['id']          as String;
    orderToken = created['accessToken'] as String;

    // 3. Obtener detalles completos de la orden para construir Order.
    final detailResp = await http.get(
      Uri.parse('$_apiBase/api/client/orders/$orderId?token=$orderToken'),
    );
    expect(detailResp.statusCode, 200);
    testOrder = Order.fromJson(
      jsonDecode(detailResp.body) as Map<String, dynamic>,
    );
  });

  // ---------------------------------------------------------------------------
  // Test de UI: PaymentScreen
  // ---------------------------------------------------------------------------
  testWidgets(
    'E2E-04: PaymentScreen — llena formulario y envía pago',
    (tester) async {
      bool paymentSubmitted = false;

      // Construir AppState con API real (mismo backend del setUpAll).
      final api = HcpApi(
        ApiClient(baseUri: Uri.parse(_apiBase)),
      );
      final state = AppState(api: api);

      // Montar PaymentScreen directamente (sin pasar por la app completa
      // para no necesitar Firebase ni el shell de navegación).
      await tester.pumpWidget(
        MaterialApp(
          theme: hcpThemeData(HcpThemeName.editorial),
          home: PaymentScreen(
            state: state,
            order: testOrder,
            onPaymentSubmitted: () => paymentSubmitted = true,
          ),
        ),
      );
      await tester.pumpAndSettle();

      // ── Verificar que la pantalla cargó correctamente ──────────────────
      expect(
        find.text('Pagar ${testOrder.orderNumber}'),
        findsOneWidget,
        reason: 'El AppBar debe mostrar el número de orden.',
      );
      expect(
        find.text('Enviar pago'),
        findsOneWidget,
        reason: 'El botón de submit debe estar visible.',
      );

      // ── Completar campos del formulario ───────────────────────────────
      // Número de referencia
      await tester.enterText(
        find.widgetWithText(TextField, 'Número de referencia (opcional)'),
        'E2E-REF-000404',
      );

      // Pagado por (nombre)
      await tester.enterText(
        find.widgetWithText(TextField, 'Pagado por (opcional)'),
        'Tester E2E-04',
      );

      // Teléfono del pagador
      await tester.enterText(
        find.widgetWithText(TextField, 'Teléfono del pagador (opcional)'),
        '04140000404',
      );

      // Scroll hasta el botón de enviar y presionarlo.
      await tester.scrollUntilVisible(
        find.text('Enviar pago'),
        300,
        scrollable: find.byType(ListView).first,
      );
      await tester.tap(find.text('Enviar pago'));

      // Esperar respuesta de la API (hasta 15 s en CI con red real).
      await tester.pumpAndSettle(const Duration(seconds: 15));

      // ── Aserciones de UI ──────────────────────────────────────────────
      expect(
        paymentSubmitted,
        isTrue,
        reason:
            'onPaymentSubmitted no fue llamado — el backend rechazó el pago.',
      );

      // ── Verificación en el backend ────────────────────────────────────
      final updatedResp = await http.get(
        Uri.parse('$_apiBase/api/client/orders/$orderId?token=$orderToken'),
      );
      expect(updatedResp.statusCode, 200);
      final updated = jsonDecode(updatedResp.body) as Map<String, dynamic>;
      expect(
        updated['status'],
        'payment_verifying',
        reason:
            'Tras el submit, la orden debe estar en payment_verifying.',
      );
    },
    timeout: const Timeout(Duration(minutes: 2)),
  );
}
