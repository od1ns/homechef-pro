// Shared data + i18n for HomeChef Pro prototype
// Everything (dishes, recipes, orders, inventory, reviews, analytics) lives here
// so both the customer app and admin dashboard reference the same source of truth.

// ─────────────────────────────────────────────────────────────
// Theme palettes — 4 complete color systems
// ─────────────────────────────────────────────────────────────
const HCP_THEMES = {
  // Original — plum + sage (Venezuelan twilight)
  plum: {
    name: { es: 'Ciruela (original)', en: 'Plum (original)' },
    bg: '#F4F1EC', card: '#FFFFFF',
    ink: '#2A1F3D', inkSoft: '#6B5F7A', inkMuted: '#A89EB4',
    line: '#E5DFE8',
    accent: '#7B4FB8', accentDark: '#5E3A93',
    green: '#3D6B5C', greenSoft: '#DCE8E2',
    sun: '#C8A8D4',
    red: '#B5463E', redSoft: '#F3E0DD',
    sidebar: '#2A1F3D', sidebarText: '#E0D8EC', sidebarMuted: '#8A7DA0',
  },
  // Paprika + cream — warm, earthy, Mediterranean
  paprika: {
    name: { es: 'Paprika', en: 'Paprika' },
    bg: '#F6EFE4', card: '#FFFFFF',
    ink: '#2D1B12', inkSoft: '#6E544A', inkMuted: '#B29B8F',
    line: '#EDE1D3',
    accent: '#C14D2A', accentDark: '#8F3418',
    green: '#5C6B3D', greenSoft: '#E2E8DC',
    sun: '#E8B66A',
    red: '#A13B2E', redSoft: '#F3DDD8',
    sidebar: '#2D1B12', sidebarText: '#F0E2D4', sidebarMuted: '#9A8478',
  },
  // Caribbean — tropical teal + coral
  caribbean: {
    name: { es: 'Caribe', en: 'Caribbean' },
    bg: '#EEF4F3', card: '#FFFFFF',
    ink: '#0F2E2B', inkSoft: '#4E6E6A', inkMuted: '#9FB4B1',
    line: '#D9E4E2',
    accent: '#E26D5C', accentDark: '#B0493A',
    green: '#1E7A6B', greenSoft: '#D4E7E3',
    sun: '#F4C06B',
    red: '#C84B3E', redSoft: '#F5DDD8',
    sidebar: '#0F2E2B', sidebarText: '#D4E7E3', sidebarMuted: '#7A9A95',
  },
  // Noche — dark mode, moody neutrals + gold accent
  noche: {
    name: { es: 'Noche', en: 'Night' },
    bg: '#18161E', card: '#22202A',
    ink: '#F0ECE4', inkSoft: '#B5AFA4', inkMuted: '#6E6A63',
    line: '#2E2B37',
    accent: '#D4A74E', accentDark: '#B0862F',
    green: '#6CA088', greenSoft: '#2D3833',
    sun: '#E8C97A',
    red: '#D46C5E', redSoft: '#3A2622',
    sidebar: '#0F0E14', sidebarText: '#E8E2D5', sidebarMuted: '#787263',
  },
};

// Read theme at runtime so Tweaks can switch it
window.HCP_ACTIVE_THEME = window.HCP_ACTIVE_THEME || 'plum';
function hcpTheme() { return HCP_THEMES[window.HCP_ACTIVE_THEME] || HCP_THEMES.plum; }

