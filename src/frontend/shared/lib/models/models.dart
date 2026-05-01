/// Hand-written DTOs that mirror the C# `*Dto` records exposed by the API.
/// Keep these in sync with `src/backend/src/HomeChefPro.Application/**/Dtos/`.

class RecipeSummary {
  final String id;
  final String name;
  final String? category;
  final bool isSubRecipe;
  final double? sellingPriceUsd;
  final int prepTimeMinutes;
  final String? imageUrl;
  final bool isActive;
  final bool isOutOfStock;
  final String menuType;

  const RecipeSummary({
    required this.id,
    required this.name,
    required this.category,
    required this.isSubRecipe,
    required this.sellingPriceUsd,
    required this.prepTimeMinutes,
    required this.imageUrl,
    required this.isActive,
    required this.isOutOfStock,
    required this.menuType,
  });

  factory RecipeSummary.fromJson(Map<String, dynamic> j) => RecipeSummary(
        id: j['id'] as String,
        name: j['name'] as String,
        category: j['category'] as String?,
        isSubRecipe: j['isSubRecipe'] as bool? ?? false,
        sellingPriceUsd: (j['sellingPriceUsd'] as num?)?.toDouble(),
        prepTimeMinutes: (j['prepTimeMinutes'] as num?)?.toInt() ?? 0,
        imageUrl: j['imageUrl'] as String?,
        isActive: j['isActive'] as bool? ?? true,
        isOutOfStock: j['isOutOfStock'] as bool? ?? false,
        menuType: j['menuType'] as String? ?? 'fixed',
      );
}

class RecipeComponent {
  final String id;
  final String? ingredientId;
  final String? subRecipeId;
  final double quantity;
  final String? notes;
  final int displayOrder;

  const RecipeComponent({
    required this.id,
    required this.ingredientId,
    required this.subRecipeId,
    required this.quantity,
    required this.notes,
    required this.displayOrder,
  });

  factory RecipeComponent.fromJson(Map<String, dynamic> j) => RecipeComponent(
        id: j['id'] as String,
        ingredientId: j['ingredientId'] as String?,
        subRecipeId: j['subRecipeId'] as String?,
        quantity: (j['quantity'] as num).toDouble(),
        notes: j['notes'] as String?,
        displayOrder: (j['displayOrder'] as num?)?.toInt() ?? 0,
      );
}

class Recipe {
  final String id;
  final String name;
  final String? description;
  final String? category;
  final bool isSubRecipe;
  final String? procedureMarkdown;
  final double? yieldQuantity;
  final String? yieldUnit;
  final double? sellingPriceUsd;
  final int prepTimeMinutes;
  final String? imageUrl;
  final bool isActive;
  final bool isOutOfStock;
  final String menuType;
  final List<RecipeComponent> components;

  const Recipe({
    required this.id,
    required this.name,
    required this.description,
    required this.category,
    required this.isSubRecipe,
    required this.procedureMarkdown,
    required this.yieldQuantity,
    required this.yieldUnit,
    required this.sellingPriceUsd,
    required this.prepTimeMinutes,
    required this.imageUrl,
    required this.isActive,
    required this.isOutOfStock,
    required this.menuType,
    required this.components,
  });

  factory Recipe.fromJson(Map<String, dynamic> j) => Recipe(
        id: j['id'] as String,
        name: j['name'] as String,
        description: j['description'] as String?,
        category: j['category'] as String?,
        isSubRecipe: j['isSubRecipe'] as bool? ?? false,
        procedureMarkdown: j['procedureMarkdown'] as String?,
        yieldQuantity: (j['yieldQuantity'] as num?)?.toDouble(),
        yieldUnit: j['yieldUnit'] as String?,
        sellingPriceUsd: (j['sellingPriceUsd'] as num?)?.toDouble(),
        prepTimeMinutes: (j['prepTimeMinutes'] as num?)?.toInt() ?? 0,
        imageUrl: j['imageUrl'] as String?,
        isActive: j['isActive'] as bool? ?? true,
        isOutOfStock: j['isOutOfStock'] as bool? ?? false,
        menuType: j['menuType'] as String? ?? 'fixed',
        components: (j['components'] as List<dynamic>? ?? const [])
            .map((c) => RecipeComponent.fromJson(c as Map<String, dynamic>))
            .toList(growable: false),
      );
}

