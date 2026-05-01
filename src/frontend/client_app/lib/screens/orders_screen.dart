import 'dart:async';

import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';
import 'package:intl/intl.dart';

import '../app_state.dart';
import '../utils/receipt_share.dart';
import 'payment_screen.dart';

class OrdersScreen extends StatefulWidget {
  final AppState state;
  const OrdersScreen({super.key, required this.state});

  @override
  State<OrdersScreen> createState() => _OrdersScreenState();
}

class _OrdersScreenState extends State<OrdersScreen> with WidgetsBindingObserver {
  Future<List<_TrackedOrder>>? _future;

  /// Cache de la ultima carga, para que el polling silencioso pueda actualizar
  /// la UI sin pasar por un estado de loading.
  List<_TrackedOrder>? _orders;

  Timer? _poll;
  static const _pollInterval = Duration(seconds: 20);

  /// Estados terminales: una vez aca, no hace falta seguir poleando.
  static const _terminal = {'delivered', 'cancelled', 'rejected'};

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _future = _load().then((res) {
      _orders = res;
      _scheduleNextPoll();
      return res;
    });
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _poll?.cancel();
    super.dispose();
  }

  /// Pausamos polling cuando la app esta en background (ahorra red y bateria).
  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.paused || state == AppLifecycleState.hidden) {
      _poll?.cancel();
    } else if (state == AppLifecycleState.resumed) {
      _scheduleNextPoll();
      // Refrescamos al volver, en caso de que algo cambio mientras estabamos afuera.
      _silentReload();
    }
  }

  Future<List<_TrackedOrder>> _load() async {
    final refs = await widget.state.orderStore.read();
    final results = <_TrackedOrder>[];
    for (final ref in refs) {
      try {
        final order = await widget.state.api.trackOrder(ref.orderId);
        results.add(_TrackedOrder(ref, order));
      } catch (_) {
        // Order might have been pruned server-side; skip silently.
      }
    }
    return results;
  }

  Future<void> _refresh() async {
    final fut = _load();
    setState(() => _future = fut);
    final res = await fut;
    if (mounted) {
      _orders = res;
      _scheduleNextPoll();
    }
  }

  /// Refresca sin mostrar el spinner de FutureBuilder. Si hay cambios reales,
  /// reemplaza la lista cacheada y rebuildea.
  Future<void> _silentReload() async {
    try {
      final next = await _load();
      if (!mounted) return;
      setState(() {
        _orders = next;
        _future = Future.value(next);
      });
    } catch (_) {
      // Silencioso: si falla, dejamos los datos viejos en pantalla.
    }
  }

  /// Programa el proximo poll si todavia hay ordenes en estado no-terminal.
  void _scheduleNextPoll() {
    _poll?.cancel();
    final hasActive = (_orders ?? const [])
        .any((t) => !_terminal.contains(t.order.status));
    if (!hasActive) return;
    _poll = Timer(_pollInterval, () async {
      await _silentReload();
      _scheduleNextPoll(); // recursivo, hasta que no queden activas
    });
  }

  @override
  Widget build(BuildContext context) {
    final t = widget.state.strings;
    return RefreshIndicator(
      onRefresh: _refresh,
      child: FutureBuilder<List<_TrackedOrder>>(
        future: _future,
        builder: (context, snap) {
          if (snap.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }
          if (snap.hasError) {
            return Center(child: Text(t.t('common.error')));
          }
          final orders = snap.data ?? const [];
          if (orders.isEmpty) {
            return ListView(
              children: [
                const SizedBox(height: 96),
                Center(
                  child: Column(
                    children: [
                      const Icon(Icons.receipt_long, size: 64),
                      const SizedBox(height: 16),
                      Text(t.t('tab.orders'),
                          style: Theme.of(context).textTheme.headlineMedium),
                      const SizedBox(height: 8),
                      const Text('No tienes pedidos todavía.'),
                    ],
                  ),
                ),
              ],
            );
          }
          return ListView.separated(
            padding: const EdgeInsets.all(16),
            itemCount: orders.length,
            separatorBuilder: (_, __) => const SizedBox(height: 12),
            itemBuilder: (_, i) => _OrderCard(state: widget.state, tracked: orders[i]),
          );
        },
      ),
    );
  }
}

class _TrackedOrder {
  final LocalOrderRef ref;
  final Order order;
  const _TrackedOrder(this.ref, this.order);
}

