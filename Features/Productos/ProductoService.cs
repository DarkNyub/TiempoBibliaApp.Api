namespace TiempoBiblia.Api.Features.Productos
{
    /// <summary>
    /// Capa de lógica de negocio para los Productos.
    /// </summary>
    public class ProductoService
    {
        private readonly ProductoRepository _repository;

        public ProductoService(ProductoRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Producto>> ObtenerTodosAdminAsync()
        {
            return await _repository.ObtenerTodosAdminAsync();
        }

        public async Task<List<Producto>> ObtenerActivosPublicoAsync()
        {
            return await _repository.ObtenerActivosPublicoAsync();
        }

        public async Task<Producto> CrearAsync(Producto producto)
        {
            // Validaciones básicas de negocio
            if (string.IsNullOrWhiteSpace(producto.Nombre))
            {
                throw new ArgumentException("El nombre del producto es obligatorio.");
            }

            if (producto.Precio < 0 || producto.Descuento < 0)
            {
                throw new ArgumentException("El precio y el descuento no pueden ser negativos.");
            }

            // Regla de coherencia: Si es gratuito, aseguramos que el precio backend refleje eso
            if (producto.EsGratuito)
            {
                producto.Precio = 0;
                producto.Descuento = 0;
            }

            return await _repository.CrearAsync(producto);
        }
    }
}