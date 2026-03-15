# Sistema Nativa — Documentación del Proyecto

## ¿Qué es esta carpeta?
Fuente de verdad compartida entre el equipo de desarrollo y la IA asistente.
Antes de escribir cualquier código, consultar esta carpeta.

## Archivos
| Archivo | Propósito |
|---|---|
| `progress.md` | Checklist de avance por parte (P1–P9) |
| `conventions.md` | Convenciones de código, naming, comentarios |
| `entity-mapping.md` | Mapeo spec (español) → código (inglés) |
| `architecture.md` | Arquitectura, flujo de datos, estructura de carpetas |
| `ai-notes.md` | Dudas, decisiones y notas técnicas de la IA durante implementación |

## Resumen del sistema
**Sistema Nativa** es una plataforma web de Pago por Servicios Ambientales (PSA) para Costa Rica.
Permite a dueños de fincas registrar predios, que un ingeniero evalúe en cola FIFO,
y que el sistema genere y ejecute un plan de pagos mensual durante 12 meses.

## Actores
| Rol | Descripción |
|---|---|
| Owner (Dueño) | Registra fincas, sube adjuntos, registra IBAN, consulta pagos |
| Engineer (Ingeniero) | Toma fincas de la cola FIFO, evalúa, emite dictamen, activa plan de pagos |
| Admin | Gestiona usuarios, configura parámetros de pago |
| System | PagoHostedService ejecuta pagos pendientes diariamente |

## Stack técnico
- **Runtime:** .NET 10, ASP.NET Core MVC
- **Proyecto:** `WEB_UI` (único — sin proyectos separados)
- **Namespace base:** `Nativa.*`
- **BD:** SQL Server local, base de datos `PSA_Dev`
- **ORM:** Entity Framework Core 10, Code First, Migrations
- **Auth:** ASP.NET Core Cookie Auth — cookie `nativa_auth`, 8h
- **Hashing:** BCrypt.Net-Next factor 12
- **Frontend:** Razor Views + Bootstrap 5 Bootswatch Minty (CDN)
- **JS:** jQuery AJAX + SweetAlert2 + Ag-Grid Community (CDN)
- **Mapas:** Leaflet.js
- **Blob:** Azure Blob Storage, contenedor `psa-docs`
- **Email (dev):** Mailtrap SMTP
- **PDF:** QuestPDF
- **Testing:** xUnit + Moq

## Cómo usar la IA asistente
1. Indicar la Parte a implementar: `"implementá P1"`, `"implementá P3"`, etc.
2. La IA lee esta carpeta antes de escribir código.
3. Las dudas y decisiones tomadas durante la sesión se registran en `ai-notes.md`.
4. Actualizar `progress.md` al completar cada parte.
