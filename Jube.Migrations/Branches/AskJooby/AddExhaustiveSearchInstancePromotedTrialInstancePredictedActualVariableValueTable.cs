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
    [Migration(20263107580000)]
    public class AddExhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValueTable : Migration
    {
        public override void Up()
        {
            Create.Table("ExhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValue")
                .WithColumn("Id").AsInt32().PrimaryKey("PK_ESIPromotedTrialInstancePAVariableValue").Identity()
                .WithColumn("ExhaustiveSearchInstancePromotedTrialInstancePredictedActualId").AsInt32().Nullable()
                .WithColumn("ExhaustiveSearchInstanceVariableId").AsInt32().Nullable()
                .WithColumn("Value").AsDouble().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.Index("IX_ESIPromotedTrialInstancePAVariableValue_PredictedActualId")
                .OnTable("ExhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValue")
                .OnColumn("ExhaustiveSearchInstancePromotedTrialInstancePredictedActualId");

            Create.Index("IX_ESIPromotedTrialInstancePAVariableValue_VariableId")
                .OnTable("ExhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValue")
                .OnColumn("ExhaustiveSearchInstanceVariableId");

            Create.ForeignKey("FK_ESIPromotedTrialInstancePAVariableValue_PredictedActual")
                .FromTable("ExhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValue")
                .ForeignColumn("ExhaustiveSearchInstancePromotedTrialInstancePredictedActualId")
                .ToTable("ExhaustiveSearchInstancePromotedTrialInstancePredictedActual")
                .PrimaryColumn("Id");

            Create.ForeignKey("FK_ESIPromotedTrialInstancePAVariableValue_Variable")
                .FromTable("ExhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValue")
                .ForeignColumn("ExhaustiveSearchInstanceVariableId")
                .ToTable("ExhaustiveSearchInstanceVariable")
                .PrimaryColumn("Id");

            Create.ForeignKey("FK_ESIPromotedTrialInstancePAVariableValue_Import")
                .FromTable("ExhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValue")
                .ForeignColumn("ImportId")
                .ToTable("Import")
                .PrimaryColumn("Id");
        }

        public override void Down()
        {
        }
    }
}