class OrderLineInput {
  final String dishId;
  final int quantity;
  final String? itemNotes;

  const OrderLineInput({required this.dishId, required this.quantity, this.itemNotes});

  Map<String, dynamic> toJson() => {
        'dishId': dishId,
        'quantity': quantity,
        if (itemNotes != null) 'itemNotes': itemNotes,
      };
}

class CreateGuestOrderRequest {
  final String guestFullName;
  final String guestPhone;
  final String deliveryType;          // 'pickup' | 'third_party'
  final List<OrderLineInput> items;
  final String? deliveryAddress;
  final String? deliveryInstructions;
  final String? customerNotes;

  const CreateGuestOrderRequest({
    required this.guestFullName,
    required this.guestPhone,
    required this.deliveryType,
    required this.items,
    this.deliveryAddress,
    this.deliveryInstructions,
    this.customerNotes,
  });

  Map<String, dynamic> toJson() => {
        'guestFullName': guestFullName,
        'guestPhone': guestPhone,
        'deliveryType': deliveryType,
        'items': items.map((i) => i.toJson()).toList(),
        if (deliveryAddress != null) 'deliveryAddress': deliveryAddress,
        if (deliveryInstructions != null) 'deliveryInstructions': deliveryInstructions,
        if (customerNotes != null) 'customerNotes': customerNotes,
      };
}

class AuthResult {
  final String userId;
  final String email;
  final String fullName;
  final List<String> roles;
  final String accessToken;
  final DateTime expiresAt;

  const AuthResult({
    required this.userId,
    required this.email,
    required this.fullName,
    required this.roles,
    required this.accessToken,
    required this.expiresAt,
  });

  factory AuthResult.fromJson(Map<String, dynamic> j) => AuthResult(
        userId: j['userId'] as String,
        email: j['email'] as String,
        fullName: j['fullName'] as String,
        roles: ((j['roles'] as List<dynamic>?) ?? const []).map((r) => r as String).toList(),
        accessToken: j['accessToken'] as String,
        expiresAt: DateTime.parse(j['expiresAt'] as String),
      );
}

class OrderItem {
  final String id;
  final String dishId;
  final String dishNameSnapshot;
  final double unitPriceUsd;
  final int quantity;
  final double lineTotalUsd;
  final String? itemNotes;
  final String kitchenStatus;        // pending | in_prep | ready
  final DateTime? prepStartedAt;
  final DateTime? prepCompletedAt;

  const OrderItem({
    required this.id,
    required this.dishId,
    required this.dishNameSnapshot,
    required this.unitPriceUsd,
    required this.quantity,
    required this.lineTotalUsd,
    required this.itemNotes,
    required this.kitchenStatus,
    required this.prepStartedAt,
    required this.prepCompletedAt,
  });

  factory OrderItem.fromJson(Map<String, dynamic> j) => OrderItem(
        id: j['id'] as String,
        dishId: j['dishId'] as String,
        dishNameSnapshot: j['dishNameSnapshot'] as String? ?? '',
        unitPriceUsd: (j['unitPriceUsd'] as num?)?.toDouble() ?? 0,
        quantity: (j['quantity'] as num?)?.toInt() ?? 0,
        lineTotalUsd: (j['lineTotalUsd'] as num?)?.toDouble() ?? 0,
        itemNotes: j['itemNotes'] as String?,
        kitchenStatus: j['kitchenStatus'] as String? ?? 'pending',
        prepStartedAt: _parseDate(j['prepStartedAt']),
        prepCompletedAt: _parseDate(j['prepCompletedAt']),
      );
}

