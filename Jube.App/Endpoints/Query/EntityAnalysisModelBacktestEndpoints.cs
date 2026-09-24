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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Dto.Query.EntityAnalysisModelBacktest;
using Jube.Service.Exceptions;
using Jube.Service.Query.EntityAnalysisModelBacktest;
using Jube.Service.Query.EntityAnalysisModelTimeWindow;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Query
{
    public static class EntityAnalysisModelBacktestEndpoints
    {
        private const string Base = "/api/EntityAnalysisModelBacktest";

        public static void MapEntityAnalysisModelBacktestEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("EntityAnalysisModelBacktest");

            group.MapGet("Fields/{entityAnalysisModelId:int}", FieldsAsync)
                .Produces<BacktestFieldsDto>()
                .WithName("EntityAnalysisModelBacktestFields");
            group.MapGet("ByEntityAnalysisModelId/{entityAnalysisModelId:int}", ListAsync)
                .Produces<List<BacktestInstanceDto>>()
                .WithName("EntityAnalysisModelBacktestList");
            group.MapGet("{id:int}", StatusAsync)
                .Produces<BacktestInstanceDto>()
                .WithName("EntityAnalysisModelBacktestStatus");
            group.MapGet("{id:int}/Result", ResultAsync)
                .Produces<BacktestResultDto>()
                .WithName("EntityAnalysisModelBacktestResult");
            group.MapPost("", SubmitAsync)
                .Produces<BacktestSubmitResultDto>()
                .WithName("EntityAnalysisModelBacktestSubmit");
            group.MapPost("{id:int}/Stop", StopAsync)
                .Produces<BacktestInstanceDto>()
                .WithName("EntityAnalysisModelBacktestStop");
            group.MapPost("Explain", ExplainAsync)
                .Produces<BacktestExplanationDto>()
                .WithName("EntityAnalysisModelBacktestExplain");
        }

        private static Task<IResult> FieldsAsync(int entityAnalysisModelId, HttpContext httpContext,
            CancellationToken token)
        {
            return ExecuteAsync("GET", $"{Base}/Fields", httpContext,
                (_, service, cancellation) => service.FieldsAsync(entityAnalysisModelId, cancellation), token);
        }

        private static Task<IResult> ListAsync(int entityAnalysisModelId, string ruleType, int? ruleId, int? take,
            HttpContext httpContext, CancellationToken token)
        {
            return ExecuteAsync("GET", $"{Base}/ByEntityAnalysisModelId", httpContext,
                (_, service, cancellation) =>
                    service.ListAsync(entityAnalysisModelId, ruleType, ruleId, take ?? 50, cancellation), token);
        }

        private static Task<IResult> StatusAsync(int id, HttpContext httpContext, CancellationToken token)
        {
            return ExecuteAsync("GET", Base, httpContext,
                (_, service, cancellation) => service.StatusAsync(id, cancellation), token);
        }

        private static Task<IResult> ResultAsync(int id, HttpContext httpContext, CancellationToken token)
        {
            return ExecuteAsync("GET", $"{Base}/Result", httpContext,
                (_, service, cancellation) => service.ResultAsync(id, cancellation), token);
        }

        private static Task<IResult> StopAsync(int id, HttpContext httpContext, CancellationToken token)
        {
            return ExecuteAsync("POST", $"{Base}/Stop", httpContext,
                (_, service, cancellation) => service.StopAsync(id, cancellation), token);
        }

        private static Task<IResult> ExplainAsync(BacktestExplainRequestDto model, HttpContext httpContext,
            CancellationToken token)
        {
            if (model.Request == null)
            {
                return Task.FromResult<IResult>(TypedResults.BadRequest());
            }

            return ExecuteAsync("POST", $"{Base}/Explain", httpContext,
                (_, service, cancellation) =>
                    service.ExplainAsync(model.Request, model.EntityAnalysisModelInstanceEntryGuid, cancellation),
                token);
        }

        private static Task<IResult> SubmitAsync(BacktestSubmitRequestDto model, HttpContext httpContext,
            CancellationToken token)
        {
            if (model.Request == null)
            {
                return Task.FromResult<IResult>(TypedResults.BadRequest());
            }

            return ExecuteAsync("POST", Base, httpContext, async (dbContext, service, cancellation) =>
            {
                if (!string.IsNullOrEmpty(model.WindowInterval) && model.WindowValue is > 0)
                {
                    var services = httpContext.RequestServices;
                    var timeWindow = await EntityAnalysisModelTimeWindowService.CreateAsync(dbContext,
                        httpContext.User.Identity?.Name, services.GetRequiredService<ILog>(),
                        services.GetRequiredService<IStringLocalizerFactory>(),
                        services.GetRequiredService<IServiceChangeBus>(), cancellation).ConfigureAwait(false);
                    var window = await timeWindow.CalculateAsync("Calendar", model.WindowInterval,
                        model.WindowValue.Value, null, cancellation).ConfigureAwait(false);
                    if (window.Valid)
                    {
                        (model.Request.From, model.Request.To) = (window.From, window.To);
                    }
                }

                return await service.SubmitAsync(model.Request, cancellation).ConfigureAwait(false);
            }, token);
        }

        private static async Task<IResult> ExecuteAsync<T>(string method, string route, HttpContext httpContext,
            Func<DbContext, EntityAnalysisModelBacktestService, CancellationToken, Task<T>> body,
            CancellationToken token)
        {
            var services = httpContext.RequestServices;
            var log = services.GetRequiredService<ILog>();
            var dynamicEnvironment = services.GetRequiredService<DynamicEnvironment.DynamicEnvironment>();
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"{method} {route}: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await EntityAnalysisModelBacktestService.CreateAsync(dbContext, user, log,
                    services.GetRequiredService<IStringLocalizerFactory>(),
                    services.GetRequiredService<IServiceChangeBus>(),
                    dynamicEnvironment.AppSettings("ReportConnectionString"), token).ConfigureAwait(false);
                return TypedResults.Ok(await body(dbContext, service, token).ConfigureAwait(false));
            }
            catch (ServiceException ex) when (ex.Code is "PermissionDenied" or "NotAuthenticated")
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{method} {route}: 403 ({ex.Code}) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ServiceException ex) when (ex.Code == "NotFound")
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{method} {route}: 404 user={user}");
                }

                return TypedResults.NotFound(new { ex.Message });
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"{method} {route}: client cancelled user={user}");
                }

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