import 'package:flutter/material.dart';

/// 5 paletas: 4 originales del design handoff + Caracas (moderna, tropical).
/// Switch by passing the chosen palette to [hcpThemeData].
enum HcpThemeName { plum, paprika, caribbean, noche, caracas, editorial }

class HcpPalette {
  final Color bg, card;
  final Color ink, inkSoft, inkMuted;
  final Color line;
  final Color accent, accentDark;
  final Color green, greenSoft;
  final Color sun;
  final Color red, redSoft;
  final Color sidebar, sidebarText, sidebarMuted;
  final bool isDark;

  const HcpPalette({
    required this.bg,
    required this.card,
    required this.ink,
    required this.inkSoft,
    required this.inkMuted,
    required this.line,
    required this.accent,
    required this.accentDark,
    required this.green,
    required this.greenSoft,
    required this.sun,
    required this.red,
    required this.redSoft,
    required this.sidebar,
    required this.sidebarText,
    required this.sidebarMuted,
    required this.isDark,
  });

  static const plum = HcpPalette(
    bg: Color(0xFFF4F1EC), card: Colors.white,
    ink: Color(0xFF2A1F3D), inkSoft: Color(0xFF6B5F7A), inkMuted: Color(0xFFA89EB4),
    line: Color(0xFFE5DFE8),
    accent: Color(0xFF7B4FB8), accentDark: Color(0xFF5E3A93),
    green: Color(0xFF3D6B5C), greenSoft: Color(0xFFDCE8E2),
    sun: Color(0xFFC8A8D4),
    red: Color(0xFFB5463E), redSoft: Color(0xFFF3E0DD),
    sidebar: Color(0xFF2A1F3D), sidebarText: Color(0xFFE0D8EC), sidebarMuted: Color(0xFF8A7DA0),
    isDark: false,
  );

  static const paprika = HcpPalette(
    bg: Color(0xFFF6EFE4), card: Colors.white,
    ink: Color(0xFF2D1B12), inkSoft: Color(0xFF6E544A), inkMuted: Color(0xFFB29B8F),
    line: Color(0xFFEDE1D3),
    accent: Color(0xFFC14D2A), accentDark: Color(0xFF8F3418),
    green: Color(0xFF5C6B3D), greenSoft: Color(0xFFE2E8DC),
    sun: Color(0xFFE8B66A),
    red: Color(0xFFA13B2E), redSoft: Color(0xFFF3DDD8),
    sidebar: Color(0xFF2D1B12), sidebarText: Color(0xFFF0E2D4), sidebarMuted: Color(0xFF9A8478),
    isDark: false,
  );

  static const caribbean = HcpPalette(
    bg: Color(0xFFEEF4F3), card: Colors.white,
    ink: Color(0xFF0F2E2B), inkSoft: Color(0xFF4E6E6A), inkMuted: Color(0xFF9FB4B1),
    line: Color(0xFFD9E4E2),
    accent: Color(0xFFE26D5C), accentDark: Color(0xFFB0493A),
    green: Color(0xFF1E7A6B), greenSoft: Color(0xFFD4E7E3),
    sun: Color(0xFFF4C06B),
    red: Color(0xFFC84B3E), redSoft: Color(0xFFF5DDD8),
    sidebar: Color(0xFF0F2E2B), sidebarText: Color(0xFFD4E7E3), sidebarMuted: Color(0xFF7A9A95),
    isDark: false,
  );

  static const noche = HcpPalette(
    bg: Color(0xFF18161E), card: Color(0xFF22202A),
    ink: Color(0xFFF0ECE4), inkSoft: Color(0xFFB5AFA4), inkMuted: Color(0xFF6E6A63),
    line: Color(0xFF2E2B37),
    accent: Color(0xFFD4A74E), accentDark: Color(0xFFB0862F),
    green: Color(0xFF6CA088), greenSoft: Color(0xFF2D3833),
    sun: Color(0xFFE8C97A),
    red: Color(0xFFD46C5E), redSoft: Color(0xFF3A2622),
    sidebar: Color(0xFF0F0E14), sidebarText: Color(0xFFE8E2D5), sidebarMuted: Color(0xFF787263),
    isDark: true,
  );

  /// Caracas: tropical venezolana — rojo coral profundo + verde esmeralda jugoso +
  /// amarillo plátano dorado. Saturación alta. Inspirada en culturalmente local
  /// (arepas, plátano, salsa). Default del cliente desde 2026-05-04.
  static const caracas = HcpPalette(
    bg: Color(0xFFFEFBF6), card: Colors.white,
    ink: Color(0xFF1D1A1A), inkSoft: Color(0xFF5A4F4F), inkMuted: Color(0xFF9C8E8E),
    line: Color(0xFFEDE3DE),
    accent: Color(0xFFE63946), accentDark: Color(0xFFB22222),  // rojo coral
    green: Color(0xFF06A77D), greenSoft: Color(0xFFD4F0E5),    // esmeralda jugoso
    sun: Color(0xFFFFD60A),                                     // amarillo plátano
    red: Color(0xFFC1121F), redSoft: Color(0xFFFFE5E8),
    sidebar: Color(0xFF1D1A1A), sidebarText: Color(0xFFFFE5E8), sidebarMuted: Color(0xFF7A6A6A),
    isDark: false,
  );

