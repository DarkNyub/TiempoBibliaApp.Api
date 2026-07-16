using System.ComponentModel.DataAnnotations;
using TiempoBiblia.Api.Features.Categorias;
using TiempoBiblia.Api.Features.Relaciones; 

namespace TiempoBiblia.Api.Features.Productos
{
    /// <summary>
    /// Entidad principal del sistema. Representa un archivo digital (PDF, Video) a consumir o comprar.
    /// </summary>
    public class Producto
    {
        public int Id { get; set; }
        
        [Required, MaxLength(200)]
        public string Nombre { get; set; } = string.Empty;
        
        public string Descripcion { get; set; } = string.Empty;
        
        /// <summary>
        /// Precio base. Si EsGratuito es true, el frontend puede ignorar esto o mostrarlo tachado.
        /// </summary>
        public decimal Precio { get; set; }

        /// <summary>
        /// Descuento aplicable (ej. monto fijo o porcentaje). El frontend decide cómo renderizarlo.
        /// </summary>
        public decimal Descuento { get; set; } = 0;
        
        public bool EsGratuito { get; set; }
        
        // URLs de recursos externos
        public string ImagenUrl { get; set; } = string.Empty; 
        public string Tipo { get; set; } = string.Empty; // Ej: "PDF", "Video", "Hibrido"
        public string PdfUrl { get; set; } = string.Empty;
        public string VideoUrl { get; set; } = string.Empty; 
        
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Bandera de visibilidad (Soft-delete).
        /// </summary>
        public bool Activo { get; set; } = true;

        // Relación 1 a Muchos: Un producto pertenece a una categoría
        public int CategoriaId { get; set; }
        public Categoria Categoria { get; set; } = null!;

        // Relaciones con Tags y Paquetes
        public ICollection<ProductoTag> ProductoTags { get; set; } = new List<ProductoTag>();
        public ICollection<PaqueteProducto> PaqueteProductos { get; set; } = new List<PaqueteProducto>();

        // Productos Relacionados (Cross-selling: "También te puede interesar...")
        public ICollection<ProductoRelacionado> ProductosRelacionadosOrigen { get; set; } = new List<ProductoRelacionado>();
        public ICollection<ProductoRelacionado> ProductosRelacionadosDestino { get; set; } = new List<ProductoRelacionado>();
    }
}