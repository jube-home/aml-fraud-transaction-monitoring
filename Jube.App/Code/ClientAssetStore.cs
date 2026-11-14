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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jube.Cache;
using log4net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using StackExchange.Redis;

namespace Jube.App.Code
{
    public sealed class ClientAssetStore(CacheService cacheService, IWebHostEnvironment webHostEnvironment, ILog log)
    {
        public const string BuildCookieName = "jube-build";

        private const string BlobPrefix = "ClientBlob:";
        private const string ManifestPrefix = "ClientBuild:";
        private const string TargetKey = "ClientBuild:Target";
        private const int LocalLimit = 512;

        private static readonly Regex SafePath = new("^[A-Za-z0-9._/-]+$", RegexOptions.Compiled);
        private static readonly Regex BuildIdShape = new("^[0-9]{1,20}$", RegexOptions.Compiled);
        private static readonly Regex Hashed = new(@"-[A-Za-z0-9_-]{8}\.[A-Za-z0-9]+$", RegexOptions.Compiled);

        private static readonly string[] Unpublished = ["index.html", ClientBuildId.FileName];

        private readonly ConcurrentDictionary<string, byte[]> local = new();

        public static bool IsBuildId(string value)
        {
            return value != null && BuildIdShape.IsMatch(value);
        }

        public static bool IsHashedName(string path)
        {
            return Hashed.IsMatch(path);
        }

        public async Task PublishAsync()
        {
            var webRoot = webHostEnvironment.WebRootFileProvider;
            var buildId = ClientBuildId.Read(webRoot);

            if (!IsBuildId(buildId))
            {
                return;
            }

            var database = cacheService.ResilientRespResilientRespDatabase;
            var manifest = new List<HashEntry>();
            var stored = 0;

            foreach (var (path, file) in Enumerate(webRoot, string.Empty))
            {
                if (Unpublished.Contains(path, StringComparer.OrdinalIgnoreCase) || !SafePath.IsMatch(path))
                {
                    continue;
                }

                await using var stream = file.CreateReadStream();
                using var memory = new MemoryStream();

                await stream.CopyToAsync(memory).ConfigureAwait(false);

                var bytes = memory.ToArray();
                var hash = Convert.ToHexString(SHA256.HashData(bytes));

                if (!await database.KeyExistsAsync(BlobPrefix + hash).ConfigureAwait(false))
                {
                    await database.StringSetAsync(BlobPrefix + hash, bytes).ConfigureAwait(false);
                    stored++;
                }

                manifest.Add(new HashEntry(path, hash));
            }

            database.HashSet(ManifestPrefix + buildId, manifest.ToArray());

            await database.StringSetAsync(TargetKey, buildId).ConfigureAwait(false);

            if (log.IsInfoEnabled)
            {
                log.Info($"Client assets: build {buildId} published {manifest.Count} file(s), {stored} new blob(s), and is now the target.");
            }
        }

        public async Task<string> GetTargetAsync(string fallback)
        {
            try
            {
                var value = await cacheService.ResilientRespResilientRespDatabase.StringGetAsync(TargetKey)
                    .ConfigureAwait(false);

                return value.HasValue && IsBuildId(value.ToString()) ? value.ToString() : fallback;
            }
            catch (Exception ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"Client assets: could not read the target build as {ex.Message}.");
                }

                return fallback;
            }
        }

        public async Task<byte[]> TryGetAsync(string buildId, string path)
        {
            if (!IsBuildId(buildId) || !SafePath.IsMatch(path))
            {
                return null;
            }

            var localKey = buildId + ":" + path;

            if (local.TryGetValue(localKey, out var cached))
            {
                return cached;
            }

            try
            {
                var database = cacheService.ResilientRespResilientRespDatabase;

                var hash = await database.HashGetAsync(ManifestPrefix + buildId, path).ConfigureAwait(false);

                if (!hash.HasValue)
                {
                    return null;
                }

                var value = await database.StringGetAsync(BlobPrefix + hash).ConfigureAwait(false);

                if (!value.HasValue)
                {
                    return null;
                }

                var bytes = (byte[])value;

                if (local.Count < LocalLimit)
                {
                    local[localKey] = bytes;
                }

                return bytes;
            }
            catch (Exception ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"Client assets: could not read {path} of build {buildId} as {ex.Message}.");
                }

                return null;
            }
        }

        private static IEnumerable<(string Path, IFileInfo File)> Enumerate(IFileProvider provider, string directory)
        {
            foreach (var entry in provider.GetDirectoryContents(directory))
            {
                var path = directory.Length == 0 ? entry.Name : directory + "/" + entry.Name;

                if (entry.IsDirectory)
                {
                    foreach (var child in Enumerate(provider, path))
                    {
                        yield return child;
                    }
                }
                else
                {
                    yield return (path, entry);
                }
            }
        }
    }
}