class Order {
  final String id;
  final String orderNumber;
  final String status;                // pending_payment | … | delivered
  final String deliveryType;
  final String? deliveryAddress;
  final double subtotalUsd;
  final double totalUsd;
  final double? totalVesAtOrderTime;
  final DateTime createdAt;
  final DateTime? paidAt;
  final DateTime? readyAt;
  final DateTime? deliveredAt;
  final DateTime? cancelledAt;
  final String? cancellationReason;
  final List<OrderItem> items;

  const Order({
    required this.id,
    required this.orderNumber,
    required this.status,
    required this.deliveryType,
    required this.deliveryAddress,
    required this.subtotalUsd,
    required this.totalUsd,
    required this.totalVesAtOrderTime,
    required this.createdAt,
    required this.paidAt,
    required this.readyAt,
    required this.deliveredAt,
    required this.cancelledAt,
    required this.cancellationReason,
    required this.items,
  });

  factory Order.fromJson(Map<String, dynamic> j) => Order(
        id: j['id'] as String,
        orderNumber: j['orderNumber'] as String? ?? '',
        status: j['status'] as String,
        deliveryType: j['deliveryType'] as String? ?? 'pickup',
        deliveryAddress: j['deliveryAddress'] as String?,
        subtotalUsd: (j['subtotalUsd'] as num?)?.toDouble() ?? 0,
        totalUsd: (j['totalUsd'] as num?)?.toDouble() ?? 0,
        totalVesAtOrderTime: (j['totalVesAtOrderTime'] as num?)?.toDouble(),
        createdAt: DateTime.parse(j['createdAt'] as String),
        paidAt: _parseDate(j['paidAt']),
        readyAt: _parseDate(j['readyAt']),
        deliveredAt: _parseDate(j['deliveredAt']),
        cancelledAt: _parseDate(j['cancelledAt']),
        cancellationReason: j['cancellationReason'] as String?,
        items: (j['items'] as List<dynamic>? ?? const [])
            .map((i) => OrderItem.fromJson(i as Map<String, dynamic>))
            .toList(growable: false),
      );

  bool get isTerminal =>
      status == 'delivered' || status == 'cancelled' || status == 'rejected';
}

class OrderSummary {
  final String id;
  final String orderNumber;
  final String status;
  final String deliveryType;
  final double totalUsd;
  final String customerName;
  final int itemCount;
  final DateTime createdAt;
  final DateTime? prepEstimatedReadyAt;

  const OrderSummary({
    required this.id,
    required this.orderNumber,
    required this.status,
    required this.deliveryType,
    required this.totalUsd,
    required this.customerName,
    required this.itemCount,
    required this.createdAt,
    required this.prepEstimatedReadyAt,
  });

  factory OrderSummary.fromJson(Map<String, dynamic> j) => OrderSummary(
        id: j['id'] as String,
        orderNumber: j['orderNumber'] as String? ?? '',
        status: j['status'] as String,
        deliveryType: j['deliveryType'] as String? ?? 'pickup',
        totalUsd: (j['totalUsd'] as num?)?.toDouble() ?? 0,
        customerName: j['customerName'] as String? ?? 'Cliente',
        itemCount: (j['itemCount'] as num?)?.toInt() ?? 0,
        createdAt: DateTime.parse(j['createdAt'] as String),
        prepEstimatedReadyAt: _parseDate(j['prepEstimatedReadyAt']),
      );
}

class PendingPayment {
  final String id;
  final String orderId;
  final String method;
  final double amountUsd;
  final String paidCurrency;
  final double amountPaidCurrency;
  final String? referenceNumber;
  final String? proofImageUrl;
  final String? payerName;
  final String? payerPhone;
  final DateTime createdAt;

  const PendingPayment({
    required this.id,
    required this.orderId,
    required this.method,
    required this.amountUsd,
    required this.paidCurrency,
    required this.amountPaidCurrency,
    required this.referenceNumber,
    required this.proofImageUrl,
    required this.payerName,
    required this.payerPhone,
    required this.createdAt,
  });

