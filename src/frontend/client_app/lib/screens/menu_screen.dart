import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';

import '../app_state.dart';
import 'dish_detail_screen.dart';

class MenuScreen extends StatefulWidget {
  final AppState state;
  const MenuScreen({super.key, required this.state});

  @override
  State<MenuScreen> createState() => _MenuScreenState();
}

class _MenuScreenState extends State<MenuScreen> {
  late Future<List<RecipeSummary>> _future = _load();

  Future<List<RecipeSummary>> _load() => widget.state.api.menu();

  Future<void> _refresh() async {
    setState(() => _future = _load());
    await _future;
  }

  @override
  Widget build(BuildContext context) {
    final t = widget.state.strings;
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;

    return RefreshIndicator(
      onRefresh: _refresh,
      child: CustomScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        slivers: [
          SliverAppBar(
            backgroundColor: palette.bg,
            elevation: 0,
            pinned: false,
            expandedHeight: 96,
            flexibleSpace: FlexibleSpaceBar(
              titlePadding: const EdgeInsets.fromLTRB(16, 16, 16, 16),
              title: Text(
                t.t('catalog.todaysMenu'),
                style: Theme.of(context).textTheme.displaySmall,
              ),
            ),
          ),
          FutureBuilder<List<RecipeSummary>>(
            future: _future,
            builder: (context, snap) {
              if (snap.connectionState == ConnectionState.waiting) {
                return const SliverFillRemaining(
                  child: Center(child: CircularProgressIndicator()),
                );
              }
              if (snap.hasError) {
                return SliverFillRemaining(
                  child: Center(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Text(t.t('catalog.error')),
                        const SizedBox(height: 12),
                        ElevatedButton(
                          onPressed: _refresh,
                          child: Text(t.t('catalog.retry')),
                        ),
                      ],
                    ),
                  ),
                );
              }
              final dishes = (snap.data ?? const <RecipeSummary>[])
                  .where((d) => !d.isSubRecipe)
                  .toList();
              if (dishes.isEmpty) {
                return SliverFillRemaining(
                  child: Center(child: Text(t.t('catalog.empty'))),
                );
              }
              return SliverPadding(
                padding: const EdgeInsets.fromLTRB(16, 0, 16, 96),
                sliver: SliverList.separated(
                  itemCount: dishes.length,
                  separatorBuilder: (_, __) => const SizedBox(height: 12),
                  itemBuilder: (_, i) => _DishCard(
                    dish: dishes[i],
                    onAdd: () => widget.state.addToCart(dishes[i]),
                    onTap: () => Navigator.of(context).push(
                      MaterialPageRoute(
                        builder: (_) => DishDetailScreen(
                          state: widget.state,
                          summary: dishes[i],
                        ),
                      ),
                    ),
                    addLabel: t.t('dish.addToCart'),
                    minutesLabel: t.t('dish.minutes'),
                    outOfStockLabel: t.t('dish.outOfStock'),
                  ),
                ),
              );
            },
          ),
        ],
      ),
    );
  }
}

class _DishCard extends StatelessWidget {
  final RecipeSummary dish;
  final VoidCallback onAdd;
  final VoidCallback onTap;
  final String addLabel;
  final String minutesLabel;
  final String outOfStockLabel;

  const _DishCard({
    required this.dish,
    required this.onAdd,
    required this.onTap,
    required this.addLabel,
    required this.minutesLabel,
    required this.outOfStockLabel,
  });

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    return Card(
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(16),
        child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Color swatch placeholder while there is no real photo.
            Container(
              width: 72,
              height: 72,
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(12),
                gradient: LinearGradient(
                  colors: [palette.accent, palette.green],
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                ),
              ),
              alignment: Alignment.center,
              child: const Text('🍽️', style: TextStyle(fontSize: 32)),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(dish.name,
                      style: Theme.of(context).textTheme.titleLarge),
                  if (dish.category != null)
                    Padding(
                      padding: const EdgeInsets.only(top: 2),
                      child: Text(
                        dish.category!,
                        style: Theme.of(context).textTheme.bodySmall,
                      ),
                    ),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      Text(
                        '\$${(dish.sellingPriceUsd ?? 0).toStringAsFixed(2)}',
                        style: Theme.of(context).textTheme.labelMedium?.copyWith(
                              fontWeight: FontWeight.w600,
                              color: palette.ink,
                            ),
                      ),
                      const SizedBox(width: 12),
                      Icon(Icons.schedule,
                          size: 14, color: palette.inkSoft),
                      const SizedBox(width: 4),
                      Text(
                        '${dish.prepTimeMinutes} $minutesLabel',
                        style: Theme.of(context).textTheme.bodySmall,
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  if (dish.isOutOfStock)
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                      decoration: BoxDecoration(
                        color: palette.redSoft,
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Text(outOfStockLabel,
                          style: TextStyle(color: palette.red, fontSize: 12)),
                    )
                  else
                    Align(
                      alignment: Alignment.centerLeft,
                      child: ElevatedButton(
                        onPressed: onAdd,
                        child: Text(addLabel),
                      ),
                    ),
                ],
              ),
            ),
          ],
        ),
      ),
    ),
  );
  }
}
