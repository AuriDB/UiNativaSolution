using Nativa.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nativa.Domain.Entities
{
    public class Activo
    {
        public int Id { get; set; }
        public int IdDueno { get; set; }
        public Sujeto? Dueno { get; set; }
        public int? IdIngeniero { get; set; }
        public Sujeto? Ingeniero { get; set; }
        [Column(TypeName = "decimal(10, 4)")]
        public decimal Hectareas { get; set; }
        [Column(TypeName = "decimal(5, 2)")]
        public decimal Vegetacion { get; set; }
        [Column(TypeName = "decimal(5, 2)")]
        public decimal Hidrologia { get; set; }
        [Column(TypeName = "decimal(5, 2)")]
        public decimal Topografia { get; set; }
        public bool EsNacional { get; set; }
        [Column(TypeName = "decimal(9, 6)")]
        public decimal Lat { get; set; }
        [Column(TypeName = "decimal(9, 6)")]
        public decimal Lng { get; set; }
        public EstadoActivoEnum Estado { get; set; }
        public DateTime FechaRegistro { get; set; }
        [MaxLength(2000)]
        public string? Observaciones { get; set; }
        [Timestamp]
        public byte[]? RowVersion { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
