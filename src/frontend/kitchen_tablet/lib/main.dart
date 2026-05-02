import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';
import 'package:intl/date_symbol_data_local.dart';

import 'app.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Inicializa simbolos de date/number para el locale es.
  await initializeDateFormatting('es', null);

  const baseUrl = String.fromEnvironment(
    'HCP_API_BASE',
    defaultValue: 'http://localhost:8080',
  );
  final api = HcpApi(ApiClient(baseUri: Uri.parse(baseUrl)));
  runApp(HomeChefKitchenApp(api: api));
}
