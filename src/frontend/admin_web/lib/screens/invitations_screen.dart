import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:homechef_shared/homechef_shared.dart';

/// Sesion A / Frente 1: gestion de codigos de invitacion.
/// Admin puede:
///  - Generar nuevos codigos (max_uses, expiresAt, notes opcionales).
///  - Listar (toggle activos / todos).
///  - Copiar al portapapeles.
///  - Revocar.
class InvitationsScreen extends StatefulWidget {
  final HcpApi api;
  const InvitationsScreen({super.key, required this.api});

  @override
  State<InvitationsScreen> createState() => _InvitationsScreenState();
}

class _InvitationsScreenState extends State<InvitationsScreen> {
  bool _busy = false;
  bool _onlyActive = true;
  String? _error;
  String? _info;
  List<InvitationCodeDto> _list = [];

  // Form de creacion
  final _maxUsesCtrl = TextEditingController(text: '1');
  final _notesCtrl = TextEditingController();
  DateTime? _expiresAt;

  @override
  void initState() {
    super.initState();
    _refresh();
  }

  Future<void> _refresh() async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final list = await widget.api.listInvitations(onlyActive: _onlyActive);
      setState(() => _list = list);
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _create() async {
    final maxUses = int.tryParse(_maxUsesCtrl.text.trim()) ?? 1;
    if (maxUses < 1 || maxUses > 10000) {
      setState(() => _error = 'maxUses debe estar entre 1 y 10000');
      return;
    }
    setState(() {
      _busy = true;
      _error = null;
      _info = null;
    });
    try {
      final created = await widget.api.createInvitation(
        maxUses: maxUses,
        expiresAt: _expiresAt,
        notes: _notesCtrl.text.trim().isEmpty ? null : _notesCtrl.text.trim(),
      );
      setState(() {
        _info = 'Codigo creado: ${created.code}';
        _maxUsesCtrl.text = '1';
        _notesCtrl.clear();
        _expiresAt = null;
      });
      await _refresh();
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _revoke(InvitationCodeDto inv) async {
    final reason = await showDialog<String>(
      context: context,
      builder: (ctx) {
        final ctrl = TextEditingController();
        return AlertDialog(
          title: Text('Revocar codigo ${inv.code}?'),
          content: TextField(
            controller: ctrl,
            decoration: const InputDecoration(labelText: 'Motivo (opcional)'),
            maxLength: 200,
          ),
          actions: [
            TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Cancelar')),
            ElevatedButton(
              onPressed: () => Navigator.pop(ctx, ctrl.text.trim()),
              style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
              child: const Text('Revocar'),
            ),
          ],
        );
      },
    );
    if (reason == null) return;
    setState(() => _busy = true);
    try {
      await widget.api.revokeInvitation(inv.id, reason: reason.isEmpty ? null : reason);
      setState(() => _info = 'Codigo ${inv.code} revocado');
      await _refresh();
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  void _copy(String code) {
    Clipboard.setData(ClipboardData(text: code));
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text('Codigo $code copiado al portapapeles')));
  }

  Future<void> _pickExpires() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: DateTime.now().add(const Duration(days: 30)),
      firstDate: DateTime.now().add(const Duration(days: 1)),
      lastDate: DateTime.now().add(const Duration(days: 365 * 2)),
    );
    if (picked != null) setState(() => _expiresAt = picked);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Codigos de invitacion')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 1100),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                if (_info != null)
                  Card(color: Colors.green.shade50, child: Padding(
                    padding: const EdgeInsets.all(12),
                    child: Row(children: [
                      const Icon(Icons.check_circle, color: Colors.green),
                      const SizedBox(width: 8),
                      Expanded(child: Text(_info!)),
                      IconButton(
                        icon: const Icon(Icons.close, size: 18),
                        onPressed: () => setState(() => _info = null),
                      ),
                    ]),
                  )),
                if (_error != null)
                  Card(color: Colors.red.shade50, child: Padding(
                    padding: const EdgeInsets.all(12),
                    child: Row(children: [
                      const Icon(Icons.error_outline, color: Colors.red),
                      const SizedBox(width: 8),
                      Expanded(child: Text(_error!)),
                    ]),
                  )),
                const SizedBox(height: 12),

                // Form de creacion
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(20),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        const Text('Generar nuevo codigo',
                            style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                        const SizedBox(height: 12),
                        Row(children: [
                          Expanded(
                            child: TextField(
                              controller: _maxUsesCtrl,
                              decoration: const InputDecoration(
                                labelText: 'Usos maximos',
                                hintText: '1',
                              ),
                              keyboardType: TextInputType.number,
                            ),
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: InkWell(
                              onTap: _pickExpires,
                              child: InputDecorator(
                                decoration: const InputDecoration(
                                  labelText: 'Vence el (opcional)',
                                ),
                                child: Text(
                                  _expiresAt == null
                                      ? 'Sin expiracion'
                                      : '${_expiresAt!.year}-${_expiresAt!.month.toString().padLeft(2, '0')}-${_expiresAt!.day.toString().padLeft(2, '0')}',
                                ),
                              ),
                            ),
                          ),
                          if (_expiresAt != null) IconButton(
                            icon: const Icon(Icons.clear),
                            onPressed: () => setState(() => _expiresAt = null),
                          ),
                        ]),
                        const SizedBox(height: 12),
                        TextField(
                          controller: _notesCtrl,
                          decoration: const InputDecoration(
                            labelText: 'Notas (opcional)',
                            hintText: 'ej. Familia Lopez, campana navidad',
                          ),
                          maxLength: 500,
                        ),
                        const SizedBox(height: 8),
                        ElevatedButton.icon(
                          onPressed: _busy ? null : _create,
                          icon: const Icon(Icons.add_card),
                          label: const Text('Generar codigo'),
                        ),
                      ],
                    ),
                  ),
                ),

                const SizedBox(height: 24),

                // Toggle filtro
                Row(
                  children: [
                    const Text('Codigos existentes',
                        style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                    const Spacer(),
                    SegmentedButton<bool>(
                      segments: const [
                        ButtonSegment(value: true, label: Text('Activos')),
                        ButtonSegment(value: false, label: Text('Todos')),
                      ],
                      selected: {_onlyActive},
                      onSelectionChanged: (s) {
                        setState(() => _onlyActive = s.first);
                        _refresh();
                      },
                    ),
                    const SizedBox(width: 8),
                    IconButton(
                      icon: const Icon(Icons.refresh),
                      onPressed: _busy ? null : _refresh,
                      tooltip: 'Refrescar',
                    ),
                  ],
                ),
                const SizedBox(height: 12),

                if (_busy && _list.isEmpty)
                  const Center(child: Padding(
                    padding: EdgeInsets.all(40),
                    child: CircularProgressIndicator(),
                  ))
                else if (_list.isEmpty)
                  Card(child: Padding(
                    padding: const EdgeInsets.all(40),
                    child: Center(child: Text(
                        _onlyActive ? 'Sin codigos activos' : 'Sin codigos generados',
                        style: const TextStyle(color: Colors.grey))),
                  ))
                else
                  Card(
                    child: SingleChildScrollView(
                      scrollDirection: Axis.horizontal,
                      child: DataTable(
                        columns: const [
                          DataColumn(label: Text('Codigo')),
                          DataColumn(label: Text('Estado')),
                          DataColumn(label: Text('Uso')),
                          DataColumn(label: Text('Vence')),
                          DataColumn(label: Text('Notas')),
                          DataColumn(label: Text('Acciones')),
                        ],
                        rows: _list.map((inv) {
                          return DataRow(cells: [
                            DataCell(Row(children: [
                              SelectableText(
                                inv.code,
                                style: const TextStyle(fontFamily: 'monospace', fontWeight: FontWeight.bold),
                              ),
                              IconButton(
                                icon: const Icon(Icons.copy, size: 16),
                                onPressed: () => _copy(inv.code),
                                tooltip: 'Copiar',
                              ),
                            ])),
                            DataCell(_StatusBadge(label: inv.statusLabel)),
                            DataCell(Text('${inv.usedCount} / ${inv.maxUses}')),
                            DataCell(Text(
                              inv.expiresAt == null ? '—' :
                                '${inv.expiresAt!.year}-${inv.expiresAt!.month.toString().padLeft(2, '0')}-${inv.expiresAt!.day.toString().padLeft(2, '0')}',
                            )),
                            DataCell(Text(inv.notes ?? '—', overflow: TextOverflow.ellipsis)),
                            DataCell(Row(children: [
                              if (inv.isActive) IconButton(
                                icon: const Icon(Icons.block, color: Colors.red, size: 18),
                                onPressed: _busy ? null : () => _revoke(inv),
                                tooltip: 'Revocar',
                              ),
                            ])),
                          ]);
                        }).toList(),
                      ),
                    ),
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  @override
  void dispose() {
    _maxUsesCtrl.dispose();
    _notesCtrl.dispose();
    super.dispose();
  }
}

class _StatusBadge extends StatelessWidget {
  final String label;
  const _StatusBadge({required this.label});

  @override
  Widget build(BuildContext context) {
    Color color;
    switch (label) {
      case 'Activo': color = Colors.green; break;
      case 'Revocado': color = Colors.red; break;
      case 'Expirado': color = Colors.orange; break;
      case 'Agotado': color = Colors.grey; break;
      default: color = Colors.grey;
    }
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.15),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(label, style: TextStyle(color: color, fontSize: 12, fontWeight: FontWeight.bold)),
    );
  }
}
