using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConnectThruEventsBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddFavoriteEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FavoriteEvents",
                columns: table => new
                {
                    FavoriteEventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PublishEventId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavoriteEvents", x => x.FavoriteEventId);
                    table.ForeignKey(
                        name: "FK_FavoriteEvents_PublishEvents_PublishEventId",
                        column: x => x.PublishEventId,
                        principalTable: "PublishEvents",
                        principalColumn: "PublishEventId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FavoriteEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteEvents_PublishEventId",
                table: "FavoriteEvents",
                column: "PublishEventId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteEvents_UserId",
                table: "FavoriteEvents",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FavoriteEvents");
        }
    }
}
