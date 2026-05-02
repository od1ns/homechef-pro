import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';

import 'screens/login_screen.dart';
import 'shell/admin_shell.dart';

class HomeChefAdminApp extends StatefulWidget {
  final HcpApi api;
  const HomeChefAdminApp({super.key, required this.api});

  @override
  State<HomeChefAdminApp> createState() => _HomeChefAdminAppState();
}

class _HomeChefAdminAppState extends State<HomeChefAdminApp> {
  bool? _authenticated;
  String _fullName = '';
  List<String> _roles = const [];
  final GlobalKey<ScaffoldMessengerState> _messengerKey = GlobalKey();

  late final void Function() _onUnauthorized = _handleUnauthorized;

  @override
  void initState() {
    super.initState();
    widget.api.client.addUnauthorizedListener(_onUnauthorized);
    _checkSession();
  }

  @override
  void dispose() {
    widget.api.client.removeUnauthorizedListener(_onUnauthorized);
    super.dispose();
  }

  /// Disparado por ApiClient cuando un endpoint devuelve 401 (token expirado).
  /// Limpia el estado local y muestra un mensaje suave al usuario.
  void _handleUnauthorized() {
    if (!mounted || _authenticated == false) return;
    setState(() {
      _authenticated = false;
      _fullName = '';
      _roles = const [];
    });
    _messengerKey.currentState?.showSnackBar(
      const SnackBar(
        content: Text('Tu sesion expiro. Por favor inicia sesion de nuevo.'),
        duration: Duration(seconds: 4),
      ),
    );
  }

  Future<void> _checkSession() async {
    final session = await widget.api.auth.readSession();
    final ok = session != null
        && session.expiresAt.isAfter(DateTime.now())
        && (session.roles.contains('Admin') || session.roles.contains('Cashier'));
    if (mounted) {
      setState(() {
        _authenticated = ok;
        _roles = session?.roles ?? const [];
      });
    }
  }

  Future<void> _onLoggedIn(AuthResult auth) async {
    if (mounted) {
      setState(() {
        _authenticated = true;
        _fullName = auth.fullName;
        _roles = auth.roles;
      });
    }
  }

  Future<void> _onLogout() async {
    await widget.api.logout();
    if (mounted) {
      setState(() {
        _authenticated = false;
        _fullName = '';
        _roles = const [];
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'HomeChef Pro · Admin',
      debugShowCheckedModeBanner: false,
      theme: hcpThemeData(HcpThemeName.plum),
      scaffoldMessengerKey: _messengerKey,
      home: switch (_authenticated) {
        null => const Scaffold(body: Center(child: CircularProgressIndicator())),
        false => LoginScreen(api: widget.api, onLoggedIn: _onLoggedIn),
        true => AdminShell(
            api: widget.api,
            fullName: _fullName,
            roles: _roles,
            onLogout: _onLogout,
          ),
      },
    );
  }
}
