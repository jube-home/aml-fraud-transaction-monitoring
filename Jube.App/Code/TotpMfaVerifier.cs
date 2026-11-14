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
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Mfa.Totp;
using Jube.Service.Authentication.Interfaces;
using log4net;

namespace Jube.App.Code
{
    public sealed class TotpMfaVerifier(DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log)
        : IMfaVerifier
    {
        public async Task<bool> VerifyAsync(string userName, string code, CancellationToken token = default)
        {
            try
            {
                await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                    dynamicEnvironment.AppSettings("ConnectionString"), log);

                var repository = new UserRegistryTotpRepository(dbContext);
                var state = await repository.GetAsync(userName, token).ConfigureAwait(false);

                if (state?.ProtectedSecret == null)
                {
                    return false;
                }

                var secret = TotpSecretProtector.Unprotect(state.ProtectedSecret,
                    dynamicEnvironment.AppSettings("MfaSecretEncryptionKey"));

                if (!TotpAuthenticator.Verify(secret, code, state.LastAcceptedStep ?? 0, out var step))
                {
                    return false;
                }

                return await repository.AcceptStepAsync(userName, step, token).ConfigureAwait(false) == 1;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error($"Totp verification failed for a user as {ex.GetType().Name}.");

                return false;
            }
        }
    }
}
