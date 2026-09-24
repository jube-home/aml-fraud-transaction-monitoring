/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

namespace Jube.Migrations.Branches.ModelIntegrity
{
    using System;
    using FluentMigrator;

    [Migration(20263107400000)]
    public class AddModelIntegrity : Migration
    {
        public override void Up()
        {
            Create.Table("EntityAnalysisModelEngineSnapshot")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Instance").AsString().NotNullable()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("EntityAnalysisModelId").AsInt32().NotNullable()
                .WithColumn("Json").AsCustom("jsonb").NotNullable()
                .WithColumn("CreatedDate").AsDateTime2().NotNullable();

            Create.Index()
                .OnTable("EntityAnalysisModelEngineSnapshot")
                .OnColumn("Instance").Ascending()
                .OnColumn("EntityAnalysisModelId").Ascending()
                .WithOptions().Unique();

            Create.Index()
                .OnTable("EntityAnalysisModelEngineSnapshot")
                .OnColumn("TenantRegistryId").Ascending()
                .OnColumn("EntityAnalysisModelId").Ascending();

            Insert.IntoTable("PermissionSpecification").Row(new
            {
                Id = 60,
                Name = "View Model Integrity"
            });

            Insert.IntoTable("RoleRegistryPermission").Row(new
            {
                RoleRegistryId = 1,
                PermissionSpecificationId = 60,
                Active = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = "Administrator",
                Version = 1,
                Guid = Guid.NewGuid()
            });
        }

        public override void Down()
        {
            Delete.FromTable("RoleRegistryPermission").Row(new { PermissionSpecificationId = 60 });
            Delete.FromTable("PermissionSpecification").Row(new { Id = 60 });
            Delete.Table("EntityAnalysisModelEngineSnapshot");
        }
    }
}