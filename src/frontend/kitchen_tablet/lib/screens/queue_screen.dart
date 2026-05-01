import 'dart:async';

import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';
import 'package:intl/intl.dart';

class QueueScreen extends StatefulWidget {
  final HcpApi api;
  final Future<void> Function() onLogout;
  const QueueScreen({super.key, required this.api, required this.onLogout});

  @override
  State<QueueScreen> createState() => _QueueScreenState();
}

class _QueueScreenState extends State<QueueScreen> {
  List<KitchenQueueItem> _items = const [];
  bool _loading = true;
  String? _error;
  Timer? _poll;

  @override
  void initState() {
    super.initState();
    _refresh();
    _poll = Timer.periodic(const Duration(seconds: 15), (_) => _refresh());
  }

  @override
  void dispose() {
    _poll?.cancel();
    super.dispose();
  }

  Future<void> _refresh() async {
    try {
      final fresh = await widget.api.kitchenQueue();
      if (mounted) {
        setState(() {
          _items = fresh;
          _error = null;
          _loading = false;
        });
      }
    } on ApiException catch (e) {
      if (mounted) setState(() {
        _error = '${e.statusCode} ${e.message}';
        _loading = false;
      });
    } catch (e) {
      if (mounted) setState(() {
        _error = '$e';
        _loading = false;
      });
    }
  }

  Future<void> _start(KitchenQueueItem item) async {
    try {
      await widget.api.startItem(item.orderId, item.orderItemId);
      await _refresh();
    } on ApiException catch (e) {
      _toast('No se pudo iniciar: ${e.message}');
    }
  }

  Future<void> _ready(KitchenQueueItem item) async {
    try {
      await widget.api.markItemReady(item.orderId, item.orderItemId);
      await _refresh();
    } on ApiException catch (e) {
      _toast('No se pudo marcar listo: ${e.message}');
    }
  }

  void _toast(String message) {
    ScaffoldMessenger.of(context)
        .showSnackBar(SnackBar(content: Text(message)));
  }

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    final pending = _items.where((i) => i.kitchenStatus == 'pending').toList();
    final inPrep = _items.where((i) => i.kitchenStatus == 'in_prep').toList();

    return Scaffold(
      backgroundColor: palette.bg,
      appBar: AppBar(
        backgroundColor: palette.bg,
        elevation: 0,
        title: Row(
          children: [
            Text('Cocina',
                style: Theme.of(context).textTheme.displaySmall),
            const SizedBox(width: 16),
            _StatusDot(palette: palette, items: _items),
          ],
        ),
        actions: [
          IconButton(
              tooltip: 'Refrescar',
              onPressed: _refresh,
              icon: const Icon(Icons.refresh)),
          IconButton(
              tooltip: 'Cerrar sesión',
              onPressed: widget.onLogout,
              icon: const Icon(Icons.logout)),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(_error!),
                      const SizedBox(height: 12),
                      ElevatedButton(
                          onPressed: _refresh, child: const Text('Reintentar')),
                    ],
                  ),
                )
              : Padding(
                  padding: const EdgeInsets.all(16),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Expanded(
                        child: _Column(
                          title: 'Por preparar (${pending.length})',
                          accent: palette.accent,
                          items: pending,
                          actionLabel: 'Empezar',
                          onAction: _start,
                        ),
                      ),
                      const SizedBox(width: 16),
                      Expanded(
                        child: _Column(
                          title: 'En preparación (${inPrep.length})',
                          accent: palette.green,
                          items: inPrep,
                          actionLabel: 'Marcar listo',
                          onAction: _ready,
                        ),
                      ),
                    ],
                  ),
                ),
    );
  }
}

class _Column extends StatelessWidget {
  final String title;
  final Color accent;
  final List<KitchenQueueItem> items;
  final String actionLabel;
  final Future<void> Function(KitchenQueueItem) onAction;
  const _Column({
    required this.title,
    required this.accent,
    required this.items,
    required this.actionLabel,
    required this.onAction,
  });

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Container(
          padding: const EdgeInsets.symmetric(vertical: 8, horizontal: 12),
          decoration: BoxDecoration(
            color: accent,
            borderRadius: BorderRadius.circular(12),
          ),
          child: Text(title,
              style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w600)),
        ),
        const SizedBox(height: 12),
        Expanded(
          child: items.isEmpty
              ? Center(
                  child: Text('Sin pedidos', style: TextStyle(color: palette.inkMuted)),
                )
              : ListView.separated(
                  itemCount: items.length,
                  separatorBuilder: (_, __) => const SizedBox(height: 12),
                  itemBuilder: (_, i) =>
                      _ItemCard(item: items[i], actionLabel: actionLabel, onAction: onAction),
                ),
        ),
      ],
    );
  }
}

class _ItemCard extends StatelessWidget {
  final KitchenQueueItem item;
  final String actionLabel;
  final Future<void> Function(KitchenQueueItem) onAction;
  const _ItemCard({
    required this.item,
    required this.actionLabel,
    required this.onAction,
  });

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    final scheduled = item.scheduledFor != null
        ? '· programado ${DateFormat('HH:mm').format(item.scheduledFor!.toLocal())}'
        : '';
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text('${item.quantity} × ${item.dishNameSnapshot}',
                    style: Theme.of(context).textTheme.titleLarge),
                Text(item.orderNumber,
                    style: Theme.of(context).textTheme.labelMedium),
              ],
            ),
            const SizedBox(height: 4),
            Text(
              '${DateFormat('HH:mm').format(item.priorityTime.toLocal())} '
              '${item.prepTimeMinutes} min $scheduled',
              style: Theme.of(context).textTheme.bodySmall,
            ),
            if (item.itemNotes != null && item.itemNotes!.isNotEmpty) ...[
              const SizedBox(height: 8),
              Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: palette.sun.withValues(alpha: 0.4),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text('Nota: ${item.itemNotes!}',
                    style: Theme.of(context).textTheme.bodyMedium),
              ),
            ],
            if (item.procedureMarkdown != null &&
                item.procedureMarkdown!.isNotEmpty) ...[
              const SizedBox(height: 8),
              ExpansionTile(
                tilePadding: EdgeInsets.zero,
                title: const Text('Procedimiento'),
                children: [
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                    child: Text(item.procedureMarkdown!,
                        style: Theme.of(context).textTheme.bodyMedium),
                  ),
                ],
              ),
            ],
            const SizedBox(height: 12),
            Align(
              alignment: Alignment.centerRight,
              child: ElevatedButton(
                onPressed: () => onAction(item),
                child: Text(actionLabel),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _StatusDot extends StatelessWidget {
  final HcpPalette palette;
  final List<KitchenQueueItem> items;
  const _StatusDot({required this.palette, required this.items});

  @override
  Widget build(BuildContext context) {
    final inPrep = items.where((i) => i.kitchenStatus == 'in_prep').length;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      decoration: BoxDecoration(
        color: items.isEmpty ? palette.greenSoft : palette.sun,
        borderRadius: BorderRadius.circular(999),
      ),
      child: Text(
        items.isEmpty
            ? 'Sin actividad'
            : '${items.length} ítem(s) · $inPrep en horno',
        style: Theme.of(context).textTheme.labelMedium,
      ),
    );
  }
}
