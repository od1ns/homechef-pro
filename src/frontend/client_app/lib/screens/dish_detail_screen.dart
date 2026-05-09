import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';
import 'package:intl/intl.dart';

import '../app_state.dart';

/// Dish detail editorial (F-22C, β híbrida):
/// - Hero 4:3 a full bleed con back/heart en glass.
/// - Label uppercase + título serif large + descripción narrativa.
/// - Meta grid 3 columnas con bordes verticales finos.
/// - Reviews minimal cards.
/// - Quantity stepper + notes en una card.
/// - CTA bar inferior fija con precio en mono y primary coral.
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
      body: FutureBuilder<_DishDetailData>(
        future: _future,
        builder: (context, snap) {
          if (snap.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }
          if (snap.hasError) {
            return SafeArea(
              child: Center(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(t.t('common.error')),
                    const SizedBox(height: 12),
                    FilledButton(
                      onPressed: () => setState(() => _future = _load()),
                      child: Text(t.t('catalog.retry')),
                    ),
                  ],
                ),
              ),
            );
          }
          final data = snap.data!;
          return Stack(
            children: [
              // F-22C v2: maxWidth 480 para desktop. En mobile ocupa todo.
              Center(
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 480),
                  child: ListView(
                padding: const EdgeInsets.only(bottom: 96),
                children: [
                  _HeroImage(
                    palette: palette,
                    isOutOfStock: s.isOutOfStock,
                    imageUrl: s.imageUrl,
                    apiBase: widget.state.api.client.baseUri.toString(),
                  ),
                  const SizedBox(height: 22),
                  _DetailHeader(
                    summary: s,
                    description: data.recipe.description,
                    palette: palette,
                  ),
                  const SizedBox(height: 22),
                  _MetaGrid(
                    palette: palette,
                    minutes: s.prepTimeMinutes,
                    minutesLabel: t.t('dish.minutes'),
                    rating: data.reviews.isEmpty ? null : data.reviews.map((r) => r.rating).reduce((a, b) => a + b) / data.reviews.length,
                    reviewCount: data.reviews.length,
                  ),
                  const SizedBox(height: 22),
                  if (data.reviews.isNotEmpty) ...[
                    Padding(
                      padding: const EdgeInsets.fromLTRB(22, 0, 22, 12),
                      child: Text(
                        'Lo que dicen',
                        style: Theme.of(context).textTheme.headlineMedium,
                      ),
                    ),
                    for (final r in data.reviews.take(5))
                      _ReviewTile(review: r, palette: palette),
                    if (data.reviews.length > 5)
                      Padding(
                        padding: const EdgeInsets.symmetric(horizontal: 22, vertical: 4),
                        child: TextButton(
                          onPressed: null,
                          child: Text('Ver todas (${data.reviews.length})'),
                        ),
                      ),
                    const SizedBox(height: 22),
                  ],
                  if (!s.isOutOfStock) ...[
                    Padding(
                      padding: const EdgeInsets.fromLTRB(22, 0, 22, 12),
                      child: Text(
                        'Tu pedido',
                        style: Theme.of(context).textTheme.headlineMedium,
                      ),
                    ),
                    Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 22),
                      child: _OrderConfig(
                        palette: palette,
                        qty: _qty,
                        onDecrement: _qty > 1 ? () => setState(() => _qty--) : null,
                        onIncrement: () => setState(() => _qty++),
                        notesCtrl: _notesCtrl,
                        notesHint: t.t('cart.notes'),
                      ),
                    ),
                  ],
                ],
                  ),
                ),
              ),

              // Top bar con back/heart en glass-morphism puntual.
              SafeArea(
                child: Padding(
                  padding: const EdgeInsets.fromLTRB(14, 8, 14, 0),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      _GlassIconButton(
                        icon: Icons.arrow_back_rounded,
                        onTap: () => Navigator.pop(context),
                      ),
                      _GlassIconButton(
                        icon: Icons.favorite_border,
                        onTap: () {},
                      ),
                    ],
                  ),
                ),
              ),

              // CTA bar inferior fijo. F-22C v2: tambien limitado a 480 en desktop.
              if (!s.isOutOfStock)
                Positioned(
                  left: 0, right: 0, bottom: 0,
                  child: Center(
                    child: ConstrainedBox(
                      constraints: const BoxConstraints(maxWidth: 480),
                      child: _BottomCta(
                    palette: palette,
                    qty: _qty,
                    unitPrice: s.sellingPriceUsd ?? 0,
                    label: t.t('dish.addToCart'),
                    onAdd: _addToCart,
                  ),
                    ),
                  ),
                ),
            ],
          );
        },
      ),
    );
  }
}

