import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';
import 'package:intl/intl.dart';

class OrderDetailDrawer extends StatefulWidget {
  final HcpApi api;
  final String orderId;
  final ScrollController scrollController;
  final Future<void> Function() onChanged;

  const OrderDetailDrawer({
    super.key,
    required this.api,
    required this.orderId,
    required this.scrollController,
    required this.onChanged,
  });

  @override
  State<OrderDetailDrawer> createState() => _OrderDetailDrawerState();
}

class _OrderDetailDrawerState extends State<OrderDetailDrawer> {
  Order? _order;
  PendingPayment? _pendingPayment;
  bool _busy = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final order = await widget.api.adminGetOrder(widget.orderId);
      PendingPayment? payment;
      if (order.status == 'payment_verifying') {
        final list = await widget.api.adminPendingPayments();
        payment = list.where((p) => p.orderId == order.id).firstOrNull;
      }
      if (mounted) {
        setState(() {
          _order = order;
          _pendingPayment = payment;
          _busy = false;
        });
      }
    } on ApiException catch (e) {
      if (mounted) {
        setState(() {
          _error = e.message;
          _busy = false;
        });
      }
    }
  }

  Future<void> _advance(String target, {String? reason}) async {
    setState(() => _busy = true);
    try {
      await widget.api.adminAdvanceOrder(widget.orderId, target, reason: reason);
      await widget.onChanged();
      await _load();
    } on ApiException catch (e) {
      _toast(e.message);
      setState(() => _busy = false);
    }
  }

  Future<void> _verifyPayment() async {
    if (_pendingPayment == null) return;
    setState(() => _busy = true);
    try {
      await widget.api.adminVerifyPayment(_pendingPayment!.id);
      await widget.onChanged();
      await _load();
    } on ApiException catch (e) {
      _toast(e.message);
      setState(() => _busy = false);
    }
  }

  Future<void> _rejectPayment() async {
    if (_pendingPayment == null) return;
    final reason = await _askReason('Rechazar pago — motivo');
    if (reason == null) return;
    setState(() => _busy = true);
    try {
      await widget.api.adminRejectPayment(_pendingPayment!.id, reason);
      await widget.onChanged();
      await _load();
    } on ApiException catch (e) {
      _toast(e.message);
      setState(() => _busy = false);
    }
  }

  Future<String?> _askReason(String title) async {
    final ctrl = TextEditingController();
    return showDialog<String>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(title),
        content: TextField(
          controller: ctrl,
          maxLines: 3,
          decoration: const InputDecoration(hintText: 'Motivo'),
          autofocus: true,
        ),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx),
              child: const Text('Cancelar')),
          ElevatedButton(
            onPressed: () => Navigator.pop(
                ctx, ctrl.text.trim().isEmpty ? null : ctrl.text.trim()),
            child: const Text('Confirmar'),
          ),
        ],
      ),
    );
  }

  void _toast(String message) =>
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    if (_busy) {
      return const Padding(
        padding: EdgeInsets.all(48),
        child: Center(child: CircularProgressIndicator()),
      );
    }
    if (_error != null) {
      return Padding(
        padding: const EdgeInsets.all(32),
        child: Column(mainAxisSize: MainAxisSize.min, children: [
          Text(_error!),
          ElevatedButton(onPressed: _load, child: const Text('Reintentar')),
        ]),
      );
    }
    final order = _order!;
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 24),
      child: ListView(
        controller: widget.scrollController,
        children: [
          const SizedBox(height: 12),
          Center(
            child: Container(
              width: 40, height: 4,
              decoration: BoxDecoration(
                color: palette.line, borderRadius: BorderRadius.circular(2),
              ),
            ),
          ),
          const SizedBox(height: 12),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(order.orderNumber,
                  style: Theme.of(context).textTheme.displaySmall),
              _StatusChip(status: order.status, palette: palette),
            ],
          ),
          Text(
            DateFormat('dd MMM yyyy · HH:mm', 'es').format(order.createdAt.toLocal()),
            style: Theme.of(context).textTheme.bodySmall,
          ),
          if (order.deliveryAddress != null) ...[
            const SizedBox(height: 8),
            Row(children: [
              const Icon(Icons.location_on_outlined, size: 16),
              const SizedBox(width: 4),
              Expanded(child: Text(order.deliveryAddress!)),
            ]),
          ],
          const Divider(height: 32),
          Text('Ítems', style: Theme.of(context).textTheme.titleLarge),
          const SizedBox(height: 8),
          ...order.items.map((i) => Padding(
                padding: const EdgeInsets.only(bottom: 6),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    SizedBox(
                      width: 36,
                      child: Text('${i.quantity}×',
                          style: Theme.of(context).textTheme.titleMedium),
                    ),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(i.dishNameSnapshot),
                          if (i.itemNotes != null && i.itemNotes!.isNotEmpty)
                            Text('Nota: ${i.itemNotes!}',
                                style: Theme.of(context).textTheme.bodySmall),
                        ],
                      ),
                    ),
                    Text('\$${i.lineTotalUsd.toStringAsFixed(2)}',
                        style: Theme.of(context).textTheme.labelMedium),
                  ],
                ),
              )),
          const Divider(height: 32),
          _totalsRow('Subtotal', order.subtotalUsd),
          _totalsRow('Total USD', order.totalUsd, bold: true),
          if (order.totalVesAtOrderTime != null)
            _totalsRow('Total VES', order.totalVesAtOrderTime!,
                prefix: 'Bs ', mono: true),
          const SizedBox(height: 24),
          if (_pendingPayment != null) ...[
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: palette.sun.withValues(alpha: 0.25),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Comprobante de pago',
                      style: Theme.of(context).textTheme.titleMedium),
                  const SizedBox(height: 8),
                  Text('Método: ${_humanMethod(_pendingPayment!.method)}'),
                  Text(
                      'Monto: ${_pendingPayment!.paidCurrency == "VES" ? "Bs " : "\$"}'
                      '${_pendingPayment!.amountPaidCurrency.toStringAsFixed(2)}'),
                  if (_pendingPayment!.referenceNumber != null)
                    Text('Ref: ${_pendingPayment!.referenceNumber!}'),
                  if (_pendingPayment!.payerName != null)
                    Text('Pagado por: ${_pendingPayment!.payerName!}'),
                  if (_pendingPayment!.proofImageUrl != null) ...[
                    const SizedBox(height: 8),
                    SelectableText(_pendingPayment!.proofImageUrl!,
                        style: const TextStyle(fontSize: 12)),
                  ],
                  const SizedBox(height: 12),
                  Row(
                    children: [
                      Expanded(
                        child: ElevatedButton.icon(
                          icon: const Icon(Icons.check),
                          onPressed: _verifyPayment,
                          label: const Text('Verificar'),
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: OutlinedButton.icon(
                          icon: Icon(Icons.close, color: palette.red),
                          onPressed: _rejectPayment,
                          label: Text('Rechazar',
                              style: TextStyle(color: palette.red)),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
            const SizedBox(height: 16),
          ],
          _AdvanceActions(order: order, onAdvance: _advance, onCancel: () async {
            final reason = await _askReason('Cancelar pedido — motivo');
            if (reason != null) await _advance('cancelled', reason: reason);
          }),
          const SizedBox(height: 32),
        ],
      ),
    );
  }

  Widget _totalsRow(String label, double value,
      {bool bold = false, String prefix = '\$', bool mono = false}) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label,
              style: bold
                  ? Theme.of(context).textTheme.titleMedium
                  : Theme.of(context).textTheme.bodyMedium),
          Text('$prefix${value.toStringAsFixed(2)}',
              style: bold
                  ? Theme.of(context).textTheme.titleMedium
                  : (mono
                      ? Theme.of(context).textTheme.labelMedium
                      : Theme.of(context).textTheme.bodyMedium)),
        ],
      ),
    );
  }

  static String _humanMethod(String m) => switch (m) {
        'pago_movil'   => 'Pago Móvil',
        'transfer_ves' => 'Transferencia VES',
        'transfer_usd' => 'Transferencia USD',
        'zelle'        => 'Zelle',
        'binance_pay'  => 'Binance Pay',
        'cash'         => 'Efectivo',
        _              => m,
      };
}

