using Microsoft.EntityFrameworkCore;
using TiempoBiblia.Api.Data;
using TiempoBiblia.Api.Features.Categorias;
using TiempoBiblia.Api.Features.Paquetes;
using TiempoBiblia.Api.Features.Productos;
using TiempoBiblia.Api.Features.Tags;
using TiempoBiblia.Api.Features.Descargas;
using TiempoBiblia.Api.Features.Correos;
using TiempoBiblia.Api.Features.Checkout; // 🔥 NUEVO: Para el Checkout
using TiempoBiblia.Api.Features.Pedidos;  // 🔥 NUEVO: Para la Auditoría
using TiempoBiblia.Api.Features.Resenas;  // 🔥 NUEVO: Para las Reseñas
using TiempoBiblia.Api.Workers; // 🔥 NUEVO: Para el Worker de Fidelización

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. CONFIGURACIÓN DE SERVICIOS (Inyección de Dependencias)
// ============================================================

// Base de Datos: Conexión robusta a PostgreSQL (Neon.tech)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registramos HttpClient para poder comunicarnos con Google Drive
builder.Services.AddHttpClient();

// Registro de dependencias por Features
builder.Services.AddScoped<CategoriaRepository>();
builder.Services.AddScoped<CategoriaService>();

builder.Services.AddScoped<PaqueteRepository>();
builder.Services.AddScoped<PaqueteService>();

builder.Services.AddScoped<ProductoRepository>();
builder.Services.AddScoped<ProductoService>();

builder.Services.AddScoped<TagRepository>();
builder.Services.AddScoped<TagService>();

builder.Services.AddScoped<DescargaRepository>();
builder.Services.AddScoped<DescargaService>();

builder.Services.AddScoped<EmailService>();

// 🔥 NUEVO: REGISTRO DEL MOTOR DE PAGOS Y AUDITORÍA
builder.Services.AddScoped<PedidoRepository>();
builder.Services.AddScoped<CheckoutService>();

builder.Services.AddScoped<ResenaRepository>();

builder.Services.AddHostedService<ReviewWorker>();

// Controladores: Habilitamos la arquitectura MVC
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// 1. AGREGAMOS CORS AQUÍ (El pase VIP)
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirFrontend", policy =>
    {
        policy.AllowAnyOrigin()  
              .AllowAnyMethod()  
              .AllowAnyHeader(); 
    });
});

// Documentación: Activamos Swagger
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

builder.Services.AddHttpClient();

var app = builder.Build();

// ============================================================
// 2. PIPELINE HTTP (Middlewares)
// ============================================================

// if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TiempoBiblia API v1");
        c.RoutePrefix = string.Empty;
    });
}

// Redirección segura a HTTPS
app.UseHttpsRedirection();

// 2. ACTIVAMOS EL MIDDLEWARE DE CORS AQUÍ
app.UseCors("PermitirFrontend");

app.MapControllers();

app.Run();