import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';

import 'app.dart';
import 'app_state.dart';

void main() {
  // Default API base; override at runtime with `--dart-define=HCP_API_BASE=https://...`
  const baseUrl = String.fromEnvironment(
    'HCP_API_BASE',
    defaultValue: 'http://localhost:5000',
  );

  final api = HcpApi(ApiClient(baseUri: Uri.parse(baseUrl)));
  runApp(HomeChefClientApp(state: AppState(api: api)));
}