  factory PendingPayment.fromJson(Map<String, dynamic> j) => PendingPayment(
        id: j['id'] as String,
        orderId: j['orderId'] as String,
        method: j['method'] as String,
        amountUsd: (j['amountUsd'] as num).toDouble(),
        paidCurrency: j['paidCurrency'] as String,
        amountPaidCurrency: (j['amountPaidCurrency'] as num).toDouble(),
        referenceNumber: j['referenceNumber'] as String?,
        proofImageUrl: j['proofImageUrl'] as String?,
        payerName: j['payerName'] as String?,
        payerPhone: j['payerPhone'] as String?,
        createdAt: DateTime.parse(j['createdAt'] as String),
      );
}

class KitchenQueueItem {
  final String orderId;
  final String orderNumber;
  final String orderStatus;
  final DateTime? scheduledFor;
  final String? customerNotes;
  final String orderItemId;
  final String dishId;
  final String dishNameSnapshot;
  final int quantity;
  final String? itemNotes;
  final String kitchenStatus;
  final DateTime? prepStartedAt;
  final String? procedureMarkdown;
  final int prepTimeMinutes;
  final DateTime priorityTime;

  const KitchenQueueItem({
    required this.orderId,
    required this.orderNumber,
    required this.orderStatus,
    required this.scheduledFor,
    required this.customerNotes,
    required this.orderItemId,
    required this.dishId,
    required this.dishNameSnapshot,
    required this.quantity,
    required this.itemNotes,
    required this.kitchenStatus,
    required this.prepStartedAt,
    required this.procedureMarkdown,
    required this.prepTimeMinutes,
    required this.priorityTime,
  });

  factory KitchenQueueItem.fromJson(Map<String, dynamic> j) => KitchenQueueItem(
        orderId: j['orderId'] as String,
        orderNumber: j['orderNumber'] as String? ?? '',
        orderStatus: j['orderStatus'] as String,
        scheduledFor: _parseDate(j['scheduledFor']),
        customerNotes: j['customerNotes'] as String?,
        orderItemId: j['orderItemId'] as String,
        dishId: j['dishId'] as String,
        dishNameSnapshot: j['dishNameSnapshot'] as String? ?? '',
        quantity: (j['quantity'] as num?)?.toInt() ?? 0,
        itemNotes: j['itemNotes'] as String?,
        kitchenStatus: j['kitchenStatus'] as String? ?? 'pending',
        prepStartedAt: _parseDate(j['prepStartedAt']),
        procedureMarkdown: j['procedureMarkdown'] as String?,
        prepTimeMinutes: (j['prepTimeMinutes'] as num?)?.toInt() ?? 0,
        priorityTime: DateTime.parse(j['priorityTime'] as String),
      );
}

DateTime? _parseDate(dynamic v) {
  if (v is String && v.isNotEmpty) return DateTime.tryParse(v);
  return null;
}

class DishMarginRow {
  final String dishId;
  final String name;
  final double? sellingPriceUsd;
  final double totalCostUsd;
  final double grossProfitUsd;
  final double grossMarginPct;
  final double? priceToCostRatio;

  const DishMarginRow({
    required this.dishId,
    required this.name,
    required this.sellingPriceUsd,
    required this.totalCostUsd,
    required this.grossProfitUsd,
    required this.grossMarginPct,
    required this.priceToCostRatio,
  });

  factory DishMarginRow.fromJson(Map<String, dynamic> j) => DishMarginRow(
        dishId: j['dishId'] as String,
        name: j['name'] as String,
        sellingPriceUsd: (j['sellingPriceUsd'] as num?)?.toDouble(),
        totalCostUsd: (j['totalCostUsd'] as num).toDouble(),
        grossProfitUsd: (j['grossProfitUsd'] as num).toDouble(),
        grossMarginPct: (j['grossMarginPct'] as num).toDouble(),
        priceToCostRatio: (j['priceToCostRatio'] as num?)?.toDouble(),
      );
}

class ReorderSuggestionRow {
  final String ingredientId;
  final String name;
  final String useUnit;
  final double currentStockUseUnit;
  final double reorderPointUseUnit;
  final double minimumStockUseUnit;
  final double avgCostPerUseUnitUsd;
  final double avgDailyConsumption;
  final double? estimatedDaysUntilStockout;
  final String priority;     // critical | urgent | soon | ok

