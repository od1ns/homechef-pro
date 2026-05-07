import 'api_client.dart';
import 'auth_storage.dart';
import '../models/models.dart';

/// High-level wrapper that knows the HomeChef Pro endpoints.
class HcpApi {
  final ApiClient _client;
  HcpApi(this._client);

  AuthStorage get auth => _client.auth;

  /// Acceso al cliente HTTP subyacente — la UI lo usa para suscribirse a
  /// `addUnauthorizedListener` y redirigir al login cuando el token expira.
  ApiClient get client => _client;

  // ---- Auth ----
  Future<AuthResult> login(String email, String password) async {
    final body = await _client.post('/api/auth/login', body: {
      'email': email,
      'password': password,
    });
    final result = AuthResult.fromJson(body as Map<String, dynamic>);
    // F-17: si user tiene 2FA, no guardamos nada — el cliente debe llamar
    // login2fa() con el partial token + codigo.
    if (!result.requires2fa) {
      await _client.auth.save(
        token: result.accessToken,
        userId: result.userId,
        email: result.email,
        roles: result.roles,
        expiresAt: result.expiresAt,
        refreshToken: result.refreshToken,
        refreshExpiresAt: result.refreshExpiresAt,
      );
    }
    return result;
  }

  /// F-17: segundo paso del login cuando user tiene 2FA. Recibe el partial
  /// token del paso 1 + el codigo del authenticator. Retorna AuthResult
  /// completo con accessToken + refreshToken.
  Future<AuthResult> login2fa({
    required String partialToken,
    required String code,
  }) async {
    final body = await _client.post('/api/auth/2fa/login', body: {
      'partialToken': partialToken,
      'code': code,
    });
    final result = AuthResult.fromJson(body as Map<String, dynamic>);
    await _client.auth.save(
      token: result.accessToken,
      userId: result.userId,
      email: result.email,
      roles: result.roles,
      expiresAt: result.expiresAt,
      refreshToken: result.refreshToken,
      refreshExpiresAt: result.refreshExpiresAt,
    );
    return result;
  }

  /// F-17: inicia setup de 2FA. Retorna sharedKey + authenticatorUri (otpauth)
  /// que el cliente convierte en QR. Requiere autenticacion.
  Future<TotpSetupResult> setup2fa() async {
    final body = await _client.post('/api/auth/2fa/setup', body: const {});
    return TotpSetupResult.fromJson(body as Map<String, dynamic>);
  }

  /// F-17: confirma el primer codigo del authenticator y activa 2FA.
  Future<void> verify2faSetup({required String code}) async {
    await _client.post('/api/auth/2fa/verify-setup', body: {'code': code});
  }

  /// F-17: desactiva 2FA. Requiere un codigo TOTP valido como prueba de
  /// posesion del authenticator (defensa contra session hijack que intenta
  /// desactivar 2FA detras del usuario legitimo).
  Future<void> disable2fa({required String code}) async {
    await _client.post('/api/auth/2fa/disable', body: {'code': code});
  }

  Future<AuthResult> register({
    required String email,
    required String password,
    required String fullName,
    String? phone,
    String preferredLanguage = 'es-VE',
    // Sesion A / Frente 1: codigo de invitacion (requerido si server lo exige).
    String? invitationCode,
  }) async {
    final body = await _client.post('/api/auth/register', body: {
      'email': email,
      'password': password,
      'fullName': fullName,
      if (phone != null) 'phone': phone,
      'preferredLanguage': preferredLanguage,
      if (invitationCode != null && invitationCode.isNotEmpty) 'invitationCode': invitationCode,
    });
    final result = AuthResult.fromJson(body as Map<String, dynamic>);
    await _client.auth.save(
      token: result.accessToken,
      userId: result.userId,
      email: result.email,
      roles: result.roles,
      expiresAt: result.expiresAt,
      refreshToken: result.refreshToken,
      refreshExpiresAt: result.refreshExpiresAt,
    );
    return result;
  }

