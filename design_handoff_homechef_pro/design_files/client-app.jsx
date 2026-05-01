// Customer-facing mobile app — catalog, detail, cart, tracking, reviews
// All screens wrapped in IOSDevice. Uses shared data from data.jsx.

const BRAND = new Proxy({}, { get: (_, k) => hcpTheme()[k] });

// ─────────────────────────────────────────────────────────────
// Reusable atoms
// ─────────────────────────────────────────────────────────────
function DishThumb({ dish, size = 72, rounded = 14 }) {
  return (
    <div style={{
      width: size, height: size, borderRadius: rounded,
      background: `linear-gradient(135deg, ${dish.swatch[0]}, ${dish.swatch[1]})`,
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      fontSize: size * 0.45, flexShrink: 0, overflow: 'hidden', position: 'relative',
      boxShadow: 'inset 0 0 0 1px rgba(0,0,0,0.04)',
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
          <div style={{
            position: 'absolute', inset: 0,
            backgroundImage: 'repeating-linear-gradient(45deg, transparent 0 8px, rgba(255,255,255,0.08) 8px 9px)',
          }} />
          <span style={{ position: 'relative', filter: 'drop-shadow(0 2px 4px rgba(0,0,0,0.15))' }}>{dish.emoji}</span>
        </React.Fragment>
      )}
    </div>
  );
}

function Pill({ children, tone = 'neutral' }) {
  const tones = {
    neutral: { bg: BRAND.line, fg: BRAND.inkSoft },
    accent: { bg: '#DCEEE5', fg: BRAND.accentDark },
    green: { bg: BRAND.greenSoft, fg: BRAND.green },
    sun: { bg: '#E8E0D0', fg: '#6B5D47' },
  };
  const t = tones[tone];
  return (
    <span style={{
      display: 'inline-flex', alignItems: 'center', gap: 4,
      padding: '3px 9px', borderRadius: 999, fontSize: 11, fontWeight: 600,
      background: t.bg, color: t.fg, letterSpacing: 0.2, textTransform: 'uppercase',
    }}>{children}</span>
  );
}

function StarRow({ rating, size = 12 }) {
  return (
    <div style={{ display: 'inline-flex', gap: 1, color: '#8B7340', fontSize: size, lineHeight: 1 }}>
      {[1,2,3,4,5].map(i => (
        <span key={i} style={{ opacity: i <= Math.round(rating) ? 1 : 0.25 }}>★</span>
      ))}
    </div>
  );
}

// ─────────────────────────────────────────────────────────────
// Screen 1 — Catalog / Browse
// ─────────────────────────────────────────────────────────────
function ClientCatalog({ lang, onOpen, onOpenCart, cartCount, fontScale }) {
  const t = HCP_I18N[lang];
  const popular = HCP_DISHES.filter(d => d.tag === 'popular');
  const nu = HCP_DISHES.filter(d => d.tag === 'new');
  const all = HCP_DISHES;

  return (
    <div style={{ background: BRAND.bg, minHeight: '100%', paddingBottom: 100, fontSize: 14 * fontScale }}>
      {/* Header */}
      <div style={{ padding: '8px 20px 18px' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
          <div>
            <div style={{ fontSize: 12 * fontScale, color: BRAND.inkSoft, fontWeight: 500 }}>
              {lang === 'es' ? 'Hola, María' : 'Hi, María'} 👋
            </div>
            <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 28 * fontScale, color: BRAND.ink, lineHeight: 1.1, marginTop: 2 }}>
              {t.todaysMenu}
            </div>
          </div>
          <div style={{ position: 'relative' }}>
            <button onClick={onOpenCart} style={{
              width: 44, height: 44, borderRadius: 22, background: BRAND.ink, border: 'none',
              display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#fff', fontSize: 18, cursor: 'pointer',
            }}>🛒</button>
            {cartCount > 0 && (
              <div style={{
                position: 'absolute', top: -4, right: -4, background: BRAND.accent, color: '#fff',
                width: 20, height: 20, borderRadius: 10, fontSize: 11, fontWeight: 700,
                display: 'flex', alignItems: 'center', justifyContent: 'center',
              }}>{cartCount}</div>
            )}
          </div>
        </div>
        {/* Search */}
        <div style={{
          background: '#fff', borderRadius: 14, padding: '12px 14px',
          display: 'flex', alignItems: 'center', gap: 10, border: `1px solid ${BRAND.line}`,
        }}>
          <span style={{ color: BRAND.inkMuted, fontSize: 15 }}>🔍</span>
          <div style={{ color: BRAND.inkMuted, fontSize: 14 * fontScale }}>
            {lang === 'es' ? 'Buscar plato, ingrediente…' : 'Search dish, ingredient…'}
          </div>
        </div>
      </div>

      {/* Popular section */}
      <SectionHeader title={t.popular} lang={lang} fontScale={fontScale} />
      <div style={{ display: 'flex', gap: 12, padding: '0 20px 20px', overflowX: 'auto' }}>
        {popular.map(d => (
          <div key={d.id} onClick={() => onOpen(d.id)} style={{
            background: BRAND.card, borderRadius: 18, padding: 12, width: 180, flexShrink: 0,
            border: `1px solid ${BRAND.line}`, cursor: 'pointer',
          }}>
            <div style={{ position: 'relative' }}>
              <DishThumb dish={d} size={156} rounded={12} />
              <div style={{ position: 'absolute', top: 8, left: 8 }}>
                <Pill tone="accent">🔥 {t.popular}</Pill>
              </div>
            </div>
            <div style={{ fontWeight: 600, fontSize: 14 * fontScale, color: BRAND.ink, marginTop: 10, lineHeight: 1.2 }}>
              {d.name[lang]}
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 8 }}>
              <div style={{ fontWeight: 700, color: BRAND.ink, fontSize: 14 * fontScale }}>{hcpFmtFull(d.price)}</div>
              <div style={{ fontSize: 11 * fontScale, color: BRAND.inkSoft, display: 'flex', alignItems: 'center', gap: 3 }}>
                <StarRow rating={d.rating} /> {d.rating}
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* New this week */}
      <SectionHeader title={t.newThisWeek} lang={lang} fontScale={fontScale} />
      <div style={{ padding: '0 20px 8px' }}>
        {nu.map(d => (
          <DishRow key={d.id} dish={d} lang={lang} onOpen={onOpen} fontScale={fontScale} showNew />
        ))}
      </div>

      {/* All */}
      <SectionHeader title={lang === 'es' ? 'Todo el menú' : 'Full menu'} lang={lang} fontScale={fontScale} />
      <div style={{ padding: '0 20px 16px' }}>
        {all.filter(d => d.tag !== 'new' && d.tag !== 'popular').map(d => (
          <DishRow key={d.id} dish={d} lang={lang} onOpen={onOpen} fontScale={fontScale} />
        ))}
      </div>
    </div>
  );
}

