// ============================================================
// RolEnum.cs — Roles de usuario en el sistema Nativa
// Define los tres tipos de actores que pueden iniciar sesión.
// El rol determina a qué módulo (dashboard) accede el usuario
// y qué operaciones puede realizar en el sistema.
// ============================================================

namespace WEB_UI.Models.Enums;

public enum RolEnum
{
    // Propietario de una o más fincas. Puede registrar fincas,
    // subir adjuntos, ver su historial de pagos y registrar su IBAN.
    Dueno     = 1,

    // Profesional ambiental. Evalúa fincas en cola FIFO y emite
    // dictámenes de aprobación, devolución o rechazo.
    Ingeniero = 2,

    // Administrador del sistema. Gestiona usuarios, configura
    // parámetros de pago, revisa auditoría y genera reportes.
    Admin     = 3
}
