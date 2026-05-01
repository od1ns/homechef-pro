import 'dart:async';

import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';

class RecipeEditorScreen extends StatefulWidget {
  final HcpApi api;
  final String recipeId;
  const RecipeEditorScreen({super.key, required this.api, required this.recipeId});

  @override
  State<RecipeEditorScreen> createState() => _RecipeEditorScreenState();
}

class _RecipeEditorScreenState extends State<RecipeEditorScreen> {
  Recipe? _recipe;
  RecipeCost? _cost;
  Map<String, IngredientSummary> _ingredientsById = const {};
  Map<String, RecipeSummary> _recipesById = const {};
  bool _busy = true;
  String? _error;
  final _priceCtrl = TextEditingController();
  Timer? _priceDebounce;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _priceDebounce?.cancel();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final recipeF = widget.api.adminGetRecipe(widget.recipeId);
      final costF = widget.api.adminGetRecipeCost(widget.recipeId);
      final ingF = widget.api.adminListIngredients(onlyActive: true);
      final allRecipesF = widget.api.adminListRecipes(includeSubRecipes: true);

      final recipe = await recipeF;
      final cost = await costF;
      final ingredients = await ingF;
      final allRecipes = await allRecipesF;

      _priceCtrl.text = recipe.sellingPriceUsd?.toStringAsFixed(2) ?? '';
      if (mounted) {
        setState(() {
          _recipe = recipe;
          _cost = cost;
          _ingredientsById = {for (final i in ingredients) i.id: i};
          _recipesById = {for (final r in allRecipes) r.id: r};
          _busy = false;
        });
      }
    } on ApiException catch (e) {
      if (mounted) setState(() {
        _error = e.message;
        _busy = false;
      });
    }
  }

  Future<void> _refreshCost() async {
    try {
      final c = await widget.api.adminGetRecipeCost(widget.recipeId);
      if (mounted) setState(() => _cost = c);
    } on ApiException {/* ignore — UI keeps last value */}
  }

  void _onPriceChanged(String value) {
    _priceDebounce?.cancel();
    _priceDebounce = Timer(const Duration(milliseconds: 800), () async {
      final parsed = double.tryParse(value.trim());
      if (parsed == null || parsed <= 0) return;
      try {
        await widget.api.adminUpdateSellingPrice(widget.recipeId, parsed);
        await _load();
      } on ApiException catch (e) {
        _toast(e.message);
      }
    });
  }

  Future<void> _toggleStock(bool outOfStock) async {
    try {
      await widget.api.adminToggleOutOfStock(widget.recipeId, outOfStock);
      await _load();
    } on ApiException catch (e) {
      _toast(e.message);
    }
  }

  Future<void> _addComponent() async {
    final all = _ingredientsById.values.toList()
      ..sort((a, b) => a.name.compareTo(b.name));
    final allSubs = _recipesById.values
        .where((r) => r.isSubRecipe && r.id != widget.recipeId)
        .toList()
      ..sort((a, b) => a.name.compareTo(b.name));

    await showDialog<void>(
      context: context,
      builder: (ctx) => _AddComponentDialog(
        ingredients: all,
        subRecipes: allSubs,
        onAddIngredient: (id, qty, notes) async {
          await widget.api.adminAddIngredientComponent(
            recipeId: widget.recipeId,
            ingredientId: id,
            quantity: qty,
            notes: notes,
          );
        },
        onAddSubRecipe: (id, qty, notes) async {
          await widget.api.adminAddSubRecipeComponent(
            recipeId: widget.recipeId,
            subRecipeId: id,
            quantity: qty,
            notes: notes,
          );
        },
      ),
    );
    await _load();
  }

  void _toast(String message) =>
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));

  @override
  Widget build(BuildContext context) {
    if (_busy) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }
    if (_error != null || _recipe == null) {
      return Scaffold(
        appBar: AppBar(),
        body: Center(child: Text(_error ?? 'Receta no encontrada')),
      );
    }

    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    final r = _recipe!;
    final c = _cost!;
    final isWide = MediaQuery.of(context).size.width >= 1100;
    final body = isWide
        ? Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Expanded(flex: 2, child: _buildLeftColumn(r)),
            const SizedBox(width: 24),
            Expanded(child: _buildCostPanel(c, r, palette)),
          ])
        : ListView(
            padding: const EdgeInsets.all(24),
            children: [
              _buildLeftColumn(r),
              const SizedBox(height: 24),
              _buildCostPanel(c, r, palette),
            ],
          );

    return Scaffold(
      backgroundColor: palette.bg,
      appBar: AppBar(
        backgroundColor: palette.bg,
        title: Text(r.name),
        actions: [
          IconButton(icon: const Icon(Icons.refresh), onPressed: _load),
        ],
      ),
      body: SafeArea(child: body),
    );
  }

  Widget _buildLeftColumn(Recipe r) {
    return Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(r.isSubRecipe ? 'Sub-receta' : 'Plato',
              style: Theme.of(context).textTheme.bodySmall),
          const SizedBox(height: 4),
          Text(r.name, style: Theme.of(context).textTheme.displaySmall),
          if (r.description != null) ...[
            const SizedBox(height: 8),
            Text(r.description!, style: Theme.of(context).textTheme.bodyMedium),
          ],
          const SizedBox(height: 24),
          if (!r.isSubRecipe) ...[
            Row(children: [
              Expanded(
                child: TextField(
                  controller: _priceCtrl,
                  decoration: const InputDecoration(
                    labelText: 'Precio de venta (USD)',
                    prefixText: r'$ ',
                  ),
                  keyboardType:
                      const TextInputType.numberWithOptions(decimal: true),
                  onChanged: _onPriceChanged,
                ),
              ),
              const SizedBox(width: 16),
              Switch.adaptive(
                value: r.isOutOfStock,
                onChanged: _toggleStock,
              ),
              const Text('Agotado'),
            ]),
            const SizedBox(height: 24),
          ],
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('Componentes (${r.components.length})',
                  style: Theme.of(context).textTheme.titleLarge),
              ElevatedButton.icon(
                onPressed: _addComponent,
                icon: const Icon(Icons.add),
                label: const Text('Agregar'),
              ),
            ],
          ),
          const SizedBox(height: 12),
          if (r.components.isEmpty)
            const Text('Aún no hay componentes.')
          else
            ...r.components.map((comp) {
              final ingName = comp.ingredientId != null
                  ? _ingredientsById[comp.ingredientId]?.name ?? '(insumo)'
                  : null;
              final subName = comp.subRecipeId != null
                  ? _recipesById[comp.subRecipeId]?.name ?? '(sub-receta)'
                  : null;
              final unit = comp.ingredientId != null
                  ? _ingredientsById[comp.ingredientId]?.useUnit ?? ''
                  : '';
              return Card(
                child: ListTile(
                  leading: Icon(comp.ingredientId != null
                      ? Icons.eco_outlined
                      : Icons.layers_outlined),
                  title: Text(ingName ?? subName ?? '?'),
                  subtitle: Text(
                    [
                      '${comp.quantity}${unit.isNotEmpty ? ' $unit' : ''}',
                      if (comp.notes != null && comp.notes!.isNotEmpty) comp.notes!,
                    ].join(' · '),
                  ),
                ),
              );
            }),
        ],
      ),
    );
  }

  Widget _buildCostPanel(RecipeCost c, Recipe r, HcpPalette palette) {
    final selling = r.sellingPriceUsd ?? 0;
    final hasSelling = selling > 0;
    final margin = hasSelling
        ? (selling - c.totalCostUsd) / selling * 100
        : 0;
    final marginColor = !hasSelling
        ? palette.inkMuted
        : margin < 30
            ? palette.red
            : margin < 50
                ? palette.sun
                : palette.green;

    return Padding(
      padding: const EdgeInsets.all(24),
      child: Card(
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Icon(Icons.calculate_outlined, color: palette.accent),
                  const SizedBox(width: 8),
                  Text('Costo en cascada',
                      style: Theme.of(context).textTheme.titleLarge),
                ],
              ),
              const SizedBox(height: 16),
              _kpi('Costo total',
                  '\$${c.totalCostUsd.toStringAsFixed(4)}',
                  palette.ink),
              if (c.isSubRecipe && c.costPerYieldUnit != null)
                _kpi('Costo por ${c.yieldUnit}',
                    '\$${c.costPerYieldUnit!.toStringAsFixed(6)}',
                    palette.ink),
              if (!r.isSubRecipe && hasSelling) ...[
                const Divider(height: 24),
                _kpi('Precio de venta',
                    '\$${selling.toStringAsFixed(2)}',
                    palette.ink),
                _kpi(
                    'Ganancia bruta',
                    '\$${(selling - c.totalCostUsd).toStringAsFixed(2)}',
                    margin >= 30 ? palette.green : palette.red),
                _kpi('Margen', '${margin.toStringAsFixed(1)}%', marginColor),
              ],
              if (c.lines.isNotEmpty) ...[
                const Divider(height: 24),
                Text('Desglose',
                    style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: 8),
                ...c.lines.map((line) => Padding(
                      padding: const EdgeInsets.symmetric(vertical: 4),
                      child: Row(
                        children: [
                          Icon(
                            line.kind == 'ingredient'
                                ? Icons.eco_outlined
                                : Icons.layers_outlined,
                            size: 16,
                            color: palette.inkSoft,
                          ),
                          const SizedBox(width: 8),
                          Expanded(
                            child: Text(
                              line.refName,
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                          const SizedBox(width: 8),
                          Text(
                            '${line.quantity.toStringAsFixed(2)} ${line.unitLabel}',
                            style: Theme.of(context).textTheme.bodySmall,
                          ),
                          const SizedBox(width: 12),
                          Text('\$${line.lineCostUsd.toStringAsFixed(4)}',
                              style:
                                  Theme.of(context).textTheme.labelMedium),
                        ],
                      ),
                    )),
              ],
              const SizedBox(height: 16),
              Text(
                'Los costos usan el avg_cost_per_use_unit_usd actual de cada '
                'insumo (calculado por trigger SQL en cada compra).',
                style: Theme.of(context).textTheme.bodySmall,
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _kpi(String label, String value, Color valueColor) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: Theme.of(context).textTheme.bodyMedium),
          Text(value,
              style: Theme.of(context)
                  .textTheme
                  .titleMedium
                  ?.copyWith(color: valueColor)),
        ],
      ),
    );
  }
}