// ─────────────────────────────────────────────────────────────
// i18n dictionary
// ─────────────────────────────────────────────────────────────
const HCP_I18N = {
  es: {
    // General
    appName: 'HomeChef Pro',
    tagline: 'Cocina casera, hecha con amor',
    // Customer tabs
    browse: 'Descubrir', orders: 'Pedidos', reviews: 'Reseñas', profile: 'Perfil',
    // Customer catalog
    todaysMenu: "Menú de hoy", popular: 'Más pedidos', newThisWeek: 'Nuevo esta semana',
    addToCart: 'Agregar', viewCart: 'Ver carrito', checkout: 'Pagar',
    pickup: 'Recoger', delivery: 'A domicilio',
    minutes: 'min', readyIn: 'Listo en',
    // Cart
    cart: 'Carrito', subtotal: 'Subtotal', deliveryFee: 'Envío', tax: 'IVA', total: 'Total',
    notes: 'Notas para el chef', placeOrder: 'Confirmar pedido',
    // Tracking
    orderReceived: 'Pedido recibido', cooking: 'Cocinando', ready: 'Listo', onTheWay: 'En camino', delivered: 'Entregado',
    // Reviews
    writeReview: 'Escribir reseña', submit: 'Enviar',
    // Admin sidebar
    overview: 'Resumen', ordersLive: 'Órdenes en vivo', menu: 'Menú y recetas',
    inventory: 'Inventario', purchasing: 'Compras', analytics: 'Analítica', invoices: 'Facturas', settings: 'Ajustes',
    // Admin - Orders
    incoming: 'Entrantes', inKitchen: 'En cocina', readyForPickup: 'Listas', completed: 'Completadas',
    accept: 'Aceptar', markReady: 'Marcar lista', paymentVerified: 'Pago verificado',
    // Admin - Recipe editor
    recipe: 'Receta', ingredients: 'Ingredientes', subRecipe: 'Sub-receta', procedure: 'Procedimiento',
    costStructure: 'Estructura de costos', suggestedPrice: 'Precio sugerido',
    foodCost: 'Costo de insumos', labor: 'Mano de obra', overhead: 'Gastos fijos',
    targetMargin: 'Margen objetivo', currentPrice: 'Precio actual',
    // Admin - Inventory
    inStock: 'En stock', lowStock: 'Stock bajo', outOfStock: 'Agotado',
    reorderPoint: 'Punto de reorden', supplier: 'Proveedor', unitCost: 'Costo unit.',
    schedulePurchase: 'Programar compra',
    // Admin - Analytics
    revenue: 'Ingresos', profit: 'Ganancia', orderCount: 'Órdenes', avgTicket: 'Ticket promedio',
    topDishes: 'Platos estrella', profitByDish: 'Ganancia por plato',
    thisWeek: 'Esta semana', thisMonth: 'Este mes', today: 'Hoy',
    // Common
    back: 'Atrás', save: 'Guardar', cancel: 'Cancelar', edit: 'Editar', add: 'Agregar',
  },
  en: {
    appName: 'HomeChef Pro',
    tagline: 'Home cooking, made with love',
    browse: 'Discover', orders: 'Orders', reviews: 'Reviews', profile: 'Profile',
    todaysMenu: "Today's menu", popular: 'Most ordered', newThisWeek: 'New this week',
    addToCart: 'Add', viewCart: 'View cart', checkout: 'Checkout',
    pickup: 'Pickup', delivery: 'Delivery',
    minutes: 'min', readyIn: 'Ready in',
    cart: 'Cart', subtotal: 'Subtotal', deliveryFee: 'Delivery', tax: 'Tax', total: 'Total',
    notes: 'Notes for the chef', placeOrder: 'Place order',
    orderReceived: 'Order received', cooking: 'Cooking', ready: 'Ready', onTheWay: 'On the way', delivered: 'Delivered',
    writeReview: 'Write a review', submit: 'Submit',
    overview: 'Overview', ordersLive: 'Live orders', menu: 'Menu & recipes',
    inventory: 'Inventory', purchasing: 'Purchasing', analytics: 'Analytics', invoices: 'Invoices', settings: 'Settings',
    incoming: 'Incoming', inKitchen: 'In kitchen', readyForPickup: 'Ready', completed: 'Completed',
    accept: 'Accept', markReady: 'Mark ready', paymentVerified: 'Payment verified',
    recipe: 'Recipe', ingredients: 'Ingredients', subRecipe: 'Sub-recipe', procedure: 'Procedure',
    costStructure: 'Cost structure', suggestedPrice: 'Suggested price',
    foodCost: 'Food cost', labor: 'Labor', overhead: 'Overhead',
    targetMargin: 'Target margin', currentPrice: 'Current price',
    inStock: 'In stock', lowStock: 'Low stock', outOfStock: 'Out of stock',
    reorderPoint: 'Reorder point', supplier: 'Supplier', unitCost: 'Unit cost',
    schedulePurchase: 'Schedule purchase',
    revenue: 'Revenue', profit: 'Profit', orderCount: 'Orders', avgTicket: 'Avg ticket',
    topDishes: 'Top dishes', profitByDish: 'Profit by dish',
    thisWeek: 'This week', thisMonth: 'This month', today: 'Today',
    back: 'Back', save: 'Save', cancel: 'Cancel', edit: 'Edit', add: 'Add',
  },
};

