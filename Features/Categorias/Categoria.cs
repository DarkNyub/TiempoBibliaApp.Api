using System.ComponentModel.DataAnnotations;
using TiempoBiblia.Api.Features.Productos; // Asumiendo que Productos estará en esta ruta

namespace TiempoBiblia.Api.Features.Categorias
{
    /// <summary>
    /// Representa una agrupación de productos (Ej: "Cursos", "PDFs Gratuitos")
    /// </summary>
    public class Categoria
    {
        public int Id { get; set; }
        
        [Required, MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Bandera de visibilidad. Si es false, no se muestra al público pero sí en el admin.
        /// </summary>
        public bool Activo { get; set; } = true; 
        
        // Relación: Una categoría tiene muchos productos
        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}