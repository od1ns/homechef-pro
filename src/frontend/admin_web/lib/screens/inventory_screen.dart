import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';

import 'ingredient_detail_drawer.dart';

class InventoryScreen extends StatefulWidget {
  final HcpApi api;
  const InventoryScreen({super.key, required this.api});

  @override
  State<InventoryScreen> createState() => _InventoryScreenState();
}

class _InventoryScreenState extends State<InventoryScreen> {
  List<IngredientSummary> _all = const [];
  bool _busy = true;
  String? _error;
  String _search = '';
  bool _onlyBelowReorder = false;
  bool _onlyActive = true;

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
      final list = await widget.api.adminListIngredients(
        onlyActive: _onlyActive,
        onlyBelowReorder: _onlyBelowReorder,
      );
      if (mounted) {
        setState(() {
          _all = list;
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

  Future<void> _openDetail(IngredientSummary s) async {
    await showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      builder: (_) => DraggableScrollableSheet(
        expand: false,
        initialChildSize: 0.85,
        maxChildSize: 0.95,
        builder: (_, controller) => IngredientDetailDrawer(
          api: widget.api,
          ingredientId: s.id,
          scrollController: controller,
          onChanged: _load,
        ),
      ),
    );
  }

  Future<void> _newIngredient() async {
    final name = TextEditingController();
    final reorder = TextEditingController(text: '0');
    final minimum = TextEditingController(text: '0');
    String useUnit = 'g';
    String? error;
    final created = await showDialog<String>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setState) => AlertDialog(
          title: const Text('Nuevo insumo'),
          content: SizedBox(
            width: 420,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                TextField(
                    controller: name,
                    decoration: const InputDecoration(labelText: 'Nombre')),
                const SizedBox(height: 12),
                Row(children: [
                  const Text('Unidad de uso:'),
                  const SizedBox(width: 12),
                  DropdownButton<String>(
                    value: useUnit,
                    items: const [
                      DropdownMenuItem(value: 'g', child: Text('g')),
                      DropdownMenuItem(value: 'ml', child: Text('ml')),
                      DropdownMenuItem(value: 'unit', child: Text('unidad')),
                    ],
                    onChanged: (v) => setState(() => useUnit = v ?? 'g'),
                  ),
                ]),
                const SizedBox(height: 12),
                TextField(
                    controller: reorder,
                    decoration: const InputDecoration(
                        labelText: 'Punto de reorden (en unidad de uso)'),
                    keyboardType:
                        const TextInputType.numberWithOptions(decimal: true)),
                const SizedBox(height: 8),
                TextField(
                    controller: minimum,
                    decoration: const InputDecoration(
                        labelText: 'Stock mínimo (en unidad de uso)'),
                    keyboardType:
                        const TextInputType.numberWithOptions(decimal: true)),
                if (error != null) ...[
                  const SizedBox(height: 12),
                  Text(error!, style: const TextStyle(color: Colors.red)),
                ],
              ],
            ),
          ),
          actions: [
            TextButton(
                onPressed: () => Navigator.pop(ctx),
                child: const Text('Cancelar')),
            ElevatedButton(
              onPressed: () async {
                try {
                  final id = await widget.api.adminCreateIngredient(
                    name: name.text.trim(),
                    useUnit: useUnit,
                    reorderPointUseUnit:
                        double.tryParse(reorder.text.trim()) ?? 0,
                    minimumStockUseUnit:
                        double.tryParse(minimum.text.trim()) ?? 0,
                  );
                  if (ctx.mounted) Navigator.pop(ctx, id);
                } on ApiException catch (e) {
                  setState(() => error = e.message);
                }
              },
              child: const Text('Crear'),
            ),
          ],
        ),
      ),
    );
    if (created != null) await _load();
  }

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
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

    final filtered = _all.where((i) =>
        _search.isEmpty || i.name.toLowerCase().contains(_search.toLowerCase()))
        .toList();

    // Metricas para las KPI cards. Iteramos una sola vez sobre _all para
    // evitar tres pasadas separadas.
    var totalValueUsd = 0.0;
    var inStockCount = 0;
    var lowStockCount = 0;
    var outOfStockCount = 0;
    for (final i in _all) {
      totalValueUsd += i.currentStockUseUnit * i.avgCostPerUseUnitUsd;
      if (i.isOutOfStock) {
        outOfStockCount++;
      } else if (i.isBelowReorderPoint) {
        lowStockCount++;
      } else {
        inStockCount++;
      }
    }

    return Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Header: titulo + boton nuevo insumo
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Inventario',
                      style: Theme.of(context).textTheme.displaySmall),
                  const SizedBox(height: 4),
                  Text(
                    'Estado actualizado segun ventas y compras',
                    style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                          color: palette.inkMuted,
                        ),
                  ),
                ],
              ),
              ElevatedButton.icon(
                  onPressed: _newIngredient,
                  icon: const Icon(Icons.add),
                  label: const Text('Nuevo insumo')),
            ],
          ),
          const SizedBox(height: 24),

          // KPI cards (4 en fila, se envuelven si la pantalla es chica)
          LayoutBuilder(
            builder: (context, c) {
              final cardWidth = ((c.maxWidth - 36) / 4).clamp(180.0, 320.0);
              return Wrap(
                spacing: 12,
                runSpacing: 12,
                children: [
                  _KpiCard(
                    label: 'Valor total',
                    value: '\$${totalValueUsd.toStringAsFixed(2)}',
                    accent: palette.accent,
                    width: cardWidth,
                  ),
                  _KpiCard(
                    label: 'En stock',
                    value: '$inStockCount',
                    accent: palette.green,
                    width: cardWidth,
                  ),
                  _KpiCard(
                    label: 'Stock bajo',
                    value: '$lowStockCount',
                    accent: palette.sun,
                    width: cardWidth,
                  ),
                  _KpiCard(
                    label: 'Agotado',
                    value: '$outOfStockCount',
                    accent: palette.red,
                    width: cardWidth,
                  ),
                ],
              );
            },
          ),
          const SizedBox(height: 24),

          // Toolbar: busqueda + filtros
          Wrap(
            spacing: 16,
            runSpacing: 8,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              SizedBox(
                width: 280,
                child: TextField(
                  decoration: const InputDecoration(
                    prefixIcon: Icon(Icons.search),
                    hintText: 'Buscar...',
                  ),
                  onChanged: (v) => setState(() => _search = v),
                ),
              ),
              FilterChip(
                label: const Text('Solo bajo reorden'),
                selected: _onlyBelowReorder,
                onSelected: (v) {
                  setState(() => _onlyBelowReorder = v);
                  _load();
                },
              ),
              FilterChip(
                label: const Text('Solo activos'),
                selected: _onlyActive,
                onSelected: (v) {
                  setState(() => _onlyActive = v);
                  _load();
                },
              ),
            ],
          ),
          const SizedBox(height: 12),

          // Tabla de ingredientes con columnas
          Expanded(
            child: filtered.isEmpty
                ? const Center(child: Text('Nada que mostrar'))
                : Card(
                    child: SingleChildScrollView(
                      scrollDirection: Axis.horizontal,
                      child: SingleChildScrollView(
                        child: DataTable(
                          showCheckboxColumn: false,
                          headingRowColor: WidgetStateProperty.all(palette.bg),
                          columns: const [
                            DataColumn(label: Text('INGREDIENTE')),
                            DataColumn(label: Text('STOCK'), numeric: true),
                            DataColumn(label: Text('PUNTO DE REORDEN'), numeric: true),
                            DataColumn(label: Text('COSTO UNIT.'), numeric: true),
                            DataColumn(label: Text('ESTADO')),
                            DataColumn(label: Text('')),
                          ],
                          rows: filtered.map((ing) => DataRow(
                                onSelectChanged: (_) => _openDetail(ing),
                                cells: [
                                  DataCell(Row(children: [
                                    _StockBadge(ingredient: ing, palette: palette),
                                    const SizedBox(width: 8),
                                    Text(
                                      ing.name,
                                      style: const TextStyle(fontWeight: FontWeight.w500),
                                    ),
                                  ])),
                                  DataCell(Text(
                                    '${_fmt(ing.currentStockUseUnit)} ${ing.useUnit}',
                                  )),
                                  DataCell(Text(
                                    '${_fmt(ing.reorderPointUseUnit)} ${ing.useUnit}',
                                  )),
                                  DataCell(Text(
                                    '\$${ing.avgCostPerUseUnitUsd.toStringAsFixed(4)}/${ing.useUnit}',
                                  )),
                                  DataCell(_StatusChip(ingredient: ing, palette: palette)),
                                  DataCell(
                                    ing.isOutOfStock || ing.isBelowReorderPoint
                                        ? OutlinedButton.icon(
                                            onPressed: () => _openDetail(ing),
                                            icon: const Icon(Icons.shopping_cart, size: 16),
                                            label: const Text('Ordenar'),
                                          )
                                        : IconButton(
                                            onPressed: () => _openDetail(ing),
                                            icon: const Icon(Icons.chevron_right),
                                          ),
                                  ),
                                ],
                              )).toList(),
                        ),
                      ),
                    ),
                  ),
          ),
        ],
      ),
    );
  }

  static String _fmt(double v) {
    if (v >= 10) return v.toStringAsFixed(0);
    if (v >= 1) return v.toStringAsFixed(1);
    return v.toStringAsFixed(2);
  }
}

