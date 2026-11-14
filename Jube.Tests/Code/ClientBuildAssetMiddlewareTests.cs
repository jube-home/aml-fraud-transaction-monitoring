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
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.App.Code;
using Jube.App.Middlewares;
using Jube.Cache;
using log4net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Jube.Test.Code;

[Trait("Category", "Unit")]
public sealed class ClientBuildAssetMiddlewareTests : IDisposable
{
    private readonly string oldRoot = Path.Combine(Path.GetTempPath(), "jube-mw-old-" + Guid.NewGuid().ToString("N"));
    private readonly string newRoot = Path.Combine(Path.GetTempPath(), "jube-mw-new-" + Guid.NewGuid().ToString("N"));
    private readonly FakeRespDatabase shared = new();

    public ClientBuildAssetMiddlewareTests()
    {
        foreach (var root in new[] { oldRoot, newRoot })
        {
            Directory.CreateDirectory(Path.Combine(root, "images"));
        }

        File.WriteAllText(Path.Combine(oldRoot, "build-id.txt"), "100");
        File.WriteAllText(Path.Combine(oldRoot, "images/jooby.png"), "old-image");
        File.WriteAllText(Path.Combine(newRoot, "build-id.txt"), "200");
        File.WriteAllText(Path.Combine(newRoot, "images/jooby.png"), "new-image");

        StoreFor(oldRoot).PublishAsync().GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        Directory.Delete(oldRoot, true);
        Directory.Delete(newRoot, true);
    }

    private ClientAssetStore StoreFor(string root)
    {
        var cacheService = (CacheService)RuntimeHelpers.GetUninitializedObject(typeof(CacheService));

        cacheService.ResilientRespResilientRespDatabase = FakeRespDatabase.Create(shared);

        return new ClientAssetStore(cacheService, new WebHost(root),
            LogManager.GetLogger(typeof(ClientBuildAssetMiddlewareTests)));
    }

    private async Task<(DefaultHttpContext Context, bool NextCalled, string Body)> RunAsync(string path, string? cookie,
        string method = "GET")
    {
        var context = new DefaultHttpContext();

        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        if (cookie != null)
        {
            context.Request.Headers.Cookie = $"jube-build={cookie}";
        }

        var nextCalled = false;
        var middleware = new ClientBuildAssetMiddleware(_ =>
        {
            nextCalled = true;

            return Task.CompletedTask;
        }, StoreFor(newRoot), new WebHost(newRoot));

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;

        return (context, nextCalled, await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync());
    }

    [Fact]
    public async Task ATabOnAnOlderBuildGetsThatBuildsFileFromTheStore()
    {
        var result = await RunAsync("/images/jooby.png", "100");

        result.NextCalled.Should().BeFalse();
        result.Body.Should().Be("old-image");
        result.Context.Response.ContentType.Should().Be("image/png");
        result.Context.Response.Headers.CacheControl.ToString().Should().Be("no-cache");
    }

    [Fact]
    public async Task ATabOnThisReplicasOwnBuildIsLeftToTheNormalStaticFiles()
    {
        (await RunAsync("/images/jooby.png", "200")).NextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ARequestWithNoBuildCookieIsLeftToTheNormalStaticFiles()
    {
        (await RunAsync("/images/jooby.png", null)).NextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task AFileTheOlderBuildNeverHadFallsThroughToThisReplica()
    {
        (await RunAsync("/images/new-only.png", "100")).NextCalled.Should().BeTrue();
    }

    [Theory]
    [InlineData("/api/Models")]
    [InlineData("/images/jooby.png", "POST")]
    public async Task ApiCallsAndWritesAreNeverServedFromTheStore(string path, string method = "GET")
    {
        (await RunAsync(path, "100", method)).NextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ABadCookieIsIgnored()
    {
        (await RunAsync("/images/jooby.png", "1x0")).NextCalled.Should().BeTrue();
    }

    private sealed class WebHost(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "test";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = root;
        public string EnvironmentName { get; set; } = "Test";
        public string WebRootPath { get; set; } = root;
        public IFileProvider WebRootFileProvider { get; set; } = new PhysicalFileProvider(root);
    }
}
