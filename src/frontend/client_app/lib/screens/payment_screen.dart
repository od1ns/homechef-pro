import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';
import 'package:image_picker/image_picker.dart';

import '../app_state.dart';

class PaymentScreen extends StatefulWidget {
  final AppState state;
  final Order order;
  final VoidCallback onPaymentSubmitted;
  const PaymentScreen({
    super.key,
    required this.state,
    required this.order,
    required this.onPaymentSubmitted,
  });

  @override
  State<PaymentScreen> createState() => _PaymentScreenState();
}

class _PaymentScreenState extends State<PaymentScreen> {
  String _method = 'pago_movil';
  String _paidCurrency = 'VES';
  late final TextEditingController _amountUsdCtrl =
      TextEditingController(text: widget.order.totalUsd.toStringAsFixed(2));
  late final TextEditingController _amountVesCtrl = TextEditingController(
      text: (widget.order.totalVesAtOrderTime ?? widget.order.totalUsd * 40)
          .toStringAsFixed(2));
  final _exchangeRateCtrl = TextEditingController(text: '40');
  final _referenceCtrl = TextEditingController();
  final _payerNameCtrl = TextEditingController();
  final _payerPhoneCtrl = TextEditingController();

  XFile? _picked;
  Uint8List? _previewBytes;
  bool _busy = false;
  String? _error;

  static const _methods = [
    ('pago_movil',   'Pago Móvil'),
    ('transfer_ves', 'Transferencia VES'),
    ('transfer_usd', 'Transferencia USD'),
    ('zelle',        'Zelle'),
    ('binance_pay',  'Binance Pay'),
    ('cash',         'Efectivo (al recoger)'),
  ];

  Future<void> _pickImage(ImageSource source) async {
    final picker = ImagePicker();
    final picked = await picker.pickImage(
      source: source,
      maxWidth: 1600,
      imageQuality: 85,
    );
    if (picked == null) return;
    final bytes = await picked.readAsBytes();
    setState(() {
      _picked = picked;
      _previewBytes = bytes;
    });
  }