  /// Cierra sesion: revoca el refresh token en el backend y limpia el storage.
  /// Si la red falla en el revoke, igual limpiamos local — el server eventualmente
  /// expira los tokens viejos.
  Future<void> logout() async {
    final refresh = await _client.auth.readRefreshToken();
    if (refresh != null && refresh.isNotEmpty) {
      try {
        await _client.post('/api/auth/logout', body: {'refreshToken': refresh});
      } catch (_) {
        // Logout local sigue adelante aunque el server falle.
      }
    }
    await _client.auth.clear();
  }

  // ---- Public catalog ----
  Future<List<RecipeSummary>> menu() async {
    final body = await _client.get('/api/client/menu');
    return (body as List<dynamic>)
        .map((e) => RecipeSummary.fromJson(e as Map<String, dynamic>))
        .toList(growable: false);
  }

  Future<Recipe> dish(String id) async {
    final body = await _client.get('/api/client/menu/$id');
    return Recipe.fromJson(body as Map<String, dynamic>);
  }

  Future<List<MyReview>> myReviews() async {
    final body = await _client.get('/api/client/reviews/mine');
    return (body as List<dynamic>)
        .map((e) => MyReview.fromJson(e as Map<String, dynamic>))
        .toList(growable: false);
  }

  Future<String> leaveReview({
    required String orderId,
    required String dishId,
    required int rating,
    String? comment,
  }) async {
    final body = await _client.post('/api/client/reviews', body: {
      'orderId': orderId,
      'dishId': dishId,
      'rating': rating,
      if (comment != null) 'comment': comment,
    });
    return (body as Map<String, dynamic>)['id'] as String;
  }

  Future<void> editReview({
    required String reviewId,
    required int rating,
    String? comment,
  }) async {
    await _client.patch('/api/client/reviews/$reviewId',
        body: {
          'rating': rating,
          if (comment != null) 'comment': comment,
        });
  }

  Future<List<PublicReview>> dishReviews(String dishId, {int take = 50}) async {
    final body = await _client.get(
      '/api/client/menu/$dishId/reviews',
      query: {'take': '$take'},
    );
    return (body as List<dynamic>)
        .map((e) => PublicReview.fromJson(e as Map<String, dynamic>))
        .toList(growable: false);
  }

  // ---- Orders (anon guest flow) ----
  /// F-24: el endpoint ahora retorna {id, accessToken}. El cliente DEBE persistir
  /// el accessToken para poder consultar el order via GET /{id}?token=...
  Future<({String id, String accessToken})> createGuestOrder(
      CreateGuestOrderRequest req) async {
    final body = await _client.post('/api/client/orders', body: req.toJson());
    final m = body as Map<String, dynamic>;
    return (
      id: m['id'] as String,
      accessToken: (m['accessToken'] as String?) ?? '',
    );
  }

  /// F-24: si [accessToken] viene, se envia como ?token=... (cliente anonymous).
  /// Si null, asume caller autenticado (admin/cashier) — tipico en tests.
  Future<Map<String, dynamic>> trackOrderRaw(String orderId, {String? accessToken}) async {
    final body = await _client.get(
      '/api/client/orders/$orderId',
      query: {if (accessToken != null && accessToken.isNotEmpty) 'token': accessToken},
    );
    return body as Map<String, dynamic>;
  }

  Future<Order> trackOrder(String orderId, {String? accessToken}) async {
    final raw = await trackOrderRaw(orderId, accessToken: accessToken);
    return Order.fromJson(raw);
  }

  // ---- Admin: orders ----
  Future<List<OrderSummary>> adminActiveOrders({String? statusFilter}) async {
    final body = await _client.get(
      '/api/admin/orders',
      query: {if (statusFilter != null) 'statusFilter': statusFilter},
    );
    return (body as List<dynamic>)
        .map((e) => OrderSummary.fromJson(e as Map<String, dynamic>))
        .toList(growable: false);
  }

  Future<Order> adminGetOrder(String id) async {
    final body = await _client.get('/api/admin/orders/$id');
    return Order.fromJson(body as Map<String, dynamic>);
  }

  Future<void> adminAdvanceOrder(String id, String target, {String? reason}) async {
    await _client.post(
      '/api/admin/orders/$id/advance',
      body: {'target': target, if (reason != null) 'reason': reason},
    );
  }

