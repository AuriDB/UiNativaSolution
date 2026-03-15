using System;

namespace Nativa.Domain.Entities
{
    public class AdjuntoActivo
    {
        public int Id { get; set; }
        public int IdActivo { get; set; }
        public Activo? Activo { get; set; }
        public string? BlobUrl { get; set; }
        public string? NombreArchivo { get; set; }
        public DateTime FechaSubida { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
