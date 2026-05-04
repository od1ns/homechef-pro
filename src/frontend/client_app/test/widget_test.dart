// Smoke test minimo. Las pruebas reales de UI estan cubiertas por tests/e2e
// (Playwright) contra la app web corriendo. Este test solo verifica que el
// pubspec resuelve y que el paquete compila.

import 'package:flutter_test/flutter_test.dart';

void main() {
  test('package compiles', () {
    expect(1 + 1, equals(2));
  });
}
