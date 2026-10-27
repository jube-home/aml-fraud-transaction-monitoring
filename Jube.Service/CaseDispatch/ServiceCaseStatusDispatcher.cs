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

using Jube.Dto.Options;
using Jube.Engine.CaseEscalation;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.Extensions;
using Jube.Service.CaseDispatch.Models;

namespace Jube.Service.CaseDispatch
{
    public sealed class ServiceCaseStatusDispatcher : ICaseStatusDispatcher
    {
        public async Task<ICaseStatusDispatch> PrepareAsync(CaseStatusDispatchRequest request,
            CancellationToken token = default)
        {
            var status = request.Status;
            var payload = request.Payload;

            var source = new DispatchSource(CaseDispatchSourceKind.Status, status.Id, status.Name ?? string.Empty,
                status.Sql, status.EnableNotification == 1, status.NotificationTypeId ?? 1,
                status.NotificationDestination, status.NotificationSubject, status.NotificationBody,
                status.EnableHttpEndpoint == 1, status.HttpEndpoint, status.HttpEndpointTypeId ?? 1);

            var dispatcher = new CaseDispatcher(request.DbContext, request.Actor, request.TenantRegistryId,
                request.Environment, request.Log, true);

            var plan = await dispatcher.PrepareAsync(source,
                new DispatchCase(request.Case.Id, request.Case.CaseKey, request.Case.CaseKeyValue), null,
                payload == null
                    ? null
                    : new LegacyRender(text => payload.ReplaceTokens(text),
                        () => CaseStatusNotifier.PreparePostBodyString(request.Case, request.Serializer, status,
                            payload)), null, token).ConfigureAwait(false);

            return new Dispatch(plan);
        }

        private sealed class Dispatch(DispatchPlan plan) : ICaseStatusDispatch
        {
            public Task<string?> NotifyAsync(CancellationToken token = default)
            {
                return plan.NotifyAsync(token);
            }

            public Task<string?> CallAsync(CancellationToken token = default)
            {
                return plan.CallAsync(token);
            }
        }
    }
}
