// ============================================================
// EstadoSujetoEnum.cs — Estado de la cuenta de un usuario
// Controla si un Sujeto puede iniciar sesión en el sistema.
// El administrador puede cambiar el estado de cualquier usuario
// (excepto del usuario root que nunca puede ser inactivado).
// ============================================================

namespace WEB_UI.Models.Enums;

public enum EstadoSujetoEnum
{
    // La cuenta está habilitada y el usuario puede iniciar sesión normalmente.
    Activo    = 1,

    // La cuenta fue desactivada por un administrador.
    // El usuario no puede iniciar sesión mientras esté inactivo.
    Inactivo  = 2,

    // La cuenta fue bloqueada temporalmente (ej: por múltiples intentos fallidos).
    // Similar a Inactivo pero indica un bloqueo automático por seguridad.
    Bloqueado = 3
}
