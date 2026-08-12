using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiempoBiblia.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarOrdenaCategorias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Orden",
                table: "Categorias",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Orden",
                table: "Categorias");
        }
    }
}
