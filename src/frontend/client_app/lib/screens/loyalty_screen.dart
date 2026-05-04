import 'package:flutter/material.dart';
import 'package:homechef_shared/homechef_shared.dart';

import '../app_state.dart';

/// Pantalla del programa Sabor (loyalty). Muestra:
///   - Balance actual + nivel + puntos al siguiente nivel
///   - Catalogo de recompensas activas (afford / no afford)
///   - Boton de canjear con confirmacion
///
/// Si el usuario no esta logueado, muestra un prompt para ingresar.
class LoyaltyScreen extends StatefulWidget {
  final AppState state;
  const LoyaltyScreen({super.key, required this.state});

  @override
  State<LoyaltyScreen> createState() => _LoyaltyScreenState();
}

class _LoyaltyScreenState extends State<LoyaltyScreen> {
  Future<({LoyaltyAccount account, List<LoyaltyReward> rewards})>? _future;

  @override
  void initState() {
    super.initState();
    _refresh();
  }

  void _refresh() {
    if (!widget.state.isLoggedIn) {
      _future = null;
      return;
    }
    _future = _load();
  }

  Future<({LoyaltyAccount account, List<LoyaltyReward> rewards})> _load() async {
    final acc = await widget.state.api.loyaltyMe();
    final rewards = await widget.state.api.loyaltyRewards();
    return (account: acc, rewards: rewards);
  }

  Future<void> _redeem(LoyaltyReward reward) async {
    final t = widget.state.strings;
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(t.t('loyalty.confirmTitle')),
        content: Text(
            '${t.t('loyalty.confirmMsg')}\n\n'
            '${reward.name}\n${reward.costPoints} pts'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: Text(t.t('loyalty.cancel')),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: Text(t.t('loyalty.redeem')),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;

    try {
      final result = await widget.state.api.loyaltyRedeem(reward.id);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(
            '${t.t('loyalty.redeemed')}: ${result.rewardName}. '
            '${t.t('loyalty.balance')}: ${result.remainingBalance} pts')),
      );
      setState(_refresh);
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('$e'), backgroundColor: Colors.red),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = widget.state.strings;
    final palette = Theme.of(context).extension<HcpThemeExtension>()!.palette;

    if (!widget.state.isLoggedIn) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(32),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(Icons.workspace_premium, size: 64),
              const SizedBox(height: 16),
              Text(t.t('loyalty.title'),
                  style: Theme.of(context).textTheme.headlineSmall),
              const SizedBox(height: 8),
              Text(t.t('loyalty.guestPrompt'),
                  textAlign: TextAlign.center),
            ],
          ),
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () async => setState(_refresh),
      child: FutureBuilder<({LoyaltyAccount account, List<LoyaltyReward> rewards})>(
        future: _future,
        builder: (context, snap) {
          if (snap.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }
          if (snap.hasError) {
            return ListView(
              padding: const EdgeInsets.all(16),
              children: [
                Text('${t.t('common.error')}: ${snap.error}'),
                const SizedBox(height: 8),
                FilledButton(
                  onPressed: () => setState(_refresh),
                  child: Text(t.t('catalog.retry')),
                ),
              ],
            );
          }
          final data = snap.data!;
          final acc = data.account;
          final rewards = data.rewards;

          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Text(t.t('loyalty.title'),
                  style: Theme.of(context).textTheme.displaySmall),
              const SizedBox(height: 16),
              _BalanceCard(account: acc, palette: palette, t: t),
              const SizedBox(height: 24),
              Text(t.t('loyalty.rewards'),
                  style: Theme.of(context).textTheme.titleLarge),
              const SizedBox(height: 8),
              if (rewards.isEmpty)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 24),
                  child: Center(child: Text(t.t('loyalty.empty'))),
                )
              else
                ...rewards.map((r) => _RewardCard(
                      reward: r,
                      onRedeem: r.isAffordable ? () => _redeem(r) : null,
                      t: t,
                    )),
            ],
          );
        },
      ),
    );
  }
}

class _BalanceCard extends StatelessWidget {
  final LoyaltyAccount account;
  final HcpPalette palette;
  final HcpStrings t;
  const _BalanceCard({
    required this.account,
    required this.palette,
    required this.t,
  });

  @override
  Widget build(BuildContext context) {
    final levelColor = switch (account.level) {
      'oro' => Colors.amber.shade700,
      'plata' => Colors.blueGrey.shade400,
      _ => Colors.brown.shade400, // bronce
    };
    return Card(
      color: palette.accent.withValues(alpha: 0.1),
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(Icons.workspace_premium, color: levelColor, size: 32),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(t.t('loyalty.level'),
                          style: Theme.of(context).textTheme.bodySmall),
                      Text(
                        account.level.toUpperCase(),
                        style: Theme.of(context).textTheme.headlineSmall
                            ?.copyWith(color: levelColor, fontWeight: FontWeight.bold),
                      ),
                    ],
                  ),
                ),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Text(t.t('loyalty.balance'),
                        style: Theme.of(context).textTheme.bodySmall),
                    Text(
                      '${account.currentBalance} pts',
                      style: Theme.of(context).textTheme.headlineSmall
                          ?.copyWith(fontWeight: FontWeight.bold),
                    ),
                  ],
                ),
              ],
            ),
            const SizedBox(height: 12),
            Text('${t.t('loyalty.lifetime')}: ${account.lifetimeEarned} pts',
                style: Theme.of(context).textTheme.bodySmall),
            const SizedBox(height: 8),
            if (account.nextLevel != null)
              Text(
                '${t.t('loyalty.toNextLevel')} '
                '${account.nextLevel!.toUpperCase()}: '
                '${account.pointsToNextLevel} pts',
                style: Theme.of(context).textTheme.bodyMedium,
              )
            else
              Text(t.t('loyalty.maxLevel'),
                  style: Theme.of(context).textTheme.bodyMedium),
          ],
        ),
      ),
    );
  }
}

class _RewardCard extends StatelessWidget {
  final LoyaltyReward reward;
  final VoidCallback? onRedeem;
  final HcpStrings t;
  const _RewardCard({
    required this.reward,
    required this.onRedeem,
    required this.t,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(
        leading: Icon(
          Icons.card_giftcard,
          color: reward.isAffordable ? Colors.green : Colors.grey,
        ),
        title: Text(reward.name),
        subtitle: Text(
          reward.description ?? '',
          maxLines: 2,
          overflow: TextOverflow.ellipsis,
        ),
        trailing: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text('${reward.costPoints} pts',
                style: Theme.of(context).textTheme.titleMedium
                    ?.copyWith(fontWeight: FontWeight.bold)),
            if (onRedeem != null)
              TextButton(
                onPressed: onRedeem,
                child: Text(t.t('loyalty.redeem')),
              )
            else
              Text(
                t.t('loyalty.notEnough'),
                style: Theme.of(context).textTheme.bodySmall
                    ?.copyWith(color: Colors.grey),
              ),
          ],
        ),
      ),
    );
  }
}
