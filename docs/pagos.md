# Sistema Nativa — Módulo de Pagos (P5-P6)

---

## Fórmula de Cálculo (CalculadoraService)

```
SumaPct = PctVegetacion + PctHidrologia + (EsNacional ? PctNacional : 0) + PctTopografia
Pago    = Hectareas × PrecioBase × (1 + Math.Min(SumaPct, Tope))
Redondeo: 2 decimales, MidpointRounding.AwayFromZero
```

**Parámetros:** leer de `ParametrosPago WHERE Vigente = true`
**Snapshot:** al activar plan, serializar parámetros vigentes en JSON → INMUTABLE

---

## Activar Plan (CU24)

```
POST /Ingeniero/Fincas/ActivarPlan/{id}
    ↓ Verificar finca Aprobada + IdIngeniero = yo
    ↓ Verificar CuentaBancaria.Activo = true del Dueño
    ↓ Leer ParametrosPago.Vigente
    ↓ Calcular MontoMensual
    ↓ INSERT PlanPago (snapshot JSON)
    ↓ FOR i=1..12: INSERT PagoMensual (FechaPago = activacion + i×30d)
```

---

## PagoHostedService (CU25)

**Tipo:** `BackgroundService` + `PeriodicTimer` (1 vez/día a medianoche UTC)

```
LOOP diario:
    SELECT PagosMensuales WHERE Estado=Pendiente AND FechaPago <= GETDATE()
    FOR EACH pago:
        BEGIN TRANSACTION
            pago.Estado         = Ejecutado
            pago.FechaEjecucion = UtcNow
            SAVE
        COMMIT
        EnviarEmail N10 (comprobante PDF QuestPDF)

        IF pago.NumeroPago == 12:
            finca.Estado = Vencida
            INSERT nuevo Activo (copia → Pendiente FIFO)
            EnviarEmail N12

        Log.Information("Pago {id} ejecutado")

    CATCH errores individuales → Log.Error → continuar ciclo (sin detener)
```

**Idempotencia:** la query filtra `Estado=Pendiente` → nunca reprocesa Ejecutados

---

## IBAN (CU23)

**Validación doble (JS y C#):** `^CR\d{20}$`
**Cifrado:** AES-256 en `IbanCompleto`
**Ofuscado:** `CR********************` en `IbanOfuscado`
**Regla:** máximo 1 cuenta activa por Dueño → desactiva anterior

---

## Historial Pagos (CU26)

**Endpoint:** `GET /Dueno/Pagos/Data`
**Retorna:** todos los PagosMensuales del Dueño (via PlanPago → Activo → IdDueno)

---

## Estados de Pago

| Estado | Descripción |
|---|---|
| `Pendiente` | Programado, aún no procesado |
| `Ejecutado` | Procesado por PagoHostedService, PDF enviado |
