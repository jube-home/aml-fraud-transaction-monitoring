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

namespace Jube.App
{
    using System;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Server.Kestrel.Core;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Middlewares;
    using TaskCancellation;
    using TaskCancellation.Interfaces;

    public static class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStopping.Register(OnShutdown);

            Console.CancelKeyPress += (_, e) =>
            {
                OnShutdown();
                e.Cancel = true;
            };

            host.Run();
            return;

            void OnShutdown()
            {
#pragma warning disable VSTHRD002
                host.StopAsync().GetAwaiter().GetResult();

                RequestTrackingMiddleware.WaitForRequestsToDrainAsync().GetAwaiter().GetResult();

                var cancellationTokenProvider = host.Services.GetService<ICancellationTokenProvider>();
                cancellationTokenProvider?.Cancel();

                var taskCoordinator = host.Services.GetService<TaskCoordinator>();
                taskCoordinator?.StillRunningAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
            }
        }

        public static long? ParseConnectionLimit(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !long.TryParse(value, out var limit))
            {
                return 10000;
            }

            return limit > 0 ? limit : null;
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.ConfigureKestrel((context, options) =>
                    {
                        options.AddServerHeader = false;
                        options.Limits.MinRequestBodyDataRate = new MinDataRate(240, TimeSpan.FromSeconds(10));
                        options.Limits.MinResponseDataRate = null;
                        options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
                        options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
                        var maxConcurrentConnections =
                            ParseConnectionLimit(context.Configuration["MaxConcurrentConnections"]);
                        options.Limits.MaxConcurrentConnections = maxConcurrentConnections;
                        options.Limits.MaxConcurrentUpgradedConnections = maxConcurrentConnections;
                        options.Limits.Http2.MaxStreamsPerConnection = 100;
                        options.Limits.Http2.InitialConnectionWindowSize = 131072;
                        options.Limits.Http2.InitialStreamWindowSize = 98304;
                    });
                })
                .ConfigureLogging((_, logging) => { logging.ClearProviders(); });
        }
    }
}