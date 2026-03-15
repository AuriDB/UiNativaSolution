# Sistema Nativa — Backlog: Dudas, Mejoras y Deuda Técnica

> Este archivo recoge mejoras identificadas, dudas abiertas y deuda técnica para abordar en iteraciones futuras.

---

## Dudas Abiertas

| # | Duda | Prioridad | Contexto |
|---|---|---|---|
| D1 | ¿Debe el Dueño poder ver el número de pago en tránsito (transferencia bancaria)? | Media | CU26 solo muestra estado/monto |
| D2 | ¿Qué pasa si un Dueño actualiza su IBAN mientras hay un plan activo con pagos Pendientes? | Alta | `RegistrarIbanAsync` desactiva el anterior pero ya existen PagosMensuales con datos del plan |
| D3 | ¿Debe el Ingeniero poder cancelar una finca que tomó pero no puede evaluar (ej: finca mal registrada)? | Media | Actualmente solo puede Rechazar/Devolver |
| D4 | ¿Los adjuntos se listan con SAS token (para seguridad) o URL pública? | Alta | `BlobService` devuelve URL directa; en producción debe ser SAS token |
| D5 | ¿Cuánto tiempo se conservan los `OtpSesion` usados/expirados? | Baja | Actualmente no hay limpieza; podrían acumularse |
| D6 | ¿El Admin puede reactivar manualmente a un Dueño bloqueado por OTP? | Alta | Solo se menciona en spec pero no implementado en CU06 |

---

## Mejoras de Seguridad

| # | Mejora | Esfuerzo | Notas |
|---|---|---|---|
| S1 | **SAS tokens para adjuntos** — URL de blob con expiración en lugar de URL pública | Medio | `BlobServiceClient.GenerateSasUri()` |
| S2 | **Rate limiting** en endpoints `/Auth/*` — prevenir ataques de fuerza bruta | Bajo | `Microsoft.AspNetCore.RateLimiting` |
| S3 | **Logging de auditoría** — registrar cambios en Sujeto, Estado de finca, Pagos | Alto | Nueva tabla `AuditLog` |
| S4 | **CSP headers** — Content Security Policy para prevenir XSS | Bajo | Middleware personalizado |
| S5 | **MimeKit 4.12.0** tiene vulnerabilidad moderada conocida — actualizar cuando salga fix | Medio | `GHSA-g7hc-96xr-gvvx` |
| S6 | **HMAC secret** en `appsettings.json` debe estar en variable de entorno o Azure Key Vault en producción | Alto | Actualmente es placeholder |

---

## Mejoras Funcionales

| # | Mejora | Prioridad | Descripción |
|---|---|---|---|
| F1 | **Notificaciones in-app (toast)** en Dashboard al entrar — fincas con cambio de estado | Baja | SignalR o polling |
| F2 | **Filtros en Cola FIFO** — por rango de hectáreas, vegetación, región | Media | Ag-Grid built-in, no requiere backend |
| F3 | **Exportar reporte PDF de historial pagos** — QuestPDF | Media | Ya tenemos QuestPDF en paquetes |
| F4 | **Mapa en Detalle Finca** — mostrar pin Leaflet read-only | Baja | Reutilizar lógica de nueva-finca.js |
| F5 | **Dashboard con datos reales** — KPIs de fincas, pagos, estados | Alta | Home/Index aún usa placeholder |
| F6 | **Paginación en historial de adjuntos** — si hay muchos archivos | Baja | Actualmente lista completa |
| F7 | **Validación de formato cédula en UI** — regex Costa Rica | Baja | Solo `maxlength=12` actualmente |
| F8 | **Dark mode** — toggle Bootstrap | Baja | Cosmético |

---

## Deuda Técnica

| # | Deuda | Prioridad | Descripción |
|---|---|---|---|
| T1 | **Dashboard Home/Index.cshtml** usa Session (legado) — migrar a User claims | Alta | Ya es placeholder; limpiar antes de producción |
| T2 | **Controllers Auth/Login y Register (viejos)** — redirigen a `/Auth/*` pero no se eliminaron | Media | Son redundantes; podrían confundir |
| T3 | **BlobService dev-mode** retorna URL simulada `https://fake-blob.azure.com/...` | Alta | OK para dev, debe desactivarse en producción |
| T4 | **ExternalApiService** ignora silenciosamente errores — mejorar logging estructurado | Media | `_logger.LogWarning(...)` mínimo |
| T5 | **No hay GlobalExceptionHandler** — excepciones no manejadas devuelven stack trace en dev | Media | Agregar `app.UseExceptionHandler` con JSON response |
| T6 | **PagoHostedService** — `PeriodicTimer` sin health check; si muere silenciosamente no hay alerta | Alta | Agregar `/health` endpoint |
| T7 | **Limpieza de OtpSesion** expiradas — no hay job de limpieza | Baja | Podrían acumularse en producción |
| T8 | **`appsettings.json` contiene placeholders** — `AZURE_CONN_STRING`, `TU_KEY`, etc. | Crítico | Variables de entorno o secrets manager en producción |

---

## Ideas para Versión 2.0

- Portal público con mapa de todas las fincas aprobadas
- Integración con SINAC (Sistema Nacional de Áreas de Conservación)
- App móvil para Dueños con notificaciones push
- Chatbot de soporte con IA para preguntas frecuentes
- Exportación de reportes Excel (NPOI / EPPlus)
- Multi-idioma (español / inglés)
- Integración con BCCR (API tipo de cambio) para mostrar equivalente en USD
