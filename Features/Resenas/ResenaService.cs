using Microsoft.Extensions.Configuration;
using TiempoBiblia.Api.Features.Correos;

namespace TiempoBiblia.Api.Features.Resenas
{
    public class ResenaService
    {
        private readonly ResenaRepository _repository;
        private readonly EmailService _emailService;
        private readonly IConfiguration _config;

        public ResenaService(ResenaRepository repository, EmailService emailService, IConfiguration config)
        {
            _repository = repository;
            _emailService = emailService;
            _config = config;
        }

        public async Task<List<ResenaDto>> ObtenerPorProductoAsync(int productoId) => 
            await _repository.ObtenerPorProductoAsync(productoId);

        public async Task<List<ResenaDto>> ObtenerTodasAdminAsync(int productoId) => 
            await _repository.ObtenerTodasAdminAsync(productoId);

        public async Task ActualizarEstadoAsync(int id, bool estado) => 
            await _repository.ActualizarEstadoAsync(id, estado);

        public async Task CrearResenaAsync(CrearResenaDto dto)
        {
            // 1. Mapeo a la entidad
            var nuevaResena = new Resena 
            { 
                ProductoId = dto.ProductoId, 
                NombreCliente = dto.NombreCliente, 
                Calificacion = dto.Calificacion, 
                Comentario = dto.Comentario, 
                Aprobada = false // Nace inactiva
            };
            
            // 2. Guardar en Base de Datos
            await _repository.GuardarAsync(nuevaResena);
            
            // 3. Disparar el correo leyendo desde la configuración (No más hardcodeo)
            string miCorreo = _config["BackendSettings:AdminEmail"] ?? "luzyyewa@gmail.com"; 
            string apiUrl = _config["BackendSettings:BaseUrl"] ?? "https://tiempobiblia-luzy.online";
            
            _ = _emailService.EnviarAlertaNuevaResenaAsync(miCorreo, nuevaResena, apiUrl);
        }

        public async Task CrearResenaGlobalAsync(CrearResenaGlobalDto dto)
        {
            // Pasa la responsabilidad al repositorio
            await _repository.MultiplicarResenaGlobalAsync(dto.PedidoId, dto.NombreCliente, dto.Calificacion, dto.Comentario);
        }
    }
}