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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jube.App.Code.AskJooby;
using Jube.Data.Context;
using Jube.Dto.AskJoobyConversation;
using Jube.Service.AskJoobyConversation;
using Jube.Service.Exceptions.AskJoobyConversation;
using Jube.Service.Reactivity.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using log4net;

namespace Jube.App.Endpoints
{
    public static class AskJoobyStreamEndpoints
    {
        private const string Base = "/api/AskJoobyConversation";

        public static void MapAskJoobyStreamEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("AskJoobyConversation");

            group.MapPost("{id:int}/Start", StartAsync)
                .WithName("AskJoobyConversationStart");

            group.MapPost("{id:int}/SendMessage", SendMessageAsync)
                .WithName("AskJoobyConversationSendMessage");

            group.MapPost("{id:int}/Messages/{messageId:int}/Feedback", SubmitFeedbackAsync)
                .WithName("AskJoobyConversationSubmitFeedback");

            group.MapPost("{id:int}/Guidance", SendGuidanceAsync)
                .WithName("AskJoobyConversationSendGuidance");
        }

        private static async Task<IResult> SendGuidanceAsync(HttpContext httpContext, int id,
            AskJoobyGuidanceRequestDto request, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            [FromServices] IChatClient chatClient, [FromKeyedServices("validation")] IChatClient validationChatClient,
            [FromServices] IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
            [FromServices] IServiceProvider serviceProvider, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            if (log.IsDebugEnabled)
            {
                log.Debug($"POST {Base}/{id}/Guidance: entry user={user}");
            }

            if (string.IsNullOrWhiteSpace(request?.Guidance))
            {
                return Results.BadRequest();
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await AskJoobyConversationServiceFactory.CreateAsync(dbContext, user,
                    dynamicEnvironment, log, stringLocalizerFactory, serviceChangeBus, chatClient,
                    validationChatClient, embeddingGenerator, serviceProvider, token);

                await service.SendNudgeAsync(id, request.Guidance, token).ConfigureAwait(false);

                return Results.Accepted();
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/{id}/Guidance: 403 user={user}");
                }

                return Results.Forbid();
            }
            catch (NotFoundException)
            {
                return Results.NotFound();
            }
        }

        private static async Task StartAsync(HttpContext httpContext, int id,
            AskJoobyStartConversationRequestDto request, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            [FromServices] IChatClient chatClient, [FromKeyedServices("validation")] IChatClient validationChatClient,
            [FromServices] IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
            [FromServices] IServiceProvider serviceProvider, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            if (log.IsDebugEnabled)
            {
                log.Debug($"POST {Base}/{id}/Start: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            var service = await CreateOrForbidAsync(httpContext, dbContext, user, dynamicEnvironment, log,
                stringLocalizerFactory, serviceChangeBus, chatClient, validationChatClient, embeddingGenerator,
                serviceProvider, token);

            if (service is null)
            {
                return;
            }

            var hasInferenceBackend = AskJoobyConversationServiceFactory.Flag(dynamicEnvironment, "EnableAskJooby");

            await WriteStreamAsync(httpContext, log, user,
                service.StartConversationAsync(id, request.PageContext, hasInferenceBackend, token), token);
        }

        private static async Task SendMessageAsync(HttpContext httpContext, int id,
            AskJoobySendMessageRequestDto request, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            [FromServices] IChatClient chatClient, [FromKeyedServices("validation")] IChatClient validationChatClient,
            [FromServices] IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
            [FromServices] IServiceProvider serviceProvider, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            if (log.IsDebugEnabled)
            {
                log.Debug($"POST {Base}/{id}/SendMessage: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            var service = await CreateOrForbidAsync(httpContext, dbContext, user, dynamicEnvironment, log,
                stringLocalizerFactory, serviceChangeBus, chatClient, validationChatClient, embeddingGenerator,
                serviceProvider, token);

            if (service is null)
            {
                return;
            }

            await WriteStreamAsync(httpContext, log, user,
                service.SendMessageAsync(id, request.Text, request.PageContext,
                    [.. request.AttachmentFileIds], token), token);
        }

        private static async Task<IResult> SubmitFeedbackAsync(HttpContext httpContext, int id, int messageId,
            AskJoobyFeedbackRequestDto request, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            [FromServices] IChatClient chatClient, [FromKeyedServices("validation")] IChatClient validationChatClient,
            [FromServices] IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
            [FromServices] IServiceProvider serviceProvider, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            if (log.IsDebugEnabled)
            {
                log.Debug($"POST {Base}/{id}/Messages/{messageId}/Feedback: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await AskJoobyConversationServiceFactory.CreateAsync(dbContext, user,
                    dynamicEnvironment, log, stringLocalizerFactory, serviceChangeBus, chatClient,
                    validationChatClient, embeddingGenerator, serviceProvider, token);

                return TypedResults.Ok(
                    await service.SubmitFeedbackAsync(id, messageId, request.IsPositive, token));
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/{id}/Messages/{messageId}/Feedback: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (NotFoundException)
            {
                return TypedResults.NotFound();
            }
            catch (DtoValidationException ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/{id}/Messages/{messageId}/Feedback: 400 user={user}");
                }

                return TypedResults.BadRequest(ex.Result);
            }
        }

        private static async Task<AskJoobyConversationService> CreateOrForbidAsync(HttpContext httpContext,
            DbContext dbContext, string user, DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            [FromServices] IChatClient chatClient, IChatClient validationChatClient,
            [FromServices] IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
            [FromServices] IServiceProvider serviceProvider, CancellationToken token)
        {
            try
            {
                return await AskJoobyConversationServiceFactory.CreateAsync(dbContext, user, dynamicEnvironment,
                    log, stringLocalizerFactory, serviceChangeBus, chatClient, validationChatClient,
                    embeddingGenerator, serviceProvider, token);
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}: 403 (not authenticated) user={user}");
                }

                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;

                return null;
            }
        }

        private static async Task WriteStreamAsync(HttpContext httpContext, ILog log, string user,
            IAsyncEnumerable<AskJoobyStreamItemDto> items, CancellationToken token)
        {
            await using var enumerator = items.GetAsyncEnumerator(token);

            bool first;

            try
            {
                first = await enumerator.MoveNextAsync();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}: 403 user={user}");
                }

                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;

                return;
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}: 403 (not authenticated) user={user}");
                }

                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;

                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }

            catch (Exception e)
            {
                log.Error($"POST {Base}: stream failed before the first item user={user}", e);

                httpContext.Response.StatusCode = StatusCodes.Status200OK;
                httpContext.Response.Headers.ContentType = "text/event-stream";
                httpContext.Response.Headers.CacheControl = "no-cache";
                httpContext.Response.Headers["X-Accel-Buffering"] = "no";

                await httpContext.Response.StartAsync(CancellationToken.None);

                var failure = JsonSerializer.Serialize(new { message = e.Message }, JsonSerializerOptions.Web);
                await httpContext.Response.WriteAsync($"event: error\ndata: {failure}\n\n", CancellationToken.None);
                await httpContext.Response.Body.FlushAsync(CancellationToken.None);

                return;
            }

            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.Response.Headers.ContentType = "text/event-stream";
            httpContext.Response.Headers.CacheControl = "no-cache";
            httpContext.Response.Headers["X-Accel-Buffering"] = "no";

            await httpContext.Response.StartAsync(token);

            try
            {
                while (first)
                {
                    var payload = JsonSerializer.Serialize(enumerator.Current, JsonSerializerOptions.Web);
                    await httpContext.Response.WriteAsync($"event: item\ndata: {payload}\n\n", token);
                    await httpContext.Response.Body.FlushAsync(token);
                    first = await enumerator.MoveNextAsync();
                }

                await httpContext.Response.WriteAsync("event: complete\ndata: {}\n\n", token);
                await httpContext.Response.Body.FlushAsync(token);
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"POST {Base}: stream cancelled user={user}");
                }
            }
            catch (Exception e)
            {
                log.Error($"POST {Base}: stream failed user={user}", e);

                var payload = JsonSerializer.Serialize(new { message = e.Message }, JsonSerializerOptions.Web);
                await httpContext.Response.WriteAsync($"event: error\ndata: {payload}\n\n", CancellationToken.None);
                await httpContext.Response.Body.FlushAsync(CancellationToken.None);
            }
        }
    }
}