# Premortem — HomeChef Pro

**Fecha de ejecución:** 2026-05-03
**Skill usada:** `premortem` (`docs/skills/premortem/SKILL.md`)
**Frame:** Es 3 de noviembre de 2026. HomeChef Pro fracasó. El chef piloto que iba a estar en producción real no quedó, o abandonó al mes y medio. La idea de empaquetar como SaaS para 3-10 chefs nunca se concretó. El repo está estancado en `main` desde julio.

---

## 1. Contexto recogido

**Qué es:** HomeChef Pro — sistema integral para negocio de cocina casera en Venezuela. Backend .NET 10 Clean Architecture (Domain / Application / Infrastructure / Api), Postgres 16 (snake_case raw SQL, sin EF Migrations, `EnsureCreated` ad-hoc para Identity), Redis 7, 3 apps Flutter 3.41 (admin_web 8090, client_app 8091, kitchen_tablet 8092), Docker Compose + nginx + certbot.

**Features completas (al 2026-05-03):**
- Auth: register, login, refresh, logout, change-password, /me, JWT con rotación.
- Catálogo público: menú, dish detail, reviews.
- Orders: guest + registered, pago manual con comprobante (Pago Móvil VES, Zelle, transferencia), kitchen flow, delivery webhooks (HMAC verificado), receipt PDF (QuestPDF).
- Invoicing: SENIAT/IGTF, mock provider en dev.
- Inventario: ingredientes, presentaciones, compras (trigger actualiza stock + avg cost), waste.
- Compras: forecast con consumo histórico.
- Reportes: dish-margin, recipe-costs, reorder-suggestions, sales-daily, inventory-rotation, peak-hours-heatmap, peak-hours-summary, customer-ranking (RFM).
- Sabor (loyalty): trigger acredita al `delivered`, niveles bronce/plata/oro, catálogo de rewards, redeem. UI cliente completa.
- HTTPS con certbot configurado (no desplegado todavía).

**Para quién:** Chef casero venezolano (cliente del negocio) + clientes finales (consumidores) + personal de cocina (tablet) + delivery de terceros.

**Éxito a 6 meses:** (a) chef piloto en producción real + (b) base lista para vender SaaS a 3-10 chefs (combinación de las opciones 1 y 2 elegidas por el usuario).

**Timeline:** 1-3 meses para el lanzamiento. Features pendientes según `INTEGRACION.md`: panel admin de Sabor (CRUD rewards, transacciones), notificaciones push, sistema de cupones, caching Redis efectivo, dashboard ejecutivo, deploy real a producción.

**Owner:** jacques. Único developer.

---

## 2. Razones de fallo (raw)

1. **Multi-tenancy ausente bloqueó el SaaS.** El código es single-tenant; refactorizarlo (data isolation, billing por cliente, onboarding self-serve) no cupo en 1-3 meses.
2. **Nunca hubo un chef piloto comprometido y on-boarded.** El sistema se construyó sobre asunciones, no sobre un flujo real validado.
3. **Pago manual con verificación humana no escaló más allá del primer cliente.** Admin quemado, pedidos colgados 30+ minutos, comida lista en cocina antes de que el pago fuera aprobado.
4. **El deploy real nunca se ejecutó end-to-end.** Issues de DNS, firewall, certbot rate limits, CORS, env vars en producción cayeron todos en la noche del lanzamiento.
5. **Sin observabilidad, sin backups probados, sin runbook.** Primer crash de producción se llevó al chef piloto.
6. **Seguridad nunca se auditó pese a manejar datos sensibles** (capturas con info bancaria, RIF, direcciones). Un IDOR / leak / path-traversal mató la reputación SaaS.
7. **Flutter CanvasKit: cliente no apareció en Google + tests UI skipped.** Adquisición orgánica nula y regresiones silenciosas.
8. **Stack regulatorio Venezuela hard-coded** (SENIAT, IGTF, Pago Móvil VES). Limitó el TAM, y un cambio normativo SENIAT en septiembre rompió la facturación.

---

## 3. Deep-dives (8 agentes en paralelo)

### Modo #1 — Multi-tenancy ausente