class _KpiCard extends StatelessWidget {
  final String label;
  final String value;
  final Color accent;
  final double width;
  const _KpiCard({
    required this.label,
    required this.value,
    required this.accent,
    required this.width,
  });

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    return SizedBox(
      width: width,
      child: Card(
        margin: EdgeInsets.zero,
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(width: 40, height: 3, color: accent),
              const SizedBox(height: 8),
              Text(
                label.toUpperCase(),
                style: Theme.of(context).textTheme.labelSmall?.copyWith(
                      color: palette.inkMuted,
                      letterSpacing: 0.6,
                    ),
              ),
              const SizedBox(height: 4),
              Text(
                value,
                style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                      fontWeight: FontWeight.w600,
                    ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _StatusChip extends StatelessWidget {
  final IngredientSummary ingredient;
  final HcpPalette palette;
  const _StatusChip({required this.ingredient, required this.palette});

  @override
  Widget build(BuildContext context) {
    final (label, bg, fg) = !ingredient.isActive
        ? ('INACTIVO', palette.line, palette.inkMuted)
        : ingredient.isOutOfStock
            ? ('AGOTADO', palette.redSoft, palette.red)
            : ingredient.isBelowReorderPoint
                ? ('STOCK BAJO', palette.sun, palette.ink)
                : ('EN STOCK', palette.greenSoft, palette.green);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(6),
      ),
      child: Text(
        label,
        style: TextStyle(
          color: fg,
          fontSize: 11,
          fontWeight: FontWeight.w600,
          letterSpacing: 0.5,
        ),
      ),
    );
  }
}

class _StockBadge extends StatelessWidget {
  final IngredientSummary ingredient;
  final HcpPalette palette;
  const _StockBadge({required this.ingredient, required this.palette});

  @override
  Widget build(BuildContext context) {
    final color = !ingredient.isActive
        ? palette.inkMuted
        : ingredient.currentStockUseUnit <= 0
            ? palette.red
            : ingredient.isBelowReorderPoint
                ? palette.sun
                : palette.green;
    return Container(
      width: 12,
      height: 12,
      decoration: BoxDecoration(color: color, shape: BoxShape.circle),
    );
  }
}
