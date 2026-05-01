import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';
import 'package:intl/intl.dart';

/// S1 Scale-to-Demand: forecast de compras basado en histórico de ventas.
/// Sliders para ajustar ventana histórica, ventana objetivo, factor de crecimiento.
class PurchasingScreen extends StatefulWidget {
  final HcpApi api;
  const PurchasingScreen({super.key, required this.api});

  @override
  State<PurchasingScreen> createState() => _PurchasingScreenState();
}

class _PurchasingScreenState extends State<PurchasingScreen> {
  int _historicalDays = 28;
  int _targetDays = 7;
  double _growth = 1.0;
  PurchaseForecast? _forecast;
  bool _busy = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _refresh();
  }

  Future<void> _refresh() async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final f = await widget.api.adminPurchaseForecast(
        historicalDays: _historicalDays,
        targetDays: _targetDays,
        growthFactor: _growth,
      );
      if (mounted) setState(() {
        _forecast = f;
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

    return Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Compras · Scale to Demand',
              style: Theme.of(context).textTheme.displaySmall),
          const SizedBox(height: 4),
          Text(
            'Predicción de qué comprar para el próximo período según ventas históricas.',
            style: Theme.of(context).textTheme.bodyMedium,
          ),
          const SizedBox(height: 16),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Wrap(
                spacing: 24,
                runSpacing: 16,
                crossAxisAlignment: WrapCrossAlignment.center,
                children: [
                  _Slider(
                    label: 'Histórico',
                    value: _historicalDays.toDouble(),
                    min: 7,
                    max: 90,
                    divisions: 83,
                    suffix: 'días',
                    onChanged: (v) => setState(() => _historicalDays = v.round()),
                    onChangeEnd: (_) => _refresh(),
                  ),
                  _Slider(
                    label: 'Objetivo',
                    value: _targetDays.toDouble(),
                    min: 1,
                    max: 30,
                    divisions: 29,
                    suffix: 'días',
                    onChanged: (v) => setState(() => _targetDays = v.round()),
                    onChangeEnd: (_) => _refresh(),
                  ),
                  _Slider(
                    label: 'Crecimiento',
                    value: _growth,
                    min: 0.5,
                    max: 3.0,
                    divisions: 25,
                    suffix: '×',
                    decimals: 1,
                    onChanged: (v) => setState(() => _growth =
                        double.parse(v.toStringAsFixed(1))),
                    onChangeEnd: (_) => _refresh(),
                  ),
                  IconButton.filled(
                    icon: const Icon(Icons.refresh),
                    onPressed: _refresh,
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),
          if (_busy)
            const Expanded(child: Center(child: CircularProgressIndicator()))
          else if (_error != null)
            Expanded(child: Center(child: Text(_error!)))
          else
            Expanded(child: _buildForecast(_forecast!, palette)),
        ],
      ),
    );
  }

  Widget _buildForecast(PurchaseForecast f, HcpPalette palette) {
    if (f.lines.isEmpty) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.inventory_outlined, size: 48),
            const SizedBox(height: 12),
            Text(
              'Aún no hay órdenes entregadas en los últimos ${f.historicalDays} días.',
              style: Theme.of(context).textTheme.bodyMedium,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 4),
            Text('Marca pedidos como "delivered" para alimentar la predicción.',
                style: Theme.of(context).textTheme.bodySmall),
          ],
        ),
      );
    }

    final df = DateFormat('dd MMM', 'es');
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Wrap(
          spacing: 24,
          children: [
            _Chip(
                label: 'Ventana',
                value: '${df.format(f.historicalFrom.toLocal())} → '
                    '${df.format(f.historicalTo.toLocal())}'),
            _Chip(
                label: 'Órdenes analizadas',
                value: '${f.ordersAnalyzed}'),
            _Chip(
                label: 'Compra estimada',
                value: '\$${f.totalEstimatedCostUsd.toStringAsFixed(2)}',
                color: palette.green),
          ],
        ),
        const SizedBox(height: 12),
        Expanded(
          child: Card(
            clipBehavior: Clip.antiAlias,
            child: SingleChildScrollView(
              child: DataTable(
                columnSpacing: 16,
                columns: const [
                  DataColumn(label: Text('Insumo')),
                  DataColumn(label: Text('Promedio diario'), numeric: true),
                  DataColumn(label: Text('Proyectado'), numeric: true),
                  DataColumn(label: Text('Stock'), numeric: true),
                  DataColumn(label: Text('Sugerido'), numeric: true),
                  DataColumn(label: Text('Costo est.'), numeric: true),
                ],
                rows: f.lines.map((l) {
                  final shortfall =
                      l.suggestedPurchaseUseUnit > 0 ? palette.red : palette.green;
                  return DataRow(cells: [
                    DataCell(Text(l.ingredientName)),
                    DataCell(Text(
                        '${l.dailyAverageUseUnit.toStringAsFixed(1)} ${l.useUnit}')),
                    DataCell(Text(
                        '${l.projectedUseUnit.toStringAsFixed(0)} ${l.useUnit}')),
                    DataCell(Text(
                        '${l.currentStockUseUnit.toStringAsFixed(0)} ${l.useUnit}')),
                    DataCell(Text(
                      '${l.suggestedPurchaseUseUnit.toStringAsFixed(0)} ${l.useUnit}',
                      style: TextStyle(color: shortfall, fontWeight: FontWeight.w600),
                    )),
                    DataCell(Text(l.estimatedCostUsd != null
                        ? '\$${l.estimatedCostUsd!.toStringAsFixed(2)}'
                        : '—')),
                  ]);
                }).toList(),
              ),
            ),
          ),
        ),
      ],
    );
  }
}

class _Slider extends StatelessWidget {
  final String label;
  final double value;
  final double min;
  final double max;
  final int divisions;
  final String suffix;
  final int decimals;
  final ValueChanged<double> onChanged;
  final ValueChanged<double> onChangeEnd;
  const _Slider({
    required this.label,
    required this.value,
    required this.min,
    required this.max,
    required this.divisions,
    required this.suffix,
    required this.onChanged,
    required this.onChangeEnd,
    this.decimals = 0,
  });

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 240,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('$label: ${value.toStringAsFixed(decimals)} $suffix',
              style: Theme.of(context).textTheme.labelMedium),
          Slider(
            value: value,
            min: min,
            max: max,
            divisions: divisions,
            onChanged: onChanged,
            onChangeEnd: onChangeEnd,
          ),
        ],
      ),
    );
  }
}

class _Chip extends StatelessWidget {
  final String label;
  final String value;
  final Color? color;
  const _Chip({required this.label, required this.value, this.color});

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: BoxDecoration(
        color: color ?? palette.line,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(label,
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: color != null ? Colors.white70 : null,
                  )),
          Text(value,
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    color: color != null ? Colors.white : null,
                  )),
        ],
      ),
    );
  }
}
