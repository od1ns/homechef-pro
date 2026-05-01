// New feature screens: customer onboarding, mobile admin view, loyalty, recipe scaling

// ─────────────────────────────────────────────────────────────
// CUSTOMER ONBOARDING (3 steps)
// ─────────────────────────────────────────────────────────────
function OnboardingStep({ step, lang, fontScale }) {
  const t = HCP_I18N[lang];
  const steps = {
    welcome: {
      emoji: '🍳',
      title: lang === 'es' ? 'Bienvenido a\nHomeChef Pro' : 'Welcome to\nHomeChef Pro',
      desc: lang === 'es' ? 'Comida casera preparada con amor por chefs de tu barrio. Ingredientes frescos, recetas de siempre.' : 'Home cooking made with love by chefs in your neighborhood. Fresh ingredients, time-tested recipes.',
      cta: lang === 'es' ? 'Empezar' : 'Get started',
      swatch: [BRAND.accent, BRAND.green],
    },
    location: {
      emoji: '📍',
      title: lang === 'es' ? '¿Dónde estás?' : 'Where are you?',
      desc: lang === 'es' ? 'Usamos tu ubicación para mostrarte chefs cercanos y calcular el tiempo de entrega.' : 'We use your location to show nearby chefs and calculate delivery time.',
      cta: lang === 'es' ? 'Permitir ubicación' : 'Allow location',
      swatch: [BRAND.sun, BRAND.accent],
    },
    preferences: {
      emoji: '🌿',
      title: lang === 'es' ? 'Tus preferencias' : 'Your preferences',
      desc: lang === 'es' ? 'Personaliza tu experiencia. Puedes cambiarlas cuando quieras.' : 'Personalize your experience. You can change them anytime.',
      cta: lang === 'es' ? 'Continuar' : 'Continue',
      swatch: [BRAND.green, BRAND.sun],
    },
  };
  const s = steps[step];
  const prefs = [
    { id: 'veg', label: lang === 'es' ? 'Vegetariano' : 'Vegetarian', emoji: '🥬' },
    { id: 'spicy', label: lang === 'es' ? 'Picante' : 'Spicy', emoji: '🌶' },
    { id: 'glut', label: lang === 'es' ? 'Sin gluten' : 'Gluten-free', emoji: '🌾' },
    { id: 'meat', label: lang === 'es' ? 'Carnes' : 'Meats', emoji: '🥩' },
    { id: 'seafood', label: lang === 'es' ? 'Mariscos' : 'Seafood', emoji: '🦐' },
    { id: 'sweet', label: lang === 'es' ? 'Dulces' : 'Sweet', emoji: '🍰' },
  ];
  const [selected, setSelected] = React.useState(['spicy', 'meat']);

  return (
    <div style={{ background: BRAND.bg, height: '100%', display: 'flex', flexDirection: 'column', padding: '20px 24px 32px', fontSize: 14 * fontScale }}>
      {/* Progress dots */}
      <div style={{ display: 'flex', gap: 6, justifyContent: 'center', marginTop: 10, marginBottom: 20 }}>
        {['welcome', 'location', 'preferences'].map(id => (
          <div key={id} style={{ width: id === step ? 24 : 8, height: 8, borderRadius: 4, background: id === step ? BRAND.accent : BRAND.line, transition: 'width 0.2s' }} />
        ))}
      </div>

      {/* Hero visual */}
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', textAlign: 'center', padding: '0 8px' }}>
        <div style={{
          width: 180, height: 180, borderRadius: 90,
          background: `linear-gradient(135deg, ${s.swatch[0]}, ${s.swatch[1]})`,
          display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 82,
          boxShadow: `0 20px 60px ${s.swatch[0]}40`,
          marginBottom: 36, position: 'relative', overflow: 'hidden',
        }}>
          <div style={{ position: 'absolute', inset: 0, backgroundImage: 'repeating-linear-gradient(45deg, transparent 0 16px, rgba(255,255,255,0.08) 16px 18px)' }} />
          <span style={{ position: 'relative', filter: 'drop-shadow(0 4px 12px rgba(0,0,0,0.2))' }}>{s.emoji}</span>
        </div>

        <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 34 * fontScale, lineHeight: 1.05, color: BRAND.ink, whiteSpace: 'pre-line', marginBottom: 14 }}>{s.title}</div>
        <div style={{ fontSize: 14 * fontScale, color: BRAND.inkSoft, lineHeight: 1.5, maxWidth: 300, textWrap: 'pretty' }}>{s.desc}</div>

        {step === 'preferences' && (
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, justifyContent: 'center', marginTop: 24, maxWidth: 320 }}>
            {prefs.map(p => {
              const on = selected.includes(p.id);
              return (
                <button key={p.id} onClick={() => setSelected(sel => on ? sel.filter(x => x !== p.id) : [...sel, p.id])}
                  style={{
                    padding: '10px 14px', borderRadius: 999, border: `1.5px solid ${on ? BRAND.accent : BRAND.line}`,
                    background: on ? BRAND.accent : '#fff', color: on ? '#fff' : BRAND.ink,
                    fontSize: 13 * fontScale, fontWeight: 600, cursor: 'pointer',
                    display: 'flex', alignItems: 'center', gap: 6,
                  }}>
                  <span>{p.emoji}</span> {p.label}
                </button>
              );
            })}
          </div>
        )}
      </div>

      <button style={{
        width: '100%', height: 54, borderRadius: 999, border: 'none',
        background: BRAND.accent, color: '#fff', fontWeight: 700, fontSize: 15 * fontScale, cursor: 'pointer',
        boxShadow: `0 6px 18px ${BRAND.accent}50`,
      }}>{s.cta}</button>
      {step !== 'welcome' && (
        <button style={{ marginTop: 12, background: 'transparent', border: 'none', color: BRAND.inkSoft, fontSize: 13 * fontScale, fontWeight: 600, cursor: 'pointer' }}>
          {lang === 'es' ? 'Saltar' : 'Skip'}
        </button>
      )}
    </div>
  );
}

