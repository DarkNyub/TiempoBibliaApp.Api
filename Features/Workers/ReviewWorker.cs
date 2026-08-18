using Microsoft.EntityFrameworkCore;
using TiempoBiblia.Api.Data;
using TiempoBiblia.Api.Features.Correos;

namespace TiempoBiblia.Api.Workers
{
    /// <summary>
    /// Servicio en segundo plano que revisa periódicamente los pedidos antiguos
    /// para enviar el correo automático de solicitud de reseñas.
    /// </summary>
    public class ReviewWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReviewWorker> _logger;

        public ReviewWorker(IServiceProvider serviceProvider, ILogger<ReviewWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 ReviewWorker de Fidelización iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Creamos un alcance (Scope) porque el Worker es un proceso infinito (Singleton),
                    // pero la base de datos (AppDbContext) vive por peticiones cortas (Scoped).
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

                    // Calculamos la barrera de tiempo: Pedidos de hace 48 horas exactas o más
                    var fechaLimite = DateTime.UtcNow.AddHours(-48);

                    // Buscamos pedidos pagados con éxito a los que no se les haya enviado la encuesta
                    var pedidosPendientes = await context.Pedidos
                        .Include(p => p.Detalles)
                        .Where(p => p.Estado == "approved" 
                                    && p.EncuestaEnviada == false 
                                    && p.FechaCreacion <= fechaLimite)
                        .ToListAsync(stoppingToken);

                    foreach (var pedido in pedidosPendientes)
                    {
                        var nombresProductos = pedido.Detalles.Select(d => d.NombreProductoHistorico).ToList();
                        
                        // Enviamos el correo
                        await emailService.EnviarCorreoFidelizacionAsync(pedido.CorreoCliente, pedido.Id, nombresProductos);

                        // Marcamos como enviado para no hacer spam mañana
                        pedido.EncuestaEnviada = true;
                        _logger.LogInformation($"✅ Encuesta enviada al pedido {pedido.Id} ({pedido.CorreoCliente})");
                    }

                    if (pedidosPendientes.Any())
                    {
                        await context.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Ocurrió un error en el ReviewWorker.");
                }

                // El robot se duerme y vuelve a revisar en 12 horas.
                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
            }
        }
    }
}