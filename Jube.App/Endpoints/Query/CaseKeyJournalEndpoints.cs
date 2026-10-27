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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Jube.App.Code;
using Jube.Data.Context;
using Jube.Dto.Options;
using Jube.Dto.Query.CaseKeyJournal;
using Jube.Service.Charts;
using Jube.Service.Exceptions.Query.CaseKeyJournal;
using Jube.Service.Query.CaseKeyJournal;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Query
{
    public static class CaseKeyJournalEndpoints
    {
        private const string Base = "/api/CaseKeyJournal";

        public static void MapCaseKeyJournalEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("CaseKeyJournal");

            group.MapGet("Transaction", TransactionAsync)
                .Produces<CaseKeyJournalTransactionDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseKeyJournalTransactionGet");

            group.MapGet("Around", AroundAsync)
                .Produces<CaseKeyJournalAroundDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseKeyJournalAroundGet");

            group.MapGet("Transaction/Payload", PayloadAsync)
                .Produces<CaseKeyJournalPayloadDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseKeyJournalTransactionPayloadGet");

            group.MapGet("Transaction/Chart/{kind}", TransactionChartAsync)
                .Produces<string>(contentType: "image/svg+xml")
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseKeyJournalTransactionChart");

            group.MapGet("Analytics", AnalyticsAsync)
                .Produces<CaseKeyJournalAnalyticsDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseKeyJournalAnalyticsGet");

            group.MapGet("Chart/{kind}", ChartAsync)
                .Produces<string>(contentType: "image/svg+xml")
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseKeyJournalChart");

            group.MapGet("Tags", TagOptionsAsync)
                .Produces<CaseKeyJournalTagOptionsDto>()
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseKeyJournalTagOptionsGet");

            group.MapGet("TagJobs/Preview", TagJobPreviewAsync)
                .Produces<CaseKeyJournalTagJobPreviewDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseKeyJournalTagJobPreview");

            group.MapGet("TagJobs", TagJobListAsync)
                .Produces<List<CaseKeyJournalTagJobDto>>()
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseKeyJournalTagJobList");

            group.MapGet("TagJobs/{guid:guid}", TagJobGetAsync)
                .Produces<CaseKeyJournalTagJobDto>()
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseKeyJournalTagJobGet");

            group.MapPost("TagJobs", TagJobSubmitAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<CaseKeyJournalTagJobDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseKeyJournalTagJobSubmit");

            group.MapPost("TagJobs/{guid:guid}/Cancel", TagJobCancelAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<CaseKeyJournalTagJobDto>()
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseKeyJournalTagJobCancel");

            group.MapPost("TagJobs/{guid:guid}/Undo", TagJobUndoAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<CaseKeyJournalTagJobDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseKeyJournalTagJobUndo");

            group.MapGet("Track", TrackAsync)
                .Produces<CaseKeyJournalTrackDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseKeyJournalTrackGet");

            group.MapGet("Replays", ReplayListAsync)
                .Produces<CaseKeyJournalReplaysDto>()
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseKeyJournalReplayList");

            group.MapPost("Replays", ReplaySaveAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<CaseKeyJournalReplayDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseKeyJournalReplaySave");

            group.MapDelete("Replays/{guid:guid}", ReplayDeleteAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces((int)HttpStatusCode.NoContent)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseKeyJournalReplayDelete");

            group.MapGet("Replays/{guid:guid}/Summary", ReplaySummaryAsync)
                .Produces<CaseKeyJournalTrackDto>()
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseKeyJournalReplaySummaryGet");

            group.MapGet("Pins", PinListAsync)
                .Produces<CaseKeyJournalPinsDto>()
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseKeyJournalPinList");

            group.MapPut("Pins", PinSetAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<CaseKeyJournalPinsDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseKeyJournalPinSet");

            group.MapPut("Tags", TagUpdateAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<CaseKeyJournalTagDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseKeyJournalTagUpdate");

            group.MapPut("Tags/Restore", TagRestoreAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<CaseKeyJournalTagDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseKeyJournalTagRestore");

            group.MapGet("", GetAsync)
                .Produces<CaseKeyJournalDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseKeyJournalGet");

        }

        private static Task<IResult> GetAsync([AsParameters] CaseKeyJournalRequestDto request, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAsync("GET", Base, httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, async service => TypedResults.Ok(await service.GetAsync(request, token)), token);
        }


        private static Task<IResult> TagJobPreviewAsync(
            [AsParameters] CaseKeyJournalTagJobPreviewRequestDto request, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteTagJobAsync("GET", $"{Base}/TagJobs/Preview", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.PreviewAsync(request, token)));
        }

        private static Task<IResult> TagJobListAsync([FromQuery] Guid caseWorkflowGuid, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteTagJobAsync("GET", $"{Base}/TagJobs", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.ListAsync(caseWorkflowGuid, token)));
        }

        private static Task<IResult> TagJobGetAsync(Guid guid, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteTagJobAsync("GET", $"{Base}/TagJobs/{{guid}}", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.GetAsync(guid, token)));
        }

        private static Task<IResult> TagJobSubmitAsync([FromBody] CaseKeyJournalTagJobRequestDto request,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteTagJobAsync("POST", $"{Base}/TagJobs", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.SubmitAsync(request, token)));
        }

        private static Task<IResult> TagJobCancelAsync(Guid guid, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteTagJobAsync("POST", $"{Base}/TagJobs/{{guid}}/Cancel", httpContext, log,
                dynamicEnvironment, stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.CancelAsync(guid, token)));
        }

        private static Task<IResult> TagJobUndoAsync(Guid guid, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteTagJobAsync("POST", $"{Base}/TagJobs/{{guid}}/Undo", httpContext, log,
                dynamicEnvironment, stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.UndoAsync(guid, token)));
        }

        private static Task<IResult> ReplayListAsync([FromQuery] int caseId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteReplayAsync("GET", $"{Base}/Replays", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.ListAsync(caseId, token)), token);
        }

        private static Task<IResult> ReplaySaveAsync([FromBody] CaseKeyJournalReplayRequestDto request,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteReplayAsync("POST", $"{Base}/Replays", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.SaveAsync(request, token)), token);
        }

        private static Task<IResult> ReplayDeleteAsync(Guid guid, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteReplayAsync("DELETE", $"{Base}/Replays/{guid}", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service =>
                {
                    await service.DeleteAsync(guid, token);

                    return TypedResults.NoContent();
                }, token);
        }

        private static Task<IResult> ReplaySummaryAsync(Guid guid, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteReplayAsync("GET", $"{Base}/Replays/{guid}/Summary", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.SummaryAsync(guid, token)), token);
        }

        private static Task<IResult> ExecuteReplayAsync(string method, string route, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            Func<CaseKeyJournalReplayService, Task<IResult>> action, CancellationToken token)
        {
            return GuardAsync(method, route, httpContext, log, dynamicEnvironment, async dbContext =>
            {
                var service = await CaseKeyJournalReplayService.CreateAsync(dbContext,
                    httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, dynamicEnvironment,
                    token);

                return await action(service);
            });
        }

        private static Task<IResult> PinListAsync([FromQuery] int caseId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecutePinAsync("GET", $"{Base}/Pins", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.ListAsync(caseId, token)));
        }

        private static Task<IResult> PinSetAsync([FromBody] CaseKeyJournalPinRequestDto request,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecutePinAsync("PUT", $"{Base}/Pins", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.SetAsync(request, token)));
        }

        private static Task<IResult> TagOptionsAsync([FromQuery] Guid caseWorkflowGuid, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteTagAsync("GET", $"{Base}/Tags", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.GetOptionsAsync(caseWorkflowGuid, token)));
        }

        private static Task<IResult> TagUpdateAsync([FromBody] CaseKeyJournalTagRequestDto request,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteTagAsync("PUT", $"{Base}/Tags", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.UpdateAsync(request, token)));
        }

        private static Task<IResult> TagRestoreAsync([FromBody] CaseKeyJournalTagRestoreRequestDto request,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteTagAsync("PUT", $"{Base}/Tags/Restore", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.RestoreAsync(request, token)));
        }

        private static Task<IResult> TrackAsync([AsParameters] CaseKeyJournalTrackRequestDto request,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return GuardAsync("GET", $"{Base}/Track", httpContext, log, dynamicEnvironment, async dbContext =>
            {
                var service = await CaseKeyJournalTrackService.CreateAsync(dbContext,
                    httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, dynamicEnvironment,
                    token);

                return TypedResults.Ok(await service.GetAsync(request, token));
            });
        }

        private static Task<IResult> TransactionAsync([AsParameters] CaseKeyJournalTransactionRequestDto request,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAsync("GET", $"{Base}/Transaction", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.GetTransactionAsync(request, token)), token);
        }

        private static Task<IResult> AroundAsync([AsParameters] CaseKeyJournalTransactionRequestDto request,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAsync("GET", $"{Base}/Around", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.GetAroundAsync(request, token)), token);
        }

        private static Task<IResult> PayloadAsync([AsParameters] CaseKeyJournalTransactionRequestDto request,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAsync("GET", $"{Base}/Transaction/Payload", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.GetTransactionPayloadAsync(request, token)), token);
        }

        private static Task<IResult> TransactionChartAsync(CaseKeyJournalTransactionChart kind,
            [AsParameters] CaseKeyJournalTransactionRequestDto request, [FromQuery] string adaptation,
            [FromQuery] string source, [FromQuery] bool? dark, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAsync("GET", $"{Base}/Transaction/Chart/{kind}", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus, async service =>
                {
                    var transaction = await service.GetTransactionAsync(request, token);
                    var match = transaction.Adaptations.FirstOrDefault(item =>
                        item.Name == adaptation && (source is null || item.Source == source));

                    if (match is null)
                    {
                        return TypedResults.NotFound();
                    }

                    return Results.Content(
                        CaseKeyJournalTransactionChartRenderer.Render(kind, match, dark ?? false), "image/svg+xml");
                }, token);
        }

        private static Task<IResult> AnalyticsAsync([AsParameters] CaseKeyJournalAnalyticsRequestDto request,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAsync("GET", $"{Base}/Analytics", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.GetAnalyticsAsync(request, token)), token);
        }

        private static Task<IResult> ChartAsync(CaseKeyJournalChart kind,
            [AsParameters] CaseKeyJournalAnalyticsRequestDto request, [FromQuery] string field,
            [FromQuery] bool? dark, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAsync("GET", $"{Base}/Chart/{kind}", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus, async service =>
                {
                    request.Sections = CaseKeyJournalChartSections.For(kind);
                    request.Fields = kind is CaseKeyJournalChart.Progression or CaseKeyJournalChart.Distribution
                        ? field
                        : request.Fields;

                    var report = await service.GetAnalyticsAsync(request, token);

                    return Results.Content(CaseKeyJournalChartRenderer.Render(kind, report, field, dark ?? false),
                        "image/svg+xml");
                }, token);
        }

        private static Task<IResult> ExecuteAsync(string method, string route, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, Func<CaseKeyJournalService, Task<IResult>> action,
            CancellationToken token)
        {
            return GuardAsync(method, route, httpContext, log, dynamicEnvironment, async dbContext =>
            {
                var service = await CaseKeyJournalService.CreateAsync(dbContext, httpContext.User.Identity?.Name,
                    log, stringLocalizerFactory, serviceChangeBus, dynamicEnvironment, token);

                return await action(service);
            });
        }

        private static Task<IResult> ExecuteTagAsync(string method, string route, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, Func<CaseKeyJournalTagService, Task<IResult>> action)
        {
            return GuardAsync(method, route, httpContext, log, dynamicEnvironment, async dbContext =>
            {
                var service = await CaseKeyJournalTagService.CreateAsync(dbContext, httpContext.User.Identity?.Name,
                    log, stringLocalizerFactory, serviceChangeBus, dynamicEnvironment,
                    httpContext.RequestAborted);

                return await action(service);
            });
        }

        private static Task<IResult> ExecuteTagJobAsync(string method, string route, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, Func<CaseKeyJournalTagJobService, Task<IResult>> action)
        {
            return GuardAsync(method, route, httpContext, log, dynamicEnvironment, async dbContext =>
            {
                var service = await CaseKeyJournalTagJobService.CreateAsync(dbContext,
                    httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus,
                    dynamicEnvironment, httpContext.RequestAborted);

                return await action(service);
            });
        }

        private static Task<IResult> ExecutePinAsync(string method, string route, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, Func<CaseKeyJournalPinService, Task<IResult>> action)
        {
            return GuardAsync(method, route, httpContext, log, dynamicEnvironment, async dbContext =>
            {
                var service = await CaseKeyJournalPinService.CreateAsync(dbContext, httpContext.User.Identity?.Name,
                    log, stringLocalizerFactory, serviceChangeBus, dynamicEnvironment,
                    httpContext.RequestAborted);

                return await action(service);
            });
        }

        private static async Task<IResult> GuardAsync(string method, string route, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, Func<DbContext, Task<IResult>> action)
        {
            var user = httpContext.User.Identity?.Name;

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                return await action(dbContext);
            }
            catch (NotAuthenticatedException)
            {
                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{method} {route}: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (DtoValidationException ex)
            {
                return TypedResults.BadRequest(ex.Result);
            }
            catch (NotFoundException)
            {
                return TypedResults.NotFound();
            }
            catch (ConflictException ex)
            {
                return TypedResults.Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return TypedResults.BadRequest(ex.Message);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                log.Error($"{method} {route}: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}
