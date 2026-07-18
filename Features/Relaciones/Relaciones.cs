using TiempoBiblia.Api.Features.Productos;
using TiempoBiblia.Api.Features.Tags;
using TiempoBiblia.Api.Features.Paquetes;
using TiempoBiblia.Api.Features.Categorias;

namespace TiempoBiblia.Api.Features.Relaciones
{
    public class ProductoTag
    {
        public int ProductoId { get; set; }
        public Producto Producto { get; set; } = null!;

        public int TagId { get; set; }
        public Tag Tag { get; set; } = null!;
    }

    public class PaqueteProducto
    {
        public int PaqueteId { get; set; }
        public Paquete Paquete { get; set; } = null!;

        public int ProductoId { get; set; }
        public Producto Producto { get; set; } = null!;
    }

    public class ProductoRelacionado
    {
        public int ProductoOrigenId { get; set; }
        public Producto ProductoOrigen { get; set; } = null!;

        public int ProductoRelacionadoId { get; set; }
        public Producto ProductoRelacionadoDestino { get; set; } = null!;
    }
    // En TiempoBiblia.Api.Features.Relaciones
    public class ProductoCategoriaSecundaria
    {
        public int ProductoId { get; set; }
        public Producto Producto { get; set; } = null!;

        public int CategoriaId { get; set; }
        public Categoria Categoria { get; set; } = null!;
    }
}