  const ReorderSuggestionRow({
    required this.ingredientId,
    required this.name,
    required this.useUnit,
    required this.currentStockUseUnit,
    required this.reorderPointUseUnit,
    required this.minimumStockUseUnit,
    required this.avgCostPerUseUnitUsd,
    required this.avgDailyConsumption,
    required this.estimatedDaysUntilStockout,
    required this.priority,
  });

  factory ReorderSuggestionRow.fromJson(Map<String, dynamic> j) =>
      ReorderSuggestionRow(
        ingredientId: j['ingredientId'] as String,
        name: j['name'] as String,
        useUnit: j['useUnit'] as String,
        currentStockUseUnit: (j['currentStockUseUnit'] as num).toDouble(),
        reorderPointUseUnit: (j['reorderPointUseUnit'] as num).toDouble(),
        minimumStockUseUnit: (j['minimumStockUseUnit'] as num).toDouble(),
        avgCostPerUseUnitUsd: (j['avgCostPerUseUnitUsd'] as num).toDouble(),
        avgDailyConsumption: (j['avgDailyConsumption'] as num).toDouble(),
        estimatedDaysUntilStockout:
            (j['estimatedDaysUntilStockout'] as num?)?.toDouble(),
        priority: j['priority'] as String,
      );
}

class SalesDailyRow {
  final DateTime saleDate;
  final int ordersCount;
  final double revenueUsd;
  final double grossProfitUsd;

  const SalesDailyRow({
    required this.saleDate,
    required this.ordersCount,
    required this.revenueUsd,
    required this.grossProfitUsd,
  });

  factory SalesDailyRow.fromJson(Map<String, dynamic> j) {
    final raw = j['saleDate'];
    final date = raw is String ? DateTime.parse(raw) : DateTime.now();
    return SalesDailyRow(
      saleDate: date,
      ordersCount: (j['ordersCount'] as num).toInt(),
      revenueUsd: (j['revenueUsd'] as num).toDouble(),
      grossProfitUsd: (j['grossProfitUsd'] as num).toDouble(),
    );
  }
}

class RecipeFullCostRow {
  final String recipeId;
  final String name;
  final bool isSubRecipe;
  final double totalCostUsd;

  const RecipeFullCostRow({
    required this.recipeId,
    required this.name,
    required this.isSubRecipe,
    required this.totalCostUsd,
  });

  factory RecipeFullCostRow.fromJson(Map<String, dynamic> j) => RecipeFullCostRow(
        recipeId: j['recipeId'] as String,
        name: j['name'] as String,
        isSubRecipe: j['isSubRecipe'] as bool? ?? false,
        totalCostUsd: (j['totalCostUsd'] as num).toDouble(),
      );
}

class MyReview {
  final String id;
  final String userId;
  final String orderId;
  final String dishId;
  final int rating;
  final String? comment;
  final bool isVisible;
  final DateTime createdAt;
  final DateTime updatedAt;

  const MyReview({
    required this.id,
    required this.userId,
    required this.orderId,
    required this.dishId,
    required this.rating,
    required this.comment,
    required this.isVisible,
    required this.createdAt,
    required this.updatedAt,
  });

  factory MyReview.fromJson(Map<String, dynamic> j) => MyReview(
        id: j['id'] as String,
        userId: j['userId'] as String,
        orderId: j['orderId'] as String,
        dishId: j['dishId'] as String,
        rating: (j['rating'] as num).toInt(),
        comment: j['comment'] as String?,
        isVisible: j['isVisible'] as bool? ?? true,
        createdAt: DateTime.parse(j['createdAt'] as String),
        updatedAt: DateTime.parse(j['updatedAt'] as String),
      );
}

// =====================================================================
// Ingredients (admin)
// =====================================================================

class IngredientSummary {
  final String id;
  final String name;
  final String useUnit;
  final double currentStockUseUnit;
  final double avgCostPerUseUnitUsd;
  final double reorderPointUseUnit;
  final double minimumStockUseUnit;
  final bool isActive;
  final bool isBelowReorderPoint;
  final bool isBelowMinimumStock;
  final bool isOutOfStock;

