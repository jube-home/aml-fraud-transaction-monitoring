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

using System;
using FluentMigrator;

namespace Jube.Migrations.Baseline
{
    [Migration(20220429124931)]
    public class AddEntityAnalysisModelHttpAdaptationTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("EntityAnalysisModelHttpAdaptation")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("ResponsePayload").AsByte().Nullable()
                .WithColumn("ReportTable").AsByte().Nullable()
                .WithColumn("HttpEndpoint").AsString().Nullable();
            
            Create.Index().OnTable("EntityAnalysisModelHttpAdaptation")
                .OnColumn("EntityAnalysisModelId").Ascending()
                .OnColumn("Deleted").Ascending();

            Insert.IntoTable("EntityAnalysisModelHttpAdaptation").Row(new
            {
                EntityAnalysisModelId = 1,
                Active = 0,
                Name = "ExampleFraudScoreLocalEndpoint",
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                ResponsePayload = 1,
                HttpEndpoint = "/api/invoke/ExampleFraudScoreLocalEndpoint"
            });
        }

        public override void Down()
        {
            Delete.Table("EntityAnalysisModelHttpAdaptation");
        }
    }
}