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

namespace Jube.Engine.CaseEscalation
{
    using System.Threading;
    using System.Threading.Tasks;
    using Jube.Case;
    using Data.Poco;
    using EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
    using EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.Extensions;
    using log4net;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public static class CaseStatusNotifier
    {
        public static async Task<ICaseStatusDispatch> PrepareAsync(CaseStatusDispatchRequest request,
            CancellationToken token = default)
        {
            var dispatcher = CaseStatusDispatchRegistry.Current;

            return dispatcher == null
                ? new LegacyCaseStatusDispatch(request)
                : await dispatcher.PrepareAsync(request, token).ConfigureAwait(false);
        }

        public static async Task<string> SendNotificationAsync(DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            ILog log, CaseWorkflowStatus status, EntityAnalysisModelInstanceEntryPayload context,
            CancellationToken token = default)
        {
            var notificationSubject = context.ReplaceTokens(status.NotificationSubject);
            var notificationDestination = context.ReplaceTokens(status.NotificationDestination);
            var notificationBody = context.ReplaceTokens(status.NotificationBody);

            var notification = new Notification(log, dynamicEnvironment);

            await notification.SendAsync(status.NotificationTypeId ?? 1, notificationDestination,
                notificationSubject, notificationBody, token);

            return notificationDestination;
        }

        public static async Task<string> CallHttpEndpointAsync(ILog log, CaseWorkflowStatus status,
            Case caseModel, JsonSerializer jsonSerializer, EntityAnalysisModelInstanceEntryPayload context)
        {
            var endpoint = context.ReplaceTokens(status.HttpEndpoint);

            if (status.HttpEndpointTypeId == 1)
            {
                await SendHttpEndpoint.PostAsync(endpoint, PreparePostBodyString(caseModel, jsonSerializer, status),
                    log);
            }
            else
            {
                await SendHttpEndpoint.GetAsync(endpoint, log);
            }

            return endpoint;
        }

        public static string PreparePostBodyString(Case caseModel, JsonSerializer jsonSerializer,
            CaseWorkflowStatus status, EntityAnalysisModelInstanceEntryPayload payload = null)
        {
            var jObject = JObject.FromObject(caseModel, jsonSerializer);

            jObject["context"] = payload == null
                ? JObject.Parse(caseModel.Json)
                : JObject.FromObject(payload);

            jObject["caseWorkflowStatus"] = status.Name;
            jObject.Remove("json");

            return jObject.ToString();
        }
    }
}
