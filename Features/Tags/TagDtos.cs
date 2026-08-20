namespace TiempoBiblia.Api.Features.Tags
{
    public class TagDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
    }
    
    public class ProductoTagDto
    {
        public int TagId { get; set; }
        public TagDto Tag { get; set; } = new();
    }
    
}