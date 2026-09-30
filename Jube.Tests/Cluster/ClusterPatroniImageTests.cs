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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Cluster
{
    public class ClusterPatroniImageTests
    {
        private const string Loader = "/usr/local/lib/patroni-secrets.sh";

        private const string Shim = "/usr/local/bin/patronictl";

        private const string RealBinary = "/opt/patroni/bin/patronictl";

        private static readonly Regex generatedFile =
            new(@"^RUN cat > (?<path>\S+) << 'EOF'$", RegexOptions.Compiled);

        private static readonly Regex environmentVariable =
            new(@"^ENV (?<name>[A-Za-z_][A-Za-z0-9_]*)=""?(?<value>[^""]*)""?$", RegexOptions.Compiled);

        private static readonly IReadOnlyList<string> patroniSecrets =
        [
            "PATRONI_SUPERUSER_PASSWORD",
            "PATRONI_REPLICATION_PASSWORD",
            "PATRONI_ADMIN_PASSWORD",
            "PATRONI_ETCD3_PASSWORD"
        ];

        private static string[] Dockerfile()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !Directory.Exists(Path.Join(directory.FullName, "Jube.Cluster")))
            {
                directory = directory.Parent;
            }

            return File.ReadAllLines(Path.Join(directory.Required().FullName, "Jube.Cluster", "patroni",
                "Dockerfile"));
        }

        private static Dictionary<string, string> GeneratedFiles()
        {
            var files = new Dictionary<string, string>(StringComparer.Ordinal);

            string? path = null;
            var body = new List<string>();

            foreach (var line in Dockerfile())
            {
                if (path == null)
                {
                    var match = generatedFile.Match(line);
                    if (match.Success)
                    {
                        path = match.Groups["path"].Value;
                        body.Clear();
                    }

                    continue;
                }

                if (line == "EOF")
                {
                    files[path] = string.Join(Environment.NewLine, body);
                    path = null;
                    continue;
                }

                body.Add(line);
            }

            path.Should().BeNull("every heredoc opened in the Dockerfile has to be terminated");

            return files;
        }

        private static Dictionary<string, string> EnvironmentVariables()
        {
            return Dockerfile()
                .Select(line => environmentVariable.Match(line))
                .Where(match => match.Success)
                .ToDictionary(match => match.Groups["name"].Value, match => match.Groups["value"].Value,
                    StringComparer.Ordinal);
        }

        [Fact]
        public void PatroniControlResolvesToTheShimAheadOfTheRealBinary()
        {
            GeneratedFiles().Should().ContainKey(Shim);

            var path = EnvironmentVariables().GetValueOrDefault("PATH").Required().Split(':');

            var shim = Array.IndexOf(path, Shim[..Shim.LastIndexOf('/')]);
            var real = Array.IndexOf(path, RealBinary[..RealBinary.LastIndexOf('/')]);

            shim.Should().BeGreaterThanOrEqualTo(0, "the shim's directory has to be on PATH to be reachable at all");
            real.Should().BeGreaterThanOrEqualTo(0);
            shim.Should().BeLessThan(real,
                "a docker exec invocation of patronictl has to resolve to the shim rather than to the real binary, " +
                "which cannot see the etcd credential the entrypoint exports into the Patroni process alone");
        }

        [Fact]
        public void TheShimLoadsTheSecretsAndHandsOffToTheRealBinaryWithoutRecursing()
        {
            var shim = GeneratedFiles().GetValueOrDefault(Shim).Required();

            shim.Should().Contain($". {Loader}");
            shim.Should().Contain("load_patroni_secrets");
            shim.Should().Contain($"exec {RealBinary} \"$@\"");
            shim.Should().NotMatchRegex(@"(?m)^\s*exec patronictl\b",
                "handing off by name would resolve back to the shim itself");
        }

        [Fact]
        public void TheEntrypointAndTheShimReadTheirSecretsFromOneDefinition()
        {
            var files = GeneratedFiles();

            var loader = files.GetValueOrDefault(Loader).Required();
            var entrypoint = files.GetValueOrDefault("/entrypoint.sh").Required();
            var shim = files.GetValueOrDefault(Shim).Required();

            loader.Should().Contain("load_secret() {");
            loader.Should().Contain("load_patroni_secrets() {");

            foreach (var secret in patroniSecrets)
            {
                loader.Should().Contain($"load_secret \"{secret}\"",
                    $"{secret} is mounted on the Patroni services and has to reach both entry paths");
            }

            files.Values.Count(body => body.Contains("load_secret() {")).Should().Be(1,
                "a second copy of the function is how the shim came to be missing the etcd credential");

            foreach (var entry in new[] { entrypoint, shim })
            {
                entry.Should().Contain($". {Loader}");
                entry.Should().MatchRegex(@"(?m)^load_patroni_secrets$");
            }
        }

        [Fact]
        public void PatroniControlDefaultsToTheConfigurationFileTheContainerIsGiven()
        {
            var command = Dockerfile().Single(line => line.StartsWith("CMD ", StringComparison.Ordinal));

            var configurationFile = command["CMD ".Length..].Trim('[', ']', '"');

            EnvironmentVariables().Should().ContainKey("PATRONICTL_CONFIG_FILE",
                "patronictl reads PATRONICTL_CONFIG_FILE rather than the PATRONI_CONFIG_FILE the daemon is given, " +
                "so without it a bare patronictl falls back to a default path no container has");

            EnvironmentVariables()["PATRONICTL_CONFIG_FILE"].Should().Be(configurationFile);
        }
    }
}