import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';

import 'app.dart';

void main() {
  const baseUrl = String.fromEnvironment(
    'HCP_API_BASE',
    defaultValue: 'http://localhost:5000',
  );
  final api = HcpApi(ApiClient(baseUri: Uri.parse(baseUrl)));
  runApp(HomeChefKitchenApp(api: api));
}
