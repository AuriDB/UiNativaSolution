# Sistema Nativa — Integraciones Externas

---

## OpenWeather API

**Uso:** obtener clima actual de la ubicación de la finca al momento de evaluación.
**Endpoint:** `GET https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lng}&appid={key}`
**Config:** `ExternalApis:OpenWeatherApiKey` en appsettings
**Datos utilizados:** `main.temp` (°C), `main.pressure` (hPa), `weather[0].description`
**Error handling:** si falla → retorna `ClimaData = null` → UI muestra "No disponible"
**Dev mode:** si key == "TU_KEY" → retorna datos simulados sin hacer request real

---

## Open Elevation API

**Uso:** obtener elevación (m.s.n.m.) del predio.
**Endpoint:** `GET https://api.open-elevation.com/api/v1/lookup?locations={lat},{lng}`
**Config:** sin autenticación
**Datos utilizados:** `results[0].elevation`
**Error handling:** si falla → `ElevacionData = null` → UI muestra "No disponible"

---

## Azure Blob Storage

**Uso:** almacenar adjuntos de fincas (planos, fotos, documentos legales).
**Contenedor:** `psa-docs`
**Config:** `AzureBlob:ConnectionString`, `AzureBlob:ContainerName`
**Validaciones:** extensiones (jpg, jpeg, png, pdf, dwg), tamaño máx 10 MB
**Operaciones:** solo UPLOAD y READ (no DELETE — sin borrado físico)
**Dev mode:** si ConnectionString == "AZURE_CONN_STRING" → retorna URL simulada

### Pendiente (backlog S1)
- Generar SAS tokens con expiración para acceso seguro a adjuntos
- `BlobServiceClient.GetBlobClient(blobName).GenerateSasUri(BlobSasPermissions.Read, expiresOn)`

---

## Mailtrap (email dev)

**Uso:** interceptar emails en entorno de desarrollo.
**Config en `appsettings.Development.json`:**
```json
{
  "Email": {
    "Host": "smtp.mailtrap.io",
    "Port": 587,
    "User": "TU_USER",
    "Pass": "TU_PASS"
  }
}
```
**Panel:** https://mailtrap.io/inboxes

---

## Paralelismo en ExternalApiService

```csharp
var (climaTask, elevTask) = (
    ObtenerClimaAsync(lat, lng),
    ObtenerElevacionAsync(lat, lng)
);
await Task.WhenAll(climaTask, elevTask);
return new DatosAmbientalesDto
{
    Clima    = climaTask.Result,
    Elevacion = elevTask.Result
};
```

Ambas APIs se consultan simultáneamente para minimizar latencia.
