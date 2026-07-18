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
        
        [Required, MaxLength(300)]
        public string Nombre { get; set; } = string.Empty;
        
        public string Descripcion { get; set; } = "";
        
        public decimal Precio { get; set; }

        public decimal Descuento { get; set; } = 0;
        
        public bool EsGratuito { get; set; } = false;
        
        // URLs de recursos externos (Ahora permiten nulos)
        public string? ImagenUrl { get; set; } 
        public string Tipo { get; set; } = "Imprimible"; 
        public string? PdfUrl { get; set; }
        public string? VideoUrl { get; set; } 
        
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        
        public bool Activo { get; set; } = true;

        // Relación 1 a Muchos: Un producto pertenece a una categoría (PRINCIPAL)
        public int CategoriaId { get; set; }
        public Categoria Categoria { get; set; } = null!;

        // Relación Muchos a Muchos: Categorías SECUNDARIAS (Cross-listing)
        public ICollection<ProductoCategoriaSecundaria> CategoriasSecundarias { get; set; } = new List<ProductoCategoriaSecundaria>();

        // Relaciones con Tags y Paquetes
        public ICollection<ProductoTag> ProductoTags { get; set; } = new List<ProductoTag>();
        public ICollection<PaqueteProducto> PaqueteProductos { get; set; } = new List<PaqueteProducto>();

        // Productos Relacionados (Cross-selling: "También te puede interesar...")
        public ICollection<ProductoRelacionado> ProductosRelacionadosOrigen { get; set; } = new List<ProductoRelacionado>();
        public ICollection<ProductoRelacionado> ProductosRelacionadosDestino { get; set; } = new List<ProductoRelacionado>();
    }
}