  /// Editorial: paleta hibrida inspirada en Refero + Linear + Aesop. Neutros calidos
  /// como base, coral #E63946 como UNICO accent fuerte (CTAs, indicadores). Esmeralda
  /// solo en estados positivos puntuales. Whitespace dramatico, tipografia serif
  /// protagonista. Default del cliente desde 2026-05-04.
  static const editorial = HcpPalette(
    bg: Color(0xFFFAF7F2), card: Colors.white,
    ink: Color(0xFF1A1614), inkSoft: Color(0xFF6E635A), inkMuted: Color(0xFFB0A89E),
    line: Color(0xFFECE5DC),
    accent: Color(0xFFE63946), accentDark: Color(0xFFB52A35),
    green: Color(0xFF06A77D), greenSoft: Color(0xFFDDF0E7),
    sun: Color(0xFFE8B05C),
    red: Color(0xFFC1121F), redSoft: Color(0xFFFCEBED),
    sidebar: Color(0xFF1A1614), sidebarText: Color(0xFFF5F1EA), sidebarMuted: Color(0xFF8A7E72),
    isDark: false,
  );

  static HcpPalette of(HcpThemeName name) => switch (name) {
        HcpThemeName.plum => plum,
        HcpThemeName.paprika => paprika,
        HcpThemeName.caribbean => caribbean,
        HcpThemeName.noche => noche,
        HcpThemeName.caracas => caracas,
        HcpThemeName.editorial => editorial,
      };
}

