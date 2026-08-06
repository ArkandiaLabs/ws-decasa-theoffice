using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TheOffice.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PublicId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PublicId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PublicId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Price = table.Column<double>(type: "REAL", nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Stock = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Description", "Name", "PublicId", "Slug" },
                values: new object[,]
                {
                    { new Guid("c0000000-0000-4000-8000-000000000001"), "Papel, cuadernos y utiles de escritura para el dia a dia de la oficina.", "Papeleria", "CAT-001", "papeleria" },
                    { new Guid("c0000000-0000-4000-8000-000000000002"), "Sillas, escritorios y archivadores para espacios de trabajo.", "Mobiliario", "CAT-002", "mobiliario" },
                    { new Guid("c0000000-0000-4000-8000-000000000003"), "Monitores, perifericos y accesorios para puestos de trabajo digitales.", "Tecnologia", "CAT-003", "tecnologia" },
                    { new Guid("c0000000-0000-4000-8000-000000000004"), "Archivo, clasificacion y orden del puesto de trabajo.", "Organizacion", "CAT-004", "organizacion" }
                });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "Email", "Name", "PublicId", "Source" },
                values: new object[,]
                {
                    { new Guid("a0000000-0000-4000-8000-000000000001"), "laura.restrepo@example.com", "Laura Restrepo", "CUS-001", 0 },
                    { new Guid("a0000000-0000-4000-8000-000000000002"), "andres.gomez@example.com", "Andres Gomez", "CUS-002", 3 },
                    { new Guid("a0000000-0000-4000-8000-000000000003"), "marcela.rios@example.com", "Marcela Rios", "CUS-003", 1 }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "CategoryId", "Description", "ImageUrl", "IsActive", "Name", "Price", "PublicId", "Stock" },
                values: new object[,]
                {
                    { new Guid("b0000000-0000-4000-8000-000000000001"), new Guid("c0000000-0000-4000-8000-000000000001"), "Resma de 500 hojas tamano carta, 75 gramos, blancura 96%.", "https://placehold.co/600x400/png?text=Resma%20de%20papel%20carta%2075g", true, "Resma de papel carta 75g", 18900.0, "PRD-001", 120 },
                    { new Guid("b0000000-0000-4000-8000-000000000002"), new Guid("c0000000-0000-4000-8000-000000000001"), "Cuaderno argollado tamano carta, 100 hojas cuadriculadas, pasta dura.", "https://placehold.co/600x400/png?text=Cuaderno%20argollado%20100%20hojas", true, "Cuaderno argollado 100 hojas", 12500.0, "PRD-002", 200 },
                    { new Guid("b0000000-0000-4000-8000-000000000003"), new Guid("c0000000-0000-4000-8000-000000000001"), "Caja por 12 boligrafos de tinta negra, punta media de 1.0 mm.", "https://placehold.co/600x400/png?text=Boligrafo%20tinta%20negra%20x12", true, "Boligrafo tinta negra x12", 9800.0, "PRD-003", 350 },
                    { new Guid("b0000000-0000-4000-8000-000000000004"), new Guid("c0000000-0000-4000-8000-000000000001"), "Set de 4 marcadores borrables en seco para tablero acrilico.", "https://placehold.co/600x400/png?text=Marcador%20borrable%20x4", true, "Marcador borrable x4", 15400.0, "PRD-004", 90 },
                    { new Guid("b0000000-0000-4000-8000-000000000005"), new Guid("c0000000-0000-4000-8000-000000000002"), "Silla de malla transpirable con soporte lumbar ajustable y apoyabrazos 3D.", "https://placehold.co/600x400/png?text=Silla%20ergonomica%20con%20soporte%20lumbar", true, "Silla ergonomica con soporte lumbar", 689000.0, "PRD-005", 25 },
                    { new Guid("b0000000-0000-4000-8000-000000000006"), new Guid("c0000000-0000-4000-8000-000000000002"), "Escritorio en L de 150 x 120 cm en madera laminada con pasacables.", "https://placehold.co/600x400/png?text=Escritorio%20en%20L%20150%20cm", true, "Escritorio en L 150 cm", 899000.0, "PRD-006", 12 },
                    { new Guid("b0000000-0000-4000-8000-000000000007"), new Guid("c0000000-0000-4000-8000-000000000002"), "Archivador metalico vertical de 4 gavetas con cerradura central.", "https://placehold.co/600x400/png?text=Archivador%20metalico%204%20gavetas", true, "Archivador metalico 4 gavetas", 745000.0, "PRD-007", 8 },
                    { new Guid("b0000000-0000-4000-8000-000000000008"), new Guid("c0000000-0000-4000-8000-000000000002"), "Cajonera movil de 3 gavetas con ruedas y freno, acabado grafito.", "https://placehold.co/600x400/png?text=Cajonera%20rodante%203%20gavetas", true, "Cajonera rodante 3 gavetas", 389000.0, "PRD-008", 15 },
                    { new Guid("b0000000-0000-4000-8000-000000000009"), new Guid("c0000000-0000-4000-8000-000000000003"), "Monitor IPS de 27 pulgadas, resolucion 2560x1440, 75 Hz, HDMI y DisplayPort.", "https://placehold.co/600x400/png?text=Monitor%2027%20pulgadas%20QHD", true, "Monitor 27 pulgadas QHD", 1250000.0, "PRD-009", 18 },
                    { new Guid("b0000000-0000-4000-8000-000000000010"), new Guid("c0000000-0000-4000-8000-000000000003"), "Teclado mecanico 75%, switches lineales, conexion Bluetooth y USB-C.", "https://placehold.co/600x400/png?text=Teclado%20mecanico%20inalambrico", true, "Teclado mecanico inalambrico", 329000.0, "PRD-010", 40 },
                    { new Guid("b0000000-0000-4000-8000-000000000011"), new Guid("c0000000-0000-4000-8000-000000000003"), "Diadema over-ear con cancelacion activa de ruido y 30 horas de bateria.", "https://placehold.co/600x400/png?text=Diadema%20con%20cancelacion%20de%20ruido", true, "Diadema con cancelacion de ruido", 459000.0, "PRD-011", 30 },
                    { new Guid("b0000000-0000-4000-8000-000000000012"), new Guid("c0000000-0000-4000-8000-000000000003"), "Base con 5 ventiladores silenciosos y altura ajustable en 6 posiciones.", "https://placehold.co/600x400/png?text=Base%20refrigerante%20para%20portatil", true, "Base refrigerante para portatil", 89900.0, "PRD-012", 45 },
                    { new Guid("b0000000-0000-4000-8000-000000000013"), new Guid("c0000000-0000-4000-8000-000000000004"), "Organizador de escritorio en metal perforado con 5 compartimentos.", "https://placehold.co/600x400/png?text=Organizador%20de%20escritorio%205%20compartimentos", true, "Organizador de escritorio 5 compartimentos", 54900.0, "PRD-013", 75 },
                    { new Guid("b0000000-0000-4000-8000-000000000014"), new Guid("c0000000-0000-4000-8000-000000000004"), "Paquete por 10 cajas de archivo inactivo en carton kraft con tapa fija.", "https://placehold.co/600x400/png?text=Caja%20de%20archivo%20tapa%20fija%20x10", true, "Caja de archivo tapa fija x10", 68000.0, "PRD-014", 60 },
                    { new Guid("b0000000-0000-4000-8000-000000000015"), new Guid("c0000000-0000-4000-8000-000000000004"), "Paquete por 6 blocks de notas adhesivas de 76 x 76 mm en colores neon.", "https://placehold.co/600x400/png?text=Notas%20adhesivas%20x6", true, "Notas adhesivas x6", 11200.0, "PRD-015", 180 },
                    { new Guid("b0000000-0000-4000-8000-000000000016"), new Guid("c0000000-0000-4000-8000-000000000004"), "Perforadora de 2 huecos con capacidad para 30 hojas y guia metrica.", "https://placehold.co/600x400/png?text=Perforadora%20industrial%2030%20hojas", true, "Perforadora industrial 30 hojas", 97500.0, "PRD-016", 22 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_PublicId",
                table: "Categories",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_PublicId",
                table: "Customers",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_PublicId",
                table: "Products",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
