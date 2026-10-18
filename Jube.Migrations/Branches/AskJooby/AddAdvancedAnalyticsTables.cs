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
    [Migration(20260920180000)]
    public class AddAdvancedAnalyticsTables : Migration
    {
        public override void Up()
        {
            Create.Table("AdvancedAnalyticsInstance")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().NotNullable()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("Name").AsString(int.MaxValue).NotNullable()
                .WithColumn("Active").AsByte().NotNullable().WithDefaultValue(0)
                .WithColumn("ResponsePayload").AsByte().NotNullable().WithDefaultValue(0)
                .WithColumn("ReportTable").AsByte().NotNullable().WithDefaultValue(0)
                .WithColumn("CreatedDate").AsDateTime().NotNullable()
                .WithColumn("CreatedUser").AsString(int.MaxValue).Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable()
                .WithColumn("Version").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("Deleted").AsByte().NotNullable().WithDefaultValue(0)
                .WithColumn("DeletedDate").AsDateTime().Nullable()
                .WithColumn("DeletedUser").AsString(int.MaxValue).Nullable();

            Create.UniqueConstraint("UQ_AdvancedAnalyticsInstance_Guid")
                .OnTable("AdvancedAnalyticsInstance").Column("Guid");

            Create.ForeignKey("FK_AdvancedAnalyticsInstance_TenantRegistry")
                .FromTable("AdvancedAnalyticsInstance").ForeignColumn("TenantRegistryId")
                .ToTable("TenantRegistry").PrimaryColumn("Id");

            Create.ForeignKey("FK_AdvancedAnalyticsInstance_EntityAnalysisModel")
                .FromTable("AdvancedAnalyticsInstance").ForeignColumn("EntityAnalysisModelId")
                .ToTable("EntityAnalysisModel").PrimaryColumn("Id");

            Create.Index("IX_AdvancedAnalyticsInstance_Tenant_Model")
                .OnTable("AdvancedAnalyticsInstance")
                .OnColumn("TenantRegistryId").Ascending()
                .OnColumn("EntityAnalysisModelId").Ascending();

            Execute.Sql("""
                        CREATE UNIQUE INDEX "UX_AdvancedAnalyticsInstance_Tenant_Name"
                        ON "AdvancedAnalyticsInstance" ("TenantRegistryId", lower("Name"))
                        WHERE "Deleted" = 0;
                        """);

            Create.Table("AdvancedAnalyticsCache")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().NotNullable()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("Data").AsBinary(int.MaxValue).NotNullable()
                .WithColumn("Rows").AsInt32().NotNullable()
                .WithColumn("Columns").AsInt32().NotNullable()
                .WithColumn("PackingVersion").AsInt32().NotNullable()
                .WithColumn("SizeBytes").AsInt64().NotNullable()
                .WithColumn("CreatedDate").AsDateTime().NotNullable()
                .WithColumn("CreatedUser").AsString(int.MaxValue).Nullable()
                .WithColumn("ExpiryDate").AsDateTime().NotNullable();

            Create.UniqueConstraint("UQ_AdvancedAnalyticsCache_Guid")
                .OnTable("AdvancedAnalyticsCache").Column("Guid");

            Create.ForeignKey("FK_AdvancedAnalyticsCache_TenantRegistry")
                .FromTable("AdvancedAnalyticsCache").ForeignColumn("TenantRegistryId")
                .ToTable("TenantRegistry").PrimaryColumn("Id");

            Create.Index("IX_AdvancedAnalyticsCache_ExpiryDate")
                .OnTable("AdvancedAnalyticsCache").OnColumn("ExpiryDate");

            Create.Index("IX_AdvancedAnalyticsCache_Tenant")
                .OnTable("AdvancedAnalyticsCache").OnColumn("TenantRegistryId");

            Create.Table("AdvancedAnalyticsTrainedModel")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().NotNullable()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("AdvancedAnalyticsInstanceId").AsInt32().NotNullable()
                .WithColumn("Name").AsString(int.MaxValue).NotNullable()
                .WithColumn("KindId").AsInt32().NotNullable()
                .WithColumn("Active").AsByte().NotNullable().WithDefaultValue(0)
                .WithColumn("ActiveVersionId").AsInt32().Nullable()
                .WithColumn("SourceId").AsInt32().NotNullable()
                .WithColumn("CorrelationKey").AsString(int.MaxValue).Nullable()
                .WithColumn("CreatedDate").AsDateTime().NotNullable()
                .WithColumn("CreatedUser").AsString(int.MaxValue).Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable()
                .WithColumn("Version").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("Deleted").AsByte().NotNullable().WithDefaultValue(0)
                .WithColumn("DeletedDate").AsDateTime().Nullable()
                .WithColumn("DeletedUser").AsString(int.MaxValue).Nullable();

            Create.UniqueConstraint("UQ_AdvancedAnalyticsTrainedModel_Guid")
                .OnTable("AdvancedAnalyticsTrainedModel").Column("Guid");

            Create.ForeignKey("FK_AdvancedAnalyticsTrainedModel_TenantRegistry")
                .FromTable("AdvancedAnalyticsTrainedModel").ForeignColumn("TenantRegistryId")
                .ToTable("TenantRegistry").PrimaryColumn("Id");

            Create.ForeignKey("FK_AdvancedAnalyticsTrainedModel_Instance")
                .FromTable("AdvancedAnalyticsTrainedModel").ForeignColumn("AdvancedAnalyticsInstanceId")
                .ToTable("AdvancedAnalyticsInstance").PrimaryColumn("Id");

            Create.Index("IX_AdvancedAnalyticsTrainedModel_Tenant_Instance")
                .OnTable("AdvancedAnalyticsTrainedModel")
                .OnColumn("TenantRegistryId").Ascending()
                .OnColumn("AdvancedAnalyticsInstanceId").Ascending();

            Create.Index("IX_AdvancedAnalyticsTrainedModel_Tenant_Correlation")
                .OnTable("AdvancedAnalyticsTrainedModel")
                .OnColumn("TenantRegistryId").Ascending()
                .OnColumn("CorrelationKey").Ascending();

            Create.Table("AdvancedAnalyticsTrainedModelVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().NotNullable()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("AdvancedAnalyticsTrainedModelId").AsInt32().NotNullable()
                .WithColumn("VersionNumber").AsInt32().NotNullable()
                .WithColumn("ModelJson").AsCustom("json").NotNullable()
                .WithColumn("VariablesJson").AsCustom("jsonb").Nullable()
                .WithColumn("MetricsJson").AsCustom("jsonb").Nullable()
                .WithColumn("CalibratorJson").AsCustom("json").Nullable()
                .WithColumn("AdvancedAnalyticsTrainingRunId").AsInt32().Nullable()
                .WithColumn("CreatedDate").AsDateTime().NotNullable()
                .WithColumn("CreatedUser").AsString(int.MaxValue).Nullable();

            Create.UniqueConstraint("UQ_AdvancedAnalyticsTrainedModelVersion_Guid")
                .OnTable("AdvancedAnalyticsTrainedModelVersion").Column("Guid");

            Create.UniqueConstraint("UQ_AdvancedAnalyticsTrainedModelVersion_Number")
                .OnTable("AdvancedAnalyticsTrainedModelVersion")
                .Columns("AdvancedAnalyticsTrainedModelId", "VersionNumber");

            Create.ForeignKey("FK_AdvancedAnalyticsTrainedModelVersion_TenantRegistry")
                .FromTable("AdvancedAnalyticsTrainedModelVersion").ForeignColumn("TenantRegistryId")
                .ToTable("TenantRegistry").PrimaryColumn("Id");

            Create.ForeignKey("FK_AdvancedAnalyticsTrainedModelVersion_Model")
                .FromTable("AdvancedAnalyticsTrainedModelVersion").ForeignColumn("AdvancedAnalyticsTrainedModelId")
                .ToTable("AdvancedAnalyticsTrainedModel").PrimaryColumn("Id");

            Create.Table("AdvancedAnalyticsTrainingRun")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().NotNullable()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("AdvancedAnalyticsInstanceId").AsInt32().NotNullable()
                .WithColumn("UserName").AsString(int.MaxValue).Nullable()
                .WithColumn("KindId").AsInt32().NotNullable()
                .WithColumn("JobMode").AsInt32().NotNullable()
                .WithColumn("StatusId").AsInt32().NotNullable()
                .WithColumn("Arguments").AsCustom("jsonb").Nullable()
                .WithColumn("InputPointer").AsGuid().Nullable()
                .WithColumn("Seed").AsInt64().Nullable()
                .WithColumn("Progress").AsDouble().NotNullable().WithDefaultValue(0)
                .WithColumn("CreatedDate").AsDateTime().NotNullable()
                .WithColumn("StartedDate").AsDateTime().Nullable()
                .WithColumn("CompletedDate").AsDateTime().Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable()
                .WithColumn("Error").AsString(int.MaxValue).Nullable()
                .WithColumn("CreatedUser").AsString(int.MaxValue).Nullable()
                .WithColumn("Deleted").AsByte().NotNullable().WithDefaultValue(0)
                .WithColumn("DeletedDate").AsDateTime().Nullable()
                .WithColumn("DeletedUser").AsString(int.MaxValue).Nullable();

            Create.UniqueConstraint("UQ_AdvancedAnalyticsTrainingRun_Guid")
                .OnTable("AdvancedAnalyticsTrainingRun").Column("Guid");

            Create.ForeignKey("FK_AdvancedAnalyticsTrainingRun_TenantRegistry")
                .FromTable("AdvancedAnalyticsTrainingRun").ForeignColumn("TenantRegistryId")
                .ToTable("TenantRegistry").PrimaryColumn("Id");

            Create.ForeignKey("FK_AdvancedAnalyticsTrainingRun_Instance")
                .FromTable("AdvancedAnalyticsTrainingRun").ForeignColumn("AdvancedAnalyticsInstanceId")
                .ToTable("AdvancedAnalyticsInstance").PrimaryColumn("Id");

            Create.Index("IX_AdvancedAnalyticsTrainingRun_Tenant_Instance")
                .OnTable("AdvancedAnalyticsTrainingRun")
                .OnColumn("TenantRegistryId").Ascending()
                .OnColumn("AdvancedAnalyticsInstanceId").Ascending();

            Create.Index("IX_AdvancedAnalyticsTrainingRun_Tenant_Status")
                .OnTable("AdvancedAnalyticsTrainingRun")
                .OnColumn("TenantRegistryId").Ascending()
                .OnColumn("StatusId").Ascending();

            Create.ForeignKey("FK_AdvancedAnalyticsTrainedModelVersion_Run")
                .FromTable("AdvancedAnalyticsTrainedModelVersion").ForeignColumn("AdvancedAnalyticsTrainingRunId")
                .ToTable("AdvancedAnalyticsTrainingRun").PrimaryColumn("Id");

            Create.Table("AdvancedAnalyticsTrainingRunStep")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("AdvancedAnalyticsInstanceId").AsInt32().NotNullable()
                .WithColumn("AdvancedAnalyticsTrainingRunId").AsInt32().NotNullable()
                .WithColumn("StepOrder").AsInt32().NotNullable()
                .WithColumn("Step").AsInt32().NotNullable()
                .WithColumn("StatusId").AsInt32().NotNullable()
                .WithColumn("StartedDate").AsDateTime().Nullable()
                .WithColumn("CompletedDate").AsDateTime().Nullable()
                .WithColumn("DurationMilliseconds").AsDouble().Nullable()
                .WithColumn("Error").AsString(int.MaxValue).Nullable();

            AddRunChildKeys("AdvancedAnalyticsTrainingRunStep", "Step");

            Create.Table("AdvancedAnalyticsTrainingRunTrial")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("AdvancedAnalyticsInstanceId").AsInt32().NotNullable()
                .WithColumn("AdvancedAnalyticsTrainingRunId").AsInt32().NotNullable()
                .WithColumn("TrialNumber").AsInt32().NotNullable()
                .WithColumn("StatusId").AsInt32().NotNullable()
                .WithColumn("Hyperparameters").AsCustom("jsonb").Nullable()
                .WithColumn("Score").AsDouble().Nullable()
                .WithColumn("Selected").AsByte().NotNullable().WithDefaultValue(0)
                .WithColumn("CreatedDate").AsDateTime().NotNullable()
                .WithColumn("CompletedDate").AsDateTime().Nullable()
                .WithColumn("DurationMilliseconds").AsDouble().Nullable()
                .WithColumn("Error").AsString(int.MaxValue).Nullable();

            AddRunChildKeys("AdvancedAnalyticsTrainingRunTrial", "Trial");

            Create.Table("AdvancedAnalyticsTrainingRunIteration")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("AdvancedAnalyticsInstanceId").AsInt32().NotNullable()
                .WithColumn("AdvancedAnalyticsTrainingRunId").AsInt32().NotNullable()
                .WithColumn("AdvancedAnalyticsTrainingRunTrialId").AsInt32().Nullable()
                .WithColumn("IterationNumber").AsInt32().NotNullable()
                .WithColumn("TrainLoss").AsDouble().Nullable()
                .WithColumn("ValidationLoss").AsDouble().Nullable()
                .WithColumn("Score").AsDouble().Nullable()
                .WithColumn("RecordedDate").AsDateTime().NotNullable();

            AddRunChildKeys("AdvancedAnalyticsTrainingRunIteration", "Iteration");

            Create.ForeignKey("FK_AdvancedAnalyticsTrainingRunIteration_Trial")
                .FromTable("AdvancedAnalyticsTrainingRunIteration")
                .ForeignColumn("AdvancedAnalyticsTrainingRunTrialId")
                .ToTable("AdvancedAnalyticsTrainingRunTrial").PrimaryColumn("Id");

            Create.Table("AdvancedAnalyticsTrainingRunVariable")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("AdvancedAnalyticsInstanceId").AsInt32().NotNullable()
                .WithColumn("AdvancedAnalyticsTrainingRunId").AsInt32().NotNullable()
                .WithColumn("VariableSequence").AsInt32().NotNullable()
                .WithColumn("Name").AsString(int.MaxValue).NotNullable()
                .WithColumn("Mean").AsDouble().Nullable()
                .WithColumn("StandardDeviation").AsDouble().Nullable()
                .WithColumn("Minimum").AsDouble().Nullable()
                .WithColumn("Maximum").AsDouble().Nullable()
                .WithColumn("ImputationValue").AsDouble().Nullable()
                .WithColumn("MissingCount").AsInt64().Nullable()
                .WithColumn("RowCount").AsInt64().Nullable()
                .WithColumn("Importance").AsDouble().Nullable();

            AddRunChildKeys("AdvancedAnalyticsTrainingRunVariable", "Variable");

            Create.Table("AdvancedAnalyticsTrainingRunMetric")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("AdvancedAnalyticsInstanceId").AsInt32().NotNullable()
                .WithColumn("AdvancedAnalyticsTrainingRunId").AsInt32().NotNullable()
                .WithColumn("AdvancedAnalyticsTrainingRunTrialId").AsInt32().Nullable()
                .WithColumn("Name").AsString(int.MaxValue).NotNullable()
                .WithColumn("Value").AsDouble().Nullable()
                .WithColumn("Split").AsInt32().NotNullable();

            AddRunChildKeys("AdvancedAnalyticsTrainingRunMetric", "Metric");

            Create.ForeignKey("FK_AdvancedAnalyticsTrainingRunMetric_Trial")
                .FromTable("AdvancedAnalyticsTrainingRunMetric")
                .ForeignColumn("AdvancedAnalyticsTrainingRunTrialId")
                .ToTable("AdvancedAnalyticsTrainingRunTrial").PrimaryColumn("Id");

            Create.Table("AdvancedAnalyticsTrainingRunCurvePoint")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("AdvancedAnalyticsInstanceId").AsInt32().NotNullable()
                .WithColumn("AdvancedAnalyticsTrainingRunId").AsInt32().NotNullable()
                .WithColumn("Curve").AsInt32().NotNullable()
                .WithColumn("Split").AsInt32().NotNullable()
                .WithColumn("PointIndex").AsInt32().NotNullable()
                .WithColumn("X").AsDouble().Nullable()
                .WithColumn("Y").AsDouble().Nullable()
                .WithColumn("Threshold").AsDouble().Nullable();

            AddRunChildKeys("AdvancedAnalyticsTrainingRunCurvePoint", "CurvePoint");
        }

        public override void Down()
        {
        }

        private void AddRunChildKeys(string table, string shortName)
        {
            Create.ForeignKey($"FK_AdvancedAnalyticsRun{shortName}_TenantRegistry")
                .FromTable(table).ForeignColumn("TenantRegistryId")
                .ToTable("TenantRegistry").PrimaryColumn("Id");

            Create.ForeignKey($"FK_AdvancedAnalyticsRun{shortName}_Instance")
                .FromTable(table).ForeignColumn("AdvancedAnalyticsInstanceId")
                .ToTable("AdvancedAnalyticsInstance").PrimaryColumn("Id");

            Create.ForeignKey($"FK_AdvancedAnalyticsRun{shortName}_Run")
                .FromTable(table).ForeignColumn("AdvancedAnalyticsTrainingRunId")
                .ToTable("AdvancedAnalyticsTrainingRun").PrimaryColumn("Id");

            Create.Index($"IX_AdvancedAnalyticsRun{shortName}_Tenant_Run")
                .OnTable(table)
                .OnColumn("TenantRegistryId").Ascending()
                .OnColumn("AdvancedAnalyticsTrainingRunId").Ascending();

            Create.Index($"IX_AdvancedAnalyticsRun{shortName}_Instance")
                .OnTable(table).OnColumn("AdvancedAnalyticsInstanceId");
        }
    }
}