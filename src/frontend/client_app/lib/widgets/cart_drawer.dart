import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';

import '../app_state.dart';

class CartDrawer extends StatefulWidget {
  final AppState state;
  const CartDrawer({super.key, required this.state});

  @override
  State<CartDrawer> createState() => _CartDrawerState();
}

class _CartDrawerState extends State<CartDrawer> {
  String _deliveryType = 'pickup';
  final _nameCtrl = TextEditingController();
  final _phoneCtrl = TextEditingController();
  final _addressCtrl = TextEditingController();
  final _notesCtrl = TextEditingController();
  bool _submitting = false;
  String? _resultMessage;

  Future<void> _submit() async {
    final s = widget.state;
    setState(() {
      _submitting = true;
      _resultMessage = null;
    });
    try {
      // F-24: createGuestOrder ahora retorna {id, accessToken}.
      final created = await s.api.createGuestOrder(CreateGuestOrderRequest(
        guestFullName: _nameCtrl.text.trim(),
        guestPhone: _phoneCtrl.text.trim(),
        deliveryType: _deliveryType,
        deliveryAddress:
            _deliveryType == 'third_party' ? _addressCtrl.text.trim() : null,
        customerNotes:
            _notesCtrl.text.trim().isEmpty ? null : _notesCtrl.text.trim(),
        items: s.cart
            .map((l) => OrderLineInput(
                  dishId: l.dish.id,
                  quantity: l.quantity,
                  itemNotes: l.notes,
                  modifiers: l.modifiers, // Etapa 2
                ))
            .toList(),
      ));
      await s.recordPlacedOrder(created.id, created.accessToken, _nameCtrl.text.trim());
      s.clearCart();
      setState(() {
        _resultMessage = 'Pedido creado · sigue su estado en la pestaña Pedidos.';
      });
    } on ApiException catch (e) {
      setState(() => _resultMessage = 'Error ${e.statusCode}: ${e.message}');
    } catch (e) {
      setState(() => _resultMessage = 'Error: $e');
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final s = widget.state;
    final t = s.strings;

    return DraggableScrollableSheet(
      expand: false,
      initialChildSize: 0.85,
      maxChildSize: 0.95,
      builder: (_, scrollController) => Padding(
        padding: EdgeInsets.only(
          left: 16,
          right: 16,
          top: 16,
          bottom: MediaQuery.of(context).viewInsets.bottom + 16,
        ),
        child: ListView(
          controller: scrollController,
          children: [
            Center(
              child: Container(
                width: 40,
                height: 4,
                decoration: BoxDecoration(
                  color: Colors.grey,
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
            ),
            const SizedBox(height: 16),
            Text(t.t('cart.title'),
                style: Theme.of(context).textTheme.displaySmall),
            const SizedBox(height: 16),
            ...s.cart.map((line) {
                  final modDesc = line.modifiers.isEmpty
                      ? null
                      : line.modifiers
                            .map((m) => '${m.modifier.name} x${m.quantity}')
                            .join(', ');
                  return ListTile(
                    contentPadding: EdgeInsets.zero,
                    title: Text(line.dish.name),
                    subtitle: Text(
                      [
                        'x\${line.quantity}',
                        if (modDesc != null) modDesc,
                      ].join(' · '),
                    ),
                    trailing: Text('\\$\${line.lineTotal.toStringAsFixed(2)}'),
                  );
                }),
            const Divider(),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(t.t('cart.subtotal'),
                    style: Theme.of(context).textTheme.titleMedium),
                Text('\$${s.cartSubtotal.toStringAsFixed(2)}',
                    style: Theme.of(context).textTheme.titleMedium),
              ],
            ),
            const SizedBox(height: 24),
            SegmentedButton<String>(
              segments: [
                ButtonSegment(value: 'pickup', label: Text(t.t('cart.pickup'))),
                ButtonSegment(
                    value: 'third_party', label: Text(t.t('cart.delivery'))),
              ],
              selected: {_deliveryType},
              onSelectionChanged: (v) =>
                  setState(() => _deliveryType = v.first),
            ),
            const SizedBox(height: 16),
            TextField(
              controller: _nameCtrl,
              decoration: const InputDecoration(labelText: 'Nombre completo'),
            ),
            const SizedBox(height: 8),
            TextField(
              controller: _phoneCtrl,
              decoration: const InputDecoration(labelText: 'Teléfono'),
              keyboardType: TextInputType.phone,
            ),
            if (_deliveryType == 'third_party') ...[
              const SizedBox(height: 8),
              TextField(
                controller: _addressCtrl,
                decoration: const InputDecoration(labelText: 'Dirección'),
                maxLines: 2,
              ),
            ],
            const SizedBox(height: 8),
            TextField(
              controller: _notesCtrl,
              decoration: InputDecoration(labelText: t.t('cart.notes')),
              maxLines: 3,
            ),
            const SizedBox(height: 16),
            ElevatedButton(
              onPressed: _submitting || s.cart.isEmpty ? null : _submit,
              child: _submitting
                  ? const SizedBox(
                      width: 18,
                      height: 18,
                      child: CircularProgressIndicator(strokeWidth: 2))
                  : Text(t.t('cart.placeOrder')),
            ),
            if (_resultMessage != null) ...[
              const SizedBox(height: 12),
              Text(_resultMessage!,
                  textAlign: TextAlign.center,
                  style: Theme.of(context).textTheme.bodyMedium),
            ],
          ],
        ),
      ),
    );
  }
}
