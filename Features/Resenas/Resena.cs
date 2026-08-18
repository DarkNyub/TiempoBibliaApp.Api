using System.ComponentModel.DataAnnotations;

namespace TiempoBiblia.Api.Features.Resenas
{
    /// <summary>
    /// Representa la calificación y comentario de un cliente sobre un producto específico.
    /// Funciona como motor de prueba social (Social Proof).
    /// </summary>
    public class Resena
    {
        public int Id { get; set; }
        
        [Required]
        public int ProductoId { get; set; }
        
        [Required, MaxLength(100)]
        public string NombreCliente { get; set; } = string.Empty;

        [Required, Range(1, 5)]
        public int Calificacion { get; set; } // 1 a 5 estrellas

        [Required, MaxLength(1000)]
        public string Comentario { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Permite ocultar reseñas ofensivas o spam en el futuro sin borrarlas.
        /// </summary>
        public bool Aprobada { get; set; } = true; 
    }
}