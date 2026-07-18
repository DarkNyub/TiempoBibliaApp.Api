using Microsoft.EntityFrameworkCore;
using TiempoBiblia.Api.Features.Categorias;
using TiempoBiblia.Api.Features.Paquetes;
using TiempoBiblia.Api.Features.Productos;
using TiempoBiblia.Api.Features.Tags;
using TiempoBiblia.Api.Features.Relaciones;
using TiempoBiblia.Api.Features.Descargas;
using TiempoBiblia.Api.shared;

namespace TiempoBiblia.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Producto> Productos { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<ProductoTag> ProductoTags { get; set; }
        public DbSet<Paquete> Paquetes { get; set; }
        public DbSet<PaqueteProducto> PaqueteProductos { get; set; }
        public DbSet<ProductoRelacionado> ProductosRelacionados { get; set; }
        public DbSet<ProductoCategoriaSecundaria> ProductoCategoriasSecundarias { get; set; }
        public DbSet<TokenDescarga> TokensDescarga { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Llaves primarias compuestas
            modelBuilder.Entity<ProductoTag>()
                .HasKey(pt => new { pt.ProductoId, pt.TagId });

            modelBuilder.Entity<PaqueteProducto>()
                .HasKey(pp => new { pp.PaqueteId, pp.ProductoId });

            modelBuilder.Entity<ProductoRelacionado>()
                .HasKey(pr => new { pr.ProductoOrigenId, pr.ProductoRelacionadoId });

            // 2. Configurar la relación de productos recomendados
            modelBuilder.Entity<ProductoRelacionado>()
                .HasOne(pr => pr.ProductoOrigen)
                .WithMany(p => p.ProductosRelacionadosOrigen)
                .HasForeignKey(pr => pr.ProductoOrigenId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductoRelacionado>()
                .HasOne(pr => pr.ProductoRelacionadoDestino)
                .WithMany(p => p.ProductosRelacionadosDestino)
                .HasForeignKey(pr => pr.ProductoRelacionadoId)
                .OnDelete(DeleteBehavior.Restrict);

            // NUEVO: Llave compuesta para las categorías secundarias
            modelBuilder.Entity<ProductoCategoriaSecundaria>()
                .HasKey(pcs => new { pcs.ProductoId, pcs.CategoriaId });
        }
    }
}