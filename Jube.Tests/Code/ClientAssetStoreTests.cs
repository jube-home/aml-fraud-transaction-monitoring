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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.App.Code;
using Jube.Cache;
using log4net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Jube.Test.Code;

[Trait("Category", "Unit")]
public sealed class ClientAssetStoreTests : IDisposable
{
    private readonly string webRoot = Path.Combine(Path.GetTempPath(), "jube-client-assets-" + Guid.NewGuid().ToString("N"));
    private readonly FakeRespDatabase shared = new();

    public ClientAssetStoreTests()
    {
        Directory.CreateDirectory(Path.Combine(webRoot, "assets"));
        Directory.CreateDirectory(Path.Combine(webRoot, "images"));
    }

    public void Dispose()
    {
        Directory.Delete(webRoot, true);
    }

    private void Write(string name, string content)
    {
        File.WriteAllText(Path.Combine(webRoot, name), content);
    }

    private ClientAssetStore Replica()
    {
        var cacheService = (CacheService)RuntimeHelpers.GetUninitializedObject(typeof(CacheService));

        cacheService.ResilientRespResilientRespDatabase = FakeRespDatabase.Create(shared);

        return new ClientAssetStore(cacheService, new WebHost(webRoot),
            LogManager.GetLogger(typeof(ClientAssetStoreTests)));
    }

    private static string Text(byte[]? bytes)
    {
        return Encoding.UTF8.GetString(bytes!);
    }

    private void Release(string buildId, string bundle, string css)
    {
        Write("build-id.txt", buildId);
        Write("assets/index-AAAAAAAA.js", bundle);
        Write("images/jooby.png", css);
        Write("index.html", "<html/>");
    }

    [Fact]
    public async Task EveryStaticFileOfABuildIsServedFromTheStoreByThatBuildsKey()
    {
        Release("100", "bundle-one", "image-one");

        await Replica().PublishAsync();

        var other = Replica();

        Text(await other.TryGetAsync("100", "assets/index-AAAAAAAA.js")).Should().Be("bundle-one");
        Text(await other.TryGetAsync("100", "images/jooby.png")).Should().Be("image-one");
    }

    [Fact]
    public async Task TheIndexPageAndTheBuildIdFileAreNotPublished()
    {
        Release("100", "bundle-one", "image-one");

        await Replica().PublishAsync();

        (await Replica().TryGetAsync("100", "index.html")).Should().BeNull();
        (await Replica().TryGetAsync("100", "build-id.txt")).Should().BeNull();
    }

    [Fact]
    public async Task AnUnhashedFileThatChangedBetweenBuildsIsServedAsEachBuildHadIt()
    {
        Release("100", "bundle-one", "image-one");
        await Replica().PublishAsync();

        Release("200", "bundle-two", "image-two");
        await Replica().PublishAsync();

        var other = Replica();

        Text(await other.TryGetAsync("100", "images/jooby.png")).Should().Be("image-one");
        Text(await other.TryGetAsync("200", "images/jooby.png")).Should().Be("image-two");
    }

    [Fact]
    public async Task AnUnchangedFileIsStoredOnceAcrossBuilds()
    {
        Release("100", "bundle-one", "same");
        await Replica().PublishAsync();

        var blobsAfterFirst = shared.Strings.Keys.Count(key => key.StartsWith("ClientBlob:"));

        Release("200", "bundle-two", "same");
        await Replica().PublishAsync();

        shared.Strings.Keys.Count(key => key.StartsWith("ClientBlob:")).Should().Be(blobsAfterFirst + 1);
    }

    [Fact]
    public async Task TheTargetIsOnlyAdvertisedOncePublishingFinishesAndTheLastBuildToStartWins()
    {
        (await Replica().GetTargetAsync("fallback")).Should().Be("fallback");

        Release("200", "bundle-two", "image-two");
        await Replica().PublishAsync();

        Release("100", "bundle-one", "image-one");
        await Replica().PublishAsync();

        (await Replica().GetTargetAsync("fallback")).Should().Be("100");
    }

    [Theory]
    [InlineData("100", "assets/../appsettings.json")]
    [InlineData("100", "assets/a b.js")]
    [InlineData("1x0", "assets/index-AAAAAAAA.js")]
    [InlineData("", "assets/index-AAAAAAAA.js")]
    [InlineData("999", "assets/index-AAAAAAAA.js")]
    public async Task ABadOrUnknownRequestIsNeverServed(string build, string path)
    {
        Release("100", "bundle-one", "image-one");
        await Replica().PublishAsync();

        (await Replica().TryGetAsync(build, path)).Should().BeNull();
    }

    [Fact]
    public async Task AMissingBuildIdPublishesNothingAndDoesNotThrow()
    {
        Write("assets/index-AAAAAAAA.js", "x");

        await Replica().PublishAsync();

        shared.Strings.Should().BeEmpty();
    }

    [Theory]
    [InlineData("assets/index-CqDspPSD.js", true)]
    [InlineData("assets/ibm-plex-sans-latin-600-normal-CuJfVYMP.woff2", true)]
    [InlineData("images/jooby.png", false)]
    [InlineData("favicon.ico", false)]
    public void OnlyContentHashedNamesAreTreatedAsImmutable(string path, bool hashed)
    {
        ClientAssetStore.IsHashedName(path).Should().Be(hashed);
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
