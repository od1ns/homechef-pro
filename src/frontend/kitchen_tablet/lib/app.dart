import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';

import 'screens/login_screen.dart';
import 'screens/queue_screen.dart';

class HomeChefKitchenApp extends StatefulWidget {
  final HcpApi api;
  const HomeChefKitchenApp({super.key, required this.api});

  @override
  State<HomeChefKitchenApp> createState() => _HomeChefKitchenAppState();
}

class _HomeChefKitchenAppState extends State<HomeChefKitchenApp> {
  bool? _authenticated;

  @override
  void initState() {
    super.initState();
    _checkSession();
  }

  Future<void> _checkSession() async {
    final session = await widget.api.auth.readSession();
    final ok = session != null && session.expiresAt.isAfter(DateTime.now());
    if (mounted) setState(() => _authenticated = ok);
  }

  Future<void> _onLoggedIn() async {
    setState(() => _authenticated = true);
  }

  Future<void> _onLogout() async {
    await widget.api.logout();
    if (mounted) setState(() => _authenticated = false);
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'HomeChef Pro · Cocina',
      debugShowCheckedModeBanner: false,
      theme: hcpThemeData(HcpThemeName.caribbean),
      home: switch (_authenticated) {
        null => const Scaffold(body: Center(child: CircularProgressIndicator())),
        false => LoginScreen(api: widget.api, onLoggedIn: _onLoggedIn),
        true => QueueScreen(api: widget.api, onLogout: _onLogout),
      },
    );
  }
}
