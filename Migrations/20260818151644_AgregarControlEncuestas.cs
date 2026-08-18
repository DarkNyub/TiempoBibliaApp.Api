using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiempoBiblia.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarControlEncuestas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EncuestaEnviada",
                table: "Pedidos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Resenas_ProductoId",
                table: "Resenas",
                column: "ProductoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Resenas_Productos_ProductoId",
                table: "Resenas",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resenas_Productos_ProductoId",
                table: "Resenas");

            migrationBuilder.DropIndex(
                name: "IX_Resenas_ProductoId",
                table: "Resenas");

            migrationBuilder.DropColumn(
                name: "EncuestaEnviada",
                table: "Pedidos");
        }
    }
}
