using System.ComponentModel.DataAnnotations;

namespace TiempoBiblia.Api.Features.Resenas
{
    // DTO para enviar al Frontend (Lectura)
    public class ResenaDto
    {
        public int Id { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public int Calificacion { get; set; }
        public string Comentario { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
    }

    // DTO para recibir del Frontend (Escritura)
    public class CrearResenaDto
    {
        public int ProductoId { get; set; }
        
        [Required(ErrorMessage = "Tu nombre es obligatorio.")]
        [MaxLength(100)]
        public string NombreCliente { get; set; } = string.Empty;

        [Range(1, 5, ErrorMessage = "Debes seleccionar entre 1 y 5 estrellas.")]
        public int Calificacion { get; set; }

        [Required(ErrorMessage = "Por favor, cuéntanos qué te pareció el recurso.")]
        [MaxLength(1000)]
        public string Comentario { get; set; } = string.Empty;
    }
    // DTO para recibir una calificación general que se aplicará a múltiples productos
    public class CrearResenaGlobalDto
    {
        public int PedidoId { get; set; }
        public int Calificacion { get; set; }
        public string Comentario { get; set; } = string.Empty;
    }
}