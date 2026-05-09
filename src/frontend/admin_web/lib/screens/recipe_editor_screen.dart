import 'dart:async';

import 'package:file_picker/file_picker.dart';
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
      if (mounted) {
        setState(() {
          _error = e.message;
          _busy = false;
        });
      }
    }
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

  /// Etapa 1: subir foto del plato. Abre file picker, lee bytes, sube al
  /// backend, recarga la receta con el nuevo imageUrl.
  Future<void> _uploadImage() async {
    final result = await FilePicker.platform.pickFiles(
      type: FileType.image,
      withData: true,
      allowMultiple: false,
    );
    if (result == null || result.files.isEmpty) return;
    final file = result.files.first;
    if (file.bytes == null) {
      _toast('No se pudo leer el archivo.');
      return;
    }
    final ext = (file.extension ?? 'jpg').toLowerCase();
    final contentType = ext == 'png'
        ? 'image/png'
        : (ext == 'webp' ? 'image/webp' : 'image/jpeg');

    setState(() => _busy = true);
    try {
      await widget.api.adminUploadRecipeImage(
        recipeId: widget.recipeId,
        bytes: file.bytes!,
        filename: file.name,
        contentType: contentType,
      );
      await _load();
    } on ApiException catch (e) {
      setState(() {
        _error = e.message;
        _busy = false;
      });
    } catch (e) {
      setState(() {
        _error = '$e';
        _busy = false;
      });
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
  // ── Etapa 2: Modificadores ────────────────────────────────────────

  Future<void> _addModifier(Recipe r) async {
    await showDialog<void>(
      context: context,
      builder: (_) => _ModifierDialog(
        recipeName: r.name,
        onSave: (name, defQty, minQty, maxQty, delta, order) async {
          await widget.api.adminCreateModifier(
            recipeId: widget.recipeId,
            name: name,
            defaultQty: defQty,
            minQty: minQty,
            maxQty: maxQty,
            priceDeltaUsd: delta,
            displayOrder: order,
          );
        },
      ),
    );
    await _load();
  }

  Future<void> _editModifier(Recipe r, RecipeModifier mod) async {
    await showDialog<void>(
      context: context,
      builder: (_) => _ModifierDialog(
        recipeName: r.name,
        initial: mod,
        onSave: (name, defQty, minQty, maxQty, delta, order) async {
          await widget.api.adminUpdateModifier(
            recipeId: widget.recipeId,
            modifierId: mod.id,
            name: name,
            defaultQty: defQty,
            minQty: minQty,
            maxQty: maxQty,
            priceDeltaUsd: delta,
            displayOrder: order,
          );
        },
      ),
    );
    await _load();
  }

  Future<void> _deleteModifier(Recipe r, RecipeModifier mod) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text('Eliminar modificador'),
        content: Text('¿Eliminar "${mod.name}" de ${r.name}?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Eliminar'),
          ),
        ],
      ),
    );
    if (ok != true) return;
    await widget.api.adminDeleteModifier(
      recipeId: widget.recipeId,
      modifierId: mod.id,
    );
    await _load();
  }

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
          // Etapa 1: Foto del plato (preview + boton subir/cambiar).
          _RecipeImageCard(
            imageUrl: r.imageUrl,
            apiBase: widget.api.client.baseUri.toString(),
            onUpload: _uploadImage,
            busy: _busy,
          ),
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
          // ── Etapa 2: Modificadores (solo platos, no sub-recetas) ─────
          if (!r.isSubRecipe) ...[
            const SizedBox(height: 24),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text('Modificadores (${r.modifiers.length})',
                    style: Theme.of(context).textTheme.titleLarge),
                ElevatedButton.icon(
                  onPressed: () => _addModifier(r),
                  icon: const Icon(Icons.add),
                  label: const Text('Agregar'),
                ),
              ],
            ),
            const SizedBox(height: 4),
            Text(
              'Opciones que el cliente puede personalizar (ej. "Extra queso +\$0.50").',
              style: Theme.of(context).textTheme.bodySmall,
            ),
            const SizedBox(height: 12),
            if (r.modifiers.isEmpty)
              const Text('Sin modificadores todavía.')
            else
              ...r.modifiers.map((mod) => Card(
                    child: ListTile(
                      leading: Icon(
                        mod.isActive
                            ? Icons.tune_rounded
                            : Icons.tune_rounded,
                        color: mod.isActive ? null : Colors.grey,
                      ),
                      title: Text(mod.name),
                      subtitle: Text([
                        'Qty: ${mod.minQty}–${mod.maxQty} (def ${mod.defaultQty})',
                        if (mod.priceDeltaUsd != 0)
                          (mod.priceDeltaUsd > 0
                              ? '+\$${mod.priceDeltaUsd.toStringAsFixed(2)}'
                              : '-\$${mod.priceDeltaUsd.abs().toStringAsFixed(2)}'),
                        if (!mod.isActive) '(inactivo)',
                      ].join(' · ')),
                      trailing: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          IconButton(
                            icon: const Icon(Icons.edit_outlined, size: 20),
                            tooltip: 'Editar',
                            onPressed: () => _editModifier(r, mod),
                          ),
                          IconButton(
                            icon: const Icon(Icons.delete_outline, size: 20),
                            tooltip: 'Eliminar',
                            onPressed: () => _deleteModifier(r, mod),
                          ),
                        ],
                      ),
                    ),
                  )),
          // ── Etapa 3: Tags ────────────────────────────────────────────
          if (!r.isSubRecipe) ...[
            const SizedBox(height: 24),
            Text('Tags / Badges',
                style: Theme.of(context).textTheme.titleLarge),
            const SizedBox(height: 4),
            Text(
              'Visible en el menú del cliente como chips de color.',
              style: Theme.of(context).textTheme.bodySmall,
            ),
            const SizedBox(height: 12),
            _TagSelector(
              current: r.tags,
              onSave: (tags) async {
                await widget.api.adminUpdateTags(
                  recipeId: widget.recipeId,
                  tags: tags,
                );
                await _load();
              },
            ),
          ],
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


