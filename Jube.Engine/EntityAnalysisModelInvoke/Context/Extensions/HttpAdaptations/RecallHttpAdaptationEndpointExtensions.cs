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
using System.Globalization;
using System.Threading.Tasks;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.HttpAdaptationProtocol;
using Jube.HttpAdaptationProtocol.Parsing;
using Jube.ResilientHttp;
using Polly.Timeout;
using Newtonsoft.Json;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.HttpAdaptations
{
    using EntityAnalysisModelHttpAdaptation = EntityAnalysisModelHttpAdaptation;

    public static class RecallHttpAdaptationEndpointExtensions
    {
        public static async Task<Adaptation> RecallHttpEndpointAsync(this Context context,
            EntityAnalysisModelHttpAdaptation modelAdaptation, JsonSerializerSettings jsonSerializerSettings)
        {
            Adaptation adaptation;

            try
            {
                var rawBody = await modelAdaptation
                    .PostAsync(context.EntityAnalysisModelInstanceEntryPayload, jsonSerializerSettings, context.Log)
                    .ConfigureAwait(false);

                adaptation = rawBody.ParseAdaptationResponse(context.Log);
            }
            catch (Exception ex)
            {
                context.Log.Error(
                    $"Http Adaptation: the call for {modelAdaptation.Id} failed with {ex.GetType().Name}: {ex.Message}");

                adaptation = new Adaptation
                {
                    Error = ResilientHttpExecutor.IsCircuitOpen(ex)
                        ? "The HTTP Adaptation endpoint is temporarily suspended after repeated failures."
                        : ex is TaskCanceledException or TimeoutRejectedException
                            ? "The HTTP Adaptation endpoint did not respond within the configured timeout."
                            : "The HTTP Adaptation endpoint could not be reached or returned an unsuccessful status."
                };
            }

            context.TraceLog(
                $"is evaluating {modelAdaptation.Id} and has called the HTTP Adaptation endpoint with a value of {adaptation.Value?.ToString(CultureInfo.InvariantCulture) ?? "null"} and error of {adaptation.Error ?? "none"}.");

            return adaptation;
        }
    }
}