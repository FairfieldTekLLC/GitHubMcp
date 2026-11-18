using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GitHubMCP.Migrations
{
    /// <inheritdoc />
    public partial class FixApprovalRequestUserRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalRequests_Repositories_RepositoryId",
                table: "ApprovalRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalRequests_Users_RequestedById",
                table: "ApprovalRequests");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalRequests_RequestedById",
                table: "ApprovalRequests");

            migrationBuilder.DropColumn(
                name: "RequestedById",
                table: "ApprovalRequests");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_DecidedByUserId",
                table: "ApprovalRequests",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_RequestedByUserId",
                table: "ApprovalRequests",
                column: "RequestedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalRequests_Repositories_RepositoryId",
                table: "ApprovalRequests",
                column: "RepositoryId",
                principalTable: "Repositories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalRequests_Users_DecidedByUserId",
                table: "ApprovalRequests",
                column: "DecidedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalRequests_Users_RequestedByUserId",
                table: "ApprovalRequests",
                column: "RequestedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalRequests_Repositories_RepositoryId",
                table: "ApprovalRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalRequests_Users_DecidedByUserId",
                table: "ApprovalRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalRequests_Users_RequestedByUserId",
                table: "ApprovalRequests");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalRequests_DecidedByUserId",
                table: "ApprovalRequests");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalRequests_RequestedByUserId",
                table: "ApprovalRequests");

            migrationBuilder.AddColumn<Guid>(
                name: "RequestedById",
                table: "ApprovalRequests",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_RequestedById",
                table: "ApprovalRequests",
                column: "RequestedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalRequests_Repositories_RepositoryId",
                table: "ApprovalRequests",
                column: "RepositoryId",
                principalTable: "Repositories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalRequests_Users_RequestedById",
                table: "ApprovalRequests",
                column: "RequestedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
