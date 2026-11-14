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

namespace Jube.Migrations.Branches.CaseSuppression
{
    using System;
    using FluentMigrator;

    [Migration(20263108220000)]
    public class CreateCaseSuppression : Migration
    {
        public override void Up()
        {
            Create.Table("CaseSuppression")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("CaseId").AsInt32().NotNullable()
                .WithColumn("CaseKey").AsString().Nullable()
                .WithColumn("CaseKeyValue").AsString().Nullable()
                .WithColumn("OverrideKey").AsString(256).NotNullable()
                .WithColumn("OverrideKeyValue").AsString(256).NotNullable()
                .WithColumn("PathId").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("Reason").AsString(int.MaxValue).Nullable()
                .WithColumn("ExpiryDate").AsDateTime2().NotNullable()
                .WithColumn("StateId").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("SourceId").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("Version").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("CountInCase").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("FirstSeenDate").AsDateTime2().Nullable()
                .WithColumn("LastSeenDate").AsDateTime2().Nullable()
                .WithColumn("WiderCountStateId").AsInt32().NotNullable().WithDefaultValue(2)
                .WithColumn("WiderTransactions").AsInt32().Nullable()
                .WithColumn("WiderKeys").AsInt32().Nullable()
                .WithColumn("WiderCapped").AsByte().NotNullable().WithDefaultValue(0)
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("DecidedDate").AsDateTime2().Nullable()
                .WithColumn("DecidedUser").AsString().Nullable();

            Create.ForeignKey("FK_CaseSuppression_CaseId_Case_Id").FromTable("CaseSuppression").ForeignColumn("CaseId")
                .ToTable("Case").PrimaryColumn("Id");

            Create.Index("IX_CaseSuppression_CaseId").OnTable("CaseSuppression").OnColumn("CaseId").Ascending();

            Create.Index("IX_CaseSuppression_StateId_CreatedDate").OnTable("CaseSuppression")
                .OnColumn("StateId").Ascending().OnColumn("CreatedDate").Ascending();

            Create.Index("IX_CaseSuppression_CaseKey_CaseKeyValue").OnTable("CaseSuppression")
                .OnColumn("CaseKey").Ascending().OnColumn("CaseKeyValue").Ascending();

            Create.Table("CaseSuppressionVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("CaseSuppressionId").AsInt32().NotNullable()
                .WithColumn("CaseId").AsInt32().NotNullable()
                .WithColumn("CaseKey").AsString().Nullable()
                .WithColumn("CaseKeyValue").AsString().Nullable()
                .WithColumn("OverrideKey").AsString(256).NotNullable()
                .WithColumn("OverrideKeyValue").AsString(256).NotNullable()
                .WithColumn("PathId").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("Reason").AsString(int.MaxValue).Nullable()
                .WithColumn("ExpiryDate").AsDateTime2().NotNullable()
                .WithColumn("StateId").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("SourceId").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("Version").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("CountInCase").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("FirstSeenDate").AsDateTime2().Nullable()
                .WithColumn("LastSeenDate").AsDateTime2().Nullable()
                .WithColumn("WiderCountStateId").AsInt32().NotNullable().WithDefaultValue(2)
                .WithColumn("WiderTransactions").AsInt32().Nullable()
                .WithColumn("WiderKeys").AsInt32().Nullable()
                .WithColumn("WiderCapped").AsByte().NotNullable().WithDefaultValue(0)
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("DecidedDate").AsDateTime2().Nullable()
                .WithColumn("DecidedUser").AsString().Nullable();

            Create.ForeignKey("FK_CaseSuppressionVersion_CaseSuppressionId_CaseSuppression_Id")
                .FromTable("CaseSuppressionVersion").ForeignColumn("CaseSuppressionId")
                .ToTable("CaseSuppression").PrimaryColumn("Id");

            Create.Index("IX_CaseSuppressionVersion_CaseSuppressionId").OnTable("CaseSuppressionVersion")
                .OnColumn("CaseSuppressionId").Ascending();

            Create.Table("CaseSuppressionTarget")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("CaseSuppressionId").AsInt32().NotNullable()
                .WithColumn("EntityAnalysisModelGuid").AsGuid().NotNullable()
                .WithColumn("EntityAnalysisModelName").AsString().Nullable()
                .WithColumn("RuleName").AsString().Nullable()
                .WithColumn("OverrideKindId").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("AppliedOverrideId").AsInt32().Nullable()
                .WithColumn("AppliedDate").AsDateTime2().Nullable();

            Create.ForeignKey("FK_CaseSuppressionTarget_CaseSuppressionId_CaseSuppression_Id")
                .FromTable("CaseSuppressionTarget").ForeignColumn("CaseSuppressionId")
                .ToTable("CaseSuppression").PrimaryColumn("Id");

            Create.Index("IX_CaseSuppressionTarget_CaseSuppressionId").OnTable("CaseSuppressionTarget")
                .OnColumn("CaseSuppressionId").Ascending();

            Create.Index("IX_CaseSuppressionTarget_AppliedOverrideId").OnTable("CaseSuppressionTarget")
                .OnColumn("AppliedOverrideId").Ascending();

            Create.Table("CaseSuppressionTransaction")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("CaseSuppressionId").AsInt32().NotNullable()
                .WithColumn("ArchiveId").AsInt64().NotNullable()
                .WithColumn("ReferenceDate").AsDateTime2().Nullable()
                .WithColumn("LaneKey").AsString().Nullable();

            Create.ForeignKey("FK_CaseSuppressionTransaction_CaseSuppressionId_CaseSuppression_Id")
                .FromTable("CaseSuppressionTransaction").ForeignColumn("CaseSuppressionId")
                .ToTable("CaseSuppression").PrimaryColumn("Id");

            Create.Index("IX_CaseSuppressionTransaction_CaseSuppressionId").OnTable("CaseSuppressionTransaction")
                .OnColumn("CaseSuppressionId").Ascending();

            Create.Table("CaseSuppressionApproval")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("CaseSuppressionId").AsInt32().NotNullable()
                .WithColumn("SuppressionVersion").AsInt32().NotNullable()
                .WithColumn("DecisionId").AsInt32().NotNullable()
                .WithColumn("Note").AsString(1000).Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable();

            Create.ForeignKey("FK_CaseSuppressionApproval_CaseSuppressionId_CaseSuppression_Id")
                .FromTable("CaseSuppressionApproval").ForeignColumn("CaseSuppressionId")
                .ToTable("CaseSuppression").PrimaryColumn("Id");

            Create.Index("IX_CaseSuppressionApproval_CaseSuppressionId_SuppressionVersion")
                .OnTable("CaseSuppressionApproval")
                .OnColumn("CaseSuppressionId").Ascending().OnColumn("SuppressionVersion").Ascending();

            Execute.Sql("""
                        CREATE UNIQUE INDEX "UX_CaseSuppressionApproval_OneApprovalPerUser"
                        ON "CaseSuppressionApproval" ("CaseSuppressionId", "SuppressionVersion", "CreatedUser")
                        WHERE "DecisionId" = 1;
                        """);

            Insert.IntoTable("PermissionSpecification").Row(new
            {
                Id = 70,
                Name = "Approve Case Suppressions"
            });

            Insert.IntoTable("RoleRegistryPermission").Row(new
            {
                RoleRegistryId = 1,
                PermissionSpecificationId = 70,
                Active = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = "Administrator",
                Version = 1,
                Guid = Guid.NewGuid()
            });

            Execute.Sql("""
                        COMMENT ON TABLE public."CaseSuppression" IS 'A request, made from a case, to suppress or force an override key and value on models and activation rules. Nothing reaches the engine until enough different people have approved it; approval applies it through the override services in one transaction. The override tables are untouched until then. Not a kind of note and not an extension of the override tables.';
                        COMMENT ON COLUMN public."CaseSuppression"."CaseId" IS 'The case the request was made from; tenant and role visibility follow the case.';
                        COMMENT ON COLUMN public."CaseSuppression"."CaseKey" IS 'Copied from the case.';
                        COMMENT ON COLUMN public."CaseSuppression"."CaseKeyValue" IS 'Copied from the case.';
                        COMMENT ON COLUMN public."CaseSuppression"."OverrideKey" IS 'The name of the request XPath (flagged Enable Override) the suppression matches on, for example an IP address field.';
                        COMMENT ON COLUMN public."CaseSuppression"."OverrideKeyValue" IS 'The value of the override key, resolved by the server from the case journal, never supplied freely by the browser.';
                        COMMENT ON COLUMN public."CaseSuppression"."PathId" IS 'How the key was chosen. 1 CaseKey (the case key and value of the case itself), 2 Transaction (a value picked from the distinct values of the case key journal).';
                        COMMENT ON COLUMN public."CaseSuppression"."Reason" IS 'Why, required. Pre-filled by the interface from facts, editable.';
                        COMMENT ON COLUMN public."CaseSuppression"."ExpiryDate" IS 'When the override created on approval is removed by the engine. Required and in the future when requested. Requests do not lapse.';
                        COMMENT ON COLUMN public."CaseSuppression"."StateId" IS 'Where the request is. 1 Pending (awaiting approval, no engine effect), 2 Approved (applied), 3 Rejected (a version ended by a rejection), 4 Retracted (withdrawn by the maker). Whether an approved request has since expired or been removed is resolved against the override rows, not stored.';
                        COMMENT ON COLUMN public."CaseSuppression"."SourceId" IS 'Who raised it. 1 User (a person through the Decide Suppress lightbox), 2 AskJooby (the chat agent, for the calling user). Only the server sets it.';
                        COMMENT ON COLUMN public."CaseSuppression"."Version" IS 'Increases each time the maker resubmits; each version has its own approvals.';
                        COMMENT ON COLUMN public."CaseSuppression"."CountInCase" IS 'Transactions in the case key journal (and merged lanes) that carried the value when the request was made.';
                        COMMENT ON COLUMN public."CaseSuppression"."FirstSeenDate" IS 'Earliest of those transactions.';
                        COMMENT ON COLUMN public."CaseSuppression"."LastSeenDate" IS 'Latest of those transactions.';
                        COMMENT ON COLUMN public."CaseSuppression"."WiderCountStateId" IS 'Whether the wider count could be made. 1 Counted, 2 NotIndexed (the archive index for the key was not ready, the wider impact was unknown).';
                        COMMENT ON COLUMN public."CaseSuppression"."WiderTransactions" IS 'Transactions in the model archive carrying the value, capped; null when not counted.';
                        COMMENT ON COLUMN public."CaseSuppression"."WiderKeys" IS 'Distinct case keys among those transactions, capped; null when not counted.';
                        COMMENT ON COLUMN public."CaseSuppression"."WiderCapped" IS '1 when the wider count stopped at its cap and the true figure is higher.';
                        COMMENT ON COLUMN public."CaseSuppression"."CreatedUser" IS 'The maker. For AskJooby, the user it acted for.';
                        COMMENT ON COLUMN public."CaseSuppression"."DecidedUser" IS 'The user whose decision last moved the request: the approver, the rejecter or the retracting maker.';
                        COMMENT ON TABLE public."CaseSuppressionVersion" IS 'The earlier state of a CaseSuppression recorded each time it is updated; the live row is in CaseSuppression.';
                        COMMENT ON COLUMN public."CaseSuppressionVersion"."CaseSuppressionId" IS 'The request this earlier state belongs to.';
                        COMMENT ON TABLE public."CaseSuppressionTarget" IS 'What a request asks to suppress or force: a whole model (RuleName null) or one activation rule on a model. Once applied, AppliedOverrideId points at the row created in EntityAnalysisModelOverride (no RuleName) or EntityAnalysisModelActivationRuleOverride (RuleName set).';
                        COMMENT ON COLUMN public."CaseSuppressionTarget"."EntityAnalysisModelGuid" IS 'The model, by Guid.';
                        COMMENT ON COLUMN public."CaseSuppressionTarget"."EntityAnalysisModelName" IS 'The model name when the request was made, so the request reads after a rename.';
                        COMMENT ON COLUMN public."CaseSuppressionTarget"."RuleName" IS 'The activation rule name, or null for the whole model.';
                        COMMENT ON COLUMN public."CaseSuppressionTarget"."OverrideKindId" IS 'How the override acts. 0 Suppress, 1 Force (permission 62), the same values as the override tables.';
                        COMMENT ON COLUMN public."CaseSuppressionTarget"."AppliedOverrideId" IS 'Id of the override row created or found already in force when the request was approved; null until applied.';
                        COMMENT ON TABLE public."CaseSuppressionTransaction" IS 'The evidence for a request: the archive transactions the analyst ticked as the reason for suppressing. Kept so the case and the approver see the same context.';
                        COMMENT ON COLUMN public."CaseSuppressionTransaction"."ArchiveId" IS 'The Archive row. Verified on request to be in the case journal scope and to carry the value.';
                        COMMENT ON COLUMN public."CaseSuppressionTransaction"."ReferenceDate" IS 'The reference date of the transaction when the request was made.';
                        COMMENT ON COLUMN public."CaseSuppressionTransaction"."LaneKey" IS 'The merged journal key the transaction came from, or null for the case key itself.';
                        COMMENT ON TABLE public."CaseSuppressionApproval" IS 'One decision on one version of a request. The number of approvals needed is the ApprovalsRequired setting, the same as for model approvals; the maker cannot approve, and nobody approves a version twice.';
                        COMMENT ON COLUMN public."CaseSuppressionApproval"."SuppressionVersion" IS 'The CaseSuppression version the decision applies to.';
                        COMMENT ON COLUMN public."CaseSuppressionApproval"."DecisionId" IS '1 Approved, 2 Rejected, 3 Retracted, 4 Resubmitted.';
                        COMMENT ON COLUMN public."CaseSuppressionApproval"."Note" IS 'Optional note, up to 1000 characters.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
