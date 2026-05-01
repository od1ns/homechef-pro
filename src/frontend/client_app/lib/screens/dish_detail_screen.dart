import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';
import 'package:intl/intl.dart';

import '../app_state.dart';

class DishDetailScreen extends StatefulWidget {
  final AppState state;
  final RecipeSummary summary;
  const DishDetailScreen({super.key, required this.state, required this.summary});

  @override
  State<DishDetailScreen> createState() => _DishDetailScreenState();
}

class _DishDetailScreenState extends State<DishDetailScreen> {
  late Future<_DishDetailData> _future = _load();
  int _qty = 1;
  final _notesCtrl = TextEditingController();

  Future<_DishDetailData> _load() async {
    final dish = widget.state.api.dish(widget.summary.id);
    final reviews = widget.state.api.dishReviews(widget.summary.id, take: 25);
    return _DishDetailData(await dish, await reviews);
  }

  void _addToCart() {
    widget.state.addToCart(
      widget.summary,
      quantity: _qty,
      notes: _notesCtrl.text.trim().isEmpty ? null : _notesCtrl.text.trim(),
    );
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(
      content: Text('Agregado al carrito · $_qty × ${widget.summary.name}'),
    ));
    Navigator.pop(context);
  }

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    final t = widget.state.strings;
    final s = widget.summary;

    return Scaffold(
      backgroundColor: palette.bg,
      body: SafeArea(
        child: FutureBuilder<_DishDetailData>(
          future: _future,
          builder: (context, snap) {
            if (snap.connectionState == ConnectionState.waiting) {
              return const Center(child: CircularProgressIndicator());
            }
            if (snap.hasError) {
              return Center(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(t.t('common.error')),
                    TextButton(
                      onPressed: () => setState(() => _future = _load()),
                      child: Text(t.t('catalog.retry')),
                    ),
                  ],
                ),
              );
            }
            final data = snap.data!;
            return CustomScrollView(
              slivers: [
                SliverAppBar(
                  pinned: true,
                  backgroundColor: palette.bg,
                  elevation: 0,
                  leading: IconButton(
                    icon: const Icon(Icons.arrow_back),
                    onPressed: () => Navigator.pop(context),
                  ),
                  expandedHeight: 200,
                  flexibleSpace: FlexibleSpaceBar(
                    background: Container(
                      decoration: BoxDecoration(
                        gradient: LinearGradient(
                          colors: [palette.accent, palette.green],
                          begin: Alignment.topLeft,
                          end: Alignment.bottomRight,
                        ),
                      ),
                      alignment: Alignment.center,
                      child: const Text('🍽️',
                          style: TextStyle(fontSize: 96)),
                    ),
                  ),
                ),
                SliverToBoxAdapter(
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(s.name,
                            style: Theme.of(context).textTheme.displaySmall),
                        if (data.recipe.description != null) ...[
                          const SizedBox(height: 8),
                          Text(data.recipe.description!,
                              style: Theme.of(context).textTheme.bodyMedium),
                        ],
                        const SizedBox(height: 16),
                        Row(
                          children: [
                            Text(
                              '\$${(s.sellingPriceUsd ?? 0).toStringAsFixed(2)}',
                              style: Theme.of(context).textTheme.titleLarge,
                            ),
                            const SizedBox(width: 16),
                            Icon(Icons.schedule, size: 16, color: palette.inkSoft),
                            const SizedBox(width: 4),
                            Text('${s.prepTimeMinutes} ${t.t('dish.minutes')}',
                                style: Theme.of(context).textTheme.bodySmall),
                          ],
                        ),
                        if (s.isOutOfStock)
                          Padding(
                            padding: const EdgeInsets.only(top: 12),
                            child: Container(
                              padding: const EdgeInsets.symmetric(
                                  horizontal: 10, vertical: 6),
                              decoration: BoxDecoration(
                                color: palette.redSoft,
                                borderRadius: BorderRadius.circular(8),
                              ),
                              child: Text(t.t('dish.outOfStock'),
                                  style: TextStyle(color: palette.red)),
                            ),
                          ),
                        const SizedBox(height: 24),
                        if (data.reviews.isNotEmpty) ...[
                          Text(t.t('tab.reviews'),
                              style: Theme.of(context).textTheme.titleLarge),
                          const SizedBox(height: 8),
                          ...data.reviews.take(5).map((r) => _ReviewTile(review: r)),
                          if (data.reviews.length > 5)
                            TextButton(
                              onPressed: null,
                              child: Text('Ver todas (${data.reviews.length})'),
                            ),
                          const SizedBox(height: 16),
                        ],
                        if (!s.isOutOfStock) ...[
                          Text('Cantidad',
                              style: Theme.of(context).textTheme.titleMedium),
                          const SizedBox(height: 8),
                          Row(
                            children: [
                              IconButton.outlined(
                                onPressed: _qty > 1 ? () => setState(() => _qty--) : null,
                                icon: const Icon(Icons.remove),
                              ),
                              const SizedBox(width: 16),
                              Text('$_qty',
                                  style: Theme.of(context).textTheme.headlineMedium),
                              const SizedBox(width: 16),
                              IconButton.outlined(
                                onPressed: () => setState(() => _qty++),
                                icon: const Icon(Icons.add),
                              ),
                            ],
                          ),
                          const SizedBox(height: 16),
                          TextField(
                            controller: _notesCtrl,
                            decoration: InputDecoration(
                              labelText: 'Notas (sin cebolla, etc.)',
                              hintText: t.t('cart.notes'),
                            ),
                            maxLines: 2,
                          ),
                          const SizedBox(height: 24),
                          SizedBox(
                            width: double.infinity,
                            child: ElevatedButton.icon(
                              icon: const Icon(Icons.add_shopping_cart),
                              onPressed: _addToCart,
                              label: Text(
                                  '${t.t('dish.addToCart')} · '
                                  '\$${((s.sellingPriceUsd ?? 0) * _qty).toStringAsFixed(2)}'),
                            ),
                          ),
                        ],
                      ],
                    ),
                  ),
                ),
              ],
            );
          },
        ),
      ),
    );
  }
}

class _DishDetailData {
  final Recipe recipe;
  final List<PublicReview> reviews;
  const _DishDetailData(this.recipe, this.reviews);
}

class _ReviewTile extends StatelessWidget {
  final PublicReview review;
  const _ReviewTile({required this.review});

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    return Container(
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: palette.card,
        border: Border.all(color: palette.line),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(review.customerDisplay,
                  style: Theme.of(context).textTheme.titleMedium),
              Row(
                children: List.generate(5, (i) => Icon(
                      i < review.rating ? Icons.star : Icons.star_border,
                      size: 16,
                      color: palette.sun,
                    )),
              ),
            ],
          ),
          if (review.comment != null && review.comment!.isNotEmpty) ...[
            const SizedBox(height: 8),
            Text(review.comment!,
                style: Theme.of(context).textTheme.bodyMedium),
          ],
          const SizedBox(height: 4),
          Text(DateFormat('dd MMM yyyy', 'es').format(review.createdAt.toLocal()),
              style: Theme.of(context).textTheme.bodySmall),
        ],
      ),
    );
  }
}
