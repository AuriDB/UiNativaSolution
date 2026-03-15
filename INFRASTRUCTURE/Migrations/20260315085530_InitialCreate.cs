using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nativa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sujetos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Cedula = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Correo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rol = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sujetos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Activos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdDueno = table.Column<int>(type: "int", nullable: false),
                    IdIngeniero = table.Column<int>(type: "int", nullable: true),
                    Hectareas = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    Vegetacion = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Hidrologia = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Topografia = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    EsNacional = table.Column<bool>(type: "bit", nullable: false),
                    Lat = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    Lng = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activos_Sujetos_IdDueno",
                        column: x => x.IdDueno,
                        principalTable: "Sujetos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Activos_Sujetos_IdIngeniero",
                        column: x => x.IdIngeniero,
                        principalTable: "Sujetos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CuentasBancarias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdDueno = table.Column<int>(type: "int", nullable: false),
                    Banco = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoCuenta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Titular = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IbanCompleto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IbanOfuscado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuentasBancarias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CuentasBancarias_Sujetos_IdDueno",
                        column: x => x.IdDueno,
                        principalTable: "Sujetos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OtpSesiones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdSujeto = table.Column<int>(type: "int", nullable: false),
                    HashOtp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Expiracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Usada = table.Column<bool>(type: "bit", nullable: false),
                    Intentos = table.Column<int>(type: "int", nullable: false),
                    UltimoReenvio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConteoReenvios = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpSesiones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OtpSesiones_Sujetos_IdSujeto",
                        column: x => x.IdSujeto,
                        principalTable: "Sujetos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ParametrosPagos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrecioBase = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PctVegetacion = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    PctHidrologia = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    PctNacional = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    PctTopografia = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    Tope = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    Vigente = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreadoPor = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametrosPagos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParametrosPagos_Sujetos_CreadoPor",
                        column: x => x.CreadoPor,
                        principalTable: "Sujetos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdjuntosActivos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdActivo = table.Column<int>(type: "int", nullable: false),
                    BlobUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NombreArchivo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaSubida = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdjuntosActivos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdjuntosActivos_Activos_IdActivo",
                        column: x => x.IdActivo,
                        principalTable: "Activos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlanesPago",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdActivo = table.Column<int>(type: "int", nullable: false),
                    IdIngeniero = table.Column<int>(type: "int", nullable: false),
                    FechaActivacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SnapshotParametrosJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MontoMensual = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanesPago", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanesPago_Activos_IdActivo",
                        column: x => x.IdActivo,
                        principalTable: "Activos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanesPago_Sujetos_IdIngeniero",
                        column: x => x.IdIngeniero,
                        principalTable: "Sujetos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PagosMensuales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdPlan = table.Column<int>(type: "int", nullable: false),
                    NumeroPago = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaEjecucion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosMensuales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagosMensuales_PlanesPago_IdPlan",
                        column: x => x.IdPlan,
                        principalTable: "PlanesPago",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activos_Estado_FechaRegistro",
                table: "Activos",
                columns: new[] { "Estado", "FechaRegistro" });

            migrationBuilder.CreateIndex(
                name: "IX_Activos_IdDueno",
                table: "Activos",
                column: "IdDueno");

            migrationBuilder.CreateIndex(
                name: "IX_Activos_IdIngeniero",
                table: "Activos",
                column: "IdIngeniero");

            migrationBuilder.CreateIndex(
                name: "IX_AdjuntosActivos_IdActivo",
                table: "AdjuntosActivos",
                column: "IdActivo");

            migrationBuilder.CreateIndex(
                name: "IX_CuentasBancarias_IdDueno",
                table: "CuentasBancarias",
                column: "IdDueno");

            migrationBuilder.CreateIndex(
                name: "IX_OtpSesiones_IdSujeto",
                table: "OtpSesiones",
                column: "IdSujeto");

            migrationBuilder.CreateIndex(
                name: "IX_PagosMensuales_IdPlan",
                table: "PagosMensuales",
                column: "IdPlan");

            migrationBuilder.CreateIndex(
                name: "IX_ParametrosPagos_CreadoPor",
                table: "ParametrosPagos",
                column: "CreadoPor");

            migrationBuilder.CreateIndex(
                name: "IX_PlanesPago_IdActivo",
                table: "PlanesPago",
                column: "IdActivo");

            migrationBuilder.CreateIndex(
                name: "IX_PlanesPago_IdIngeniero",
                table: "PlanesPago",
                column: "IdIngeniero");

            migrationBuilder.CreateIndex(
                name: "IX_Sujetos_Cedula",
                table: "Sujetos",
                column: "Cedula",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sujetos_Correo",
                table: "Sujetos",
                column: "Correo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdjuntosActivos");

            migrationBuilder.DropTable(
                name: "CuentasBancarias");

            migrationBuilder.DropTable(
                name: "OtpSesiones");

            migrationBuilder.DropTable(
                name: "PagosMensuales");

            migrationBuilder.DropTable(
                name: "ParametrosPagos");

            migrationBuilder.DropTable(
                name: "PlanesPago");

            migrationBuilder.DropTable(
                name: "Activos");

            migrationBuilder.DropTable(
                name: "Sujetos");
        }
    }
}
