import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';

import '../app_state.dart';

class ProfileScreen extends StatelessWidget {
  final AppState state;
  final VoidCallback onReplayOnboarding;
  const ProfileScreen({
    super.key,
    required this.state,
    required this.onReplayOnboarding,
  });

  @override
  Widget build(BuildContext context) {
    final t = state.strings;
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Text(t.t('tab.profile'),
            style: Theme.of(context).textTheme.displaySmall),
        const SizedBox(height: 16),
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: state.isLoggedIn
                ? Row(
                    children: [
                      CircleAvatar(
                        backgroundColor: palette.accent,
                        child: const Icon(Icons.person, color: Colors.white),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(state.session?.fullName ?? state.session?.email ?? 'Cuenta',
                                style: Theme.of(context).textTheme.titleMedium),
                            Text(state.session!.email,
                                style: Theme.of(context).textTheme.bodySmall),
                          ],
                        ),
                      ),
                      TextButton.icon(
                        icon: const Icon(Icons.logout, size: 18),
                        label: const Text('Salir'),
                        onPressed: () async {
                          await state.clearSession();
                        },
                      ),
                    ],
                  )
                : Row(
                    children: [
                      const Icon(Icons.account_circle_outlined, size: 32),
                      const SizedBox(width: 12),
                      const Expanded(
                          child: Text('Sin cuenta · pedidos como invitado')),
                    ],
                  ),
          ),
        ),
        const SizedBox(height: 16),
        Text('Tema', style: Theme.of(context).textTheme.titleMedium),
        Wrap(
          spacing: 8,
          children: HcpThemeName.values
              .map((n) => ChoiceChip(
                    label: Text(n.name),
                    selected: state.theme == n,
                    onSelected: (_) => state.setTheme(n),
                  ))
              .toList(),
        ),
        const SizedBox(height: 24),
        Text('Idioma', style: Theme.of(context).textTheme.titleMedium),
        Wrap(
          spacing: 8,
          children: HcpLang.values
              .map((l) => ChoiceChip(
                    label: Text(l.name == 'es' ? 'Español' : 'English'),
                    selected: state.lang == l,
                    onSelected: (_) => state.setLang(l),
                  ))
              .toList(),
        ),
        const SizedBox(height: 24),
        Text('Tamaño del texto',
            style: Theme.of(context).textTheme.titleMedium),
        Slider(
          value: state.fontScale,
          min: 0.85,
          max: 1.30,
          divisions: 9,
          label: '${(state.fontScale * 100).round()}%',
          onChanged: state.setFontScale,
        ),
        const SizedBox(height: 24),
        Text('Mis preferencias',
            style: Theme.of(context).textTheme.titleMedium),
        const SizedBox(height: 8),
        FutureBuilder<OnboardingData>(
          future: OnboardingState().read(),
          builder: (context, snap) {
            final data = snap.data ?? const OnboardingData();
            final hasAny = data.dietary.isNotEmpty ||
                data.allergens.isNotEmpty ||
                data.favoriteCategories.isNotEmpty ||
                (data.defaultAddress?.isNotEmpty ?? false);
            return Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    if (!hasAny)
                      const Text('Aún no nos contaste tus preferencias.')
                    else ...[
                      if (data.defaultAddress != null)
                        Text('Dirección: ${data.defaultAddress}'),
                      if (data.dietary.isNotEmpty)
                        _line('Preferencias', data.dietary),
                      if (data.allergens.isNotEmpty)
                        _line('Alergias', data.allergens),
                      if (data.favoriteCategories.isNotEmpty)
                        _line('Categorías favoritas', data.favoriteCategories),
                    ],
                    const SizedBox(height: 12),
                    Align(
                      alignment: Alignment.centerLeft,
                      child: TextButton.icon(
                        icon: const Icon(Icons.refresh, size: 16),
                        label: const Text('Volver a hacer onboarding'),
                        onPressed: onReplayOnboarding,
                      ),
                    ),
                  ],
                ),
              ),
            );
          },
        ),
      ],
    );
  }

  Widget _line(String label, List<String> items) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Text('$label: ${items.join(', ')}'),
    );
  }
}