  // ---- Admin: payments ----
  Future<List<PendingPayment>> adminPendingPayments() async {
    final body = await _client.get('/api/admin/payments/pending');
    return (body as List<dynamic>)
        .map((e) => PendingPayment.fromJson(e as Map<String, dynamic>))
        .toList(growable: false);
  }

  Future<void> adminVerifyPayment(String paymentId) async {
    await _client.post('/api/admin/payments/$paymentId/verify');
  }

  Future<void> adminRejectPayment(String paymentId, String reason) async {
    await _client.post(
      '/api/admin/payments/$paymentId/reject',
      body: {'reason': reason},
    );
  }

  // ---- Admin: recipes ----
  Future<List<RecipeSummary>> adminListRecipes({
    bool includeSubRecipes = false,
    bool onlyActive = true,
    String? search,
  }) async {
    final body = await _client.get('/api/admin/recipes', query: {
      'includeSubRecipes': includeSubRecipes.toString(),
      'onlyActive': onlyActive.toString(),
      if (search != null && search.isNotEmpty) 'search': search,
    });
    return (body as List<dynamic>)
        .map((e) => RecipeSummary.fromJson(e as Map<String, dynamic>))
        .toList(growable: false);
  }

  Future<Recipe> adminGetRecipe(String id) async {
    final body = await _client.get('/api/admin/recipes/$id');
    return Recipe.fromJson(body as Map<String, dynamic>);
  }

  Future<RecipeCost> adminGetRecipeCost(String id) async {
    final body = await _client.get('/api/admin/recipes/$id/cost');
    return RecipeCost.fromJson(body as Map<String, dynamic>);
  }

  Future<String> adminCreateDish({
    required String name,
    required double sellingPriceUsd,
    int prepTimeMinutes = 0,
    String menuType = 'fixed',
    DateTime? specialFrom,
    DateTime? specialTo,
    String? description,
    String? category,
  }) async {
    final body = await _client.post('/api/admin/recipes/dishes', body: {
      'name': name,
      'sellingPriceUsd': sellingPriceUsd,
      'prepTimeMinutes': prepTimeMinutes,
      'menuType': menuType,
      if (specialFrom != null) 'specialFrom': specialFrom.toIso8601String(),
      if (specialTo != null) 'specialTo': specialTo.toIso8601String(),
      if (description != null) 'description': description,
      if (category != null) 'category': category,
    });
    return (body as Map<String, dynamic>)['id'] as String;
  }

  Future<String> adminCreateSubRecipe({
    required String name,
    required double yieldQuantity,
    required String yieldUnit,
    int prepTimeMinutes = 0,
    String? description,
    String? category,
  }) async {
    final body = await _client.post('/api/admin/recipes/sub-recipes', body: {
      'name': name,
      'yieldQuantity': yieldQuantity,
      'yieldUnit': yieldUnit,
      'prepTimeMinutes': prepTimeMinutes,
      if (description != null) 'description': description,
      if (category != null) 'category': category,
    });
    return (body as Map<String, dynamic>)['id'] as String;
  }

  Future<String> adminAddIngredientComponent({
    required String recipeId,
    required String ingredientId,
    required double quantity,
    String? notes,
    int displayOrder = 0,
  }) async {
    final body = await _client.post(
      '/api/admin/recipes/$recipeId/components/ingredient',
      body: {
        'ingredientId': ingredientId,
        'quantity': quantity,
        if (notes != null) 'notes': notes,
        'displayOrder': displayOrder,
      },
    );
    return (body as Map<String, dynamic>)['id'] as String;
  }

  Future<String> adminAddSubRecipeComponent({
    required String recipeId,
    required String subRecipeId,
    required double quantity,
    String? notes,
    int displayOrder = 0,
  }) async {
    final body = await _client.post(
      '/api/admin/recipes/$recipeId/components/sub-recipe',
      body: {
        'subRecipeId': subRecipeId,
        'quantity': quantity,
        if (notes != null) 'notes': notes,
        'displayOrder': displayOrder,
      },
    );
    return (body as Map<String, dynamic>)['id'] as String;
  }

  Future<void> adminUpdateSellingPrice(String recipeId, double sellingPriceUsd) async {
    await _client.patch(
      '/api/admin/recipes/$recipeId/selling-price',
      body: {'sellingPriceUsd': sellingPriceUsd},
    );
  }

