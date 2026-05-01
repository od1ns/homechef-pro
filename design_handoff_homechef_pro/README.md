# Handoff: HomeChef Pro

> Design reference package for developers implementing the HomeChef Pro product in a production codebase.

---

## 1. Overview

**HomeChef Pro** is an end-to-end platform for independent home chefs in Colombia/Latin America to run a professional micro-restaurant from their own kitchen. It consists of two surfaces:

1. **Customer mobile app** (iOS-first) — diners browse the chef's daily menu, order, track, and review.
2. **Chef admin** (responsive web + mobile web) — the chef manages live orders, recipes with full cost structure, inventory, scheduled purchasing, profit analytics, and DIAN-compliant invoicing.

The platform is bilingual (Spanish primary, English secondary) and themeable across four complete palettes.

---

## 2. About the Design Files

**The files bundled in `/design_files/` are design references created in HTML** — interactive prototypes built with React + Babel standalone + a custom design-canvas shell. They illustrate the intended look, layout, content, and micro-interactions. **They are not production code to copy directly.**

**Your job is to recreate these designs in the target codebase's environment**, using its established patterns:

- If the target codebase already exists (React Native for the mobile app, Next.js / React / Vue for the web admin), **use its conventions, component libraries, styling solution, and state management**.
- If no environment exists yet, propose the most appropriate stack (recommended: **React Native + Expo** for the customer app, **Next.js 14 App Router + Tailwind + shadcn/ui** for the admin, **NestJS/Postgres** or **Supabase** for the backend) and build there.
- Do **not** ship the bundled HTML as production.

---

## 3. Fidelity

**High-fidelity (hifi)** — colors, typography, spacing, copy, states, and layouts in the reference files are intentional and should be matched closely. Hex values, px spacing, and radii are listed in §9 Design Tokens.

Photography uses Unsplash placeholders in the prototype. **Swap for real chef/dish photography in production.**

---

## 4. Screens / Views

The product is organized into five surface groups. Each artboard label in the prototype corresponds to one view.

### 4.1 Customer App · iOS (6 screens)

| # | Name | Purpose |
|---|---|---|
| C1 | **Catalog** (`browse`) | Today's menu — hero dish, popular list, new-this-week. Filter chips for pickup vs delivery. |
| C2 | **Dish detail** (`detail`) | Photo, price, prep time, rating + reviews count, ingredients, chef note, allergens, add-to-cart with qty + notes. |
| C3 | **Cart & checkout** (`cart`) | Line items, subtotal/delivery/IVA/total, notes-to-chef, pickup-or-delivery toggle, payment method, Confirm order CTA. |
| C4 | **Order tracking** (`tracking`) | 5-step progress (received → cooking → ready → on the way → delivered), ETA, chef contact, live map placeholder. |
| C5 | **Reviews** (`reviews`) | Past-order list with "Write review" CTA, star rating, photo upload, text field, submit. |
| C6 | **Profile & history** (`profile`) | Name, saved addresses, order history, Sabor loyalty balance, settings. |

**Layout baseline**: iOS 402×874 px (iPhone 16), safe-area-respecting status bar, home indicator, bottom tab bar (4 tabs: Descubrir · Pedidos · Reseñas · Perfil).

### 4.2 Customer Onboarding · iOS (4 screens)

| # | Name | Purpose |
|---|---|---|
| O1 | **Welcome** | Brand moment, 1-line value prop, "Empezar" CTA. |
| O2 | **Location** | Request location permission + address confirmation. |
| O3 | **Preferences** | Dietary preferences, allergen chips, favorite categories. |
| O4 | **Sabor loyalty** | Introduces the loyalty program (points = "Sabor"), tiers, what you earn. |

### 4.3 Mobile Admin · Chef on the go (1 screen)

| # | Name | Purpose |
|---|---|---|
| M1 | **Day start** | First-glance: today's prep list, active orders count, revenue so far, shortcut to mark dish sold-out. Optimized for chef using phone one-handed while cooking. |

### 4.4 Recipe Scaling · Web (1 screen)

| # | Name | Purpose |
|---|---|---|
| S1 | **Scale to demand** | AI-powered demand forecast for a specific dish (e.g. Pabellón Criollo) → computes total required ingredient quantities + shopping list for tomorrow's service. |

### 4.5 Admin Dashboard · Web (7 screens)

