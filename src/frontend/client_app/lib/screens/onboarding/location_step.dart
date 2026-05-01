import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';

import '../../app_state.dart';

class LocationStep extends StatefulWidget {
  final AppState state;
  final String? initialAddress;
  final ValueChanged<String?> onChanged;
  final VoidCallback onNext;
  const LocationStep({
    super.key,
    required this.state,
    required this.initialAddress,
    required this.onChanged,
    required this.onNext,
  });

  @override
  State<LocationStep> createState() => _LocationStepState();
}

class _LocationStepState extends State<LocationStep> {
  late final TextEditingController _ctrl =
      TextEditingController(text: widget.initialAddress ?? '');

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    return Padding(
      padding: const EdgeInsets.all(32),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const SizedBox(height: 16),
          Icon(Icons.location_on_outlined, size: 64, color: palette.accent),
          const SizedBox(height: 16),
          Text('¿Dónde te encuentras?',
              style: Theme.of(context).textTheme.displaySmall,
              textAlign: TextAlign.center),
          const SizedBox(height: 8),
          Text(
            'Lo usamos para sugerirte si te conviene retiro en local o delivery, '
            'y para llenar tus pedidos más rápido. No la compartimos con nadie.',
            style: Theme.of(context).textTheme.bodyMedium,
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 24),
          TextField(
            controller: _ctrl,
            decoration: const InputDecoration(
              labelText: 'Dirección por defecto (opcional)',
              hintText: 'Av Principal, Edificio X, Apto Y, Caracas',
              prefixIcon: Icon(Icons.home_outlined),
            ),
            maxLines: 3,
            onChanged: (v) =>
                widget.onChanged(v.trim().isEmpty ? null : v.trim()),
          ),
          const Spacer(),
          ElevatedButton(
            onPressed: widget.onNext,
            child: const Text('Continuar'),
          ),
          const SizedBox(height: 12),
          TextButton(
            onPressed: widget.onNext,
            child: const Text('Lo dejo para luego'),
          ),
        ],
      ),
    );
  }
}
