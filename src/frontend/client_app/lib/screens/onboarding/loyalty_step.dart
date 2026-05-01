import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';

import '../../app_state.dart';

class LoyaltyStep extends StatelessWidget {
  final AppState state;
  final bool wantsUpdates;
  final ValueChanged<bool> onToggle;
  final VoidCallback onFinish;

  const LoyaltyStep({
    super.key,
    required this.state,
    required this.wantsUpdates,
    required this.onToggle,
    required this.onFinish,
  });

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    return Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        children: [
          const SizedBox(height: 8),
          Container(
            width: 96,
            height: 96,
            decoration: BoxDecoration(
              color: palette.sun,
              shape: BoxShape.circle,
            ),
            alignment: Alignment.center,
            child: const Text('⭐', style: TextStyle(fontSize: 48)),
          ),
          const SizedBox(height: 16),
          Text('Sabor',
              style: Theme.of(context).textTheme.displaySmall,
              textAlign: TextAlign.center),
          const SizedBox(height: 4),
          Text(
            'Nuestro programa de lealtad. Acumulas Sabor con cada pedido y lo '
            'canjeas por descuentos, platos especiales y eventos privados.',
            style: Theme.of(context).textTheme.bodyMedium,
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 24),
          Expanded(
            child: ListView(
              children: [
                _Tier(
                    color: palette.greenSoft,
                    label: 'Casero',
                    points: '0 – 99 Sabor',
                    perks: 'Bienvenida con tequeño de cortesía en tu primer pickup'),
                const SizedBox(height: 8),
                _Tier(
                    color: palette.sun.withValues(alpha: 0.4),
                    label: 'Familiar',
                    points: '100 – 499 Sabor',
                    perks: 'Acceso anticipado a especiales del día'),
                const SizedBox(height: 8),
                _Tier(
                    color: palette.accent.withValues(alpha: 0.25),
                    label: 'Compadre',
                    points: '500+ Sabor',
                    perks: 'Reservas privadas + descuento permanente del 10%'),
              ],
            ),
          ),
          Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: palette.line,
              borderRadius: BorderRadius.circular(12),
            ),
            child: Row(
              children: [
                Switch.adaptive(
                  value: wantsUpdates,
                  onChanged: onToggle,
                ),
                const SizedBox(width: 8),
                const Expanded(
                  child: Text(
                    'Avísame cuando Sabor esté disponible',
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 4),
          Text(
            'Próximamente · aún estamos afinando los detalles.',
            style: Theme.of(context).textTheme.bodySmall,
          ),
          const SizedBox(height: 16),
          SizedBox(
            width: double.infinity,
            child: ElevatedButton(
              onPressed: onFinish,
              child: const Text('Empezar a pedir'),
            ),
          ),
        ],
      ),
    );
  }
}

class _Tier extends StatelessWidget {
  final Color color;
  final String label;
  final String points;
  final String perks;
  const _Tier({
    required this.color,
    required this.label,
    required this.points,
    required this.perks,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: color,
        borderRadius: BorderRadius.circular(16),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text('⭐', style: TextStyle(fontSize: 24)),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(label,
                        style: Theme.of(context).textTheme.titleMedium),
                    Text(points,
                        style: Theme.of(context).textTheme.bodySmall),
                  ],
                ),
                const SizedBox(height: 4),
                Text(perks, style: Theme.of(context).textTheme.bodyMedium),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
