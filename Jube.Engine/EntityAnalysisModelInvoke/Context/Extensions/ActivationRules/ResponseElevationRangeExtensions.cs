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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HookTokens = Jube.Case.HookTokens;
using OutboundHostPolicy = Jube.Case.Outbound.OutboundHostPolicy;
using WebhookTarget = Jube.Case.WebhookTarget;
using Jube.Dto.Hold;
using Jube.Dto.Options;
using Jube.Engine.EntityAnalysisModelInvoke.Models;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.Extensions;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ActivationRules
{
    public static class ResponseElevationRangeExtensions
    {
        public static async Task ApplyResponseElevationRangeAsync(this Context context)
        {
            var payload = context.EntityAnalysisModelInstanceEntryPayload;
            var value = payload.ResponseElevation.Value;

            if (!(value > 0))
            {
                context.TraceLog($"response elevation is zero so no response elevation range applies.");

                return;
            }

            var level = (int)value;
            var range = context.Snapshot.EntityAnalysisModelResponseElevationRanges.FirstOrDefault(f => f.Contains(level));

            if (range == null)
            {
                context.TraceLog($"response elevation {level} is not inside any active range, so none applies.");

                return;
            }

            payload.ResponseElevation.Name = range.Name;

            if (payload.EntityAnalysisModelReprocessingRuleInstanceId.HasValue)
            {
                context.TraceLog(
                    $"response elevation range {range.Id} has named the elevation but is not stamped nor dispatched on reprocessing.");

                return;
            }

            if (!range.Decline && !range.Hold && !range.EnableHttpEndpoint && !range.EnableNotification)
            {
                return;
            }

            var hold = range.Decline || range.Hold
                ? new ResponseHold
                {
                    StateId = (int)(range.Decline ? ResponseHoldState.Declined : ResponseHoldState.Held),
                    RangeId = range.Id,
                    RangeName = range.Name,
                    RangeMinimum = range.Minimum,
                    RangeMaximum = range.Maximum,
                    Elevation = level,
                    StampedDate = DateTime.UtcNow
                }
                : null;

            if (await ClaimLegsAsync(context, range).ConfigureAwait(false))
            {
                QueueNotification(context, range, hold);
                QueueWebhook(context, range, hold);
            }

            payload.Hold = hold;

            context.TraceLog(
                $"response elevation range {range.Id} has been applied with {hold?.Legs.Count ?? 0} legs queued.");
        }

        private static async Task<bool> ClaimLegsAsync(Context context, EntityAnalysisModelResponseElevationRange range)
        {
            if (!range.EnableHttpEndpoint && !range.EnableNotification)
            {
                return false;
            }

            if (!context.Environment.AppSettings("EnableNotification")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                context.TraceLog($"has not queued the response elevation legs as notification is disabled.");

                return false;
            }

            if (!context.Environment.AppSettings("ActivationRuleIdempotency")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var claimed = await context.EntityAnalysisModel.Services.CacheService
                .CacheActivationNotificationIdempotencyRepository.CheckAndClaimIdempotencyAsync(
                    context.EntityAnalysisModel.Instance.TenantRegistryId,
                    context.EntityAnalysisModel.Instance.Guid,
                    context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid,
                    range.Guid).ConfigureAwait(false);

            if (!claimed)
            {
                context.TraceLog($"response elevation range guid {range.Guid} has failed the idempotency check.");
            }

            return claimed;
        }

        private static void QueueNotification(Context context, EntityAnalysisModelResponseElevationRange range,
            ResponseHold hold)
        {
            if (!range.EnableNotification)
            {
                return;
            }

            var payload = context.EntityAnalysisModelInstanceEntryPayload;

            var notification = new Notification
            {
                NotificationBody = payload.ReplaceTokens(range.NotificationBody),
                NotificationDestination = payload.ReplaceTokens(range.NotificationDestination),
                NotificationSubject = payload.ReplaceTokens(range.NotificationSubject),
                NotificationTypeId = range.NotificationTypeId,
                EntityAnalysisModelInstanceEntryGuid = hold == null ? null : payload.EntityAnalysisModelInstanceEntryGuid
            };

            var email = range.NotificationTypeId == (int)NotificationType.Email;

            hold?.Legs.Add(new ResponseHoldLeg
            {
                KindId = (int)(email ? ResponseHoldLegKind.Email : ResponseHoldLegKind.Sms),
                OutcomeId = (int)ResponseHoldLegOutcome.Queued,
                Date = DateTime.UtcNow,
                Destination = email ? notification.NotificationDestination : Mask(notification.NotificationDestination)
            });

            if (context.Environment.AppSettings("AMQP").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                var jsonString = JsonConvert.SerializeObject(notification,
                    context.EntityAnalysisModel.JsonSerializationHelper.DefaultJsonSerializerSettingsSettings);

                var bodyBytes = Encoding.UTF8.GetBytes(jsonString);
                context.EntityAnalysisModel.Services.RabbitMqChannel.BasicPublish("", "jubeNotifications", null,
                    bodyBytes);

                context.TraceLog($"has sent a message to the notification dispatcher as {jsonString}.");
            }
            else
            {
                context.EntityAnalysisModel.ConcurrentQueues.PendingNotifications.Enqueue(notification);

                context.TraceLog(
                    $"has not sent a message to the internal notification dispatcher because AMQP is not enabled.");
            }
        }

        private static void QueueWebhook(Context context, EntityAnalysisModelResponseElevationRange range,
            ResponseHold hold)
        {
            if (!range.EnableHttpEndpoint)
            {
                return;
            }

            var payload = context.EntityAnalysisModelInstanceEntryPayload;

            var builtins = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Hold.Name"] = range.Name,
                ["Hold.Elevation"] = ((int)payload.ResponseElevation.Value).ToString(),
                ["Hold.EntryGuid"] = payload.EntityAnalysisModelInstanceEntryGuid.ToString()
            };

            var (url, refusal) = WebhookTarget.Resolve(range.HttpEndpoint,
                template => payload.ReplaceTokens(HookTokens.Replace(template, builtins!, HookTokens.Url),
                    HookTokens.Url));

            refusal ??= url == null ? null : OutboundHostPolicy.From(context.Environment).CheckUri(new Uri(url));

            if (url == null || refusal != null)
            {
                hold?.Legs.Add(new ResponseHoldLeg
                {
                    KindId = (int)ResponseHoldLegKind.Webhook,
                    OutcomeId = (int)ResponseHoldLegOutcome.Refused,
                    Date = DateTime.UtcNow,
                    Detail = refusal
                });

                context.TraceLog($"has refused the response elevation web hook as {refusal}.");

                return;
            }

            var webhook = new Webhook
            {
                Url = url,
                HttpEndpointTypeId = range.HttpEndpointTypeId,
                Body = range.HttpEndpointTypeId == (int)HttpEndpointType.Post ? BuildBody(payload, range) : null,
                HeaderName = range.HttpEndpointHeaderName,
                SecretEnvironmentVariable = range.HttpEndpointCredentialVariable,
                EntityAnalysisModelInstanceEntryGuid = payload.EntityAnalysisModelInstanceEntryGuid
            };

            hold?.Legs.Add(new ResponseHoldLeg
            {
                KindId = (int)ResponseHoldLegKind.Webhook,
                OutcomeId = (int)ResponseHoldLegOutcome.Queued,
                Date = DateTime.UtcNow,
                Destination = new Uri(url).GetLeftPart(UriPartial.Path)
            });

            if (context.Environment.AppSettings("AMQP").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                var jsonString = JsonConvert.SerializeObject(webhook,
                    context.EntityAnalysisModel.JsonSerializationHelper.DefaultJsonSerializerSettingsSettings);

                var bodyBytes = Encoding.UTF8.GetBytes(jsonString);
                context.EntityAnalysisModel.Services.RabbitMqChannel.BasicPublish("", "jubeWebhooks", null,
                    bodyBytes);

                context.TraceLog($"has sent a message to the web hook dispatcher.");
            }
            else
            {
                context.EntityAnalysisModel.ConcurrentQueues.PendingWebhooks.Enqueue(webhook);

                context.TraceLog($"has not sent a message to the internal web hook dispatcher because AMQP is not enabled.");
            }
        }

        private static string BuildBody(EntityAnalysisModelInstanceEntryPayload payload,
            EntityAnalysisModelResponseElevationRange range)
        {
            return new JObject
            {
                ["source"] = "responseElevation",
                ["entityAnalysisModelInstanceEntryGuid"] = payload.EntityAnalysisModelInstanceEntryGuid,
                ["elevation"] = (int)payload.ResponseElevation.Value,
                ["name"] = range.Name,
                ["decline"] = range.Decline,
                ["hold"] = range.Hold,
                ["createdDate"] = payload.CreatedDate
            }.ToString(Formatting.None);
        }

        private static string Mask(string destination)
        {
            return string.IsNullOrEmpty(destination) || destination.Length <= 4
                ? "****"
                : $"***{destination[^4..]}";
        }
    }
}
