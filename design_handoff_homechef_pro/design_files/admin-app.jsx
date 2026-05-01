// Admin dashboard — órdenes en vivo, recetas, inventario, analítica, facturas
// Wrapped in ChromeWindow. Uses shared data from data.jsx.

const ADM = new Proxy({}, { get: (_, k) => {
  const th = hcpTheme();
  if (k === 'surface') return th.card;
  if (k === 'lineStrong') return th.line;
  if (k === 'sunSoft') return th.greenSoft;
  return th[k];
}});

function AdminSidebar({ active, onNav, lang, fontScale }) {
  const t = HCP_I18N[lang];
  const items = [
    { id: 'overview',  label: t.overview,    icon: '◈' },
    { id: 'orders',    label: t.ordersLive,  icon: '◉', badge: 2 },
    { id: 'menu',      label: t.menu,        icon: '❖' },
    { id: 'inventory', label: t.inventory,   icon: '▣' },
    { id: 'purchasing', label: t.purchasing, icon: '▤' },
    { id: 'analytics', label: t.analytics,   icon: '◐' },
    { id: 'invoices',  label: t.invoices,    icon: '▢' },
  ];
  return (
    <div style={{
      width: 220, background: ADM.sidebar, color: ADM.sidebarText,
      padding: '20px 14px', display: 'flex', flexDirection: 'column', fontSize: 13 * fontScale, flexShrink: 0,
    }}>
      <div style={{ padding: '0 8px 22px', display: 'flex', alignItems: 'center', gap: 10 }}>
        <div style={{
          width: 34, height: 34, borderRadius: 10,
          background: `linear-gradient(135deg, ${ADM.accent}, ${ADM.sun})`,
          display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 17, color: '#fff',
        }}>🍳</div>
        <div>
          <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 17 * fontScale, color: '#fff', lineHeight: 1 }}>HomeChef Pro</div>
          <div style={{ fontSize: 10 * fontScale, color: ADM.sidebarMuted, marginTop: 2 }}>{lang === 'es' ? 'Panel admin' : 'Admin panel'}</div>
        </div>
      </div>
      <div style={{ flex: 1 }}>
        {items.map(i => (
          <div key={i.id} onClick={() => onNav(i.id)} style={{
            padding: '10px 12px', borderRadius: 10, cursor: 'pointer',
            background: active === i.id ? 'rgba(255,140,66,0.15)' : 'transparent',
            color: active === i.id ? ADM.accent : ADM.sidebarText,
            display: 'flex', alignItems: 'center', gap: 10, fontWeight: active === i.id ? 600 : 500,
            marginBottom: 2,
          }}>
            <span style={{ fontSize: 14, opacity: active === i.id ? 1 : 0.7 }}>{i.icon}</span>
            <span style={{ flex: 1 }}>{i.label}</span>
            {i.badge && <span style={{ background: ADM.accent, color: '#fff', fontSize: 10, fontWeight: 700, padding: '2px 7px', borderRadius: 999 }}>{i.badge}</span>}
          </div>
        ))}
      </div>
      <div style={{ padding: 10, background: 'rgba(255,255,255,0.05)', borderRadius: 10, display: 'flex', alignItems: 'center', gap: 10 }}>
        <div style={{ width: 32, height: 32, borderRadius: 16, background: ADM.accent, display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 700, color: '#fff' }}>R</div>
        <div style={{ flex: 1, minWidth: 0 }}>
          <div style={{ fontSize: 12 * fontScale, fontWeight: 600, color: '#fff' }}>Rocío Herrera</div>
          <div style={{ fontSize: 10 * fontScale, color: ADM.sidebarMuted }}>{lang === 'es' ? 'Chef · Dueña' : 'Chef · Owner'}</div>
        </div>
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────
// Overview (dashboard home)
// ─────────────────────────────────────────────────────────────
function AdminOverview({ lang, fontScale, onNav }) {
  const t = HCP_I18N[lang];
  const k = HCP_ANALYTICS.kpis;
  const today = HCP_ANALYTICS.week[HCP_ANALYTICS.week.length - 1];
  const lowStock = HCP_INVENTORY.filter(i => i.status !== 'ok');

  return (
    <div style={{ padding: 28, fontSize: 14 * fontScale }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 22 }}>
        <div>
          <div style={{ fontSize: 12 * fontScale, color: ADM.inkSoft, fontWeight: 500 }}>
            {lang === 'es' ? 'Viernes, 24 de abril' : 'Friday, April 24'}
          </div>
          <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 34 * fontScale, color: ADM.ink, lineHeight: 1.1, marginTop: 4 }}>
            {lang === 'es' ? '¡Buen día, Rocío!' : 'Good morning, Rocío!'}
          </div>
          <div style={{ fontSize: 13 * fontScale, color: ADM.inkSoft, marginTop: 6 }}>
            {lang === 'es' ? 'Tienes 2 pedidos nuevos y 4 ingredientes por reordenar.' : '2 new orders and 4 ingredients need reordering.'}
          </div>
        </div>
        <button onClick={() => onNav('orders')} style={{
          padding: '12px 20px', background: ADM.ink, color: '#fff', border: 'none',
          borderRadius: 10, fontWeight: 600, fontSize: 13 * fontScale, cursor: 'pointer',
          display: 'flex', alignItems: 'center', gap: 8,
        }}>
          <span style={{ width: 8, height: 8, borderRadius: 4, background: ADM.accent, animation: 'hcp-pulse 2s infinite' }} /> {lang === 'es' ? 'Ver cocina en vivo' : 'Live kitchen'}
        </button>
      </div>

      {/* KPI cards */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 14, marginBottom: 24 }}>
        <KpiCard label={t.revenue + ' · ' + t.thisWeek} value={hcpFmtFull(k.revenue)} delta="+12.4%" fontScale={fontScale} />
        <KpiCard label={t.orderCount + ' · ' + t.thisWeek} value={k.orders.toString()} delta="+8.1%" fontScale={fontScale} />
        <KpiCard label={t.avgTicket} value={hcpFmtFull(k.avgTicket)} delta="+2.3%" fontScale={fontScale} />
        <KpiCard label={t.profit + ' · ' + t.thisWeek} value={hcpFmtFull(k.profit)} delta={`${Math.round(k.margin * 100)}%`} deltaLabel={lang === 'es' ? 'margen' : 'margin'} fontScale={fontScale} />
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1.3fr 1fr', gap: 18 }}>
        {/* Revenue chart */}
        <div style={{ background: ADM.surface, padding: 22, borderRadius: 16, border: `1px solid ${ADM.line}` }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', marginBottom: 18 }}>
            <div>
              <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 20 * fontScale, color: ADM.ink }}>
                {lang === 'es' ? 'Ingresos de la semana' : 'Weekly revenue'}
              </div>
              <div style={{ fontSize: 12 * fontScale, color: ADM.inkSoft, marginTop: 2 }}>{hcpFmtFull(k.revenue)} · {k.orders} {t.orderCount.toLowerCase()}</div>
            </div>
            <div style={{ display: 'flex', gap: 14, fontSize: 11 * fontScale, color: ADM.inkSoft }}>
              <span style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                <span style={{ width: 10, height: 10, borderRadius: 2, background: ADM.accent }} /> {t.revenue}
              </span>
              <span style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                <span style={{ width: 10, height: 10, borderRadius: 2, background: ADM.green }} /> {t.profit}
              </span>
            </div>
          </div>
          <WeekBarChart data={HCP_ANALYTICS.week} lang={lang} fontScale={fontScale} />
        </div>

        {/* Top dishes */}
        <div style={{ background: ADM.surface, padding: 22, borderRadius: 16, border: `1px solid ${ADM.line}` }}>
          <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 20 * fontScale, color: ADM.ink, marginBottom: 14 }}>
            {t.topDishes}
          </div>
          {HCP_ANALYTICS.topDishes.slice(0, 5).map((td, i) => {
            const d = hcpDish(td.id);
            return (
              <div key={td.id} style={{ display: 'flex', gap: 12, alignItems: 'center', padding: '10px 0', borderBottom: i < 4 ? `1px solid ${ADM.line}` : 'none' }}>
                <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 22 * fontScale, color: ADM.inkMuted, width: 24 }}>{i + 1}</div>
                <DishThumbAdm dish={d} size={40} />
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div style={{ fontWeight: 600, fontSize: 13 * fontScale, color: ADM.ink, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{d.name[lang]}</div>
                  <div style={{ fontSize: 11 * fontScale, color: ADM.inkSoft, marginTop: 2 }}>{td.sold} {lang === 'es' ? 'vendidos' : 'sold'}</div>
                </div>
                <div style={{ textAlign: 'right' }}>
                  <div style={{ fontWeight: 700, fontSize: 13 * fontScale }}>{hcpFmtFull(td.revenue)}</div>
                  <div style={{ fontSize: 10 * fontScale, color: ADM.green }}>+{hcpFmtFull(td.profit)}</div>
                </div>
              </div>
            );
          })}
        </div>
      </div>

      {/* Stock alerts */}
      <div style={{ marginTop: 18, background: ADM.surface, padding: 22, borderRadius: 16, border: `1px solid ${ADM.line}` }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 14 }}>
          <div>
            <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 20 * fontScale, color: ADM.ink }}>
              {lang === 'es' ? 'Alertas de inventario' : 'Inventory alerts'}
            </div>
            <div style={{ fontSize: 12 * fontScale, color: ADM.inkSoft, marginTop: 2 }}>
              {lowStock.length} {lang === 'es' ? 'ingredientes necesitan atención' : 'ingredients need attention'}
            </div>
          </div>
          <button onClick={() => onNav('purchasing')} style={{ padding: '8px 14px', background: ADM.accent, color: '#fff', border: 'none', borderRadius: 8, fontWeight: 600, fontSize: 12 * fontScale, cursor: 'pointer' }}>
            {t.schedulePurchase} →
          </button>
        </div>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 10 }}>
          {lowStock.slice(0, 6).map(inv => (
            <div key={inv.id} style={{
              padding: 12, borderRadius: 12,
              background: inv.status === 'out' ? ADM.redSoft : ADM.sunSoft,
              borderLeft: `3px solid ${inv.status === 'out' ? ADM.red : ADM.sun}`,
            }}>
              <div style={{ fontSize: 10 * fontScale, fontWeight: 700, color: inv.status === 'out' ? ADM.red : '#8A6A10', textTransform: 'uppercase', letterSpacing: 0.4 }}>
                {inv.status === 'out' ? t.outOfStock : t.lowStock}
              </div>
              <div style={{ fontWeight: 600, fontSize: 13 * fontScale, color: ADM.ink, marginTop: 2 }}>{inv.name[lang]}</div>
              <div style={{ fontSize: 11 * fontScale, color: ADM.inkSoft, marginTop: 2, fontFamily: 'JetBrains Mono, monospace' }}>
                {inv.stock}/{inv.reorder} {inv.unit}
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

function KpiCard({ label, value, delta, deltaLabel, fontScale }) {
  const up = delta && delta.startsWith('+');
  return (
    <div style={{ background: ADM.surface, padding: 18, borderRadius: 14, border: `1px solid ${ADM.line}` }}>
      <div style={{ fontSize: 11 * fontScale, color: ADM.inkSoft, textTransform: 'uppercase', letterSpacing: 0.4, fontWeight: 600 }}>{label}</div>
      <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 28 * fontScale, color: ADM.ink, marginTop: 8, lineHeight: 1 }}>{value}</div>
      {delta && (
        <div style={{ marginTop: 6, fontSize: 11 * fontScale, color: up ? ADM.green : ADM.inkSoft, fontWeight: 600 }}>
          {delta} {deltaLabel && <span style={{ color: ADM.inkSoft, fontWeight: 500 }}>{deltaLabel}</span>}
        </div>
      )}
    </div>
  );
}

function WeekBarChart({ data, lang, fontScale }) {
  const max = Math.max(...data.map(d => d.revenue));
  return (
    <div style={{ display: 'flex', gap: 10, alignItems: 'flex-end', height: 200, paddingBottom: 28 }}>
      {data.map((d, i) => {
        const h = (d.revenue / max) * 170;
        const ph = (d.profit / max) * 170;
        return (
          <div key={i} style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 6, position: 'relative' }}>
            <div style={{ fontSize: 10 * fontScale, color: ADM.inkSoft, fontFamily: 'JetBrains Mono, monospace', whiteSpace: 'nowrap' }}>
              {Math.round(d.revenue / 1000)}k
            </div>
            <div style={{ width: '100%', height: h, background: ADM.accent, borderRadius: '6px 6px 0 0', position: 'relative', display: 'flex', alignItems: 'flex-end' }}>
              <div style={{ width: '100%', height: ph, background: ADM.green, borderRadius: '6px 6px 0 0', opacity: 0.85 }} />
            </div>
            <div style={{ position: 'absolute', bottom: -24, fontSize: 11 * fontScale, color: ADM.inkSoft, fontWeight: 500 }}>{d.day[lang]}</div>
          </div>
        );
      })}
    </div>
  );
}

