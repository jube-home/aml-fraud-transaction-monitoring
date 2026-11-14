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
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.EntityAnalysisModelResponseElevationRange
{
    [FormEndpoint("EntityAnalysisModelResponseElevationRange")]
    [FormApproval]
    [FormKeys(Id = nameof(Id), Parent = nameof(EntityAnalysisModelId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Range", Order = 20)]
    [FormGroup("Outcome", Order = 30)]
    [FormGroup("Response", Order = 40)]
    [FormGroup("HttpEndpoint", Order = 50, Collapsed = true)]
    [FormGroup("Notification", Order = 60, Collapsed = true)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    [MessagePackObject]
    public class EntityAnalysisModelResponseElevationRangeDto : IUpdated, IActivatable, ILockable, ITreeChild
    {
        [Description("Identifier of the Model this Response Elevation Range is registered against. Set from the " +
                     "parent Model context; not user-editable.")]
        [Key(0)]
        public int EntityAnalysisModelId { get; set; }

        [Description("Display name of this Response Elevation Range, shown beside the integer level on the " +
                     "Activation Watcher and the Case Key Journal. Required and unique within the Model " +
                     "(case-insensitive).")]
        [FormField(Group = "Identity", Order = 10, Placeholder = "e.g. Hold for review")]
        [ListColumn(Order = 10, Title = "Name")]
        [Key(1)]
        public string? Name { get; set; }

        [Description("Globally-addressable identifier for this row, stable across environments and the form "
                     + "in which every other area refers to it. Server-assigned. Read-only.")]
        [Key(2)]
        public Guid Guid { get; set; }

        [Description("When true, Activation Rules may elevate the response to a level inside this range. A range " +
                     "that is not active is not sent out.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        [ListColumn(Order = 60, Title = "Active")]
        [Key(3)]
        public bool Active { get; set; }

        [Description("When true, this Response Elevation Range is locked and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        [Key(4)]
        public bool Locked { get; set; }

        [Description("The lowest integer response elevation level, inclusive, that falls in this range. The " +
                     "receiving system expects an integer. Must not be negative.")]
        [FormField(Group = "Range", Order = 10, Widget = "number")]
        [NewDefault(0)]
        [ListColumn(Order = 20, Title = "From")]
        [Key(5)]
        public int Minimum { get; set; }

        [Description("The highest integer response elevation level, inclusive, that falls in this range; equal to " +
                     "Minimum for a single level. Must not be below Minimum, and the range must not overlap " +
                     "another range of the Model.")]
        [FormField(Group = "Range", Order = 20, Widget = "number")]
        [NewDefault(0)]
        [ListColumn(Order = 30, Title = "To")]
        [Key(6)]
        public int Maximum { get; set; }

        [Description("When true, the outcome of reaching this range is a decline. A decline never holds, so " +
                     "Decline and Hold cannot both be true.")]
        [FormField(Group = "Outcome", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        [ListColumn(Order = 40, Title = "Decline")]
        [Key(7)]
        public bool Decline { get; set; }

        [Description("When true, the outcome of reaching this range is that the payment is held for review. Cannot " +
                     "be true together with Decline.")]
        [FormField(Group = "Outcome", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        [ListColumn(Order = 50, Title = "Hold")]
        [Key(8)]
        public bool Hold { get; set; }

        [Description("Free text served in the response when this range is reached -- for example, the message to " +
                     "be shown to the customer.")]
        [FormField(Group = "Response", Order = 10, Widget = "textarea",
            Placeholder = "e.g. This transaction needs verification.")]
        [Key(9)]
        public string? Content { get; set; }

        [Description("A URL to redirect the user or advertising exchange to when this range is reached.")]
        [FormField(Group = "Response", Order = 20, Placeholder = "https://example.com/verify")]
        [Key(10)]
        public string? Redirect { get; set; }

        [Description("The fore colour (hex) sent to the Activation Watcher and in the response when this range is " +
                     "reached.")]
        [FormField(Group = "Response", Order = 30, Widget = "colour")]
        [Key(11)]
        public string? ForeColor { get; set; }

        [Description("The back colour (hex) sent to the Activation Watcher and in the response when this range is " +
                     "reached.")]
        [FormField(Group = "Response", Order = 40, Widget = "colour")]
        [Key(12)]
        public string? BackColor { get; set; }

        [Description("When true, a web hook is called asynchronously when this range is reached.")]
        [FormField(Group = "HttpEndpoint", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        [Key(13)]
        public bool EnableHttpEndpoint { get; set; }

        [Description(
            "Web hook URL to call. Required when EnableHttpEndpoint is true. The scheme, host and port must be " +
            "written out literally; tokens such as [@Payload.AccountId@], [@Hold.Name@] and " +
            "[@Hold.Elevation@] are allowed only in the path and query.")]
        [FormField(Group = "HttpEndpoint", Order = 20, Placeholder = "https://example.com/hooks/elevation")]
        [VisibleWhen(nameof(EnableHttpEndpoint), true)]
        [RequiredWhen(nameof(EnableHttpEndpoint), true)]
        [Key(14)]
        public string? HttpEndpoint { get; set; }

        [Description("HTTP method used for the web hook call: 1 = POST, 2 = GET. Required when " +
                     "EnableHttpEndpoint is true.")]
        [FormField(Group = "HttpEndpoint", Order = 30, Widget = "radio")]
        [VisibleWhen(nameof(EnableHttpEndpoint), true)]
        [Options(typeof(HttpEndpointType))]
        [NewDefault(1)]
        [Key(15)]
        public int HttpEndpointTypeId { get; set; }

        [Description("Optional name of a request header that carries a credential on the web hook call, for " +
                     "example Authorization or X-Api-Key. Set together with HttpEndpointCredentialVariable.")]
        [FormField(Group = "HttpEndpoint", Order = 40, Placeholder = "Authorization")]
        [VisibleWhen(nameof(EnableHttpEndpoint), true)]
        [RequiredWhen(nameof(HttpEndpointCredentialVariable), Op = ConditionOp.IsNotEmpty)]
        [Key(16)]
        public string? HttpEndpointHeaderName { get; set; }

        [Description("Optional name of the environment variable that holds the credential sent in the header. " +
                     "It must start with JUBE_HOOK_. The credential itself is never stored in the configuration.")]
        [FormField(Group = "HttpEndpoint", Order = 50, Placeholder = "JUBE_HOOK_PAYMENTS")]
        [VisibleWhen(nameof(EnableHttpEndpoint), true)]
        [RequiredWhen(nameof(HttpEndpointHeaderName), Op = ConditionOp.IsNotEmpty)]
        [Key(17)]
        public string? HttpEndpointCredentialVariable { get; set; }

        [Description("When true, a notification is sent asynchronously when this range is reached.")]
        [FormField(Group = "Notification", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        [Key(18)]
        public bool EnableNotification { get; set; }

        [Description("Notification channel: 1 Email, 2 SMS. Required when EnableNotification is true.")]
        [FormField(Group = "Notification", Order = 20, Widget = "radio", Span = 2)]
        [VisibleWhen(nameof(EnableNotification), true)]
        [Options(typeof(NotificationType))]
        [NewDefault(1)]
        [Key(19)]
        public int NotificationTypeId { get; set; }

        [Description("Notification destination (email address or phone number). Required when " +
                     "EnableNotification is true.")]
        [FormField(Group = "Notification", Order = 30, Placeholder = "name@example.com")]
        [VisibleWhen(nameof(EnableNotification), true)]
        [RequiredWhen(nameof(EnableNotification), true)]
        [Key(20)]
        public string? NotificationDestination { get; set; }

        [Description("Notification subject line, for an Email notification.")]
        [FormField(Group = "Notification", Order = 40, Placeholder = "Response elevated for [@AccountId]")]
        [VisibleWhen(nameof(EnableNotification), true)]
        [Key(21)]
        public string? NotificationSubject { get; set; }

        [Description("Notification body. Tokens are replaced with values from the Payload.")]
        [FormField(Group = "Notification", Order = 50, Widget = "textarea",
            Placeholder = "Account [@AccountId] reached this response elevation.")]
        [VisibleWhen(nameof(EnableNotification), true)]
        [Key(22)]
        public string? NotificationBody { get; set; }

        [Description("The name and the integer levels of this range, for example 'Hold for review (100-199)'. " +
                     "Server-assigned. Read-only; used as the text of the Activation Rule's response elevation " +
                     "picker.")]
        [Key(23)]
        public string? Label { get; set; }

        [Description("Server-assigned row identifier. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        [Key(24)]
        public int Id { get; set; }

        [Description("User who created this Response Elevation Range. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        [Key(25)]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) this Response Elevation Range was created. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true, Widget = "date")]
        [Key(26)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("User who last updated this Response Elevation Range. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        [Key(27)]
        public string? UpdatedUser { get; set; }

        [Description("Timestamp (UTC) this Response Elevation Range was last updated. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true, Widget = "date")]
        [Key(28)]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned optimistic-concurrency version number, incremented on every update. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 60, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        [Key(29)]
        public int Version { get; set; }

        [Description("User who deleted this Response Elevation Range, if soft-deleted. Server-assigned. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 70, ReadOnly = true)]
        [Key(30)]
        public string? DeletedUser { get; set; }

        [Description("Timestamp (UTC) this Response Elevation Range was soft-deleted, if applicable. " +
                     "Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 80, ReadOnly = true, Widget = "date")]
        [Key(31)]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}