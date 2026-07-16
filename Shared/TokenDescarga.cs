using System.ComponentModel.DataAnnotations;
using TiempoBiblia.Api.Features.Productos;

namespace TiempoBiblia.Api.shared
{
    public class TokenDescarga
    {
        // Usamos un Guid para que el link sea inmensamente difícil de adivinar
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public int ProductoId { get; set; }
        public Producto Producto { get; set; } = null!;

        public string CorreoCliente { get; set; } = string.Empty;
        
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime FechaExpiracion { get; set; }
        
        public int DescargasRealizadas { get; set; } = 0;
        public int LimiteDescargas { get; set; } = 3; // Límite para evitar abusos
        
        public bool EsValido => DateTime.UtcNow <= FechaExpiracion && DescargasRealizadas < LimiteDescargas;
    }
}