// ─────────────────────────────────────────────────────────────
// Dishes (with bilingual name/desc, recipes, sub-recipes)
// ─────────────────────────────────────────────────────────────
const HCP_DISHES = [
  {
    id: 'pabellon',
    name: { es: 'Pabellón Criollo', en: 'Pabellón Criollo' },
    desc: {
      es: 'El plato nacional. Carne mechada, caraotas negras, arroz blanco y tajadas de plátano maduro. Los colores de la bandera en un solo plato.',
      en: 'The national dish. Shredded beef, black beans, white rice and sweet plantain slices. The flag colors on one plate.',
    },
    category: { es: 'Plato fuerte', en: 'Mains' },
    price: 32000, cost: 10800, rating: 4.9, reviews: 186,
    prepTime: 28, tag: 'popular',
    swatch: ['#7B4FB8', '#3D6B5C'],
    emoji: '🍖',
    photo: 'https://images.unsplash.com/photo-1625938145312-c7deb93bde7f?w=600&q=80',
    ingredients: [
      { name: { es: 'Falda de res', en: 'Beef flank' }, qty: '180g', cost: 3200 },
      { name: { es: 'Caraotas negras', en: 'Black beans' }, qty: '150g', cost: 1200 },
      { name: { es: 'Arroz blanco', en: 'White rice' }, qty: '100g', cost: 500 },
      { name: { es: 'Plátano maduro', en: 'Ripe plantain' }, qty: '1 unidad', cost: 900 },
      { name: { es: 'Cebolla', en: 'Onion' }, qty: '40g', cost: 300 },
      { name: { es: 'Pimentón', en: 'Bell pepper' }, qty: '30g', cost: 400 },
      { name: { es: 'Tomate', en: 'Tomato' }, qty: '50g', cost: 350 },
      { name: { es: 'Ajo', en: 'Garlic' }, qty: '5g', cost: 100 },
      { name: { es: 'Comino', en: 'Cumin' }, qty: '2g', cost: 80 },
    ],
    subRecipes: ['sofrito'],
    steps: {
      es: [
        'Hervir la falda con laurel y cebolla 1.5h, dejar enfriar en el caldo y desmechar.',
        'Sofreír con sofrito venezolano hasta que la carne absorba todo el sabor.',
        'Cocinar caraotas con pimentón y comino hasta que queden cremosas.',
        'Freír tajadas de plátano maduro hasta dorar.',
        'Servir los componentes separados sin mezclar, imitando los colores del pabellón.',
      ],
      en: [
        'Simmer flank with bay leaf and onion 1.5h, cool in broth and shred.',
        'Sauté with Venezuelan sofrito until meat absorbs all flavor.',
        'Cook black beans with pepper and cumin until creamy.',
        'Fry ripe plantain slices until golden.',
        'Serve components separated without mixing, mirroring the pabellón colors.',
      ],
    },
  },
  {
    id: 'reina',
    name: { es: 'Arepa Reina Pepiada', en: 'Reina Pepiada Arepa' },
    desc: {
      es: 'La reina de las arepas: maíz asado, rellena de ensalada cremosa de pollo con aguacate maduro y un toque de mayonesa casera.',
      en: 'The queen of arepas: grilled corn patty stuffed with creamy chicken-avocado salad and a touch of house mayo.',
    },
    category: { es: 'Entrada', en: 'Starter' },
    price: 14000, cost: 4200, rating: 4.9, reviews: 214,
    prepTime: 14, tag: 'popular',
    swatch: ['#C8A8D4', '#7B4FB8'],
    emoji: '🫓',
    photo: 'https://images.unsplash.com/photo-1626203309569-a21b72b30b5d?w=600&q=80',
    ingredients: [
      { name: { es: 'Harina P.A.N.', en: 'P.A.N. corn flour' }, qty: '100g', cost: 600 },
      { name: { es: 'Pechuga de pollo', en: 'Chicken breast' }, qty: '100g', cost: 1800 },
      { name: { es: 'Aguacate maduro', en: 'Ripe avocado' }, qty: '½ unidad', cost: 1200 },
      { name: { es: 'Mayonesa casera', en: 'House mayo' }, qty: '20g', cost: 400 },
      { name: { es: 'Cilantro', en: 'Cilantro' }, qty: '5g', cost: 150 },
      { name: { es: 'Limón', en: 'Lime' }, qty: '¼ unidad', cost: 50 },
    ],
    subRecipes: [],
    steps: {
      es: [
        'Amasar la harina P.A.N. con agua tibia y sal hasta lograr una masa suave sin grietas.',
        'Formar discos de 1cm y asar en budare caliente 5 min por lado hasta que suenen huecos.',
        'Hervir pechuga, desmechar y mezclar con aguacate machacado, mayo y cilantro.',
        'Abrir la arepa por un lado formando un bolsillo y rellenar generosamente.',
      ],
      en: [
        'Knead P.A.N. flour with warm water and salt until smooth dough with no cracks.',
        'Form 1cm discs and grill on hot budare 5 min per side until they sound hollow.',
        'Boil chicken, shred, mix with mashed avocado, mayo and cilantro.',
        'Split arepa on one side forming a pocket and stuff generously.',
      ],
    },
  },
  {
    id: 'hallaca',
    name: { es: 'Hallaca Navideña', en: 'Christmas Hallaca' },
    desc: {
      es: 'Masa de maíz teñida con onoto, rellena de guiso de res, cerdo y pollo con aceitunas, pasas y alcaparras. Envuelta en hoja de plátano.',
      en: 'Corn dough tinted with annatto, filled with beef, pork and chicken stew, olives, raisins and capers. Wrapped in plantain leaf.',
    },
    category: { es: 'Plato fuerte', en: 'Mains' },
    price: 18000, cost: 6400, rating: 4.9, reviews: 97,
    prepTime: 22, tag: 'new',
    swatch: ['#3D6B5C', '#C8A8D4'],
    emoji: '🎄',
    photo: 'https://images.unsplash.com/photo-1574484284002-952d92456975?w=600&q=80',
    ingredients: [
      { name: { es: 'Harina de maíz', en: 'Corn flour' }, qty: '100g', cost: 600 },
      { name: { es: 'Aceite onotado', en: 'Annatto oil' }, qty: '20ml', cost: 400 },
      { name: { es: 'Guiso tricarne', en: 'Three-meat stew' }, qty: '80g', cost: 2800 },
      { name: { es: 'Aceitunas verdes', en: 'Green olives' }, qty: '4 und', cost: 300 },
      { name: { es: 'Pasas', en: 'Raisins' }, qty: '4 und', cost: 150 },
      { name: { es: 'Alcaparras', en: 'Capers' }, qty: '4 und', cost: 200 },
      { name: { es: 'Pimentón', en: 'Bell pepper' }, qty: '15g', cost: 150 },
      { name: { es: 'Hoja de plátano', en: 'Plantain leaf' }, qty: '1 und', cost: 500 },
    ],
    subRecipes: ['guisotricarne'],
    steps: {
      es: [
        'Preparar aceite onotado calentando el onoto en aceite hasta que suelte color rojizo.',
        'Amasar harina de maíz con caldo de pollo y aceite onotado hasta lograr color naranja.',
        'Limpiar hojas de plátano, cortar en cuadros y engrasar con aceite onotado.',
        'Extender la masa, colocar guiso en el centro con aceitunas, pasas, alcaparras y pimentón.',
        'Doblar hoja como regalo, amarrar con pabilo y cocinar en agua hirviendo 45 min.',
      ],
      en: [
        'Make annatto oil by heating annatto seeds in oil until releasing red color.',
        'Knead corn flour with chicken broth and annatto oil until orange.',
        'Clean plantain leaves, cut into squares, grease with annatto oil.',
        'Spread dough, place stew in center with olives, raisins, capers and pepper.',
        'Fold leaf like a gift, tie with twine and boil 45 min.',
      ],
    },
  },
  {
    id: 'cachapa',
    name: { es: 'Cachapa con Queso de Mano', en: 'Cachapa with Queso de Mano' },
    desc: {
      es: 'Panqueca dulce de jojoto (maíz tierno) tostada en budare, doblada sobre queso de mano fresco que se derrite al contacto.',
      en: 'Sweet tender-corn pancake griddled on budare, folded over fresh queso de mano that melts on contact.',
    },
    category: { es: 'Plato fuerte', en: 'Mains' },
    price: 16000, cost: 4800, rating: 4.8, reviews: 128,
    prepTime: 10, tag: 'popular',
    swatch: ['#D4B86A', '#7B4FB8'],
    emoji: '🌽',
    photo: 'https://images.unsplash.com/photo-1630383249896-424e482df921?w=600&q=80',
    ingredients: [
      { name: { es: 'Jojoto (maíz tierno)', en: 'Tender corn' }, qty: '200g', cost: 1200 },
      { name: { es: 'Queso de mano', en: 'Queso de mano' }, qty: '80g', cost: 2400 },
      { name: { es: 'Leche', en: 'Milk' }, qty: '30ml', cost: 150 },
      { name: { es: 'Azúcar', en: 'Sugar' }, qty: '10g', cost: 50 },
      { name: { es: 'Mantequilla', en: 'Butter' }, qty: '15g', cost: 400 },
    ],
    subRecipes: [],
    steps: {
      es: [
        'Licuar los granos de jojoto con leche, azúcar y sal hasta una mezcla espesa con trocitos.',
        'Verter un cucharón grande en budare caliente con mantequilla, formar disco de 20cm.',
        'Cocinar 3 min hasta que los bordes se doren y burbujee encima, voltear con cuidado.',
        'Colocar queso de mano en el centro, doblar por la mitad y servir inmediatamente.',
      ],
      en: [
        'Blend corn kernels with milk, sugar and salt until thick mix with little chunks.',
        'Pour a big ladle onto hot buttered budare, form 20cm disc.',
        'Cook 3 min until edges brown and top bubbles, flip carefully.',
        'Place queso de mano in center, fold in half and serve immediately.',
      ],
    },
  },
  {
    id: 'tequenos',
    name: { es: 'Tequeños Caseros', en: 'House Tequeños' },
    desc: {
      es: 'Los favoritos de toda fiesta venezolana. Dedos de masa crocante rellenos de queso blanco duro que se derrite al freír. Con salsa rosada.',
      en: 'The star of every Venezuelan party. Crunchy dough fingers stuffed with firm white cheese that melts when fried. With pink sauce.',
    },
    category: { es: 'Entrada', en: 'Starter' },
    price: 12000, cost: 3600, rating: 4.9, reviews: 243,
    prepTime: 8, tag: 'popular',
    swatch: ['#E8C580', '#B5463E'],
    emoji: '🧀',
    photo: 'https://images.unsplash.com/photo-1619683548293-50cbd1c2ca02?w=600&q=80',
    ingredients: [
      { name: { es: 'Harina de trigo', en: 'Wheat flour' }, qty: '60g', cost: 300 },
      { name: { es: 'Queso blanco duro', en: 'Firm white cheese' }, qty: '100g', cost: 2200 },
      { name: { es: 'Mantequilla', en: 'Butter' }, qty: '15g', cost: 400 },
      { name: { es: 'Huevo', en: 'Egg' }, qty: '½ und', cost: 300 },
      { name: { es: 'Aceite para freír', en: 'Frying oil' }, qty: '50ml', cost: 400 },
    ],
    subRecipes: [],
    steps: {
      es: [
        'Amasar harina con mantequilla, huevo, sal y agua tibia. Dejar reposar 30 min.',
        'Cortar el queso en bastones rectangulares de 1.5 x 6 cm.',
        'Estirar la masa delgada y cortar tiras. Enrollar cada bastón de queso en espiral.',
        'Freír en aceite profundo a 180°C 3-4 min hasta dorar uniformemente.',
        'Escurrir en papel absorbente y servir con salsa rosada o guasacaca.',
      ],
      en: [
        'Knead flour with butter, egg, salt and warm water. Rest 30 min.',
        'Cut cheese into 1.5 x 6 cm sticks.',
        'Roll dough thin and cut strips. Wrap each cheese stick in spiral.',
        'Deep-fry at 180°C 3-4 min until evenly golden.',
        'Drain on paper and serve with pink sauce or guasacaca.',
      ],
    },
  },
  {
    id: 'asadonegro',
    name: { es: 'Asado Negro', en: 'Asado Negro' },
    desc: {
      es: 'Muchacho redondo braseado lento en una salsa oscura de papelón y vino tinto. La clásica del domingo, con arroz y tajadas.',
      en: 'Eye-round beef slow-braised in a dark sauce of brown sugar and red wine. The Sunday classic, with rice and plantain.',
    },
    category: { es: 'Plato fuerte', en: 'Mains' },
    price: 34000, cost: 11500, rating: 4.8, reviews: 72,
    prepTime: 35, tag: 'new',
    swatch: ['#2A1F3D', '#C8A8D4'],
    emoji: '🥩',
    photo: 'https://images.unsplash.com/photo-1544025162-d76694265947?w=600&q=80',
    ingredients: [
      { name: { es: 'Muchacho redondo', en: 'Eye round beef' }, qty: '200g', cost: 4200 },
      { name: { es: 'Papelón', en: 'Panela/brown sugar' }, qty: '40g', cost: 400 },
      { name: { es: 'Vino tinto', en: 'Red wine' }, qty: '50ml', cost: 900 },
      { name: { es: 'Cebolla', en: 'Onion' }, qty: '80g', cost: 400 },
      { name: { es: 'Pimentón', en: 'Bell pepper' }, qty: '40g', cost: 500 },
      { name: { es: 'Salsa inglesa', en: 'Worcestershire' }, qty: '10ml', cost: 300 },
      { name: { es: 'Arroz', en: 'Rice' }, qty: '100g', cost: 500 },
      { name: { es: 'Plátano maduro', en: 'Ripe plantain' }, qty: '½ und', cost: 450 },
    ],
    subRecipes: ['sofrito'],
    steps: {
      es: [
        'Mechar el muchacho con ajo y pimentón, adobar con sal y pimienta.',
        'Derretir papelón en aceite hasta caramelo oscuro, sellar la carne volteando constantemente.',
        'Agregar sofrito, vino tinto, salsa inglesa y agua. Tapar.',
        'Cocinar a fuego muy bajo 3-4 horas hasta que esté blando y la salsa oscura y brillante.',
        'Cortar en rodajas gruesas, napar con la salsa y servir con arroz y tajadas.',
      ],
      en: [
        'Lard the beef with garlic and bell pepper, season with salt and pepper.',
        'Melt panela in oil until dark caramel, sear meat turning constantly.',
        'Add sofrito, red wine, Worcestershire and water. Cover.',
        'Cook on very low 3-4 hours until tender and sauce dark and glossy.',
        'Slice thick, nap with sauce and serve with rice and plantain.',
      ],
    },
  },
];