/// Hero 4:3 con foto del plato (si esta) o gradient calido como fallback,
/// + badge "Fresco" o "Agotado".
class _HeroImage extends StatelessWidget {
  final HcpPalette palette;
  final bool isOutOfStock;
  final String? imageUrl;
  final String apiBase;
  const _HeroImage({
    required this.palette,
    required this.isOutOfStock,
    required this.imageUrl,
    required this.apiBase,
  });

  @override
  Widget build(BuildContext context) {
    final hasImage = imageUrl != null && imageUrl!.isNotEmpty;
    final fullUrl = hasImage
        ? (imageUrl!.startsWith('http')
            ? imageUrl!
            : '${apiBase.replaceAll(RegExp(r'/$'), '')}$imageUrl')
        : null;

    return AspectRatio(
      aspectRatio: 4 / 3,
      child: Stack(
        fit: StackFit.expand,
        children: [
          if (hasImage)
            Image.network(
              fullUrl!,
              fit: BoxFit.cover,
              loadingBuilder: (_, child, progress) {
                if (progress == null) return child;
                return Container(
                  color: const Color(0xFFD4A574),
                  alignment: Alignment.center,
                  child: const SizedBox(
                    width: 28, height: 28,
                    child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
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
                child: const Icon(Icons.restaurant_menu_outlined, size: 64, color: Colors.white60),
              ),
            )
          else
            Container(
              decoration: const BoxDecoration(
                gradient: LinearGradient(
                  colors: [Color(0xFFE8B996), Color(0xFFD4A574), Color(0xFFC49164)],
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                ),
              ),
            ),
          // Badge bottom-left
          Align(
            alignment: Alignment.bottomLeft,
            child: Padding(
              padding: const EdgeInsets.fromLTRB(22, 0, 22, 22),
              child: Wrap(
                spacing: 6,
                children: [
                  _SoftBadge(
                    text: isOutOfStock ? 'Agotado' : 'Fresco hoy',
                    bg: palette.card,
                    fg: isOutOfStock ? palette.red : palette.ink,
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _SoftBadge extends StatelessWidget {
  final String text;
  final Color bg;
  final Color fg;
  const _SoftBadge({required this.text, required this.bg, required this.fg});

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 11, vertical: 5),
        decoration: BoxDecoration(
          color: bg,
          borderRadius: BorderRadius.circular(999),
        ),
        child: Text(
          text,
          style: TextStyle(
            fontSize: 11, fontWeight: FontWeight.w500,
            color: fg, letterSpacing: 0.02,
          ),
        ),
      );
}

class _GlassIconButton extends StatelessWidget {
  final IconData icon;
  final VoidCallback onTap;
  const _GlassIconButton({required this.icon, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.white.withValues(alpha: 0.92),
      shape: const CircleBorder(),
      child: InkWell(
        onTap: onTap,
        customBorder: const CircleBorder(),
        child: const SizedBox(
          width: 38, height: 38,
          child: Icon(Icons.arrow_back_rounded, size: 18, color: Color(0xFF1A1614)),
        ),
      ),
    );
  }
}

/// Header editorial: label uppercase + título serif large + descripción narrativa.
class _DetailHeader extends StatelessWidget {
  final RecipeSummary summary;
  final String? description;
  final HcpPalette palette;
  const _DetailHeader({
    required this.summary,
    required this.description,
    required this.palette,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 22),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (summary.category != null && summary.category!.isNotEmpty)
            Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: Text(
                summary.category!.toUpperCase(),
                style: TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.w500,
                  letterSpacing: 0.1,
                  color: palette.inkMuted,
                ),
              ),
            ),
          Text(summary.name, style: theme.textTheme.displaySmall),
          if (description != null && description!.isNotEmpty) ...[
            const SizedBox(height: 14),
            Text(
              description!,
              style: TextStyle(
                fontFamily: 'Inter',
                fontSize: 14, height: 1.7,
                color: palette.inkSoft,
              ),
            ),
          ],
        ],
      ),
    );
  }
}

/// Meta grid 3 columnas con bordes verticales finos editoriales.
class _MetaGrid extends StatelessWidget {
  final HcpPalette palette;
  final int minutes;
  final String minutesLabel;
  final double? rating;
  final int reviewCount;
  const _MetaGrid({
    required this.palette,
    required this.minutes,
    required this.minutesLabel,
    required this.rating,
    required this.reviewCount,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.symmetric(horizontal: 22),
      decoration: BoxDecoration(
        border: Border(
          top: BorderSide(color: palette.line, width: 0.5),
          bottom: BorderSide(color: palette.line, width: 0.5),
        ),
      ),
      child: Row(
        children: [
          _MetaCell(
            palette: palette,
            label: 'Prep',
            value: '$minutes',
            unit: minutesLabel,
            divider: true,
          ),
          _MetaCell(
            palette: palette,
            label: 'Rating',
            value: rating == null ? '—' : rating!.toStringAsFixed(1),
            unit: rating == null ? '' : '★',
            divider: true,
          ),
          _MetaCell(
            palette: palette,
            label: 'Reseñas',
            value: '$reviewCount',
            unit: '',
            divider: false,
          ),
        ],
      ),
    );
  }
}

class _MetaCell extends StatelessWidget {
  final HcpPalette palette;
  final String label;
  final String value;
  final String unit;
  final bool divider;
  const _MetaCell({
    required this.palette,
    required this.label,
    required this.value,
    required this.unit,
    required this.divider,
  });

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: Container(
        decoration: BoxDecoration(
          border: divider
              ? Border(right: BorderSide(color: palette.line, width: 0.5))
              : null,
        ),
        padding: const EdgeInsets.symmetric(vertical: 14),
        child: Column(
          children: [
            Text(
              label.toUpperCase(),
              style: TextStyle(
                fontSize: 10,
                fontWeight: FontWeight.w500,
                letterSpacing: 0.1,
                color: palette.inkMuted,
              ),
            ),
            const SizedBox(height: 5),
            RichText(
              textAlign: TextAlign.center,
              text: TextSpan(children: [
                TextSpan(
                  text: value,
                  style: TextStyle(
                    fontFamily: 'Instrument Serif',
                    fontSize: 22, height: 1,
                    letterSpacing: -0.01,
                    color: palette.ink,
                  ),
                ),
                if (unit.isNotEmpty)
                  TextSpan(
                    text: ' $unit',
                    style: TextStyle(
                      fontSize: 11,
                      color: palette.inkSoft,
                    ),
                  ),
              ]),
            ),
          ],
        ),
      ),
    );
  }
}

class _OrderConfig extends StatelessWidget {
  final HcpPalette palette;
  final int qty;
  final VoidCallback? onDecrement;
  final VoidCallback onIncrement;
  final TextEditingController notesCtrl;
  final String notesHint;
  const _OrderConfig({
    required this.palette,
    required this.qty,
    required this.onDecrement,
    required this.onIncrement,
    required this.notesCtrl,
    required this.notesHint,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: palette.card,
        borderRadius: BorderRadius.circular(28),
        border: Border.all(color: palette.line, width: 0.5),
      ),
      padding: const EdgeInsets.all(20),
      child: Column(
        children: [
          Row(
            children: [
              Text(
                'Cantidad',
                style: TextStyle(
                  fontSize: 14, fontWeight: FontWeight.w500,
                  color: palette.ink,
                ),
              ),
              const Spacer(),
              _StepperBtn(palette: palette, icon: Icons.remove, onTap: onDecrement),
              SizedBox(
                width: 44,
                child: Text(
                  '$qty',
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    fontFamily: 'Instrument Serif',
                    fontSize: 26,
                    color: palette.ink,
                  ),
                ),
              ),
              _StepperBtn(palette: palette, icon: Icons.add, onTap: onIncrement),
            ],
          ),
          const SizedBox(height: 16),
          Divider(color: palette.line, height: 1),
          const SizedBox(height: 16),
          TextField(
            controller: notesCtrl,
            maxLines: 2,
            decoration: InputDecoration(
              labelText: 'Notas (sin cebolla, etc.)',
              hintText: notesHint,
              border: InputBorder.none,
              focusedBorder: InputBorder.none,
              enabledBorder: InputBorder.none,
              filled: false,
              contentPadding: EdgeInsets.zero,
            ),
            style: TextStyle(fontSize: 14, color: palette.ink),
          ),
        ],
      ),
    );
  }
}

class _StepperBtn extends StatelessWidget {
  final HcpPalette palette;
  final IconData icon;
  final VoidCallback? onTap;
  const _StepperBtn({required this.palette, required this.icon, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final enabled = onTap != null;
    return Material(
      color: Colors.transparent,
      shape: CircleBorder(
        side: BorderSide(
          color: enabled ? palette.line : palette.line.withValues(alpha: 0.4),
          width: 0.5,
        ),
      ),
      child: InkWell(
        onTap: onTap,
        customBorder: const CircleBorder(),
        child: SizedBox(
          width: 38, height: 38,
          child: Icon(
            icon,
            size: 16,
            color: enabled ? palette.ink : palette.inkMuted.withValues(alpha: 0.6),
          ),
        ),
      ),
    );
  }
}

class _BottomCta extends StatelessWidget {
  final HcpPalette palette;
  final int qty;
  final double unitPrice;
  final String label;
  final VoidCallback onAdd;
  const _BottomCta({
    required this.palette,
    required this.qty,
    required this.unitPrice,
    required this.label,
    required this.onAdd,
  });

