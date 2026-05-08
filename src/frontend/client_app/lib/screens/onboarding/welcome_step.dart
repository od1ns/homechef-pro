import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';

import '../../app_state.dart';

class WelcomeStep extends StatelessWidget {
  final AppState state;
  final VoidCallback onNext;
  const WelcomeStep({super.key, required this.state, required this.onNext});

  @override
  Widget build(BuildContext context) {
    final t = state.strings;
    // F-22C v2: maxWidth 480 para desktop responsive.
    return Center(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 480),
        child: Padding(
      padding: const EdgeInsets.all(32),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const Spacer(),
          // F-22C v2: gradient calido apetitoso (terracota → mostaza), NO rojo+verde.
          Container(
            height: 240,
            decoration: BoxDecoration(
              gradient: const LinearGradient(
                colors: [
                  Color(0xFFE8B996),  // terracota suave
                  Color(0xFFD4A574),  // dorado
                  Color(0xFFC49164),  // mostaza profunda
                ],
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
              ),
              borderRadius: BorderRadius.circular(28),
            ),
            alignment: Alignment.center,
            child: const Text('🍳', style: TextStyle(fontSize: 96)),
          ),
          const SizedBox(height: 32),
          Text(t.t('app.name'),
              style: Theme.of(context).textTheme.displayMedium,
              textAlign: TextAlign.center),
          const SizedBox(height: 8),
          Text(t.t('app.tagline'),
              style: Theme.of(context).textTheme.bodyLarge,
              textAlign: TextAlign.center),
          const SizedBox(height: 24),
          Text(
            'Comida casera venezolana, hecha al momento por tu chef de confianza.',
            style: Theme.of(context).textTheme.bodyMedium,
            textAlign: TextAlign.center,
          ),
          const Spacer(),
          ElevatedButton(
            onPressed: onNext,
            child: const Text('Empezar'),
          ),
          const SizedBox(height: 8),
        ],
      ),
        ),
      ),
    );
  }
}