**Historia.** En junio 2026, jacques cerró al primer chef piloto (Doña Carmen, parrilla en Las Mercedes) y desplegó la instancia única en un VPS de Hetzner con su Docker Compose: postgres + redis + api + nginx + certbot, todo apuntando a `carmen.homechef.pro`. Funcionó. Para agosto ya tenía dos chefs más interesados tras una demo en una feria gastronómica en Caracas. La segunda chef (Andreína, repostería) firmó carta de intención, pero cuando jacques le explicó que su instancia sería un VPS separado, con su propio postgres, su propio dominio, su propio certbot y su propio deploy manual, ella pidió ver cómo se gestionaba la facturación consolidada y el panel multi-chef. No existía.

jacques abrió el repo y midió el daño: cero columna `tenant_id` en las 40+ tablas de `src/database/schema/`, cero filtro en los repositorios de Application, los `asp_net_users` compartidos sin scope de organización, los reportes (RFM, peak hours, margen) hechos con vistas SQL planas en `10_views.sql` que asumen una sola org, el módulo Sabor recién terminado escrito sobre la misma premisa. En septiembre intentó la ruta "schema-per-tenant" en PostgreSQL, pero los triggers de `11_functions_triggers.sql` y los casts a `numeric(N,M)` se replicaban mal entre schemas y el bootstrap de Identity (`EnsureCreated` ad-hoc) chocaba. En octubre pivotó a "una BD por chef en VPS dedicado" y cobró $80/mes por chef solo para cubrir infra: Andreína se fue a un competidor con onboarding self-serve, el tercer prospecto nunca firmó, y con un solo cliente pagando $80 menos $45 de VPS, el SaaS no era SaaS — era consultoría disfrazada.

**Supuesto oculto:** "Tener un chef en producción" y "vender a 10 chefs" eran el mismo producto con distinta cantidad de instancias.

**Señales tempranas:**
- En el repo: `grep -r "tenant_id\|organization_id" src/` devuelve cero resultados después de 6 meses, y `01a_identity_tables.sql` no tiene FK hacia ninguna tabla `organizations`.
- En la conversación de venta con Andreína (agosto): la pregunta "¿puedo ver el dashboard de mis ventas sin que Carmen vea las mías?" se respondió con "te monto un servidor aparte" en lugar de "claro, te creo el tenant" — fricción de onboarding medida en horas de jacques, no en clics del cliente.

---

### Modo #2 — Sin chef piloto comprometido (más probable)

**Historia.** Jacques arrancó en abril 2026 con un brief mental claro: "un chef casero necesita catálogo, órdenes, pagos y cocina". Sin chef real al lado, ese brief se llenó con su propia imaginación de SaaS. Para junio ya tenía orders + pagos manuales funcionando, y en lugar de salir a la calle a sentar a un chef frente al admin web, abrió el siguiente ticket: facturación SENIAT con IGTF y mock provider. Para agosto, inventario con conversión de unidades y forecast de compras basado en consumo histórico — features que asumen que el chef ya tiene SKUs normalizados y un histórico limpio, cosa que ningún chef casero venezolano tiene. Para octubre el módulo Sabor tenía bronce/plata/oro, 12 endpoints y vistas de RFM, pero ningún chef había pedido loyalty antes de pedir "que mi esposa pueda anotar el pedido por WhatsApp". El peak hours heatmap se construyó con datos sintéticos del seeder.

Cuando en octubre Jacques finalmente contactó a una chef real (Carmen, tequeños y pasticho por encargo), el desencaje fue inmediato: Carmen cobra a fin de mes por Pago Móvil sin comprobante (confianza), no usa tablet en cocina (cocina sola, lee del teléfono), sus "recetas" son memoria muscular sin gramos exactos, y sus "ingredientes" son "queso del mercado" no un SKU con factor de conversión. El kitchen_tablet en 8092 sobraba. Faltaba: ingreso de pedido por voz/WhatsApp, lista de compras imprimible para el mercado, y un modo "fiado" para clientes recurrentes.

**Supuesto oculto:** Jacques asumió que el problema del chef casero era el mismo que resuelven los SaaS de restaurantes que él ya conocía, solo que más chico.

**Señales tempranas:**
- `scripts/seed-purchases.ps1` existe precisamente porque los reportes no tenían datos reales — se inventaron compras sintéticas para que las vistas analíticas (margen, rotación) "se vieran bien" en demos sin usuario.
- 37/37 integration tests verde sobre un flujo end-to-end (`smoke-deep.ps1`, 10 pasos) que nunca fue caminado por un chef: la cobertura mide consistencia interna, no encaje con la realidad.

---

