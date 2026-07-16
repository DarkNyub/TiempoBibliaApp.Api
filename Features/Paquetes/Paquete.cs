using System.ComponentModel.DataAnnotations;
using TiempoBiblia.Api.Features.Relaciones; // Asumiendo que aquí metimos las tablas intermedias

namespace TiempoBiblia.Api.Features.Paquetes
{
    /// <summary>
    /// Representa una agrupación de productos que se venden juntos, generalmente con un precio especial.
    /// </summary>
    public class Paquete
    {
        public int Id { get; set; }
        
        [Required, MaxLength(200)]
        public string Nombre { get; set; } = string.Empty;
        
        public string Descripcion { get; set; } = string.Empty;
        
        /// <summary>
        /// Mantenemos decimal para la BD. El frontend recibe esto y hace su magia visual.
        /// </summary>
        public decimal Precio { get; set; } 

        /// <summary>
        /// Puede ser un porcentaje (ej. 15.00 para 15%). El cálculo lo hará el frontend.
        /// </summary>
        public decimal Descuento { get; set; } = 0; 
        
        public string ImagenUrl { get; set; } = string.Empty;

        /// <summary>
        /// Bandera para soft-delete. False oculta el paquete de la tienda pública.
        /// </summary>
        public bool Activo { get; set; } = true;

        // Relación Muchos a Muchos con Productos
        public ICollection<PaqueteProducto> PaqueteProductos { get; set; } = new List<PaqueteProducto>();
    }
}