/// Etapa 1: card que muestra la foto del plato + boton subir/cambiar.
class _RecipeImageCard extends StatelessWidget {
  final String? imageUrl;
  final String apiBase;
  final VoidCallback onUpload;
  final bool busy;

  const _RecipeImageCard({
    required this.imageUrl,
    required this.apiBase,
    required this.onUpload,
    required this.busy,
  });

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    final hasImage = imageUrl != null && imageUrl!.isNotEmpty;
    // imageUrl viene como path absoluto del API (ej. /api/uploads/{chef}/recipes/x.png).
    // Para renderizar desde admin_web necesitamos prefijo absoluto.
    final fullUrl = hasImage
        ? (imageUrl!.startsWith('http')
            ? imageUrl!
            : '${apiBase.replaceAll(RegExp(r'/$'), '')}$imageUrl')
        : null;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text('Foto del plato',
                style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 12),
            if (hasImage)
              ClipRRect(
                borderRadius: BorderRadius.circular(12),
                child: AspectRatio(
                  aspectRatio: 16 / 9,
                  child: Image.network(
                    fullUrl!,
                    fit: BoxFit.cover,
                    errorBuilder: (_, __, ___) => Container(
                      color: palette.bg,
                      child: const Center(child: Icon(Icons.broken_image)),
                    ),
                  ),
                ),
              )
            else
              Container(
                height: 180,
                decoration: BoxDecoration(
                  color: palette.bg,
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Center(
                  child: Icon(Icons.image_outlined,
                      size: 64, color: palette.inkMuted),
                ),
              ),
            const SizedBox(height: 12),
            ElevatedButton.icon(
              onPressed: busy ? null : onUpload,
              icon: const Icon(Icons.upload),
              label: Text(hasImage ? 'Cambiar foto' : 'Subir foto'),
            ),
          ],
        ),
      ),
    );
  }
}