function SectionHeader({ title, lang, fontScale, right }) {
  return (
    <div style={{ padding: '8px 20px 10px', display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
      <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 22 * fontScale, color: BRAND.ink }}>{title}</div>
      {right || <div style={{ fontSize: 12 * fontScale, color: BRAND.accent, fontWeight: 600 }}>{lang === 'es' ? 'Ver todo →' : 'See all →'}</div>}
    </div>
  );
}

function DishRow({ dish, lang, onOpen, fontScale, showNew }) {
  return (
    <div onClick={() => onOpen(dish.id)} style={{
      background: BRAND.card, borderRadius: 16, padding: 12, marginBottom: 10,
      display: 'flex', gap: 12, alignItems: 'center', border: `1px solid ${BRAND.line}`, cursor: 'pointer',
    }}>
      <DishThumb dish={dish} size={72} />
      <div style={{ flex: 1, minWidth: 0 }}>
        <div style={{ display: 'flex', gap: 6, alignItems: 'center', marginBottom: 4 }}>
          <div style={{ fontWeight: 600, fontSize: 14 * fontScale, color: BRAND.ink }}>{dish.name[lang]}</div>
          {showNew && <Pill tone="green">{lang === 'es' ? 'Nuevo' : 'New'}</Pill>}
        </div>
        <div style={{
          fontSize: 12 * fontScale, color: BRAND.inkSoft, lineHeight: 1.35,
          display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical',
          overflow: 'hidden', textWrap: 'pretty',
        }}>{dish.desc[lang]}</div>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 6 }}>
          <div style={{ fontWeight: 700, fontSize: 14 * fontScale, color: BRAND.ink }}>{hcpFmtFull(dish.price)}</div>
          <div style={{ fontSize: 11 * fontScale, color: BRAND.inkSoft, display: 'flex', alignItems: 'center', gap: 4 }}>
            <StarRow rating={dish.rating} /> {dish.rating} · {dish.reviews}
          </div>
        </div>
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────
// Screen 2 — Dish detail + order
// ─────────────────────────────────────────────────────────────
function ClientDetail({ dishId, lang, onBack, onAdd, fontScale }) {
  const dish = hcpDish(dishId);
  const t = HCP_I18N[lang];
  const [qty, setQty] = React.useState(1);
  const reviews = HCP_REVIEWS.filter(r => r.dishId === dishId).slice(0, 2);

  return (
    <div style={{ background: BRAND.bg, minHeight: '100%', paddingBottom: 100, fontSize: 14 * fontScale }}>
      {/* Hero */}
      <div style={{ position: 'relative', height: 280,
        background: `linear-gradient(135deg, ${dish.swatch[0]}, ${dish.swatch[1]})`,
      }}>
        <div style={{
          position: 'absolute', inset: 0,
          backgroundImage: 'repeating-linear-gradient(45deg, transparent 0 12px, rgba(255,255,255,0.1) 12px 14px)',
        }} />
        <div style={{ position: 'absolute', top: 12, left: 16 }}>
          <button onClick={onBack} style={{
            width: 40, height: 40, borderRadius: 20, background: 'rgba(255,255,255,0.9)',
            border: 'none', fontSize: 18, cursor: 'pointer',
          }}>←</button>
        </div>
        <div style={{
          position: 'absolute', bottom: 0, left: 0, right: 0, padding: '40px 20px 20px',
          background: 'linear-gradient(180deg, transparent, rgba(0,0,0,0.35))',
        }}>
          <div style={{ fontSize: 64, textAlign: 'center', filter: 'drop-shadow(0 4px 10px rgba(0,0,0,0.3))' }}>{dish.emoji}</div>
        </div>
      </div>

      {/* Card overlap */}
      <div style={{
        background: BRAND.card, borderRadius: '24px 24px 0 0', marginTop: -20, padding: 20, position: 'relative',
      }}>
        <div style={{ display: 'flex', gap: 8, marginBottom: 8 }}>
          <Pill tone="neutral">{dish.category[lang]}</Pill>
          <Pill tone="sun">⏱ {dish.prepTime} {t.minutes}</Pill>
        </div>
        <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 26 * fontScale, color: BRAND.ink, lineHeight: 1.1 }}>
          {dish.name[lang]}
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 8, fontSize: 12 * fontScale, color: BRAND.inkSoft }}>
          <StarRow rating={dish.rating} size={14} />
          <span style={{ fontWeight: 600, color: BRAND.ink }}>{dish.rating}</span>
          <span>· {dish.reviews} {t.reviews.toLowerCase()}</span>
        </div>
        <div style={{ marginTop: 14, fontSize: 14 * fontScale, color: BRAND.inkSoft, lineHeight: 1.5, textWrap: 'pretty' }}>
          {dish.desc[lang]}
        </div>

        {/* Ingredients teaser */}
        <div style={{ marginTop: 18, padding: 14, background: BRAND.bg, borderRadius: 14 }}>
          <div style={{ fontSize: 12 * fontScale, fontWeight: 700, color: BRAND.green, textTransform: 'uppercase', letterSpacing: 0.5, marginBottom: 8 }}>
            {lang === 'es' ? '✿ Ingredientes frescos' : '✿ Fresh ingredients'}
          </div>
          <div style={{ fontSize: 13 * fontScale, color: BRAND.ink, lineHeight: 1.5, textWrap: 'pretty' }}>
            {dish.ingredients.slice(0, 5).map(i => i.name[lang]).join(' · ')}
            {dish.ingredients.length > 5 && ` · +${dish.ingredients.length - 5}`}
          </div>
        </div>

        {/* Reviews */}
        <div style={{ marginTop: 22 }}>
          <SectionHeader title={t.reviews} lang={lang} fontScale={fontScale} />
          {reviews.map(r => (
            <div key={r.id} style={{ padding: '12px 0', borderBottom: `1px solid ${BRAND.line}` }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 4 }}>
                <div style={{ width: 28, height: 28, borderRadius: 14, background: BRAND.line,
                  display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 11, fontWeight: 700, color: BRAND.inkSoft }}>
                  {r.customer[0]}
                </div>
                <div style={{ fontWeight: 600, fontSize: 13 * fontScale, color: BRAND.ink }}>{r.customer}</div>
                <div style={{ marginLeft: 'auto', fontSize: 11 * fontScale, color: BRAND.inkMuted }}>{r.time[lang]}</div>
              </div>
              <StarRow rating={r.rating} />
              <div style={{ marginTop: 4, fontSize: 13 * fontScale, color: BRAND.inkSoft, lineHeight: 1.45 }}>{r.text[lang]}</div>
            </div>
          ))}
        </div>
      </div>

      {/* Bottom bar */}
      <div style={{
        position: 'absolute', bottom: 0, left: 0, right: 0, background: '#fff',
        padding: '14px 20px 30px', borderTop: `1px solid ${BRAND.line}`,
        display: 'flex', gap: 12, alignItems: 'center',
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 0, border: `1px solid ${BRAND.line}`, borderRadius: 999, padding: 2 }}>
          <button onClick={() => setQty(Math.max(1, qty - 1))} style={{
            width: 34, height: 34, borderRadius: 999, border: 'none', background: 'transparent', fontSize: 18, cursor: 'pointer', color: BRAND.ink,
          }}>−</button>
          <div style={{ padding: '0 10px', fontWeight: 700, minWidth: 20, textAlign: 'center' }}>{qty}</div>
          <button onClick={() => setQty(qty + 1)} style={{
            width: 34, height: 34, borderRadius: 999, border: 'none', background: 'transparent', fontSize: 18, cursor: 'pointer', color: BRAND.ink,
          }}>+</button>
        </div>
        <button onClick={() => onAdd(dish, qty)} style={{
          flex: 1, height: 50, borderRadius: 999, border: 'none',
          background: BRAND.accent, color: '#fff', fontWeight: 700, fontSize: 15 * fontScale, cursor: 'pointer',
          display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 10,
          boxShadow: '0 6px 18px rgba(255,140,66,0.35)',
        }}>
          <span>{t.addToCart}</span>
          <span style={{ opacity: 0.8 }}>·</span>
          <span>{hcpFmtFull(dish.price * qty)}</span>
        </button>
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────
// Screen 3 — Cart + checkout
// ─────────────────────────────────────────────────────────────
function ClientCart({ cart, lang, onBack, onPlace, fontScale }) {
  const t = HCP_I18N[lang];
  const [mode, setMode] = React.useState('delivery');
  const [payment, setPayment] = React.useState('card');
  const subtotal = cart.reduce((s, it) => s + it.dish.price * it.qty, 0);
  const fee = mode === 'delivery' ? 4500 : 0;
  const tax = Math.round(subtotal * 0.08);
  const total = subtotal + fee + tax;

  return (
    <div style={{ background: BRAND.bg, minHeight: '100%', paddingBottom: 120, fontSize: 14 * fontScale }}>
      <div style={{ padding: '8px 20px 0', display: 'flex', alignItems: 'center', gap: 12 }}>
        <button onClick={onBack} style={{ width: 40, height: 40, borderRadius: 20, background: '#fff', border: `1px solid ${BRAND.line}`, fontSize: 18, cursor: 'pointer' }}>←</button>
        <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 26 * fontScale, color: BRAND.ink }}>{t.cart}</div>
      </div>

      {/* Mode toggle */}
      <div style={{ margin: '18px 20px 14px', background: '#fff', borderRadius: 14, padding: 4, display: 'flex', border: `1px solid ${BRAND.line}` }}>
        {['pickup', 'delivery'].map(m => (
          <button key={m} onClick={() => setMode(m)} style={{
            flex: 1, padding: '10px 12px', borderRadius: 10, border: 'none',
            background: mode === m ? BRAND.ink : 'transparent',
            color: mode === m ? '#fff' : BRAND.inkSoft,
            fontWeight: 600, fontSize: 13 * fontScale, cursor: 'pointer',
            display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 6,
          }}>
            <span>{m === 'pickup' ? '🏪' : '🛵'}</span> {t[m]}
          </button>
        ))}
      </div>

      {/* Address (delivery) */}
      {mode === 'delivery' && (
        <div style={{ margin: '0 20px 14px', background: '#fff', padding: 14, borderRadius: 14, border: `1px solid ${BRAND.line}` }}>
          <div style={{ fontSize: 11 * fontScale, fontWeight: 700, color: BRAND.inkSoft, textTransform: 'uppercase', letterSpacing: 0.4 }}>
            {lang === 'es' ? 'Entregar en' : 'Deliver to'}
          </div>
          <div style={{ fontWeight: 600, fontSize: 14 * fontScale, color: BRAND.ink, marginTop: 4 }}>Cra 13 #85-42, Chapinero</div>
          <div style={{ fontSize: 12 * fontScale, color: BRAND.inkSoft, marginTop: 2 }}>
            {lang === 'es' ? 'Entrega estimada: 35-45 min' : 'Estimated: 35-45 min'}
          </div>
        </div>
      )}

      {/* Items */}
      <div style={{ padding: '0 20px' }}>
        {cart.map((it, i) => (
          <div key={i} style={{
            background: '#fff', padding: 12, borderRadius: 14, marginBottom: 10,
            display: 'flex', gap: 12, alignItems: 'center', border: `1px solid ${BRAND.line}`,
          }}>
            <DishThumb dish={it.dish} size={56} />
            <div style={{ flex: 1 }}>
              <div style={{ fontWeight: 600, fontSize: 14 * fontScale, color: BRAND.ink }}>{it.dish.name[lang]}</div>
              <div style={{ fontSize: 12 * fontScale, color: BRAND.inkSoft, marginTop: 2 }}>
                {it.qty} × {hcpFmtFull(it.dish.price)}
              </div>
            </div>
            <div style={{ fontWeight: 700, fontSize: 14 * fontScale, color: BRAND.ink }}>{hcpFmtFull(it.dish.price * it.qty)}</div>
          </div>
        ))}
      </div>

      {/* Notes */}
      <div style={{ margin: '4px 20px 14px' }}>
        <div style={{ background: '#fff', padding: 14, borderRadius: 14, border: `1px solid ${BRAND.line}` }}>
          <div style={{ fontSize: 11 * fontScale, fontWeight: 700, color: BRAND.inkSoft, textTransform: 'uppercase', letterSpacing: 0.4, marginBottom: 6 }}>
            📝 {t.notes}
          </div>
          <div style={{ fontSize: 13 * fontScale, color: BRAND.inkMuted }}>
            {lang === 'es' ? 'Ej: poco picante, sin cilantro…' : 'E.g. less spicy, no cilantro…'}
          </div>
        </div>
      </div>

      {/* Payment */}
      <div style={{ padding: '0 20px 12px' }}>
        <div style={{ fontSize: 12 * fontScale, fontWeight: 700, color: BRAND.inkSoft, textTransform: 'uppercase', letterSpacing: 0.4, marginBottom: 8 }}>
          {lang === 'es' ? 'Método de pago' : 'Payment method'}
        </div>
        {[
          { id: 'card', label: lang === 'es' ? 'Visa •••• 4821' : 'Visa •••• 4821', icon: '💳' },
          { id: 'nequi', label: 'Nequi', icon: '📱' },
          { id: 'cash', label: lang === 'es' ? 'Efectivo al recibir' : 'Cash on arrival', icon: '💵' },
        ].map(p => (
          <div key={p.id} onClick={() => setPayment(p.id)} style={{
            background: '#fff', padding: 12, borderRadius: 12, marginBottom: 8,
            display: 'flex', alignItems: 'center', gap: 10, cursor: 'pointer',
            border: `1.5px solid ${payment === p.id ? BRAND.accent : BRAND.line}`,
          }}>
            <span style={{ fontSize: 18 }}>{p.icon}</span>
            <div style={{ fontWeight: 600, fontSize: 13 * fontScale, color: BRAND.ink, flex: 1 }}>{p.label}</div>
            <div style={{
              width: 20, height: 20, borderRadius: 10, border: `2px solid ${payment === p.id ? BRAND.accent : BRAND.line}`,
              display: 'flex', alignItems: 'center', justifyContent: 'center',
            }}>
              {payment === p.id && <div style={{ width: 10, height: 10, borderRadius: 5, background: BRAND.accent }} />}
            </div>
          </div>
        ))}
      </div>

      {/* Totals */}
      <div style={{ margin: '0 20px 14px', background: '#fff', padding: 14, borderRadius: 14, border: `1px solid ${BRAND.line}` }}>
        <TotalRow label={t.subtotal} value={hcpFmtFull(subtotal)} fontScale={fontScale} />
        {fee > 0 && <TotalRow label={t.deliveryFee} value={hcpFmtFull(fee)} fontScale={fontScale} />}
        <TotalRow label={t.tax} value={hcpFmtFull(tax)} fontScale={fontScale} />
        <div style={{ height: 1, background: BRAND.line, margin: '8px 0' }} />
        <TotalRow label={t.total} value={hcpFmtFull(total)} fontScale={fontScale} bold />
      </div>

      {/* CTA */}
      <div style={{
        position: 'absolute', bottom: 0, left: 0, right: 0, background: '#fff', padding: '14px 20px 30px',
        borderTop: `1px solid ${BRAND.line}`,
      }}>
        <button onClick={onPlace} style={{
          width: '100%', height: 54, borderRadius: 999, border: 'none',
          background: BRAND.accent, color: '#fff', fontWeight: 700, fontSize: 15 * fontScale, cursor: 'pointer',
          display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 10,
          boxShadow: '0 6px 18px rgba(255,140,66,0.35)',
        }}>
          {t.placeOrder} · {hcpFmtFull(total)}
        </button>
      </div>
    </div>
  );
}

