import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';
import 'package:intl/date_symbol_data_local.dart';

import 'app.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Inicializa los simbolos de date/number para los locales que el admin usa.
  // Sin esto DateFormat tira LocaleDataException al primer formato.
  await initializeDateFormatting('es', null);
  await initializeDateFormatting('en', null);

  const baseUrl = String.fromEnvironment(
    'HCP_API_BASE',
    defaultValue: 'http://localhost:8080',
  );
  final api = HcpApi(ApiClient(baseUri: Uri.parse(baseUrl)));
  runApp(HomeChefAdminApp(api: api));
}