  @override
  Widget build(BuildContext context) {
    final total = unitPrice * qty;
    return Container(
      decoration: BoxDecoration(
        color: palette.bg,
        border: Border(
          top: BorderSide(color: palette.line, width: 0.5),
        ),
      ),
      child: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(14, 14, 14, 14),
          child: Material(
            color: palette.accent,
            borderRadius: BorderRadius.circular(24),
            child: InkWell(
              onTap: onAdd,
              borderRadius: BorderRadius.circular(24),
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 22, vertical: 14),
                child: Row(
                  children: [
                    Text(
                      label,
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 15,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                    const Spacer(),
                    Text(
                      '\$ ${total.toStringAsFixed(2)}',
                      style: const TextStyle(
                        fontFamily: 'JetBrains Mono',
                        color: Colors.white,
                        fontSize: 14,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _ReviewTile extends StatelessWidget {
  final PublicReview review;
  final HcpPalette palette;
  const _ReviewTile({required this.review, required this.palette});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(22, 0, 22, 10),
      child: Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: palette.card,
          border: Border.all(color: palette.line, width: 0.5),
          borderRadius: BorderRadius.circular(20),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  review.customerDisplay,
                  style: TextStyle(
                    fontSize: 14, fontWeight: FontWeight.w500,
                    color: palette.ink,
                  ),
                ),
                Row(
                  children: List.generate(5, (i) => Icon(
                        i < review.rating ? Icons.star_rounded : Icons.star_outline_rounded,
                        size: 14,
                        color: i < review.rating ? palette.accent : palette.inkMuted,
                      )),
                ),
              ],
            ),
            if (review.comment != null && review.comment!.isNotEmpty) ...[
              const SizedBox(height: 8),
              Text(
                review.comment!,
                style: TextStyle(
                  fontSize: 13, height: 1.6,
                  color: palette.inkSoft,
                ),
              ),
            ],
            const SizedBox(height: 6),
            Text(
              DateFormat('dd MMM yyyy', 'es').format(review.createdAt.toLocal()),
              style: TextStyle(
                fontSize: 11,
                fontFamily: 'JetBrains Mono',
                color: palette.inkMuted,
              ),
            ),
          ],
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