function DishThumbAdm({ dish, size = 40 }) {
  return (
    <div style={{
      width: size, height: size, borderRadius: size * 0.24, flexShrink: 0,
      background: `linear-gradient(135deg, ${dish.swatch[0]}, ${dish.swatch[1]})`,
      display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: size * 0.5,
      position: 'relative', overflow: 'hidden',
    }}>
      {dish.photo ? (
        <img
          src={dish.photo}
          alt={dish.name?.es || ''}
          style={{ position: 'absolute', inset: 0, width: '100%', height: '100%', objectFit: 'cover' }}
          onError={(e) => { e.currentTarget.style.display = 'none'; }}
        />
      ) : (
        <React.Fragment>
          <div style={{ position: 'absolute', inset: 0,
            backgroundImage: 'repeating-linear-gradient(45deg, transparent 0 6px, rgba(255,255,255,0.1) 6px 7px)' }} />
          <span style={{ position: 'relative' }}>{dish.emoji}</span>
        </React.Fragment>
      )}
    </div>
  );
}

// ─────────────────────────────────────────────────────────────
// Live Orders (kitchen)
// ─────────────────────────────────────────────────────────────
function AdminOrders({ lang, fontScale }) {
  const t = HCP_I18N[lang];
  const cols = [
    { id: 'incoming', label: t.incoming,       tone: ADM.accent },
    { id: 'cooking',  label: t.inKitchen,      tone: ADM.sun },
    { id: 'ready',    label: t.readyForPickup, tone: ADM.green },
    { id: 'delivered', label: t.completed,     tone: ADM.inkMuted },
  ];
  return (
    <div style={{ padding: 28, fontSize: 14 * fontScale }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 22 }}>
        <div>
          <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 30 * fontScale, color: ADM.ink }}>{t.ordersLive}</div>
          <div style={{ fontSize: 13 * fontScale, color: ADM.inkSoft, marginTop: 4 }}>
            {lang === 'es' ? '7 pedidos activos · actualizado hace 3s' : '7 active orders · updated 3s ago'}
          </div>
        </div>
        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 6, padding: '8px 14px', background: ADM.greenSoft, borderRadius: 999, fontSize: 12 * fontScale, color: ADM.green, fontWeight: 600 }}>
            <span style={{ width: 8, height: 8, borderRadius: 4, background: ADM.green, animation: 'hcp-pulse 2s infinite' }} />
            {lang === 'es' ? 'Conectado' : 'Connected'}
          </div>
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 14 }}>
        {cols.map(col => {
          const items = HCP_ORDERS.filter(o => o.status === col.id);
          return (
            <div key={col.id} style={{ background: ADM.surface, borderRadius: 14, border: `1px solid ${ADM.line}`, display: 'flex', flexDirection: 'column' }}>
              <div style={{ padding: '14px 16px', borderBottom: `1px solid ${ADM.line}`, display: 'flex', alignItems: 'center', gap: 8 }}>
                <span style={{ width: 8, height: 8, borderRadius: 4, background: col.tone }} />
                <div style={{ fontWeight: 700, fontSize: 13 * fontScale, color: ADM.ink, flex: 1 }}>{col.label}</div>
                <div style={{ fontSize: 12 * fontScale, color: ADM.inkSoft, fontFamily: 'JetBrains Mono, monospace' }}>{items.length}</div>
              </div>
              <div style={{ padding: 10, display: 'flex', flexDirection: 'column', gap: 10, minHeight: 400 }}>
                {items.map(o => <OrderCard key={o.id} order={o} lang={lang} fontScale={fontScale} />)}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

function OrderCard({ order, lang, fontScale }) {
  const t = HCP_I18N[lang];
  return (
    <div style={{
      background: '#fff', border: `1px solid ${ADM.line}`, borderRadius: 12, padding: 12,
    }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 8 }}>
        <div>
          <div style={{ fontFamily: 'JetBrains Mono, monospace', fontWeight: 700, fontSize: 13 * fontScale, color: ADM.ink }}>{order.id}</div>
          <div style={{ fontSize: 12 * fontScale, color: ADM.ink, fontWeight: 600, marginTop: 2 }}>{order.customer}</div>
          <div style={{ fontSize: 11 * fontScale, color: ADM.inkSoft, marginTop: 2, display: 'flex', alignItems: 'center', gap: 4 }}>
            <span>{order.type === 'delivery' ? '🛵' : '🏪'}</span> {order.type === 'delivery' ? t.delivery : t.pickup} · {order.time}
          </div>
        </div>
        {order.paid && (
          <div style={{ fontSize: 9 * fontScale, fontWeight: 700, color: ADM.green, background: ADM.greenSoft, padding: '3px 7px', borderRadius: 5, letterSpacing: 0.3, textTransform: 'uppercase' }}>
            ✓ {lang === 'es' ? 'Pagado' : 'Paid'}
          </div>
        )}
      </div>
      <div style={{ padding: '8px 0', borderTop: `1px solid ${ADM.line}`, borderBottom: `1px solid ${ADM.line}` }}>
        {order.items.map((it, i) => {
          const d = hcpDish(it.dish);
          return (
            <div key={i} style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '3px 0' }}>
              <div style={{ fontWeight: 700, fontSize: 12 * fontScale, color: ADM.accent, width: 20, fontFamily: 'JetBrains Mono, monospace' }}>{it.qty}×</div>
              <div style={{ fontSize: 12 * fontScale, color: ADM.ink, flex: 1 }}>{d.name[lang]}</div>
            </div>
          );
        })}
      </div>
      {order.notes && (
        <div style={{ marginTop: 8, padding: 8, background: ADM.sunSoft, borderRadius: 8, fontSize: 11 * fontScale, color: '#6B5D47', lineHeight: 1.35 }}>
          📝 {order.notes[lang]}
        </div>
      )}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 10 }}>
        <div style={{ fontWeight: 700, fontSize: 14 * fontScale, color: ADM.ink }}>{hcpFmtFull(order.total)}</div>
        {order.status === 'incoming' && (
          <button style={{ padding: '6px 12px', background: ADM.ink, color: '#fff', border: 'none', borderRadius: 7, fontWeight: 600, fontSize: 11 * fontScale, cursor: 'pointer' }}>{t.accept}</button>
        )}
        {order.status === 'cooking' && (
          <button style={{ padding: '6px 12px', background: ADM.accent, color: '#fff', border: 'none', borderRadius: 7, fontWeight: 600, fontSize: 11 * fontScale, cursor: 'pointer' }}>{t.markReady}</button>
        )}
        {order.status === 'ready' && order.type === 'delivery' && (
          <button style={{ padding: '6px 12px', background: ADM.green, color: '#fff', border: 'none', borderRadius: 7, fontWeight: 600, fontSize: 11 * fontScale, cursor: 'pointer' }}>
            {lang === 'es' ? 'Llamar mensajero' : 'Call courier'}
          </button>
        )}
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────
// Recipe editor
// ─────────────────────────────────────────────────────────────
function AdminRecipeEditor({ lang, fontScale }) {
  const t = HCP_I18N[lang];
  const [selectedId, setSelectedId] = React.useState('pabellon');
  const dish = hcpDish(selectedId);
  const [targetMargin, setTargetMargin] = React.useState(65);

  const directCost = dish.ingredients.reduce((s, i) => s + i.cost, 0);
  const subCost = (dish.subRecipes || []).reduce((s, id) => {
    const sr = HCP_SUBRECIPES[id];
    if (!sr) return s;
    // Estimate portion cost of sub-recipe (assume 50g used)
    const total = sr.ingredients.reduce((a, b) => a + b.cost, 0);
    return s + Math.round(total * 0.15);
  }, 0);
  const foodCost = directCost + subCost;
  const laborCost = 1800;
  const overheadCost = 1200;
  const totalCost = foodCost + laborCost + overheadCost;
  const suggestedPrice = Math.round(totalCost / (1 - targetMargin / 100));
  const currentMargin = Math.round((1 - totalCost / dish.price) * 100);

  return (
    <div style={{ padding: 28, fontSize: 14 * fontScale, display: 'grid', gridTemplateColumns: '260px 1fr', gap: 18, height: '100%', overflow: 'hidden' }}>
      {/* Dish list */}
      <div style={{ overflowY: 'auto' }}>
        <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 24 * fontScale, color: ADM.ink, marginBottom: 14 }}>{t.menu}</div>
        <button style={{
          width: '100%', padding: '10px 12px', background: ADM.accent, color: '#fff',
          border: 'none', borderRadius: 10, fontWeight: 600, fontSize: 13 * fontScale,
          marginBottom: 12, cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 8, justifyContent: 'center',
        }}>+ {lang === 'es' ? 'Nuevo plato' : 'New dish'}</button>
        {HCP_DISHES.map(d => (
          <div key={d.id} onClick={() => setSelectedId(d.id)} style={{
            padding: 10, borderRadius: 10, display: 'flex', gap: 10, alignItems: 'center', cursor: 'pointer',
            background: selectedId === d.id ? '#fff' : 'transparent',
            border: `1px solid ${selectedId === d.id ? ADM.lineStrong : 'transparent'}`,
            marginBottom: 4,
          }}>
            <DishThumbAdm dish={d} size={36} />
            <div style={{ flex: 1, minWidth: 0 }}>
              <div style={{ fontWeight: 600, fontSize: 12 * fontScale, color: ADM.ink, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{d.name[lang]}</div>
              <div style={{ fontSize: 10 * fontScale, color: ADM.inkSoft, fontFamily: 'JetBrains Mono, monospace' }}>{hcpFmtFull(d.price)}</div>
            </div>
          </div>
        ))}
        <div style={{ marginTop: 18, fontSize: 11 * fontScale, color: ADM.inkSoft, textTransform: 'uppercase', letterSpacing: 0.4, fontWeight: 700, padding: '0 10px' }}>
          {t.subRecipe}
        </div>
        <div style={{ marginTop: 6 }}>
          {Object.entries(HCP_SUBRECIPES).map(([id, sr]) => (
            <div key={id} style={{ padding: 10, borderRadius: 10, display: 'flex', gap: 10, alignItems: 'center', marginBottom: 4 }}>
              <div style={{ width: 36, height: 36, borderRadius: 8, background: ADM.greenSoft, display: 'flex', alignItems: 'center', justifyContent: 'center', color: ADM.green, fontWeight: 700 }}>🫙</div>
              <div style={{ flex: 1, minWidth: 0 }}>
                <div style={{ fontWeight: 600, fontSize: 12 * fontScale, color: ADM.ink }}>{sr.name[lang]}</div>
                <div style={{ fontSize: 10 * fontScale, color: ADM.inkSoft }}>{sr.yield}</div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Editor */}
      <div style={{ overflowY: 'auto', paddingRight: 4 }}>
        <div style={{ display: 'flex', gap: 16, marginBottom: 20 }}>
          <div style={{ width: 140, height: 140, borderRadius: 16, background: `linear-gradient(135deg, ${dish.swatch[0]}, ${dish.swatch[1]})`, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 60, flexShrink: 0, position: 'relative', overflow: 'hidden' }}>
            <div style={{ position: 'absolute', inset: 0, backgroundImage: 'repeating-linear-gradient(45deg, transparent 0 10px, rgba(255,255,255,0.08) 10px 12px)' }} />
            <span style={{ position: 'relative' }}>{dish.emoji}</span>
          </div>
          <div style={{ flex: 1 }}>
            <div style={{ fontSize: 11 * fontScale, color: ADM.inkSoft, textTransform: 'uppercase', letterSpacing: 0.4, fontWeight: 700 }}>{dish.category[lang]}</div>
            <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 32 * fontScale, color: ADM.ink, marginTop: 2, lineHeight: 1.05 }}>{dish.name[lang]}</div>
            <div style={{ fontSize: 13 * fontScale, color: ADM.inkSoft, marginTop: 8, lineHeight: 1.45, textWrap: 'pretty' }}>{dish.desc[lang]}</div>
            <div style={{ display: 'flex', gap: 10, marginTop: 10 }}>
              <button style={{ padding: '7px 14px', background: 'transparent', border: `1px solid ${ADM.lineStrong}`, borderRadius: 8, fontSize: 12 * fontScale, color: ADM.ink, fontWeight: 600, cursor: 'pointer' }}>✎ {t.edit}</button>
              <button style={{ padding: '7px 14px', background: 'transparent', border: `1px solid ${ADM.lineStrong}`, borderRadius: 8, fontSize: 12 * fontScale, color: ADM.ink, fontWeight: 600, cursor: 'pointer' }}>📷 {lang === 'es' ? 'Cambiar foto' : 'Change photo'}</button>
            </div>
          </div>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1.1fr 1fr', gap: 16 }}>
          {/* Ingredients */}
          <div style={{ background: ADM.surface, padding: 18, borderRadius: 14, border: `1px solid ${ADM.line}` }}>
            <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 18 * fontScale, color: ADM.ink, marginBottom: 10 }}>{t.ingredients}</div>
            {dish.ingredients.map((ing, i) => (
              <div key={i} style={{ display: 'flex', gap: 10, alignItems: 'center', padding: '8px 0', borderBottom: i < dish.ingredients.length - 1 || dish.subRecipes.length > 0 ? `1px solid ${ADM.line}` : 'none' }}>
                <div style={{ flex: 1, fontSize: 13 * fontScale, color: ADM.ink }}>{ing.name[lang]}</div>
                <div style={{ fontSize: 12 * fontScale, color: ADM.inkSoft, fontFamily: 'JetBrains Mono, monospace', width: 70, textAlign: 'right' }}>{ing.qty}</div>
                <div style={{ fontSize: 12 * fontScale, color: ADM.ink, fontFamily: 'JetBrains Mono, monospace', width: 70, textAlign: 'right', fontWeight: 600 }}>{hcpFmtFull(ing.cost)}</div>
              </div>
            ))}
            {/* Sub-recipes as linked ingredients */}
            {dish.subRecipes.map((srId, i) => {
              const sr = HCP_SUBRECIPES[srId];
              if (!sr) return null;
              return (
                <div key={srId} style={{ display: 'flex', gap: 10, alignItems: 'center', padding: '8px 0', borderBottom: i < dish.subRecipes.length - 1 ? `1px solid ${ADM.line}` : 'none', background: ADM.greenSoft, margin: '0 -10px', paddingLeft: 10, paddingRight: 10, borderRadius: 8 }}>
                  <span style={{ width: 4, height: 24, background: ADM.green, borderRadius: 2 }} />
                  <div style={{ flex: 1 }}>
                    <div style={{ fontSize: 9 * fontScale, fontWeight: 700, color: ADM.green, textTransform: 'uppercase', letterSpacing: 0.4 }}>↳ {t.subRecipe}</div>
                    <div style={{ fontSize: 13 * fontScale, color: ADM.ink, fontWeight: 600 }}>{sr.name[lang]}</div>
                  </div>
                  <div style={{ fontSize: 12 * fontScale, color: ADM.inkSoft, fontFamily: 'JetBrains Mono, monospace' }}>~50g</div>
                  <div style={{ fontSize: 12 * fontScale, color: ADM.ink, fontFamily: 'JetBrains Mono, monospace', width: 70, textAlign: 'right', fontWeight: 600 }}>{hcpFmtFull(Math.round(sr.ingredients.reduce((a, b) => a + b.cost, 0) * 0.15))}</div>
                </div>
              );
            })}
            <button style={{ marginTop: 12, width: '100%', padding: 10, background: 'transparent', border: `1.5px dashed ${ADM.lineStrong}`, borderRadius: 10, color: ADM.inkSoft, fontWeight: 600, fontSize: 12 * fontScale, cursor: 'pointer' }}>
              + {lang === 'es' ? 'Añadir ingrediente o sub-receta' : 'Add ingredient or sub-recipe'}
            </button>
          </div>

          {/* Cost structure */}
          <div>
            <div style={{ background: ADM.surface, padding: 18, borderRadius: 14, border: `1px solid ${ADM.line}`, marginBottom: 12 }}>
              <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 18 * fontScale, color: ADM.ink, marginBottom: 10 }}>{t.costStructure}</div>
              <CostRow label={t.foodCost} value={foodCost} total={totalCost} color={ADM.accent} fontScale={fontScale} />
              <CostRow label={t.labor} value={laborCost} total={totalCost} color={ADM.sun} fontScale={fontScale} />
              <CostRow label={t.overhead} value={overheadCost} total={totalCost} color={ADM.green} fontScale={fontScale} />
              <div style={{ marginTop: 10, paddingTop: 10, borderTop: `1px solid ${ADM.line}`, display: 'flex', justifyContent: 'space-between' }}>
                <div style={{ fontWeight: 700, color: ADM.ink, fontSize: 13 * fontScale }}>{lang === 'es' ? 'Costo total' : 'Total cost'}</div>
                <div style={{ fontWeight: 700, fontFamily: 'JetBrains Mono, monospace', fontSize: 14 * fontScale, color: ADM.ink }}>{hcpFmtFull(totalCost)}</div>
              </div>
            </div>

            <div style={{ background: ADM.ink, padding: 18, borderRadius: 14, color: '#fff' }}>
              <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 18 * fontScale, marginBottom: 12 }}>{t.suggestedPrice}</div>
              <div style={{ fontSize: 11 * fontScale, color: 'rgba(255,255,255,0.65)', textTransform: 'uppercase', letterSpacing: 0.4, marginBottom: 6 }}>{t.targetMargin}</div>
              <div style={{ display: 'flex', gap: 10, alignItems: 'center', marginBottom: 12 }}>
                <input type="range" min="30" max="80" value={targetMargin} onChange={e => setTargetMargin(+e.target.value)} style={{ flex: 1, accentColor: ADM.accent }} />
                <div style={{ fontFamily: 'JetBrains Mono, monospace', fontWeight: 700, fontSize: 14 * fontScale, width: 46, textAlign: 'right', color: ADM.accent }}>{targetMargin}%</div>
              </div>
              <div style={{ background: 'rgba(255,255,255,0.06)', padding: 14, borderRadius: 10, display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10 }}>
                <div>
                  <div style={{ fontSize: 10 * fontScale, color: 'rgba(255,255,255,0.6)', textTransform: 'uppercase', letterSpacing: 0.4 }}>{t.currentPrice}</div>
                  <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 22 * fontScale, marginTop: 2 }}>{hcpFmtFull(dish.price)}</div>
                  <div style={{ fontSize: 10 * fontScale, color: currentMargin >= targetMargin ? '#A8E4A0' : '#F4A261', marginTop: 2 }}>
                    {currentMargin}% {lang === 'es' ? 'margen' : 'margin'}
                  </div>
                </div>
                <div style={{ borderLeft: '1px solid rgba(255,255,255,0.15)', paddingLeft: 10 }}>
                  <div style={{ fontSize: 10 * fontScale, color: ADM.accent, textTransform: 'uppercase', letterSpacing: 0.4, fontWeight: 700 }}>★ {lang === 'es' ? 'Sugerido' : 'Suggested'}</div>
                  <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 22 * fontScale, marginTop: 2, color: ADM.accent }}>{hcpFmtFull(Math.round(suggestedPrice / 500) * 500)}</div>
                  <div style={{ fontSize: 10 * fontScale, color: 'rgba(255,255,255,0.6)', marginTop: 2 }}>{targetMargin}% {lang === 'es' ? 'margen' : 'margin'}</div>
                </div>
              </div>
              <button style={{ marginTop: 12, width: '100%', padding: '10px', background: ADM.accent, border: 'none', borderRadius: 10, fontWeight: 700, fontSize: 13 * fontScale, color: '#fff', cursor: 'pointer' }}>
                {lang === 'es' ? 'Aplicar precio sugerido' : 'Apply suggested price'}
              </button>
            </div>
          </div>
        </div>

        {/* Procedure */}
        <div style={{ marginTop: 16, background: ADM.surface, padding: 18, borderRadius: 14, border: `1px solid ${ADM.line}` }}>
          <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 18 * fontScale, color: ADM.ink, marginBottom: 10 }}>{t.procedure}</div>
          <ol style={{ margin: 0, paddingLeft: 0, listStyle: 'none' }}>
            {dish.steps[lang].map((s, i) => (
              <li key={i} style={{ display: 'flex', gap: 14, padding: '10px 0', borderBottom: i < dish.steps[lang].length - 1 ? `1px solid ${ADM.line}` : 'none' }}>
                <div style={{ width: 28, height: 28, borderRadius: 14, background: ADM.accent, color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 700, fontSize: 12 * fontScale, flexShrink: 0 }}>{i + 1}</div>
                <div style={{ fontSize: 13 * fontScale, color: ADM.ink, lineHeight: 1.5, textWrap: 'pretty' }}>{s}</div>
              </li>
            ))}
          </ol>
        </div>
      </div>
    </div>
  );
}

