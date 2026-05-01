import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:homechef_shared/homechef_shared.dart';

import 'app_state.dart';
import 'screens/menu_screen.dart';
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

  @override
  void initState() {
    super.initState();
    widget.state.addListener(_onStateChanged);
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
    super.dispose();
  }

  void _onStateChanged() => setState(() {});

  @override
  Widget build(BuildContext context) {
    final s = widget.state;
    return MaterialApp(
      title: s.strings.t('app.name'),
      debugShowCheckedModeBanner: false,
      theme: hcpThemeData(s.theme),
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
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _tab,
        onTap: (i) => setState(() => _tab = i),
        items: [
          BottomNavigationBarItem(
              icon: const Icon(Icons.restaurant_outlined),
              activeIcon: const Icon(Icons.restaurant),
              label: t.t('tab.browse')),
          BottomNavigationBarItem(
              icon: const Icon(Icons.receipt_long_outlined),
              activeIcon: const Icon(Icons.receipt_long),
              label: t.t('tab.orders')),
          BottomNavigationBarItem(
              icon: const Icon(Icons.star_outline),
              activeIcon: const Icon(Icons.star),
              label: t.t('tab.reviews')),
          BottomNavigationBarItem(
              icon: const Icon(Icons.person_outline),
              activeIcon: const Icon(Icons.person),
              label: t.t('tab.profile')),
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
