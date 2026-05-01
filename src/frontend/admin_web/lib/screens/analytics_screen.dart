import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';
import 'package:intl/intl.dart';

/// A6 Analytics — sales over time + top dishes by margin + recipe cost spread.
class AnalyticsScreen extends StatefulWidget {
  final HcpApi api;
  const AnalyticsScreen({super.key, required this.api});

  @override
  State<AnalyticsScreen> createState() => _AnalyticsScreenState();
}

class _AnalyticsScreenState extends State<AnalyticsScreen> {
  int _days = 30;
  bool _busy = true;
  String? _error;
  List<SalesDailyRow> _sales = const [];
  List<DishMarginRow> _margins = const [];

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
        widget.api.adminSalesDaily(days: _days),
        widget.api.adminDishMargin(),
      ]);
      if (!mounted) return;
      setState(() {
        _sales = results[0] as List<SalesDailyRow>;
        _margins = results[1] as List<DishMarginRow>;
        _busy = false;
      });
    } on ApiException catch (e) {
      if (mounted) setState(() {
        _error = e.message;
        _busy = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    if (_busy) return const Center(child: CircularProgressIndicator());
    if (_error != null) {
      return Center(child: Text(_error!));
    }

    final asc = (_sales.toList()..sort((a, b) => a.saleDate.compareTo(b.saleDate)));
    final maxRevenue = asc.fold<double>(0, (m, r) => r.revenueUsd > m ? r.revenueUsd : m);
    final totalRevenue = asc.fold(0.0, (s, r) => s + r.revenueUsd);
    final totalProfit = asc.fold(0.0, (s, r) => s + r.grossProfitUsd);
    final totalOrders = asc.fold(0, (s, r) => s + r.ordersCount);

    final marginsSorted = (_margins.toList()
          ..sort((a, b) => b.grossMarginPct.compareTo(a.grossMarginPct)));

    return SingleChildScrollView(
      padding: const EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('Analítica',
                  style: Theme.of(context).textTheme.displaySmall),
              SegmentedButton<int>(
                segments: const [
                  ButtonSegment(value: 7, label: Text('7d')),
                  ButtonSegment(value: 30, label: Text('30d')),
                  ButtonSegment(value: 90, label: Text('90d')),
                ],
                selected: {_days},
                onSelectionChanged: (v) {
                  setState(() => _days = v.first);
                  _load();
                },
              ),
            ],
          ),
          const SizedBox(height: 16),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Wrap(
                    spacing: 32,
                    children: [
                      _kpi('Ingresos', '\$${totalRevenue.toStringAsFixed(2)}'),
                      _kpi('Ganancia bruta', '\$${totalProfit.toStringAsFixed(2)}'),
                      _kpi('Órdenes', '$totalOrders'),
                      _kpi(
                          'Margen',
                          totalRevenue > 0
                              ? '${(totalProfit / totalRevenue * 100).toStringAsFixed(1)}%'
                              : '—'),
                    ],
                  ),
                  const SizedBox(height: 16),
                  SizedBox(
                    height: 160,
                    child: asc.isEmpty
                        ? Center(child: Text('Sin datos', style: TextStyle(color: palette.inkMuted)))
                        : LayoutBuilder(builder: (context, c) {
                            final maxBars = (c.maxWidth / 12).floor();
                            final bars = asc.length > maxBars
                                ? asc.sublist(asc.length - maxBars)
                                : asc;
                            return Row(
                              crossAxisAlignment: CrossAxisAlignment.end,
                              children: bars.map((d) {
                                final h = maxRevenue == 0
                                    ? 0.0
                                    : (d.revenueUsd / maxRevenue) * 140;
                                return Expanded(
                                  child: Tooltip(
                                    message:
                                        '${DateFormat('dd MMM', 'es').format(d.saleDate.toLocal())}\n'
                                        '\$${d.revenueUsd.toStringAsFixed(2)} · ${d.ordersCount} órdenes',
                                    child: Padding(
                                      padding: const EdgeInsets.symmetric(horizontal: 2),
                                      child: Container(
                                        height: h,
                                        decoration: BoxDecoration(
                                          color: palette.accent,
                                          borderRadius: const BorderRadius.vertical(
                                              top: Radius.circular(4)),
                                        ),
                                      ),
                                    ),
                                  ),
                                );
                              }).toList(),
                            );
                          }),
                  ),
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
                  child: Text('Margen por plato (${marginsSorted.length})',
                      style: Theme.of(context).textTheme.titleLarge),
                ),
                if (marginsSorted.isEmpty)
                  const Padding(
                    padding: EdgeInsets.all(16),
                    child: Text('Aún no hay platos publicados.'),
                  )
                else
                  SingleChildScrollView(
                    scrollDirection: Axis.horizontal,
                    child: DataTable(
                      columns: const [
                        DataColumn(label: Text('Plato')),
                        DataColumn(label: Text('Precio'), numeric: true),
                        DataColumn(label: Text('Costo'), numeric: true),
                        DataColumn(label: Text('Ganancia'), numeric: true),
                        DataColumn(label: Text('Margen'), numeric: true),
                        DataColumn(label: Text('Ratio'), numeric: true),
                      ],
                      rows: marginsSorted.map((d) {
                        final color = d.grossMarginPct >= 50
                            ? palette.green
                            : d.grossMarginPct >= 30
                                ? palette.sun
                                : palette.red;
                        return DataRow(cells: [
                          DataCell(Text(d.name)),
                          DataCell(Text('\$${(d.sellingPriceUsd ?? 0).toStringAsFixed(2)}')),
                          DataCell(Text('\$${d.totalCostUsd.toStringAsFixed(2)}')),
                          DataCell(Text('\$${d.grossProfitUsd.toStringAsFixed(2)}')),
                          DataCell(Text(
                              '${d.grossMarginPct.toStringAsFixed(1)}%',
                              style: TextStyle(color: color, fontWeight: FontWeight.w600))),
                          DataCell(Text(d.priceToCostRatio != null
                              ? '${d.priceToCostRatio!.toStringAsFixed(1)}×'
                              : '—')),
                        ]);
                      }).toList(),
                    ),
                  ),
              ],
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