  Future<void> adminToggleOutOfStock(String recipeId, bool outOfStock) async {
    await _client.post(
      '/api/admin/recipes/$recipeId/out-of-stock',
      body: {'outOfStock': outOfStock},
    );
  }

  // ---- Admin: ingredients ----
  Future<List<IngredientSummary>> adminListIngredients({
    bool onlyActive = true,
    bool onlyBelowReorder = false,
    String? search,
  }) async {
    final body = await _client.get('/api/admin/ingredients', query: {
      'onlyActive': onlyActive.toString(),
      'onlyBelowReorder': onlyBelowReorder.toString(),
      if (search != null && search.isNotEmpty) 'search': search,
    });
    return (body as List<dynamic>)
        .map((e) => IngredientSummary.fromJson(e as Map<String, dynamic>))
        .toList(growable: false);
  }

  Future<IngredientDetail> adminGetIngredient(String id) async {
    final body = await _client.get('/api/admin/ingredients/$id');
    return IngredientDetail.fromJson(body as Map<String, dynamic>);
  }

  Future<String> adminCreateIngredient({
    required String name,
    required String useUnit,
    double reorderPointUseUnit = 0,
    double minimumStockUseUnit = 0,
    String? description,
  }) async {
    final body = await _client.post('/api/admin/ingredients', body: {
      'name': name,
      'useUnit': useUnit,
      'reorderPointUseUnit': reorderPointUseUnit,
      'minimumStockUseUnit': minimumStockUseUnit,
      if (description != null) 'description': description,
    });
    return (body as Map<String, dynamic>)['id'] as String;
  }

  Future<String> adminAddPresentation({
    required String ingredientId,
    required String name,
    required String purchaseUnit,
    required double purchaseQuantity,
    required double conversionToUseUnit,
    double? lastPurchasePriceUsd,
  }) async {
    final body = await _client.post(
      '/api/admin/ingredients/$ingredientId/presentations',
      body: {
        'name': name,
        'purchaseUnit': purchaseUnit,
        'purchaseQuantity': purchaseQuantity,
        'conversionToUseUnit': conversionToUseUnit,
        if (lastPurchasePriceUsd != null)
          'lastPurchasePriceUsd': lastPurchasePriceUsd,
      },
    );
    return (body as Map<String, dynamic>)['id'] as String;
  }

  Future<void> adminUpdateThresholds({
    required String ingredientId,
    required double reorderPointUseUnit,
    required double minimumStockUseUnit,
  }) async {
    await _client.patch(
      '/api/admin/ingredients/$ingredientId/thresholds',
      body: {
        'reorderPointUseUnit': reorderPointUseUnit,
        'minimumStockUseUnit': minimumStockUseUnit,
      },
    );
  }

  Future<void> adminDeactivateIngredient(String ingredientId) async {
    await _client.post('/api/admin/ingredients/$ingredientId/deactivate');
  }

  Future<String> adminRecordPurchase({
    required String ingredientId,
    required String presentationId,
    required double quantityPurchased,
    required double unitPriceUsd,
    String? supplier,
    String? reference,
    String? notes,
    DateTime? purchasedAt,
  }) async {
    final body = await _client.post('/api/admin/inventory/purchases', body: {
      'ingredientId': ingredientId,
      'presentationId': presentationId,
      'quantityPurchased': quantityPurchased,
      'unitPriceUsd': unitPriceUsd,
      if (supplier != null) 'supplier': supplier,
      if (reference != null) 'reference': reference,
      if (notes != null) 'notes': notes,
      if (purchasedAt != null) 'purchasedAt': purchasedAt.toIso8601String(),
    });
    return (body as Map<String, dynamic>)['id'] as String;
  }

  Future<String> adminRecordWaste({
    required String ingredientId,
    required double quantityUseUnit,
    required String reason,
    String? notes,
    DateTime? recordedAt,
  }) async {
    final body = await _client.post('/api/admin/inventory/waste', body: {
      'ingredientId': ingredientId,
      'quantityUseUnit': quantityUseUnit,
      'reason': reason,
      if (notes != null) 'notes': notes,
      if (recordedAt != null) 'recordedAt': recordedAt.toIso8601String(),
    });
    return (body as Map<String, dynamic>)['id'] as String;
  }

