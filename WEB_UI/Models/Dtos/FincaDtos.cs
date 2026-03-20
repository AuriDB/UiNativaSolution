// ============================================================
// FincaDtos.cs — DTOs para operaciones sobre fincas (Activos)
// Se usan como cuerpo (body) de las llamadas que WEB_UI hace
// a la API externa para gestionar fincas, cuentas IBAN
// y el proceso de evaluación del ingeniero.
// ============================================================

namespace WEB_UI.Models.Dtos;

// DTO para POST /api/owner/properties
// Datos necesarios para registrar una nueva finca.
// Los porcentajes (Vegetacion, Hidrologia, Topografia) son valores 0-100.
// Las coordenadas (Lat, Lng) provienen del mapa Leaflet.
public record RegistroFincaDto(
    decimal Hectareas,
    decimal Vegetacion,
    decimal Hidrologia,
    decimal Topografia,
    bool    EsNacional,
    decimal Lat,
    decimal Lng);

// DTO para PUT /api/owner/properties/{id}
// Datos actualizados de una finca existente.
// Solo se puede editar si la finca está en estado "Pendiente" o "Devuelta".
// Incluye Observaciones que el dueño puede agregar como respuesta
// a las observaciones del ingeniero en una devolución.
public record EditarFincaDto(
    decimal  Hectareas,
    decimal  Vegetacion,
    decimal  Hidrologia,
    decimal  Topografia,
    bool     EsNacional,
    decimal  Lat,
    decimal  Lng,
    string?  Observaciones);

// DTO para POST /api/owner/bank-account
// Datos del IBAN bancario del dueño para recibir pagos PSA.
// La API cifra el Iban con AES-256 antes de guardarlo en la BD.
public record RegistrarIbanDto(
    string Banco,
    string TipoCuenta,
    string Titular,
    string Iban);

// DTO para POST /api/engineer/properties/{id}/dictamen
// Dictamen del ingeniero sobre una finca en revisión.
// Tipo puede ser: "Aprobar", "Devolver" o "Rechazar".
// Observaciones son obligatorias si Tipo = "Devolver".
public record DictamenDto(string Tipo, string? Observaciones);

// DTO para POST /api/engineer/properties/{id}/tomar
// El ingeniero "toma" una finca de la cola FIFO para evaluarla.
// RowVersion es el token de concurrencia optimista (Base64) que la API
// usa para detectar si la finca fue tomada por otro ingeniero al mismo tiempo.
// Si hay conflicto, la API retorna HTTP 409 Conflict.
public record TomarFincaDto(string RowVersion);