class _OrderCard extends StatelessWidget {
  final AppState state;
  final _TrackedOrder tracked;
  const _OrderCard({required this.state, required this.tracked});

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    final t = state.strings;
    final order = tracked.order;
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
                _StatusChip(status: order.status, palette: palette),
              ],
            ),
            const SizedBox(height: 4),
            Text(
              DateFormat('dd MMM HH:mm', 'es').format(order.createdAt.toLocal()),
              style: Theme.of(context).textTheme.bodySmall,
            ),
            const Divider(height: 24),
            ...order.items.map((i) => Padding(
                  padding: const EdgeInsets.only(bottom: 4),
                  child: Row(
                    children: [
                      Expanded(child: Text('${i.quantity} × ${i.dishNameSnapshot}')),
                      Text('\$${i.lineTotalUsd.toStringAsFixed(2)}',
                          style: Theme.of(context).textTheme.labelMedium),
                    ],
                  ),
                )),
            const SizedBox(height: 12),
            _Timeline(status: order.status, palette: palette, t: t),
            if (order.totalVesAtOrderTime != null)
              Padding(
                padding: const EdgeInsets.only(top: 12),
                child: Text(
                  'Total: \$${order.totalUsd.toStringAsFixed(2)} '
                  '(Bs ${order.totalVesAtOrderTime!.toStringAsFixed(2)})',
                  style: Theme.of(context).textTheme.titleMedium,
                ),
              )
            else
              Padding(
                padding: const EdgeInsets.only(top: 12),
                child: Text('Total: \$${order.totalUsd.toStringAsFixed(2)}',
                    style: Theme.of(context).textTheme.titleMedium),
              ),
            if (order.status == 'pending_payment')
              Padding(
                padding: const EdgeInsets.only(top: 12),
                child: SizedBox(
                  width: double.infinity,
                  child: ElevatedButton.icon(
                    icon: const Icon(Icons.qr_code_2),
                    label: const Text('Pagar este pedido'),
                    onPressed: () async {
                      // Capturamos el messenger raiz antes de navegar — es estable
                      // independientemente de si esta card se reconstruye.
                      final messenger = ScaffoldMessenger.of(context);
                      await Navigator.of(context).push(
                        MaterialPageRoute(
                          builder: (_) => PaymentScreen(
                            state: state,
                            order: order,
                            onPaymentSubmitted: () {
                              messenger.showSnackBar(
                                const SnackBar(content: Text(
                                  'Comprobante enviado · el chef lo verifica enseguida.'),
                                ),
                              );
                            },
                          ),
                        ),
                      );
                    },
                  ),
                ),
              )
            else if (order.status == 'rejected' || order.status == 'cancelled')
              Padding(
                padding: const EdgeInsets.only(top: 12),
                child: Text(order.cancellationReason ?? '',
                    style: Theme.of(context).textTheme.bodySmall),
              ),
            if (order.status == 'delivered')
              Padding(
                padding: const EdgeInsets.only(top: 8),
                child: Align(
                  alignment: Alignment.centerRight,
                  child: TextButton.icon(
                    icon: const Icon(Icons.receipt_long, size: 18),
                    label: const Text('Recibo / Factura'),
                    onPressed: () async {
                      final messenger = ScaffoldMessenger.of(context);
                      try {
                        final bytes = await state.api.orderReceiptPdf(order.id);
                        await shareReceiptPdf(
                          bytes: bytes,
                          filename: 'recibo-${order.orderNumber}.pdf',
                        );
                      } on ApiException catch (e) {
                        messenger.showSnackBar(SnackBar(content: Text(e.message)));
                      }
                    },
                  ),
                ),
              ),
            if (order.isTerminal)
              Align(
                alignment: Alignment.centerRight,
                child: TextButton.icon(
                  icon: const Icon(Icons.delete_outline, size: 18),
                  label: const Text('Quitar de mi historial'),
                  onPressed: () async {
                    final messenger = ScaffoldMessenger.of(context);
                    await state.forgetOrder(order.id);
                    messenger.showSnackBar(
                        const SnackBar(content: Text('Pedido removido del historial')));
                  },
                ),
              ),
          ],
        ),
      ),
    );
  }
}

class _StatusChip extends StatelessWidget {
  final String status;
  final HcpPalette palette;
  const _StatusChip({required this.status, required this.palette});

  @override
  Widget build(BuildContext context) {
    final (label, bg, fg) = switch (status) {
      'pending_payment'    => ('Pago pendiente', palette.sun, palette.ink),
      'payment_verifying'  => ('Verificando pago', palette.sun, palette.ink),
      'paid'               => ('Pagado', palette.greenSoft, palette.green),
      'in_preparation'     => ('Cocinando', palette.greenSoft, palette.green),
      'ready'              => ('Listo', palette.green, Colors.white),
      'in_delivery'        => ('En camino', palette.accent, Colors.white),
      'delivered'          => ('Entregado', palette.greenSoft, palette.green),
      'cancelled'          => ('Cancelado', palette.redSoft, palette.red),
      'rejected'           => ('Rechazado', palette.redSoft, palette.red),
      _                    => (status, palette.line, palette.ink),
    };
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration:
          BoxDecoration(color: bg, borderRadius: BorderRadius.circular(8)),
      child: Text(label, style: TextStyle(color: fg, fontSize: 12, fontWeight: FontWeight.w600)),
    );
  }
}

class _Timeline extends StatelessWidget {
  final String status;
  final HcpPalette palette;
  final HcpStrings t;
  const _Timeline({required this.status, required this.palette, required this.t});

  static const _steps = ['paid', 'in_preparation', 'ready', 'delivered'];

  int get _currentIndex {
    final i = _steps.indexOf(status);
    if (i >= 0) return i;
    if (status == 'in_delivery') return 2;
    return -1;
  }

  @override
  Widget build(BuildContext context) {
    final labels = [
      t.t('order.received'),
      t.t('order.cooking'),
      t.t('order.ready'),
      t.t('order.delivered'),
    ];
    final cur = _currentIndex;
    return Row(
      children: List.generate(_steps.length, (i) {
        final reached = cur >= i;
        return Expanded(
          child: Column(
            children: [
              Container(
                width: 16,
                height: 16,
                decoration: BoxDecoration(
                  color: reached ? palette.accent : palette.line,
                  shape: BoxShape.circle,
                ),
              ),
              const SizedBox(height: 4),
              Text(labels[i],
                  style: TextStyle(
                      fontSize: 11,
                      color: reached ? palette.ink : palette.inkMuted),
                  textAlign: TextAlign.center),
            ],
          ),
        );
      }),
    );
  }
}
