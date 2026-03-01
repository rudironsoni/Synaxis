// <copyright file="20260218195127_AddGroupDeletedBy.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaxis.InferenceGateway.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupDeletedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                schema: "identity",
                table: "Groups",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "identity",
                table: "Groups");
        }
    }
}
