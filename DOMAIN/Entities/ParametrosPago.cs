using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nativa.Domain.Entities
{
    public class ParametrosPago
    {
        public int Id { get; set; }
        [Column(TypeName = "decimal(10, 2)")]
        public decimal PrecioBase { get; set; }
        [Column(TypeName = "decimal(5, 4)")]
        public decimal PctVegetacion { get; set; }
        [Column(TypeName = "decimal(5, 4)")]
        public decimal PctHidrologia { get; set; }
        [Column(TypeName = "decimal(5, 4)")]
        public decimal PctNacional { get; set; }
        [Column(TypeName = "decimal(5, 4)")]
        public decimal PctTopografia { get; set; }
        [Column(TypeName = "decimal(5, 4)")]
        public decimal Tope { get; set; }
        public bool Vigente { get; set; }
        public DateTime FechaCreacion { get; set; }
        public int CreadoPor { get; set; }
        public Sujeto? Creador { get; set; }
    }
}
