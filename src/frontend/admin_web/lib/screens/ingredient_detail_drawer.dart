import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';

class IngredientDetailDrawer extends StatefulWidget {
  final HcpApi api;
  final String ingredientId;
  final ScrollController scrollController;
  final Future<void> Function() onChanged;
  const IngredientDetailDrawer({
    super.key,
    required this.api,
    required this.ingredientId,
    required this.scrollController,
    required this.onChanged,
  });

  @override
  State<IngredientDetailDrawer> createState() => _IngredientDetailDrawerState();
}

class _IngredientDetailDrawerState extends State<IngredientDetailDrawer> {
  IngredientDetail? _detail;
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
      final d = await widget.api.adminGetIngredient(widget.ingredientId);
      if (mounted) {
        setState(() {
          _detail = d;
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

  Future<void> _editThresholds() async {
    final d = _detail!;
    final reorder = TextEditingController(text: d.reorderPointUseUnit.toString());
    final minimum = TextEditingController(text: d.minimumStockUseUnit.toString());
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Editar umbrales'),
        content: SizedBox(
          width: 320,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              TextField(
                controller: reorder,
                decoration:
                    const InputDecoration(labelText: 'Punto de reorden'),
                keyboardType:
                    const TextInputType.numberWithOptions(decimal: true),
              ),
              const SizedBox(height: 8),
              TextField(
                controller: minimum,
                decoration:
                    const InputDecoration(labelText: 'Stock mínimo'),
                keyboardType:
                    const TextInputType.numberWithOptions(decimal: true),
              ),
            ],
          ),
        ),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx, false),
              child: const Text('Cancelar')),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Guardar'),
          ),
        ],
      ),
    );
    if (ok == true) {
      try {
        await widget.api.adminUpdateThresholds(
          ingredientId: widget.ingredientId,
          reorderPointUseUnit: double.tryParse(reorder.text.trim()) ?? 0,
          minimumStockUseUnit: double.tryParse(minimum.text.trim()) ?? 0,
        );
        await widget.onChanged();
        await _load();
      } on ApiException catch (e) {
        _toast(e.message);
      }
    }
  }

  Future<void> _addPresentation() async {
    final name = TextEditingController();
    final qty = TextEditingController();
    final conv = TextEditingController();
    String unit = 'kg';
    String? error;
    final created = await showDialog<bool>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setState) => AlertDialog(
          title: const Text('Nueva presentación'),
          content: SizedBox(
            width: 420,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                TextField(
                    controller: name,
                    decoration: const InputDecoration(
                        labelText: 'Nombre (Saco 50kg, Caja 12u, …)')),
                const SizedBox(height: 12),
                Row(children: [
                  Expanded(
                    child: DropdownButtonFormField<String>(
                      initialValue: unit,
                      decoration: const InputDecoration(labelText: 'Unidad de compra'),
                      items: const [
                        DropdownMenuItem(value: 'kg', child: Text('kg')),
                        DropdownMenuItem(value: 'g', child: Text('g')),
                        DropdownMenuItem(value: 'l', child: Text('l')),
                        DropdownMenuItem(value: 'ml', child: Text('ml')),
                        DropdownMenuItem(value: 'unit', child: Text('unidad')),
                        DropdownMenuItem(value: 'box', child: Text('caja')),
                        DropdownMenuItem(value: 'sack', child: Text('saco')),
                        DropdownMenuItem(value: 'bag', child: Text('bolsa')),
                        DropdownMenuItem(value: 'bottle', child: Text('botella')),
                        DropdownMenuItem(value: 'pack', child: Text('pack')),
                      ],
                      onChanged: (v) => setState(() => unit = v ?? 'kg'),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: TextField(
                      controller: qty,
                      decoration: const InputDecoration(labelText: 'Cantidad'),
                      keyboardType: const TextInputType.numberWithOptions(decimal: true),
                    ),
                  ),
                ]),
                const SizedBox(height: 12),
                TextField(
                  controller: conv,
                  decoration: InputDecoration(
                    labelText: 'Conversión a ${_detail!.useUnit}',
                    helperText:
                        '1 ${unit} = X ${_detail!.useUnit}. Ej: 1 kg = 1000 g.',
                  ),
                  keyboardType: const TextInputType.numberWithOptions(decimal: true),
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
                child: const Text('Cancelar')),
            ElevatedButton(
              onPressed: () async {
                try {
                  await widget.api.adminAddPresentation(
                    ingredientId: widget.ingredientId,
                    name: name.text.trim(),
                    purchaseUnit: unit,
                    purchaseQuantity:
                        double.tryParse(qty.text.trim()) ?? 0,
                    conversionToUseUnit:
                        double.tryParse(conv.text.trim()) ?? 0,
                  );
                  if (ctx.mounted) Navigator.pop(ctx, true);
                } on ApiException catch (e) {
                  setState(() => error = e.message);
                }
              },
              child: const Text('Agregar'),
            ),
          ],
        ),
      ),
    );
    if (created == true) {
      await widget.onChanged();
      await _load();
    }
  }

  Future<void> _recordPurchase(IngredientPresentation p) async {
    final qty = TextEditingController();
    final price = TextEditingController(
        text: p.lastPurchasePriceUsd?.toStringAsFixed(2) ?? '');
    final supplier = TextEditingController();
    String? error;
    final created = await showDialog<bool>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setState) => AlertDialog(
          title: Text('Registrar compra · ${p.name}'),
          content: SizedBox(
            width: 420,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                TextField(
                  controller: qty,
                  decoration: const InputDecoration(
                      labelText: '¿Cuántas presentaciones?'),
                  keyboardType:
                      const TextInputType.numberWithOptions(decimal: true),
                ),
                const SizedBox(height: 8),
                TextField(
                  controller: price,
                  decoration: const InputDecoration(
                      labelText: 'Precio USD por presentación',
                      prefixText: r'$ '),
                  keyboardType:
                      const TextInputType.numberWithOptions(decimal: true),
                ),
                const SizedBox(height: 8),
                TextField(
                  controller: supplier,
                  decoration:
                      const InputDecoration(labelText: 'Proveedor (opcional)'),
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
                child: const Text('Cancelar')),
            ElevatedButton(
              onPressed: () async {
                try {
                  await widget.api.adminRecordPurchase(
                    ingredientId: widget.ingredientId,
                    presentationId: p.id,
                    quantityPurchased:
                        double.tryParse(qty.text.trim()) ?? 0,
                    unitPriceUsd:
                        double.tryParse(price.text.trim()) ?? 0,
                    supplier: supplier.text.trim().isEmpty
                        ? null
                        : supplier.text.trim(),
                  );
                  if (ctx.mounted) Navigator.pop(ctx, true);
                } on ApiException catch (e) {
                  setState(() => error = e.message);
                }
              },
              child: const Text('Registrar'),
            ),
          ],
        ),
      ),
    );
    if (created == true) {
      await widget.onChanged();
      await _load();
    }
  }

  Future<void> _recordWaste() async {
    final d = _detail!;
    final qty = TextEditingController();
    final notes = TextEditingController();
    String reason = 'spoiled';
    String? error;
    final created = await showDialog<bool>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setState) => AlertDialog(
          title: Text('Registrar merma · ${d.name}'),
          content: SizedBox(
            width: 420,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                    'Stock actual: ${d.currentStockUseUnit.toStringAsFixed(2)} ${d.useUnit}',
                    style: Theme.of(ctx).textTheme.bodySmall),
                const SizedBox(height: 12),
                TextField(
                  controller: qty,
                  decoration: InputDecoration(
                      labelText: 'Cantidad perdida (${d.useUnit})'),
                  keyboardType:
                      const TextInputType.numberWithOptions(decimal: true),
                ),
                const SizedBox(height: 12),
                DropdownButtonFormField<String>(
                  initialValue: reason,
                  decoration: const InputDecoration(labelText: 'Motivo'),
                  items: const [
                    DropdownMenuItem(value: 'spoiled', child: Text('Echado a perder')),
                    DropdownMenuItem(value: 'burnt', child: Text('Quemado')),
                    DropdownMenuItem(value: 'dropped', child: Text('Caído')),
                    DropdownMenuItem(value: 'expired', child: Text('Vencido')),
                    DropdownMenuItem(value: 'over_prepped', child: Text('Sobrante de prep')),
                    DropdownMenuItem(value: 'theft', child: Text('Robo')),
                    DropdownMenuItem(value: 'other', child: Text('Otro')),
                  ],
                  onChanged: (v) => setState(() => reason = v ?? 'spoiled'),
                ),
                const SizedBox(height: 8),
                TextField(
                  controller: notes,
                  decoration: const InputDecoration(labelText: 'Notas (opcional)'),
                  maxLines: 2,
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
                child: const Text('Cancelar')),
            ElevatedButton(
              onPressed: () async {
                try {
                  await widget.api.adminRecordWaste(
                    ingredientId: widget.ingredientId,
                    quantityUseUnit: double.tryParse(qty.text.trim()) ?? 0,
                    reason: reason,
                    notes: notes.text.trim().isEmpty
                        ? null
                        : notes.text.trim(),
                  );
                  if (ctx.mounted) Navigator.pop(ctx, true);
                } on ApiException catch (e) {
                  setState(() => error = e.message);
                }
              },
              child: const Text('Registrar'),
            ),
          ],
        ),
      ),
    );
    if (created == true) {
      await widget.onChanged();
      await _load();
    }
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
    if (_error != null) return Center(child: Text(_error!));
    final d = _detail!;

    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 24),
      child: ListView(
        controller: widget.scrollController,
        children: [
          const SizedBox(height: 12),
          Center(
            child: Container(
              width: 40,
              height: 4,
              decoration: BoxDecoration(
                color: palette.line,
                borderRadius: BorderRadius.circular(2),
              ),
            ),
          ),
          const SizedBox(height: 12),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(d.name, style: Theme.of(context).textTheme.displaySmall),
              if (!d.isActive)
                const Chip(label: Text('Inactivo')),
            ],
          ),
          if (d.description != null)
            Text(d.description!,
                style: Theme.of(context).textTheme.bodyMedium),
          const Divider(height: 32),
          Wrap(
            spacing: 24,
            runSpacing: 12,
            children: [
              _Kpi(
                  label: 'Stock actual',
                  value: '${d.currentStockUseUnit.toStringAsFixed(2)} ${d.useUnit}',
                  color: d.isOutOfStock
                      ? palette.red
                      : d.isBelowReorderPoint
                          ? palette.sun
                          : palette.green),
              _Kpi(
                  label: 'Punto de reorden',
                  value: '${d.reorderPointUseUnit.toStringAsFixed(2)} ${d.useUnit}'),
              _Kpi(
                  label: 'Stock mínimo',
                  value: '${d.minimumStockUseUnit.toStringAsFixed(2)} ${d.useUnit}'),
              _Kpi(
                  label: 'Avg cost',
                  value: '\$${d.avgCostPerUseUnitUsd.toStringAsFixed(6)}/${d.useUnit}'),
            ],
          ),
          const SizedBox(height: 16),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              OutlinedButton.icon(
                  onPressed: _editThresholds,
                  icon: const Icon(Icons.edit, size: 16),
                  label: const Text('Editar umbrales')),
              OutlinedButton.icon(
                  onPressed: d.currentStockUseUnit > 0 ? _recordWaste : null,
                  icon: Icon(Icons.delete_sweep_outlined,
                      size: 16, color: palette.red),
                  style: OutlinedButton.styleFrom(foregroundColor: palette.red),
                  label: const Text('Registrar merma')),
            ],
          ),
          const Divider(height: 32),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('Presentaciones (${d.presentations.length})',
                  style: Theme.of(context).textTheme.titleLarge),
              ElevatedButton.icon(
                onPressed: _addPresentation,
                icon: const Icon(Icons.add),
                label: const Text('Agregar'),
              ),
            ],
          ),
          const SizedBox(height: 8),
          if (d.presentations.isEmpty)
            const Text('Aún no hay presentaciones — agrega al menos una para registrar compras.')
          else
            ...d.presentations.map((p) => Card(
                  child: ListTile(
                    title: Text(p.name),
                    subtitle: Text(
                      '${p.purchaseQuantity} ${p.purchaseUnit} · '
                      '1 ${p.purchaseUnit} = ${p.conversionToUseUnit} ${d.useUnit}'
                      '${p.lastPurchasePriceUsd != null ? ' · último \$${p.lastPurchasePriceUsd!.toStringAsFixed(2)}' : ''}',
                    ),
                    trailing: TextButton.icon(
                      icon: const Icon(Icons.add_shopping_cart, size: 16),
                      label: const Text('Compra'),
                      onPressed: () => _recordPurchase(p),
                    ),
                  ),
                )),
          const SizedBox(height: 32),
        ],
      ),
    );
  }
}

class _Kpi extends StatelessWidget {
  final String label;
  final String value;
  final Color? color;
  const _Kpi({required this.label, required this.value, this.color});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: Theme.of(context).textTheme.bodySmall),
        Text(value,
            style: Theme.of(context)
                .textTheme
                .titleMedium
                ?.copyWith(color: color)),
      ],
    );
  }
}