  const IngredientSummary({
    required this.id,
    required this.name,
    required this.useUnit,
    required this.currentStockUseUnit,
    required this.avgCostPerUseUnitUsd,
    required this.reorderPointUseUnit,
    required this.minimumStockUseUnit,
    required this.isActive,
    required this.isBelowReorderPoint,
    required this.isBelowMinimumStock,
    required this.isOutOfStock,
  });

  factory IngredientSummary.fromJson(Map<String, dynamic> j) => IngredientSummary(
        id: j['id'] as String,
        name: j['name'] as String,
        useUnit: j['useUnit'] as String? ?? '',
        currentStockUseUnit: (j['currentStockUseUnit'] as num?)?.toDouble() ?? 0,
        avgCostPerUseUnitUsd: (j['avgCostPerUseUnitUsd'] as num?)?.toDouble() ?? 0,
        reorderPointUseUnit: (j['reorderPointUseUnit'] as num?)?.toDouble() ?? 0,
        minimumStockUseUnit: (j['minimumStockUseUnit'] as num?)?.toDouble() ?? 0,
        isActive: j['isActive'] as bool? ?? true,
        isBelowReorderPoint: j['isBelowReorderPoint'] as bool? ?? false,
        isBelowMinimumStock: j['isBelowMinimumStock'] as bool? ?? false,
        isOutOfStock: j['isOutOfStock'] as bool? ?? false,
      );
}

class IngredientPresentation {
  final String id;
  final String name;
  final String purchaseUnit;
  final double purchaseQuantity;
  final double conversionToUseUnit;
  final double? lastPurchasePriceUsd;
  final bool isActive;

  const IngredientPresentation({
    required this.id,
    required this.name,
    required this.purchaseUnit,
    required this.purchaseQuantity,
    required this.conversionToUseUnit,
    required this.lastPurchasePriceUsd,
    required this.isActive,
  });

  factory IngredientPresentation.fromJson(Map<String, dynamic> j) =>
      IngredientPresentation(
        id: j['id'] as String,
        name: j['name'] as String,
        purchaseUnit: j['purchaseUnit'] as String,
        purchaseQuantity: (j['purchaseQuantity'] as num).toDouble(),
        conversionToUseUnit: (j['conversionToUseUnit'] as num).toDouble(),
        lastPurchasePriceUsd: (j['lastPurchasePriceUsd'] as num?)?.toDouble(),
        isActive: j['isActive'] as bool? ?? true,
      );
}

class IngredientDetail {
  final String id;
  final String name;
  final String? description;
  final String useUnit;
  final double currentStockUseUnit;
  final double reorderPointUseUnit;
  final double minimumStockUseUnit;
  final double avgCostPerUseUnitUsd;
  final bool isActive;
  final bool isBelowReorderPoint;
  final bool isBelowMinimumStock;
  final bool isOutOfStock;
  final List<IngredientPresentation> presentations;

  const IngredientDetail({
    required this.id,
    required this.name,
    required this.description,
    required this.useUnit,
    required this.currentStockUseUnit,
    required this.reorderPointUseUnit,
    required this.minimumStockUseUnit,
    required this.avgCostPerUseUnitUsd,
    required this.isActive,
    required this.isBelowReorderPoint,
    required this.isBelowMinimumStock,
    required this.isOutOfStock,
    required this.presentations,
  });

