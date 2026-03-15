# Sistema Nativa — Notificaciones de Correo (P8)

---

## Matriz de Notificaciones

| Código | Evento | Destinatario | Servicio que la envía | Estado |
|---|---|---|---|---|
| N03 | OTP generado + bloqueo por 3 intentos | Dueño | `AuthService` (registro) | ✅ En código |
| N05 | Finca pasa a EnRevision | Dueño | `IngenieroService.TomarFincaAsync` | ✅ En código |
| N06 | Ingeniero solicita corrección (futuro) | Dueño | `IngenieroService` | ⬜ No requerido en spec actual |
| N07 | Dictamen Aprobada | Dueño | `IngenieroService.DictamenAsync` | ✅ En código |
| N08 | Dictamen Rechazada | Dueño | `IngenieroService.DictamenAsync` | ✅ En código |
| N09 | Dictamen Devuelta | Dueño | `IngenieroService.DictamenAsync` | ✅ En código |
| N10 | Pago mensual ejecutado + PDF | Dueño | `PagoHostedService` | ⬜ P6 |
| N12 | Contrato vencido (pago #12) | Dueño | `PagoHostedService` | ⬜ P6 |
| N13 | Cuenta inactivada por Admin | Usuario | `AdminService` | ⬜ P7 |
| N14 | Recálculo Opción B de parámetros | Dueño | `AdminService` | ⬜ P7 |

---

## EmailService — Métodos disponibles

```csharp
// Envía OTP al nuevo Dueño
Task EnviarOtpAsync(string correo, string nombre, string otp)

// Envía link de restablecimiento de contraseña
Task EnviarResetPasswordAsync(string correo, string nombre, string resetUrl)

// Genérico (HTML libre) — usado por N05, N07, N08, N09
Task EnviarGenericoAsync(string correo, string asunto, string htmlBody)
```

---

## Configuración SMTP

### Producción
```json
{
  "Email": {
    "From":        "nativa@noreply.cr",
    "DisplayName": "Sistema Nativa"
  }
}
```

### Desarrollo (Mailtrap)
```json
// appsettings.Development.json
{
  "Email": {
    "Host": "smtp.mailtrap.io",
    "Port": 587,
    "User": "TU_USER_MAILTRAP",
    "Pass": "TU_PASS_MAILTRAP"
  }
}
```

---

## Plantilla N10 — Pago Ejecutado (con PDF)

```
Asunto: Tu pago mensual #N fue procesado — Sistema Nativa
HTML:
  Hola {nombre},
  Tu pago mensual #{numeroPago} de ₡{monto:N2} para la finca #{fincaId}
  fue procesado el {fechaEjecucion:dd/MM/yyyy}.
  Adjunto encontrarás tu comprobante en PDF.
```

El PDF se genera con **QuestPDF** y se adjunta via `BodyBuilder.Attachments`.

---

## Plantilla N12 — Contrato Vencido

```
Asunto: Tu contrato PSA ha vencido — Sistema Nativa
HTML:
  Hola {nombre},
  Tu contrato para la finca #{fincaId} ha concluido (pago #12 ejecutado).
  Tu propiedad ha sido ingresada nuevamente al programa para un nuevo período.
  Pronto recibirás noticias de un ingeniero.
```

---

## Plantilla N13 — Cuenta Inactivada

```
Asunto: Tu cuenta ha sido inactivada — Sistema Nativa
HTML:
  Hola {nombre},
  Tu cuenta en el Sistema Nativa ha sido inactivada por un administrador.
  Si crees que esto es un error, contacta al soporte.
```

---

## Plantilla N14 — Recálculo Opción B

```
Asunto: Actualización en tu plan de pagos — Sistema Nativa
HTML:
  Hola {nombre},
  Los parámetros de pago del programa han sido actualizados.
  Tu plan de pagos activo para la finca #{fincaId} fue recalculado.
  Nuevo monto mensual: ₡{nuevoMonto:N2}
  Los pagos ya ejecutados no fueron modificados.
```
