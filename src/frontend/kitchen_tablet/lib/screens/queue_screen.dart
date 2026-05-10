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

class _QueueScreenState extends State<QueueScreen>
    with SingleTickerProviderStateMixin {
  late final TabController _tabs;

  // ---- Cola activa (items por plato) ----
  List<KitchenQueueItem> _items = const [];

  // ---- Pedidos programados ----
  List<OrderSummary> _scheduled = const [];

  bool _loading = true;
  String? _error;
  Timer? _poll;

  @override
  void initState() {
    super.initState();
    _tabs = TabController(length: 2, vsync: this);
    _refresh();
    _poll = Timer.periodic(const Duration(seconds: 15), (_) => _refresh());
  }

  @override
  void dispose() {
    _poll?.cancel();
    _tabs.dispose();
    super.dispose();
  }

  Future<void> _refresh() async {
    try {
      final results = await Future.wait([
        widget.api.kitchenQueue(),
        widget.api.kitchenScheduledOrders(),
      ]);
      if (mounted) {
        setState(() {
          _items = results[0] as List<KitchenQueueItem>;
          _scheduled = results[1] as List<OrderSummary>;
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
        bottom: TabBar(
          controller: _tabs,
          tabs: [
            const Tab(icon: Icon(Icons.restaurant), text: 'En curso'),
            Tab(
              icon: Stack(
                clipBehavior: Clip.none,
                children: [
                  const Icon(Icons.schedule),
                  if (_scheduled.isNotEmpty)
                    Positioned(
                      top: -4,
                      right: -6,
                      child: Container(
                        width: 14,
                        height: 14,
                        decoration: BoxDecoration(
                          color: palette.accent,
                          shape: BoxShape.circle,
                        ),
                        child: Center(
                          child: Text(
                            '${_scheduled.length}',
                            style: const TextStyle(
                                color: Colors.white, fontSize: 9),
                          ),
                        ),
                      ),
                    ),
                ],
              ),
              text: 'Programados',
            ),
          ],
        ),
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
              : TabBarView(
                  controller: _tabs,
                  children: [
                    // ---- Pestaña 1: cola activa ----
                    Padding(
                      padding: const EdgeInsets.all(16),
                      child: Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Expanded(
                            child: _ActiveColumn(
                              title: 'Por preparar (${pending.length})',
                              accent: palette.accent,
                              items: pending,
                              actionLabel: 'Empezar',
                              onAction: _start,
                            ),
                          ),
                          const SizedBox(width: 16),
                          Expanded(
                            child: _ActiveColumn(
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

                    // ---- Pestaña 2: pedidos programados ----
                    _ScheduledTab(
                      orders: _scheduled,
                      palette: palette,
                    ),
                  ],
                ),
    );
  }
}

// ======================================================================
// Pestaña 1: cola activa
// ======================================================================

class _ActiveColumn extends StatelessWidget {
  final String title;
  final Color accent;
  final List<KitchenQueueItem> items;
  final String actionLabel;
  final Future<void> Function(KitchenQueueItem) onAction;
  const _ActiveColumn({
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

// ======================================================================
// Pestaña 2: pedidos programados
// ======================================================================

class _ScheduledTab extends StatelessWidget {
  final List<OrderSummary> orders;
  final HcpPalette palette;
  const _ScheduledTab({required this.orders, required this.palette});

  @override
  Widget build(BuildContext context) {
    if (orders.isEmpty) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.event_available, size: 64, color: palette.inkMuted),
            const SizedBox(height: 16),
            Text('Sin pedidos programados',
                style: Theme.of(context)
                    .textTheme
                    .titleMedium
                    ?.copyWith(color: palette.inkMuted)),
          ],
        ),
      );
    }

    return ListView.separated(
      padding: const EdgeInsets.all(16),
      itemCount: orders.length,
      separatorBuilder: (_, __) => const SizedBox(height: 12),
      itemBuilder: (_, i) => _ScheduledCard(order: orders[i], palette: palette),
    );
  }
}

class _ScheduledCard extends StatelessWidget {
  final OrderSummary order;
  final HcpPalette palette;
  const _ScheduledCard({required this.order, required this.palette});

  String _timeUntil(DateTime scheduledFor) {
    final diff = scheduledFor.toLocal().difference(DateTime.now());
    if (diff.isNegative) return 'vencido';
    if (diff.inMinutes < 60) return 'en ${diff.inMinutes} min';
    final h = diff.inHours;
    final m = diff.inMinutes % 60;
    return m == 0 ? 'en ${h}h' : 'en ${h}h ${m}min';
  }

  Color _urgencyColor(DateTime scheduledFor) {
    final mins = scheduledFor.toLocal().difference(DateTime.now()).inMinutes;
    if (mins < 0) return palette.red;
    if (mins < 30) return palette.sun;
    return palette.green;
  }

  @override
  Widget build(BuildContext context) {
    final sf = order.scheduledFor!.toLocal();
    final urgency = _urgencyColor(sf);
    final timeUntil = _timeUntil(sf);

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(order.orderNumber,
                    style: Theme.of(context).textTheme.titleLarge),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                  decoration: BoxDecoration(
                    color: urgency.withValues(alpha: 0.15),
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(color: urgency, width: 1),
                  ),
                  child: Text(timeUntil,
                      style: TextStyle(
                          color: urgency,
                          fontSize: 12,
                          fontWeight: FontWeight.w700)),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                Icon(Icons.schedule, size: 16, color: palette.inkMuted),
                const SizedBox(width: 6),
                Text(
                  DateFormat('EEEE d MMM, HH:mm', 'es').format(sf),
                  style: Theme.of(context)
                      .textTheme
                      .bodyMedium
                      ?.copyWith(fontWeight: FontWeight.w600),
                ),
              ],
            ),
            const SizedBox(height: 4),
            Row(
              children: [
                Icon(Icons.person_outline, size: 16, color: palette.inkMuted),
                const SizedBox(width: 6),
                Text(order.customerName,
                    style: Theme.of(context).textTheme.bodySmall),
                const SizedBox(width: 16),
                Icon(Icons.shopping_bag_outlined,
                    size: 16, color: palette.inkMuted),
                const SizedBox(width: 4),
                Text('${order.itemCount} ítem(s)',
                    style: Theme.of(context).textTheme.bodySmall),
                const Spacer(),
                Text('\$${order.totalUsd.toStringAsFixed(2)}',
                    style: Theme.of(context)
                        .textTheme
                        .labelMedium
                        ?.copyWith(fontWeight: FontWeight.w700)),
              ],
            ),
            const SizedBox(height: 8),
            _StatusBadge(status: order.status, palette: palette),
          ],
        ),
      ),
    );
  }
}

class _StatusBadge extends StatelessWidget {
  final String status;
  final HcpPalette palette;
  const _StatusBadge({required this.status, required this.palette});

  @override
  Widget build(BuildContext context) {
    final (label, color) = switch (status) {
      'pending_payment'   => ('Pago pendiente', palette.sun),
      'payment_verifying' => ('Verificando pago', palette.sun),
      'paid'              => ('Pagado · listo para cocinar', palette.green),
      'in_preparation'    => ('En preparación', palette.accent),
      _                   => (status, palette.line),
    };
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.15),
        borderRadius: BorderRadius.circular(6),
      ),
      child: Text(label,
          style: TextStyle(color: color, fontSize: 11, fontWeight: FontWeight.w600)),
    );
  }
}

// ======================================================================
// Indicador de actividad en AppBar
// ======================================================================

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
