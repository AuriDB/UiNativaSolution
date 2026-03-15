using Nativa.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace Nativa.Domain.Entities
{
    public class Sujeto
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(20)]
        public string? Cedula { get; set; }
        [Required]
        [MaxLength(200)]
        public string? Nombre { get; set; }
        [Required]
        [MaxLength(200)]
        public string? Correo { get; set; }
        [Required]
        public string? PasswordHash { get; set; }
        public RolEnum Rol { get; set; }
        public EstadoSujetoEnum Estado { get; set; }
        [Timestamp]
        public byte[]? RowVersion { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