function TotalRow({ label, value, bold, fontScale }) {
  return (
    <div style={{ display: 'flex', justifyContent: 'space-between', padding: '4px 0',
      fontSize: (bold ? 15 : 13) * fontScale, color: BRAND.ink, fontWeight: bold ? 700 : 500 }}>
      <span style={{ color: bold ? BRAND.ink : BRAND.inkSoft }}>{label}</span>
      <span>{value}</span>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────
// Screen 4 — Order tracking
// ─────────────────────────────────────────────────────────────
function ClientTracking({ lang, fontScale }) {
  const t = HCP_I18N[lang];
  const steps = [
    { id: 'received',  label: t.orderReceived, icon: '✓', done: true, time: '12:42' },
    { id: 'cooking',   label: t.cooking,       icon: '🔥', done: true, time: '12:48', current: false },
    { id: 'ready',     label: t.ready,         icon: '📦', done: true, time: '13:12', current: false },
    { id: 'ontheway',  label: t.onTheWay,      icon: '🛵', done: false, time: '—',    current: true },
    { id: 'delivered', label: t.delivered,     icon: '🏠', done: false, time: '—' },
  ];

  return (
    <div style={{ background: BRAND.bg, minHeight: '100%', paddingBottom: 30, fontSize: 14 * fontScale }}>
      <div style={{ padding: '8px 20px 18px' }}>
        <div style={{ fontSize: 12 * fontScale, color: BRAND.inkSoft, fontWeight: 500 }}>
          {lang === 'es' ? 'Pedido' : 'Order'} #A041
        </div>
        <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 26 * fontScale, color: BRAND.ink, marginTop: 2 }}>
          {lang === 'es' ? 'Tu comida va en camino' : 'Your food is on the way'}
        </div>
      </div>

      {/* Map placeholder */}
      <div style={{ margin: '0 20px 16px', height: 180, borderRadius: 18, position: 'relative', overflow: 'hidden',
        background: `linear-gradient(135deg, ${BRAND.greenSoft}, #F3E8CF)`,
        border: `1px solid ${BRAND.line}`,
      }}>
        <div style={{ position: 'absolute', inset: 0,
          backgroundImage: 'repeating-linear-gradient(0deg, rgba(0,0,0,0.04) 0 1px, transparent 1px 32px), repeating-linear-gradient(90deg, rgba(0,0,0,0.04) 0 1px, transparent 1px 32px)' }} />
        {/* Route */}
        <svg style={{ position: 'absolute', inset: 0, width: '100%', height: '100%' }}>
          <path d="M 40 140 Q 120 80, 200 100 T 340 50" stroke={BRAND.accent} strokeWidth="3" strokeDasharray="6 4" fill="none" />
        </svg>
        <div style={{ position: 'absolute', left: 32, bottom: 126, width: 16, height: 16, borderRadius: 8, background: BRAND.green, border: '3px solid #fff', boxShadow: '0 2px 6px rgba(0,0,0,0.15)' }} />
        <div style={{ position: 'absolute', left: 190, top: 82, fontSize: 28 }}>🛵</div>
        <div style={{ position: 'absolute', right: 28, top: 38, width: 18, height: 18, borderRadius: 9, background: BRAND.accent, border: '3px solid #fff', boxShadow: '0 2px 6px rgba(0,0,0,0.15)' }} />

        <div style={{ position: 'absolute', bottom: 12, left: 12, right: 12, background: 'rgba(255,255,255,0.95)', padding: 10, borderRadius: 12, display: 'flex', alignItems: 'center', gap: 10 }}>
          <div style={{ width: 36, height: 36, borderRadius: 18, background: BRAND.ink, color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 700 }}>JO</div>
          <div style={{ flex: 1 }}>
            <div style={{ fontWeight: 600, fontSize: 13 * fontScale }}>Jorge · {lang === 'es' ? 'Repartidor' : 'Courier'}</div>
            <div style={{ fontSize: 11 * fontScale, color: BRAND.inkSoft }}>{lang === 'es' ? 'Llega en ~12 min' : 'Arrives in ~12 min'}</div>
          </div>
          <button style={{ width: 36, height: 36, borderRadius: 18, border: 'none', background: BRAND.green, color: '#fff', fontSize: 16, cursor: 'pointer' }}>📞</button>
        </div>
      </div>

      {/* Timeline */}
      <div style={{ margin: '0 20px 16px', background: '#fff', padding: 16, borderRadius: 16, border: `1px solid ${BRAND.line}` }}>
        {steps.map((s, i) => (
          <div key={s.id} style={{ display: 'flex', gap: 12, position: 'relative', paddingBottom: i === steps.length - 1 ? 0 : 16 }}>
            {i < steps.length - 1 && (
              <div style={{ position: 'absolute', left: 14, top: 30, bottom: 0, width: 2,
                background: s.done ? BRAND.accent : BRAND.line }} />
            )}
            <div style={{
              width: 30, height: 30, borderRadius: 15,
              background: s.done ? BRAND.accent : (s.current ? '#fff' : BRAND.line),
              border: s.current ? `2px solid ${BRAND.accent}` : 'none',
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              color: s.done ? '#fff' : BRAND.inkSoft, fontSize: 13, flexShrink: 0, zIndex: 1,
            }}>{s.done ? '✓' : (s.current ? '●' : '')}</div>
            <div style={{ flex: 1 }}>
              <div style={{ fontWeight: 600, fontSize: 14 * fontScale, color: s.done || s.current ? BRAND.ink : BRAND.inkMuted }}>{s.label}</div>
              <div style={{ fontSize: 12 * fontScale, color: BRAND.inkSoft, marginTop: 2 }}>{s.time}</div>
            </div>
          </div>
        ))}
      </div>

      {/* Order summary */}
      <div style={{ margin: '0 20px', background: '#fff', padding: 14, borderRadius: 16, border: `1px solid ${BRAND.line}` }}>
        <div style={{ fontSize: 11 * fontScale, fontWeight: 700, color: BRAND.inkSoft, textTransform: 'uppercase', letterSpacing: 0.4, marginBottom: 10 }}>
          {lang === 'es' ? 'Resumen' : 'Summary'}
        </div>
        {[{ dish: hcpDish('pabellon'), qty: 2 }, { dish: hcpDish('tequenos'), qty: 2 }].map((it, i) => (
          <div key={i} style={{ display: 'flex', gap: 10, alignItems: 'center', padding: '6px 0' }}>
            <DishThumb dish={it.dish} size={40} rounded={10} />
            <div style={{ flex: 1, fontSize: 13 * fontScale, color: BRAND.ink }}>
              <span style={{ fontWeight: 600 }}>{it.qty}×</span> {it.dish.name[lang]}
            </div>
            <div style={{ fontSize: 13 * fontScale, fontWeight: 600 }}>{hcpFmtFull(it.dish.price * it.qty)}</div>
          </div>
        ))}
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────
// Screen 5 — Reviews (browse + write)
// ─────────────────────────────────────────────────────────────
function ClientReviews({ lang, fontScale }) {
  const t = HCP_I18N[lang];
  const [rating, setRating] = React.useState(5);
  return (
    <div style={{ background: BRAND.bg, minHeight: '100%', paddingBottom: 100, fontSize: 14 * fontScale }}>
      <div style={{ padding: '8px 20px 12px' }}>
        <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 26 * fontScale, color: BRAND.ink }}>
          {t.reviews}
        </div>
      </div>

      {/* Write review — pending dish */}
      <div style={{ margin: '0 20px 18px', background: '#fff', padding: 16, borderRadius: 16, border: `1.5px solid ${BRAND.accent}` }}>
        <div style={{ fontSize: 11 * fontScale, fontWeight: 700, color: BRAND.accent, textTransform: 'uppercase', letterSpacing: 0.5 }}>
          {t.writeReview}
        </div>
        <div style={{ display: 'flex', gap: 10, alignItems: 'center', marginTop: 10 }}>
          <DishThumb dish={hcpDish('pabellon')} size={48} rounded={12} />
          <div style={{ flex: 1 }}>
            <div style={{ fontWeight: 600, fontSize: 14 * fontScale, color: BRAND.ink }}>{hcpDish('pabellon').name[lang]}</div>
            <div style={{ fontSize: 12 * fontScale, color: BRAND.inkSoft }}>{lang === 'es' ? 'Entregado ayer' : 'Delivered yesterday'}</div>
          </div>
        </div>
        <div style={{ marginTop: 12, display: 'flex', gap: 6, justifyContent: 'center' }}>
          {[1,2,3,4,5].map(i => (
            <button key={i} onClick={() => setRating(i)} style={{
              width: 44, height: 44, border: 'none', background: 'transparent', cursor: 'pointer',
              fontSize: 30, color: i <= rating ? '#8B7340' : '#C8D0C9',
            }}>★</button>
          ))}
        </div>
        <div style={{
          marginTop: 8, padding: 12, background: BRAND.bg, borderRadius: 12,
          fontSize: 13 * fontScale, color: BRAND.inkMuted, minHeight: 60,
        }}>
          {lang === 'es' ? '¿Cómo estuvo tu pedido?' : 'How was your order?'}
        </div>
        <button style={{
          marginTop: 10, width: '100%', padding: '12px', borderRadius: 12, border: 'none',
          background: BRAND.ink, color: '#fff', fontWeight: 700, fontSize: 14 * fontScale, cursor: 'pointer',
        }}>{t.submit}</button>
      </div>

      {/* Existing reviews */}
      <div style={{ padding: '0 20px' }}>
        <div style={{ fontSize: 11 * fontScale, fontWeight: 700, color: BRAND.inkSoft, textTransform: 'uppercase', letterSpacing: 0.4, marginBottom: 10 }}>
          {lang === 'es' ? 'Reseñas recientes de la comunidad' : 'Recent community reviews'}
        </div>
        {HCP_REVIEWS.map(r => {
          const d = hcpDish(r.dishId);
          return (
            <div key={r.id} style={{ background: '#fff', padding: 14, borderRadius: 14, marginBottom: 10, border: `1px solid ${BRAND.line}` }}>
              <div style={{ display: 'flex', gap: 10, alignItems: 'center', marginBottom: 8 }}>
                <DishThumb dish={d} size={40} rounded={10} />
                <div style={{ flex: 1 }}>
                  <div style={{ fontWeight: 600, fontSize: 13 * fontScale, color: BRAND.ink }}>{d.name[lang]}</div>
                  <div style={{ fontSize: 11 * fontScale, color: BRAND.inkSoft, marginTop: 2 }}>
                    {r.customer} · {r.time[lang]}
                  </div>
                </div>
                <StarRow rating={r.rating} size={13} />
              </div>
              <div style={{ fontSize: 13 * fontScale, color: BRAND.inkSoft, lineHeight: 1.45, textWrap: 'pretty' }}>{r.text[lang]}</div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────
// Bottom tab bar
// ─────────────────────────────────────────────────────────────
function ClientTabBar({ active, onNav, lang, fontScale }) {
  const t = HCP_I18N[lang];
  const tabs = [
    { id: 'browse',   label: t.browse,   icon: '🍽' },
    { id: 'tracking', label: t.orders,   icon: '📦' },
    { id: 'reviews',  label: t.reviews,  icon: '★' },
    { id: 'profile',  label: t.profile,  icon: '👤' },
  ];
  return (
    <div style={{
      position: 'absolute', bottom: 0, left: 0, right: 0,
      background: 'rgba(255,255,255,0.96)', backdropFilter: 'blur(20px)',
      borderTop: `1px solid ${BRAND.line}`, padding: '10px 8px 28px',
      display: 'flex', justifyContent: 'space-around',
    }}>
      {tabs.map(tab => (
        <button key={tab.id} onClick={() => onNav(tab.id)} style={{
          border: 'none', background: 'transparent', cursor: 'pointer',
          display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 2,
          color: active === tab.id ? BRAND.accent : BRAND.inkMuted,
          padding: '4px 12px',
        }}>
          <div style={{ fontSize: 20 }}>{tab.icon}</div>
          <div style={{ fontSize: 10 * fontScale, fontWeight: 600 }}>{tab.label}</div>
        </button>
      ))}
    </div>
  );
}

// ─────────────────────────────────────────────────────────────
// Full client app shell — handles screen state
// ─────────────────────────────────────────────────────────────
function ClientApp({ lang, fontScale, initial = 'browse' }) {
  const [screen, setScreen] = React.useState(initial);
  const [detailId, setDetailId] = React.useState(initial === 'detail' ? 'pabellon' : null);
  const [cart, setCart] = React.useState([
    { dish: hcpDish('pabellon'), qty: 2 },
    { dish: hcpDish('tequenos'), qty: 2 },
  ]);
  const cartCount = cart.reduce((s, it) => s + it.qty, 0);

  const addToCart = (dish, qty) => {
    setCart(c => {
      const existing = c.find(it => it.dish.id === dish.id);
      if (existing) return c.map(it => it.dish.id === dish.id ? { ...it, qty: it.qty + qty } : it);
      return [...c, { dish, qty }];
    });
    setScreen('cart');
  };

  let content;
  if (screen === 'browse') content = <ClientCatalog lang={lang} onOpen={id => { setDetailId(id); setScreen('detail'); }} onOpenCart={() => setScreen('cart')} cartCount={cartCount} fontScale={fontScale} />;
  else if (screen === 'detail')  content = <ClientDetail dishId={detailId} lang={lang} onBack={() => setScreen('browse')} onAdd={addToCart} fontScale={fontScale} />;
  else if (screen === 'cart')    content = <ClientCart cart={cart} lang={lang} onBack={() => setScreen('browse')} onPlace={() => setScreen('tracking')} fontScale={fontScale} />;
  else if (screen === 'tracking') content = <ClientTracking lang={lang} fontScale={fontScale} />;
  else if (screen === 'reviews') content = <ClientReviews lang={lang} fontScale={fontScale} />;
  else content = <ClientProfile lang={lang} fontScale={fontScale} />;

  const showTabs = screen !== 'detail' && screen !== 'cart';

  return (
    <div style={{ position: 'relative', width: '100%', height: '100%', overflow: 'hidden' }}>
      <div style={{ position: 'absolute', inset: 0, overflowY: 'auto' }}>
        {content}
      </div>
      {showTabs && <ClientTabBar active={screen} onNav={setScreen} lang={lang} fontScale={fontScale} />}
    </div>
  );
}

function ClientProfile({ lang, fontScale }) {
  const t = HCP_I18N[lang];
  const past = [
    { id: '#A038', total: 54000, date: { es: '22 abr', en: 'Apr 22' }, items: 2 },
    { id: '#A031', total: 32000, date: { es: '18 abr', en: 'Apr 18' }, items: 1 },
    { id: '#A025', total: 67500, date: { es: '12 abr', en: 'Apr 12' }, items: 3 },
  ];
  return (
    <div style={{ background: BRAND.bg, minHeight: '100%', paddingBottom: 100, fontSize: 14 * fontScale }}>
      <div style={{ padding: '16px 20px 18px', display: 'flex', alignItems: 'center', gap: 14 }}>
        <div style={{ width: 64, height: 64, borderRadius: 32, background: `linear-gradient(135deg, ${BRAND.accent}, ${BRAND.sun})`, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 26, color: '#fff', fontWeight: 700 }}>M</div>
        <div>
          <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 22 * fontScale, color: BRAND.ink }}>María Fernández</div>
          <div style={{ fontSize: 12 * fontScale, color: BRAND.inkSoft }}>maria.f@correo.co · {lang === 'es' ? 'Cliente desde 2024' : 'Customer since 2024'}</div>
        </div>
      </div>
      <div style={{ margin: '0 20px 16px', display: 'flex', gap: 10 }}>
        {[
          { label: lang === 'es' ? 'Pedidos' : 'Orders', value: '14' },
          { label: lang === 'es' ? 'Favoritos' : 'Favorites', value: '4' },
          { label: lang === 'es' ? 'Puntos' : 'Points', value: '280' },
        ].map(s => (
          <div key={s.label} style={{ flex: 1, background: '#fff', padding: 14, borderRadius: 14, border: `1px solid ${BRAND.line}`, textAlign: 'center' }}>
            <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 24 * fontScale, color: BRAND.ink }}>{s.value}</div>
            <div style={{ fontSize: 11 * fontScale, color: BRAND.inkSoft, marginTop: 2 }}>{s.label}</div>
          </div>
        ))}
      </div>
      <div style={{ padding: '0 20px' }}>
        <div style={{ fontSize: 11 * fontScale, fontWeight: 700, color: BRAND.inkSoft, textTransform: 'uppercase', letterSpacing: 0.4, marginBottom: 10 }}>
          {lang === 'es' ? 'Historial de pedidos' : 'Order history'}
        </div>
        {past.map(o => (
          <div key={o.id} style={{ background: '#fff', padding: 12, borderRadius: 12, marginBottom: 8, border: `1px solid ${BRAND.line}`, display: 'flex', alignItems: 'center' }}>
            <div style={{ flex: 1 }}>
              <div style={{ fontWeight: 600, fontSize: 14 * fontScale, color: BRAND.ink }}>{o.id}</div>
              <div style={{ fontSize: 12 * fontScale, color: BRAND.inkSoft, marginTop: 2 }}>{o.date[lang]} · {o.items} {lang === 'es' ? 'platos' : 'items'}</div>
            </div>
            <div style={{ fontWeight: 700, fontSize: 14 * fontScale }}>{hcpFmtFull(o.total)}</div>
          </div>
        ))}
      </div>
    </div>
  );
}

Object.assign(window, { ClientApp, BRAND });
