using System.ComponentModel.DataAnnotations;

namespace TiempoBiblia.Api.Features.Productos
{
    /// <summary>
    /// DTO utilizado para recibir los datos desde el Frontend al Crear o Editar un producto.
    /// Contiene los campos básicos y las listas de IDs para establecer las relaciones.
    /// </summary>
    public class ProductoDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Nombre { get; set; } = string.Empty;
        
        public string Descripcion { get; set; } = string.Empty;
        
        public decimal Precio { get; set; }
        
        public decimal Descuento { get; set; }
        
        public bool EsGratuito { get; set; }
        
        public string? ImagenUrl { get; set; } 
        
        [Required]
        public string Tipo { get; set; } = "Imprimible"; 
        
        public string? PdfUrl { get; set; }
        
        public string? VideoUrl { get; set; }
        
        public bool Activo { get; set; } = true;

        // 🔥 RELACIONES: En lugar de objetos completos, recibimos solo los IDs seleccionados
        [Required(ErrorMessage = "Debe seleccionar una categoría principal")]
        public int CategoriaId { get; set; }

        public List<int> CategoriasSecundariasIds { get; set; } = new();
        
        public List<int> TagsIds { get; set; } = new();
        
        public List<int> ProductosRelacionadosIds { get; set; } = new();
    }
}