// Sub-recipes (sauces and prep bases reused across dishes)
const HCP_SUBRECIPES = {
  sofrito: {
    name: { es: 'Sofrito Venezolano', en: 'Venezuelan Sofrito' },
    desc: {
      es: 'Base aromática de cebolla, ají dulce, pimentón y ajo. Alma de la cocina venezolana.',
      en: 'Aromatic base of onion, ají dulce, bell pepper and garlic. Soul of Venezuelan cooking.',
    },
    yield: '400g',
    ingredients: [
      { name: { es: 'Cebolla', en: 'Onion' }, qty: '200g', cost: 800 },
      { name: { es: 'Ají dulce', en: 'Ají dulce pepper' }, qty: '100g', cost: 1800 },
      { name: { es: 'Pimentón rojo', en: 'Red bell pepper' }, qty: '150g', cost: 1200 },
      { name: { es: 'Ajo', en: 'Garlic' }, qty: '20g', cost: 400 },
      { name: { es: 'Cilantro', en: 'Cilantro' }, qty: '20g', cost: 300 },
      { name: { es: 'Aceite', en: 'Oil' }, qty: '40ml', cost: 300 },
    ],
    steps: {
      es: [
        'Picar finamente cebolla, ají dulce, pimentón y ajo.',
        'Sofreír en aceite a fuego medio 8 min hasta que suelten aroma pero sin dorar.',
        'Agregar cilantro picado al final. Enfriar y guardar en frascos.',
      ],
      en: [
        'Finely chop onion, ají dulce, bell pepper and garlic.',
        'Sauté in oil on medium 8 min until fragrant but not browned.',
        'Add chopped cilantro at end. Cool and store in jars.',
      ],
    },
  },
  guisotricarne: {
    name: { es: 'Guiso Tricarne para Hallacas', en: 'Three-Meat Stew for Hallacas' },
    desc: {
      es: 'Guiso profundo de res, cerdo y pollo cocido 3 horas con papelón y vino.',
      en: 'Deep three-meat stew braised 3 hours with panela and wine.',
    },
    yield: '2kg',
    ingredients: [
      { name: { es: 'Res', en: 'Beef' }, qty: '800g', cost: 14000 },
      { name: { es: 'Cerdo', en: 'Pork' }, qty: '500g', cost: 8500 },
      { name: { es: 'Pollo', en: 'Chicken' }, qty: '500g', cost: 6500 },
      { name: { es: 'Sofrito', en: 'Sofrito' }, qty: '400g', cost: 4800 },
      { name: { es: 'Papelón', en: 'Panela' }, qty: '60g', cost: 600 },
      { name: { es: 'Vino tinto', en: 'Red wine' }, qty: '150ml', cost: 2800 },
      { name: { es: 'Salsa inglesa', en: 'Worcestershire' }, qty: '30ml', cost: 600 },
    ],
    steps: {
      es: [
        'Cortar las tres carnes en cubos pequeños de 1cm.',
        'Sellar cada carne por separado hasta dorar.',
        'Unir todas las carnes con el sofrito y cocinar 15 min.',
        'Agregar papelón derretido, vino y salsa inglesa. Cocinar tapado 3h a fuego bajo.',
      ],
      en: [
        'Cut the three meats into small 1cm cubes.',
        'Sear each meat separately until browned.',
        'Combine all meats with sofrito and cook 15 min.',
        'Add melted panela, wine and Worcestershire. Cook covered 3h on low.',
      ],
    },
  },
  guasacaca: {
    name: { es: 'Guasacaca', en: 'Guasacaca' },
    desc: {
      es: 'Salsa verde venezolana de aguacate, cilantro y perejil. Fresca, ácida, imprescindible.',
      en: 'Venezuelan green sauce of avocado, cilantro and parsley. Fresh, tangy, essential.',
    },
    yield: '500g',
    ingredients: [
      { name: { es: 'Aguacate', en: 'Avocado' }, qty: '2 und', cost: 4800 },
      { name: { es: 'Cilantro', en: 'Cilantro' }, qty: '50g', cost: 600 },
      { name: { es: 'Perejil', en: 'Parsley' }, qty: '30g', cost: 400 },
      { name: { es: 'Vinagre blanco', en: 'White vinegar' }, qty: '40ml', cost: 200 },
      { name: { es: 'Aceite', en: 'Oil' }, qty: '100ml', cost: 700 },
      { name: { es: 'Ajo', en: 'Garlic' }, qty: '10g', cost: 200 },
    ],
    steps: {
      es: [
        'Licuar aguacate, cilantro, perejil y ajo con vinagre.',
        'Agregar aceite en hilo con licuadora encendida hasta emulsionar.',
        'Salpimentar. Guardar en frasco con un chorrito de aceite encima.',
      ],
      en: [
        'Blend avocado, cilantro, parsley and garlic with vinegar.',
        'Stream in oil with blender running until emulsified.',
        'Season. Store in jar with a splash of oil on top.',
      ],
    },
  },
};

