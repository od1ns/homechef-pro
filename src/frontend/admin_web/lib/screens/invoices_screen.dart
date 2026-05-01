import 'package:flutter/material.dart';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:homechef_shared/homechef_shared.dart';
import 'package:intl/intl.dart';

import '../utils/download_helper.dart';

/// A7 Facturas — placeholder funcional hasta integrar SENIAT/IGTF.
/// Lista los pedidos terminales (delivered/cancelled) con descarga del recibo PDF.
/// La integración con facturación electrónica venezolana queda como deuda.
class InvoicesScreen extends StatefulWidget {
  final HcpApi api;
  const InvoicesScreen({super.key, required this.api});

  @override
  State<InvoicesScreen> createState() => _InvoicesScreenState();
}

class _InvoicesScreenState extends State<InvoicesScreen> {
  bool _busy = true;
  String? _error;
  List<SalesDailyRow> _sales = const [];
  List<OrderSummary> _allDelivered = const [];
  int _monthsBack = 0;       // 0 = mes actual

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
      // The backend exposes summary endpoints; this is a thin placeholder that
      // pulls every active and delivered order to keep the demo honest.
      final futures = await Future.wait([
        widget.api.adminSalesDaily(days: 90),
        widget.api.adminActiveOrders(statusFilter: 'delivered'),
      ]);
      if (!mounted) return;
      setState(() {
        _sales = futures[0] as List<SalesDailyRow>;
        _allDelivered = futures[1] as List<OrderSummary>;
        _busy = false;
      });
    } on ApiException catch (e) {
      if (mounted) setState(() {
        _error = e.message;
        _busy = false;
      });
    }
  }

  Future<void> _downloadReceipt(OrderSummary o) async {
    try {
      final bytes = await widget.api.orderReceiptPdf(o.id);
      if (!mounted) return;
      final filename = 'recibo-${o.orderNumber}.pdf';
      if (kIsWeb) {
        downloadBytes(bytes, filename, 'application/pdf');
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(
            content: Text('Recibo $filename descargado (${bytes.length ~/ 1024} KB)')));
      } else {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(
            content: Text(
                'Recibo $filename listo (${bytes.length ~/ 1024} KB) · '
                'descarga manual no implementada en desktop/mobile')));
      }
    } on ApiException catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.message)));
    }
  }

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    if (_busy) return const Center(child: CircularProgressIndicator());
    if (_error != null) return Center(child: Text(_error!));

    final now = DateUtils.dateOnly(DateTime.now());
    final firstOfMonth = DateTime(now.year, now.month - _monthsBack, 1);
    final lastOfMonth = DateTime(now.year, now.month - _monthsBack + 1, 1)
        .subtract(const Duration(days: 1));

    final monthSales = _sales.where((s) =>
        !s.saleDate.isBefore(firstOfMonth) && !s.saleDate.isAfter(lastOfMonth)).toList();
    final monthRevenue = monthSales.fold(0.0, (sum, r) => sum + r.revenueUsd);
    final monthOrders = monthSales.fold(0, (sum, r) => sum + r.ordersCount);

    return SingleChildScrollView(
      padding: const EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Facturas',
              style: Theme.of(context).textTheme.displaySmall),
          const SizedBox(height: 4),
          Text(
              'Pedidos entregados con recibo PDF descargable. La integración con facturación '
              'electrónica del SENIAT/IGTF está pendiente — esta vista es operativa, no fiscal.',
              style: Theme.of(context).textTheme.bodyMedium),
          const SizedBox(height: 16),
          Row(children: [
            IconButton.filled(
              icon: const Icon(Icons.chevron_left),
              onPressed: () => setState(() => _monthsBack += 1),
            ),
            const SizedBox(width: 12),
            Text(DateFormat('MMMM yyyy', 'es').format(firstOfMonth),
                style: Theme.of(context).textTheme.titleLarge),
            const SizedBox(width: 12),
            IconButton.filled(
              icon: const Icon(Icons.chevron_right),
              onPressed: _monthsBack > 0 ? () => setState(() => _monthsBack -= 1) : null,
            ),
          ]),
          const SizedBox(height: 16),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Wrap(
                spacing: 32,
                children: [
                  _kpi('Días con ventas', '${monthSales.length}'),
                  _kpi('Órdenes del mes', '$monthOrders'),
                  _kpi('Ingresos del mes', '\$${monthRevenue.toStringAsFixed(2)}'),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),
          Card(
            clipBehavior: Clip.antiAlias,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Padding(
                  padding: const EdgeInsets.all(16),
                  child: Text('Recibos disponibles',
                      style: Theme.of(context).textTheme.titleLarge),
                ),
                if (_allDelivered.isEmpty)
                  Padding(
                    padding: const EdgeInsets.all(16),
                    child: Text('Aún no hay pedidos entregados.',
                        style: TextStyle(color: palette.inkMuted)),
                  )
                else
                  ListView.separated(
                    shrinkWrap: true,
                    physics: const NeverScrollableScrollPhysics(),
                    itemCount: _allDelivered.length,
                    separatorBuilder: (_, __) => const Divider(height: 1),
                    itemBuilder: (_, i) {
                      final o = _allDelivered[i];
                      return ListTile(
                        title: Text(o.orderNumber),
                        subtitle: Text(
                            '${o.customerName} · ${DateFormat('dd MMM yyyy HH:mm', 'es').format(o.createdAt.toLocal())} · '
                            '\$${o.totalUsd.toStringAsFixed(2)}'),
                        trailing: TextButton.icon(
                          icon: const Icon(Icons.download),
                          label: const Text('Recibo'),
                          onPressed: () => _downloadReceipt(o),
                        ),
                      );
                    },
                  ),
              ],
            ),
          ),
          const SizedBox(height: 16),
          Card(
            color: palette.sun.withValues(alpha: 0.18),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                children: [
                  Icon(Icons.info_outline, color: palette.accent),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Text(
                      'Pendiente: integración con SENIAT/IGTF para emitir factura fiscal '
                      'electrónica al verificar el pago. Mientras tanto, el recibo PDF de '
                      'cortesía cumple para entrega al cliente.',
                      style: Theme.of(context).textTheme.bodyMedium,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _kpi(String label, String value) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: Theme.of(context).textTheme.bodySmall),
        Text(value, style: Theme.of(context).textTheme.headlineMedium),
      ],
    );
  }
}