/// Builds a Material 3 [ThemeData] honoring the design handoff's typography
/// (Instrument Serif display, Inter UI, JetBrains Mono numerals) and the chosen
/// palette. Uses [ColorScheme.fromSeed] to generate the full M3 token set
/// (primaryContainer, tertiary, surfaceContainer, etc.) and overlays the explicit
/// colors from the palette for backwards compatibility.
ThemeData hcpThemeData(HcpThemeName name) {
  final p = HcpPalette.of(name);

  // Generamos paleta M3 completa a partir del accent. fromSeed cubre los ~30
  // tokens de Material 3 (primaryContainer, secondaryContainer, tertiary,
  // surfaceContainerLow/High, etc.). Despues sobreescribimos los que tenemos
  // explicitos en la palette para mantener brand consistency.
  final scheme = ColorScheme.fromSeed(
    seedColor: p.accent,
    brightness: p.isDark ? Brightness.dark : Brightness.light,
  ).copyWith(
    primary: p.accent,
    onPrimary: Colors.white,
    secondary: p.green,
    onSecondary: Colors.white,
    error: p.red,
    onError: Colors.white,
    surface: p.card,
    onSurface: p.ink,
  );

  const display = 'Instrument Serif';
  const body = 'Inter';
  const mono = 'JetBrains Mono';

  return ThemeData(
    useMaterial3: true,  // F-21B: opt-in a Material 3
    colorScheme: scheme,
    scaffoldBackgroundColor: p.bg,
    fontFamily: body,
    textTheme: TextTheme(
      // Editorial: serif protagonista. Tighter letter-spacing y line-height para feel premium.
      displayLarge: TextStyle(fontFamily: display, fontSize: 56, height: 1.05, color: p.ink, letterSpacing: -0.02),
      displayMedium: TextStyle(fontFamily: display, fontSize: 40, height: 1.05, color: p.ink, letterSpacing: -0.02),
      displaySmall: TextStyle(fontFamily: display, fontSize: 30, height: 1.1, color: p.ink, letterSpacing: -0.02),
      headlineMedium: TextStyle(fontFamily: display, fontSize: 22, height: 1.1, color: p.ink, letterSpacing: -0.01),
      titleLarge: TextStyle(fontFamily: body, fontSize: 20, fontWeight: FontWeight.w600, color: p.ink),
      titleMedium: TextStyle(fontFamily: body, fontSize: 16, fontWeight: FontWeight.w500, color: p.ink),
      bodyLarge: TextStyle(fontFamily: body, fontSize: 16, color: p.ink, height: 1.4),
      bodyMedium: TextStyle(fontFamily: body, fontSize: 14, color: p.ink, height: 1.4),
      bodySmall: TextStyle(fontFamily: body, fontSize: 13, color: p.inkSoft, height: 1.4),
      labelLarge: TextStyle(fontFamily: body, fontSize: 14, fontWeight: FontWeight.w600, color: p.ink),
      labelMedium: TextStyle(fontFamily: mono, fontSize: 14, color: p.ink),
      labelSmall: TextStyle(fontFamily: mono, fontSize: 12, color: p.inkSoft),
    ),
    cardTheme: CardThemeData(
      color: p.card,
      elevation: 0,
      surfaceTintColor: p.accent.withValues(alpha: 0.03),  // M3 tonal elevation suave
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(28)),  // Editorial: bordes generosos
      margin: EdgeInsets.zero,
    ),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: p.card,
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(14),
        borderSide: BorderSide(color: p.line),
      ),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(14),
        borderSide: BorderSide(color: p.line),
      ),
      focusedBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(14),
        borderSide: BorderSide(color: p.accent, width: 2),
      ),
    ),
    // M3: FilledButton es el default. Se mantiene ElevatedButton legacy para
    // codigo existente, pero pantallas nuevas deberian usar FilledButton.
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(
        backgroundColor: p.accent,
        foregroundColor: Colors.white,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
        padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
        textStyle: const TextStyle(fontFamily: body, fontSize: 16, fontWeight: FontWeight.w600),
      ),
    ),
    elevatedButtonTheme: ElevatedButtonThemeData(
      style: ElevatedButton.styleFrom(
        backgroundColor: p.accent,
        foregroundColor: Colors.white,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
        padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
        textStyle: const TextStyle(fontFamily: body, fontSize: 16, fontWeight: FontWeight.w600),
      ),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        foregroundColor: p.accent,
        side: BorderSide(color: p.accent.withValues(alpha: 0.4)),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 14),
        textStyle: const TextStyle(fontFamily: body, fontSize: 15, fontWeight: FontWeight.w600),
      ),
    ),
    chipTheme: ChipThemeData(
      backgroundColor: p.card,
      selectedColor: p.accent.withValues(alpha: 0.12),
      side: BorderSide(color: p.line),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
      labelStyle: TextStyle(fontFamily: body, fontSize: 13, fontWeight: FontWeight.w500, color: p.ink),
    ),
    // Editorial: NavigationBar minimal — sin pildora grande, solo cambio de color.
    // El dot indicator coral lo dibujamos manualmente en cada NavigationDestination
    // del widget (ver app.dart). Aca solo configuramos colores y tipografia.
    navigationBarTheme: NavigationBarThemeData(
      backgroundColor: p.card,
      indicatorColor: Colors.transparent,  // sin pildora tonal — usamos dot custom
      surfaceTintColor: Colors.transparent,
      shadowColor: Colors.transparent,
      elevation: 0,
      labelTextStyle: WidgetStateProperty.resolveWith((states) {
        final selected = states.contains(WidgetState.selected);
        return TextStyle(
          fontFamily: body,
          fontSize: 11,
          fontWeight: selected ? FontWeight.w500 : FontWeight.w400,
          letterSpacing: 0.02,
          color: selected ? p.ink : p.inkMuted,
        );
      }),
      iconTheme: WidgetStateProperty.resolveWith((states) {
        final selected = states.contains(WidgetState.selected);
        return IconThemeData(color: selected ? p.ink : p.inkMuted, size: 22);
      }),
      height: 68,
    ),
    // Mantenemos el theme legacy de BottomNavigationBar para pantallas que aun
    // no se migraron. Pantallas nuevas deberian usar NavigationBar.
    bottomNavigationBarTheme: BottomNavigationBarThemeData(
      backgroundColor: p.card,
      selectedItemColor: p.accent,
      unselectedItemColor: p.inkMuted,
      type: BottomNavigationBarType.fixed,
    ),
    appBarTheme: AppBarTheme(
      backgroundColor: p.bg,  // Editorial: appbar se funde con el bg, no card
      foregroundColor: p.ink,
      elevation: 0,
      scrolledUnderElevation: 0,  // sin shadow al hacer scroll, mas editorial
      centerTitle: false,
      titleTextStyle: TextStyle(
        fontFamily: display, fontSize: 22, height: 1.1, color: p.ink, letterSpacing: -0.02,
      ),
      iconTheme: IconThemeData(color: p.ink, size: 22),
    ),
    extensions: [HcpThemeExtension(palette: p)],
  );
}

/// Access the full palette inside widgets:
/// `final p = Theme.of(context).extension<HcpThemeExtension>()!.palette;`
class HcpThemeExtension extends ThemeExtension<HcpThemeExtension> {
  final HcpPalette palette;
  const HcpThemeExtension({required this.palette});

  @override
  HcpThemeExtension copyWith({HcpPalette? palette}) =>
      HcpThemeExtension(palette: palette ?? this.palette);

  @override
  HcpThemeExtension lerp(ThemeExtension<HcpThemeExtension>? other, double t) => this;
}