// =====================================================================
// Etapa 2: Dialogo de creacion / edicion de modificador
// =====================================================================

typedef _ModifierSaveCallback = Future<void> Function(
  String name, int defaultQty, int minQty, int maxQty,
  double priceDeltaUsd, int displayOrder);

class _ModifierDialog extends StatefulWidget {
  final String recipeName;
  final RecipeModifier? initial;
  final _ModifierSaveCallback onSave;

  const _ModifierDialog({
    required this.recipeName,
    required this.onSave,
    this.initial,
  });

  @override
  State<_ModifierDialog> createState() => _ModifierDialogState();
}

class _ModifierDialogState extends State<_ModifierDialog> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _nameCtrl;
  late final TextEditingController _deltaCtrl;
  late final TextEditingController _minCtrl;
  late final TextEditingController _maxCtrl;
  late final TextEditingController _defCtrl;
  late final TextEditingController _orderCtrl;
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    final m = widget.initial;
    _nameCtrl  = TextEditingController(text: m?.name ?? '');
    _deltaCtrl = TextEditingController(text: m?.priceDeltaUsd.toStringAsFixed(2) ?? '0.00');
    _minCtrl   = TextEditingController(text: '${m?.minQty ?? 0}');
    _maxCtrl   = TextEditingController(text: '${m?.maxQty ?? 1}');
    _defCtrl   = TextEditingController(text: '${m?.defaultQty ?? 0}');
    _orderCtrl = TextEditingController(text: '${m?.displayOrder ?? 0}');
  }

  @override
  void dispose() {
    for (final c in [_nameCtrl, _deltaCtrl, _minCtrl, _maxCtrl, _defCtrl, _orderCtrl]) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _saving = true);
    try {
      await widget.onSave(
        _nameCtrl.text.trim(),
        int.parse(_defCtrl.text.trim()),
        int.parse(_minCtrl.text.trim()),
        int.parse(_maxCtrl.text.trim()),
        double.parse(_deltaCtrl.text.trim()),
        int.parse(_orderCtrl.text.trim()),
      );
      if (mounted) Navigator.pop(context);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isEdit = widget.initial != null;
    return AlertDialog(
      title: Text(isEdit ? 'Editar modificador' : 'Nuevo modificador'),
      content: SizedBox(
        width: 380,
        child: Form(
          key: _formKey,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextFormField(
                controller: _nameCtrl,
                decoration: const InputDecoration(
                  labelText: 'Nombre',
                  hintText: 'Ej: Extra queso, Sin cebolla, Aguacate',
                ),
                validator: (v) =>
                    v == null || v.trim().isEmpty ? 'Requerido' : null,
                textCapitalization: TextCapitalization.sentences,
              ),
              const SizedBox(height: 12),
              Row(children: [
                Expanded(
                  child: TextFormField(
                    controller: _deltaCtrl,
                    decoration: const InputDecoration(
                      labelText: 'Delta precio (USD)',
                      prefixText: r'$ ',
                      hintText: '0.50 o -0.30',
                    ),
                    keyboardType: const TextInputType.numberWithOptions(
                        decimal: true, signed: true),
                    validator: (v) =>
                        double.tryParse(v ?? '') == null ? 'Numero requerido' : null,
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: TextFormField(
                    controller: _orderCtrl,
                    decoration: const InputDecoration(labelText: 'Orden visual'),
                    keyboardType: TextInputType.number,
                    validator: (v) =>
                        int.tryParse(v ?? '') == null ? 'Entero requerido' : null,
                  ),
                ),
              ]),
              const SizedBox(height: 12),
              Row(children: [
                Expanded(
                  child: TextFormField(
                    controller: _minCtrl,
                    decoration: const InputDecoration(labelText: 'Cant. min'),
                    keyboardType: TextInputType.number,
                    validator: (v) {
                      final n = int.tryParse(v ?? '');
                      if (n == null) return 'Entero';
                      if (n < 0) return '>= 0';
                      return null;
                    },
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: TextFormField(
                    controller: _maxCtrl,
                    decoration: const InputDecoration(labelText: 'Cant. max'),
                    keyboardType: TextInputType.number,
                    validator: (v) {
                      final max = int.tryParse(v ?? '');
                      final min = int.tryParse(_minCtrl.text);
                      if (max == null) return 'Entero';
                      if (min != null && max < min) return '>= min';
                      return null;
                    },
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: TextFormField(
                    controller: _defCtrl,
                    decoration: const InputDecoration(labelText: 'Cant. def.'),
                    keyboardType: TextInputType.number,
                    validator: (v) {
                      final def = int.tryParse(v ?? '');
                      final min = int.tryParse(_minCtrl.text) ?? 0;
                      final max = int.tryParse(_maxCtrl.text) ?? 1;
                      if (def == null) return 'Entero';
                      if (def < min || def > max) return '$min–$max';
                      return null;
                    },
                  ),
                ),
              ]),
            ],
          ),
        ),
      ),
      actions: [
        TextButton(
          onPressed: _saving ? null : () => Navigator.pop(context),
          child: const Text('Cancelar'),
        ),
        FilledButton(
          onPressed: _saving ? null : _submit,
          child: _saving
              ? const SizedBox(
                  width: 18, height: 18,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : Text(isEdit ? 'Guardar' : 'Crear'),
        ),
      ],
    );
  }
}

// ── Etapa 3: selector de tags ─────────────────────────────────────────────────

/// Mapa de tags permitidos → (icono, label, color).
const Map<String, (String, String, Color)> _allowedTags = {
  'vegano':      ('🌿', 'Vegano',      Color(0xFF2E7D32)),
  'vegetariano': ('🥗', 'Vegetariano', Color(0xFF558B2F)),
  'picante':     ('🌶', 'Picante',     Color(0xFFB71C1C)),
  'sin_gluten':  ('🌾', 'Sin gluten',  Color(0xFF1565C0)),
  'sin_lactosa': ('🥛', 'Sin lactosa', Color(0xFF6A1B9A)),
  'nuevo':       ('✨', 'Nuevo',        Color(0xFFE65100)),
  'popular':     ('🔥', 'Popular',     Color(0xFFAD1457)),
};

/// Widget de chips multi-selección para asignar tags al plato.
class _TagSelector extends StatefulWidget {
  final List<String> current;
  final Future<void> Function(List<String> tags) onSave;

  const _TagSelector({required this.current, required this.onSave});

  @override
  State<_TagSelector> createState() => _TagSelectorState();
}

class _TagSelectorState extends State<_TagSelector> {
  late Set<String> _selected;
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    _selected = Set<String>.from(widget.current);
  }

  @override
  void didUpdateWidget(_TagSelector old) {
    super.didUpdateWidget(old);
    // Sincronizar si la receta se recargó.
    if (old.current != widget.current) {
      _selected = Set<String>.from(widget.current);
    }
  }

  Future<void> _toggle(String tag) async {
    final next = Set<String>.from(_selected);
    if (next.contains(tag)) {
      next.remove(tag);
    } else {
      next.add(tag);
    }
    setState(() {
      _selected = next;
      _saving = true;
    });
    try {
      await widget.onSave(next.toList());
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: [
        for (final entry in _allowedTags.entries)
          FilterChip(
            label: Text('${entry.value.$1} ${entry.value.$2}'),
            selected: _selected.contains(entry.key),
            onSelected: _saving ? null : (_) => _toggle(entry.key),
            selectedColor: entry.value.$3.withValues(alpha: 0.2),
            checkmarkColor: entry.value.$3,
            labelStyle: TextStyle(
              color: _selected.contains(entry.key)
                  ? entry.value.$3
                  : null,
              fontWeight: _selected.contains(entry.key)
                  ? FontWeight.w600
                  : FontWeight.normal,
            ),
            side: _selected.contains(entry.key)
                ? BorderSide(color: entry.value.$3, width: 1.5)
                : null,
          ),
      ],
    );
  }
}
