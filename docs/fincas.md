# Sistema Nativa — Módulo Fincas / Activos (P3-P4)

---

## Ciclo de Vida de una Finca

```
[Dueño registra] → Pendiente
    ↓ Ingeniero toma (CU18)
EnRevision
    ↓ Dictamen Devolver (CU22)
Devuelta → [Dueño corrige + reenvía (CU15)] → Pendiente (nuevo timestamp FIFO)
    ↓ Dictamen Rechazar (CU21)
Rechazada (FINAL, irreversible)
    ↓ Dictamen Aprobar (CU20)
Aprobada
    ↓ Dueño registra IBAN (CU23) + Ingeniero activa plan (CU24)
[Plan activo, 12 pagos generados]
    ↓ PagoHostedService ejecuta pago #12
Vencida → [sistema re-ingresa copia a FIFO] → Pendiente
```

---

## Restricciones por Estado

| Estado | Dueño puede editar | Ingeniero puede evaluar | Puede reenviar |
|---|---|---|---|
| Pendiente | No | No (no asignada) | No |
| EnRevision | **No** | Sí | No |
| Devuelta | **Sí** | No | **Sí** |
| Aprobada | No | No | No |
| Rechazada | No | No | No |
| Vencida | No | No | No |

---

## Adjuntos

- **Contenedor Azure Blob:** `psa-docs`
- **Extensiones permitidas:** jpg, jpeg, png, pdf, dwg
- **Tamaño máximo:** 10 MB por archivo
- **NUNCA borrado físico** — sin botón de eliminar
- **Dev mode:** `BlobService` retorna URL simulada si ConnectionString == placeholder
- **Producción:** considerar SAS tokens (ver backlog S1)

---

## Concurrencia Optimista (FIFO CU18)

El `RowVersion` ([Timestamp] EF Core) previene que dos Ingenieros tomen la misma finca:

```csharp
_db.Entry(finca).Property(f => f.RowVersion).OriginalValue = rv;
finca.Estado      = EstadoActivoEnum.EnRevision;
finca.IdIngeniero = ingenieroId;

try { await _db.SaveChangesAsync(); }
catch (DbUpdateConcurrencyException)
{
    return (false, 409, "Conflicto: otro ingeniero tomó esta finca.");
}
```

El frontend (cola.js) muestra SweetAlert con 409 y refresca la cola automáticamente.

---

## APIs Externas en Evaluación (CU19)

Ambas APIs se llaman en **paralelo** con `Task.WhenAll`:

| API | URL | Datos |
|---|---|---|
| OpenWeather | `.../weather?lat=&lon=&appid=` | temperatura, presión, descripción |
| OpenElevation | `.../lookup?locations=lat,lng` | m.s.n.m. |

**Manejo de errores:** si falla → `null` → UI muestra "No disponible" — flujo continúa.

---

## Fórmula Resumen (ver pagos.md para detalle)

```
Pago = Hectareas × PrecioBase × (1 + Min(SumaPct, Tope))
```
