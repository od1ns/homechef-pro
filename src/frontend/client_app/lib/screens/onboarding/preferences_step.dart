import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';

import '../../app_state.dart';

typedef OnboardingMutator = OnboardingData Function(OnboardingData);

class PreferencesStep extends StatelessWidget {
  final AppState state;
  final OnboardingData data;
  final void Function(OnboardingMutator) onChanged;
  final VoidCallback onNext;

  const PreferencesStep({
    super.key,
    required this.state,
    required this.data,
    required this.onChanged,
    required this.onNext,
  });

  static const _dietary = [
    ('vegetarian',   'Vegetariano'),
    ('vegan',        'Vegano'),
    ('gluten_free',  'Sin gluten'),
    ('lactose_free', 'Sin lactosa'),
    ('low_sugar',    'Bajo en azúcar'),
  ];
  static const _allergens = [
    ('peanut',    'Maní'),
    ('gluten',    'Gluten'),
    ('lactose',   'Lactosa'),
    ('soy',       'Soja'),
    ('shellfish', 'Mariscos'),
    ('egg',       'Huevo'),
  ];
  static const _categories = [
    ('main',     'Plato fuerte'),
    ('starter',  'Entrada'),
    ('side',     'Acompañante'),
    ('drink',    'Bebida'),
    ('dessert',  'Postre'),
    ('sauce',    'Salsa'),
  ];

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        children: [
          Text('Cuéntanos cómo te gusta comer',
              style: Theme.of(context).textTheme.displaySmall,
              textAlign: TextAlign.center),
          const SizedBox(height: 4),
          Text('Filtramos el menú con esto. Lo cambias después.',
              style: Theme.of(context).textTheme.bodyMedium,
              textAlign: TextAlign.center),
          const SizedBox(height: 16),
          Expanded(
            child: ListView(
              children: [
                _section(
                  context,
                  title: 'Preferencias',
                  options: _dietary,
                  selected: data.dietary,
                  onToggle: (v, on) {
                    final next = data.dietary.toList();
                    on ? next.add(v) : next.remove(v);
                    onChanged((d) => d.copyWith(dietary: next));
                  },
                ),
                _section(
                  context,
                  title: 'Alergias',
                  options: _allergens,
                  selected: data.allergens,
                  onToggle: (v, on) {
                    final next = data.allergens.toList();
                    on ? next.add(v) : next.remove(v);
                    onChanged((d) => d.copyWith(allergens: next));
                  },
                ),
                _section(
                  context,
                  title: 'Categorías favoritas',
                  options: _categories,
                  selected: data.favoriteCategories,
                  onToggle: (v, on) {
                    final next = data.favoriteCategories.toList();
                    on ? next.add(v) : next.remove(v);
                    onChanged((d) => d.copyWith(favoriteCategories: next));
                  },
                ),
              ],
            ),
          ),
          ElevatedButton(
            onPressed: onNext,
            child: const Text('Continuar'),
          ),
          const SizedBox(height: 8),
        ],
      ),
    );
  }

  Widget _section(
    BuildContext context, {
    required String title,
    required List<(String, String)> options,
    required List<String> selected,
    required void Function(String value, bool selected) onToggle,
  }) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(title, style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 8),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: options.map((entry) {
              final on = selected.contains(entry.$1);
              return FilterChip(
                label: Text(entry.$2),
                selected: on,
                onSelected: (v) => onToggle(entry.$1, v),
              );
            }).toList(),
          ),
        ],
      ),
    );
  }
}
