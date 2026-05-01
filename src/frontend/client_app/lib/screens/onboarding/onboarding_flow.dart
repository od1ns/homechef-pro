import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';

import '../../app_state.dart';
import 'welcome_step.dart';
import 'location_step.dart';
import 'preferences_step.dart';
import 'loyalty_step.dart';

class OnboardingFlow extends StatefulWidget {
  final AppState state;
  final OnboardingData initial;
  final VoidCallback onCompleted;

  const OnboardingFlow({
    super.key,
    required this.state,
    required this.initial,
    required this.onCompleted,
  });

  @override
  State<OnboardingFlow> createState() => _OnboardingFlowState();
}

class _OnboardingFlowState extends State<OnboardingFlow> {
  final _pageCtrl = PageController();
  int _index = 0;

  late OnboardingData _data = widget.initial;
  late final OnboardingState _store = OnboardingState();

  void _update(OnboardingData Function(OnboardingData) f) {
    setState(() => _data = f(_data));
  }

  Future<void> _persist() async {
    await _store.write(_data);
  }

  Future<void> _next() async {
    if (_index < 3) {
      await _persist();
      _pageCtrl.animateToPage(
        _index + 1,
        duration: const Duration(milliseconds: 300),
        curve: Curves.easeOutCubic,
      );
    } else {
      await _finish();
    }
  }

  Future<void> _back() async {
    if (_index > 0) {
      _pageCtrl.animateToPage(
        _index - 1,
        duration: const Duration(milliseconds: 300),
        curve: Curves.easeOutCubic,
      );
    }
  }

  Future<void> _skip() async {
    await _store.write(_data);
    await _store.markCompleted(completed: true);
    widget.onCompleted();
  }

  Future<void> _finish() async {
    await _store.write(_data);
    await _store.markCompleted(completed: true);
    widget.onCompleted();
  }

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    final t = widget.state.strings;
    return Scaffold(
      backgroundColor: palette.bg,
      body: SafeArea(
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
              child: Row(
                children: [
                  if (_index > 0)
                    IconButton(
                        icon: const Icon(Icons.arrow_back),
                        onPressed: _back)
                  else
                    const SizedBox(width: 48),
                  Expanded(
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: List.generate(4, (i) {
                        final filled = i <= _index;
                        return Container(
                          width: 24,
                          height: 4,
                          margin: const EdgeInsets.symmetric(horizontal: 3),
                          decoration: BoxDecoration(
                            color: filled ? palette.accent : palette.line,
                            borderRadius: BorderRadius.circular(2),
                          ),
                        );
                      }),
                    ),
                  ),
                  TextButton(
                    onPressed: _skip,
                    child: Text(t.t('common.cancel') == 'Cancelar'
                        ? 'Saltar'
                        : 'Skip'),
                  ),
                ],
              ),
            ),
            Expanded(
              child: PageView(
                controller: _pageCtrl,
                onPageChanged: (i) => setState(() => _index = i),
                physics: const NeverScrollableScrollPhysics(),
                children: [
                  WelcomeStep(state: widget.state, onNext: _next),
                  LocationStep(
                    state: widget.state,
                    initialAddress: _data.defaultAddress,
                    onChanged: (addr) =>
                        _update((d) => d.copyWith(defaultAddress: addr)),
                    onNext: _next,
                  ),
                  PreferencesStep(
                    state: widget.state,
                    data: _data,
                    onChanged: _update,
                    onNext: _next,
                  ),
                  LoyaltyStep(
                    state: widget.state,
                    wantsUpdates: _data.wantsLoyaltyUpdates,
                    onToggle: (v) =>
                        _update((d) => d.copyWith(wantsLoyaltyUpdates: v)),
                    onFinish: _finish,
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
