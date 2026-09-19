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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.App.Code;
using Jube.App.Code.signalr;
using Jube.Data.Context;
using Jube.Data.Security;
using Jube.Dto.Authentication;
using Jube.Service.Authentication;
using Jube.Service.UserLogout;
using log4net;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Jube.App.Endpoints
{
    public static class AuthenticationEndpoints
    {
        private const string Base = "/api/Authentication";
        private const long MaxRequestBytes = 32 * 1024;

        public static void MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base).WithTags("Authentication")
                .WithMetadata(new RequestSizeLimitAttribute(MaxRequestBytes));

            group.MapPost("WirePasswordHash", WirePasswordHashAsync)
                .AllowAnonymous()
                .Produces<PasswordSchemeDto>()
                .WithName("AuthenticationWirePasswordHash");

            group.MapGet("ByNegotiate", ByNegotiateAsync)
                .RequireAuthorization(new AuthorizeAttribute
                    { AuthenticationSchemes = NegotiateDefaults.AuthenticationScheme })
                .Produces<AuthenticationResponseDto>()
                .WithName("AuthenticationByNegotiate");

            group.MapPost("ByNegotiateMfa", ByNegotiateMfaAsync)
                .RequireAuthorization(new AuthorizeAttribute
                    { AuthenticationSchemes = NegotiateDefaults.AuthenticationScheme })
                .Produces<AuthenticationResponseDto>()
                .WithName("AuthenticationByNegotiateMfa");

            group.MapPost("ByUserNamePassword", ByUserNamePasswordAsync)
                .AllowAnonymous()
                .Produces<AuthenticationResponseDto>()
                .WithName("AuthenticationByUserNamePassword");

            group.MapPost("ChangePassword", ChangePasswordAsync)
                .RequireAuthorization()
                .Produces<AuthenticationResponseDto>()
                .WithName("AuthenticationChangePassword");

            group.MapPost("Logout", LogoutAsync)
                .AllowAnonymous()
                .Produces(StatusCodes.Status200OK)
                .WithName("AuthenticationLogout");
        }

        private static async Task<IResult> LogoutAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            AuthenticationCookieIssuer.DeleteCookies(httpContext.Response, dynamicEnvironment);

            try
            {
                var timeProvider = httpContext.RequestServices.GetService<TimeProvider>();
                var identity = httpContext.User.Identity;

                var apiKey = httpContext.Request.Headers.ContainsKey("X-API-KEY");
                var presented = !apiKey && identity is { IsAuthenticated: true, Name: not null }
                    ? new NegotiateIdentity(true, identity.Name)
                    : NegotiateIdentity.Anonymous;

                DateTime? sessionStart = null;
                if (presented.IsAuthenticated
                    && long.TryParse(httpContext.User.FindFirst(TokenValidity.IssuedMillisecondsClaim)?.Value,
                        System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture,
                        out var startMilliseconds))
                {
                    sessionStart = DateTimeOffset.FromUnixTimeMilliseconds(startMilliseconds).UtcDateTime;
                }

                var service = new AuthenticationLogoutService(
                    () => DataConnectionDbContext.GetResilientDbContextDataConnection(
                        dynamicEnvironment.AppSettings("ConnectionString"), log),
                    log, WatcherConnectionRegistry.Instance.AbortUser, timeProvider);

                var outcome = await service.LogoutAsync(new AuthenticationLogoutRequest(presented,
                    httpContext.Connection.RemoteIpAddress?.ToString(),
                    httpContext.Connection.LocalIpAddress?.ToString(),
                    httpContext.Request.Headers["User-Agent"].ToString(), sessionStart)).ConfigureAwait(false);

                if (outcome == LogoutOutcome.RevokeFailed)
                {
                    httpContext.Response.Headers["X-Jube-Logout-Warning"] = "revocation-failed";
                }
            }
            catch (Exception e)
            {
                log.Error($"POST {Base}/Logout: unexpected failure; the cookies are cleared regardless.", e);
                httpContext.Response.Headers["X-Jube-Logout-Warning"] = "revocation-failed";
            }

            return TypedResults.Ok();
        }

        private static Task<IResult> WirePasswordHashAsync(AuthenticationSchemaRequestDto request,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            CancellationToken token)
        {
            return RunAsync("WirePasswordHash", httpContext, log, dynamicEnvironment,
                (service, context) => service.WirePasswordHashAsync(request, context.Identity, token));
        }

        private static Task<IResult> ByNegotiateAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token)
        {
            return RunAsync("ByNegotiate", httpContext, log, dynamicEnvironment,
                (service, context) => service.ByNegotiateAsync(context, token));
        }

        private static Task<IResult> ByNegotiateMfaAsync(AuthenticationRequestDto model, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token)
        {
            return RunAsync("ByNegotiateMfa", httpContext, log, dynamicEnvironment,
                (service, context) => service.ByNegotiateMfaAsync(model, context, token));
        }

        private static Task<IResult> ByUserNamePasswordAsync(AuthenticationRequestDto model, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token)
        {
            return RunAsync("ByUserNamePassword", httpContext, log, dynamicEnvironment,
                async (service, context) =>
                {
                    var timeProvider = httpContext.RequestServices.GetService<TimeProvider>() ?? TimeProvider.System;
                    var started = timeProvider.GetUtcNow();
                    var outcome = await service.ByUserNamePasswordAsync(model, context, token).ConfigureAwait(false);

                    if (outcome.Kind == AuthenticationOutcomeKind.Ok && !string.IsNullOrEmpty(model?.NewPassword))
                    {
                        await using var revokeContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                            dynamicEnvironment.AppSettings("ConnectionString"), log);
                        await TokenValidity.RevokeUserBeforeAsync(revokeContext, model.UserName, started, token)
                            .ConfigureAwait(false);
                        await RecordPasswordChangeAsync(httpContext, revokeContext, model.UserName)
                            .ConfigureAwait(false);
                    }

                    return outcome;
                });
        }

        private static Task<IResult> ChangePasswordAsync(ChangePasswordRequestDto model, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token)
        {
            return RunAsync("ChangePassword", httpContext, log, dynamicEnvironment,
                (service, context) => service.ChangePasswordAsync(model, context.Identity, token),
                RotateSessionAfterPasswordChangeAsync);
        }

        private static async Task<IResult> RunAsync(string operation, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            Func<AuthenticationLoginService, AuthenticationRequestContext, Task<AuthenticationOutcome>> run,
            Func<HttpContext, DbContext, DynamicEnvironment.DynamicEnvironment, AuthenticationRequestContext,
                AuthenticationOutcome, Task> afterOutcome = null)
        {
            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var services = httpContext.RequestServices;
                var timeProvider = services.GetService<TimeProvider>();
                var service = new AuthenticationLoginService(dbContext, dynamicEnvironment, log,
                    new HttpAuthenticationCookieIssuer(httpContext.Response, dynamicEnvironment, timeProvider),
                    services.GetService<IMfaVerifier>() ?? new RsaMfaVerifier(dynamicEnvironment, log),
                    services.GetService<IPasswordHashScheme>(), timeProvider);

                var identity = httpContext.User.Identity;
                var callerIdentityApplies = operation is "ByNegotiate" or "ByNegotiateMfa" or "ChangePassword";
                var context = new AuthenticationRequestContext(
                    callerIdentityApplies
                        ? new NegotiateIdentity(identity?.IsAuthenticated ?? false, identity?.Name)
                        : NegotiateIdentity.Anonymous,
                    httpContext.Request.Headers["User-Agent"].ToString(),
                    httpContext.Connection.LocalIpAddress?.ToString(),
                    httpContext.Connection.RemoteIpAddress?.ToString());

                var outcome = await run(service, context).ConfigureAwait(false);
                if (afterOutcome != null)
                {
                    await afterOutcome(httpContext, dbContext, dynamicEnvironment, context, outcome)
                        .ConfigureAwait(false);
                }

                return ToResult(outcome);
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"POST {Base}/{operation}: client cancelled");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"{Base}/{operation}: 500", e);
                return TypedResults.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        private static async Task RotateSessionAfterPasswordChangeAsync(HttpContext httpContext, DbContext dbContext,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, AuthenticationRequestContext context,
            AuthenticationOutcome outcome)
        {
            if (outcome.Kind != AuthenticationOutcomeKind.Ok || string.IsNullOrEmpty(context.Identity.Name))
            {
                return;
            }

            var timeProvider = httpContext.RequestServices.GetService<TimeProvider>() ?? TimeProvider.System;
            await TokenValidity.RevokeUserBeforeAsync(dbContext, context.Identity.Name, timeProvider.GetUtcNow())
                .ConfigureAwait(false);
            await RecordPasswordChangeAsync(httpContext, dbContext, context.Identity.Name).ConfigureAwait(false);
            AuthenticationCookieIssuer.IssueAuthenticationCookies(httpContext.Response, dynamicEnvironment,
                context.Identity.Name, timeProvider);
        }

        private static Task RecordPasswordChangeAsync(HttpContext httpContext, DbContext dbContext, string userName)
        {
            var log = httpContext.RequestServices.GetService<ILog>() ?? LogManager.GetLogger("Jube.Audit");
            return UserLogoutRecorder.RecordAsync(dbContext, log, new UserLogoutEntry(userName,
                UserLogoutReason.PasswordChange, UserLogoutOutcome.Revoked,
                httpContext.Connection.RemoteIpAddress?.ToString(),
                httpContext.Connection.LocalIpAddress?.ToString(),
                httpContext.Request.Headers["User-Agent"].ToString()));
        }

        private static IResult ToResult(AuthenticationOutcome outcome)
        {
            return outcome.Kind switch
            {
                AuthenticationOutcomeKind.Ok when outcome.WirePasswordHash.HasValue =>
                    TypedResults.Ok(new PasswordSchemeDto { WirePasswordHash = outcome.WirePasswordHash.Value }),
                AuthenticationOutcomeKind.Ok when outcome.Response != null => TypedResults.Ok(outcome.Response),
                AuthenticationOutcomeKind.Ok => TypedResults.Ok(),
                AuthenticationOutcomeKind.Accepted => TypedResults.StatusCode(StatusCodes.Status202Accepted),
                AuthenticationOutcomeKind.Unauthorized => TypedResults.Unauthorized(),
                AuthenticationOutcomeKind.Forbidden => TypedResults.Forbid(),
                AuthenticationOutcomeKind.NotFound => TypedResults.NotFound(),
                AuthenticationOutcomeKind.BadRequest => TypedResults.BadRequest(),
                AuthenticationOutcomeKind.ValidationFailed => TypedResults.BadRequest(outcome.Validation),
                AuthenticationOutcomeKind.PasswordStrength => TypedResults.BadRequest(new
                {
                    errors = (outcome.Errors ?? Array.Empty<string>()).Select(m => new { errorMessage = m }).ToArray()
                }),
                AuthenticationOutcomeKind.BadRequestMessage => TypedResults.BadRequest(outcome.Message),
                _ => TypedResults.StatusCode(StatusCodes.Status500InternalServerError)
            };
        }
    }
}