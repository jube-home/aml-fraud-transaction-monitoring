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
using Jube.App.Code;
using Jube.Data.Context;
using Jube.Dto.Repository.CaseReport;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.CaseReport;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.App.Endpoints.Repository
{
    public static class CaseReportEndpoints
    {
        private const string Base = "/api/CaseReport";

        public static void MapCaseReportEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base).RequireAuthorization().WithTags("CaseReport");

            group.MapGet("ByCaseId/{caseId:int}", ListAsync)
                .Produces<List<CaseReportDto>>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("CaseReportByCaseId");
            group.MapGet("AwaitingApproval", AwaitingAsync)
                .Produces<List<CaseReportDto>>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("CaseReportAwaitingApproval");
            group.MapGet("{guid:guid}", GetAsync)
                .Produces<CaseReportDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("CaseReportGet");
            group.MapPost("Start", StartAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<CaseReportDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("CaseReportStart");
            group.MapPost("Save", SaveAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<CaseReportDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("CaseReportSave");
            group.MapPost("Validate/{guid:guid}", ValidateAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<CaseReportDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("CaseReportValidate");
            group.MapPost("RequestApproval/{guid:guid}", RequestApprovalAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<CaseReportDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("CaseReportRequestApproval");
            group.MapPost("Approve", ApproveAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<CaseReportDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("CaseReportApprove");
            group.MapPost("Reject", RejectAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<CaseReportDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("CaseReportReject");
            group.MapPost("Withdraw/{guid:guid}", WithdrawAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<CaseReportDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("CaseReportWithdraw");
            group.MapPost("NewVersion/{guid:guid}", NewVersionAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<CaseReportDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("CaseReportNewVersion");
            group.MapPost("RecordSubmission", RecordSubmissionAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<CaseReportDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("CaseReportRecordSubmission");
            group.MapPost("RecordFeedback", RecordFeedbackAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<CaseReportDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("CaseReportRecordFeedback");
            group.MapGet("Download/{guid:guid}", DownloadAsync)
                .Produces((int)HttpStatusCode.OK)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("CaseReportDownload");

        }

        private static Task<IResult> ListAsync(int caseId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.ListAsync(caseId, token)), token);
        }

        private static Task<IResult> AwaitingAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.ListAwaitingApprovalAsync(token)), token);
        }

        private static Task<IResult> GetAsync(Guid guid, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.GetAsync(guid, token)), token);
        }

        private static Task<IResult> StartAsync([FromBody] CaseReportStartDto model, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.StartAsync(model, token)), token);
        }

        private static Task<IResult> SaveAsync([FromBody] CaseReportSaveDto model, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.SaveAsync(model, token)), token);
        }

        private static Task<IResult> ValidateAsync(Guid guid, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.ValidateAsync(guid, token)), token);
        }

        private static Task<IResult> RequestApprovalAsync(Guid guid, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.RequestApprovalAsync(guid, token)), token);
        }

        private static Task<IResult> ApproveAsync([FromBody] CaseReportDecisionDto model, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.ApproveAsync(model, token)), token);
        }

        private static Task<IResult> RejectAsync([FromBody] CaseReportDecisionDto model, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.RejectAsync(model, token)), token);
        }

        private static Task<IResult> WithdrawAsync(Guid guid, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.WithdrawAsync(guid, token)), token);
        }

        private static Task<IResult> NewVersionAsync(Guid guid, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.NewVersionAsync(guid, token)), token);
        }

        private static Task<IResult> RecordSubmissionAsync([FromBody] CaseReportSubmissionDto model, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.RecordSubmissionAsync(model, token)), token);
        }

        private static Task<IResult> RecordFeedbackAsync([FromBody] CaseReportFeedbackRequestDto model, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.RecordFeedbackAsync(model, token)), token);
        }

        private static Task<IResult> DownloadAsync(Guid guid, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus, async service =>
            {
                var (bytes, name, sha) = await service.DownloadAsync(guid, token);
                httpContext.Response.Headers["X-Content-SHA256"] = sha;

                return TypedResults.File(bytes, "application/xml", name);
            }, token);
        }


        private static async Task<IResult> ExecuteAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus,
            Func<CaseReportService, Task<IResult>> action, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                return await action(await CaseReportService.CreateAsync(dbContext, user, log, bus, token));
            }
            catch (Exception ex) when (ex is CaseExceptions.NotAuthenticatedException
                                           or CaseExceptions.ForbiddenException)
            {
                return TypedResults.Forbid();
            }
            catch (CaseExceptions.NotFoundException)
            {
                return TypedResults.NotFound();
            }
            catch (CaseExceptions.ConflictException ex)
            {
                return TypedResults.Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return TypedResults.BadRequest(ex.Message);
            }
        }
    }
}
