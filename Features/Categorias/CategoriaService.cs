namespace TiempoBiblia.Api.Features.Categorias
{
    /// <summary>
    /// Capa de reglas de negocio. Aquí validamos antes de tocar la base de datos.
    /// </summary>
    public class CategoriaService
    {
        private readonly CategoriaRepository _repository;

        public CategoriaService(CategoriaRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Categoria>> ObtenerTodasAdminAsync()
        {
            return await _repository.ObtenerTodasAdminAsync();
        }

        public async Task<List<Categoria>> ObtenerActivasPublicoAsync()
        {
            return await _repository.ObtenerActivasPublicoAsync();
        }

        public async Task<Categoria> CrearAsync(Categoria categoria)
        {
            // Regla de negocio: No se permiten categorías sin nombre
            if (string.IsNullOrWhiteSpace(categoria.Nombre))
            {
                throw new ArgumentException("El nombre de la categoría no puede estar vacío");
            }

            return await _repository.CrearAsync(categoria);
        }
    }
}