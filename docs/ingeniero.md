# Sistema Nativa — Módulo Ingeniero (P4)

---

## Cola FIFO (CU17-18)

La cola muestra todas las fincas con `Estado = Pendiente` ordenadas por `FechaRegistro ASC`.
El ingeniero toma la que prefiera (no está obligado a seguir el orden visualmente, pero el orden garantiza equidad).

### Tomar Finca — Mecanismo RowVersion
1. UI envía `{ rowVersion: "base64..." }` junto con el ID
2. Servicio establece `OriginalValue` del RowVersion en EF Core
3. EF genera `WHERE Id=@id AND RowVersion=@rv` implícitamente
4. Si otro ingeniero ya tomó la finca → 0 rows affected → `DbUpdateConcurrencyException` → 409

### Estados post-Tomar
- Finca: `EnRevision`
- `IdIngeniero` asignado
- Email N05 al Dueño

---

## Evaluar Finca (CU19)

Panel de evaluación incluye:
- Datos técnicos de la finca (readonly)
- Datos ambientales de APIs (carga lazy al presionar botón)
- Lista de adjuntos (links a Azure Blob)
- Panel de Dictamen (lateral)

### APIs Externas
Cargadas en paralelo (`Task.WhenAll`). Si fallan, se muestra "No disponible" sin interrumpir la evaluación.

---

## Dictamen (CU20-22)

| Acción | Tipo enviado | Estado resultante | Obs obligatorias | Email |
|---|---|---|---|---|
| Aprobar | `"Aprobar"` | Aprobada | No | N07 |
| Rechazar | `"Rechazar"` | Rechazada (FINAL) | **Sí** | N08 |
| Devolver | `"Devolver"` | Devuelta | **Sí** | N09 |

**Rechazar es irreversible.** La UI debe mostrar confirmación explícita.

Post-Aprobación: UI muestra botón "Activar Plan de Pagos". Si el Dueño no tiene IBAN, el botón retorna error sin ejecutar.

---

## Activar Plan (CU24)

1. Verificar `CuentaBancaria.Activo = true` del Dueño
2. Leer `ParametrosPago WHERE Vigente = true`
3. Calcular monto via `CalculadoraService`
4. Persistir snapshot JSON (inmutable)
5. Crear `PlanPago` + 12 `PagoMensual`

**Fechas de pago:** `FechaActivacion + i × 30 días` para i = 1..12

---

## Ver Adjuntos (CU16)

El Ingeniero puede ver los adjuntos de la finca que tiene asignada.
Endpoint: `GET /Ingeniero/Fincas/{id}/Adjuntos`
Restricción: solo si es la finca asignada a él y está en `EnRevision`.
