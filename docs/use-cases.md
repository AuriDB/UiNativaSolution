# Sistema Nativa — Casos de Uso Detallados

---

## CU01 — Registrar Dueño + OTP

**Actor:** Dueño (público)
**Flujo principal:**
1. Usuario llena form 3 pasos (datos personales → contraseña → resumen)
2. POST `/Auth/Register` → `AuthService.RegistrarAsync`
3. Validación contraseña (doble: JS + C#): min 6, mayúscula, número, especial
4. Normalización cédula: strip guiones, 9 dígitos, formato `1-XXXX-XXXX`
5. Sujeto creado con `Estado = Inactivo`
6. OTP 6 dígitos generado → BCrypt → `OtpSesion` con TTL 90s
7. Email N03 enviado con OTP
8. Redirect → `/Auth/VerificarOtp`

**Flujo alternativo — Fallos OTP:**
- 3 fallos → `Estado = Bloqueado` → email N03 → solo Admin reactiva
- TTL expirado → error 400
- Cooldown reenvío: 30s entre intentos, máx 3 en 5 min

**Postcondición:** `Sujeto.Estado = Activo`, cookie `nativa_auth` establecida

---

## CU02 — Iniciar Sesión

**Actor:** Todos los roles
**Endpoint:** `POST /Auth/Login`
**Validaciones:** BCrypt verify, Estado = Activo (no Inactivo/Bloqueado)
**Claims:** `NameIdentifier` (userId), `Name`, `Email`, `Role`
**Cookie:** `nativa_auth`, 8h, HttpOnly, SameSite=Strict, SecurePolicy=Always

---

## CU04 — Recuperar Contraseña

**Actor:** Todos
**Endpoints:** `POST /Auth/Forgot` + `POST /Auth/Reset`
**Token:** `Base64(sujetoId|expiry.Ticks)` + `.` + `Base64(HMAC-SHA256)`
**Reglas OWASP:** respuesta siempre genérica (no revelar si correo existe)
**TTL:** 15 minutos, un solo uso
**Caducado/usado:** HTTP 410 Gone

---

## CU11 — Registrar Finca

**Actor:** Dueño autenticado
**Endpoint:** `POST /Dueno/Fincas/Nueva` (multipart/form-data)
**Campos:** hectáreas, vegetación%, hidrología%, topografía, esNacional, lat, lng
**Adjuntos:** extensiones jpg/jpeg/png/pdf/dwg, máx 10 MB c/u → Azure Blob `psa-docs`
**Postcondición:** `Activo.Estado = Pendiente`, `FechaRegistro = UTC now`

---

## CU12 — Ver mis Fincas

**Actor:** Dueño
**Endpoint:** `GET /Dueno/Fincas/Data` → JSON para Ag-Grid
**Incluye:** id, hectáreas, vegetación, hidrología, topografía, esNacional, fechaRegistro, estado, rowVersion(base64)

---

## CU13 — Detalle Finca

**Actor:** Dueño
**Endpoint:** `GET /Dueno/Fincas/{id}`
**Seguridad:** verifica `IdDueno == UserId` en consulta
**Incluye:** datos técnicos + observaciones + lista adjuntos (lazy via `/Adjuntos`)

---

## CU14 — Actualizar Finca

**Actor:** Dueño
**Endpoint:** `POST /Dueno/Fincas/Editar/{id}`
**Restricción:** solo si `Estado == Devuelta`
**No permite:** editar mientras `EnRevision`

---

## CU15 — Reenviar a Evaluación

**Actor:** Dueño
**Endpoint:** `POST /Dueno/Fincas/Reenviar/{id}`
**Acción:** `Estado = Pendiente`, `FechaRegistro = UTC now` (nuevo timestamp FIFO)
**Restricción:** solo si `Estado == Devuelta`

---

## CU17 — Ver Cola FIFO

**Actor:** Ingeniero
**Endpoint:** `GET /Ingeniero/Cola/Data`
**Query:** `Activos WHERE Estado=Pendiente ORDER BY FechaRegistro ASC`
**Expone RowVersion** en base64 para CU18

---

## CU18 — Tomar Finca (Concurrencia Optimista)

**Actor:** Ingeniero
**Endpoint:** `POST /Ingeniero/Cola/Tomar/{id}`
**Mecanismo:** `_db.Entry(finca).Property(f => f.RowVersion).OriginalValue = rv`
**Conflicto:** `DbUpdateConcurrencyException` → HTTP 409 → UI muestra alerta y refresca cola
**Postcondición:** `Estado = EnRevision`, `IdIngeniero = ingenieroId`, email N05 al Dueño

---

## CU19 — Evaluar Finca

**Actor:** Ingeniero
**Endpoint:** `GET /Ingeniero/Fincas/Evaluar/{id}`
**APIs externas (paralelo):**
- OpenWeather: temperatura, presión, descripción clima
- OpenElevation: m.s.n.m.
**Manejo errores:** si API falla → mostrar "No disponible", NO bloquear flujo

---

## CU20/21/22 — Dictamen

**Actor:** Ingeniero
**Endpoint:** `POST /Ingeniero/Fincas/Dictamen/{id}` con `{tipo: "Aprobar"|"Rechazar"|"Devolver"}`

| Tipo | Estado | Obs obligatorias | Email |
|---|---|---|---|
| Aprobar | Aprobada | No | N07 |
| Rechazar | Rechazada (FINAL) | Sí | N08 |
| Devolver | Devuelta | Sí | N09 |

---

## CU23 — Registrar/Actualizar IBAN

**Actor:** Dueño
**Endpoint:** `POST /Dueno/CuentaBancaria` (JSON)
**Validación:** `^CR\d{20}$` en JS y C#
**Almacenamiento:** AES-256 cifrado en `IbanCompleto`, ofuscado `CR**...` en `IbanOfuscado`
**Regla:** desactiva cuenta anterior si existía (solo 1 activa por Dueño)

---

## CU24 — Activar Plan de Pagos

**Actor:** Ingeniero (post-dictamen Aprobar)
**Endpoint:** `POST /Ingeniero/Fincas/ActivarPlan/{id}`
**Precondición:** Dueño debe tener IBAN activo
**Proceso:**
1. Leer `ParametrosPago WHERE Vigente = true`
2. Calcular monto: `Hectareas × PrecioBase × (1 + Min(SumaPct, Tope))`
3. Persistir snapshot JSON de parámetros (inmutable)
4. Crear `PlanPago` + 12 `PagoMensual` con `FechaPago = activación + i×30 días`

---

## CU25 — Pago Automático (BackgroundService)

**Actor:** Sistema (`PagoHostedService`)
**Frecuencia:** 1 vez/día (`PeriodicTimer`)
**Query:** `PagosMensuales WHERE Estado=Pendiente AND FechaPago <= GETDATE()`
**Idempotencia:** NUNCA reprocesar `Estado=Ejecutado`
**Pago #12:** marca finca como `Vencida` + genera nueva entrada FIFO + email N12
**Errores:** individuales logueados sin detener ciclo

---

## CU26 — Ver Historial Pagos

**Actor:** Dueño
**Endpoint:** `GET /Dueno/Pagos/Data`
**Muestra:** numeroPago, fincaId, monto, fechaPago, fechaEjecución, estado

---

## CU27/28 — Reconfigurar/Ver Parámetros

**Actor:** Admin
**Endpoint:** `POST/GET /Admin/Parametros`
**Regla:** NUNCA modificar fila existente → siempre INSERT nueva fila
**Opción A:** nueva fila Vigente=true, planes activos intocables
**Opción B:** nueva fila + recalcular PagoMensual Pendientes (no Ejecutados) + email N14

---

## CU05-08 — Gestión Usuarios (Admin)

| CU | Acción | Endpoint |
|---|---|---|
| CU05 | Crear Ingeniero | `POST /Admin/Usuarios/Crear` |
| CU06 | Editar Usuario | `POST /Admin/Usuarios/Editar/{id}` |
| CU07 | Inactivar Usuario | `POST /Admin/Usuarios/Inactivar/{id}` |
| CU08 | Ver Usuarios | `GET /Admin/Usuarios` |

**CU07 — Inactivar cascada:** si Ingeniero tiene fincas EnRevision → devolverlas a Pendiente primero