| # | Name | Purpose |
|---|---|---|
| A1 | **Overview** | KPI strip (today's revenue, orders, avg prep time, margin), live order feed, top dishes, inventory alerts. |
| A2 | **Live orders (kitchen)** | Kanban by status (Entrantes / En cocina / Listas / Completadas). Tap to accept, mark-ready, verify payment. Optimized for tablet in kitchen. |
| A3 | **Recipe editor + costs** | Full recipe: ingredients with qty & unit cost → food cost → labor → overhead → **suggested price** based on target margin, vs **current price**. |
| A4 | **Inventory** | Per-ingredient: in-stock qty, reorder point, supplier, unit cost. Badges for low-stock / out-of-stock. |
| A5 | **Scheduled purchases** | Calendar view of upcoming purchase orders, supplier groupings. |
| A6 | **Profit analytics** | Revenue, COGS, gross margin trends. Per-dish profitability. Day-of-week patterns. |
| A7 | **Invoicing** | DIAN-compliant electronic invoice list, status (emitted / accepted / rejected), download PDF/XML. |

**Layout baseline**: 1280×820 px viewport. Fixed left sidebar (240 px) with logo, nav, chef profile. Main content area with its own top bar (breadcrumb + quick actions) and scrollable body.

---

## 5. Interactions & Behavior

### Customer app
- **Add to cart**: qty stepper (-/+), optional notes, slides up bottom sheet confirmation.
- **Checkout**: radio choice pickup/delivery; delivery fee appears conditionally; IVA (19%) calculated on subtotal.
- **Tracking**: polls order status every 30s; timestamp on each step when reached; chef contact opens native phone/WhatsApp.
- **Reviews**: 5-star tap-to-rate, photo picker, submit disabled until rating + text min 10 chars.

### Admin
- **Live orders**: drag-and-drop or tap-action to move orders across columns. Audible chime on new incoming order. Payment-verified toggle required before marking ready.
- **Recipe editor**: changing any ingredient qty or unit cost live-recomputes food cost → suggested price. Warning banner if current price < suggested.
- **Inventory**: clicking low-stock row opens "Schedule purchase" drawer pre-filled with supplier + reorder qty.

### Transitions & animation
- Modal / bottom-sheet: 250 ms ease-out, spring-ish (`cubic-bezier(0.2, 0.8, 0.2, 1)`).
- Tab switches: 150 ms crossfade.
- KPI numbers: count-up animation on mount, 600 ms.
- Loading skeletons (not spinners) for lists.

### Responsive
- Customer app is **mobile only** (iOS + Android).
- Admin web: **≥1280 px** = full dashboard; **768–1279 px** = collapsed sidebar (icon-only); **<768 px** = single column, bottom tab bar (used for chef on the phone — see Mobile Admin screen M1).

---

## 6. State Management

Source of truth for the prototype is `design_files/data.jsx`. Entities:

- **Dish**: id, name (es/en), description, category, price, cost, rating, reviews, prepTime, tag (`popular` | `new` | null), swatch (2 colors), emoji (placeholder), photo URL, ingredients[], allergens[].
- **Order**: id, customerId, dishes[], status (`received` | `cooking` | `ready` | `on_the_way` | `delivered`), timestamps, paymentVerified, pickup/delivery, address, notes, totals.
- **Ingredient / Inventory item**: id, name, unit, inStockQty, reorderPoint, unitCost, supplierId.
- **Review**: id, orderId, customerId, rating, text, photos[], createdAt.
- **Customer**: id, name, addresses[], loyaltyPoints, preferences.

### Required state flows
- Auth (customer login, chef login — separate). OTP by SMS preferred in LatAm.
- Real-time order updates (admin ↔ customer). Socket or server-sent events.
- Push notifications: new order (chef), status changes (customer).
- Offline buffer: admin should keep working if internet drops; sync when back.

---

## 7. Bilingual (i18n)

Every user-facing string has `es` and `en` variants. **Spanish is primary**. The full dictionary lives in `design_files/data.jsx` under `HCP_I18N` — use it as the seed for your i18n framework (react-i18next, next-intl, LinguiJS).

Currency: **COP (Colombian Peso)** with thousands separator as `.` and no decimals (e.g. `$32.000`).
Phone format: Colombian mobile (`+57 3XX XXX XXXX`).
Tax: **IVA 19%**.
Invoicing: **DIAN electronic invoice** (Colombia's tax authority — there are third-party APIs like Siigo, Alegra, or Facturante).

---

## 8. Theming

The product ships with **4 complete color themes**, selectable by the chef. Each theme is a full semantic token set. The user can switch at runtime (admin settings → appearance).

Tokens per theme: `bg`, `card`, `ink`, `inkSoft`, `inkMuted`, `line`, `accent`, `accentDark`, `green`, `greenSoft`, `sun`, `red`, `redSoft`, `sidebar`, `sidebarText`, `sidebarMuted`.

### Theme values

**Plum (default — Venezuelan twilight)**
| Token | Value |
|---|---|
| bg | `#F4F1EC` |
| card | `#FFFFFF` |
| ink | `#2A1F3D` |
| inkSoft | `#6B5F7A` |
| inkMuted | `#A89EB4` |
| line | `#E5DFE8` |
| accent | `#7B4FB8` |
| accentDark | `#5E3A93` |
| green | `#3D6B5C` |
| greenSoft | `#DCE8E2` |
| sun | `#C8A8D4` |
| red | `#B5463E` |
| redSoft | `#F3E0DD` |
| sidebar | `#2A1F3D` |
| sidebarText | `#E0D8EC` |
| sidebarMuted | `#8A7DA0` |

**Paprika (warm, Mediterranean)**
| Token | Value |
|---|---|
| bg | `#F6EFE4` |
| card | `#FFFFFF` |
| ink | `#2D1B12` |
| inkSoft | `#6E544A` |
| inkMuted | `#B29B8F` |
| line | `#EDE1D3` |
| accent | `#C14D2A` |
| accentDark | `#8F3418` |
| green | `#5C6B3D` |
| greenSoft | `#E2E8DC` |
| sun | `#E8B66A` |
| red | `#A13B2E` |
| redSoft | `#F3DDD8` |
| sidebar | `#2D1B12` |
| sidebarText | `#F0E2D4` |
| sidebarMuted | `#9A8478` |

**Caribbean (tropical)**
| Token | Value |
|---|---|
| bg | `#EEF4F3` |
| card | `#FFFFFF` |
| ink | `#0F2E2B` |
| inkSoft | `#4E6E6A` |
| inkMuted | `#9FB4B1` |
| line | `#D9E4E2` |
| accent | `#E26D5C` |
| accentDark | `#B0493A` |
| green | `#1E7A6B` |
| greenSoft | `#D4E7E3` |
| sun | `#F4C06B` |
| red | `#C84B3E` |
| redSoft | `#F5DDD8` |
| sidebar | `#0F2E2B` |
| sidebarText | `#D4E7E3` |
| sidebarMuted | `#7A9A95` |

**Noche (dark mode)**
| Token | Value |
|---|---|
| bg | `#18161E` |
| card | `#22202A` |
| ink | `#F0ECE4` |
| inkSoft | `#B5AFA4` |
| inkMuted | `#6E6A63` |
| line | `#2E2B37` |
| accent | `#D4A74E` |
| accentDark | `#B0862F` |
| green | `#6CA088` |
| greenSoft | `#2D3833` |
| sun | `#E8C97A` |
| red | `#D46C5E` |
| redSoft | `#3A2622` |
| sidebar | `#0F0E14` |
| sidebarText | `#E8E2D5` |
| sidebarMuted | `#787263` |

Expose these as CSS custom properties (web) or theme objects (RN) and drive the whole UI off the semantic names, never raw hex.

---

## 9. Design Tokens

### Typography
- **Display / Headlines**: `"Instrument Serif", serif` — italic variant used for brand moments and hero titles. Sizes: 32 / 40 / 56 / 72.
- **UI / Body**: `"Inter", system-ui, sans-serif` — weights 400 / 500 / 600 / 700. Sizes: 12 / 13 / 14 / 15 / 16 / 18 / 20 / 24.
- **Numerals / Data**: `"JetBrains Mono", monospace` — weights 400 / 500 / 600. Used for prices, metrics, KPIs, codes.

Line height: 1.4 for body, 1.2 for display. Letter-spacing: -0.01em on display, 0 on body, 0 on mono.

### Spacing scale (px)
`4 · 8 · 12 · 16 · 20 · 24 · 32 · 40 · 48 · 64 · 80`

Component padding baseline:
- Mobile screens: 16 px horizontal safe-area + 20 px vertical between sections.
- Desktop cards: 24 px padding.
- Desktop sidebar: 240 px wide, 20 px horizontal padding.

### Radii (px)
- Small controls (chips, badges): `8`
- Inputs, buttons: `12`
- Cards: `16`
- Modals, hero tiles: `24`
- Pill / fully-rounded: `999`

### Shadows
- Card resting: `0 1px 2px rgb(0 0 0 / 0.04), 0 2px 8px rgb(0 0 0 / 0.04)`
- Card hover: `0 2px 4px rgb(0 0 0 / 0.06), 0 12px 28px rgb(0 0 0 / 0.08)`
- Modal: `0 24px 64px rgb(0 0 0 / 0.18)`
- Focus ring: `0 0 0 3px color-mix(in oklch, var(--accent) 35%, transparent)`

### Accessibility
- Font scale slider: 85%–130% (implemented in prototype as `fontScale` prop). **Must be shipped** — target audience includes older diners.
- Minimum tap target: 44×44 px.
- Minimum body font: 14 px (clients often bump to 16 px via the scale control).
- Color contrast: accent on card must be ≥ 4.5:1. Verified for all 4 themes.

---

## 10. Brand Voice & Copy

- Warm, personal, first-name ("Luisa te saluda"), never corporate.
- Food emojis allowed in the prototype as photo placeholders — **remove in production**, use real photography.
- Spanish uses vos/tú inconsistently in LatAm; prototype uses **tú** (more neutral across Colombia/Mexico/Peru).
- Calls to action are verbs in infinitive: "Agregar", "Pagar", "Confirmar pedido" — not "Click here".

---

## 11. Assets

The prototype uses:
- **Unsplash photo URLs** as dish photo placeholders (see `data.jsx` — each dish has a `photo` field). **Replace with real photography** before ship.
- **Emoji** as a visual fallback behind a gradient (the two-color `swatch` per dish). Keep the swatch idea — it's the dish's brand color — but swap emoji for photo.
- **Google Fonts**: Instrument Serif, Inter, JetBrains Mono. Self-host in production.
- **No custom icon set** in prototype. Use **Lucide** or **Heroicons (outline)** in implementation — the prototype uses inline SVG equivalents of Lucide glyphs.

---

## 12. Bundled Files

Inside `design_files/`:

| File | What it is |
|---|---|
| `HomeChef Pro.html` | Entry point — mounts the design canvas with all artboards. |
| `HomeChef Pro Pitch Deck.html` | Companion pitch deck (context for why the product exists). |
| `data.jsx` | **Read first.** All sample data, themes, i18n, domain types. |
| `client-app.jsx` | Customer iOS app — all 6 screens + onboarding. |
| `admin-app.jsx` | Admin web dashboard — all 7 screens + recipe scaling. |
| `new-features.jsx` | Supporting screens (mobile admin home, loyalty, etc.). |
| `design-canvas.jsx`, `ios-frame.jsx`, `browser-window.jsx`, `tweaks-panel.jsx`, `deck-stage.js` | Prototype chrome — **do not port**. They exist only to frame artboards in the canvas. |

Inside `screenshots/` — one PNG per artboard, named by section and order:

**Customer app** · 01–06
**Onboarding + mobile admin** · 07–11
**Admin web** · 12–19 (recipe scaling, then full dashboard)

To view the prototype interactively: open `design_files/HomeChef Pro.html` in a modern browser (no build step; uses Babel standalone).

---

## 13. Out of Scope for This Handoff

- Backend schema/API shape — infer from the entities in §6 and `data.jsx`, or propose your own.
- Actual DIAN integration — pick a provider (Siigo, Alegra, Facturante).
- Payment rails — Wompi / PayU / Mercado Pago / Bold are the Colombian options.
- Real-time transport — choose (WebSocket / SSE / Pusher / Ably).
- Analytics instrumentation — propose events list after reviewing the admin Analytics screen.

---

## 14. Open Questions for Product

Raise these before starting implementation:

1. Multi-chef / single-chef? The prototype assumes one chef per account, but the name "Pro" hints at a marketplace future.
2. Delivery — does the chef deliver themselves, or integrate Rappi/iFood/DiDi?
3. Is the "Sabor" loyalty program launch-day or phase 2?
4. Are scheduled purchases auto-placed with suppliers (via API) or just a shopping list?
5. Table-ordering / dine-in at the chef's home — in scope?

---

*End of handoff. Ping the design team on any ambiguity — better one question up front than a rebuild later.*