  // ---- Admin: reports ----
  Future<List<DishMarginRow>> adminDishMargin() async {
    final body = await _client.get('/api/admin/reports/dish-margin');
    return (body as List<dynamic>)
        .map((e) => DishMarginRow.fromJson(e as Map<String, dynamic>))
        .toList(growable: false);
  }

  Future<List<RecipeFullCostRow>> adminRecipeCosts(
      {bool includeSubRecipes = false}) async {
    final body = await _client.get('/api/admin/reports/recipe-costs', query: {
      'includeSubRecipes': includeSubRecipes.toString(),
    });
    return (body as List<dynamic>)
        .map((e) => RecipeFullCostRow.fromJson(e as Map<String, dynamic>))
        .toList(growable: false);
  }

  Future<List<ReorderSuggestionRow>> adminReorderSuggestions(
      {String? priority}) async {
    final body = await _client.get('/api/admin/reports/reorder-suggestions',
        query: {if (priority != null) 'priority': priority});
    return (body as List<dynamic>)
        .map((e) => ReorderSuggestionRow.fromJson(e as Map<String, dynamic>))
        .toList(growable: false);
  }

  Future<List<SalesDailyRow>> adminSalesDaily({int days = 30}) async {
    final body = await _client.get('/api/admin/reports/sales-daily',
        query: {'days': days.toString()});
    return (body as List<dynamic>)
        .map((e) => SalesDailyRow.fromJson(e as Map<String, dynamic>))
        .toList(growable: false);
  }

  Future<PurchaseForecast> adminPurchaseForecast({
    int historicalDays = 28,
    int targetDays = 7,
    double growthFactor = 1.0,
  }) async {
    final body = await _client.get('/api/admin/purchasing/forecast', query: {
      'historicalDays': historicalDays.toString(),
      'targetDays': targetDays.toString(),
      'growthFactor': growthFactor.toString(),
    });
    return PurchaseForecast.fromJson(body as Map<String, dynamic>);
  }

  // ---- Kitchen (Cook role) ----
  Future<List<KitchenQueueItem>> kitchenQueue() async {
    final body = await _client.get('/api/kitchen/queue');
    return (body as List<dynamic>)
        .map((e) => KitchenQueueItem.fromJson(e as Map<String, dynamic>))
        .toList(growable: false);
  }

  Future<void> startItem(String orderId, String itemId) async {
    await _client.post('/api/kitchen/orders/$orderId/items/$itemId/start');
  }

  Future<void> markItemReady(String orderId, String itemId) async {
    await _client.post('/api/kitchen/orders/$orderId/items/$itemId/ready');
  }

  /// Returns the receipt PDF bytes for an order. F-24: requiere [accessToken]
  /// para clientes anonymous. Si null, el endpoint solo funciona para admin/cashier
  /// autenticado.
  Future<List<int>> orderReceiptPdf(String orderId, {String? accessToken}) async {
    final body = await _client.get(
      '/api/client/orders/$orderId/receipt.pdf',
      query: {if (accessToken != null && accessToken.isNotEmpty) 'token': accessToken},
    );
    if (body is List<int>) return body;
    throw ApiException(0, 'Expected binary PDF response', null);
  }

  /// Uploads a payment-proof image. F-23: retorna {id, url}. El cliente debe
  /// pasar el `id` (no la url) a [submitPayment] como `proofImageId`. La url
  /// es solo informativa para preview en UI.
  Future<({String id, String url})> uploadPaymentProof({
    required List<int> bytes,
    required String filename,
    required String contentType,
  }) async {
    final body = await _client.postMultipart(
      '/api/uploads/payment-proofs',
      fieldName: 'file',
      filename: filename,
      contentType: contentType,
      bytes: bytes,
    );
    final m = body as Map<String, dynamic>;
    return (id: m['id'] as String, url: m['url'] as String);
  }

