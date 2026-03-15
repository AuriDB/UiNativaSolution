using System;

namespace Nativa.Domain.Entities
{
    public class OtpSesion
    {
        public int Id { get; set; }
        public int IdSujeto { get; set; }
        public Sujeto? Sujeto { get; set; }
        public string? HashOtp { get; set; }
        public DateTime Expiracion { get; set; }
        public bool Usada { get; set; }
        public int Intentos { get; set; }
        public DateTime? UltimoReenvio { get; set; }
        public int ConteoReenvios { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
