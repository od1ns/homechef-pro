import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';

import '../app_state.dart';
import 'dish_detail_screen.dart';

const Map<String, String> _categoryDisplay = {
  'main': 'Plato principal',
  'mains': 'Platos principales',
  'appetizer': 'Para empezar',
  'starters': 'Para empezar',
  'dessert': 'Postres del día',
  'desserts': 'Postres del día',
  'side': 'Acompañantes',
  'sides': 'Acompañantes',
  'drink': 'Para beber',
  'drinks': 'Para beber',
  'breakfast': 'Desayunos',
  'lunch': 'Almuerzos',
  'dinner': 'Cenas',
  'snack': 'Antojos',
  'snacks': 'Antojos',
};

String _displayCategory(String? raw) {
  if (raw == null || raw.trim().isEmpty) return 'Lo del día';
  final key = raw.trim().toLowerCase();
  return _categoryDisplay[key] ?? raw.trim();
}

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

  Map<String, List<RecipeSummary>> _groupByCategory(List<RecipeSummary> dishes) {
    final groups = <String, List<RecipeSummary>>{};
    for (final d in dishes) {
      final key = _displayCategory(d.category);
      groups.putIfAbsent(key, () => []).add(d);
    }
    return groups;
  }

  @override
  Widget build(BuildContext context) {
    final t = widget.state.strings;
    final theme = Theme.of(context);
    final palette = theme.extension<HcpThemeExtension>()!.palette;

    return RefreshIndicator(
      onRefresh: _refresh,
      child: FutureBuilder<List<RecipeSummary>>(
        future: _future,
        builder: (context, snap) {
          if (snap.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }
          if (snap.hasError) {
            return _ErrorState(message: t.t('catalog.error'), onRetry: _refresh, retryLabel: t.t('catalog.retry'));
          }

          final dishes = (snap.data ?? const <RecipeSummary>[])
              .where((d) => !d.isSubRecipe)
              .toList();

          if (dishes.isEmpty) {
            return _EmptyState(message: t.t('catalog.empty'));
          }

          final groups = _groupByCategory(dishes);
          final categories = groups.keys.toList();

          return Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 480),
              child: ListView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: const EdgeInsets.only(bottom: 96),
                children: [
                  _EditorialHero(state: widget.state, palette: palette),
                  const SizedBox(height: 18),
                  _ChefStoryCard(palette: palette),
                  const SizedBox(height: 28),
                  for (final cat in categories) ...[
                    _SectionHeader(
                      title: cat,
                      count: groups[cat]!.length,
                      countLabel: groups[cat]!.length == 1 ? 'plato' : 'platos',
                    ),
                    const SizedBox(height: 12),
                    for (final dish in groups[cat]!) ...[
                      _EditorialDishCard(
                        dish: dish,
                        palette: palette,
                        onAdd: () => widget.state.addToCart(dish),
                        onTap: () => Navigator.of(context).push(
                          MaterialPageRoute(
                            builder: (_) => DishDetailScreen(
                              state: widget.state,
                              summary: dish,
                            ),
                          ),
                        ),
                        minutesLabel: t.t('dish.minutes'),
                        outOfStockLabel: t.t('dish.outOfStock'),
                        apiBase: widget.state.api.client.baseUri.toString(),
                      ),
                      const SizedBox(height: 10),
                    ],
                    const SizedBox(height: 18),
                  ],
                ],
              ),
            ),
          );
        },
      ),
    );
  }
}

class _EditorialHero extends StatelessWidget {
  final AppState state;
  final HcpPalette palette;
  const _EditorialHero({required this.state, required this.palette});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.fromLTRB(22, 24, 22, 4),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'Doña Carmen'.toUpperCase(),
            style: TextStyle(
              fontSize: 11,
              fontWeight: FontWeight.w500,
              letterSpacing: 0.12,
              color: palette.inkMuted,
            ),
          ),
          const SizedBox(height: 14),
          Text(
            'Cocina\ncasera, hoy.',
            style: theme.textTheme.displayMedium,
          ),
        ],
      ),
    );
  }
}

