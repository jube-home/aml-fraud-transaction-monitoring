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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jube.App.Endpoints;
using log4net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;

namespace Jube.Test.Infrastructure.ModelScaffolding
{
    public sealed class ModelInvokeHost : IAsyncDisposable
    {
        private readonly WebApplication app;
        private readonly HttpClient client;

        private ModelInvokeHost(WebApplication app)
        {
            this.app = app;
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Required()
                .Addresses.Single();
            client = new HttpClient(new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false })
                { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(120) };
        }

        public static async Task<ModelInvokeHost> StartAsync(Jube.DynamicEnvironment.DynamicEnvironment environment,
            ILog log, global::Jube.Engine.Engine? engine)
        {
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Services.AddSingleton(log);
            builder.Services.AddSingleton(environment);
            if (engine != null)
            {
                builder.Services.AddSingleton(engine);
            }

            builder.Services.AddAuthentication(InvokeTestUserHandler.TestScheme)
                .AddScheme<AuthenticationSchemeOptions, InvokeTestUserHandler>(InvokeTestUserHandler.TestScheme,
                    _ => { });
            builder.Services.AddAuthorization();
            builder.Services.AddLocalization();
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddMvcCore().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                };
            });

            var app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapInvokeEndpoints();
            await app.StartAsync().ConfigureAwait(false);
            return new ModelInvokeHost(app);
        }

        public static Task<ModelInvokeHost> StartAsync(ModelEngineHost engine)
        {
            return StartAsync(engine.Environment, engine.Log, engine.Engine);
        }

        public Task<InvokeResponse> InvokeAsync(ModelScaffold scaffold, PayloadBuilder? payload, bool async = false,
            string? user = null, CancellationToken cancellationToken = default)
        {
            return InvokeAsync(scaffold, payload?.ToBytes(), async, user, cancellationToken);
        }

        public Task<InvokeResponse> InvokeAsync(ModelScaffold scaffold, byte[]? body, bool async = false,
            string? user = null, CancellationToken cancellationToken = default)
        {
            var path = $"/api/Invoke/EntityAnalysisModel/{scaffold.ModelGuid}" + (async ? "/Async" : string.Empty);
            return SendAsync("POST", path, user ?? scaffold.UserName, body, "application/json",
                cancellationToken: cancellationToken);
        }

        public async Task<InvokeResponse> SendAsync(string method, string path, string? user = null,
            byte[]? body = null, string? contentType = null, bool sendContentLength = true,
            CancellationToken cancellationToken = default, params (string Name, string Value)[] headers)
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), path);
            if (user != null)
            {
                request.Headers.Add(InvokeTestUserHandler.Header, user);
            }

            if (body != null)
            {
                request.Content = new ByteArrayContent(body);
                if (!sendContentLength)
                {
                    request.Content = new StreamContent(new ChunkedStream(body));
                    request.Headers.TransferEncodingChunked = true;
                }

                if (contentType != null)
                {
                    request.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
                }
            }

            foreach (var (name, value) in headers)
            {
                request.Headers.TryAddWithoutValidation(name, value);
            }

            using var response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            var responseHeaders = response.Headers.Concat(response.Content.Headers)
                .ToDictionary(h => h.Key.ToLowerInvariant(), h => string.Join(",", h.Value));
            return new InvokeResponse((int)response.StatusCode, bytes,
                response.Content.Headers.ContentType?.ToString(), response.Content.Headers.ContentLength,
                responseHeaders);
        }

        public async ValueTask DisposeAsync()
        {
            client.Dispose();
            await app.StopAsync().ConfigureAwait(false);
            await app.DisposeAsync().ConfigureAwait(false);
        }
    }
}