class _StatusChip extends StatelessWidget {
  final String status;
  final HcpPalette palette;
  const _StatusChip({required this.status, required this.palette});

  @override
  Widget build(BuildContext context) {
    final (label, bg, fg) = switch (status) {
      'pending_payment'    => ('Pago pendiente', palette.sun, palette.ink),
      'payment_verifying'  => ('Verificando', palette.sun, palette.ink),
      'paid'               => ('Pagado', palette.greenSoft, palette.green),
      'in_preparation'     => ('Cocinando', palette.accent, Colors.white),
      'ready'              => ('Listo', palette.green, Colors.white),
      'in_delivery'        => ('En camino', palette.accentDark, Colors.white),
      'delivered'          => ('Entregado', palette.greenSoft, palette.green),
      'cancelled'          => ('Cancelado', palette.redSoft, palette.red),
      'rejected'           => ('Rechazado', palette.redSoft, palette.red),
      _                    => (status, palette.line, palette.ink),
    };
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      decoration:
          BoxDecoration(color: bg, borderRadius: BorderRadius.circular(999)),
      child: Text(label,
          style: TextStyle(color: fg, fontSize: 12, fontWeight: FontWeight.w600)),
    );
  }
}

class _AdvanceActions extends StatelessWidget {
  final Order order;
  final Future<void> Function(String target, {String? reason}) onAdvance;
  final Future<void> Function() onCancel;
  const _AdvanceActions({required this.order, required this.onAdvance, required this.onCancel});

  @override
  Widget build(BuildContext context) {
    final actions = <_Action>[];
    switch (order.status) {
      case 'paid':
        actions.add(_Action('Empezar a cocinar', () => onAdvance('in_preparation')));
        break;
      case 'in_preparation':
        actions.add(_Action('Marcar como lista', () => onAdvance('ready')));
        break;
      case 'ready':
        if (order.deliveryType == 'third_party') {
          actions.add(_Action('Despachar a delivery', () => onAdvance('in_delivery')));
        } else {
          actions.add(_Action('Cliente recogió', () => onAdvance('delivered')));
        }
        break;
      case 'in_delivery':
        actions.add(_Action('Entregada', () => onAdvance('delivered')));
        break;
    }
    final canCancel = !order.isTerminal;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        for (final a in actions) ...[
          ElevatedButton(onPressed: a.onTap, child: Text(a.label)),
          const SizedBox(height: 8),
        ],
        if (canCancel)
          OutlinedButton(
            onPressed: onCancel,
            child: const Text('Cancelar pedido'),
          ),
      ],
    );
  }
}

class _Action {
  final String label;
  final VoidCallback onTap;
  _Action(this.label, this.onTap);
}