### Modo #3 — Pago manual no escala

**Historia.** Junio 2026. El chef piloto, Andrés, llega a 45 pedidos en un sábado de almuerzo concentrados entre las 11:30 y las 13:30. Cada comprobante toma 90 segundos en promedio (abrir imagen, leer referencia, cruzar con SMS del banco, aprobar). 45 × 90s = 67 minutos de trabajo administrativo puro, pero comprimidos en una ventana de 120 minutos donde Andrés también está cocinando. Resultado: el pago número 30 se aprueba 38 minutos después de subido. Para entonces, el cliente ya canceló por WhatsApp y dejó reseña de una estrella. Peor aún: 12 pedidos se aprobaron tarde, llegaron a la kitchen tablet a las 13:50, cuando el almuerzo ya cerró, y la cocina los preparó igual porque el sistema no distinguía. Comida desperdiciada, devoluciones manuales.

En agosto vendimos al segundo chef, Marisol. Ella delega la verificación a su asistente, pero el asistente confunde montos en bolívares vs dólares (tasa BCV cambia diario), aprueba 4 pagos falsificados en una semana. Pérdida directa: 180 USD. En septiembre, Andrés se va de vacaciones tres días; sin backup, los pedidos se acumulan, 60% se cancelan, los clientes migran al competidor de Instagram que cobra por Pago Móvil con confirmación SMS automática. Para octubre, ningún chef renueva la suscripción mensual.

**Supuesto oculto:** Jacques asumió que el admin (chef) tendría disponibilidad humana sincrónica de 8-12 horas diarias para revisar comprobantes con latencia menor a 2 minutos, sin enfermedades, vacaciones, ni picos concurrentes con la cocina.

**Señales tempranas:**
- Latencia de aprobación P95 > 10 minutos durante hora pico (medible con timestamp `payment_uploaded_at` vs `payment_approved_at` en `payments`).
- Tasa de cancelación por timeout > 8% y reseñas mencionando "tardaron en confirmar mi pago" o "ya tenía hambre cuando aprobaron"; pedidos que llegan a `kitchen_tablet` con `created_at` posterior al cierre de servicio.

---

### Modo #4 — Deploy nunca ejecutado end-to-end

**Historia.** Lunes 19 de octubre de 2026, 22:40. Jacques apunta el A record de `homechef.app` al VPS de Hetzner y ejecuta `init-letsencrypt.sh` por primera vez en producción. El script falla en silencio porque el contenedor `nginx` arranca antes de que certbot tenga el challenge listo: el `webroot` está vacío. Reintenta cinco veces cambiando el `server_name`. A las 23:15, Let's Encrypt devuelve `too many certificates already issued for exact set of domains: homechef.app` — golpeó el rate limit de 5 certificados por dominio registrado por semana. Decide usar `--staging`, pero los navegadores rechazan el cert; el chef piloto, Andrés, no puede abrir `admin_web`.

Mientras tanto, la API levanta pero devuelve 500 en cada request. En logs: `Npgsql.NpgsqlException: Failed to connect to ::1:5432`. La cadena de conexión apunta a `localhost` porque `appsettings.Production.json` quedó con el valor de desarrollo; en Docker Compose el host correcto es `postgres` (alias del servicio). Jacques corrige y reinicia, pero ahora el JWT firma con una clave distinta a la de staging porque `JWT_KEY` no está en `deploy/.env`, sino hardcodeada en `appsettings.json` — los tokens emitidos antes invalidan, los tres dispositivos del chef quedan deslogueados en loop. A las 02:30, el navegador de Andrés bloquea `admin_web` con `CORS policy: No 'Access-Control-Allow-Origin'` porque la lista blanca solo contiene `localhost:8090`.

Tres días de degradación. Andrés recibe 14 pedidos por WhatsApp manual, pierde 4 por confusión de pagos. Cuando el sistema estabiliza el viernes, Andrés ya decidió que "esto no está listo" y vuelve a su cuaderno. El piloto nunca se reactiva.

**Supuesto oculto:** Que un deploy documentado en scripts equivale a un deploy probado end-to-end.

**Señales tempranas:**
- No existe `deploy/.env.production.example` ni `docs/RUNBOOK.md`; toda la configuración de producción vive en la cabeza de jacques.
- `init-letsencrypt.sh` nunca se ejecutó contra un dominio real de staging — no hay registro de un dry-run con `--staging` antes del lanzamiento.