// ─────────────────────────────────────────────────────────────
// Live orders (admin dashboard)
// ─────────────────────────────────────────────────────────────
const HCP_ORDERS = [
  { id: '#A041', customer: 'María Fernández',   items: [{ dish: 'pabellon', qty: 2 }, { dish: 'tequenos', qty: 2 }],            type: 'delivery', address: 'Av Francisco de Miranda, Chacao',    status: 'incoming', paid: true,  total: 88000, time: '12:42', notes: { es: 'Sin ají por favor', en: 'No chili please' } },
  { id: '#A042', customer: 'Carlos Ruiz',       items: [{ dish: 'reina', qty: 2 }],                                             type: 'pickup',   address: null,                                   status: 'incoming', paid: true,  total: 28000, time: '12:45', notes: null },
  { id: '#A043', customer: 'Ana Gómez',         items: [{ dish: 'cachapa', qty: 2 }, { dish: 'tequenos', qty: 1 }],              type: 'delivery', address: 'Las Mercedes, Baruta',                 status: 'cooking',  paid: true,  total: 44000, time: '12:28', notes: null },
  { id: '#A044', customer: 'Diego Martín',      items: [{ dish: 'asadonegro', qty: 1 }, { dish: 'reina', qty: 2 }],              type: 'pickup',   address: null,                                   status: 'cooking',  paid: true,  total: 62000, time: '12:31', notes: { es: 'Listo a la 1:15', en: 'Ready at 1:15' } },
  { id: '#A045', customer: 'Sofía Pérez',       items: [{ dish: 'pabellon', qty: 1 }, { dish: 'tequenos', qty: 1 }],             type: 'delivery', address: 'Altamira, Chacao',                     status: 'ready',    paid: true,  total: 44000, time: '12:15', notes: null },
  { id: '#A046', customer: 'Luis Ortega',       items: [{ dish: 'hallaca', qty: 4 }],                                            type: 'delivery', address: 'La Castellana, Chacao',                status: 'delivered', paid: true, total: 72000, time: '11:50', notes: null },
  { id: '#A047', customer: 'Paula Rincón',      items: [{ dish: 'cachapa', qty: 1 }],                                            type: 'pickup',   address: null,                                   status: 'delivered', paid: true, total: 16000, time: '11:32', notes: null },
];

