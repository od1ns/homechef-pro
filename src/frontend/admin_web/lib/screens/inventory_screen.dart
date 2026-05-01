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
      if (mounted) setState(() {
        _all = list;
        _busy = false;
      });
    } on ApiException catch (e) {
      if (mounted) setState(() {
        _error = e.message;
        _busy = false;
      });
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

    return Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('Inventario',
                  style: Theme.of(context).textTheme.displaySmall),
              ElevatedButton.icon(
                  onPressed: _newIngredient,
                  icon: const Icon(Icons.add),
                  label: const Text('Nuevo insumo')),
            ],
          ),
          const SizedBox(height: 16),
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
                    hintText: 'Buscar…',
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
          const SizedBox(height: 16),
          Expanded(
            child: filtered.isEmpty
                ? const Center(child: Text('Nada que mostrar'))
                : ListView.separated(
                    itemCount: filtered.length,
                    separatorBuilder: (_, __) => const SizedBox(height: 8),
                    itemBuilder: (_, i) {
                      final ing = filtered[i];
                      return Card(
                        child: ListTile(
                          leading: _StockBadge(ingredient: ing, palette: palette),
                          title: Text(ing.name),
                          subtitle: Text(
                              'Stock: ${ing.currentStockUseUnit.toStringAsFixed(2)} ${ing.useUnit} '
                              '· avg \$${ing.avgCostPerUseUnitUsd.toStringAsFixed(4)}/${ing.useUnit}'),
                          trailing: const Icon(Icons.chevron_right),
                          onTap: () => _openDetail(ing),
                        ),
                      );
                    },
                  ),
          ),
        ],
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
