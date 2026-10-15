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
    [Migration(20260920170000)]
    public class AddExhaustiveSearchInstancePrescriptionHistogramTable : Migration
    {
        public override void Up()
        {
            Create.Table("ExhaustiveSearchInstancePrescriptionHistogram")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("ExhaustiveSearchInstancePromotedTrialInstanceVariableId").AsInt32().Nullable()
                .WithColumn("BinIndex").AsInt32().Nullable()
                .WithColumn("BinRangeStart").AsDouble().Nullable()
                .WithColumn("BinRangeEnd").AsDouble().Nullable()
                .WithColumn("Frequency").AsInt32().Nullable();

            Create.Index("IX_ESIPrescriptionHistogram_PromotedVariableId")
                .OnTable("ExhaustiveSearchInstancePrescriptionHistogram")
                .OnColumn("ExhaustiveSearchInstancePromotedTrialInstanceVariableId");

            Create.ForeignKey("FK_ESIPrescriptionHistogram_PromotedVariable")
                .FromTable("ExhaustiveSearchInstancePrescriptionHistogram")
                .ForeignColumn("ExhaustiveSearchInstancePromotedTrialInstanceVariableId")
                .ToTable("ExhaustiveSearchInstancePromotedTrialInstanceVariable")
                .PrimaryColumn("Id");
        }

        public override void Down()
        {
        }
    }
}