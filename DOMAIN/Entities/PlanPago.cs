using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nativa.Domain.Entities
{
    public class PlanPago
    {
        public int Id { get; set; }
        public int IdActivo { get; set; }
        public Activo? Activo { get; set; }
        public int IdIngeniero { get; set; }
        public Sujeto? Ingeniero { get; set; }
        public DateTime FechaActivacion { get; set; }
        public string? SnapshotParametrosJson { get; set; }
        [Column(TypeName = "decimal(12, 2)")]
        public decimal MontoMensual { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
