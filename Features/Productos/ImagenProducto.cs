namespace TiempoBiblia.Api.Features.Productos
{
    public class ImagenProducto
    {
        public int Id { get; set; }
        public string Url { get; set; } = string.Empty;
        
        // Relación con el producto
        public int ProductoId { get; set; }
        public Producto Producto { get; set; } = null!;
    }
}