// ─────────────────────────────────────────────────────────────
// Inventory
// ─────────────────────────────────────────────────────────────
const HCP_INVENTORY = [
  { id: 'chicken',   name: { es: 'Pollo',            en: 'Chicken' },          unit: 'kg', stock: 4.2,  reorder: 5,   cost: 3800,  supplier: 'Avícola El Tunal',   status: 'low' },
  { id: 'beef',      name: { es: 'Falda de res',     en: 'Beef flank' },       unit: 'kg', stock: 8.5,  reorder: 6,   cost: 9200,  supplier: 'Carnicería Chacao',  status: 'ok' },
  { id: 'pork',      name: { es: 'Cerdo',            en: 'Pork' },             unit: 'kg', stock: 2.1,  reorder: 4,   cost: 7500,  supplier: 'Carnicería Chacao',  status: 'low' },
  { id: 'harinapan', name: { es: 'Harina P.A.N.',    en: 'P.A.N. corn flour' }, unit: 'kg', stock: 14,   reorder: 10,  cost: 1800,  supplier: 'Distribuidora Altamira', status: 'ok' },
  { id: 'caraotas',  name: { es: 'Caraotas negras',  en: 'Black beans' },      unit: 'kg', stock: 3,    reorder: 5,   cost: 2400,  supplier: 'Mercado Guaicaipuro', status: 'low' },
  { id: 'onoto',     name: { es: 'Onoto',            en: 'Annatto seeds' },    unit: 'kg', stock: 0,    reorder: 0.3, cost: 18000, supplier: 'Mercado Guaicaipuro', status: 'out' },
  { id: 'tomato',    name: { es: 'Tomate',           en: 'Tomato' },           unit: 'kg', stock: 18,   reorder: 15,  cost: 1200,  supplier: 'Mercado Guaicaipuro', status: 'ok' },
  { id: 'quesomano', name: { es: 'Queso de mano',    en: 'Queso de mano' },    unit: 'kg', stock: 3.1,  reorder: 3,   cost: 12000, supplier: 'Lácteos Los Andes',   status: 'ok' },
  { id: 'jojoto',    name: { es: 'Jojoto (maíz tierno)', en: 'Tender corn' }, unit: 'und', stock: 14,  reorder: 20,  cost: 800,   supplier: 'Mercado Guaicaipuro', status: 'low' },
  { id: 'platano',   name: { es: 'Plátano maduro',   en: 'Ripe plantain' },    unit: 'und', stock: 22,  reorder: 15,  cost: 600,   supplier: 'Mercado Guaicaipuro', status: 'ok' },
  { id: 'aguacate',  name: { es: 'Aguacate',         en: 'Avocado' },          unit: 'und', stock: 8,   reorder: 12,  cost: 2400,  supplier: 'Mercado Guaicaipuro', status: 'low' },
  { id: 'papelon',   name: { es: 'Papelón',          en: 'Panela' },           unit: 'kg', stock: 5,    reorder: 2,   cost: 3200,  supplier: 'Distribuidora Altamira', status: 'ok' },
  { id: 'ajidulce',  name: { es: 'Ají dulce',        en: 'Ají dulce pepper' }, unit: 'kg', stock: 1.2,  reorder: 1.5, cost: 8000,  supplier: 'Mercado Guaicaipuro', status: 'low' },
];

