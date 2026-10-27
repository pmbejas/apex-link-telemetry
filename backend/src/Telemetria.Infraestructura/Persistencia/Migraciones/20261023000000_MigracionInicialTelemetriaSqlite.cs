using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Telemetria.Infraestructura.Persistencia.Migraciones;

/// <inheritdoc />
public partial class MigracionInicialTelemetriaSqlite : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "sesiones",
            columns: table => new
            {
                id = table.Column<Guid>(type: "TEXT", nullable: false),
                usuario_id = table.Column<Guid>(type: "TEXT", nullable: true),
                circuito = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                configuracion_circuito = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                modelo_vehiculo = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                tipo_sesion = table.Column<int>(type: "INTEGER", nullable: false),
                es_simulacion_mock = table.Column<bool>(type: "INTEGER", nullable: false),
                fecha_inicio = table.Column<long>(type: "INTEGER", nullable: false),
                fecha_fin = table.Column<long>(type: "INTEGER", nullable: true),
                total_vueltas = table.Column<int>(type: "INTEGER", nullable: false),
                mejor_tiempo_ms = table.Column<int>(type: "INTEGER", nullable: false),
                sincronizado_servidor_central = table.Column<bool>(type: "INTEGER", nullable: false),
                created_at = table.Column<long>(type: "INTEGER", nullable: false),
                updated_at = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_sesiones", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "vueltas",
            columns: table => new
            {
                id = table.Column<Guid>(type: "TEXT", nullable: false),
                sesion_id = table.Column<Guid>(type: "TEXT", nullable: false),
                numero_vuelta = table.Column<int>(type: "INTEGER", nullable: false),
                tiempo_vuelta_ms = table.Column<int>(type: "INTEGER", nullable: false),
                tiempo_vuelta_formateado = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                sector_1_ms = table.Column<int>(type: "INTEGER", nullable: false),
                sector_2_ms = table.Column<int>(type: "INTEGER", nullable: false),
                sector_3_ms = table.Column<int>(type: "INTEGER", nullable: false),
                es_valida = table.Column<bool>(type: "INTEGER", nullable: false),
                es_mejor_vuelta = table.Column<bool>(type: "INTEGER", nullable: false),
                created_at = table.Column<long>(type: "INTEGER", nullable: false),
                updated_at = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_vueltas", x => x.id);
                table.ForeignKey(
                    name: "FK_vueltas_sesiones_sesion_id",
                    column: x => x.sesion_id,
                    principalTable: "sesiones",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "telemetria_vueltas",
            columns: table => new
            {
                id = table.Column<Guid>(type: "TEXT", nullable: false),
                vuelta_id = table.Column<Guid>(type: "TEXT", nullable: false),
                marca_temporal_unix_ms = table.Column<long>(type: "INTEGER", nullable: false),
                distancia_pista_metros = table.Column<float>(type: "REAL", nullable: false),
                velocidad_kmh = table.Column<float>(type: "REAL", nullable: false),
                acelerador = table.Column<float>(type: "REAL", nullable: false),
                freno = table.Column<float>(type: "REAL", nullable: false),
                marcha = table.Column<int>(type: "INTEGER", nullable: false),
                rpms = table.Column<int>(type: "INTEGER", nullable: false),
                fuerza_g_lat = table.Column<float>(type: "REAL", nullable: false),
                fuerza_g_lon = table.Column<float>(type: "REAL", nullable: false),
                pos_x = table.Column<float>(type: "REAL", nullable: false),
                pos_y = table.Column<float>(type: "REAL", nullable: false),
                pos_z = table.Column<float>(type: "REAL", nullable: false),
                created_at = table.Column<long>(type: "INTEGER", nullable: false),
                updated_at = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_telemetria_vueltas", x => x.id);
                table.ForeignKey(
                    name: "FK_telemetria_vueltas_vueltas_vuelta_id",
                    column: x => x.vuelta_id,
                    principalTable: "vueltas",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_sesiones_fecha_inicio",
            table: "sesiones",
            column: "fecha_inicio");

        migrationBuilder.CreateIndex(
            name: "IX_sesiones_sincronizado_servidor_central_es_simulacion_mock_fecha_fin",
            table: "sesiones",
            columns: new[] { "sincronizado_servidor_central", "es_simulacion_mock", "fecha_fin" });

        migrationBuilder.CreateIndex(
            name: "IX_telemetria_vueltas_vuelta_id_distancia_pista_metros",
            table: "telemetria_vueltas",
            columns: new[] { "vuelta_id", "distancia_pista_metros" });

        migrationBuilder.CreateIndex(
            name: "IX_vueltas_sesion_id_numero_vuelta",
            table: "vueltas",
            columns: new[] { "sesion_id", "numero_vuelta" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "telemetria_vueltas");

        migrationBuilder.DropTable(
            name: "vueltas");

        migrationBuilder.DropTable(
            name: "sesiones");
    }
}
