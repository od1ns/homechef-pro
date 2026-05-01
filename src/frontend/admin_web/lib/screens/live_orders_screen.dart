import 'dart:async';

import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';
import 'package:intl/intl.dart';

import 'order_detail_drawer.dart';

/// A2 Live Orders — kanban with 4 columns grouped from the FSM:
///   Entrantes:  pending_payment, payment_verifying
///   En cocina:  paid, in_preparation
///   Listas:     ready
///   En camino:  in_delivery
/// Polls /api/admin/orders every 15 seconds.
class LiveOrdersScreen extends StatefulWidget {
  final HcpApi api;
  const LiveOrdersScreen({super.key, required this.api});

  @override
  State<LiveOrdersScreen> createState() => _LiveOrdersScreenState();
}

class _LiveOrdersScreenState extends State<LiveOrdersScreen> {
  List<OrderSummary> _orders = const [];
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
      final fresh = await widget.api.adminActiveOrders();
      if (mounted) {
        setState(() {
          _orders = fresh;
          _error = null;
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

  Future<void> _openOrder(OrderSummary summary) async {
    await showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      builder: (_) => DraggableScrollableSheet(
        expand: false,
        initialChildSize: 0.85,
        maxChildSize: 0.95,
        builder: (_, controller) => OrderDetailDrawer(
          api: widget.api,
          orderId: summary.id,
          scrollController: controller,
          onChanged: _refresh,
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) return const Center(child: CircularProgressIndicator());
    if (_error != null) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(_error!),
            const SizedBox(height: 12),
            ElevatedButton(onPressed: _refresh, child: const Text('Reintentar')),
          ],
        ),
      );
    }

    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    final incoming = _orders.where((o) =>
        o.status == 'pending_payment' || o.status == 'payment_verifying').toList();
    final inKitchen = _orders.where((o) =>
        o.status == 'paid' || o.status == 'in_preparation').toList();
    final ready = _orders.where((o) => o.status == 'ready').toList();
    final outForDelivery = _orders.where((o) => o.status == 'in_delivery').toList();

    return Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('Órdenes en vivo',
                  style: Theme.of(context).textTheme.displaySmall),
              IconButton(
                tooltip: 'Refrescar',
                icon: const Icon(Icons.refresh),
                onPressed: _refresh,
              ),
            ],
          ),
          Text('${_orders.length} órdenes activas · refresca cada 15s',
              style: Theme.of(context).textTheme.bodyMedium),
          const SizedBox(height: 16),
          Expanded(
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                _buildColumn('Entrantes', palette.sun, incoming),
                const SizedBox(width: 16),
                _buildColumn('En cocina', palette.accent, inKitchen),
                const SizedBox(width: 16),
                _buildColumn('Listas', palette.green, ready),
                const SizedBox(width: 16),
                _buildColumn('En camino', palette.accentDark, outForDelivery),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildColumn(String title, Color accent, List<OrderSummary> orders) {
    return Expanded(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
            decoration:
                BoxDecoration(color: accent, borderRadius: BorderRadius.circular(12)),
            child: Text('$title (${orders.length})',
                style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w600)),
          ),
          const SizedBox(height: 12),
          Expanded(
            child: orders.isEmpty
                ? Center(
                    child: Text('—',
                        style: Theme.of(context).textTheme.bodyMedium))
                : ListView.separated(
                    itemCount: orders.length,
                    separatorBuilder: (_, __) => const SizedBox(height: 8),
                    itemBuilder: (_, i) => _OrderCard(
                      order: orders[i],
                      onTap: () => _openOrder(orders[i]),
                    ),
                  ),
          ),
        ],
      ),
    );
  }
}

class _OrderCard extends StatelessWidget {
  final OrderSummary order;
  final VoidCallback onTap;
  const _OrderCard({required this.order, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(16),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(order.orderNumber,
                      style: Theme.of(context).textTheme.titleMedium),
                  Text(DateFormat('HH:mm').format(order.createdAt.toLocal()),
                      style: Theme.of(context).textTheme.labelMedium),
                ],
              ),
              const SizedBox(height: 4),
              Text(order.customerName,
                  style: Theme.of(context).textTheme.bodyMedium,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis),
              const SizedBox(height: 8),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text('${order.itemCount} ítem${order.itemCount == 1 ? '' : 's'}',
                      style: Theme.of(context).textTheme.bodySmall),
                  Text('\$${order.totalUsd.toStringAsFixed(2)}',
                      style: Theme.of(context).textTheme.labelMedium),
                ],
              ),
              const SizedBox(height: 4),
              Row(
                children: [
                  Icon(
                    order.deliveryType == 'pickup'
                        ? Icons.shopping_bag_outlined
                        : Icons.delivery_dining,
                    size: 14,
                  ),
                  const SizedBox(width: 4),
                  Text(
                    order.deliveryType == 'pickup' ? 'Retiro' : 'Delivery',
                    style: Theme.of(context).textTheme.bodySmall,
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}
