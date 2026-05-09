import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:homechef_shared/homechef_shared.dart';

/// Lightweight app-wide state. ChangeNotifier es la opcion de menor friccion
/// cuando no se necesita plumbing completo de state-management todavia.
class AppState extends ChangeNotifier {
  final HcpApi api;
  final LocalOrderStore orderStore;

  HcpThemeName _theme = HcpThemeName.editorial;
  HcpLang _lang = HcpLang.es;
  double _fontScale = 1.0;
  final List<CartLine> _cart = [];
  CustomerSession? _session;

  AppState({required this.api, LocalOrderStore? orderStore})
      : orderStore = orderStore ?? LocalOrderStore();

  CustomerSession? get session => _session;
  bool get isLoggedIn => _session != null;

  HcpThemeName get theme => _theme;
  HcpLang get lang => _lang;
  HcpStrings get strings => HcpStrings.of(_lang);
  double get fontScale => _fontScale;
  List<CartLine> get cart => List.unmodifiable(_cart);

  double get cartSubtotal =>
      _cart.fold(0.0, (sum, l) => sum + l.unitPriceUsd * l.quantity);

  void setTheme(HcpThemeName t) {
    _theme = t;
    notifyListeners();
  }

  void setLang(HcpLang l) {
    _lang = l;
    notifyListeners();
  }

  void setFontScale(double s) {
    _fontScale = s.clamp(0.85, 1.30);
    notifyListeners();
  }

  /// Etapa 2: addToCart recibe modificadores opcionales.
  /// Dos CartLines del mismo plato se consolidan solo si tienen el mismo
  /// set de modificadores (mismo modifiersKey). Si difieren = lineas separadas
  /// (misma hamburguesa con distinta personalizacion = productos distintos).
  void addToCart(
    RecipeSummary dish, {
    int quantity = 1,
    String? notes,
    List<CartLineModifier> modifiers = const [],
  }) {
    final key = _modifiersKey(modifiers);
    final existing = _cart.indexWhere(
      (l) => l.dish.id == dish.id && l.modifiersKey == key,
    );

    final modDelta = modifiers.fold(0.0, (sum, m) => sum + m.lineDelta);
    final unitPrice = (dish.sellingPriceUsd ?? 0) + modDelta;

    if (existing >= 0) {
      _cart[existing] = _cart[existing]
          .withQuantity(_cart[existing].quantity + quantity);
    } else {
      _cart.add(CartLine(
        dish: dish,
        quantity: quantity,
        unitPriceUsd: unitPrice,
        notes: notes,
        modifiers: modifiers,
      ));
    }
    notifyListeners();
  }

  void removeFromCart(String cartLineKey) {
    _cart.removeWhere((l) => l.lineKey == cartLineKey);
    notifyListeners();
  }

  void clearCart() {
    _cart.clear();
    notifyListeners();
  }

  /// Clave estable de modificadores: IDs ordenados + cantidades.
  /// Usada para saber si dos lineas del mismo plato son "el mismo producto".
  static String _modifiersKey(List<CartLineModifier> mods) {
    if (mods.isEmpty) return '';
    final sorted = [...mods]
      ..sort((a, b) => a.modifier.id.compareTo(b.modifier.id));
    return sorted.map((m) => '${m.modifier.id}:${m.quantity}').join('|');
  }

  /// F-24: persiste el orderId Y el accessToken anti-IDOR retornado por el backend.
  Future<void> recordPlacedOrder(
      String orderId, String accessToken, String guestName) =>
      orderStore.add(LocalOrderRef(
        orderId: orderId,
        accessToken: accessToken,
        guestName: guestName,
        placedAt: DateTime.now(),
      ));

  Future<void> forgetOrder(String orderId) => orderStore.remove(orderId);

  Future<void> restoreSession() async {
    final stored = await api.auth.readSession();
    if (stored != null && stored.expiresAt.isAfter(DateTime.now())) {
      _session = CustomerSession(
        userId: stored.userId,
        email: stored.email,
        roles: stored.roles,
      );
      notifyListeners();
    }
  }

  Future<void> setSessionFromAuth(AuthResult auth) async {
    _session = CustomerSession(
      userId: auth.userId,
      email: auth.email,
      roles: auth.roles,
      fullName: auth.fullName,
    );
    notifyListeners();
    unawaited(_syncPreferencesAfterLogin());
  }

  Future<void> _syncPreferencesAfterLogin() async {
    final store = OnboardingState();
    try {
      final remote = await api.getMyPreferences();
      final local = await store.read();
      final localUpdated = await store.lastUpdated()
          ?? DateTime.fromMillisecondsSinceEpoch(0);
      final remoteUpdated = remote.updatedAt
          ?? DateTime.fromMillisecondsSinceEpoch(0);

      if (remote.payload.isEmpty && _hasLocalData(local)) {
        await api.putMyPreferences(local.toJson());
      } else if (remoteUpdated.isAfter(localUpdated) && remote.payload.isNotEmpty) {
        await store.write(OnboardingData.fromJson(remote.payload));
      } else if (localUpdated.isAfter(remoteUpdated) && _hasLocalData(local)) {
        await api.putMyPreferences(local.toJson());
      }
    } catch (_) {/* offline / 401 — silencioso */}
  }

  bool _hasLocalData(OnboardingData d) =>
      d.dietary.isNotEmpty
      || d.allergens.isNotEmpty
      || d.favoriteCategories.isNotEmpty
      || (d.defaultAddress?.isNotEmpty ?? false)
      || d.wantsLoyaltyUpdates;

  Future<void> clearSession() async {
    await api.logout();
    _session = null;
    notifyListeners();
  }
}

class CustomerSession {
  final String userId;
  final String email;
  final List<String> roles;
  final String? fullName;
  const CustomerSession({
    required this.userId,
    required this.email,
    required this.roles,
    this.fullName,
  });
}

class CartLine {
  final RecipeSummary dish;
  final int quantity;
  final double unitPriceUsd;
  final String? notes;
  final List<CartLineModifier> modifiers; // Etapa 2

  const CartLine({
    required this.dish,
    required this.quantity,
    required this.unitPriceUsd,
    this.notes,
    this.modifiers = const [],
  });

  /// Clave unica de la linea: dishId + hash de modificadores.
  String get lineKey => '${dish.id}|${modifiersKey}';

  String get modifiersKey => AppState._modifiersKey(modifiers);

  CartLine withQuantity(int q) => CartLine(
        dish: dish,
        quantity: q,
        unitPriceUsd: unitPriceUsd,
        notes: notes,
        modifiers: modifiers,
      );

  double get lineTotal => unitPriceUsd * quantity;
}
