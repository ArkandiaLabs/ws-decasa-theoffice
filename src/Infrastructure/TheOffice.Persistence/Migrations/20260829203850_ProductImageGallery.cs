using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TheOffice.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductImageGallery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ProductImages",
                columns: new[] { "Id", "IsPrimary", "ProductId", "PublicId", "SortOrder", "Url" },
                values: new object[,]
                {
                    { new Guid("b1000000-0000-4000-8000-000000000101"), false, new Guid("b0000000-0000-4000-8000-000000000001"), "PRD-001-IMG-2", 1, "https://placehold.co/600x400/png?text=Resma%20de%20papel%20carta%2075g%20-%20detalle" },
                    { new Guid("b1000000-0000-4000-8000-000000000102"), false, new Guid("b0000000-0000-4000-8000-000000000002"), "PRD-002-IMG-2", 1, "https://placehold.co/600x400/png?text=Cuaderno%20argollado%20100%20hojas%20-%20detalle" },
                    { new Guid("b1000000-0000-4000-8000-000000000104"), false, new Guid("b0000000-0000-4000-8000-000000000004"), "PRD-004-IMG-2", 1, "https://placehold.co/600x400/png?text=Marcador%20borrable%20x4%20-%20detalle" },
                    { new Guid("b1000000-0000-4000-8000-000000000105"), false, new Guid("b0000000-0000-4000-8000-000000000005"), "PRD-005-IMG-2", 1, "https://placehold.co/600x400/png?text=Silla%20ergonomica%20con%20soporte%20lumbar%20-%20detalle" },
                    { new Guid("b1000000-0000-4000-8000-000000000106"), false, new Guid("b0000000-0000-4000-8000-000000000006"), "PRD-006-IMG-2", 1, "https://placehold.co/600x400/png?text=Escritorio%20en%20L%20150%20cm%20-%20detalle" },
                    { new Guid("b1000000-0000-4000-8000-000000000107"), false, new Guid("b0000000-0000-4000-8000-000000000007"), "PRD-007-IMG-2", 1, "https://placehold.co/600x400/png?text=Archivador%20metalico%204%20gavetas%20-%20detalle" },
                    { new Guid("b1000000-0000-4000-8000-000000000108"), false, new Guid("b0000000-0000-4000-8000-000000000008"), "PRD-008-IMG-2", 1, "https://placehold.co/600x400/png?text=Cajonera%20rodante%203%20gavetas%20-%20detalle" },
                    { new Guid("b1000000-0000-4000-8000-000000000109"), false, new Guid("b0000000-0000-4000-8000-000000000009"), "PRD-009-IMG-2", 1, "https://placehold.co/600x400/png?text=Monitor%2027%20pulgadas%20QHD%20-%20detalle" },
                    { new Guid("b1000000-0000-4000-8000-000000000110"), false, new Guid("b0000000-0000-4000-8000-000000000010"), "PRD-010-IMG-2", 1, "https://placehold.co/600x400/png?text=Teclado%20mecanico%20inalambrico%20-%20detalle" },
                    { new Guid("b1000000-0000-4000-8000-000000000111"), false, new Guid("b0000000-0000-4000-8000-000000000011"), "PRD-011-IMG-2", 1, "https://placehold.co/600x400/png?text=Diadema%20con%20cancelacion%20de%20ruido%20-%20detalle" },
                    { new Guid("b1000000-0000-4000-8000-000000000112"), false, new Guid("b0000000-0000-4000-8000-000000000012"), "PRD-012-IMG-2", 1, "https://placehold.co/600x400/png?text=Base%20refrigerante%20para%20portatil%20-%20detalle" },
                    { new Guid("b1000000-0000-4000-8000-000000000113"), false, new Guid("b0000000-0000-4000-8000-000000000013"), "PRD-013-IMG-2", 1, "https://placehold.co/600x400/png?text=Organizador%20de%20escritorio%205%20compartimentos%20-%20detalle" },
                    { new Guid("b1000000-0000-4000-8000-000000000114"), false, new Guid("b0000000-0000-4000-8000-000000000014"), "PRD-014-IMG-2", 1, "https://placehold.co/600x400/png?text=Caja%20de%20archivo%20tapa%20fija%20x10%20-%20detalle" },
                    { new Guid("b1000000-0000-4000-8000-000000000116"), false, new Guid("b0000000-0000-4000-8000-000000000016"), "PRD-016-IMG-2", 1, "https://placehold.co/600x400/png?text=Perforadora%20industrial%2030%20hojas%20-%20detalle" },
                    { new Guid("b1000000-0000-4000-8000-000000000201"), false, new Guid("b0000000-0000-4000-8000-000000000001"), "PRD-001-IMG-3", 2, "https://placehold.co/600x400/png?text=Resma%20de%20papel%20carta%2075g%20-%20en%20uso" },
                    { new Guid("b1000000-0000-4000-8000-000000000202"), false, new Guid("b0000000-0000-4000-8000-000000000002"), "PRD-002-IMG-3", 2, "https://placehold.co/600x400/png?text=Cuaderno%20argollado%20100%20hojas%20-%20en%20uso" },
                    { new Guid("b1000000-0000-4000-8000-000000000204"), false, new Guid("b0000000-0000-4000-8000-000000000004"), "PRD-004-IMG-3", 2, "https://placehold.co/600x400/png?text=Marcador%20borrable%20x4%20-%20en%20uso" },
                    { new Guid("b1000000-0000-4000-8000-000000000205"), false, new Guid("b0000000-0000-4000-8000-000000000005"), "PRD-005-IMG-3", 2, "https://placehold.co/600x400/png?text=Silla%20ergonomica%20con%20soporte%20lumbar%20-%20en%20uso" },
                    { new Guid("b1000000-0000-4000-8000-000000000206"), false, new Guid("b0000000-0000-4000-8000-000000000006"), "PRD-006-IMG-3", 2, "https://placehold.co/600x400/png?text=Escritorio%20en%20L%20150%20cm%20-%20en%20uso" },
                    { new Guid("b1000000-0000-4000-8000-000000000207"), false, new Guid("b0000000-0000-4000-8000-000000000007"), "PRD-007-IMG-3", 2, "https://placehold.co/600x400/png?text=Archivador%20metalico%204%20gavetas%20-%20en%20uso" },
                    { new Guid("b1000000-0000-4000-8000-000000000208"), false, new Guid("b0000000-0000-4000-8000-000000000008"), "PRD-008-IMG-3", 2, "https://placehold.co/600x400/png?text=Cajonera%20rodante%203%20gavetas%20-%20en%20uso" },
                    { new Guid("b1000000-0000-4000-8000-000000000209"), false, new Guid("b0000000-0000-4000-8000-000000000009"), "PRD-009-IMG-3", 2, "https://placehold.co/600x400/png?text=Monitor%2027%20pulgadas%20QHD%20-%20en%20uso" },
                    { new Guid("b1000000-0000-4000-8000-000000000210"), false, new Guid("b0000000-0000-4000-8000-000000000010"), "PRD-010-IMG-3", 2, "https://placehold.co/600x400/png?text=Teclado%20mecanico%20inalambrico%20-%20en%20uso" },
                    { new Guid("b1000000-0000-4000-8000-000000000211"), false, new Guid("b0000000-0000-4000-8000-000000000011"), "PRD-011-IMG-3", 2, "https://placehold.co/600x400/png?text=Diadema%20con%20cancelacion%20de%20ruido%20-%20en%20uso" },
                    { new Guid("b1000000-0000-4000-8000-000000000212"), false, new Guid("b0000000-0000-4000-8000-000000000012"), "PRD-012-IMG-3", 2, "https://placehold.co/600x400/png?text=Base%20refrigerante%20para%20portatil%20-%20en%20uso" },
                    { new Guid("b1000000-0000-4000-8000-000000000213"), false, new Guid("b0000000-0000-4000-8000-000000000013"), "PRD-013-IMG-3", 2, "https://placehold.co/600x400/png?text=Organizador%20de%20escritorio%205%20compartimentos%20-%20en%20uso" },
                    { new Guid("b1000000-0000-4000-8000-000000000214"), false, new Guid("b0000000-0000-4000-8000-000000000014"), "PRD-014-IMG-3", 2, "https://placehold.co/600x400/png?text=Caja%20de%20archivo%20tapa%20fija%20x10%20-%20en%20uso" },
                    { new Guid("b1000000-0000-4000-8000-000000000216"), false, new Guid("b0000000-0000-4000-8000-000000000016"), "PRD-016-IMG-3", 2, "https://placehold.co/600x400/png?text=Perforadora%20industrial%2030%20hojas%20-%20en%20uso" },
                    { new Guid("b1000000-0000-4000-8000-000000000305"), false, new Guid("b0000000-0000-4000-8000-000000000005"), "PRD-005-IMG-4", 3, "https://placehold.co/600x400/png?text=Silla%20ergonomica%20con%20soporte%20lumbar%20-%20empaque" },
                    { new Guid("b1000000-0000-4000-8000-000000000309"), false, new Guid("b0000000-0000-4000-8000-000000000009"), "PRD-009-IMG-4", 3, "https://placehold.co/600x400/png?text=Monitor%2027%20pulgadas%20QHD%20-%20empaque" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000101"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000102"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000104"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000105"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000106"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000107"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000108"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000109"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000110"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000111"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000112"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000113"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000114"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000116"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000201"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000202"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000204"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000205"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000206"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000207"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000208"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000209"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000210"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000211"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000212"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000213"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000214"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000216"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000305"));

            migrationBuilder.DeleteData(
                table: "ProductImages",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-000000000309"));
        }
    }
}
