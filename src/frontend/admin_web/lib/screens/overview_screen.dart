import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';
import 'package:intl/intl.dart';

/// A1 Overview — KPI strip + 7-day sparkline + top dishes + inventory alerts.
/// Aggregates data from /api/admin/reports/* and /api/admin/orders.
class OverviewScreen extends StatefulWidget {
  final HcpApi api;
  const OverviewScreen({super.key, required this.api});

  @override
  State<OverviewScreen> createState() => _OverviewScreenState();
}

class _OverviewScreenState extends State<OverviewScreen> {
  bool _busy = true;
  String? _error;
  List<SalesDailyRow> _sales = const [];
  List<DishMarginRow> _margins = const [];
  List<ReorderSuggestionRow> _alerts = const [];
  List<OrderSummary> _liveOrders = const [];

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
      final results = await Future.wait([
        widget.api.adminSalesDaily(days: 30),
        widget.api.adminDishMargin(),
        widget.api.adminReorderSuggestions(),
        widget.api.adminActiveOrders(),
      ]);
      if (!mounted) return;
      setState(() {
        _sales = results[0] as List<SalesDailyRow>;
        _margins = results[1] as List<DishMarginRow>;
        _alerts = results[2] as List<ReorderSuggestionRow>;
        _liveOrders = results[3] as List<OrderSummary>;
        _busy = false;
      });
    } on ApiException catch (e) {
      if (mounted) {
        setState(() {
          _error = e.message;
          _busy = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_busy) return const Center(child: CircularProgressIndicator());
    if (_error != null) {
      return Center(
        child: Column(mainAxisSize: MainAxisSize.min, children: [
          Text(_error!),
          const SizedBox(height: 12),
          ElevatedButton(onPressed: _load, child: const Text('Reintentar')),
        ]),
      );
    }

    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    final today = DateUtils.dateOnly(DateTime.now());
    final todayRow = _sales.where((s) => DateUtils.isSameDay(s.saleDate, today)).firstOrNull;

    final last7 = (_sales.toList()..sort((a, b) => a.saleDate.compareTo(b.saleDate)));
    final week = last7.length > 7 ? last7.sublist(last7.length - 7) : last7;

    final weekRevenue = week.fold(0.0, (s, r) => s + r.revenueUsd);
    final weekProfit = week.fold(0.0, (s, r) => s + r.grossProfitUsd);
    final weekOrders = week.fold(0, (s, r) => s + r.ordersCount);
    final weekMargin =
        weekRevenue > 0 ? (weekProfit / weekRevenue) * 100 : 0.0;

    final criticalAlerts = _alerts
        .where((a) => a.priority == 'critical' || a.priority == 'urgent')
        .toList();

    final topMargin = (_margins.toList()
          ..sort((a, b) => b.grossMarginPct.compareTo(a.grossMarginPct)))
        .take(5)
        .toList();

    final width = MediaQuery.of(context).size.width;
    final useTwoCols = width >= 1100;

    return SingleChildScrollView(
      padding: const EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Resumen', style: Theme.of(context).textTheme.displaySmall),
          Text('Pulso del negocio · ${DateFormat('EEEE dd MMM', 'es').format(DateTime.now())}',
              style: Theme.of(context).textTheme.bodyMedium),
          const SizedBox(height: 20),
          Wrap(
            spacing: 16,
            runSpacing: 16,
            children: [
              _Kpi(
                label: 'Hoy · Ingresos',
                value: '\$${(todayRow?.revenueUsd ?? 0).toStringAsFixed(2)}',
                hint: '${todayRow?.ordersCount ?? 0} órdenes',
                accent: palette.accent,
              ),
              _Kpi(
                label: 'Hoy · Ganancia',
                value: '\$${(todayRow?.grossProfitUsd ?? 0).toStringAsFixed(2)}',
                hint: todayRow != null && todayRow.revenueUsd > 0
                    ? '${(todayRow.grossProfitUsd / todayRow.revenueUsd * 100).toStringAsFixed(1)}% margen'
                    : '—',
                accent: palette.green,
              ),
              _Kpi(
                label: 'Activas',
                value: '${_liveOrders.length}',
                hint: 'pedidos en pipeline',
                accent: palette.sun,
              ),
              _Kpi(
                label: 'Alertas inv.',
                value: '${criticalAlerts.length}',
                hint: criticalAlerts.isEmpty ? 'todo OK' : 'reabastecer pronto',
                accent: criticalAlerts.isEmpty ? palette.green : palette.red,
              ),
            ],
          ),
          const SizedBox(height: 24),
          if (useTwoCols)
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(child: _salesCard(week, weekRevenue, weekProfit, weekOrders, weekMargin, palette)),
                const SizedBox(width: 16),
                Expanded(child: _liveOrdersCard(palette)),
              ],
            )
          else ...[
            _salesCard(week, weekRevenue, weekProfit, weekOrders, weekMargin, palette),
            const SizedBox(height: 16),
            _liveOrdersCard(palette),
          ],
          const SizedBox(height: 16),
          if (useTwoCols)
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(child: _topDishesCard(topMargin, palette)),
                const SizedBox(width: 16),
                Expanded(child: _alertsCard(criticalAlerts, palette)),
              ],
            )
          else ...[
            _topDishesCard(topMargin, palette),
            const SizedBox(height: 16),
            _alertsCard(criticalAlerts, palette),
          ],
        ],
      ),
    );
  }

  Widget _salesCard(List<SalesDailyRow> week, double revenue, double profit,
      int orders, double margin, HcpPalette palette) {
    final maxRevenue =
        week.fold<double>(0, (m, r) => r.revenueUsd > m ? r.revenueUsd : m);
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text('Últimos 7 días',
                    style: Theme.of(context).textTheme.titleLarge),
                Text(
                    '${DateFormat('dd MMM', 'es').format(DateTime.now().subtract(const Duration(days: 7)))} → hoy'),
              ],
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                _miniMetric('Ingresos', '\$${revenue.toStringAsFixed(2)}'),
                const SizedBox(width: 24),
                _miniMetric('Ganancia', '\$${profit.toStringAsFixed(2)}'),
                const SizedBox(width: 24),
                _miniMetric('Órdenes', '$orders'),
                const SizedBox(width: 24),
                _miniMetric('Margen', '${margin.toStringAsFixed(1)}%'),
              ],
            ),
            const SizedBox(height: 16),
            SizedBox(
              height: 80,
              child: week.isEmpty
                  ? Center(child: Text('Sin datos aún', style: TextStyle(color: palette.inkMuted)))
                  : Row(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: week.map((d) {
                        final h = maxRevenue == 0 ? 0.0 : (d.revenueUsd / maxRevenue) * 64;
                        return Expanded(
                          child: Padding(
                            padding: const EdgeInsets.symmetric(horizontal: 4),
                            child: Column(
                              mainAxisAlignment: MainAxisAlignment.end,
                              children: [
                                Container(
                                  height: h,
                                  decoration: BoxDecoration(
                                    color: palette.accent,
                                    borderRadius: BorderRadius.circular(4),
                                  ),
                                ),
                                const SizedBox(height: 4),
                                Text(
                                    DateFormat('E', 'es')
                                        .format(d.saleDate.toLocal())
                                        .substring(0, 1)
                                        .toUpperCase(),
                                    style: Theme.of(context).textTheme.bodySmall),
                              ],
                            ),
                          ),
                        );
                      }).toList(),
                    ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _liveOrdersCard(HcpPalette palette) {
    final recent = (_liveOrders.toList()..sort((a, b) => b.createdAt.compareTo(a.createdAt)))
        .take(5)
        .toList();
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Pedidos activos (${_liveOrders.length})',
                style: Theme.of(context).textTheme.titleLarge),
            const SizedBox(height: 12),
            if (recent.isEmpty)
              Text('Sin pedidos activos.',
                  style: TextStyle(color: palette.inkMuted))
            else
              ...recent.map((o) => Padding(
                    padding: const EdgeInsets.symmetric(vertical: 4),
                    child: Row(
                      children: [
                        Container(
                          width: 8,
                          height: 8,
                          margin: const EdgeInsets.only(right: 12),
                          decoration: BoxDecoration(
                            color: _statusColor(o.status, palette),
                            shape: BoxShape.circle,
                          ),
                        ),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(o.orderNumber,
                                  style: Theme.of(context).textTheme.titleMedium),
                              Text('${o.customerName} · ${o.itemCount} ítems',
                                  style: Theme.of(context).textTheme.bodySmall),
                            ],
                          ),
                        ),
                        Text('\$${o.totalUsd.toStringAsFixed(2)}',
                            style: Theme.of(context).textTheme.labelMedium),
                      ],
                    ),
                  )),
          ],
        ),
      ),
    );
  }

  Widget _topDishesCard(List<DishMarginRow> dishes, HcpPalette palette) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Platos con mejor margen',
                style: Theme.of(context).textTheme.titleLarge),
            const SizedBox(height: 12),
            if (dishes.isEmpty)
              Text('Sin platos publicados.',
                  style: TextStyle(color: palette.inkMuted))
            else
              ...dishes.map((d) {
                final color = d.grossMarginPct >= 50
                    ? palette.green
                    : d.grossMarginPct >= 30
                        ? palette.sun
                        : palette.red;
                return Padding(
                  padding: const EdgeInsets.symmetric(vertical: 4),
                  child: Row(
                    children: [
                      Expanded(child: Text(d.name, maxLines: 1, overflow: TextOverflow.ellipsis)),
                      Text(
                          '\$${(d.sellingPriceUsd ?? 0).toStringAsFixed(2)} · '
                          '${d.grossMarginPct.toStringAsFixed(1)}%',
                          style: TextStyle(color: color, fontWeight: FontWeight.w600)),
                    ],
                  ),
                );
              }),
          ],
        ),
      ),
    );
  }

  Widget _alertsCard(List<ReorderSuggestionRow> alerts, HcpPalette palette) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Alertas de inventario',
                style: Theme.of(context).textTheme.titleLarge),
            const SizedBox(height: 12),
            if (alerts.isEmpty)
              Row(children: [
                Icon(Icons.check_circle, color: palette.green),
                const SizedBox(width: 8),
                const Text('Sin urgencias por ahora'),
              ])
            else
              ...alerts.take(8).map((a) {
                final color = a.priority == 'critical' ? palette.red : palette.sun;
                return Padding(
                  padding: const EdgeInsets.symmetric(vertical: 4),
                  child: Row(
                    children: [
                      Container(
                        width: 8,
                        height: 8,
                        margin: const EdgeInsets.only(right: 8),
                        decoration: BoxDecoration(color: color, shape: BoxShape.circle),
                      ),
                      Expanded(
                        child: Text(a.name,
                            maxLines: 1, overflow: TextOverflow.ellipsis),
                      ),
                      Text(
                          a.estimatedDaysUntilStockout != null
                              ? '${a.estimatedDaysUntilStockout!.toStringAsFixed(0)}d'
                              : a.priority,
                          style: TextStyle(color: color, fontWeight: FontWeight.w600)),
                    ],
                  ),
                );
              }),
          ],
        ),
      ),
    );
  }

  Widget _miniMetric(String label, String value) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: Theme.of(context).textTheme.bodySmall),
        Text(value, style: Theme.of(context).textTheme.titleLarge),
      ],
    );
  }

  Color _statusColor(String status, HcpPalette palette) {
    return switch (status) {
      'pending_payment' || 'payment_verifying' => palette.sun,
      'paid' || 'in_preparation' => palette.accent,
      'ready' => palette.green,
      'in_delivery' => palette.accentDark,
      _ => palette.line,
    };
  }
}

class _Kpi extends StatelessWidget {
  final String label;
  final String value;
  final String hint;
  final Color accent;
  const _Kpi({
    required this.label,
    required this.value,
    required this.hint,
    required this.accent,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 240,
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Theme.of(context).cardTheme.color,
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 32,
            height: 4,
            decoration: BoxDecoration(
              color: accent,
              borderRadius: BorderRadius.circular(2),
            ),
          ),
          const SizedBox(height: 12),
          Text(label, style: Theme.of(context).textTheme.bodySmall),
          const SizedBox(height: 4),
          Text(value, style: Theme.of(context).textTheme.displaySmall),
          const SizedBox(height: 4),
          Text(hint, style: Theme.of(context).textTheme.bodySmall),
        ],
      ),
    );
  }
}
