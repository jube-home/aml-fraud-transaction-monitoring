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

namespace Jube.Migrations.Branches.ResponseHold
{
    using System;
    using FluentMigrator;

    [Migration(20263108160000)]
    public class AddResponseHold : Migration
    {
        public override void Up()
        {
            Alter.Table("EntityAnalysisModelResponseElevationRange")
                .AddColumn("HttpEndpointHeaderName").AsString().Nullable()
                .AddColumn("HttpEndpointCredentialVariable").AsString().Nullable();

            Alter.Table("EntityAnalysisModelResponseElevationRangeVersion")
                .AddColumn("HttpEndpointHeaderName").AsString().Nullable()
                .AddColumn("HttpEndpointCredentialVariable").AsString().Nullable();

            Alter.Table("EntityAnalysisAsynchronousQueueBalance")
                .AddColumn("Webhook").AsInt32().Nullable();

            Alter.Table("ActivationWatcher")
                .AddColumn("ResponseElevationName").AsString().Nullable();

            Create.Table("CaseWorkflowReleaseHook")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("CaseWorkflowGuid").AsGuid().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("SourceField").AsString().Nullable()
                .WithColumn("SourceValue").AsString().Nullable()
                .WithColumn("ReleaseUrl").AsString().Nullable()
                .WithColumn("RefuseUrl").AsString().Nullable()
                .WithColumn("HttpEndpointTypeId").AsByte().Nullable()
                .WithColumn("HeaderName").AsString().Nullable()
                .WithColumn("CredentialVariable").AsString().Nullable()
                .WithColumn("Body").AsString(int.MaxValue).Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.Index().OnTable("CaseWorkflowReleaseHook")
                .OnColumn("CaseWorkflowGuid").Ascending()
                .OnColumn("Deleted").Ascending();

            Create.Table("CaseWorkflowReleaseHookVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("CaseWorkflowReleaseHookId").AsInt32().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("CaseWorkflowGuid").AsGuid().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("SourceField").AsString().Nullable()
                .WithColumn("SourceValue").AsString().Nullable()
                .WithColumn("ReleaseUrl").AsString().Nullable()
                .WithColumn("RefuseUrl").AsString().Nullable()
                .WithColumn("HttpEndpointTypeId").AsByte().Nullable()
                .WithColumn("HeaderName").AsString().Nullable()
                .WithColumn("CredentialVariable").AsString().Nullable()
                .WithColumn("Body").AsString(int.MaxValue).Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.Index().OnTable("CaseWorkflowReleaseHookVersion")
                .OnColumn("CaseWorkflowReleaseHookId").Ascending();

            Insert.IntoTable("PermissionSpecification").Row(new
            {
                Id = 68,
                Name = "Release Held Payments"
            });

            Insert.IntoTable("PermissionSpecification").Row(new
            {
                Id = 69,
                Name = "Author Case Workflow Release Hooks"
            });

            foreach (var permission in new[] { 68, 69 })
            {
                Insert.IntoTable("RoleRegistryPermission").Row(new
                {
                    RoleRegistryId = 1,
                    PermissionSpecificationId = permission,
                    Active = 1,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = "Administrator",
                    Version = 1,
                    Guid = Guid.NewGuid()
                });
            }

            Execute.Sql("""
                        COMMENT ON COLUMN public."EntityAnalysisModelResponseElevationRange"."HttpEndpointHeaderName" IS 'Optional name of the request header that carries a credential on the web hook call, for example Authorization or X-Api-Key. Set together with HttpEndpointCredentialVariable.';
                        COMMENT ON COLUMN public."EntityAnalysisModelResponseElevationRange"."HttpEndpointCredentialVariable" IS 'Optional name of the environment variable, which must start with JUBE_HOOK_, whose value is sent in the header named by HttpEndpointHeaderName. The credential itself is never stored in the database.';
                        COMMENT ON COLUMN public."EntityAnalysisAsynchronousQueueBalance"."Webhook" IS 'The number of web hook calls waiting in the engine queue when the interval was written, raised by a response elevation range that has a web hook.';
                        COMMENT ON COLUMN public."ActivationWatcher"."ResponseElevationName" IS 'The name of the response elevation range, from EntityAnalysisModelResponseElevationRange, that the ResponseElevation of the activation falls inside; null when it falls inside none.';
                        COMMENT ON COLUMN public."CaseWorkflowDispatch"."SourceKindId" IS 'What fired the call: 1 Form, a form entry was completed; 2 Macro, a macro was run; 3 Action, a note was added with the action; 4 Status, the case moved to the status; 5 Release hook, a held payment was released or refused.';
                        COMMENT ON TABLE public."CaseWorkflowReleaseHook" IS 'The web hook called to release, or refuse, a payment held on a case of the Case Workflow. Joins CaseWorkflow by CaseWorkflowGuid on Guid rather than on Id so that a configuration export carries the hook with the Case Workflow. Which hook handles a held transaction is chosen by the SourceField and SourceValue of the hook against the payload of the transaction, so that each source system can release in its own way. The credential is never stored: CredentialVariable names an environment variable.';
                        COMMENT ON TABLE public."CaseWorkflowReleaseHookVersion" IS 'The earlier state of a CaseWorkflowReleaseHook recorded each time it is updated; the live row is in CaseWorkflowReleaseHook.';
                        COMMENT ON COLUMN public."CaseWorkflowReleaseHook"."SourceField" IS 'Optional name of a payload field of the held transaction. The hook handles a transaction when the field equals SourceValue, ignoring case; when blank the hook handles every transaction no more specific hook handles.';
                        COMMENT ON COLUMN public."CaseWorkflowReleaseHook"."SourceValue" IS 'The value the payload field named by SourceField must equal for the hook to handle the transaction.';
                        COMMENT ON COLUMN public."CaseWorkflowReleaseHook"."ReleaseUrl" IS 'The URL called to release the payment. The scheme, host and port are written out literally; tokens such as [@Payload.PaymentId@], [@Hold.Name@], [@Hold.Elevation@] and [@Hold.EntryGuid@] are allowed only in the path and query. A response of exactly 200 means released.';
                        COMMENT ON COLUMN public."CaseWorkflowReleaseHook"."RefuseUrl" IS 'Optional URL called to refuse the payment, with the same rules as ReleaseUrl. When blank a refusal is recorded without calling anything.';
                        COMMENT ON COLUMN public."CaseWorkflowReleaseHook"."HttpEndpointTypeId" IS 'The HTTP method used for the call: 1 POST, 2 GET.';
                        COMMENT ON COLUMN public."CaseWorkflowReleaseHook"."HeaderName" IS 'Optional name of the request header that carries the credential. Set together with CredentialVariable.';
                        COMMENT ON COLUMN public."CaseWorkflowReleaseHook"."CredentialVariable" IS 'Optional name of the environment variable, which must start with JUBE_HOOK_, whose value is sent in the header named by HeaderName. The credential itself is never stored in the database.';
                        COMMENT ON COLUMN public."CaseWorkflowReleaseHook"."Body" IS 'Optional body template for a POST, in which the tokens of the URL and [@Decision.Action@], [@Decision.Reason@] and [@Decision.User@] may be used; each value is escaped for JSON. When blank a fixed JSON body is sent.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
