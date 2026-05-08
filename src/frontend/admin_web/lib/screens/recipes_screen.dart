import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';

import 'recipe_editor_screen.dart';

class RecipesScreen extends StatefulWidget {
  final HcpApi api;
  const RecipesScreen({super.key, required this.api});

  @override
  State<RecipesScreen> createState() => _RecipesScreenState();
}

class _RecipesScreenState extends State<RecipesScreen> {
  List<RecipeSummary> _all = const [];
  bool _loading = true;
  String? _error;
  String _filter = 'dishes';     // 'dishes' | 'subs'
  String _search = '';

  @override
  void initState() {
    super.initState();
    _refresh();
  }

  Future<void> _refresh() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final list = await widget.api.adminListRecipes(
        includeSubRecipes: true,
        onlyActive: false,
      );
      if (mounted) {
        setState(() {
          _all = list;
          _loading = false;
        });
      }
    } on ApiException catch (e) {
      if (mounted) {
        setState(() {
          _error = '${e.statusCode} ${e.message}';
          _loading = false;
        });
      }
    }
  }

  Future<void> _newDish() async {
    final id = await _showCreateDialog(isSubRecipe: false);
    if (id != null) await _openEditor(id);
  }

  Future<void> _newSub() async {
    final id = await _showCreateDialog(isSubRecipe: true);
    if (id != null) await _openEditor(id);
  }

  Future<String?> _showCreateDialog({required bool isSubRecipe}) async {
    final nameCtrl = TextEditingController();
    final priceCtrl = TextEditingController();
    final yieldCtrl = TextEditingController();
    String yieldUnit = 'g';
    final prepCtrl = TextEditingController(text: '0');
    String? error;

    return showDialog<String>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setState) => AlertDialog(
          title: Text(isSubRecipe ? 'Nueva sub-receta' : 'Nuevo plato'),
          content: SizedBox(
            width: 420,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                TextField(
                    controller: nameCtrl,
                    decoration: const InputDecoration(labelText: 'Nombre')),
                const SizedBox(height: 12),
                if (isSubRecipe) ...[
                  Row(children: [
                    Expanded(
                      child: TextField(
                        controller: yieldCtrl,
                        decoration: const InputDecoration(labelText: 'Rendimiento'),
                        keyboardType:
                            const TextInputType.numberWithOptions(decimal: true),
                      ),
                    ),
                    const SizedBox(width: 12),
                    DropdownButton<String>(
                      value: yieldUnit,
                      items: const [
                        DropdownMenuItem(value: 'g', child: Text('g')),
                        DropdownMenuItem(value: 'ml', child: Text('ml')),
                        DropdownMenuItem(value: 'portion', child: Text('porción')),
                        DropdownMenuItem(value: 'unit', child: Text('unidad')),
                      ],
                      onChanged: (v) => setState(() => yieldUnit = v ?? 'g'),
                    ),
                  ]),
                ] else ...[
                  TextField(
                    controller: priceCtrl,
                    decoration: const InputDecoration(
                      labelText: 'Precio de venta (USD)',
                      prefixText: r'$ ',
                    ),
                    keyboardType: const TextInputType.numberWithOptions(decimal: true),
                  ),
                ],
                const SizedBox(height: 12),
                TextField(
                  controller: prepCtrl,
                  decoration: const InputDecoration(labelText: 'Tiempo prep (min)'),
                  keyboardType: TextInputType.number,
                ),
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
              child: const Text('Cancelar'),
            ),
            ElevatedButton(
              onPressed: () async {
                try {
                  final prep = int.tryParse(prepCtrl.text.trim()) ?? 0;
                  String id;
                  if (isSubRecipe) {
                    id = await widget.api.adminCreateSubRecipe(
                      name: nameCtrl.text.trim(),
                      yieldQuantity:
                          double.tryParse(yieldCtrl.text.trim()) ?? 0,
                      yieldUnit: yieldUnit,
                      prepTimeMinutes: prep,
                    );
                  } else {
                    id = await widget.api.adminCreateDish(
                      name: nameCtrl.text.trim(),
                      sellingPriceUsd:
                          double.tryParse(priceCtrl.text.trim()) ?? 0,
                      prepTimeMinutes: prep,
                    );
                  }
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
  }

  Future<void> _openEditor(String recipeId) async {
    await Navigator.of(context).push(MaterialPageRoute(
      builder: (_) => RecipeEditorScreen(api: widget.api, recipeId: recipeId),
    ));
    _refresh();
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) return const Center(child: CircularProgressIndicator());
    if (_error != null) {
      return Center(
        child: Column(mainAxisSize: MainAxisSize.min, children: [
          Text(_error!),
          const SizedBox(height: 12),
          ElevatedButton(onPressed: _refresh, child: const Text('Reintentar')),
        ]),
      );
    }

    final filtered = _all.where((r) {
      final matchesType = _filter == 'subs' ? r.isSubRecipe : !r.isSubRecipe;
      final matchesSearch = _search.isEmpty ||
          r.name.toLowerCase().contains(_search.toLowerCase());
      return matchesType && matchesSearch;
    }).toList();

    return Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('Menú y recetas',
                  style: Theme.of(context).textTheme.displaySmall),
              Row(children: [
                ElevatedButton.icon(
                    onPressed: _newDish,
                    icon: const Icon(Icons.add),
                    label: const Text('Nuevo plato')),
                const SizedBox(width: 8),
                OutlinedButton.icon(
                    onPressed: _newSub,
                    icon: const Icon(Icons.add),
                    label: const Text('Nueva sub-receta')),
              ]),
            ],
          ),
          const SizedBox(height: 16),
          Row(
            children: [
              SegmentedButton<String>(
                segments: const [
                  ButtonSegment(value: 'dishes', label: Text('Platos')),
                  ButtonSegment(value: 'subs', label: Text('Sub-recetas')),
                ],
                selected: {_filter},
                onSelectionChanged: (v) =>
                    setState(() => _filter = v.first),
              ),
              const SizedBox(width: 16),
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
                      final r = filtered[i];
                      return Card(
                        child: ListTile(
                          title: Text(r.name),
                          subtitle: Text(
                            [
                              if (r.category != null) r.category!,
                              if (!r.isSubRecipe && r.sellingPriceUsd != null)
                                '\$${r.sellingPriceUsd!.toStringAsFixed(2)}',
                              '${r.prepTimeMinutes} min',
                              if (!r.isActive) 'inactivo',
                              if (r.isOutOfStock) 'agotado',
                            ].join(' · '),
                          ),
                          trailing: const Icon(Icons.chevron_right),
                          onTap: () => _openEditor(r.id),
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
