using Nativa.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nativa.Domain.Entities
{
    public class PagoMensual
    {
        public int Id { get; set; }
        public int IdPlan { get; set; }
        public PlanPago? Plan { get; set; }
        public int NumeroPago { get; set; }
        [Column(TypeName = "decimal(12, 2)")]
        public decimal Monto { get; set; }
        public DateTime FechaPago { get; set; }
        public EstadoPagoEnum Estado { get; set; }
        public DateTime? FechaEjecucion { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