  factory IngredientDetail.fromJson(Map<String, dynamic> j) => IngredientDetail(
        id: j['id'] as String,
        name: j['name'] as String,
        description: j['description'] as String?,
        useUnit: j['useUnit'] as String? ?? '',
        currentStockUseUnit: (j['currentStockUseUnit'] as num?)?.toDouble() ?? 0,
        reorderPointUseUnit: (j['reorderPointUseUnit'] as num?)?.toDouble() ?? 0,
        minimumStockUseUnit: (j['minimumStockUseUnit'] as num?)?.toDouble() ?? 0,
        avgCostPerUseUnitUsd:
            (j['avgCostPerUseUnitUsd'] as num?)?.toDouble() ?? 0,
        isActive: j['isActive'] as bool? ?? true,
        isBelowReorderPoint: j['isBelowReorderPoint'] as bool? ?? false,
        isBelowMinimumStock: j['isBelowMinimumStock'] as bool? ?? false,
        isOutOfStock: j['isOutOfStock'] as bool? ?? false,
        presentations: (j['presentations'] as List<dynamic>? ?? const [])
            .map((p) => IngredientPresentation.fromJson(p as Map<String, dynamic>))
            .toList(growable: false),
      );
}

// =====================================================================
// Recipe cost (admin)
// =====================================================================

class RecipeCostLine {
  final String kind; // "ingredient" | "sub_recipe"
  final String refId;
  final String refName;
  final double quantity;
  final String unitLabel;
  final double unitCostUsd;
  final double lineCostUsd;
  final RecipeCost? subBreakdown;

  const RecipeCostLine({
    required this.kind,
    required this.refId,
    required this.refName,
    required this.quantity,
    required this.unitLabel,
    required this.unitCostUsd,
    required this.lineCostUsd,
    required this.subBreakdown,
  });

  factory RecipeCostLine.fromJson(Map<String, dynamic> j) => RecipeCostLine(
        kind: j['kind'] as String? ?? 'ingredient',
        refId: j['refId'] as String,
        refName: j['refName'] as String? ?? '',
        quantity: (j['quantity'] as num?)?.toDouble() ?? 0,
        unitLabel: j['unitLabel'] as String? ?? '',
        unitCostUsd: (j['unitCostUsd'] as num?)?.toDouble() ?? 0,
        lineCostUsd: (j['lineCostUsd'] as num?)?.toDouble() ?? 0,
        subBreakdown: j['subBreakdown'] is Map<String, dynamic>
            ? RecipeCost.fromJson(j['subBreakdown'] as Map<String, dynamic>)
            : null,
      );
}

class RecipeCost {
  final String recipeId;
  final String recipeName;
  final bool isSubRecipe;
  final double totalCostUsd;
  final double? yieldQuantity;
  final String? yieldUnit;
  final double? costPerYieldUnit;
  final List<RecipeCostLine> lines;

  const RecipeCost({
    required this.recipeId,
    required this.recipeName,
    required this.isSubRecipe,
    required this.totalCostUsd,
    required this.yieldQuantity,
    required this.yieldUnit,
    required this.costPerYieldUnit,
    required this.lines,
  });

  factory RecipeCost.fromJson(Map<String, dynamic> j) => RecipeCost(
        recipeId: j['recipeId'] as String,
        recipeName: j['recipeName'] as String? ?? '',
        isSubRecipe: j['isSubRecipe'] as bool? ?? false,
        totalCostUsd: (j['totalCostUsd'] as num?)?.toDouble() ?? 0,
        yieldQuantity: (j['yieldQuantity'] as num?)?.toDouble(),
        yieldUnit: j['yieldUnit'] as String?,
        costPerYieldUnit: (j['costPerYieldUnit'] as num?)?.toDouble(),
        lines: (j['lines'] as List<dynamic>? ?? const [])
            .map((l) => RecipeCostLine.fromJson(l as Map<String, dynamic>))
            .toList(growable: false),
      );
}

// =====================================================================
// Purchase forecast (admin)
// =====================================================================

class PurchaseForecastLine {
  final String ingredientId;
  final String ingredientName;
  final String useUnit;
  final double historicalConsumedUseUnit;
  final double dailyAverageUseUnit;
  final double projectedUseUnit;
  final double currentStockUseUnit;
  final double reorderPointUseUnit;
  final double suggestedPurchaseUseUnit;
  final double? lastPurchasePriceUsd;
  final double? avgCostPerUseUnitUsd;
  final double? estimatedCostUsd;