---

### Modo #5 — Sin observabilidad ni backups

**Historia.** Es la noche del jueves 12 de noviembre. El chef piloto cierra cocina a las 23:00 después de un servicio de 18 órdenes. A las 02:40 de la madrugada, los logs JSON de Docker (que nadie rotó porque no hay `max-size` en el `daemon.json`) terminan de llenar `/var/lib/docker` en el VPS de 40 GB. Postgres detecta el disco lleno, entra en modo solo-lectura y empieza a rechazar `INSERT`. Redis sigue vivo, nginx también, así que la home carga; pero cualquier intento de crear orden devuelve 500.

Viernes 11:30, el chef abre el menú para el almuerzo. Los primeros tres clientes se quejan por WhatsApp: "no me toma el pago". El chef recarga, prueba en otro celular, ve el 500 y llama a jacques. Jacques no contesta hasta las 13:10 (estaba durmiendo después de programar hasta tarde, y nunca configuró Healthchecks.io ni Sentry). Entra por SSH, ve `df -h` al 100%, hace `docker system prune -af` a ciegas, postgres vuelve, pero el `wal` de las últimas horas quedó sucio. Pide el último backup: no hay `pg_dump` automatizado, solo un dump manual de hace tres semanas. No existe `RUNBOOK.md`, así que jacques improvisa por dos horas más.

Saldo del día: 12 órdenes perdidas (~200 USD), seis clientes regulares migrados a la competencia, y el chef enviando un audio de cinco minutos terminando el contrato piloto. Sin caso de éxito, no hay SaaS vendible.

**Supuesto oculto:** Jacques creía que un VPS bien configurado "se cuida solo" hasta tener clientes pagando que justifiquen invertir en observabilidad.

**Señales tempranas:**
- `grep -r "pg_dump\|restic\|backup" deploy/ scripts/` no devuelve nada programado, y el `docker-compose.yml` no define `logging.options.max-size`.
- No existe `docs/RUNBOOK.md` ni un ping externo (Healthchecks.io, UptimeRobot) apuntando a `/health/db`; el único monitoreo es abrir el navegador.

---

### Modo #6 — Seguridad sin audit (más peligroso)

**Historia.** Septiembre 2026. María, cliente registrada del chef piloto, hace una orden y sube su comprobante de Pago Móvil vía `POST /api/orders/{id}/payment-proof`. La imagen se guarda en `wwwroot/uploads/payment-proofs/` con nombre predecible (`{orderId}_{timestamp}.jpg`) y se sirve estáticamente por nginx sin pasar por el pipeline de auth de la API. María inspecciona la respuesta del endpoint, nota la URL `https://homechef.local/uploads/payment-proofs/142_20260912.jpg`, y prueba decrementar el ID: `141`, `140`, `139`. Cada uno devuelve 200 OK con el comprobante de otro cliente — número de cuenta, banco emisor, cédula visible en la captura.

Sin detenerse, María encuentra que `GET /api/admin/orders/{id}` solo valida `[Authorize(Roles="Admin")]` pero no verifica `chef_id` del admin contra el `chef_id` de la orden (falla de multi-tenancy en `OrdersController` de Application). Combinado con un JWT_KEY débil hardcoded en `appsettings.json` (visible en `git log -p`), forja un token de admin con `chef_id` arbitrario y descarga la lista completa de órdenes con sus URLs de comprobantes. Exporta 340 capturas con datos bancarios.

María publica un thread en X con redacciones parciales: "HomeChef Pro filtra tu cuenta bancaria. PoC: cambiar un número en la URL." Hilo viral en 6 horas. El chef piloto recibe llamadas de clientes furiosos, cancela el contrato esa misma noche. SUDEBAN abre expediente. El segundo chef que estaba en negociación se retira. Proyecto muerto.

**Supuesto oculto:** Asumimos que "está detrás de auth" equivale a "está seguro", sin verificar autorización a nivel de recurso ni controlar el path de servido estático.

**Señales tempranas:**
- La skill `security-audit` existe en `.claude/skills/` pero nunca se ejecutó; no hay tests de integración con nombres tipo `Should_Reject_CrossChef_OrderAccess` ni `Should_Deny_DirectUploadUrlWithoutAuth`.
- `grep -r "JWT_KEY\|Jwt:Key" appsettings*.json` devuelve un valor literal commiteado, y nginx sirve `/uploads/` como `alias` sin `auth_request` hacia la API.