class _ChefStoryCard extends StatelessWidget {
  final HcpPalette palette;
  const _ChefStoryCard({required this.palette});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 22),
      child: Container(
        padding: const EdgeInsets.all(18),
        decoration: BoxDecoration(
          color: palette.card,
          border: Border.all(color: palette.line, width: 0.5),
          borderRadius: BorderRadius.circular(24),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Container(
                  width: 52, height: 52,
                  decoration: const BoxDecoration(
                    shape: BoxShape.circle,
                    gradient: LinearGradient(
                      colors: [Color(0xFFE8B996), Color(0xFFC49164)],
                      begin: Alignment.topLeft,
                      end: Alignment.bottomRight,
                    ),
                  ),
                  alignment: Alignment.center,
                  child: const Text('DC',
                    style: TextStyle(
                      fontFamily: 'Instrument Serif',
                      fontSize: 22,
                      color: Colors.white,
                      letterSpacing: -0.01,
                    ),
                  ),
                ),
                const SizedBox(width: 14),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Doña Carmen Rodríguez',
                        style: TextStyle(
                          fontFamily: 'Instrument Serif',
                          fontSize: 18,
                          height: 1.1,
                          letterSpacing: -0.01,
                          color: palette.ink,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        'Las Mercedes · Caracas',
                        style: TextStyle(fontSize: 12, color: palette.inkSoft),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 14),
            Text(
              'Recetas de mi abuela, masa fresca cada mañana, ragú a fuego lento. Cocino para 8-10 familias por día.',
              style: TextStyle(
                fontSize: 13, height: 1.55,
                color: palette.inkSoft,
              ),
            ),
            const SizedBox(height: 14),
            Row(
              children: [
                Container(
                  width: 7, height: 7,
                  decoration: BoxDecoration(color: palette.green, shape: BoxShape.circle),
                ),
                const SizedBox(width: 8),
                Text(
                  'En vivo · 11:00 - 15:00',
                  style: TextStyle(
                    fontSize: 12, fontWeight: FontWeight.w500,
                    color: palette.green,
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _SectionHeader extends StatelessWidget {
  final String title;
  final int count;
  final String countLabel;
  const _SectionHeader({required this.title, required this.count, required this.countLabel});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.extension<HcpThemeExtension>()!.palette;
    return Padding(
      padding: const EdgeInsets.fromLTRB(22, 0, 22, 0),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          Expanded(
            child: Text(title, style: theme.textTheme.headlineMedium),
          ),
          Text(
            '$count $countLabel',
            style: TextStyle(
              fontFamily: 'JetBrains Mono',
              fontSize: 11,
              color: palette.inkMuted,
            ),
          ),
        ],
      ),
    );
  }
}

class _EditorialDishCard extends StatelessWidget {
  final RecipeSummary dish;
  final HcpPalette palette;
  final VoidCallback onAdd;
  final VoidCallback onTap;
  final String minutesLabel;
  final String outOfStockLabel;
  final String apiBase;

  const _EditorialDishCard({
    required this.dish,
    required this.palette,
    required this.onAdd,
    required this.onTap,
    required this.minutesLabel,
    required this.outOfStockLabel,
    required this.apiBase,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 22),
      child: Material(
        color: palette.card,
        elevation: 0,
        borderRadius: BorderRadius.circular(28),
        clipBehavior: Clip.antiAlias,
        child: InkWell(
          onTap: onTap,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              AspectRatio(
                aspectRatio: 21 / 9,
                child: Stack(
                  fit: StackFit.expand,
                  children: [
                    if (dish.imageUrl != null && dish.imageUrl!.isNotEmpty)
                      _DishImage(imageUrl: dish.imageUrl!, apiBase: apiBase)
                    else
                      Container(
                        decoration: const BoxDecoration(
                          gradient: LinearGradient(
                            colors: [
                              Color(0xFFE8B996),
                              Color(0xFFD4A574),
                              Color(0xFFC49164),
                            ],
                            begin: Alignment.topLeft,
                            end: Alignment.bottomRight,
                          ),
                        ),
                        alignment: Alignment.center,
                        child: Icon(Icons.restaurant_menu_outlined,
                            size: 56, color: palette.card.withValues(alpha: 0.55)),
                      ),
                    // Badges overlay
                    Align(
                      alignment: Alignment.bottomLeft,
                      child: Padding(
                        padding: const EdgeInsets.all(12),
                        child: Wrap(
                          spacing: 6,
                          children: [
                            if (dish.isOutOfStock)
                              _Badge(text: outOfStockLabel, bg: palette.card, fg: palette.red)
                            else
                              _Badge(text: 'Hecho hoy', bg: palette.card, fg: palette.ink),
                          ],
                        ),
                      ),
                    ),
                    // Indicador "tap para ver detalle" (chip discreto top-right).
                    Positioned(
                      top: 10,
                      right: 10,
                      child: Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                        decoration: BoxDecoration(
                          color: palette.card.withValues(alpha: 0.92),
                          borderRadius: BorderRadius.circular(999),
                        ),
                        child: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(Icons.touch_app_outlined, size: 13, color: palette.ink),
                            const SizedBox(width: 4),
                            Text('Ver',
                                style: TextStyle(
                                    fontSize: 11,
                                    color: palette.ink,
                                    fontWeight: FontWeight.w600)),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              Padding(
                padding: const EdgeInsets.fromLTRB(16, 12, 16, 14),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      dish.name,
                      style: TextStyle(
                        fontFamily: 'Instrument Serif',
                        fontSize: 18,
                        height: 1.1,
                        letterSpacing: -0.01,
                        color: palette.ink,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Row(
                      children: [
                        Icon(Icons.schedule_outlined, size: 14, color: palette.inkMuted),
                        const SizedBox(width: 4),
                        Text(
                          '${dish.prepTimeMinutes} $minutesLabel',
                          style: TextStyle(fontFamily: 'JetBrains Mono', fontSize: 11, color: palette.inkSoft),
                        ),
                        const Spacer(),
                        Text(
                          '\$${(dish.sellingPriceUsd ?? 0).toStringAsFixed(0)}',
                          style: TextStyle(
                            fontFamily: 'Instrument Serif',
                            fontSize: 22,
                            height: 1,
                            letterSpacing: -0.02,
                            color: palette.accent,
                          ),
                        ),
                        const SizedBox(width: 10),
                        if (!dish.isOutOfStock)
                          _AddButton(palette: palette, onTap: onAdd),
                      ],
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

class _Badge extends StatelessWidget {
  final String text;
  final Color bg;
  final Color fg;
  const _Badge({required this.text, required this.bg, required this.fg});
  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 11, vertical: 5),
        decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(999)),
        child: Text(
          text,
          style: TextStyle(fontSize: 11, fontWeight: FontWeight.w500, color: fg, letterSpacing: 0.02),
        ),
      );
}

class _AddButton extends StatelessWidget {
  final HcpPalette palette;
  final VoidCallback onTap;
  const _AddButton({required this.palette, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return Material(
      color: palette.accent,
      shape: const CircleBorder(),
      child: InkWell(
        onTap: onTap,
        customBorder: const CircleBorder(),
        child: const SizedBox(
          width: 38, height: 38,
          child: Icon(Icons.add, color: Colors.white, size: 20),
        ),
      ),
    );
  }
}

class _ErrorState extends StatelessWidget {
  final String message;
  final VoidCallback onRetry;
  final String retryLabel;
  const _ErrorState({required this.message, required this.onRetry, required this.retryLabel});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Text(message, textAlign: TextAlign.center, style: Theme.of(context).textTheme.bodyLarge),
            const SizedBox(height: 16),
            FilledButton(onPressed: onRetry, child: Text(retryLabel)),
          ],
        ),
      ),
    );
  }
}

class _EmptyState extends StatelessWidget {
  final String message;
  const _EmptyState({required this.message});

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(40),
        child: Text(
          message,
          textAlign: TextAlign.center,
          style: TextStyle(
            fontFamily: 'Instrument Serif',
            fontSize: 24,
            height: 1.2,
            color: palette.inkSoft,
          ),
        ),
      ),
    );
  }
}

/// Etapa 1: render de la imagen del plato. Recibe el apiBase como param
/// para construir la URL absoluta cuando imageUrl viene como path relativo.
class _DishImage extends StatelessWidget {
  final String imageUrl;
  final String apiBase;
  const _DishImage({required this.imageUrl, required this.apiBase});

  @override
  Widget build(BuildContext context) {
    final base = apiBase.replaceAll(RegExp(r'/$'), '');
    final fullUrl = imageUrl.startsWith('http') ? imageUrl : '$base$imageUrl';
    return Image.network(
      fullUrl,
      fit: BoxFit.cover,
      loadingBuilder: (_, child, progress) {
        if (progress == null) return child;
        return Container(
          color: const Color(0xFFE8B996),
          alignment: Alignment.center,
          child: const SizedBox(
            width: 22, height: 22,
            child: CircularProgressIndicator(strokeWidth: 2),
          ),
        );
      },
      errorBuilder: (_, __, ___) => Container(
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            colors: [Color(0xFFE8B996), Color(0xFFD4A574), Color(0xFFC49164)],
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
          ),
        ),
        alignment: Alignment.center,
        child: const Icon(Icons.restaurant_menu_outlined, size: 56, color: Colors.white60),
      ),
    );
  }
}
