using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChillSharp.Tests.Migrations
{
    /// <inheritdoc />
    public partial class v1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BlogGuid",
                table: "Post",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Blog",
                columns: table => new
                {
                    Guid = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: false),
                    ShortLabel = table.Column<string>(type: "TEXT", nullable: false),
                    FullTextContent = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blog", x => x.Guid);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Post_BlogGuid",
                table: "Post",
                column: "BlogGuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Post_Blog_BlogGuid",
                table: "Post",
                column: "BlogGuid",
                principalTable: "Blog",
                principalColumn: "Guid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Post_Blog_BlogGuid",
                table: "Post");

            migrationBuilder.DropTable(
                name: "Blog");

            migrationBuilder.DropIndex(
                name: "IX_Post_BlogGuid",
                table: "Post");

            migrationBuilder.DropColumn(
                name: "BlogGuid",
                table: "Post");
        }
    }
}
