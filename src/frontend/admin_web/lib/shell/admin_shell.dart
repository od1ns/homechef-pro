import 'package:flutter/material.dart';
import '../screens/security_2fa_screen.dart';
import 'package:homechef_shared/homechef_shared.dart';

import '../screens/analytics_screen.dart';
import '../screens/inventory_screen.dart';
import '../screens/invoices_screen.dart';
import '../screens/live_orders_screen.dart';
import '../screens/overview_screen.dart';
import '../screens/placeholder_screen.dart';
import '../screens/purchasing_screen.dart';
import '../screens/recipes_screen.dart';

/// Sidebar layout. The design handoff §4.5 calls for a 240px sidebar at
/// ≥1280px and an icon-only collapsed rail between 768–1279px. Below 768px
/// the chef would normally use the mobile admin app, but here we still
/// degrade to a single column with a top app bar.
class AdminShell extends StatefulWidget {
  final HcpApi api;
  final String fullName;
  final List<String> roles;
  final Future<void> Function() onLogout;

  const AdminShell({
    super.key,
    required this.api,
    required this.fullName,
    required this.roles,
    required this.onLogout,
  });

  @override
  State<AdminShell> createState() => _AdminShellState();
}

class _AdminShellState extends State<AdminShell> {
  int _section = 0;

  late final List<_Section> _sections = [
    _Section('Resumen', Icons.bar_chart_outlined,
        OverviewScreen(api: widget.api)),
    _Section('Órdenes en vivo', Icons.receipt_long_outlined,
        LiveOrdersScreen(api: widget.api)),
    _Section('Menú y recetas', Icons.menu_book_outlined,
        RecipesScreen(api: widget.api)),
    _Section('Inventario', Icons.inventory_2_outlined,
        InventoryScreen(api: widget.api)),
    _Section('Compras', Icons.shopping_cart_outlined,
        PurchasingScreen(api: widget.api)),
    _Section('Analítica', Icons.insights_outlined,
        AnalyticsScreen(api: widget.api)),
    _Section('Facturas', Icons.description_outlined,
        InvoicesScreen(api: widget.api)),
    _Section('Seguridad / 2FA', Icons.security_outlined,
        Security2faScreen(api: widget.api)),
  ];

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    final width = MediaQuery.of(context).size.width;
    final extended = width >= 1280;
    final collapsed = width >= 768 && width < 1280;
    final mobile = width < 768;

    final body = _sections[_section].child;

    if (mobile) {
      return Scaffold(
        appBar: AppBar(
          title: Text(_sections[_section].label),
          actions: [
            IconButton(icon: const Icon(Icons.logout), onPressed: widget.onLogout),
          ],
        ),
        drawer: _MobileDrawer(
          sections: _sections,
          current: _section,
          fullName: widget.fullName,
          roles: widget.roles,
          onSelect: (i) {
            setState(() => _section = i);
            Navigator.of(context).pop();
          },
          onLogout: widget.onLogout,
        ),
        body: body,
      );
    }

    return Scaffold(
      body: Row(
        children: [
          Container(
            width: extended ? 240 : 72,
            color: palette.sidebar,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const SizedBox(height: 24),
                Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 20),
                  child: extended
                      ? Text('HomeChef Pro',
                          style: TextStyle(
                              color: palette.sidebarText,
                              fontFamily: 'Instrument Serif',
                              fontSize: 22))
                      : const Center(
                          child: Text('🍳', style: TextStyle(fontSize: 24))),
                ),
                const SizedBox(height: 24),
                for (var i = 0; i < _sections.length; i++)
                  _SidebarItem(
                    section: _sections[i],
                    selected: _section == i,
                    extended: extended,
                    palette: palette,
                    onTap: () => setState(() => _section = i),
                  ),
                const Spacer(),
                if (extended) ...[
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 20),
                    child: Text(widget.fullName,
                        style: TextStyle(color: palette.sidebarText, fontWeight: FontWeight.w600),
                        overflow: TextOverflow.ellipsis),
                  ),
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 20),
                    child: Text(widget.roles.join(', '),
                        style: TextStyle(color: palette.sidebarMuted, fontSize: 12),
                        overflow: TextOverflow.ellipsis),
                  ),
                  const SizedBox(height: 8),
                ],
                Padding(
                  padding: const EdgeInsets.all(12),
                  child: TextButton.icon(
                    style: TextButton.styleFrom(foregroundColor: palette.sidebarText),
                    icon: const Icon(Icons.logout, size: 18),
                    label: extended
                        ? const Text('Cerrar sesión')
                        : const SizedBox.shrink(),
                    onPressed: widget.onLogout,
                  ),
                ),
              ],
            ),
          ),
          Expanded(
            child: Container(
              color: palette.bg,
              child: SafeArea(child: body),
            ),
          ),
        ],
      ),
    );
  }
}

class _Section {
  final String label;
  final IconData icon;
  final Widget child;
  _Section(this.label, this.icon, this.child);
}

class _SidebarItem extends StatelessWidget {
  final _Section section;
  final bool selected;
  final bool extended;
  final HcpPalette palette;
  final VoidCallback onTap;

  const _SidebarItem({
    required this.section,
    required this.selected,
    required this.extended,
    required this.palette,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      child: Container(
        margin: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 12),
        decoration: BoxDecoration(
          color: selected ? palette.accent : Colors.transparent,
          borderRadius: BorderRadius.circular(10),
        ),
        child: Row(
          children: [
            Icon(section.icon,
                size: 20,
                color: selected ? Colors.white : palette.sidebarText),
            if (extended) ...[
              const SizedBox(width: 12),
              Expanded(
                child: Text(section.label,
                    style: TextStyle(
                        color: selected ? Colors.white : palette.sidebarText,
                        fontWeight: selected ? FontWeight.w600 : FontWeight.w400),
                    overflow: TextOverflow.ellipsis),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _MobileDrawer extends StatelessWidget {
  final List<_Section> sections;
  final int current;
  final String fullName;
  final List<String> roles;
  final void Function(int) onSelect;
  final Future<void> Function() onLogout;

  const _MobileDrawer({
    required this.sections,
    required this.current,
    required this.fullName,
    required this.roles,
    required this.onSelect,
    required this.onLogout,
  });

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;
    return Drawer(
      backgroundColor: palette.sidebar,
      child: ListView(
        padding: EdgeInsets.zero,
        children: [
          DrawerHeader(
            decoration: BoxDecoration(color: palette.sidebar),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                Text('HomeChef Pro',
                    style: TextStyle(color: palette.sidebarText, fontSize: 20)),
                const SizedBox(height: 4),
                Text(fullName,
                    style: TextStyle(color: palette.sidebarText, fontWeight: FontWeight.w600)),
                Text(roles.join(', '),
                    style: TextStyle(color: palette.sidebarMuted, fontSize: 12)),
              ],
            ),
          ),
          for (var i = 0; i < sections.length; i++)
            ListTile(
              leading: Icon(sections[i].icon,
                  color: i == current ? palette.accent : palette.sidebarText),
              title: Text(sections[i].label,
                  style: TextStyle(color: palette.sidebarText)),
              selected: i == current,
              onTap: () => onSelect(i),
            ),
          ListTile(
            leading: Icon(Icons.logout, color: palette.sidebarText),
            title: Text('Cerrar sesión', style: TextStyle(color: palette.sidebarText)),
            onTap: () async {
              Navigator.of(context).pop();
              await onLogout();
            },
          ),
        ],
      ),
    );
  }
}
