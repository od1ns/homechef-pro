import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';
import 'package:intl/date_symbol_data_local.dart';

import 'app.dart';
import 'app_state.dart';

/// Etapa 5: handler de mensajes en background (debe ser top-level).
@pragma('vm:entry-point')
Future<void> _firebaseMessagingBackgroundHandler(RemoteMessage message) async {
  // Firebase ya muestra la notificación automáticamente en background/terminated.
  // Aquí solo necesitamos asegurarnos de que Firebase esté inicializado.
  await Firebase.initializeApp();
}

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Inicializa símbolos de date/number para los locales que la app usa.
  await initializeDateFormatting('es', null);
  await initializeDateFormatting('en', null);

  // Etapa 5: inicializar Firebase.
  await Firebase.initializeApp();
  FirebaseMessaging.onBackgroundMessage(_firebaseMessagingBackgroundHandler);

  // Solicitar permiso de notificaciones (Android 13+ lo requiere explícitamente).
  await FirebaseMessaging.instance.requestPermission(
    alert: true,
    badge: true,
    sound: true,
  );

  // Default API base; override al compilar con `--dart-define=HCP_API_BASE=https://...`
  const baseUrl = String.fromEnvironment(
    'HCP_API_BASE',
    defaultValue: 'http://localhost:8080',
  );

  final api = HcpApi(ApiClient(baseUri: Uri.parse(baseUrl)));
  runApp(HomeChefClientApp(state: AppState(api: api)));
}
