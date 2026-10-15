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
    [Migration(20260921150000)]
    public class AddExhaustiveSearchInstanceVariableHistogramClassTable : Migration
    {
        public override void Up()
        {
            Create.Table("ExhaustiveSearchInstanceVariableHistogramClass")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("ExhaustiveSearchInstanceVariableId").AsInt32().Nullable()
                .WithColumn("ClassLabel").AsInt32().Nullable()
                .WithColumn("BinSequence").AsInt32().Nullable()
                .WithColumn("BinRangeStart").AsDouble().Nullable()
                .WithColumn("BinRangeEnd").AsDouble().Nullable()
                .WithColumn("Frequency").AsInt32().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.Index("IX_ESIVariableHistogramClass_VariableId")
                .OnTable("ExhaustiveSearchInstanceVariableHistogramClass")
                .OnColumn("ExhaustiveSearchInstanceVariableId");

            Create.ForeignKey("FK_ESIVariableHistogramClass_Variable")
                .FromTable("ExhaustiveSearchInstanceVariableHistogramClass")
                .ForeignColumn("ExhaustiveSearchInstanceVariableId")
                .ToTable("ExhaustiveSearchInstanceVariable")
                .PrimaryColumn("Id");

            Create.ForeignKey("FK_ESIVariableHistogramClass_Import")
                .FromTable("ExhaustiveSearchInstanceVariableHistogramClass")
                .ForeignColumn("ImportId")
                .ToTable("Import")
                .PrimaryColumn("Id");
        }

        public override void Down()
        {
        }
    }
}