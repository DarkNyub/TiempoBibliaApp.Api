using Microsoft.EntityFrameworkCore;
using TiempoBiblia.Api.Data;
using TiempoBiblia.Api.Features.Categorias;
using TiempoBiblia.Api.Features.Paquetes;
using TiempoBiblia.Api.Features.Productos;
using TiempoBiblia.Api.Features.Tags;
// 🔥 NUEVO: Importamos el espacio de nombres de Descargas
using TiempoBiblia.Api.Features.Descargas;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. CONFIGURACIÓN DE SERVICIOS (Inyección de Dependencias)
// ============================================================

// Base de Datos: Conexión robusta a PostgreSQL (Neon.tech)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🔥 NUEVO: Registramos HttpClient para poder comunicarnos con Google Drive
builder.Services.AddHttpClient();

// Registro de dependencias por Features (Clases concretas, sin interfaces)
builder.Services.AddScoped<CategoriaRepository>();
builder.Services.AddScoped<CategoriaService>();

builder.Services.AddScoped<PaqueteRepository>();
builder.Services.AddScoped<PaqueteService>();

builder.Services.AddScoped<ProductoRepository>();
builder.Services.AddScoped<ProductoService>();

builder.Services.AddScoped<TagRepository>();
builder.Services.AddScoped<TagService>();

// 🔥 NUEVO: Registro de los servicios para la bóveda de Descargas Seguras
builder.Services.AddScoped<DescargaRepository>();
builder.Services.AddScoped<DescargaService>();

// Controladores: Habilitamos la arquitectura MVC para APIs estructuradas
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Evita el bucle infinito al serializar JSON con relaciones
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// 🔥 1. AGREGAMOS CORS AQUÍ (El pase VIP para tu Frontend) 🔥
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirFrontend", policy =>
    {
        policy.AllowAnyOrigin()  // Permite peticiones desde cualquier origen
              .AllowAnyMethod()  // Permite usar GET, POST, PUT, DELETE, etc.
              .AllowAnyHeader(); // Permite cualquier cabecera de seguridad
    });
});

// Documentación: Activamos Swagger para tener una UI de pruebas impecable
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() 
    { 
        Title = "TiempoBiblia API", 
        Version = "v1",
        Description = "API Core para la Biblioteca Digital Tiempo Biblia"
    });
});

var app = builder.Build();

// ============================================================
// 2. PIPELINE HTTP (Middlewares)
// ============================================================

// Activamos Swagger UI cuando estamos en modo de desarrollo
// if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TiempoBiblia API v1");
        // Opcional pero recomendado: haz que Swagger sea la página principal en la nube
        c.RoutePrefix = string.Empty;
    });
}

// Redirección segura a HTTPS
app.UseHttpsRedirection();

// 🔥 2. ACTIVAMOS EL MIDDLEWARE DE CORS AQUÍ (¡Justo antes de MapControllers!) 🔥
app.UseCors("PermitirFrontend");

// Mapeo automático de todos los controladores que crearemos
app.MapControllers();

app.Run();