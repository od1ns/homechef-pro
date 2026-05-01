import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';
import 'package:intl/date_symbol_data_local.dart';

import 'app.dart';
import 'app_state.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Inicializa simbolos de date/number para los locales que la app usa.
  await initializeDateFormatting('es', null);
  await initializeDateFormatting('en', null);

  // Default API base; override at runtime con `--dart-define=HCP_API_BASE=https://...`
  const baseUrl = String.fromEnvironment(
    'HCP_API_BASE',
    defaultValue: 'http://localhost:8080',
  );

  final api = HcpApi(ApiClient(baseUri: Uri.parse(baseUrl)));
  runApp(HomeChefClientApp(state: AppState(api: api)));
}