  const PurchaseForecastLine({
    required this.ingredientId,
    required this.ingredientName,
    required this.useUnit,
    required this.historicalConsumedUseUnit,
    required this.dailyAverageUseUnit,
    required this.projectedUseUnit,
    required this.currentStockUseUnit,
    required this.reorderPointUseUnit,
    required this.suggestedPurchaseUseUnit,
    required this.lastPurchasePriceUsd,
    required this.avgCostPerUseUnitUsd,
    required this.estimatedCostUsd,
  });

  factory PurchaseForecastLine.fromJson(Map<String, dynamic> j) =>
      PurchaseForecastLine(
        ingredientId: j['ingredientId'] as String,
        ingredientName: j['ingredientName'] as String? ?? '',
        useUnit: j['useUnit'] as String? ?? '',
        historicalConsumedUseUnit:
            (j['historicalConsumedUseUnit'] as num?)?.toDouble() ?? 0,
        dailyAverageUseUnit:
            (j['dailyAverageUseUnit'] as num?)?.toDouble() ?? 0,
        projectedUseUnit: (j['projectedUseUnit'] as num?)?.toDouble() ?? 0,
        currentStockUseUnit:
            (j['currentStockUseUnit'] as num?)?.toDouble() ?? 0,
        reorderPointUseUnit:
            (j['reorderPointUseUnit'] as num?)?.toDouble() ?? 0,
        suggestedPurchaseUseUnit:
            (j['suggestedPurchaseUseUnit'] as num?)?.toDouble() ?? 0,
        lastPurchasePriceUsd: (j['lastPurchasePriceUsd'] as num?)?.toDouble(),
        avgCostPerUseUnitUsd:
            (j['avgCostPerUseUnitUsd'] as num?)?.toDouble(),
        estimatedCostUsd: (j['estimatedCostUsd'] as num?)?.toDouble(),
      );
}

class PurchaseForecast {
  final DateTime historicalFrom;
  final DateTime historicalTo;
  final int historicalDays;
  final int targetDays;
  final double growthFactor;
  final int ordersAnalyzed;
  final List<PurchaseForecastLine> lines;

  const PurchaseForecast({
    required this.historicalFrom,
    required this.historicalTo,
    required this.historicalDays,
    required this.targetDays,
    required this.growthFactor,
    required this.ordersAnalyzed,
    required this.lines,
  });

  factory PurchaseForecast.fromJson(Map<String, dynamic> j) => PurchaseForecast(
        historicalFrom: DateTime.parse(j['historicalFrom'] as String),
        historicalTo: DateTime.parse(j['historicalTo'] as String),
        historicalDays: (j['historicalDays'] as num?)?.toInt() ?? 0,
        targetDays: (j['targetDays'] as num?)?.toInt() ?? 0,
        growthFactor: (j['growthFactor'] as num?)?.toDouble() ?? 1,
        ordersAnalyzed: (j['ordersAnalyzed'] as num?)?.toInt() ?? 0,
        lines: (j['lines'] as List<dynamic>? ?? const [])
            .map((l) => PurchaseForecastLine.fromJson(l as Map<String, dynamic>))
            .toList(growable: false),
      );

  /// Suma de los estimados de costo conocidos. Las lineas sin precio (avg
  /// cost null) no contribuyen al total.
  double get totalEstimatedCostUsd =>
      lines.fold(0.0, (acc, l) => acc + (l.estimatedCostUsd ?? 0));
}

class PublicReview {
  final String id;
  final String dishId;
  final int rating;
  final String? comment;
  final String customerDisplay;
  final DateTime createdAt;

  const PublicReview({
    required this.id,
    required this.dishId,
    required this.rating,
    required this.comment,
    required this.customerDisplay,
    required this.createdAt,
  });

  factory PublicReview.fromJson(Map<String, dynamic> j) => PublicReview(
        id: j['id'] as String,
        dishId: j['dishId'] as String,
        rating: (j['rating'] as num).toInt(),
        comment: j['comment'] as String?,
        customerDisplay: j['customerDisplay'] as String? ?? 'Cliente',
        createdAt: DateTime.parse(j['createdAt'] as String),
      );
}
