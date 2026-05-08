import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';
import 'package:intl/intl.dart';

import '../app_state.dart';
import 'auth/login_screen.dart';

class ReviewsScreen extends StatefulWidget {
  final AppState state;
  const ReviewsScreen({super.key, required this.state});

  @override
  State<ReviewsScreen> createState() => _ReviewsScreenState();
}

class _ReviewsScreenState extends State<ReviewsScreen> {
  bool _busy = true;
  String? _error;
  List<MyReview> _reviews = const [];
  List<_PendingReviewTarget> _pending = const [];
  Map<String, RecipeSummary> _dishesById = const {};

  @override
  void initState() {
    super.initState();
    widget.state.addListener(_onStateChanged);
    _load();
  }

  @override
  void dispose() {
    widget.state.removeListener(_onStateChanged);
    super.dispose();
  }

  void _onStateChanged() {
    if (mounted) _load();
  }

  Future<void> _load() async {
    if (!widget.state.isLoggedIn) {
      setState(() {
        _busy = false;
        _reviews = const [];
        _pending = const [];
      });
      return;
    }
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final reviews = await widget.state.api.myReviews();
      // Find delivered orders that don't have a review yet for one of their dishes.
      final localOrders = await widget.state.orderStore.read();
      final pending = <_PendingReviewTarget>[];
      Map<String, RecipeSummary> dishMap = {};
      try {
        final menu = await widget.state.api.menu();
        dishMap = {for (final r in menu) r.id: r};
      } catch (_) {/* menu fetch optional */}

      for (final ref in localOrders) {
        try {
          final order = await widget.state.api.trackOrder(
            ref.orderId,
            accessToken: ref.accessToken,
          );
          if (order.status != 'delivered') {
            continue;
          }
          for (final item in order.items) {
            final already = reviews.any((r) =>
                r.orderId == order.id && r.dishId == item.dishId);
            if (already) {
              continue;
            }
            pending.add(_PendingReviewTarget(
              orderId: order.id,
              orderNumber: order.orderNumber,
              dishId: item.dishId,
              dishName: item.dishNameSnapshot,
              deliveredAt: order.deliveredAt ?? order.createdAt,
            ));
          }
        } catch (_) {/* skip stale orders */}
      }

      if (mounted) {
        setState(() {
          _reviews = reviews;
          _pending = pending;
          _dishesById = dishMap;
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

  Future<void> _loginFlow() async {
    await Navigator.push(context,
        MaterialPageRoute(builder: (_) => CustomerLoginScreen(state: widget.state)));
    if (widget.state.isLoggedIn) await _load();
  }

  Future<void> _writeReview(_PendingReviewTarget target) async {
    final ok = await _showReviewDialog(
      title: 'Reseña · ${target.dishName}',
      onSubmit: (rating, comment) async {
        await widget.state.api.leaveReview(
          orderId: target.orderId,
          dishId: target.dishId,
          rating: rating,
          comment: comment,
        );
      },
    );
    if (ok == true) await _load();
  }

  Future<void> _editReview(MyReview r) async {
    final ok = await _showReviewDialog(
      title: 'Editar reseña',
      initialRating: r.rating,
      initialComment: r.comment,
      onSubmit: (rating, comment) async {
        await widget.state.api.editReview(
          reviewId: r.id,
          rating: rating,
          comment: comment,
        );
      },
    );
    if (ok == true) await _load();
  }

  Future<bool?> _showReviewDialog({
    required String title,
    int initialRating = 5,
    String? initialComment,
    required Future<void> Function(int rating, String? comment) onSubmit,
  }) {
    int rating = initialRating;
    final comment = TextEditingController(text: initialComment ?? '');
    String? error;
    bool busy = false;
    return showDialog<bool>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setState) => AlertDialog(
          title: Text(title),
          content: SizedBox(
            width: 400,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: List.generate(5, (i) {
                    final filled = i < rating;
                    return IconButton(
                      icon: Icon(filled ? Icons.star : Icons.star_border,
                          color: Colors.amber, size: 32),
                      onPressed: () => setState(() => rating = i + 1),
                    );
                  }),
                ),
                const SizedBox(height: 8),
                TextField(
                  controller: comment,
                  decoration: const InputDecoration(
                    labelText: 'Comentario',
                    hintText: 'Cuéntale al chef qué te pareció…',
                  ),
                  maxLines: 3,
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
                onPressed: () => Navigator.pop(ctx, false),
                child: const Text('Cancelar')),
            ElevatedButton(
              onPressed: busy
                  ? null
                  : () async {
                      setState(() => busy = true);
                      try {
                        await onSubmit(
                            rating,
                            comment.text.trim().isEmpty ? null : comment.text.trim());
                        if (ctx.mounted) Navigator.pop(ctx, true);
                      } on ApiException catch (e) {
                        setState(() {
                          error = e.message;
                          busy = false;
                        });
                      }
                    },
              child: busy
                  ? const SizedBox(
                      width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
                  : const Text('Enviar'),
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;

    if (!widget.state.isLoggedIn) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(32),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(Icons.star_outline, size: 64, color: palette.accent),
              const SizedBox(height: 16),
              Text('Tus reseñas',
                  style: Theme.of(context).textTheme.headlineMedium),
              const SizedBox(height: 8),
              const Text(
                'Inicia sesión para dejarle reseña al chef y guardar tu historial.',
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 24),
              ElevatedButton.icon(
                icon: const Icon(Icons.login),
                onPressed: _loginFlow,
                label: const Text('Iniciar sesión / Crear cuenta'),
              ),
            ],
          ),
        ),
      );
    }

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

    return RefreshIndicator(
      onRefresh: _load,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          if (_pending.isNotEmpty) ...[
            Text('Por reseñar',
                style: Theme.of(context).textTheme.titleLarge),
            const SizedBox(height: 8),
            ..._pending.map((p) => Card(
                  child: ListTile(
                    title: Text(p.dishName),
                    subtitle: Text(
                        '${p.orderNumber} · ${DateFormat('dd MMM', 'es').format(p.deliveredAt.toLocal())}'),
                    trailing: ElevatedButton.icon(
                      icon: const Icon(Icons.star, size: 16),
                      label: const Text('Reseñar'),
                      onPressed: () => _writeReview(p),
                    ),
                  ),
                )),
            const SizedBox(height: 24),
          ],
          Text('Mis reseñas',
              style: Theme.of(context).textTheme.titleLarge),
          const SizedBox(height: 8),
          if (_reviews.isEmpty)
            const Padding(
              padding: EdgeInsets.symmetric(vertical: 24),
              child: Text('Aún no has dejado reseñas.', textAlign: TextAlign.center),
            )
          else
            ..._reviews.map((r) => Card(
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Expanded(
                              child: Text(
                                _dishesById[r.dishId]?.name ?? 'Plato',
                                style: Theme.of(context).textTheme.titleMedium,
                              ),
                            ),
                            Row(
                              children: List.generate(5, (i) => Icon(
                                    i < r.rating ? Icons.star : Icons.star_border,
                                    size: 18,
                                    color: Colors.amber,
                                  )),
                            ),
                          ],
                        ),
                        if (r.comment != null && r.comment!.isNotEmpty) ...[
                          const SizedBox(height: 8),
                          Text(r.comment!),
                        ],
                        const SizedBox(height: 4),
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Text(
                                DateFormat('dd MMM yyyy', 'es')
                                    .format(r.createdAt.toLocal()),
                                style: Theme.of(context).textTheme.bodySmall),
                            if (!r.isVisible)
                              Text('Oculta por moderación',
                                  style: TextStyle(
                                      color: palette.red, fontSize: 12)),
                          ],
                        ),
                        Align(
                          alignment: Alignment.centerRight,
                          child: TextButton.icon(
                            icon: const Icon(Icons.edit, size: 16),
                            label: const Text('Editar'),
                            onPressed: () => _editReview(r),
                          ),
                        ),
                      ],
                    ),
                  ),
                )),
        ],
      ),
    );
  }
}

class _PendingReviewTarget {
  final String orderId;
  final String orderNumber;
  final String dishId;
  final String dishName;
  final DateTime deliveredAt;
  const _PendingReviewTarget({
    required this.orderId,
    required this.orderNumber,
    required this.dishId,
    required this.dishName,
    required this.deliveredAt,
  });
}