---

### Modo #7 — Flutter CanvasKit (SEO + tests UI)

**Historia.** En mayo, jacques lanzó la beta con el chef piloto Andrea, prometiéndole dos cosas: que sus clientes la encontrarían buscando "almuerzos caseros Caracas" en Google, y que la app sería estable porque "Flutter es un framework maduro". Para julio, Andrea reportó que ni siquiera su propio nombre la encontraba: el `client_app` servía un `<canvas>` opaco y Googlebot solo veía un `<body>` vacío con un loader. Andrea tuvo que gastar 180 USD/mes en Instagram Ads y reenviar el link por WhatsApp uno por uno. El pitch del SaaS a otros tres chefs candidatos murió en la primera demo: ninguno aceptó pagar mensualidad por una plataforma invisible en buscadores.

En septiembre, jacques agregó el campo "referencia bancaria" a `OrderForm` para mejorar la conciliación de Pago Móvil. Los specs de Playwright `04-admin-login-ui` y `05-client-catalog-ui` seguían marcados como skipped desde abril porque CanvasKit hacía imposible leer el DOM. El cambio rompió el binding del botón "Pagar" en mobile web, pero pasó CI sin alertas. Durante 24 horas, Andrea recibió reviews de clientes diciendo "no me deja pagar" antes de notarlo ella misma. Perdió 30 órdenes (~450 USD) y la confianza del barrio. Para noviembre, Andrea volvió a vender por status de WhatsApp y el repo de HomeChef Pro quedó archivado.

**Supuesto oculto:** Jacques asumió que Flutter web con CanvasKit era equivalente funcional a una SPA HTML para SEO y testabilidad, sin verificar que el renderer de canvas excluye al producto de toda la cadena de descubrimiento orgánico y validación automatizada de UI.

**Señales tempranas:**
- `tests/e2e/specs/04-admin-login-ui.spec.ts` y `05-client-catalog-ui.spec.ts` con `test.skip` y comentario "CanvasKit no expone DOM" — la deuda ya está declarada por escrito.
- `client_app/web/index.html` sin `<meta name="description">`, sin Open Graph, sin `<title>` dinámico; ausencia de `robots.txt` y `sitemap.xml` en la raíz servida por nginx.

---

### Modo #8 — Stack regulatorio Venezuela

**Historia.** En septiembre de 2026, el SENIAT publicó la Providencia Administrativa SNAT/2026/000071, que ajustó el formato XML de la factura electrónica (nuevo bloque `<RetencionIGTF>` obligatorio y cambio en la tasa IGTF del 3% al 2,5% para ciertos medios de pago). El módulo de facturación de HomeChef Pro tenía la tasa hardcoded como `const decimal IGTF_RATE = 0.03m` en `InvoiceService`, y el XML serializer estaba acoplado al esquema viejo. La integración real con TheFactoryHKA empezó a rechazar facturas con error de validación. Durante dos semanas, el chef piloto emitió comprobantes fuera de norma, quedando expuesto a multas y al rechazo del crédito fiscal por parte de sus clientes corporativos. Recuperar la confianza del piloto tomó otro mes, y la prensa local del nicho gastronómico se enteró.

En octubre, Andrés desde Bogotá quiso licenciar el sistema para su servicio de viandas. Jacques abrió `InvoiceService.cs`, `IgtfCalculator.cs` y el schema SQL: encontró referencias a SENIAT, IGTF y RIF dispersas en 14 archivos, sin ninguna abstracción de proveedor fiscal. Estimó dos a tres meses solo para reescribir hacia DIAN (factura electrónica con CUFE, sin IGTF, con IVA del 19%). Andrés se fue a una solución colombiana ya certificada. Sin clientes fuera de Venezuela y con un piloto golpeado, el runway no aguantó.

**Supuesto oculto:** Jacques creyó que la normativa SENIAT sería estable durante el piloto y que el TAM venezolano alcanzaba para validar el SaaS antes de internacionalizar.

**Señales tempranas:**
- Constantes como `IGTF_RATE`, `RIF`, `SENIAT_*` repartidas en código sin una interfaz `ITaxProvider` / `IInvoiceProvider` que permita inyectar régimen por país o tenant.
- Un único `invoice` schema en SQL con columnas específicas de Venezuela (`igtf_amount`, `rif_emisor`, `numero_control`) sin discriminador de región ni tabla `tax_regime`.