// ─────────────────────────────────────────────────────────────
// Reviews
// ─────────────────────────────────────────────────────────────
const HCP_REVIEWS = [
  { id: 1, dishId: 'pabellon', customer: 'María F.',  rating: 5, time: { es: 'hace 2 días',   en: '2 days ago' },   text: { es: 'Me supo al pabellón de mi mamá. La carne mechada tiene ese sabor profundo que solo sale con horas de cocción.', en: "Tasted like my mom's pabellón. The shredded beef has that deep flavor only hours of cooking can give." } },
  { id: 2, dishId: 'reina',    customer: 'Carlos R.', rating: 5, time: { es: 'hace 3 días',   en: '3 days ago' },   text: { es: 'La mejor Reina Pepiada de Caracas. Aguacate en su punto, pollo jugoso. Pido dos siempre.', en: 'Best Reina Pepiada in Caracas. Perfect avocado, juicy chicken. I always order two.' } },
  { id: 3, dishId: 'cachapa',  customer: 'Ana G.',    rating: 5, time: { es: 'hace 1 semana', en: '1 week ago' },   text: { es: 'El jojoto se siente fresco, no enlatado. Y el queso de mano se derrite divino.', en: 'You can tell the corn is fresh, not canned. And the queso de mano melts beautifully.' } },
  { id: 4, dishId: 'tequenos', customer: 'Diego M.',  rating: 5, time: { es: 'hace 1 semana', en: '1 week ago' },   text: { es: 'Los tequeños quedan crocantes y con el queso chorreado. Como deben ser.', en: 'Tequeños come crispy with cheese oozing out. As they should be.' } },
  { id: 5, dishId: 'hallaca',  customer: 'Sofía P.',  rating: 5, time: { es: 'hace 2 semanas', en: '2 weeks ago' }, text: { es: 'Las hallacas saben a Navidad. El guiso tiene el balance perfecto de dulce y salado.', en: 'The hallacas taste like Christmas. The stew has the perfect sweet-salty balance.' } },
];