  Future<void> _submit() async {
    // Capturamos el navigator ANTES de cualquier await — despues del await el
    // widget puede estar deactivated y `context` ya no resuelve ancestros.
    final navigator = Navigator.of(context);

    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      String? proofUrl;
      if (_picked != null && _previewBytes != null) {
        final lower = _picked!.name.toLowerCase();
        final mime = lower.endsWith('.png')
            ? 'image/png'
            : lower.endsWith('.webp')
                ? 'image/webp'
                : 'image/jpeg';
        proofUrl = await widget.state.api.uploadPaymentProof(
          bytes: _previewBytes!,
          filename: _picked!.name,
          contentType: mime,
        );
      }

      await widget.state.api.submitPayment(
        orderId: widget.order.id,
        method: _method,
        amountUsd: double.tryParse(_amountUsdCtrl.text.trim()) ?? 0,
        paidCurrency: _paidCurrency,
        amountPaidCurrency: _paidCurrency == 'VES'
            ? (double.tryParse(_amountVesCtrl.text.trim()) ?? 0)
            : (double.tryParse(_amountUsdCtrl.text.trim()) ?? 0),
        exchangeRateUsed: _paidCurrency == 'VES'
            ? double.tryParse(_exchangeRateCtrl.text.trim())
            : null,
        referenceNumber:
            _referenceCtrl.text.trim().isEmpty ? null : _referenceCtrl.text.trim(),
        proofImageUrl: proofUrl,
        payerName:
            _payerNameCtrl.text.trim().isEmpty ? null : _payerNameCtrl.text.trim(),
        payerPhone:
            _payerPhoneCtrl.text.trim().isEmpty ? null : _payerPhoneCtrl.text.trim(),
      );

      // Notificamos al caller (snackbar en orders_screen) y cerramos la pantalla.
      // Usamos el navigator capturado, no `context` directo.
      widget.onPaymentSubmitted();
      navigator.pop();
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.message);
    } catch (e) {
      if (mounted) setState(() => _error = '$e');
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    return Scaffold(
      backgroundColor: palette.bg,
      appBar: AppBar(
        backgroundColor: palette.bg,
        title: Text('Pagar ${widget.order.orderNumber}'),
      ),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('Total a pagar',
                        style: Theme.of(context).textTheme.bodyMedium),
                    Text('\$${widget.order.totalUsd.toStringAsFixed(2)} USD',
                        style: Theme.of(context).textTheme.displaySmall),
                    if (widget.order.totalVesAtOrderTime != null)
                      Text(
                          'Equivalente: Bs ${widget.order.totalVesAtOrderTime!.toStringAsFixed(2)}',
                          style: Theme.of(context).textTheme.bodySmall),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 16),
            Text('Método de pago',
                style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 4),
            DropdownButtonFormField<String>(
              initialValue: _method,
              items: _methods
                  .map((m) =>
                      DropdownMenuItem(value: m.$1, child: Text(m.$2)))
                  .toList(),
              onChanged: (v) {
                setState(() {
                  _method = v ?? 'pago_movil';
                  // Switch currency by default
                  if (_method == 'transfer_usd' ||
                      _method == 'zelle' ||
                      _method == 'binance_pay') {
                    _paidCurrency = 'USD';
                  } else if (_method == 'pago_movil' || _method == 'transfer_ves') {
                    _paidCurrency = 'VES';
                  }
                });
              },
            ),
            const SizedBox(height: 16),
            SegmentedButton<String>(
              segments: const [
                ButtonSegment(value: 'USD', label: Text('USD')),
                ButtonSegment(value: 'VES', label: Text('VES')),
              ],
              selected: {_paidCurrency},
              onSelectionChanged: (v) =>
                  setState(() => _paidCurrency = v.first),
            ),
            const SizedBox(height: 16),
            TextField(
              controller: _amountUsdCtrl,
              decoration: const InputDecoration(
                labelText: 'Monto en USD',
                prefixText: r'$ ',
              ),
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
            ),
            if (_paidCurrency == 'VES') ...[
              const SizedBox(height: 8),
              TextField(
                controller: _amountVesCtrl,
                decoration: const InputDecoration(
                  labelText: 'Monto pagado en VES',
                  prefixText: 'Bs ',
                ),
                keyboardType:
                    const TextInputType.numberWithOptions(decimal: true),
              ),
              const SizedBox(height: 8),
              TextField(
                controller: _exchangeRateCtrl,
                decoration: const InputDecoration(
                  labelText: 'Tasa Bs/USD',
                ),
                keyboardType:
                    const TextInputType.numberWithOptions(decimal: true),
              ),
            ],
            const SizedBox(height: 16),
            TextField(
              controller: _referenceCtrl,
              decoration: const InputDecoration(
                labelText: 'Número de referencia (opcional)',
              ),
            ),
            const SizedBox(height: 8),
            TextField(
              controller: _payerNameCtrl,
              decoration: const InputDecoration(
                labelText: 'Pagado por (opcional)',
              ),
            ),
            const SizedBox(height: 8),
            TextField(
              controller: _payerPhoneCtrl,
              decoration: const InputDecoration(
                labelText: 'Teléfono del pagador (opcional)',
              ),
              keyboardType: TextInputType.phone,
            ),
            const SizedBox(height: 16),
            Text('Comprobante',
                style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 4),
            Container(
              decoration: BoxDecoration(
                border: Border.all(color: palette.line, width: 2),
                borderRadius: BorderRadius.circular(12),
              ),
              padding: const EdgeInsets.all(12),
              child: Column(
                children: [
                  if (_previewBytes != null)
                    ClipRRect(
                      borderRadius: BorderRadius.circular(8),
                      child: Image.memory(
                        _previewBytes!,
                        height: 200,
                        fit: BoxFit.cover,
                      ),
                    )
                  else
                    Padding(
                      padding: const EdgeInsets.all(24),
                      child: Column(
                        children: [
                          Icon(Icons.upload_file,
                              size: 48, color: palette.inkSoft),
                          const SizedBox(height: 8),
                          const Text(
                              'Captura del Pago Móvil, Zelle, etc.',
                              textAlign: TextAlign.center),
                        ],
                      ),
                    ),
                  const SizedBox(height: 12),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      OutlinedButton.icon(
                        icon: const Icon(Icons.photo_library),
                        label: const Text('Galería'),
                        onPressed: () => _pickImage(ImageSource.gallery),
                      ),
                      const SizedBox(width: 12),
                      OutlinedButton.icon(
                        icon: const Icon(Icons.camera_alt),
                        label: const Text('Cámara'),
                        onPressed: () => _pickImage(ImageSource.camera),
                      ),
                    ],
                  ),
                ],
              ),
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
            const SizedBox(height: 16),
            ElevatedButton.icon(
              onPressed: _busy ? null : _submit,
              icon: _busy
                  ? const SizedBox(
                      width: 16,
                      height: 16,
                      child: CircularProgressIndicator(strokeWidth: 2))
                  : const Icon(Icons.check),
              label: Text(_busy ? 'Enviando…' : 'Enviar pago'),
            ),
          ],
        ),
      ),
    );
  }
}

