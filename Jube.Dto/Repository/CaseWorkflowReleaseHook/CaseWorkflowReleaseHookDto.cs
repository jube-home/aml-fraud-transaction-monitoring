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

using System.ComponentModel;
using Jube.Dto.Forms;
using Jube.Dto.Interfaces;
using Jube.Dto.Options;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.CaseWorkflowReleaseHook
{
    [FormEndpoint("CaseWorkflowReleaseHook")]
    [FormKeys(Id = nameof(Id), Parent = nameof(CaseWorkflowId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Source", Order = 20)]
    [FormGroup("Release", Order = 30)]
    [FormGroup("Credential", Order = 40, Collapsed = true)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class CaseWorkflowReleaseHookDto : IUpdated, IActivatable, ILockable
    {
        [Description("Identifier of the Case Workflow this release hook belongs to. Set from the parent Case " +
                     "Workflow context; not user-editable.")]
        public int CaseWorkflowId { get; set; }

        [Description("Display name of the release hook, shown to the investigator who releases a held payment.")]
        [FormField(Group = "Identity", Order = 10, Placeholder = "e.g. Faster payments release")]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; set; }

        [Description("When true, the release hook is used to release or refuse held payments on the cases of the " +
                     "Case Workflow.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [ListColumn(Order = 40, Title = "Active")]
        public bool Active { get; set; }

        [Description("When true, the release hook is locked to read-only and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        public bool Locked { get; set; }

        [Description("Optional name of a field of the payload of the held transaction that decides whether this " +
                     "release hook handles it, for example the source system. Leave blank for a hook that handles " +
                     "every transaction no more specific hook handles.")]
        [FormField(Group = "Source", Order = 10, Placeholder = "e.g. SourceSystem")]
        [ListColumn(Order = 20, Title = "Source field")]
        public string? SourceField { get; set; }

        [Description("The value the payload field named by SourceField must equal, ignoring case, for this " +
                     "release hook to handle the transaction. Required when SourceField is given.")]
        [FormField(Group = "Source", Order = 20, Placeholder = "e.g. FasterPayments")]
        [RequiredWhen(nameof(SourceField), Op = ConditionOp.IsNotEmpty)]
        [ListColumn(Order = 30, Title = "Source value")]
        public string? SourceValue { get; set; }

        [Description("The URL called to release the payment; only an answer of exactly 200 counts as released. " +
                     "The scheme, host and port must be written out literally. The tokens [@Payload.FieldName@], " +
                     "[@Hold.Name@], [@Hold.Elevation@] and [@Hold.EntryGuid@] are allowed only in the path and " +
                     "query.")]
        [FormField(Group = "Release", Order = 10, Placeholder = "https://payments.example.com/release/[@Payload.PaymentId@]")]
        public string? ReleaseUrl { get; set; }

        [Description("Optional URL called to refuse the payment, under the same rules as ReleaseUrl. When blank, " +
                     "a refusal is recorded without calling anything.")]
        [FormField(Group = "Release", Order = 20, Placeholder = "https://payments.example.com/refuse/[@Payload.PaymentId@]")]
        public string? RefuseUrl { get; set; }

        [Description("HTTP method used for the call: 1 = POST, 2 = GET.")]
        [FormField(Group = "Release", Order = 30, Widget = "radio")]
        [Options(typeof(HttpEndpointType))]
        [NewDefault(1)]
        public int HttpEndpointTypeId { get; set; }

        [Description("Optional body template for a POST. The tokens of the URL and [@Decision.Action@], " +
                     "[@Decision.Reason@] and [@Decision.User@] may be used and each value is escaped for JSON. " +
                     "When blank, a fixed JSON body is sent.")]
        [FormField(Group = "Release", Order = 40, Widget = "textarea", Span = 2)]
        public string? Body { get; set; }

        [Description("Optional name of the request header that carries the credential, for example " +
                     "Authorization or X-Api-Key. Set together with CredentialVariable.")]
        [FormField(Group = "Credential", Order = 10, Placeholder = "Authorization")]
        [RequiredWhen(nameof(CredentialVariable), Op = ConditionOp.IsNotEmpty)]
        public string? HeaderName { get; set; }

        [Description("Optional name of the environment variable that holds the credential. It must start with " +
                     "JUBE_HOOK_. The credential itself is never stored in the configuration.")]
        [FormField(Group = "Credential", Order = 20, Placeholder = "JUBE_HOOK_PAYMENTS")]
        [RequiredWhen(nameof(HeaderName), Op = ConditionOp.IsNotEmpty)]
        public string? CredentialVariable { get; set; }

        [Description("Server-assigned globally-unique identifier of this release hook. Read-only.")]
        public Guid Guid { get; set; }

        [Description("Server-assigned identifier. Read-only; required when updating an existing release hook.")]
        public int Id { get; set; }

        [Description("Server-assigned creation timestamp. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Server-assigned user who created this release hook. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("Server-assigned user who most recently updated this release hook. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true)]
        public string? UpdatedUser { get; set; }

        [Description("Server-assigned timestamp of the most recent update. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned version number, incremented on every update. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("Server-assigned user who soft-deleted this release hook, if any. Read-only.")]
        public string? DeletedUser { get; set; }

        [Description("Server-assigned timestamp this release hook was soft-deleted, if any. Read-only.")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}
