using System.ComponentModel.DataAnnotations;
using TiempoBiblia.Api.Features.Categorias;

namespace TiempoBiblia.Api.Features.Productos
{
    // DTO para la colección de imágenes
    public class ImagenProductoDto
    {
        public int Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public int ProductoId { get; set; }
    }

    /// <summary>
    /// DTO utilizado para recibir y enviar los datos del Producto.
    /// </summary>
    public class ProductoDto
    {
        public int Id { get; set; } // 🔥 Añadido para que el Frontend lo pueda leer
        
        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Nombre { get; set; } = string.Empty;
        
        public string Descripcion { get; set; } = string.Empty;
        
        public decimal Precio { get; set; }
        public decimal PrecioUsd { get; set; }
        public decimal Descuento { get; set; }
        public bool EsGratuito { get; set; }
        public string? ImagenUrl { get; set; } 
        
        [Required]
        public string Tipo { get; set; } = "Imprimible"; 
        
        public string? PdfUrl { get; set; }
        public string? VideoUrl { get; set; }
        public bool Activo { get; set; } = true;

        // 🔥 NUEVOS CAMPOS MATEMÁTICOS PARA EL FRONTEND
        public int PromedioCalificacion { get; set; }
        public int TotalResenas { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una categoría principal")]
        public int CategoriaId { get; set; }
        public CategoriaDto Categoria { get; set; } = new(); // 🔥 NUEVO: El objeto Categoría

        // Relaciones (Lectura/Escritura)
        public List<int> CategoriasSecundariasIds { get; set; } = new();
        public List<int> TagsIds { get; set; } = new();
        public List<int> ProductosRelacionadosIds { get; set; } = new();
        public List<ImagenProductoDto> ImagenesSecundarias { get; set; } = new();
    }
}