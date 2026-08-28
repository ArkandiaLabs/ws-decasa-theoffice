using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TheOffice.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductImagesAndVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PublicId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPrimary = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PublicId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Price = table.Column<double>(type: "REAL", nullable: false),
                    Stock = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariants_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ProductImages",
                columns: new[] { "Id", "IsPrimary", "ProductId", "PublicId", "SortOrder", "Url" },
                values: new object[,]
                {
                    { new Guid("b1000000-0000-4000-8000-000000000001"), true, new Guid("b0000000-0000-4000-8000-000000000001"), "PRD-001-IMG-1", 0, "https://placehold.co/600x400/png?text=Resma%20de%20papel%20carta%2075g" },
                    { new Guid("b1000000-0000-4000-8000-000000000002"), true, new Guid("b0000000-0000-4000-8000-000000000002"), "PRD-002-IMG-1", 0, "https://placehold.co/600x400/png?text=Cuaderno%20argollado%20100%20hojas" },
                    { new Guid("b1000000-0000-4000-8000-000000000003"), true, new Guid("b0000000-0000-4000-8000-000000000003"), "PRD-003-IMG-1", 0, "https://placehold.co/600x400/png?text=Boligrafo%20tinta%20negra%20x12" },
                    { new Guid("b1000000-0000-4000-8000-000000000004"), true, new Guid("b0000000-0000-4000-8000-000000000004"), "PRD-004-IMG-1", 0, "https://placehold.co/600x400/png?text=Marcador%20borrable%20x4" },
                    { new Guid("b1000000-0000-4000-8000-000000000005"), true, new Guid("b0000000-0000-4000-8000-000000000005"), "PRD-005-IMG-1", 0, "https://placehold.co/600x400/png?text=Silla%20ergonomica%20con%20soporte%20lumbar" },
                    { new Guid("b1000000-0000-4000-8000-000000000006"), true, new Guid("b0000000-0000-4000-8000-000000000006"), "PRD-006-IMG-1", 0, "https://placehold.co/600x400/png?text=Escritorio%20en%20L%20150%20cm" },
                    { new Guid("b1000000-0000-4000-8000-000000000007"), true, new Guid("b0000000-0000-4000-8000-000000000007"), "PRD-007-IMG-1", 0, "https://placehold.co/600x400/png?text=Archivador%20metalico%204%20gavetas" },
                    { new Guid("b1000000-0000-4000-8000-000000000008"), true, new Guid("b0000000-0000-4000-8000-000000000008"), "PRD-008-IMG-1", 0, "https://placehold.co/600x400/png?text=Cajonera%20rodante%203%20gavetas" },
                    { new Guid("b1000000-0000-4000-8000-000000000009"), true, new Guid("b0000000-0000-4000-8000-000000000009"), "PRD-009-IMG-1", 0, "https://placehold.co/600x400/png?text=Monitor%2027%20pulgadas%20QHD" },
                    { new Guid("b1000000-0000-4000-8000-000000000010"), true, new Guid("b0000000-0000-4000-8000-000000000010"), "PRD-010-IMG-1", 0, "https://placehold.co/600x400/png?text=Teclado%20mecanico%20inalambrico" },
                    { new Guid("b1000000-0000-4000-8000-000000000011"), true, new Guid("b0000000-0000-4000-8000-000000000011"), "PRD-011-IMG-1", 0, "https://placehold.co/600x400/png?text=Diadema%20con%20cancelacion%20de%20ruido" },
                    { new Guid("b1000000-0000-4000-8000-000000000012"), true, new Guid("b0000000-0000-4000-8000-000000000012"), "PRD-012-IMG-1", 0, "https://placehold.co/600x400/png?text=Base%20refrigerante%20para%20portatil" },
                    { new Guid("b1000000-0000-4000-8000-000000000013"), true, new Guid("b0000000-0000-4000-8000-000000000013"), "PRD-013-IMG-1", 0, "https://placehold.co/600x400/png?text=Organizador%20de%20escritorio%205%20compartimentos" },
                    { new Guid("b1000000-0000-4000-8000-000000000014"), true, new Guid("b0000000-0000-4000-8000-000000000014"), "PRD-014-IMG-1", 0, "https://placehold.co/600x400/png?text=Caja%20de%20archivo%20tapa%20fija%20x10" },
                    { new Guid("b1000000-0000-4000-8000-000000000015"), true, new Guid("b0000000-0000-4000-8000-000000000015"), "PRD-015-IMG-1", 0, "https://placehold.co/600x400/png?text=Notas%20adhesivas%20x6" },
                    { new Guid("b1000000-0000-4000-8000-000000000016"), true, new Guid("b0000000-0000-4000-8000-000000000016"), "PRD-016-IMG-1", 0, "https://placehold.co/600x400/png?text=Perforadora%20industrial%2030%20hojas" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId",
                table: "ProductImages",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_PublicId",
                table: "ProductImages",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId",
                table: "ProductVariants",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_PublicId",
                table: "ProductVariants",
                column: "PublicId",
                unique: true);

            // Backfill de las filas que no vienen del seeder: un producto creado por
            // POST /api/v1/products tiene ImageUrl pero ningun HasData que lo respalde, y
            // el DropColumn de abajo se la llevaria sin dejar rastro. El differ no ordena
            // esto solo: tiene que quedar despues de crear la tabla y antes de dropear.
            migrationBuilder.Sql(
                @"INSERT INTO ProductImages (Id, PublicId, Url, SortOrder, IsPrimary, ProductId)
                  SELECT
                    upper(
                      hex(randomblob(4)) || '-' ||
                      hex(randomblob(2)) || '-4' ||
                      substr(hex(randomblob(2)), 2) || '-' ||
                      substr('89AB', abs(random()) % 4 + 1, 1) ||
                      substr(hex(randomblob(2)), 2) || '-' ||
                      hex(randomblob(6))),
                    p.PublicId || '-IMG-1',
                    p.ImageUrl,
                    0,
                    1,
                    p.Id
                  FROM Products p
                  WHERE p.ImageUrl IS NOT NULL
                    AND p.ImageUrl <> ''
                    AND p.Id NOT IN (SELECT ProductId FROM ProductImages);");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductImages");

            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-4000-8000-000000000001"),
                column: "ImageUrl",
                value: "https://placehold.co/600x400/png?text=Resma%20de%20papel%20carta%2075g");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-4000-8000-000000000002"),
                column: "ImageUrl",
                value: "https://placehold.co/600x400/png?text=Cuaderno%20argollado%20100%20hojas");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-4000-8000-000000000003"),
                column: "ImageUrl",
                value: "https://placehold.co/600x400/png?text=Boligrafo%20tinta%20negra%20x12");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-4000-8000-000000000004"),
                column: "ImageUrl",
                value: "https://placehold.co/600x400/png?text=Marcador%20borrable%20x4");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-4000-8000-000000000005"),
                column: "ImageUrl",
                value: "https://placehold.co/600x400/png?text=Silla%20ergonomica%20con%20soporte%20lumbar");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-4000-8000-000000000006"),
                column: "ImageUrl",
                value: "https://placehold.co/600x400/png?text=Escritorio%20en%20L%20150%20cm");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-4000-8000-000000000007"),
                column: "ImageUrl",
                value: "https://placehold.co/600x400/png?text=Archivador%20metalico%204%20gavetas");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-4000-8000-000000000008"),
                column: "ImageUrl",
                value: "https://placehold.co/600x400/png?text=Cajonera%20rodante%203%20gavetas");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-4000-8000-000000000009"),
                column: "ImageUrl",
                value: "https://placehold.co/600x400/png?text=Monitor%2027%20pulgadas%20QHD");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-4000-8000-000000000010"),
                column: "ImageUrl",
                value: "https://placehold.co/600x400/png?text=Teclado%20mecanico%20inalambrico");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-4000-8000-000000000011"),
                column: "ImageUrl",
                value: "https://placehold.co/600x400/png?text=Diadema%20con%20cancelacion%20de%20ruido");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-4000-8000-000000000012"),
                column: "ImageUrl",
                value: "https://placehold.co/600x400/png?text=Base%20refrigerante%20para%20portatil");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-4000-8000-000000000013"),
                column: "ImageUrl",
                value: "https://placehold.co/600x400/png?text=Organizador%20de%20escritorio%205%20compartimentos");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-4000-8000-000000000014"),
                column: "ImageUrl",
                value: "https://placehold.co/600x400/png?text=Caja%20de%20archivo%20tapa%20fija%20x10");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-4000-8000-000000000015"),
                column: "ImageUrl",
                value: "https://placehold.co/600x400/png?text=Notas%20adhesivas%20x6");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-4000-8000-000000000016"),
                column: "ImageUrl",
                value: "https://placehold.co/600x400/png?text=Perforadora%20industrial%2030%20hojas");
        }
    }
}
