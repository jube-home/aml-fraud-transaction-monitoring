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

using FluentMigrator;

namespace Jube.Migrations.Branches.AskJooby
{
    [Migration(20260921140000)]
    public class AddExhaustiveCategoricalVariables : Migration
    {
        public override void Up()
        {
            Alter.Table("ExhaustiveSearchInstance")
                .AddColumn("CategoricalOptionsJson").AsCustom("text").Nullable();

            Create.Table("ExhaustiveSearchInstanceCategoricalField")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("ExhaustiveSearchInstanceId").AsInt32().Nullable()
                .WithColumn("SourceName").AsString(512).Nullable()
                .WithColumn("ProcessingTypeId").AsByte().Nullable()
                .WithColumn("VocabularyJson").AsCustom("text").Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.Index("IX_ESICategoricalField_InstanceId")
                .OnTable("ExhaustiveSearchInstanceCategoricalField")
                .OnColumn("ExhaustiveSearchInstanceId");

            Create.ForeignKey("FK_ESICategoricalField_Instance")
                .FromTable("ExhaustiveSearchInstanceCategoricalField")
                .ForeignColumn("ExhaustiveSearchInstanceId")
                .ToTable("ExhaustiveSearchInstance")
                .PrimaryColumn("Id");

            Alter.Table("ExhaustiveSearchInstanceVariable")
                .AddColumn("CategoricalFieldId").AsInt32().Nullable()
                .AddColumn("CategoricalKind").AsByte().Nullable()
                .AddColumn("CategoricalLevel").AsString(256).Nullable()
                .AddColumn("CategoricalIndicatorIndex").AsInt32().Nullable();

            Create.ForeignKey("FK_ESIVariable_CategoricalField")
                .FromTable("ExhaustiveSearchInstanceVariable")
                .ForeignColumn("CategoricalFieldId")
                .ToTable("ExhaustiveSearchInstanceCategoricalField")
                .PrimaryColumn("Id");
        }

        public override void Down()
        {
        }
    }
}