function CostRow({ label, value, total, color, fontScale }) {
  const pct = Math.round(value / total * 100);
  return (
    <div style={{ padding: '6px 0' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 12 * fontScale, color: ADM.ink, marginBottom: 4 }}>
        <span>{label}</span>
        <span style={{ fontFamily: 'JetBrains Mono, monospace', fontWeight: 600 }}>{hcpFmtFull(value)}</span>
      </div>
      <div style={{ height: 6, background: ADM.line, borderRadius: 3, overflow: 'hidden' }}>
        <div style={{ width: pct + '%', height: '100%', background: color }} />
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────
// Inventory & Purchasing
// ─────────────────────────────────────────────────────────────
function AdminInventory({ lang, fontScale }) {
  const t = HCP_I18N[lang];
  return (
    <div style={{ padding: 28, fontSize: 14 * fontScale }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 22 }}>
        <div>
          <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 30 * fontScale, color: ADM.ink }}>{t.inventory}</div>
          <div style={{ fontSize: 13 * fontScale, color: ADM.inkSoft, marginTop: 4 }}>
            {lang === 'es' ? 'Estado actualizado según ventas y compras' : 'Updated from sales and purchases'}
          </div>
        </div>
        <button style={{ padding: '10px 18px', background: ADM.ink, color: '#fff', border: 'none', borderRadius: 10, fontWeight: 600, fontSize: 13 * fontScale, cursor: 'pointer' }}>
          + {lang === 'es' ? 'Añadir ingrediente' : 'Add ingredient'}
        </button>
      </div>

      {/* Summary */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 14, marginBottom: 22 }}>
        <KpiCard label={lang === 'es' ? 'Valor total' : 'Total value'} value={hcpFmtFull(HCP_INVENTORY.reduce((s, i) => s + i.stock * i.cost, 0))} fontScale={fontScale} />
        <KpiCard label={t.inStock} value={HCP_INVENTORY.filter(i => i.status === 'ok').length + ''} fontScale={fontScale} />
        <KpiCard label={t.lowStock} value={HCP_INVENTORY.filter(i => i.status === 'low').length + ''} fontScale={fontScale} />
        <KpiCard label={t.outOfStock} value={HCP_INVENTORY.filter(i => i.status === 'out').length + ''} fontScale={fontScale} />
      </div>

      <div style={{ background: ADM.surface, borderRadius: 14, border: `1px solid ${ADM.line}`, overflow: 'hidden' }}>
          <div style={{ padding: '14px 20px', background: '#E8F0EB', fontSize: 10 * fontScale, fontWeight: 700, color: ADM.inkSoft, textTransform: 'uppercase', letterSpacing: 0.4, borderBottom: `1px solid ${ADM.line}`, display: 'grid', gridTemplateColumns: '2fr 1fr 1fr 1fr 1.5fr 1fr 120px' }}>
          <div>{lang === 'es' ? 'Ingrediente' : 'Ingredient'}</div>
          <div>{lang === 'es' ? 'Stock' : 'Stock'}</div>
          <div>{t.reorderPoint}</div>
          <div>{t.unitCost}</div>
          <div>{t.supplier}</div>
          <div>{lang === 'es' ? 'Estado' : 'Status'}</div>
          <div></div>
        </div>
        {HCP_INVENTORY.map((inv, i) => {
          const statusMap = {
            ok:  { label: t.inStock, bg: ADM.greenSoft, fg: ADM.green },
            low: { label: t.lowStock, bg: ADM.sunSoft, fg: '#6B5D47' },
            out: { label: t.outOfStock, bg: ADM.redSoft, fg: ADM.red },
          };
          const s = statusMap[inv.status];
          const pct = Math.min(100, Math.round(inv.stock / inv.reorder * 100));
          return (
            <div key={inv.id} style={{ display: 'grid', gridTemplateColumns: '2fr 1fr 1fr 1fr 1.5fr 1fr 120px', padding: '14px 20px', alignItems: 'center', borderBottom: i < HCP_INVENTORY.length - 1 ? `1px solid ${ADM.line}` : 'none', fontSize: 13 * fontScale }}>
              <div>
                <div style={{ fontWeight: 600, color: ADM.ink }}>{inv.name[lang]}</div>
              </div>
              <div>
                <div style={{ fontFamily: 'JetBrains Mono, monospace', fontWeight: 600, color: ADM.ink }}>{inv.stock} {inv.unit}</div>
                <div style={{ marginTop: 4, height: 4, background: ADM.line, borderRadius: 2, overflow: 'hidden', width: 70 }}>
                  <div style={{ width: pct + '%', height: '100%', background: inv.status === 'out' ? ADM.red : inv.status === 'low' ? ADM.sun : ADM.green }} />
                </div>
              </div>
              <div style={{ fontFamily: 'JetBrains Mono, monospace', color: ADM.inkSoft }}>{inv.reorder} {inv.unit}</div>
              <div style={{ fontFamily: 'JetBrains Mono, monospace', color: ADM.ink }}>{hcpFmtFull(inv.cost)}/{inv.unit}</div>
              <div style={{ color: ADM.inkSoft }}>{inv.supplier}</div>
              <div>
                <span style={{ padding: '4px 10px', background: s.bg, color: s.fg, borderRadius: 999, fontSize: 10 * fontScale, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.3 }}>{s.label}</span>
              </div>
              <div style={{ textAlign: 'right' }}>
                {inv.status !== 'ok' && (
                  <button style={{ padding: '6px 12px', background: ADM.accent, color: '#fff', border: 'none', borderRadius: 6, fontWeight: 600, fontSize: 11 * fontScale, cursor: 'pointer' }}>
                    {lang === 'es' ? 'Ordenar' : 'Order'}
                  </button>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────
// Purchasing (compras programadas)
// ─────────────────────────────────────────────────────────────
function AdminPurchasing({ lang, fontScale }) {
  const t = HCP_I18N[lang];
  // Group low/out items by supplier
  const bySupplier = {};
  HCP_INVENTORY.filter(i => i.status !== 'ok').forEach(i => {
    const need = Math.max(0, (i.reorder * 1.5) - i.stock);
    if (!bySupplier[i.supplier]) bySupplier[i.supplier] = [];
    bySupplier[i.supplier].push({ ...i, need });
  });

  const upcoming = [
    { date: lang === 'es' ? 'Lun 28 abr · 08:00' : 'Mon Apr 28 · 08:00', supplier: 'Corabastos', items: 4, total: 178000 },
    { date: lang === 'es' ? 'Mar 29 abr · 10:00' : 'Tue Apr 29 · 10:00', supplier: 'Carnes Premium', items: 3, total: 312000 },
    { date: lang === 'es' ? 'Mié 30 abr · 07:30' : 'Wed Apr 30 · 07:30', supplier: 'Granja El Rosal', items: 2, total: 96000 },
  ];

  return (
    <div style={{ padding: 28, fontSize: 14 * fontScale }}>
      <div style={{ marginBottom: 22 }}>
        <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 30 * fontScale, color: ADM.ink }}>{t.purchasing}</div>
        <div style={{ fontSize: 13 * fontScale, color: ADM.inkSoft, marginTop: 4 }}>
          {lang === 'es' ? 'Sugerencias automáticas según inventario y ventas proyectadas' : 'Automatic suggestions from inventory and projected sales'}
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1.6fr 1fr', gap: 18 }}>
        {/* Suggested purchase orders */}
        <div>
          <div style={{ fontSize: 11 * fontScale, fontWeight: 700, color: ADM.inkSoft, textTransform: 'uppercase', letterSpacing: 0.4, marginBottom: 10 }}>
            {lang === 'es' ? 'Órdenes sugeridas' : 'Suggested orders'}
          </div>
          {Object.entries(bySupplier).map(([sup, items]) => {
            const total = items.reduce((s, i) => s + Math.ceil(i.need) * i.cost, 0);
            return (
              <div key={sup} style={{ background: ADM.surface, padding: 18, borderRadius: 14, border: `1px solid ${ADM.line}`, marginBottom: 12 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
                  <div>
                    <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 18 * fontScale, color: ADM.ink }}>{sup}</div>
                    <div style={{ fontSize: 11 * fontScale, color: ADM.inkSoft, marginTop: 2 }}>{items.length} {lang === 'es' ? 'ingredientes' : 'ingredients'}</div>
                  </div>
                  <div style={{ textAlign: 'right' }}>
                    <div style={{ fontSize: 10 * fontScale, color: ADM.inkSoft, textTransform: 'uppercase', letterSpacing: 0.4 }}>{t.total}</div>
                    <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 22 * fontScale, color: ADM.ink }}>{hcpFmtFull(total)}</div>
                  </div>
                </div>
                <div style={{ borderTop: `1px solid ${ADM.line}`, paddingTop: 10 }}>
                  {items.map(it => (
                    <div key={it.id} style={{ display: 'flex', alignItems: 'center', padding: '6px 0', fontSize: 12 * fontScale }}>
                      <div style={{ flex: 1, color: ADM.ink }}>{it.name[lang]}</div>
                      <div style={{ fontFamily: 'JetBrains Mono, monospace', color: ADM.inkSoft, width: 100, textAlign: 'right' }}>{Math.ceil(it.need)} {it.unit}</div>
                      <div style={{ fontFamily: 'JetBrains Mono, monospace', color: ADM.ink, width: 100, textAlign: 'right', fontWeight: 600 }}>{hcpFmtFull(Math.ceil(it.need) * it.cost)}</div>
                    </div>
                  ))}
                </div>
                <div style={{ display: 'flex', gap: 8, marginTop: 12 }}>
                  <button style={{ flex: 1, padding: '10px', background: ADM.accent, color: '#fff', border: 'none', borderRadius: 8, fontWeight: 600, fontSize: 12 * fontScale, cursor: 'pointer' }}>
                    ✓ {t.schedulePurchase}
                  </button>
                  <button style={{ padding: '10px 14px', background: 'transparent', border: `1px solid ${ADM.lineStrong}`, borderRadius: 8, fontWeight: 600, fontSize: 12 * fontScale, color: ADM.ink, cursor: 'pointer' }}>
                    ✎ {t.edit}
                  </button>
                </div>
              </div>
            );
          })}
        </div>

        {/* Calendar */}
        <div>
          <div style={{ fontSize: 11 * fontScale, fontWeight: 700, color: ADM.inkSoft, textTransform: 'uppercase', letterSpacing: 0.4, marginBottom: 10 }}>
            {lang === 'es' ? 'Próximas entregas' : 'Upcoming deliveries'}
          </div>
          <div style={{ background: ADM.surface, padding: 18, borderRadius: 14, border: `1px solid ${ADM.line}` }}>
            {upcoming.map((u, i) => (
              <div key={i} style={{ display: 'flex', gap: 14, padding: '12px 0', borderBottom: i < upcoming.length - 1 ? `1px solid ${ADM.line}` : 'none' }}>
                <div style={{ width: 44, height: 44, borderRadius: 10, background: ADM.greenSoft, display: 'flex', alignItems: 'center', justifyContent: 'center', color: ADM.green, fontSize: 20, flexShrink: 0 }}>📦</div>
                <div style={{ flex: 1 }}>
                  <div style={{ fontWeight: 600, fontSize: 13 * fontScale, color: ADM.ink }}>{u.supplier}</div>
                  <div style={{ fontSize: 11 * fontScale, color: ADM.inkSoft, marginTop: 2, fontFamily: 'JetBrains Mono, monospace' }}>{u.date}</div>
                  <div style={{ fontSize: 11 * fontScale, color: ADM.inkSoft, marginTop: 2 }}>{u.items} {lang === 'es' ? 'ingredientes' : 'items'} · {hcpFmtFull(u.total)}</div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────
// Analytics
// ─────────────────────────────────────────────────────────────
function AdminAnalytics({ lang, fontScale }) {
  const t = HCP_I18N[lang];
  const [range, setRange] = React.useState('week');
  const k = HCP_ANALYTICS.kpis;

  return (
    <div style={{ padding: 28, fontSize: 14 * fontScale }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 22 }}>
        <div>
          <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 30 * fontScale, color: ADM.ink }}>{t.analytics}</div>
          <div style={{ fontSize: 13 * fontScale, color: ADM.inkSoft, marginTop: 4 }}>
            {lang === 'es' ? 'Desempeño de ventas, ganancias y platos' : 'Sales, profit and dish performance'}
          </div>
        </div>
        <div style={{ display: 'flex', gap: 4, background: ADM.surface, padding: 4, borderRadius: 10, border: `1px solid ${ADM.line}` }}>
          {[{ id: 'today', l: t.today }, { id: 'week', l: t.thisWeek }, { id: 'month', l: t.thisMonth }].map(r => (
            <button key={r.id} onClick={() => setRange(r.id)} style={{
              padding: '8px 16px', borderRadius: 7, border: 'none', fontWeight: 600, fontSize: 12 * fontScale, cursor: 'pointer',
              background: range === r.id ? ADM.ink : 'transparent',
              color: range === r.id ? '#fff' : ADM.inkSoft,
            }}>{r.l}</button>
          ))}
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 14, marginBottom: 18 }}>
        <KpiCard label={t.revenue} value={hcpFmtFull(k.revenue)} delta="+12.4%" fontScale={fontScale} />
        <KpiCard label={t.profit} value={hcpFmtFull(k.profit)} delta={`${Math.round(k.margin * 100)}%`} deltaLabel={lang === 'es' ? 'margen' : 'margin'} fontScale={fontScale} />
        <KpiCard label={t.orderCount} value={k.orders.toString()} delta="+8.1%" fontScale={fontScale} />
        <KpiCard label={t.avgTicket} value={hcpFmtFull(k.avgTicket)} delta="+2.3%" fontScale={fontScale} />
      </div>

      <div style={{ background: ADM.surface, padding: 22, borderRadius: 16, border: `1px solid ${ADM.line}`, marginBottom: 18 }}>
        <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 20 * fontScale, color: ADM.ink, marginBottom: 18 }}>
          {lang === 'es' ? 'Ingresos vs. ganancia' : 'Revenue vs. profit'}
        </div>
        <WeekBarChart data={HCP_ANALYTICS.week} lang={lang} fontScale={fontScale} />
      </div>

      <div style={{ background: ADM.surface, padding: 22, borderRadius: 16, border: `1px solid ${ADM.line}` }}>
        <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 20 * fontScale, color: ADM.ink, marginBottom: 14 }}>
          {t.profitByDish}
        </div>
        <div style={{ display: 'grid', gridTemplateColumns: '40px 2fr 1fr 1fr 1fr 1.5fr', fontSize: 10 * fontScale, fontWeight: 700, color: ADM.inkSoft, textTransform: 'uppercase', letterSpacing: 0.4, padding: '8px 0', borderBottom: `1px solid ${ADM.line}` }}>
          <div></div>
          <div>{lang === 'es' ? 'Plato' : 'Dish'}</div>
          <div style={{ textAlign: 'right' }}>{lang === 'es' ? 'Vendidos' : 'Sold'}</div>
          <div style={{ textAlign: 'right' }}>{t.revenue}</div>
          <div style={{ textAlign: 'right' }}>{t.profit}</div>
          <div></div>
        </div>
        {HCP_ANALYTICS.topDishes.map(td => {
          const d = hcpDish(td.id);
          const margin = td.profit / td.revenue;
          const maxProfit = Math.max(...HCP_ANALYTICS.topDishes.map(x => x.profit));
          return (
            <div key={td.id} style={{ display: 'grid', gridTemplateColumns: '40px 2fr 1fr 1fr 1fr 1.5fr', padding: '12px 0', alignItems: 'center', borderBottom: `1px solid ${ADM.line}` }}>
              <DishThumbAdm dish={d} size={32} />
              <div style={{ fontWeight: 600, fontSize: 13 * fontScale, color: ADM.ink }}>{d.name[lang]}</div>
              <div style={{ textAlign: 'right', fontFamily: 'JetBrains Mono, monospace', fontSize: 13 * fontScale, color: ADM.inkSoft }}>{td.sold}</div>
              <div style={{ textAlign: 'right', fontFamily: 'JetBrains Mono, monospace', fontSize: 13 * fontScale, color: ADM.ink }}>{hcpFmtFull(td.revenue)}</div>
              <div style={{ textAlign: 'right', fontFamily: 'JetBrains Mono, monospace', fontSize: 13 * fontScale, color: ADM.green, fontWeight: 600 }}>+{hcpFmtFull(td.profit)}</div>
              <div style={{ paddingLeft: 18, display: 'flex', alignItems: 'center', gap: 8 }}>
                <div style={{ flex: 1, height: 6, background: ADM.line, borderRadius: 3, overflow: 'hidden' }}>
                  <div style={{ width: `${td.profit / maxProfit * 100}%`, height: '100%', background: ADM.green }} />
                </div>
                <div style={{ fontSize: 10 * fontScale, color: ADM.inkSoft, fontFamily: 'JetBrains Mono, monospace', width: 32 }}>{Math.round(margin * 100)}%</div>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────
// Invoices
// ─────────────────────────────────────────────────────────────
function AdminInvoices({ lang, fontScale }) {
  const t = HCP_I18N[lang];
  const invoices = HCP_ORDERS.filter(o => o.status === 'delivered' || o.status === 'ready').concat(
    [{ id: '#A039', customer: 'Laura Torres', total: 48000, time: '10:15', type: 'delivery', paid: true, status: 'delivered', items: [] },
     { id: '#A037', customer: 'Miguel Rojas', total: 30000, time: '09:42', type: 'pickup',   paid: true, status: 'delivered', items: [] }]
  );

  return (
    <div style={{ padding: 28, fontSize: 14 * fontScale }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 22 }}>
        <div>
          <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 30 * fontScale, color: ADM.ink }}>{t.invoices}</div>
          <div style={{ fontSize: 13 * fontScale, color: ADM.inkSoft, marginTop: 4 }}>
            {lang === 'es' ? 'Facturación electrónica · DIAN' : 'Electronic invoicing · DIAN'}
          </div>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <button style={{ padding: '10px 18px', background: 'transparent', border: `1px solid ${ADM.lineStrong}`, borderRadius: 10, fontWeight: 600, fontSize: 13 * fontScale, color: ADM.ink, cursor: 'pointer' }}>
            ↓ {lang === 'es' ? 'Exportar CSV' : 'Export CSV'}
          </button>
          <button style={{ padding: '10px 18px', background: ADM.ink, color: '#fff', border: 'none', borderRadius: 10, fontWeight: 600, fontSize: 13 * fontScale, cursor: 'pointer' }}>
            + {lang === 'es' ? 'Nueva factura' : 'New invoice'}
          </button>
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 14, marginBottom: 20 }}>
        <KpiCard label={lang === 'es' ? 'Facturadas hoy' : 'Invoiced today'} value="7" fontScale={fontScale} />
        <KpiCard label={lang === 'es' ? 'Valor facturado' : 'Invoiced value'} value={hcpFmtFull(298500)} fontScale={fontScale} />
        <KpiCard label={lang === 'es' ? 'Pagos pendientes' : 'Pending payments'} value="0" deltaLabel={lang === 'es' ? '✓ al día' : '✓ up to date'} fontScale={fontScale} />
      </div>

      <div style={{ background: ADM.surface, borderRadius: 14, border: `1px solid ${ADM.line}`, overflow: 'hidden' }}>
        <div style={{ display: 'grid', gridTemplateColumns: '100px 2fr 1fr 1fr 1fr 110px', padding: '14px 20px', background: '#E8F0EB', fontSize: 10 * fontScale, fontWeight: 700, color: ADM.inkSoft, textTransform: 'uppercase', letterSpacing: 0.4, borderBottom: `1px solid ${ADM.line}` }}>
          <div># {lang === 'es' ? 'Factura' : 'Invoice'}</div>
          <div>{lang === 'es' ? 'Cliente' : 'Customer'}</div>
          <div>{lang === 'es' ? 'Fecha' : 'Date'}</div>
          <div>{lang === 'es' ? 'Pago' : 'Payment'}</div>
          <div style={{ textAlign: 'right' }}>{t.total}</div>
          <div></div>
        </div>
        {invoices.map((o, i) => (
          <div key={o.id} style={{ display: 'grid', gridTemplateColumns: '100px 2fr 1fr 1fr 1fr 110px', padding: '14px 20px', alignItems: 'center', borderBottom: i < invoices.length - 1 ? `1px solid ${ADM.line}` : 'none', fontSize: 13 * fontScale }}>
            <div style={{ fontFamily: 'JetBrains Mono, monospace', fontWeight: 700, color: ADM.ink }}>FE-{o.id.slice(1)}</div>
            <div style={{ fontWeight: 600, color: ADM.ink }}>{o.customer}</div>
            <div style={{ color: ADM.inkSoft, fontSize: 12 * fontScale }}>{lang === 'es' ? '24 abr' : 'Apr 24'} · {o.time}</div>
            <div>
              <span style={{ padding: '3px 8px', background: ADM.greenSoft, color: ADM.green, borderRadius: 5, fontSize: 10 * fontScale, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.3 }}>✓ {t.paymentVerified}</span>
            </div>
            <div style={{ textAlign: 'right', fontFamily: 'JetBrains Mono, monospace', fontWeight: 700, color: ADM.ink }}>{hcpFmtFull(o.total)}</div>
            <div style={{ textAlign: 'right' }}>
              <button style={{ padding: '6px 12px', background: 'transparent', border: `1px solid ${ADM.lineStrong}`, borderRadius: 6, fontWeight: 600, fontSize: 11 * fontScale, color: ADM.ink, cursor: 'pointer' }}>↓ PDF</button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────
// Admin shell
// ─────────────────────────────────────────────────────────────
function AdminDashboard({ lang, fontScale, initial = 'overview' }) {
  const [active, setActive] = React.useState(initial);
  let content;
  if (active === 'overview') content = <AdminOverview lang={lang} fontScale={fontScale} onNav={setActive} />;
  else if (active === 'orders') content = <AdminOrders lang={lang} fontScale={fontScale} />;
  else if (active === 'menu') content = <AdminRecipeEditor lang={lang} fontScale={fontScale} />;
  else if (active === 'inventory') content = <AdminInventory lang={lang} fontScale={fontScale} />;
  else if (active === 'purchasing') content = <AdminPurchasing lang={lang} fontScale={fontScale} />;
  else if (active === 'analytics') content = <AdminAnalytics lang={lang} fontScale={fontScale} />;
  else if (active === 'invoices') content = <AdminInvoices lang={lang} fontScale={fontScale} />;

  return (
    <div style={{ display: 'flex', width: '100%', height: '100%', background: ADM.bg, overflow: 'hidden' }}>
      <AdminSidebar active={active} onNav={setActive} lang={lang} fontScale={fontScale} />
      <div style={{ flex: 1, overflowY: 'auto' }}>{content}</div>
    </div>
  );
}

Object.assign(window, { AdminDashboard, ADM });