---

## 4. Síntesis

### Fallo más probable
**#2 — Sin chef piloto comprometido.** Es la raíz: las features se construyen sobre asunciones, no sobre flujo real. El seed sintético de compras y los 37/37 tests verde sobre un flujo nunca caminado por un chef son las dos huellas más claras. Si esto no se cierra primero, los otros modos se vuelven secundarios.

### Fallo más peligroso
**#6 — Seguridad sin audit.** Datos sensibles (comprobantes con info bancaria, RIF, direcciones) no admiten un fallo. Un IDOR o URL predecible de uploads destruye la reputación SaaS antes del segundo cliente, con riesgo regulatorio (SUDEBAN) y posibilidad de quedar listed en hilos virales en X. No es recuperable.

### Supuesto oculto raíz
**Construir bien el sistema = tener un producto que vende y opera.** Los detalles operacionales (deploy real, observabilidad, pago automatizable, seguridad auditada, regulación, multi-tenancy) están siendo tratados como "ya habrá tiempo", pero a partir del primer cliente ESOS son el producto, no las features de catálogo o reportes.

### Plan revisado (acciones concretas)

1. **Conseguir chef piloto antes de seguir construyendo features.** 5 conversaciones presenciales en 2 semanas. Llevar laptop con admin_web. Si en 2 semanas no hay carta de intención de un chef para usar el sistema 30 días en exclusiva, el plan se replantea (no se sigue codeando).
2. **Probar deploy end-to-end en staging real ANTES del lanzamiento.** Dominio barato (`homechef-staging.app`), VPS Hetzner $5/mes, correr `init-letsencrypt.sh` con flag `--staging` de certbot, smoke-deep contra el staging. Documentar issues encontrados en `docs/RUNBOOK.md`.
3. **Cerrar pago automatizado mínimo o mitigar.** Si Pago Móvil tiene confirmación SMS, integrar. Si no: timeout 15 min auto-cancel, webhook a WhatsApp del admin al recibir comprobante, lista blanca de bancos emisores, validación de monto exacto. Antes del segundo cliente.
4. **Correr la skill `security-audit` esta semana.** Mínimos: `JWT_KEY` a env var, tests `Should_Reject_CrossChef_OrderAccess`, proteger `/uploads/` con `auth_request` en nginx, no servir static directo. Antes del deploy real.
5. **Backup + observabilidad mínimos.** Cron diario de `pg_dump` a Backblaze B2 ($5/TB). Healthchecks.io ping cada 5 min a `/health/db`. UptimeRobot con email. `logging.options.max-size: "10m"` en `docker-compose.yml`. Antes del cliente real.
6. **Decidir multi-tenancy AHORA, no después.** Dos caminos: (a) agregar `org_id` a tablas de dominio + filtros en queries y middleware; (b) confirmar que la estrategia es "una instancia por chef" con precio que cubra ($60-80 mes) y aceptarlo como consultoría, no SaaS. Sin decisión, los próximos 3 meses son trabajo perdido.
7. **Resolver SEO del client_app.** Landing en Astro/Next.js con menú indexable, link "Ordenar" al cliente Flutter. `robots.txt` y `sitemap.xml` en nginx. Meta tags Open Graph en `index.html`.
8. **Aislar Venezuela detrás de `ITaxProvider`.** Sin reescribir, solo extraer interfaz; las constantes `IGTF_RATE` y el XML SENIAT a una sola clase. Permite que "DIAN futuro" no sea un bloqueador conceptual.

### Pre-launch checklist (verificar antes de prod)

1. **Carta firmada del chef piloto** (incluso screenshot WhatsApp) comprometiéndose a usar el sistema 30 días en exclusiva.
2. **Deploy a staging real con dominio real** completado. `smoke-deep.ps1` verde contra staging.
3. **Skill `security-audit` ejecutada** y findings críticos cerrados (`JWT_KEY` a env var, test IDOR multi-org, uploads protegidos).
4. **Backup automatizado funcional** con prueba de restore real, más `docs/RUNBOOK.md` con los 3 incidentes más probables y cómo recuperar.
5. **Plan de pago mitigado**: integración SMS, o timeout auto-cancel + reglas anti-fraude + humano backup entrenado.

---

*Generado por la skill `premortem` (`docs/skills/premortem/SKILL.md`).*
