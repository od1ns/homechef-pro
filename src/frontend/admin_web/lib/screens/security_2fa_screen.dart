import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:homechef_shared/homechef_shared.dart';
import 'package:qr_flutter/qr_flutter.dart';

/// F-17: pantalla de gestion de 2FA TOTP. Estados:
///  - 2FA no habilitado -> boton "Activar 2FA" abre modal con QR + input.
///  - 2FA habilitado     -> boton "Desactivar 2FA" pide codigo y desactiva.
class Security2faScreen extends StatefulWidget {
  final HcpApi api;
  const Security2faScreen({super.key, required this.api});

  @override
  State<Security2faScreen> createState() => _Security2faScreenState();
}

class _Security2faScreenState extends State<Security2faScreen> {
  bool _busy = false;
  String? _error;
  String? _info;
  TotpSetupResult? _setupResult;
  final _verifyCtrl = TextEditingController();
  final _disableCtrl = TextEditingController();

  Future<void> _startSetup() async {
    setState(() {
      _busy = true;
      _error = null;
      _info = null;
    });
    try {
      final result = await widget.api.setup2fa();
      setState(() => _setupResult = result);
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _verifySetup() async {
    final code = _verifyCtrl.text.trim();
    if (code.isEmpty) {
      setState(() => _error = 'Ingresa el codigo del authenticator.');
      return;
    }
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await widget.api.verify2faSetup(code: code);
      setState(() {
        _setupResult = null;
        _verifyCtrl.clear();
        _info = '2FA activado correctamente. La proxima vez que inicies sesion '
                'la app pedira el codigo del authenticator.';
      });
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _disable() async {
    final code = _disableCtrl.text.trim();
    if (code.isEmpty) {
      setState(() => _error = 'Ingresa un codigo del authenticator para confirmar.');
      return;
    }
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await widget.api.disable2fa(code: code);
      setState(() {
        _disableCtrl.clear();
        _info = '2FA desactivado. Tu cuenta volvio a usar solo password.';
      });
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Seguridad / 2FA')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 640),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text('Autenticacion de dos factores (TOTP)',
                    style: Theme.of(context).textTheme.titleLarge),
                const SizedBox(height: 8),
                const Text(
                  'Aumenta la seguridad de tu cuenta exigiendo un codigo de '
                  'tu telefono ademas del password al iniciar sesion. '
                  'Compatible con Google Authenticator, Authy, 1Password, etc.',
                ),
                const SizedBox(height: 16),

                if (_info != null)
                  Card(
                    color: Colors.green.shade50,
                    child: Padding(
                      padding: const EdgeInsets.all(12),
                      child: Row(children: [
                        const Icon(Icons.check_circle, color: Colors.green),
                        const SizedBox(width: 8),
                        Expanded(child: Text(_info!)),
                      ]),
                    ),
                  ),

                if (_error != null)
                  Card(
                    color: Colors.red.shade50,
                    child: Padding(
                      padding: const EdgeInsets.all(12),
                      child: Row(children: [
                        const Icon(Icons.error_outline, color: Colors.red),
                        const SizedBox(width: 8),
                        Expanded(child: Text(_error!)),
                      ]),
                    ),
                  ),

                const SizedBox(height: 16),

                // Activar 2FA - paso 1: setup
                if (_setupResult == null) ...[
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(20),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          const Text('Activar 2FA',
                              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                          const SizedBox(height: 8),
                          const Text(
                            'Al activar, vas a ver un QR para escanearlo con tu '
                            'authenticator app. Despues vas a tener que confirmar '
                            'con el primer codigo que genere.',
                          ),
                          const SizedBox(height: 16),
                          ElevatedButton.icon(
                            onPressed: _busy ? null : _startSetup,
                            icon: const Icon(Icons.security),
                            label: const Text('Activar 2FA'),
                          ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 24),
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(20),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          const Text('Desactivar 2FA',
                              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                          const SizedBox(height: 4),
                          const Text(
                            'Si actualmente tienes 2FA activado y querias desactivarlo, '
                            'ingresa un codigo valido del authenticator para confirmar.',
                          ),
                          const SizedBox(height: 12),
                          TextField(
                            controller: _disableCtrl,
                            decoration: const InputDecoration(
                              labelText: 'Codigo TOTP (6 digitos)',
                              hintText: '123456',
                            ),
                            keyboardType: TextInputType.number,
                            maxLength: 8,
                          ),
                          const SizedBox(height: 8),
                          OutlinedButton.icon(
                            onPressed: _busy ? null : _disable,
                            icon: const Icon(Icons.lock_open),
                            label: const Text('Desactivar 2FA'),
                          ),
                        ],
                      ),
                    ),
                  ),
                ],

                // Activar 2FA - paso 2: QR + confirmar codigo
                if (_setupResult != null) ...[
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(20),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          const Text('Paso 1: escanea este QR con tu authenticator',
                              style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                          const SizedBox(height: 16),
                          Center(
                            child: Container(
                              padding: const EdgeInsets.all(16),
                              color: Colors.white,
                              child: QrImageView(
                                data: _setupResult!.authenticatorUri,
                                size: 240,
                                version: QrVersions.auto,
                              ),
                            ),
                          ),
                          const SizedBox(height: 12),
                          const Text('Si no podes escanear, ingresa esta key manualmente:',
                              style: TextStyle(fontSize: 12)),
                          const SizedBox(height: 4),
                          Row(children: [
                            Expanded(
                              child: SelectableText(
                                _setupResult!.sharedKey,
                                style: const TextStyle(
                                  fontFamily: 'monospace',
                                  fontSize: 14,
                                ),
                              ),
                            ),
                            IconButton(
                              icon: const Icon(Icons.copy, size: 18),
                              tooltip: 'Copiar',
                              onPressed: () {
                                Clipboard.setData(ClipboardData(text: _setupResult!.sharedKey));
                                ScaffoldMessenger.of(context).showSnackBar(
                                    const SnackBar(content: Text('Copiado al portapapeles')));
                              },
                            ),
                          ]),
                          const Divider(height: 32),
                          const Text('Paso 2: ingresa el codigo de 6 digitos que muestra la app',
                              style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                          const SizedBox(height: 12),
                          TextField(
                            controller: _verifyCtrl,
                            decoration: const InputDecoration(
                              labelText: 'Codigo TOTP',
                              hintText: '123456',
                            ),
                            keyboardType: TextInputType.number,
                            maxLength: 8,
                            autofocus: true,
                          ),
                          const SizedBox(height: 8),
                          Row(children: [
                            Expanded(
                              child: ElevatedButton.icon(
                                onPressed: _busy ? null : _verifySetup,
                                icon: _busy
                                    ? const SizedBox(
                                        width: 16, height: 16,
                                        child: CircularProgressIndicator(strokeWidth: 2))
                                    : const Icon(Icons.check),
                                label: const Text('Confirmar y activar'),
                              ),
                            ),
                            const SizedBox(width: 8),
                            OutlinedButton(
                              onPressed: _busy ? null : () => setState(() {
                                _setupResult = null;
                                _verifyCtrl.clear();
                                _error = null;
                              }),
                              child: const Text('Cancelar'),
                            ),
                          ]),
                        ],
                      ),
                    ),
                  ),
                ],
              ],
            ),
          ),
        ),
      ),
    );
  }

  @override
  void dispose() {
    _verifyCtrl.dispose();
    _disableCtrl.dispose();
    super.dispose();
  }
}
