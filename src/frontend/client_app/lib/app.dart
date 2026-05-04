import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:homechef_shared/homechef_shared.dart';

import 'app_state.dart';
import 'screens/menu_screen.dart';
import 'screens/loyalty_screen.dart';
import 'screens/orders_screen.dart';
import 'screens/reviews_screen.dart';
import 'screens/profile_screen.dart';
import 'screens/onboarding/onboarding_flow.dart';
import 'widgets/cart_drawer.dart';

class HomeChefClientApp extends StatefulWidget {
  final AppState state;
  const HomeChefClientApp({super.key, required this.state});

  @override
  State<HomeChefClientApp> createState() => _HomeChefClientAppState();
}

class _HomeChefClientAppState extends State<HomeChefClientApp> {
  bool? _onboarded;
  OnboardingData _data = const OnboardingData();
  final GlobalKey<ScaffoldMessengerState> _messengerKey = GlobalKey();

  late final void Function() _onUnauthorized = _handleUnauthorized;

  @override
  void initState() {
    super.initState();
    widget.state.addListener(_onStateChanged);
    widget.state.api.client.addUnauthorizedListener(_onUnauthorized);
    _checkOnboarding();
  }

  Future<void> _checkOnboarding() async {
    final store = OnboardingState();
    final completed = await store.isCompleted();
    final data = await store.read();
    await widget.state.restoreSession();
    if (mounted) {
      setState(() {
        _onboarded = completed;
        _data = data;
      });
    }
  }

  Future<void> _replayOnboarding() async {
    setState(() => _onboarded = false);
  }

  @override
  void dispose() {
    widget.state.removeListener(_onStateChanged);
    widget.state.api.client.removeUnauthorizedListener(_onUnauthorized);
    super.dispose();
  }

  void _onStateChanged() => setState(() {});

  /// Disparado cuando un endpoint del backend devuelve 401 (token expirado).
  /// El ApiClient ya limpio el storage; aca solo refrescamos la UI y
  /// notificamos al usuario. El cliente puede seguir navegando como guest;
  /// la pantalla de Perfil/Reviews va a mostrar el login otra vez.
  void _handleUnauthorized() {
    if (!mounted) return;
    setState(() {});
    _messengerKey.currentState?.showSnackBar(
      const SnackBar(
        content: Text('Tu sesion expiro. Por favor inicia sesion de nuevo.'),
        duration: Duration(seconds: 4),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final s = widget.state;
    return MaterialApp(
      title: s.strings.t('app.name'),
      debugShowCheckedModeBanner: false,
      theme: hcpThemeData(s.theme),
      scaffoldMessengerKey: _messengerKey,
      locale: Locale(s.lang.name),
      supportedLocales: const [Locale('es'), Locale('en')],
      localizationsDelegates: const [
        GlobalMaterialLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
      ],
      builder: (context, child) {
        return MediaQuery(
          data: MediaQuery.of(context).copyWith(
            textScaler: TextScaler.linear(s.fontScale),
          ),
          child: child!,
        );
      },
      home: switch (_onboarded) {
        null => const Scaffold(body: Center(child: CircularProgressIndicator())),
        false => OnboardingFlow(
            state: s,
            initial: _data,
            onCompleted: () => setState(() => _onboarded = true),
          ),
        true => HomeShell(state: s, onReplayOnboarding: _replayOnboarding),
      },
    );
  }
}

class HomeShell extends StatefulWidget {
  final AppState state;
  final VoidCallback onReplayOnboarding;
  const HomeShell({
    super.key,
    required this.state,
    required this.onReplayOnboarding,
  });

  @override
  State<HomeShell> createState() => _HomeShellState();
}

class _HomeShellState extends State<HomeShell> {
  int _tab = 0;

  @override
  Widget build(BuildContext context) {
    final t = widget.state.strings;
    final tabs = [
      MenuScreen(state: widget.state),
      OrdersScreen(state: widget.state),
      ReviewsScreen(state: widget.state),
      LoyaltyScreen(state: widget.state),
      ProfileScreen(
        state: widget.state,
        onReplayOnboarding: widget.onReplayOnboarding,
      ),
    ];

    return Scaffold(
      body: SafeArea(child: tabs[_tab]),
      floatingActionButton: widget.state.cart.isEmpty
          ? null
          : FloatingActionButton.extended(
              onPressed: () => _showCart(context),
              icon: const Icon(Icons.shopping_bag_outlined),
              label: Text(
                '${t.t('dish.viewCart')} '
                '(${widget.state.cart.fold(0, (s, l) => s + l.quantity)})',
              ),
            ),
      // F-21B: NavigationBar de Material 3 — animacion de pildora moderna,
      // mejor accesibilidad, alturas estandar M3.
      bottomNavigationBar: NavigationBar(
        selectedIndex: _tab,
        onDestinationSelected: (i) => setState(() => _tab = i),
        destinations: [
          NavigationDestination(
            icon: const Icon(Icons.restaurant_outlined),
            selectedIcon: const Icon(Icons.restaurant),
            label: t.t('tab.browse'),
          ),
          NavigationDestination(
            icon: const Icon(Icons.receipt_long_outlined),
            selectedIcon: const Icon(Icons.receipt_long),
            label: t.t('tab.orders'),
          ),
          NavigationDestination(
            icon: const Icon(Icons.star_outline),
            selectedIcon: const Icon(Icons.star),
            label: t.t('tab.reviews'),
          ),
          NavigationDestination(
            icon: const Icon(Icons.workspace_premium_outlined),
            selectedIcon: const Icon(Icons.workspace_premium),
            label: t.t('tab.sabor'),
          ),
          NavigationDestination(
            icon: const Icon(Icons.person_outline),
            selectedIcon: const Icon(Icons.person),
            label: t.t('tab.profile'),
          ),
        ],
      ),
    );
  }

  void _showCart(BuildContext context) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      builder: (_) => CartDrawer(state: widget.state),
    );
  }
}
