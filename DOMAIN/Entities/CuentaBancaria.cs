namespace Nativa.Domain.Entities
{
    public class CuentaBancaria
    {
        public int Id { get; set; }
        public int IdDueno { get; set; }
        public Sujeto? Dueno { get; set; }
        public string? Banco { get; set; }
        public string? TipoCuenta { get; set; }
        public string? Titular { get; set; }
        public string? IbanCompleto { get; set; }
        public string? IbanOfuscado { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
