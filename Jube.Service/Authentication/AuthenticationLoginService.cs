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

using Jube.Data.Context;
using Jube.Dto.Authentication;
using Jube.Service.Exceptions.Authentication;
using Jube.Service.Observability;
using Jube.Service.Reactivity;
using Jube.Validations.Authentication;
using log4net;

namespace Jube.Service.Authentication
{
    public sealed class AuthenticationLoginService(
        DbContext dbContext,
        DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
        ILog log,
        IAuthenticationCookieIssuer cookieIssuer,
        IMfaVerifier mfaVerifier,
        IPasswordHashScheme? passwordHashScheme = null,
        TimeProvider? timeProvider = null,
        ILog? auditLog = null)
    {
        private const string Area = "Authentication";

        private readonly ILog auditLog = auditLog ?? LogManager.GetLogger("Jube.Audit");

        private readonly Authentication service = new(dbContext,
            dynamicEnvironment.AppSettings("PasswordAsymmetricEncryption")
                .Equals("True", StringComparison.OrdinalIgnoreCase),
            dynamicEnvironment.AppSettings("PasswordAsymmetricEncryptionPrivateKey"),
            passwordHashScheme, timeProvider);

        private bool OAuth => Setting("OAuthAuthentication");
        private bool Negotiate => Setting("NegotiateAuthentication");
        private bool MfaEnabled => Setting("EnableMultifactorAuthentication");

        private bool Setting(string key)
        {
            return dynamicEnvironment.AppSettings(key).Equals("True", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<AuthenticationOutcome> WirePasswordHashAsync(AuthenticationSchemaRequestDto? request,
            NegotiateIdentity identity, CancellationToken token = default)
        {
            using var op = Start("WirePasswordHash", null);
            if (request == null)
            {
                op.Outcome("bad-request");
                return AuthenticationOutcome.BadRequest();
            }

            if (!OAuth && !Negotiate)
            {
                var wire = request.UserName != null
                           && await service.IsWirePasswordHashAsync(request.UserName, token).ConfigureAwait(false);
                return Succeeded(op, AuthenticationOutcome.WireScheme(wire));
            }

            _ = identity;
            op.Outcome("not-found");
            return AuthenticationOutcome.NotFound();
        }

        public async Task<AuthenticationOutcome> ByNegotiateAsync(AuthenticationRequestContext context,
            CancellationToken token = default)
        {
            var identity = context.Identity;
            using var op = Start("ByNegotiate", identity);
            if (OAuth || !Negotiate)
            {
                op.Outcome("not-found");
                return AuthenticationOutcome.NotFound();
            }

            if (!identity.IsAuthenticated)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("ByNegotiate: unauthenticated identity presented, rejecting.");
                }

                op.Outcome("unauthorized");
                return AuthenticationOutcome.Unauthorized();
            }

            var name = identity.Name;
            try
            {
                if (name != null)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"ByNegotiate: resolved identity '{Safe(name)}' from Negotiate, proceeding to AuthenticateByNegotiateAsync.");
                    }

                    await service.AuthenticateByNegotiateAsync(name, context.LocalIp, context.UserAgent, true, token,
                            context.RemoteIp)
                        .ConfigureAwait(false);
                }
                else
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn("ByNegotiate: Negotiate authenticated but Identity.Name was null.");
                    }

                    op.Outcome("unauthorized");
                    return AuthenticationOutcome.Unauthorized();
                }
            }
            catch (Exception ex)
            {
                log.Error($"ByNegotiate: AuthenticateByNegotiateAsync failed for identity '{Safe(name)}'.", ex);
                op.Outcome("forbidden");
                return AuthenticationOutcome.Forbidden();
            }

            if (MfaEnabled)
            {
                op.Outcome("mfa-required");
                return AuthenticationOutcome.Accepted();
            }

            return Succeeded(op, AuthenticationOutcome.Ok(cookieIssuer.Issue(name)));
        }

        public async Task<AuthenticationOutcome> ByNegotiateMfaAsync(AuthenticationRequestDto? model,
            AuthenticationRequestContext context, CancellationToken token = default)
        {
            var identity = context.Identity;
            using var op = Start("ByNegotiateMfa", identity);
            if (OAuth || !Negotiate)
            {
                op.Outcome("not-found");
                return AuthenticationOutcome.NotFound();
            }

            if (model == null)
            {
                op.Outcome("bad-request");
                return AuthenticationOutcome.BadRequest();
            }

            model.UserAgent = context.UserAgent;
            model.LocalIp = context.LocalIp;
            model.RemoteIp = context.RemoteIp;

            if (!identity.IsAuthenticated)
            {
                op.Outcome("forbidden");
                return AuthenticationOutcome.Forbidden();
            }

            var name = identity.Name;
            try
            {
                if (name != null)
                {
                    await service.AuthenticateByNegotiateAsync(name, model.LocalIp, model.UserAgent, true, token,
                            model.RemoteIp)
                        .ConfigureAwait(false);
                }
                else
                {
                    op.Outcome("unauthorized");
                    return AuthenticationOutcome.Unauthorized();
                }
            }
            catch (Exception)
            {
                op.Outcome("forbidden");
                return AuthenticationOutcome.Forbidden();
            }

            if (MfaEnabled)
            {
                if (string.IsNullOrEmpty(model.Mfa))
                {
                    op.Outcome("mfa-required");
                    return AuthenticationOutcome.Accepted();
                }

                var attempts = int.Parse(dynamicEnvironment.AppSettings("PasswordAttempts"));
                if (!await service.ReserveMfaAttemptAsync(name, attempts, token).ConfigureAwait(false))
                {
                    op.Outcome("unauthorized");
                    return AuthenticationOutcome.Unauthorized();
                }

                if (!await VerifyMfaAsync(name, name, model.Mfa, token).ConfigureAwait(false))
                {
                    await service.RegisterMfaFailureAsync(name, model.LocalIp, model.UserAgent, 2, attempts, token,
                        model.RemoteIp).ConfigureAwait(false);
                    op.Outcome("unauthorized");
                    return AuthenticationOutcome.Unauthorized();
                }

                await service.CompleteMfaSuccessAsync(name, model.LocalIp, model.UserAgent, 2, false, token,
                    model.RemoteIp).ConfigureAwait(false);
            }

            model.UserName = name;
            return Succeeded(op, AuthenticationOutcome.Ok(cookieIssuer.Issue(name)));
        }

        public async Task<AuthenticationOutcome> ByUserNamePasswordAsync(AuthenticationRequestDto? model,
            AuthenticationRequestContext context, CancellationToken token = default)
        {
            using var op = Start("ByUserNamePassword", null);
            if (OAuth || Negotiate)
            {
                op.Outcome("not-found");
                return AuthenticationOutcome.NotFound();
            }

            if (model == null)
            {
                op.Outcome("bad-request");
                return AuthenticationOutcome.BadRequest();
            }

            var validator = new AuthenticationRequestDtoValidator(dynamicEnvironment);
            var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
            if (!results.IsValid)
            {
                op.Outcome("invalid");
                return AuthenticationOutcome.ValidationFailed(results);
            }

            try
            {
                model.UserAgent = context.UserAgent;
                model.LocalIp = context.LocalIp;
                model.RemoteIp = context.RemoteIp;

                await service.AuthenticateByUserNamePasswordAsync(model,
                        dynamicEnvironment.AppSettings("PasswordHashingKey"),
                        int.Parse(dynamicEnvironment.AppSettings("PasswordAttempts")), token, MfaEnabled)
                    .ConfigureAwait(false);
            }
            catch (PasswordExpiredException)
            {
                op.Outcome("forbidden");
                return AuthenticationOutcome.Forbidden();
            }
            catch (PasswordNewMustChangeException)
            {
                op.Outcome("forbidden");
                return AuthenticationOutcome.Forbidden();
            }
            catch (PasswordStrengthException ex)
            {
                op.Outcome("password-strength");
                return AuthenticationOutcome.PasswordStrength(ex.Errors);
            }
            catch (Exception)
            {
                op.Outcome("unauthorized");
                return AuthenticationOutcome.Unauthorized();
            }

            var authenticatedUserName = context.Identity.Name ?? model.UserName;

            if (string.IsNullOrEmpty(authenticatedUserName))
            {
                op.Outcome("unauthorized");
                return AuthenticationOutcome.Unauthorized();
            }

            if (MfaEnabled)
            {
                if (string.IsNullOrEmpty(model.Mfa))
                {
                    op.Outcome("mfa-required");
                    return AuthenticationOutcome.Accepted();
                }

                if (!await VerifyMfaAsync(authenticatedUserName, model.UserName ?? authenticatedUserName, model.Mfa,
                        token).ConfigureAwait(false))
                {
                    await service.RegisterMfaFailureAsync(model.UserName ?? authenticatedUserName, model.LocalIp,
                            model.UserAgent, 1,
                            int.Parse(dynamicEnvironment.AppSettings("PasswordAttempts")), token, model.RemoteIp)
                        .ConfigureAwait(false);
                    op.Outcome("unauthorized");
                    return AuthenticationOutcome.Unauthorized();
                }

                await service.CompleteMfaSuccessAsync(model.UserName ?? authenticatedUserName, model.LocalIp,
                    model.UserAgent, 1, true, token,
                    model.RemoteIp).ConfigureAwait(false);
            }

            return Succeeded(op, AuthenticationOutcome.Ok(cookieIssuer.Issue(authenticatedUserName)));
        }

        public async Task<AuthenticationOutcome> ChangePasswordAsync(ChangePasswordRequestDto? model,
            NegotiateIdentity identity, CancellationToken token = default)
        {
            using var op = Start("ChangePassword", identity);
            if (OAuth || Negotiate)
            {
                op.Outcome("forbidden");
                return AuthenticationOutcome.Forbidden();
            }

            if (model == null)
            {
                op.Outcome("bad-request");
                return AuthenticationOutcome.BadRequest();
            }

            if (identity.Name == null)
            {
                op.Outcome("bad-request");
                return AuthenticationOutcome.BadRequestMessage("Authorized but the username is null.");
            }

            try
            {
                await service.ChangePasswordAsync(identity.Name, model,
                    dynamicEnvironment.AppSettings("PasswordHashingKey"), token).ConfigureAwait(false);
            }
            catch (BadCredentialsException)
            {
                op.Outcome("unauthorized");
                return AuthenticationOutcome.Unauthorized();
            }
            catch (PasswordEmptyException)
            {
                op.Outcome("bad-request");
                return AuthenticationOutcome.BadRequest();
            }
            catch (PasswordStrengthException ex)
            {
                op.Outcome("password-strength");
                return AuthenticationOutcome.PasswordStrength(ex.Errors);
            }

            return Succeeded(op, new AuthenticationOutcome(AuthenticationOutcomeKind.Ok));
        }

        private async Task<bool> VerifyMfaAsync(string subject, string counted, string code,
            CancellationToken token)
        {
            try
            {
                return await mfaVerifier.VerifyAsync(subject, code, token).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await service.ReleaseAttemptAsync(counted, token).ConfigureAwait(false);
                throw;
            }
        }

        private static AuthenticationOutcome Succeeded(OperationScope op, AuthenticationOutcome outcome)
        {
            op.Outcome("ok");
            return outcome;
        }

        private OperationScope Start(string operation, NegotiateIdentity? identity)
        {
            var actor = identity is { IsAuthenticated: true, Name: not null } ? Safe(identity.Name) : null;
            var op = OperationScope.Start(Area, operation, actor, null, auditLog, log, new NullServiceChangeBus());
            op.Outcome("error");
            return op;
        }

        private static string Safe(string? value)
        {
            if (value == null)
            {
                return "(null)";
            }

            return string.Create(value.Length, value, static (span, v) =>
            {
                for (var i = 0; i < v.Length; i++)
                {
                    span[i] = char.IsControl(v[i]) ? '?' : v[i];
                }
            });
        }
    }
}