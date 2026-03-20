// ============================================================
// EstadoActivoEnum.cs — Estado de una finca en el flujo PSA
// Una finca sigue este ciclo de vida desde que se registra
// hasta que termina su plan de pago o es rechazada:
//
//   Pendiente → EnRevision → Aprobada  (flujo exitoso)
//                          → Devuelta  → (el dueño corrige) → Pendiente
//                          → Rechazada (fin del proceso)
//   Aprobada → Vencida     (cuando el plan de 12 meses termina)
// ============================================================

namespace WEB_UI.Models.Enums;

public enum EstadoActivoEnum
{
    // La finca fue registrada por el dueño y está esperando en la cola FIFO
    // a que un ingeniero la tome para evaluarla.
    Pendiente   = 1,

    // Un ingeniero tomó la finca de la cola y la está evaluando actualmente.
    // La finca queda "bloqueada" para ese ingeniero hasta que emita su dictamen.
    EnRevision  = 2,

    // El ingeniero aprobó la finca. Se puede activar un plan de pago PSA.
    // Este es el único estado desde el que se puede generar un PlanPago.
    Aprobada    = 3,

    // El ingeniero devolvió la finca con observaciones para que el dueño la corrija.
    // El dueño puede editarla y volver a enviarla (cambia a Pendiente de nuevo).
    Devuelta    = 4,

    // El ingeniero rechazó la finca definitivamente. No puede volver a evaluarse
    // en el sistema. El dueño debería registrar una nueva finca si aplica.
    Rechazada   = 5,

    // El plan de pago de 12 meses terminó. La finca necesitaría un nuevo
    // proceso de evaluación para recibir un nuevo plan PSA.
    Vencida     = 6
}
