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
    public class ClusterComposeSecretTests
    {
        private static readonly Regex tokenReference = new(@"\[@(?<name>[A-Za-z0-9_]+)@\]", RegexOptions.Compiled);

        private static readonly IReadOnlyList<string> etcdClientSettings =
            ["EtcdClientUsername", "EtcdClientPassword"];

        private static string ComposePath()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Jube.Cluster")))
            {
                directory = directory.Parent;
            }

            return Path.Combine(directory.Required().FullName, "Jube.Cluster", "docker-compose.yml");
        }

        private static ComposeFile Compose()
        {
            return ComposeFile.Parse(File.ReadAllLines(ComposePath()));
        }

        [Fact]
        public void EveryComposeServiceRunningTheEngineAuthenticatesToEtcd()
        {
            var compose = Compose();

            var engineServices = compose.Services
                .Where(service => string.Equals(service.Value.Environment.GetValueOrDefault("EnableEngine"), "True",
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            engineServices.Should().NotBeEmpty();

            foreach (var (name, service) in engineServices)
            {
                foreach (var setting in etcdClientSettings)
                {
                    service.Environment.Should().ContainKey(setting,
                        $"{name} runs the engine, whose Infrastructure Health Metrics samplers poll etcd, and " +
                        "etcd client authentication is mandatory in this deployment");
                    service.Environment[setting].Should().NotBeNullOrWhiteSpace();
                }

                service.Environment["EtcdClientPassword"].Should().MatchRegex(@"^\[@[A-Za-z0-9_]+@\]$");
            }
        }

        [Fact]
        public void EveryComposeServiceMountsTheSecretsItsEnvironmentTokenises()
        {
            var compose = Compose();

            foreach (var (name, service) in compose.Services)
            {
                foreach (var (setting, value) in service.Environment)
                {
                    foreach (var reference in tokenReference.Matches(value).Select(match => match.Groups["name"].Value))
                    {
                        service.Secrets.Should().Contain(reference,
                            $"{name} tokenises {reference} from {setting} and can only resolve it from a " +
                            "mounted secret file");

                        compose.DeclaredSecrets.Should().Contain(reference,
                            $"{name} mounts {reference}, so it has to be declared in the top level secrets block");
                    }
                }
            }
        }

        [Fact]
        public void EveryComposeServiceSecretIsDeclaredOnce()
        {
            var compose = Compose();

            foreach (var (name, service) in compose.Services)
            {
                service.Secrets.Should().OnlyHaveUniqueItems($"{name} should not mount the same secret twice");

                foreach (var secret in service.Secrets)
                {
                    compose.DeclaredSecrets.Should().Contain(secret,
                        $"{name} mounts {secret}, so it has to be declared in the top level secrets block");
                }
            }
        }

        private sealed class ComposeService
        {
            public Dictionary<string, string> Environment { get; } = new(StringComparer.Ordinal);

            public List<string> Secrets { get; } = [];
        }

        private sealed class ComposeFile
        {
            private ComposeFile(Dictionary<string, ComposeService> services, List<string> declaredSecrets)
            {
                Services = services;
                DeclaredSecrets = declaredSecrets;
            }

            public Dictionary<string, ComposeService> Services { get; }

            public List<string> DeclaredSecrets { get; }

            public static ComposeFile Parse(IReadOnlyList<string> lines)
            {
                var services = new Dictionary<string, ComposeService>(StringComparer.Ordinal);
                var declaredSecrets = new List<string>();

                string? topLevel = null;
                ComposeService? service = null;
                string? block = null;

                foreach (var line in lines)
                {
                    if (line.Length == 0 || line.TrimStart().StartsWith('#'))
                    {
                        continue;
                    }

                    var indent = line.Length - line.TrimStart().Length;
                    var trimmed = line.Trim();

                    if (indent == 0)
                    {
                        topLevel = trimmed.TrimEnd(':');
                        service = null;
                        block = null;
                        continue;
                    }

                    if (indent == 2 && trimmed.EndsWith(':'))
                    {
                        var name = trimmed.TrimEnd(':');
                        block = null;

                        if (topLevel == "services")
                        {
                            service = new ComposeService();
                            services[name] = service;
                        }
                        else
                        {
                            service = null;

                            if (topLevel == "secrets")
                            {
                                declaredSecrets.Add(name);
                            }
                        }

                        continue;
                    }

                    if (service == null)
                    {
                        continue;
                    }

                    if (indent == 4)
                    {
                        block = trimmed.EndsWith(':') ? trimmed.TrimEnd(':') : null;
                        continue;
                    }

                    if (indent != 6 || !trimmed.StartsWith("- "))
                    {
                        continue;
                    }

                    var item = trimmed[2..].Trim();

                    switch (block)
                    {
                        case "secrets":
                            service.Secrets.Add(item);
                            break;
                        case "environment":
                            var separator = item.IndexOf('=');
                            if (separator > 0)
                            {
                                service.Environment[item[..separator]] = item[(separator + 1)..];
                            }

                            break;
                    }
                }

                return new ComposeFile(services, declaredSecrets);
            }
        }
    }
}