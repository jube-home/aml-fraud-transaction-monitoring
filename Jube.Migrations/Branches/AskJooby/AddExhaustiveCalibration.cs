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
    [Migration(20263107271500)]
    public class AddExhaustiveCalibration : Migration
    {
        public override void Up()
        {
            Alter.Table("ExhaustiveSearchInstancePromotedTrialInstance")
                .AddColumn("CalibrationMethodId").AsByte().Nullable()
                .AddColumn("CalibrationInputId").AsByte().Nullable()
                .AddColumn("CalibrationSlope").AsDouble().Nullable()
                .AddColumn("CalibrationIntercept").AsDouble().Nullable()
                .AddColumn("CalibrationEpsilon").AsDouble().Nullable()
                .AddColumn("TrainingPrior").AsDouble().Nullable()
                .AddColumn("CalibrationPrior").AsDouble().Nullable()
                .AddColumn("BaseRate").AsDouble().Nullable()
                .AddColumn("BaseRateSourceId").AsByte().Nullable()
                .AddColumn("CalibrationSampleSize").AsInt32().Nullable()
                .AddColumn("CalibrationStatusId").AsByte().Nullable()
                .AddColumn("CalibrationReason").AsString().Nullable()
                .AddColumn("CalibrationQualityJson").AsString().Nullable();

            Alter.Table("ExhaustiveSearchInstance")
                .AddColumn("BaseRate").AsDouble().Nullable();
        }

        public override void Down()
        {
        }
    }
}