import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';

import '../../app_state.dart';

class WelcomeStep extends StatelessWidget {
  final AppState state;
  final VoidCallback onNext;
  const WelcomeStep({super.key, required this.state, required this.onNext});

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    final t = state.strings;
    return Padding(
      padding: const EdgeInsets.all(32),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const Spacer(),
          Container(
            height: 220,
            decoration: BoxDecoration(
              gradient: LinearGradient(
                colors: [palette.accent, palette.green],
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
              ),
              borderRadius: BorderRadius.circular(24),
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
    );
  }
}
