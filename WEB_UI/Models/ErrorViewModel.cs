// ============================================================
// ErrorViewModel.cs — Modelo para la vista de error global
// Se usa en Views/Shared/Error.cshtml cuando ocurre una excepción
// no manejada en la aplicación.
// ============================================================

namespace WEB_UI.Models
{
    public class ErrorViewModel
    {
        // ID único del request donde ocurrió el error.
        // Puede ser el ID de la actividad de diagnóstico (Activity.Current?.Id)
        // o el TraceIdentifier del HttpContext si no hay actividad activa.
        // Se muestra en la vista para ayudar al soporte a localizar el error en los logs.
        public string? RequestId { get; set; }

        // Indica si el RequestId tiene un valor válido para mostrar en la vista.
        // La vista usa esta propiedad para decidir si renderiza el RequestId o no.
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
