import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';

/// F-17: login con flujo de 2 pasos cuando el user tiene 2FA activado.
/// Paso 1: email + password.
/// Paso 2: si requires2fa, mostrar campo de codigo TOTP.
class LoginScreen extends StatefulWidget {
  final HcpApi api;
  final Future<void> Function(AuthResult auth) onLoggedIn;
  const LoginScreen({super.key, required this.api, required this.onLoggedIn});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _emailCtrl = TextEditingController();
  final _passCtrl = TextEditingController();
  final _codeCtrl = TextEditingController();
  bool _busy = false;
  String? _error;

  // F-17: cuando paso 1 retorna requires2fa, guardamos el partial token
  // y mostramos el campo de codigo TOTP.
  String? _partialToken;
  String? _partialEmail;

  Future<void> _login() async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final auth = await widget.api.login(_emailCtrl.text.trim(), _passCtrl.text);
      if (auth.requires2fa) {
        // Paso 2: pedir codigo TOTP.
        setState(() {
          _partialToken = auth.partialToken;
          _partialEmail = auth.email;
        });
        return;
      }
      // No 2FA -> login completo, validar rol y entrar.
      await _validateRoleAndEnter(auth);
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } catch (e) {
      setState(() => _error = '$e');
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _login2fa() async {
    final code = _codeCtrl.text.trim();
    if (code.isEmpty) {
      setState(() => _error = 'Ingresa el codigo de tu authenticator.');
      return;
    }
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final auth = await widget.api.login2fa(
        partialToken: _partialToken!,
        code: code,
      );
      await _validateRoleAndEnter(auth);
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } catch (e) {
      setState(() => _error = '$e');
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _validateRoleAndEnter(AuthResult auth) async {
    final allowed =
        auth.roles.contains('Admin') || auth.roles.contains('Cashier');
    if (!allowed) {
      await widget.api.logout();
      setState(() => _error = 'Esta cuenta no tiene rol Admin o Cajero.');
      return;
    }
    await widget.onLoggedIn(auth);
  }

  void _cancelTwoFactor() {
    setState(() {
      _partialToken = null;
      _partialEmail = null;
      _codeCtrl.clear();
      _error = null;
    });
  }

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    return Scaffold(
      backgroundColor: palette.bg,
      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 520),
          child: Card(
            child: Padding(
              padding: const EdgeInsets.all(40),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text('HomeChef Pro',
                      style: Theme.of(context).textTheme.displaySmall,
                      textAlign: TextAlign.center),
                  const SizedBox(height: 4),
                  Text(_partialToken == null
                          ? 'Panel de administracion'
                          : 'Verificacion de dos factores',
                      style: Theme.of(context).textTheme.bodyMedium,
                      textAlign: TextAlign.center),
                  const SizedBox(height: 32),

                  // PASO 1: email + password
                  if (_partialToken == null) ...[
                    TextField(
                      controller: _emailCtrl,
                      decoration: const InputDecoration(labelText: 'Email'),
                      keyboardType: TextInputType.emailAddress,
                      autocorrect: false,
                      textInputAction: TextInputAction.next,
                    ),
                    const SizedBox(height: 16),
                    TextField(
                      controller: _passCtrl,
                      decoration: const InputDecoration(labelText: 'Contrasena'),
                      obscureText: true,
                      textInputAction: TextInputAction.done,
                      onSubmitted: (_) => _login(),
                    ),
                  ],

                  // PASO 2: codigo TOTP
                  if (_partialToken != null) ...[
                    Text('Cuenta: \${_partialEmail ?? ""}',
                        textAlign: TextAlign.center,
                        style: const TextStyle(fontSize: 12, color: Colors.grey)),
                    const SizedBox(height: 16),
                    const Text(
                      'Ingresa el codigo de 6 digitos que muestra tu authenticator app.',
                      textAlign: TextAlign.center,
                    ),
                    const SizedBox(height: 16),
                    TextField(
                      controller: _codeCtrl,
                      decoration: const InputDecoration(
                        labelText: 'Codigo TOTP',
                        hintText: '123456',
                      ),
                      keyboardType: TextInputType.number,
                      maxLength: 8,
                      autofocus: true,
                      textInputAction: TextInputAction.done,
                      onSubmitted: (_) => _login2fa(),
                    ),
                  ],

                  if (_error != null) ...[
                    const SizedBox(height: 16),
                    Container(
                      padding: const EdgeInsets.all(12),
                      decoration: BoxDecoration(
                        color: palette.redSoft,
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Text(_error!, style: TextStyle(color: palette.red)),
                    ),
                  ],
                  const SizedBox(height: 24),

                  if (_partialToken == null)
                    ElevatedButton(
                      onPressed: _busy ? null : _login,
                      child: _busy
                          ? const SizedBox(
                              height: 18,
                              width: 18,
                              child: CircularProgressIndicator(strokeWidth: 2))
                          : const Text('Entrar'),
                    )
                  else ...[
                    ElevatedButton(
                      onPressed: _busy ? null : _login2fa,
                      child: _busy
                          ? const SizedBox(
                              height: 18,
                              width: 18,
                              child: CircularProgressIndicator(strokeWidth: 2))
                          : const Text('Verificar codigo'),
                    ),
                    const SizedBox(height: 8),
                    TextButton(
                      onPressed: _busy ? null : _cancelTwoFactor,
                      child: const Text('Volver al login'),
                    ),
                  ],
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  @override
  void dispose() {
    _emailCtrl.dispose();
    _passCtrl.dispose();
    _codeCtrl.dispose();
    super.dispose();
  }
}
