using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncColumnsToTodoEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "TodoList",
                type: "nvarchar(450)",
                nullable: true
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "SyncedAt",
                table: "TodoList",
                type: "datetime2",
                nullable: true
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "TodoList",
                type: "datetime2",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "TodoItem",
                type: "nvarchar(450)",
                nullable: true
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "SyncedAt",
                table: "TodoItem",
                type: "datetime2",
                nullable: true
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "TodoItem",
                type: "datetime2",
                nullable: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_TodoList_ExternalId",
                table: "TodoList",
                column: "ExternalId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_TodoItem_ExternalId",
                table: "TodoItem",
                column: "ExternalId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_TodoList_ExternalId", table: "TodoList");

            migrationBuilder.DropIndex(name: "IX_TodoItem_ExternalId", table: "TodoItem");

            migrationBuilder.DropColumn(name: "ExternalId", table: "TodoList");

            migrationBuilder.DropColumn(name: "SyncedAt", table: "TodoList");

            migrationBuilder.DropColumn(name: "UpdatedAt", table: "TodoList");

            migrationBuilder.DropColumn(name: "ExternalId", table: "TodoItem");

            migrationBuilder.DropColumn(name: "SyncedAt", table: "TodoItem");

            migrationBuilder.DropColumn(name: "UpdatedAt", table: "TodoItem");
        }
    }
}
