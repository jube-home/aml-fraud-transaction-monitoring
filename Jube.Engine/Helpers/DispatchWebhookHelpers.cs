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

namespace Jube.Engine.Helpers
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using BackgroundTasks.Context;
    using Case;
    using Case.Outbound;
    using Dto.Hold;
    using Dto.Options;
    using EntityAnalysisModelInvoke.Models;

    public static class DispatchWebhookHelpers
    {
        public static async Task DispatchWebhookAsync(Context context, Webhook webhook,
            CancellationToken token = default)
        {
            DispatchResult result;

            try
            {
                var headerName = HookCredentials.IsAllowedHeaderName(webhook.HeaderName) ? webhook.HeaderName : null;
                var secret = headerName == null ? null : HookCredentials.ReadSecret(webhook.SecretEnvironmentVariable);

                var guarded = GuardedHttpEndpoint.For(context.Services.DynamicEnvironment);

                result = webhook.HttpEndpointTypeId == (int)HttpEndpointType.Post
                    ? await guarded.PostAsync(webhook.Url, webhook.Body ?? "{}", context.Services.Log, headerName,
                        secret, token).ConfigureAwait(false)
                    : await guarded.GetAsync(webhook.Url, context.Services.Log, headerName, secret, token)
                        .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"Webhook Dispatch: Unexpected error: {ex}");

                result = DispatchResult.Failure($"{ex.GetType().Name}: {ex.Message}");
            }

            if (webhook.EntityAnalysisModelInstanceEntryGuid == Guid.Empty)
            {
                return;
            }

            HoldLegRecorder.Record(context, webhook.EntityAnalysisModelInstanceEntryGuid, new ResponseHoldLeg
            {
                KindId = (int)ResponseHoldLegKind.Webhook,
                OutcomeId = (int)(result.Ok
                    ? ResponseHoldLegOutcome.Sent
                    : result.Refusal != null
                        ? ResponseHoldLegOutcome.Refused
                        : ResponseHoldLegOutcome.Failed),
                Date = DateTime.UtcNow,
                HttpStatus = result.Status,
                Detail = result.Ok ? null : result.Error
            });
        }
    }
}