  /// Submits the payment proof for a guest order. F-23: el cliente debe primero
  /// llamar [uploadPaymentProof] para obtener el id del comprobante, y pasarlo
  /// aqui como [proofImageId]. El servidor valida que el id existe y no esta
  /// reclamado, y lo marca como reclamado al asociarlo al payment.
  Future<String> submitPayment({
    required String orderId,
    required String method,
    required double amountUsd,
    required String paidCurrency,
    required double amountPaidCurrency,
    double? exchangeRateUsed,
    String? referenceNumber,
    String? proofImageId,
    String? payerName,
    String? payerPhone,
  }) async {
    final body = await _client.post(
      '/api/client/orders/$orderId/payment',
      body: {
        'method': method,
        'amountUsd': amountUsd,
        'paidCurrency': paidCurrency,
        'amountPaidCurrency': amountPaidCurrency,
        if (exchangeRateUsed != null) 'exchangeRateUsed': exchangeRateUsed,
        if (referenceNumber != null) 'referenceNumber': referenceNumber,
        if (proofImageId != null) 'proofImageId': proofImageId,
        if (payerName != null) 'payerName': payerName,
        if (payerPhone != null) 'payerPhone': payerPhone,
      },
    );
    return (body as Map<String, dynamic>)['id'] as String;
  }

  // ---- Customer preferences (auth) ----
  Future<({Map<String, dynamic> payload, DateTime? updatedAt})> getMyPreferences() async {
    final body = await _client.get('/api/client/me/preferences');
    final map = body as Map<String, dynamic>;
    final payload = (map['payload'] as Map<String, dynamic>?) ?? <String, dynamic>{};
    final updatedRaw = map['updatedAt'] as String?;
    return (
      payload: payload,
      updatedAt: updatedRaw == null ? null : DateTime.parse(updatedRaw),
    );
  }

  Future<void> putMyPreferences(Map<String, dynamic> payload) async {
    await _client.put('/api/client/me/preferences', body: payload);
  }

  // ---- Loyalty (Sabor) ----
  Future<LoyaltyAccount> loyaltyMe() async {
    final body = await _client.get('/api/client/loyalty/me');
    return LoyaltyAccount.fromJson(body as Map<String, dynamic>);
  }

  Future<List<LoyaltyReward>> loyaltyRewards() async {
    final body = await _client.get('/api/client/loyalty/rewards');
    return (body as List<dynamic>)
        .map((e) => LoyaltyReward.fromJson(e as Map<String, dynamic>))
        .toList(growable: false);
  }

  Future<RedeemRewardResult> loyaltyRedeem(String rewardId) async {
    final body = await _client.post('/api/client/loyalty/redeem/$rewardId');
    return RedeemRewardResult.fromJson(body as Map<String, dynamic>);
  }

  // ---- Invitations (Admin) ----

  /// Sesion A / Frente 1: lista codigos de invitacion. Solo Admin.
  Future<List<InvitationCodeDto>> listInvitations({
    bool onlyActive = false,
    String? chefId,
    int pageSize = 50,
  }) async {
    final query = <String, String>{
      'onlyActive': onlyActive ? 'true' : 'false',
      'pageSize': '$pageSize',
    };
    if (chefId != null) query['chefId'] = chefId;
    final body = await _client.get('/api/admin/invitations', query: query);
    return (body as List<dynamic>)
        .map((e) => InvitationCodeDto.fromJson(e as Map<String, dynamic>))
        .toList(growable: false);
  }

  /// Sesion A: crear codigo. Solo Admin.
  Future<InvitationCodeDto> createInvitation({
    int maxUses = 1,
    DateTime? expiresAt,
    String? notes,
    String? customCode,
    String? chefId,
  }) async {
    final body = await _client.post('/api/admin/invitations', body: {
      'maxUses': maxUses,
      if (expiresAt != null) 'expiresAt': expiresAt.toUtc().toIso8601String(),
      if (notes != null) 'notes': notes,
      if (customCode != null) 'customCode': customCode,
      if (chefId != null) 'chefId': chefId,
    });
    return InvitationCodeDto.fromJson(body as Map<String, dynamic>);
  }

  /// Sesion A: revocar codigo. Solo Admin.
  Future<void> revokeInvitation(String id, {String? reason}) async {
    await _client.post('/api/admin/invitations/$id/revoke', body: {
      if (reason != null) 'reason': reason,
    });
  }
}