// ─────────────────────────────────────────────────────────────
// Analytics (last 7 days + top dishes)
// ─────────────────────────────────────────────────────────────
const HCP_ANALYTICS = {
  week: [
    { day: { es: 'Lun', en: 'Mon' }, revenue: 842000,  orders: 28, profit: 361000 },
    { day: { es: 'Mar', en: 'Tue' }, revenue: 912000,  orders: 31, profit: 394000 },
    { day: { es: 'Mié', en: 'Wed' }, revenue: 785000,  orders: 26, profit: 336000 },
    { day: { es: 'Jue', en: 'Thu' }, revenue: 1041000, orders: 35, profit: 451000 },
    { day: { es: 'Vie', en: 'Fri' }, revenue: 1284000, orders: 42, profit: 557000 },
    { day: { es: 'Sáb', en: 'Sat' }, revenue: 1456000, orders: 48, profit: 631000 },
    { day: { es: 'Dom', en: 'Sun' }, revenue: 1128000, orders: 38, profit: 489000 },
  ],
  kpis: {
    revenue: 7448000, orders: 248, avgTicket: 30032, profit: 3219000, margin: 0.432,
  },
  topDishes: [
    { id: 'pabellon',   sold: 62, revenue: 1984000, profit: 1314400 },
    { id: 'reina',      sold: 98, revenue: 1372000, profit: 960400 },
    { id: 'tequenos',   sold: 112, revenue: 1344000, profit: 940800 },
    { id: 'cachapa',    sold: 52, revenue: 832000, profit: 582400 },
    { id: 'asadonegro', sold: 28, revenue: 952000, profit: 630000 },
    { id: 'hallaca',    sold: 48, revenue: 864000, profit: 556800 },
  ],
};

// ─────────────────────────────────────────────────────────────
// Utility fns
// ─────────────────────────────────────────────────────────────
function hcpFmt(n, lang) {
  // Colombian peso formatting, bilingual
  if (lang === 'en') return '$' + Math.round(n / 100) / 10 + 'k';
  return '$' + n.toLocaleString('es-CO');
}
function hcpFmtFull(n) {
  return '$' + n.toLocaleString('es-CO');
}
function hcpDish(id) { return HCP_DISHES.find(d => d.id === id); }

Object.assign(window, {
  HCP_I18N, HCP_DISHES, HCP_SUBRECIPES, HCP_ORDERS, HCP_INVENTORY, HCP_REVIEWS, HCP_ANALYTICS,
  hcpFmt, hcpFmtFull, hcpDish,
});