// ─────────────────────────────────────────────────────────────
// LOYALTY / POINTS screen
// ─────────────────────────────────────────────────────────────
function LoyaltyScreen({ lang, fontScale }) {
  const points = 280;
  const nextTier = 500;
  const pct = points / nextTier * 100;
  const rewards = [
    { id: 1, cost: 100, label: lang === 'es' ? 'Postre gratis' : 'Free dessert', emoji: '🍮', available: true },
    { id: 2, cost: 250, label: lang === 'es' ? 'Envío gratis' : 'Free delivery', emoji: '🛵', available: true },
    { id: 3, cost: 500, label: lang === 'es' ? '25% de descuento' : '25% off', emoji: '🎁', available: false },
    { id: 4, cost: 1000, label: lang === 'es' ? 'Cena para 2' : 'Dinner for 2', emoji: '🍽', available: false },
  ];
  return (
    <div style={{ background: BRAND.bg, height: '100%', overflowY: 'auto', paddingBottom: 30, fontSize: 14 * fontScale }}>
      <div style={{ padding: '8px 20px 18px' }}>
        <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 28 * fontScale, color: BRAND.ink }}>
          {lang === 'es' ? 'Programa Sabor' : 'Flavor Program'}
        </div>
      </div>

      {/* Points card */}
      <div style={{ margin: '0 20px 18px', padding: 22, borderRadius: 20,
        background: `linear-gradient(135deg, ${BRAND.ink}, ${BRAND.accent})`,
        color: '#fff', position: 'relative', overflow: 'hidden',
      }}>
        <div style={{ position: 'absolute', right: -40, top: -40, width: 180, height: 180, borderRadius: 90, background: 'rgba(255,255,255,0.06)' }} />
        <div style={{ position: 'absolute', right: 10, top: 40, width: 100, height: 100, borderRadius: 50, background: 'rgba(255,255,255,0.04)' }} />
        <div style={{ fontSize: 11 * fontScale, opacity: 0.7, textTransform: 'uppercase', letterSpacing: 0.5, fontWeight: 600 }}>
          {lang === 'es' ? 'Tus puntos' : 'Your points'}
        </div>
        <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 56 * fontScale, lineHeight: 1, marginTop: 6 }}>{points}</div>
        <div style={{ fontSize: 12 * fontScale, opacity: 0.85, marginTop: 4 }}>
          {lang === 'es' ? `${nextTier - points} puntos para nivel Oro` : `${nextTier - points} points to Gold tier`}
        </div>
        <div style={{ height: 6, background: 'rgba(255,255,255,0.2)', borderRadius: 3, marginTop: 14, overflow: 'hidden' }}>
          <div style={{ width: pct + '%', height: '100%', background: '#fff', borderRadius: 3 }} />
        </div>
        <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 10 * fontScale, marginTop: 6, opacity: 0.7 }}>
          <span>🥉 {lang === 'es' ? 'Bronce' : 'Bronze'}</span>
          <span>🥈 {lang === 'es' ? 'Plata' : 'Silver'}</span>
          <span>🥇 {lang === 'es' ? 'Oro' : 'Gold'}</span>
        </div>
      </div>

      <div style={{ padding: '0 20px' }}>
        <div style={{ fontSize: 11 * fontScale, fontWeight: 700, color: BRAND.inkSoft, textTransform: 'uppercase', letterSpacing: 0.4, marginBottom: 10 }}>
          {lang === 'es' ? 'Recompensas disponibles' : 'Available rewards'}
        </div>
        {rewards.map(r => (
          <div key={r.id} style={{ background: '#fff', padding: 14, borderRadius: 14, marginBottom: 10, border: `1px solid ${BRAND.line}`,
            display: 'flex', alignItems: 'center', gap: 12, opacity: r.available ? 1 : 0.5 }}>
            <div style={{ width: 48, height: 48, borderRadius: 12, background: r.available ? BRAND.greenSoft : BRAND.line,
              display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 24 }}>{r.emoji}</div>
            <div style={{ flex: 1 }}>
              <div style={{ fontWeight: 600, fontSize: 14 * fontScale, color: BRAND.ink }}>{r.label}</div>
              <div style={{ fontSize: 12 * fontScale, color: BRAND.inkSoft, marginTop: 2, fontFamily: 'JetBrains Mono, monospace' }}>
                {r.cost} {lang === 'es' ? 'puntos' : 'points'}
              </div>
            </div>
            {r.available ? (
              <button style={{ padding: '8px 14px', background: BRAND.accent, color: '#fff', border: 'none', borderRadius: 8, fontWeight: 600, fontSize: 12 * fontScale, cursor: 'pointer' }}>
                {lang === 'es' ? 'Canjear' : 'Redeem'}
              </button>
            ) : (
              <div style={{ fontSize: 10 * fontScale, color: BRAND.inkMuted, fontWeight: 600 }}>🔒</div>
            )}
          </div>
        ))}
      </div>

      <div style={{ margin: '16px 20px 0', padding: 14, background: BRAND.greenSoft, borderRadius: 12, fontSize: 12 * fontScale, color: BRAND.green, lineHeight: 1.4 }}>
        <div style={{ fontWeight: 700, marginBottom: 4 }}>💡 {lang === 'es' ? '¿Cómo gano puntos?' : 'How do I earn points?'}</div>
        {lang === 'es' ? 'Ganas 1 punto por cada $1.000 gastados. Las reseñas con foto te dan 20 puntos extra.' : 'Earn 1 point per $1,000 spent. Reviews with photos give you 20 bonus points.'}
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────
// MOBILE ADMIN — chef on the go
// ─────────────────────────────────────────────────────────────
function MobileAdminHome({ lang, fontScale }) {
  const t = HCP_I18N[lang];
  const incoming = HCP_ORDERS.filter(o => o.status === 'incoming');
  const cooking = HCP_ORDERS.filter(o => o.status === 'cooking');

  return (
    <div style={{ background: ADM.bg, height: '100%', overflowY: 'auto', paddingBottom: 30, fontSize: 13 * fontScale }}>
      {/* Header */}
      <div style={{ padding: '12px 18px 14px', background: ADM.sidebar, color: '#fff' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <div>
            <div style={{ fontSize: 11 * fontScale, opacity: 0.7 }}>{lang === 'es' ? 'Viernes · 24 abr' : 'Friday · Apr 24'}</div>
            <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 22 * fontScale, marginTop: 2 }}>
              {lang === 'es' ? 'Hola, Rocío 👋' : 'Hi, Rocío 👋'}
            </div>
          </div>
          <div style={{ width: 40, height: 40, borderRadius: 20, background: ADM.accent, display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 700 }}>R</div>
        </div>
      </div>

      {/* Today KPI strip */}
      <div style={{ padding: 16, display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10 }}>
        <div style={{ background: '#fff', padding: 14, borderRadius: 12, border: `1px solid ${ADM.line}` }}>
          <div style={{ fontSize: 10 * fontScale, color: ADM.inkSoft, textTransform: 'uppercase', letterSpacing: 0.4, fontWeight: 600 }}>
            {lang === 'es' ? 'Ingresos hoy' : 'Today revenue'}
          </div>
          <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 22 * fontScale, color: ADM.ink, marginTop: 4 }}>
            {hcpFmtFull(1128000)}
          </div>
          <div style={{ fontSize: 10 * fontScale, color: ADM.green, marginTop: 2, fontWeight: 600 }}>+18% vs ayer</div>
        </div>
        <div style={{ background: '#fff', padding: 14, borderRadius: 12, border: `1px solid ${ADM.line}` }}>
          <div style={{ fontSize: 10 * fontScale, color: ADM.inkSoft, textTransform: 'uppercase', letterSpacing: 0.4, fontWeight: 600 }}>
            {lang === 'es' ? 'Pedidos activos' : 'Active orders'}
          </div>
          <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 22 * fontScale, color: ADM.ink, marginTop: 4 }}>{incoming.length + cooking.length}</div>
          <div style={{ fontSize: 10 * fontScale, color: ADM.accent, marginTop: 2, fontWeight: 600 }}>{incoming.length} {lang === 'es' ? 'nuevas' : 'new'}</div>
        </div>
      </div>

      {/* Alert banner */}
      <div style={{ margin: '0 16px 14px', padding: 12, background: ADM.sunSoft, borderRadius: 12, display: 'flex', alignItems: 'center', gap: 10, borderLeft: `3px solid ${ADM.sun}` }}>
        <div style={{ fontSize: 20 }}>⚠️</div>
        <div style={{ flex: 1, fontSize: 12 * fontScale, color: ADM.ink, lineHeight: 1.3 }}>
          <strong>{lang === 'es' ? 'Onoto agotado' : 'Annatto out'}</strong> · {lang === 'es' ? 'afecta 12 pedidos de hallaca' : 'affects 12 hallaca orders'}
        </div>
        <button style={{ padding: '6px 10px', background: ADM.ink, color: '#fff', border: 'none', borderRadius: 7, fontWeight: 600, fontSize: 11 * fontScale, cursor: 'pointer' }}>
          {lang === 'es' ? 'Ordenar' : 'Order'}
        </button>
      </div>

      {/* Incoming orders */}
      <div style={{ padding: '0 16px' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', marginBottom: 10 }}>
          <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 18 * fontScale, color: ADM.ink }}>
            {lang === 'es' ? 'Pedidos entrantes' : 'Incoming orders'}
          </div>
          <div style={{ fontSize: 11 * fontScale, color: ADM.accent, fontWeight: 600 }}>
            {lang === 'es' ? 'Ver todos →' : 'See all →'}
          </div>
        </div>
        {incoming.map(o => (
          <div key={o.id} style={{ background: '#fff', padding: 12, borderRadius: 12, marginBottom: 10, border: `1px solid ${ADM.line}` }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 8 }}>
              <div>
                <div style={{ fontFamily: 'JetBrains Mono, monospace', fontWeight: 700, fontSize: 12 * fontScale, color: ADM.ink }}>{o.id}</div>
                <div style={{ fontSize: 13 * fontScale, color: ADM.ink, fontWeight: 600, marginTop: 2 }}>{o.customer}</div>
                <div style={{ fontSize: 11 * fontScale, color: ADM.inkSoft, marginTop: 2 }}>
                  {o.type === 'delivery' ? '🛵' : '🏪'} · {o.time}
                </div>
              </div>
              <div style={{ fontWeight: 700, fontSize: 14 * fontScale }}>{hcpFmtFull(o.total)}</div>
            </div>
            <div style={{ padding: '8px 0', borderTop: `1px solid ${ADM.line}`, borderBottom: `1px solid ${ADM.line}`, marginBottom: 10 }}>
              {o.items.map((it, i) => {
                const d = hcpDish(it.dish);
                return <div key={i} style={{ fontSize: 12 * fontScale, color: ADM.ink, padding: '2px 0' }}>{it.qty}× {d.name[lang]}</div>;
              })}
            </div>
            <div style={{ display: 'flex', gap: 8 }}>
              <button style={{ flex: 1, padding: 10, background: '#F3E0DD', color: ADM.red, border: 'none', borderRadius: 8, fontWeight: 600, fontSize: 12 * fontScale, cursor: 'pointer' }}>
                ✕ {lang === 'es' ? 'Rechazar' : 'Reject'}
              </button>
              <button style={{ flex: 2, padding: 10, background: ADM.accent, color: '#fff', border: 'none', borderRadius: 8, fontWeight: 700, fontSize: 12 * fontScale, cursor: 'pointer' }}>
                ✓ {lang === 'es' ? 'Aceptar y empezar' : 'Accept & start'}
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────
// RECIPE SCALING
// ─────────────────────────────────────────────────────────────
function RecipeScaling({ lang, fontScale }) {
  const t = HCP_I18N[lang];
  const dish = hcpDish('pabellon');
  const [portions, setPortions] = React.useState(24);
  const base = 1;
  const factor = portions / base;

  // Weekly forecast
  const forecast = [
    { day: { es: 'Lun', en: 'Mon' }, predicted: 16, actual: null },
    { day: { es: 'Mar', en: 'Tue' }, predicted: 18, actual: null },
    { day: { es: 'Mié', en: 'Wed' }, predicted: 14, actual: null },
    { day: { es: 'Jue', en: 'Thu' }, predicted: 22, actual: null },
    { day: { es: 'Vie', en: 'Fri' }, predicted: 28, actual: null },
    { day: { es: 'Sáb', en: 'Sat' }, predicted: 34, actual: null },
    { day: { es: 'Dom', en: 'Sun' }, predicted: 24, actual: null },
  ];
  const weekTotal = forecast.reduce((s, d) => s + d.predicted, 0);

  return (
    <div style={{ padding: 28, fontSize: 14 * fontScale }}>
      <div style={{ marginBottom: 22 }}>
        <div style={{ fontSize: 12 * fontScale, color: ADM.inkSoft, fontWeight: 500 }}>{t.menu} · {lang === 'es' ? 'Escalado' : 'Scaling'}</div>
        <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 30 * fontScale, color: ADM.ink, marginTop: 4 }}>
          {lang === 'es' ? 'Escalar receta según demanda' : 'Scale recipe to demand'}
        </div>
        <div style={{ fontSize: 13 * fontScale, color: ADM.inkSoft, marginTop: 4 }}>
          {lang === 'es' ? 'Multiplicamos ingredientes automáticamente y revisamos si tienes stock suficiente.' : 'We auto-multiply ingredients and check if you have enough stock.'}
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 18 }}>
        {/* Left: Forecast + scaling */}
        <div>
          <div style={{ background: ADM.surface, padding: 18, borderRadius: 14, border: `1px solid ${ADM.line}`, marginBottom: 14 }}>
            <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 18 * fontScale, color: ADM.ink, marginBottom: 4 }}>
              {lang === 'es' ? 'Pronóstico de demanda' : 'Demand forecast'}
            </div>
            <div style={{ fontSize: 11 * fontScale, color: ADM.inkSoft, marginBottom: 14 }}>
              {dish.name[lang]} · {lang === 'es' ? `${weekTotal} porciones esta semana` : `${weekTotal} portions this week`}
            </div>
            <div style={{ display: 'flex', gap: 6, alignItems: 'flex-end', height: 120 }}>
              {forecast.map((f, i) => {
                const max = Math.max(...forecast.map(x => x.predicted));
                const h = f.predicted / max * 90;
                return (
                  <div key={i} style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 4 }}>
                    <div style={{ fontSize: 10 * fontScale, color: ADM.ink, fontFamily: 'JetBrains Mono, monospace' }}>{f.predicted}</div>
                    <div style={{ width: '100%', height: h, background: ADM.accent, borderRadius: '4px 4px 0 0', opacity: i === 4 ? 1 : 0.5 }} />
                    <div style={{ fontSize: 10 * fontScale, color: ADM.inkSoft }}>{f.day[lang]}</div>
                  </div>
                );
              })}
            </div>
            <div style={{ marginTop: 12, padding: 10, background: BRAND.greenSoft, borderRadius: 8, fontSize: 11 * fontScale, color: ADM.green, lineHeight: 1.35 }}>
              🤖 {lang === 'es' ? 'IA sugiere preparar viernes al mediodía para cubrir el pico del fin de semana.' : 'AI suggests prepping Friday midday to cover the weekend peak.'}
            </div>
          </div>

          <div style={{ background: ADM.ink, padding: 22, borderRadius: 14, color: '#fff' }}>
            <div style={{ fontSize: 11 * fontScale, opacity: 0.7, textTransform: 'uppercase', letterSpacing: 0.5, fontWeight: 600, marginBottom: 8 }}>
              {lang === 'es' ? 'Escalar a' : 'Scale to'}
            </div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 14, marginBottom: 18 }}>
              <button onClick={() => setPortions(Math.max(1, portions - 4))} style={{ width: 44, height: 44, borderRadius: 22, border: '1px solid rgba(255,255,255,0.2)', background: 'transparent', color: '#fff', fontSize: 20, cursor: 'pointer' }}>−</button>
              <div style={{ flex: 1, textAlign: 'center' }}>
                <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 48 * fontScale, lineHeight: 1, color: ADM.accent }}>{portions}</div>
                <div style={{ fontSize: 11 * fontScale, opacity: 0.7, marginTop: 4 }}>{lang === 'es' ? 'porciones' : 'portions'}</div>
              </div>
              <button onClick={() => setPortions(portions + 4)} style={{ width: 44, height: 44, borderRadius: 22, border: '1px solid rgba(255,255,255,0.2)', background: 'transparent', color: '#fff', fontSize: 20, cursor: 'pointer' }}>+</button>
            </div>
            <input type="range" min="1" max="60" value={portions} onChange={e => setPortions(+e.target.value)} style={{ width: '100%', accentColor: ADM.accent }} />
            <div style={{ marginTop: 16, padding: 12, background: 'rgba(255,255,255,0.08)', borderRadius: 10, display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10 }}>
              <div>
                <div style={{ fontSize: 10 * fontScale, opacity: 0.7 }}>{lang === 'es' ? 'Costo total' : 'Total cost'}</div>
                <div style={{ fontFamily: 'JetBrains Mono, monospace', fontSize: 16 * fontScale, marginTop: 2, fontWeight: 600 }}>{hcpFmtFull(dish.cost * factor)}</div>
              </div>
              <div>
                <div style={{ fontSize: 10 * fontScale, opacity: 0.7 }}>{lang === 'es' ? 'Tiempo prep.' : 'Prep time'}</div>
                <div style={{ fontFamily: 'JetBrains Mono, monospace', fontSize: 16 * fontScale, marginTop: 2, fontWeight: 600 }}>~{Math.round(dish.prepTime + factor * 0.8)}m</div>
              </div>
            </div>
          </div>
        </div>

        {/* Right: scaled ingredients */}
        <div style={{ background: ADM.surface, padding: 18, borderRadius: 14, border: `1px solid ${ADM.line}` }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', marginBottom: 12 }}>
            <div style={{ fontFamily: '"Instrument Serif", Georgia, serif', fontSize: 18 * fontScale, color: ADM.ink }}>
              {t.ingredients} × {portions}
            </div>
            <div style={{ fontSize: 11 * fontScale, color: ADM.inkSoft }}>{dish.name[lang]}</div>
          </div>
          {dish.ingredients.map((ing, i) => {
            // Scale qty naively (parse number)
            const m = ing.qty.match(/^(\d+(?:\.\d+)?)(.*)$/);
            const scaled = m ? `${Math.round(parseFloat(m[1]) * factor)}${m[2]}` : ing.qty;
            // Check stock (fake)
            const invItem = HCP_INVENTORY.find(iv => iv.name[lang] === ing.name[lang]);
            const enough = !invItem || invItem.status === 'ok';
            return (
              <div key={i} style={{ display: 'flex', alignItems: 'center', padding: '10px 0', borderBottom: i < dish.ingredients.length - 1 ? `1px solid ${ADM.line}` : 'none' }}>
                <div style={{ flex: 1, fontSize: 13 * fontScale, color: ADM.ink }}>{ing.name[lang]}</div>
                <div style={{ fontSize: 11 * fontScale, color: ADM.inkMuted, fontFamily: 'JetBrains Mono, monospace', width: 60, textAlign: 'right', textDecoration: 'line-through' }}>{ing.qty}</div>
                <div style={{ fontSize: 13 * fontScale, color: ADM.accent, fontFamily: 'JetBrains Mono, monospace', width: 70, textAlign: 'right', fontWeight: 700 }}>{scaled}</div>
                <div style={{ width: 24, textAlign: 'center', marginLeft: 8 }}>
                  {enough ? <span style={{ color: ADM.green, fontSize: 14 }}>✓</span> : <span style={{ color: ADM.red, fontSize: 14 }}>!</span>}
                </div>
              </div>
            );
          })}
          <div style={{ marginTop: 14, padding: 12, background: '#F3E0DD', borderRadius: 10, fontSize: 12 * fontScale, color: ADM.red, display: 'flex', alignItems: 'center', gap: 10 }}>
            <span style={{ fontSize: 16 }}>!</span>
            <div style={{ flex: 1, lineHeight: 1.35 }}>
              <strong>{lang === 'es' ? 'Stock insuficiente' : 'Insufficient stock'}</strong> · {lang === 'es' ? 'falta guascas y papa sabanera' : 'missing guascas and sabanera potato'}
            </div>
            <button style={{ padding: '6px 12px', background: ADM.red, color: '#fff', border: 'none', borderRadius: 6, fontWeight: 600, fontSize: 11 * fontScale, cursor: 'pointer' }}>
              {lang === 'es' ? 'Comprar' : 'Buy'}
            </button>
          </div>
          <button style={{ marginTop: 10, width: '100%', padding: 12, background: ADM.accent, color: '#fff', border: 'none', borderRadius: 10, fontWeight: 700, fontSize: 13 * fontScale, cursor: 'pointer' }}>
            {lang === 'es' ? 'Imprimir lista de prep' : 'Print prep list'}
          </button>
        </div>
      </div>
    </div>
  );
}

Object.assign(window, { OnboardingStep, LoyaltyScreen, MobileAdminHome, RecipeScaling });
