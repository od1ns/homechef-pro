import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';

import '../../app_state.dart';

class CustomerLoginScreen extends StatefulWidget {
  final AppState state;
  const CustomerLoginScreen({super.key, required this.state});

  @override
  State<CustomerLoginScreen> createState() => _CustomerLoginScreenState();
}

class _CustomerLoginScreenState extends State<CustomerLoginScreen>
    with SingleTickerProviderStateMixin {
  late final TabController _tab = TabController(length: 2, vsync: this);

  @override
  void dispose() {
    _tab.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    return Scaffold(
      backgroundColor: palette.bg,
      appBar: AppBar(
        backgroundColor: palette.bg,
        elevation: 0,
        title: const Text('Cuenta'),
      ),
      body: SafeArea(
        child: Column(
          children: [
            TabBar(
              controller: _tab,
              tabs: const [
                Tab(text: 'Entrar'),
                Tab(text: 'Crear cuenta'),
              ],
            ),
            Expanded(
              child: TabBarView(
                controller: _tab,
                children: [
                  _LoginForm(state: widget.state),
                  _RegisterForm(state: widget.state),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _LoginForm extends StatefulWidget {
  final AppState state;
  const _LoginForm({required this.state});

  @override
  State<_LoginForm> createState() => _LoginFormState();
}

class _LoginFormState extends State<_LoginForm> {
  final _email = TextEditingController();
  final _pass = TextEditingController();
  bool _busy = false;
  String? _error;

  Future<void> _login() async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final auth = await widget.state.api.login(_email.text.trim(), _pass.text);
      await widget.state.setSessionFromAuth(auth);
      if (mounted) Navigator.pop(context);
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } catch (e) {
      setState(() => _error = '$e');
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    return Padding(
      padding: const EdgeInsets.all(24),
      child: ListView(
        children: [
          const SizedBox(height: 24),
          TextField(
            controller: _email,
            decoration: const InputDecoration(labelText: 'Email'),
            keyboardType: TextInputType.emailAddress,
            autocorrect: false,
            textInputAction: TextInputAction.next,
          ),
          const SizedBox(height: 12),
          TextField(
            controller: _pass,
            decoration: const InputDecoration(labelText: 'Contraseña'),
            obscureText: true,
            textInputAction: TextInputAction.done,
            onSubmitted: (_) => _login(),
          ),
          if (_error != null) ...[
            const SizedBox(height: 12),
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
          ElevatedButton(
            onPressed: _busy ? null : _login,
            child: _busy
                ? const SizedBox(
                    width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
                : const Text('Entrar'),
          ),
        ],
      ),
    );
  }
}

class _RegisterForm extends StatefulWidget {
  final AppState state;
  const _RegisterForm({required this.state});

  @override
  State<_RegisterForm> createState() => _RegisterFormState();
}

class _RegisterFormState extends State<_RegisterForm> {
  final _email = TextEditingController();
  final _pass = TextEditingController();
  final _name = TextEditingController();
  final _phone = TextEditingController();
  bool _busy = false;
  String? _error;

  Future<void> _register() async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final auth = await widget.state.api.register(
        email: _email.text.trim(),
        password: _pass.text,
        fullName: _name.text.trim(),
        phone: _phone.text.trim().isEmpty ? null : _phone.text.trim(),
      );
      await widget.state.setSessionFromAuth(auth);
      if (mounted) Navigator.pop(context);
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } catch (e) {
      setState(() => _error = '$e');
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    return Padding(
      padding: const EdgeInsets.all(24),
      child: ListView(
        children: [
          const SizedBox(height: 12),
          Text(
              'Tu cuenta te permite dejar reseñas, ver tu historial completo y '
              'guardar direcciones favoritas.',
              style: Theme.of(context).textTheme.bodyMedium),
          const SizedBox(height: 16),
          TextField(
              controller: _name,
              decoration: const InputDecoration(labelText: 'Nombre completo')),
          const SizedBox(height: 8),
          TextField(
              controller: _email,
              decoration: const InputDecoration(labelText: 'Email'),
              keyboardType: TextInputType.emailAddress,
              autocorrect: false),
          const SizedBox(height: 8),
          TextField(
              controller: _phone,
              decoration: const InputDecoration(labelText: 'Teléfono (opcional)'),
              keyboardType: TextInputType.phone),
          const SizedBox(height: 8),
          TextField(
              controller: _pass,
              decoration:
                  const InputDecoration(labelText: 'Contraseña (mín. 8 caracteres)'),
              obscureText: true),
          if (_error != null) ...[
            const SizedBox(height: 12),
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
          ElevatedButton(
            onPressed: _busy ? null : _register,
            child: _busy
                ? const SizedBox(
                    width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
                : const Text('Crear cuenta'),
          ),
        ],
      ),
    );
  }
}
