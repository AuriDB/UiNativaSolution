# PENDIENTE — Sistema Nativa
> Última actualización: 2026-03-15
> Documento de referencia: qué falta para que la aplicación funcione en producción.

---

## ESTADO ACTUAL

La aplicación **compila y corre localmente** con SQL Server local (`PSA_Dev`).
Los flujos principales están implementados (auth, fincas, FIFO, dictamen, pagos, admin).
Lo que sigue es configuración externa + pequeñas funcionalidades faltantes.

---

## 1. CUENTAS A CREAR (obligatorias)

### 1.1 Mailtrap — Email de desarrollo
**Propósito:** Recibir todos los correos enviados por la app en un inbox de prueba.
**Es gratis.** Sin esto el OTP, reset de contraseña y notificaciones no llegan.

| Paso | Acción |
|------|--------|
| 1 | Ir a [mailtrap.io](https://mailtrap.io) → Registrarse |
| 2 | Email Testing → Inboxes → tu inbox → SMTP Settings |
| 3 | Copiar Host, Port, Username, Password |
| 4 | Pegar en `appsettings.Development.json` (ver sección 3) |

---

### 1.2 OpenWeather API — Datos climáticos en evaluación de finca
**Propósito:** Mostrar temperatura, presión y elevación al Ingeniero en la pantalla de evaluación.
**Gratis hasta 1 000 llamadas/día** (más que suficiente).

| Paso | Acción |
|------|--------|
| 1 | Ir a [openweathermap.org](https://openweathermap.org) → Sign Up |
| 2 | My API Keys → copiar la API Key |
| 3 | Pegar en `appsettings.json` campo `ExternalApis:OpenWeatherApiKey` |

> **Sin esta key:** La sección de APIs externas en Evaluar mostrará "No disponible" pero no bloqueará el flujo.

---

### 1.3 Azure — Blob Storage + SQL (para producción)
**Propósito:** Almacenar adjuntos de fincas en la nube y hospedar la BD en Azure.

**Pasos Azure Blob:**

| Paso | Acción |
|------|--------|
| 1 | [portal.azure.com](https://portal.azure.com) → Crear recurso → Storage Account |
| 2 | Nombre: `nativastorage` (o similar) — Region: East US 2 |
| 3 | Una vez creada: Security + Networking → Access Keys → copiar Connection String |
| 4 | Containers → + Container → Nombre: `psa-docs` → Private (no public) |
| 5 | Pegar la Connection String en `appsettings.json` campo `AzureBlob:ConnectionString` |

> **Sin esto:** BlobService corre en **modo dev** automáticamente — simula la subida y devuelve una URL falsa (`dev.blob.local/...`). Los adjuntos no se pierden pero no se guardan realmente.

**Pasos Azure SQL (solo para producción):**

| Paso | Acción |
|------|--------|
| 1 | portal.azure.com → SQL Server → SQL Database `PSA_Dev` |
| 2 | Copiar Connection String ADO.NET |
| 3 | Reemplazar en `appsettings.json` → `ConnectionStrings:DefaultConnection` |

---

## 2. CLAVES DE SEGURIDAD A GENERAR (obligatorio antes de producción)

Los valores actuales en `appsettings.json` son placeholders. **Hay que reemplazarlos.**

### Generar con PowerShell:
```powershell
# Auth:HmacSecret (mínimo 32 bytes, para firmar tokens de reset de contraseña)
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))

# Encryption:Key (exactamente 32 bytes, para cifrar IBANs con AES-256)
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

### Poner los valores en `appsettings.json`:
```json
"Auth": {
  "HmacSecret": "PEGAR_AQUI_EL_RESULTADO_DEL_PRIMER_COMANDO"
},
"Encryption": {
  "Key": "PEGAR_AQUI_EL_RESULTADO_DEL_SEGUNDO_COMANDO"
}
```

> **CRÍTICO:** Estos valores deben ser los mismos entre deploys. Si cambian, los IBANs cifrados en BD y los tokens de reset existentes quedan inválidos.

---

## 3. ARCHIVOS DE CONFIGURACIÓN A COMPLETAR

### `appsettings.json` (todos los entornos)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PSA_Dev;Trusted_Connection=True;TrustServerCertificate=True;"
    // Producción → Connection String de Azure SQL
  },
  "Auth": {
    "CookieName": "nativa_auth",
    "HmacSecret": "GENERAR_CON_POWERSHELL_32_BYTES_BASE64"   // ← PENDIENTE
  },
  "Encryption": {
    "Key": "GENERAR_CON_POWERSHELL_32_BYTES_BASE64=="         // ← PENDIENTE
  },
  "AzureBlob": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...", // ← PENDIENTE (Azure)
    "ContainerName": "psa-docs"
  },
  "ExternalApis": {
    "OpenWeatherApiKey": "PEGAR_KEY_DE_OPENWEATHERMAP"        // ← PENDIENTE
  },
  "Email": {
    "From": "nativa@noreply.cr",
    "DisplayName": "Sistema Nativa"
  }
}
```

### `appsettings.Development.json` (solo dev local — NO subir a git)
```json
{
  "Email": {
    "Host": "sandbox.smtp.mailtrap.io",   // ← PENDIENTE (Mailtrap)
    "Port": 587,
    "User": "TU_USER_MAILTRAP",           // ← PENDIENTE
    "Pass": "TU_PASS_MAILTRAP"            // ← PENDIENTE
  }
}
```

> Verificar que `appsettings.Development.json` está en `.gitignore`. Si no, agregarlo.

---

## 4. FUNCIONALIDADES PENDIENTES (código)

| # | Funcionalidad | Spec ref | Estado | Impacto |
|---|--------------|----------|--------|---------|
| 1 | **PDF comprobante de pago (N10)** | QuestPDF | Sin implementar | El email de pago ejecutado llega sin adjunto PDF |
| 2 | **CU09/CU10 Editar perfil Dueño** | `/Dueno/Perfil` | Sin implementar | El perfil es solo lectura para todos los roles |
| 3 | **SAS token real para adjuntos** | BlobService | Simplificado | El Ingeniero ve URL directa, no SAS expirable |
| 4 | **Logout por POST (CSRF)** | `_Layout.cshtml` | GET actual | Riesgo CSRF moderado en producción |

---

## 5. LIMPIEZA DE CÓDIGO (deuda técnica)

| Archivo/Carpeta | Problema | Acción recomendada |
|----------------|----------|--------------------|
| `Controllers/LoginController.cs` | Legacy, no se usa | Eliminar |
| `Controllers/RegisterController.cs` | Legacy, no se usa | Eliminar |
| `Views/Login/` | Legacy, no se usa | Eliminar carpeta |
| `Views/Register/` | Legacy, no se usa | Eliminar carpeta |

---

## 6. CHECKLIST PARA DEMO / ENTREGA

```
[ ] appsettings.json → Auth:HmacSecret generado
[ ] appsettings.json → Encryption:Key generado
[ ] appsettings.Development.json → credenciales Mailtrap
[ ] appsettings.json → OpenWeatherApiKey
[ ] appsettings.json → AzureBlob:ConnectionString  (o dejar modo dev)
[ ] dotnet ef database update  →  BD PSA_Dev actualizada
[ ] Ejecutar app → DataSeeder siembra usuarios de prueba automáticamente
[ ] Login con root@nativa.cr / Root@1234  →  Admin
[ ] Login con ing@nativa.cr  / Ing@1234   →  Ingeniero
[ ] Login con dueno@nativa.cr / Dueno@1234 →  Dueño
[ ] Admin → Parámetros → Crear parámetros vigentes (precio base + porcentajes)
[ ] Dueño → Registrar finca → aparece en cola FIFO del Ingeniero
[ ] Ingeniero → Tomar finca → Evaluar → Aprobar
[ ] Dueño → Registrar IBAN → Ingeniero → Activar Plan
[ ] Verificar correos en Mailtrap inbox
```

---

## 7. ORDEN DE CONFIGURACIÓN RECOMENDADO

1. Generar HmacSecret y Encryption:Key → pegar en `appsettings.json`
2. Crear cuenta Mailtrap → pegar credenciales en `appsettings.Development.json`
3. Crear API Key OpenWeather → pegar en `appsettings.json`
4. Ejecutar `dotnet ef database update` si la BD local no existe
5. Ejecutar la app → DataSeeder corre automáticamente
6. Ir a Admin → Parámetros → crear primer juego de parámetros vigentes
7. Probar flujo completo con los usuarios de prueba
8. Para producción: crear recursos Azure (SQL + Blob) y actualizar connection strings
