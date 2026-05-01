# Contexto del Proyecto: Sistema Integral "HomeChef Pro"

Actúa como Arquitecto de Software Senior. Debes desarrollar una aplicación para un negocio de cocina casera con lógica de costos compleja y gestión de inventario inteligente.

## 1. Requerimientos Técnicos Prioritarios
- **Conversión de Unidades:** El sistema debe permitir comprar en unidades grandes (ej. Saco de 50kg) y usar en unidades pequeñas (ej. 200g), realizando la conversión automática de costos.
- **DB Recursiva (Recetas Anidadas):** Implementar una estructura en PostgreSQL que permita que un Plato final contenga Ingredientes y Sub-recetas (ej. Salsas), calculando el costo total en cascada.
- **Algoritmo de Reabastecimiento:** Crear una lógica que analice el promedio de ventas diarias para predecir en cuántos días se agotará un ingrediente y generar una lista de compras sugerida.
- **Módulo de Tablet (Cocina):** Una vista simplificada para tablet donde el personal vea el "Procedimiento" paso a paso y marque platos como "En preparación" o "Listos para entrega".
- **Logística de Terceros:** Integración de campos para tracking de delivery externo (webhook para estados de pedido).

## 2. Stack Tecnológico
- Backend: C# con .NET 8 (Clean Architecture).
- DB: PostgreSQL + Entity Framework Core.
- Frontend: Flutter (Web y Mobile) con enfoque en UI alegre (colores cálidos) y botones grandes para uso en cocina.
- Pagos: Integración para verificación de pagos y emisión de recibos internos (PDF).

## 3. Instrucciones de Desarrollo
1. Define los modelos de datos en C# siguiendo la lógica de recetas recursivas.
2. Crea un servicio de "Calculadora de Costos" que sume el costo de ingredientes + el costo proporcional de las sub-recetas.
3. Diseña la API para el panel de cliente (pedidos) y el panel de administración (análisis de ganancias).
4. Implementa el módulo de "Predicción de Compras" basado en el historial de ventas.

Genera primero el esquema de la base de datos (SQL) y la estructura del proyecto en .NET 8.