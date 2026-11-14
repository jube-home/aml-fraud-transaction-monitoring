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
using Jube.Svirfd;
using log4net;
using Microsoft.Extensions.Hosting;

namespace Jube.App.Code
{
    public sealed class SvirfdStartup(SvirfdHost host, SvirfdSettings settings, ILog log) : IHostedService
    {
        private Task started = Task.CompletedTask;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!host.Enabled)
            {
                return;
            }

            started = host.StartAsync(CancellationToken.None);

            if (!settings.WaitForReady)
            {
                return;
            }

            var finished = await Task.WhenAny(started, Task.Delay(settings.StartupWait, cancellationToken))
                .ConfigureAwait(false);

            if (finished != started && log.IsWarnEnabled)
            {
                log.Warn($"Svirfd: the index did not load within {settings.StartupWait}; the host starts without it and lookups answer once it is ready.");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                await host.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"Svirfd: disposing the client failed with {ex.Message}.");
                }
            }
        }
    }
}
