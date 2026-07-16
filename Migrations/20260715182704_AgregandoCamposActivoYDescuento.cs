using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiempoBiblia.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregandoCamposActivoYDescuento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TokensDescargas");

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Tags",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Descuento",
                table: "Productos",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Paquetes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Descuento",
                table: "Paquetes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Categorias",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "Descuento",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Paquetes");

            migrationBuilder.DropColumn(
                name: "Descuento",
                table: "Paquetes");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Categorias");

            migrationBuilder.CreateTable(
                name: "TokensDescargas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductoId = table.Column<int>(type: "integer", nullable: false),
                    CorreoCliente = table.Column<string>(type: "text", nullable: false),
                    DescargasRealizadas = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LimiteDescargas = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokensDescargas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokensDescargas_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TokensDescargas_ProductoId",
                table: "TokensDescargas",
                column: "ProductoId");
        }
    }
}
