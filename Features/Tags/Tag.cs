using System.ComponentModel.DataAnnotations;
using TiempoBiblia.Api.Features.Relaciones; // Para acceder a ProductoTag

namespace TiempoBiblia.Api.Features.Tags
{
    /// <summary>
    /// Representa una etiqueta para facilitar la búsqueda y filtrado de productos.
    /// </summary>
    public class Tag
    {
        public int Id { get; set; }
        
        [Required, MaxLength(50)]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Bandera de visibilidad. Si es false, no se muestra en los filtros del público.
        /// </summary>
        public bool Activo { get; set; } = true;

        // Relación Muchos a Muchos con Productos
        public ICollection<ProductoTag> ProductoTags { get; set; } = new List<ProductoTag>();
    }
}