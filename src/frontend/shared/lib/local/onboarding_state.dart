import 'dart:convert';

import 'package:shared_preferences/shared_preferences.dart';

/// Persists the customer's onboarding answers locally. None of these values are
/// sent to the backend yet — when there's a customer-profile endpoint they will
/// be uploaded there. Living locally also means the data goes away with an
/// "uninstall", which is the right behavior for an anonymous guest.
class OnboardingState {
  static const _kCompleted = 'hcp.onboarding.completed.v1';
  static const _kData = 'hcp.onboarding.data.v1';
  static const _kUpdatedAt = 'hcp.onboarding.data.updatedAt.v1';

  Future<bool> isCompleted() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getBool(_kCompleted) ?? false;
  }

  Future<void> markCompleted({required bool completed}) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setBool(_kCompleted, completed);
  }

  Future<OnboardingData> read() async {
    final prefs = await SharedPreferences.getInstance();
    final raw = prefs.getString(_kData);
    if (raw == null || raw.isEmpty) return const OnboardingData();
    return OnboardingData.fromJson(jsonDecode(raw) as Map<String, dynamic>);
  }

  Future<DateTime?> lastUpdated() async {
    final prefs = await SharedPreferences.getInstance();
    final raw = prefs.getString(_kUpdatedAt);
    return raw == null ? null : DateTime.tryParse(raw);
  }

  Future<void> write(OnboardingData data) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_kData, jsonEncode(data.toJson()));
    await prefs.setString(_kUpdatedAt, DateTime.now().toIso8601String());
  }

  Future<void> reset() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_kCompleted);
    await prefs.remove(_kData);
    await prefs.remove(_kUpdatedAt);
  }
}

class OnboardingData {
  final String? defaultAddress;
  final List<String> dietary;        // 'vegetarian' | 'vegan' | 'gluten_free' | 'lactose_free' | 'low_sugar'
  final List<String> allergens;      // 'peanut' | 'gluten' | 'lactose' | 'soy' | 'shellfish' | 'egg'
  final List<String> favoriteCategories;  // 'main' | 'starter' | 'drink' | 'dessert'
  final bool wantsLoyaltyUpdates;

  const OnboardingData({
    this.defaultAddress,
    this.dietary = const [],
    this.allergens = const [],
    this.favoriteCategories = const [],
    this.wantsLoyaltyUpdates = false,
  });

  OnboardingData copyWith({
    String? defaultAddress,
    List<String>? dietary,
    List<String>? allergens,
    List<String>? favoriteCategories,
    bool? wantsLoyaltyUpdates,
  }) =>
      OnboardingData(
        defaultAddress: defaultAddress ?? this.defaultAddress,
        dietary: dietary ?? this.dietary,
        allergens: allergens ?? this.allergens,
        favoriteCategories: favoriteCategories ?? this.favoriteCategories,
        wantsLoyaltyUpdates: wantsLoyaltyUpdates ?? this.wantsLoyaltyUpdates,
      );

  Map<String, dynamic> toJson() => {
        if (defaultAddress != null) 'defaultAddress': defaultAddress,
        'dietary': dietary,
        'allergens': allergens,
        'favoriteCategories': favoriteCategories,
        'wantsLoyaltyUpdates': wantsLoyaltyUpdates,
      };

  factory OnboardingData.fromJson(Map<String, dynamic> j) => OnboardingData(
        defaultAddress: j['defaultAddress'] as String?,
        dietary: ((j['dietary'] as List<dynamic>?) ?? const [])
            .map((e) => e as String)
            .toList(),
        allergens: ((j['allergens'] as List<dynamic>?) ?? const [])
            .map((e) => e as String)
            .toList(),
        favoriteCategories: ((j['favoriteCategories'] as List<dynamic>?) ?? const [])
            .map((e) => e as String)
            .toList(),
        wantsLoyaltyUpdates: j['wantsLoyaltyUpdates'] as bool? ?? false,
      );
}
