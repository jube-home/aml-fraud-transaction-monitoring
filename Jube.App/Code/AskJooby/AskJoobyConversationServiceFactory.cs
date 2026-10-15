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
using System.Threading;
using System.Threading.Tasks;
using Jube.AskJooby.Guidance.Interfaces;
using Jube.AskJooby.Routing.Interfaces;
using Jube.AskJooby.Tools.OpenCorporates.Interfaces;
using Jube.AskJooby.Tools.WebSearch.Interfaces;
using Jube.AskJooby.Tools.WebSearch.Models;
using Jube.Data.Context;
using Jube.Service.AskJoobyConversation;
using Jube.Service.AskJoobyRagChunk.Interfaces;
using Jube.Service.Reactivity.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using log4net;

namespace Jube.App.Code.AskJooby
{
    public static class AskJoobyConversationServiceFactory
    {
        public static Task<AskJoobyConversationService> CreateAsync(DbContext dbContext, string userName,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            IChatClient chatClient, IChatClient validationChatClient,
            IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
            IServiceProvider serviceProvider, CancellationToken token)
        {
            var enableValidation = Flag(dynamicEnvironment, "AskJoobyEnableValidation");
            var enableReranking = FlagDefaultTrue(dynamicEnvironment, "AskJoobyEnableReranking");
            var maxToolResultCharacters = Integer(dynamicEnvironment, "AskJoobyMaxToolResultCharacters") ?? 6000;
            var maxSearchesPerTurn = Integer(dynamicEnvironment, "AskJoobyMaxSearchesPerTurn") ?? 64;
            var enableSuggestions = Flag(dynamicEnvironment, "AskJoobyEnableSuggestions");
            var embedAllMessages = Flag(dynamicEnvironment, "AskJoobyEmbedAllMessages");
            var ragTopK = Integer(dynamicEnvironment, "AskJoobyRagTopK") ?? 5;
            var ragMinScore = Real(dynamicEnvironment, "AskJoobyRagMinScore") ?? 0;
            var validationRagMinScore = Real(dynamicEnvironment, "AskJoobyValidationRagMinScore") ?? ragMinScore;
            var temperature = (float?)Real(dynamicEnvironment, "AskJoobyTemperature");
            var validationTemperature = (float?)Real(dynamicEnvironment, "AskJoobyValidationTemperature");
            var maxFanOut = Integer(dynamicEnvironment, "AskJoobyMaxFanOut") ?? 8;
            var memoryDecayHalfLifeDays = Real(dynamicEnvironment, "AskJoobyMemoryDecayHalfLifeDays") ?? 30;
            var connectionString = dynamicEnvironment.AppSettings("ConnectionString");
            var serviceToolsReadOnly = Flag(dynamicEnvironment, "AskJoobyReadOnly");
            var maxConcurrentToolCalls = Integer(dynamicEnvironment, "AskJoobyMaxConcurrentToolCalls") ?? 4;
            var webSearchMaxResults = Integer(dynamicEnvironment, "AskJoobyWebSearchMaxResults") ?? 5;

            var webSearchDepth =
                Enum.TryParse<WebSearchDepth>(dynamicEnvironment.AppSettings("AskJoobyWebSearchDepth"), true,
                    out var parsedDepth)
                    ? parsedDepth
                    : WebSearchDepth.Advanced;

            var webSearchAnswerMode =
                Enum.TryParse<WebSearchAnswerMode>(dynamicEnvironment.AppSettings("AskJoobyWebSearchAnswerMode"),
                    true, out var parsedAnswerMode)
                    ? parsedAnswerMode
                    : WebSearchAnswerMode.Advanced;

            var webSearchRawContentMode =
                Enum.TryParse<WebSearchRawContentMode>(
                    dynamicEnvironment.AppSettings("AskJoobyWebSearchRawContentMode"), true,
                    out var parsedRawContentMode)
                    ? parsedRawContentMode
                    : WebSearchRawContentMode.Markdown;

            var openCorporatesMaxResults = Integer(dynamicEnvironment, "AskJoobyOpenCorporatesMaxResults") ?? 5;

            return AskJoobyConversationService.CreateAsync(dbContext, userName, log, stringLocalizerFactory,
                serviceChangeBus, chatClient, embeddingGenerator, enableValidation,
                maxToolResultCharacters, maxSearchesPerTurn,
                enableSuggestions,
                validationChatClient, embedAllMessages, ragTopK, ragMinScore, validationRagMinScore,
                temperature, validationTemperature, maxFanOut, connectionString,
                serviceToolsReadOnly, dynamicEnvironment, maxConcurrentToolCalls,
                enableReranking ? serviceProvider.GetService<IRagReranker>() : null,
                memoryDecayHalfLifeDays, serviceProvider.GetService<IAgentGuidanceExchange>(),
                serviceProvider.GetService<IAskJoobyTurnTracker>(),
                serviceProvider.GetService<IAgentRoutingLedger>(),
                serviceProvider.GetService<IWebSearchClient>(), webSearchMaxResults, webSearchDepth,
                webSearchAnswerMode, webSearchRawContentMode,
                serviceProvider.GetService<IOpenCorporatesClient>(), openCorporatesMaxResults, token);
        }

        public static bool Flag(DynamicEnvironment.DynamicEnvironment dynamicEnvironment, string key)
        {
            return "True".Equals(dynamicEnvironment.AppSettings(key), StringComparison.OrdinalIgnoreCase);
        }

        public static bool FlagDefaultTrue(DynamicEnvironment.DynamicEnvironment dynamicEnvironment, string key)
        {
            return !"False".Equals(dynamicEnvironment.AppSettings(key), StringComparison.OrdinalIgnoreCase);
        }

        private static int? Integer(DynamicEnvironment.DynamicEnvironment dynamicEnvironment, string key)
        {
            return int.TryParse(dynamicEnvironment.AppSettings(key), out var parsed) ? parsed : null;
        }

        private static double? Real(DynamicEnvironment.DynamicEnvironment dynamicEnvironment, string key)
        {
            return double.TryParse(dynamicEnvironment.AppSettings(key), NumberStyles.Float,
                CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
        }
    }
}