class _AddComponentDialog extends StatefulWidget {
  final List<IngredientSummary> ingredients;
  final List<RecipeSummary> subRecipes;
  final Future<void> Function(String id, double qty, String? notes) onAddIngredient;
  final Future<void> Function(String id, double qty, String? notes) onAddSubRecipe;

  const _AddComponentDialog({
    required this.ingredients,
    required this.subRecipes,
    required this.onAddIngredient,
    required this.onAddSubRecipe,
  });

  @override
  State<_AddComponentDialog> createState() => _AddComponentDialogState();
}

class _AddComponentDialogState extends State<_AddComponentDialog>
    with SingleTickerProviderStateMixin {
  late final TabController _tab = TabController(length: 2, vsync: this);
  String _search = '';
  String? _selectedIngredientId;
  String? _selectedSubId;
  final _qtyCtrl = TextEditingController();
  final _notesCtrl = TextEditingController();
  bool _busy = false;
  String? _error;

  @override
  void dispose() {
    _tab.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final qty = double.tryParse(_qtyCtrl.text.trim()) ?? 0;
      if (qty <= 0) {
        setState(() {
          _error = 'Cantidad debe ser positiva';
          _busy = false;
        });
        return;
      }
      final notes = _notesCtrl.text.trim().isEmpty
          ? null
          : _notesCtrl.text.trim();
      if (_tab.index == 0) {
        if (_selectedIngredientId == null) {
          setState(() {
            _error = 'Selecciona un insumo';
            _busy = false;
          });
          return;
        }
        await widget.onAddIngredient(_selectedIngredientId!, qty, notes);
      } else {
        if (_selectedSubId == null) {
          setState(() {
            _error = 'Selecciona una sub-receta';
            _busy = false;
          });
          return;
        }
        await widget.onAddSubRecipe(_selectedSubId!, qty, notes);
      }
      if (mounted) Navigator.pop(context);
    } on ApiException catch (e) {
      setState(() {
        _error = e.message;
        _busy = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Agregar componente'),
      content: SizedBox(
        width: 520,
        height: 480,
        child: Column(children: [
          TabBar(
            controller: _tab,
            tabs: const [Tab(text: 'Insumo'), Tab(text: 'Sub-receta')],
          ),
          const SizedBox(height: 12),
          TextField(
            decoration: const InputDecoration(
              prefixIcon: Icon(Icons.search),
              hintText: 'Buscar…',
            ),
            onChanged: (v) => setState(() => _search = v.toLowerCase()),
          ),
          const SizedBox(height: 8),
          Expanded(
            child: TabBarView(
              controller: _tab,
              children: [
                _list<IngredientSummary>(
                  items: widget.ingredients
                      .where((i) =>
                          _search.isEmpty || i.name.toLowerCase().contains(_search))
                      .toList(),
                  isSelected: (i) => i.id == _selectedIngredientId,
                  label: (i) => i.name,
                  subtitle: (i) =>
                      '${i.useUnit} · stock ${i.currentStockUseUnit.toStringAsFixed(0)}',
                  onSelect: (i) => setState(() => _selectedIngredientId = i.id),
                ),
                _list<RecipeSummary>(
                  items: widget.subRecipes
                      .where((r) =>
                          _search.isEmpty || r.name.toLowerCase().contains(_search))
                      .toList(),
                  isSelected: (r) => r.id == _selectedSubId,
                  label: (r) => r.name,
                  subtitle: (r) => r.category ?? '',
                  onSelect: (r) => setState(() => _selectedSubId = r.id),
                ),
              ],
            ),
          ),
          const SizedBox(height: 12),
          Row(children: [
            Expanded(
              child: TextField(
                controller: _qtyCtrl,
                decoration: const InputDecoration(labelText: 'Cantidad'),
                keyboardType: const TextInputType.numberWithOptions(decimal: true),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: TextField(
                controller: _notesCtrl,
                decoration: const InputDecoration(labelText: 'Notas (opcional)'),
              ),
            ),
          ]),
          if (_error != null) ...[
            const SizedBox(height: 8),
            Text(_error!, style: const TextStyle(color: Colors.red)),
          ],
        ]),
      ),
      actions: [
        TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Cancelar')),
        ElevatedButton(
          onPressed: _busy ? null : _submit,
          child: _busy
              ? const SizedBox(
                  height: 18,
                  width: 18,
                  child: CircularProgressIndicator(strokeWidth: 2))
              : const Text('Agregar'),
        ),
      ],
    );
  }

  Widget _list<T>({
    required List<T> items,
    required bool Function(T) isSelected,
    required String Function(T) label,
    required String Function(T) subtitle,
    required ValueChanged<T> onSelect,
  }) {
    if (items.isEmpty) return const Center(child: Text('Sin resultados'));
    return ListView.separated(
      itemCount: items.length,
      separatorBuilder: (_, __) => const Divider(height: 1),
      itemBuilder: (_, i) {
        final it = items[i];
        return ListTile(
          dense: true,
          title: Text(label(it)),
          subtitle: Text(subtitle(it)),
          trailing:
              isSelected(it) ? const Icon(Icons.check_circle) : null,
          onTap: () => onSelect(it),
        );
      },
    );
  }
}
