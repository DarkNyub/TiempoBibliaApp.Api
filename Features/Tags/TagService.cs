namespace TiempoBiblia.Api.Features.Tags
{
    /// <summary>
    /// Capa de lógica de negocio para los Tags.
    /// </summary>
    public class TagService
    {
        private readonly TagRepository _repository;

        public TagService(TagRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Tag>> ObtenerTodosAdminAsync()
        {
            return await _repository.ObtenerTodosAdminAsync();
        }

        public async Task<List<Tag>> ObtenerActivosPublicoAsync()
        {
            return await _repository.ObtenerActivosPublicoAsync();
        }

        public async Task<Tag> CrearAsync(Tag tag)
        {
            // Validamos que no nos envíen tags en blanco
            if (string.IsNullOrWhiteSpace(tag.Nombre))
            {
                throw new ArgumentException("El nombre del tag no puede estar vacío.");
            }

            // Opcional: Podrías forzar a que todos los tags se guarden en minúsculas 
            // para evitar duplicados visuales (ej. "C#" vs "c#").
            tag.Nombre = tag.Nombre.Trim().ToLower();

            return await _repository.CrearAsync(tag);
        }
    }
}