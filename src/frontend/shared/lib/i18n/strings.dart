/// Bilingual dictionary seeded from `design_handoff_homechef_pro/data.jsx` HCP_I18N.
/// Spanish (es) is primary; English (en) secondary.
enum HcpLang { es, en }

class HcpStrings {
  final Map<String, String> _map;
  const HcpStrings._(this._map);

  String t(String key, {String? fallback}) =>
      _map[key] ?? fallback ?? key;

  static HcpStrings of(HcpLang lang) =>
      HcpStrings._(lang == HcpLang.en ? _en : _es);

  static const Map<String, String> _es = {
    'app.name': 'HomeChef Pro',
    'app.tagline': 'Cocina casera, hecha con amor',
    // Tabs
    'tab.browse': 'Descubrir',
    'tab.orders': 'Pedidos',
    'tab.reviews': 'Reseñas',
    'tab.profile': 'Perfil',
    'tab.sabor': 'Sabor',
    // Loyalty (Sabor)
    'loyalty.title': 'Programa Sabor',
    'loyalty.balance': 'Tus puntos',
    'loyalty.lifetime': 'Total acumulado',
    'loyalty.level': 'Nivel',
    'loyalty.toNextLevel': 'Para llegar a',
    'loyalty.maxLevel': 'Maximo nivel alcanzado',
    'loyalty.rewards': 'Recompensas',
    'loyalty.redeem': 'Canjear',
    'loyalty.redeemed': 'Canjeada',
    'loyalty.notEnough': 'Te faltan puntos',
    'loyalty.empty': 'No hay recompensas activas.',
    'loyalty.confirmTitle': 'Canjear recompensa',
    'loyalty.confirmMsg': 'Vas a usar puntos por esta recompensa. Confirmas?',
    'loyalty.cancel': 'Cancelar',
    'loyalty.guestPrompt': 'Inicia sesion para acumular puntos.',
    // Catalog
    'catalog.todaysMenu': 'Menú de hoy',
    'catalog.popular': 'Más pedidos',
    'catalog.newThisWeek': 'Nuevo esta semana',
    'catalog.empty': 'No hay platos disponibles ahora.',
    'catalog.error': 'No pudimos cargar el menú.',
    'catalog.retry': 'Reintentar',
    // Dish
    'dish.addToCart': 'Agregar',
    'dish.viewCart': 'Ver carrito',
    'dish.minutes': 'min',
    'dish.readyIn': 'Listo en',
    'dish.outOfStock': 'Agotado por hoy',
    // Cart / checkout
    'cart.title': 'Carrito',
    'cart.subtotal': 'Subtotal',
    'cart.deliveryFee': 'Envío',
    'cart.tax': 'IVA',
    'cart.total': 'Total',
    'cart.notes': 'Notas para el chef',
    'cart.placeOrder': 'Confirmar pedido',
    'cart.pickup': 'Recoger',
    'cart.delivery': 'A domicilio',
    'cart.checkout': 'Pagar',
    // Tracking
    'order.received': 'Pedido recibido',
    'order.cooking': 'Cocinando',
    'order.ready': 'Listo',
    'order.onTheWay': 'En camino',
    'order.delivered': 'Entregado',
    // Reviews
    'review.write': 'Escribir reseña',
    'review.submit': 'Enviar',
    'review.placeholder': 'Cuéntale al chef qué te pareció…',
    // Generic
    'common.back': 'Atrás',
    'common.save': 'Guardar',
    'common.cancel': 'Cancelar',
    'common.edit': 'Editar',
    'common.add': 'Agregar',
    'common.loading': 'Cargando…',
    'common.error': 'Algo salió mal.',
  };

  static const Map<String, String> _en = {
    'app.name': 'HomeChef Pro',
    'app.tagline': 'Home cooking, made with love',
    'tab.browse': 'Discover',
    'tab.orders': 'Orders',
    'tab.reviews': 'Reviews',
    'tab.profile': 'Profile',
    'tab.sabor': 'Rewards',
    // Loyalty (Sabor)
    'loyalty.title': 'Sabor program',
    'loyalty.balance': 'Your points',
    'loyalty.lifetime': 'Lifetime earned',
    'loyalty.level': 'Tier',
    'loyalty.toNextLevel': 'To reach',
    'loyalty.maxLevel': 'Top tier reached',
    'loyalty.rewards': 'Rewards',
    'loyalty.redeem': 'Redeem',
    'loyalty.redeemed': 'Redeemed',
    'loyalty.notEnough': 'Not enough points',
    'loyalty.empty': 'No active rewards.',
    'loyalty.confirmTitle': 'Redeem reward',
    'loyalty.confirmMsg': 'You will spend points for this reward. Confirm?',
    'loyalty.cancel': 'Cancel',
    'loyalty.guestPrompt': 'Log in to earn points.',
    'catalog.todaysMenu': "Today's menu",
    'catalog.popular': 'Most ordered',
    'catalog.newThisWeek': 'New this week',
    'catalog.empty': 'No dishes available right now.',
    'catalog.error': "Couldn't load the menu.",
    'catalog.retry': 'Retry',
    'dish.addToCart': 'Add',
    'dish.viewCart': 'View cart',
    'dish.minutes': 'min',
    'dish.readyIn': 'Ready in',
    'dish.outOfStock': 'Sold out today',
    'cart.title': 'Cart',
    'cart.subtotal': 'Subtotal',
    'cart.deliveryFee': 'Delivery',
    'cart.tax': 'Tax',
    'cart.total': 'Total',
    'cart.notes': 'Notes for the chef',
    'cart.placeOrder': 'Place order',
    'cart.pickup': 'Pickup',
    'cart.delivery': 'Delivery',
    'cart.checkout': 'Checkout',
    'order.received': 'Order received',
    'order.cooking': 'Cooking',
    'order.ready': 'Ready',
    'order.onTheWay': 'On the way',
    'order.delivered': 'Delivered',
    'review.write': 'Write a review',
    'review.submit': 'Submit',
    'review.placeholder': 'Tell the chef what you thought…',
    'common.back': 'Back',
    'common.save': 'Save',
    'common.cancel': 'Cancel',
    'common.edit': 'Edit',
    'common.add': 'Add',
    'common.loading': 'Loading…',
    'common.error': 'Something went wrong.',
  };
}
