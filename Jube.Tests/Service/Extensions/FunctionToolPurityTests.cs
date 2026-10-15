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
using System.Reflection;
using FluentAssertions;
using Jube.Service.Agent.Models;
using Jube.Test.Dictionary.Extensions;
using Xunit;

namespace Jube.Test.Service.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class FunctionToolPurityTests
    {
        private static readonly string[] forbiddenNamespacePrefixes =
        [
            "System.IO.",
            "System.Net.Http",
            "System.Net.Sockets",
            "System.Net.NetworkInformation",
            "System.Net.Mail",
            "System.Diagnostics.Process",
            "System.Runtime.InteropServices",
            "System.Reflection.Emit",
            "System.Runtime.Loader",
            "Microsoft.Win32",
            "Npgsql",
            "StackExchange.Redis"
        ];

        private static readonly string[] allowedIoTypes =
        [
            "System.IO.MemoryStream",
            "System.IO.StringReader",
            "System.IO.StringWriter",
            "System.IO.TextReader",
            "System.IO.TextWriter",
            "System.IO.Stream",
            "System.Runtime.InteropServices.CollectionsMarshal",
            "System.Runtime.InteropServices.MemoryMarshal"
        ];

        private static readonly string[] forbiddenMembers =
        [
            "System.Environment.",
            "System.Activator.",
            "System.AppDomain.",
            "System.Type.GetType",
            "System.Reflection.Assembly.Load",
            "System.Reflection.Assembly.LoadFrom",
            "System.Reflection.Assembly.LoadFile",
            "System.Net.Dns.",
            "System.Net.WebClient.",
            "System.Net.WebRequest.",
            "System.Threading.Thread..ctor",
            "System.GC."
        ];

        private static readonly string[] nonDeterministicMembers =
        [
            "System.DateTime.get_Now",
            "System.DateTime.get_UtcNow",
            "System.DateTime.get_Today",
            "System.DateTimeOffset.get_Now",
            "System.DateTimeOffset.get_UtcNow",
            "System.Guid.NewGuid",
            "System.Random."
        ];

        public static IEnumerable<object[]> ScannedTypes()
        {
            return PureTypes().Select(t => new object[] { t.FullName! });
        }

        [Theory]
        [MemberData(nameof(ScannedTypes))]
        public void PureCodeReachesNoFileNetworkProcessOrNativeInterop(string typeName)
        {
            var violations = Violations(Resolve(typeName), forbiddenMembers).ToList();

            violations.Should().BeEmpty("agent tool functions must stay free of IO, OS access and platform invoke");
        }

        [Fact]
        public void ExtensionFacadesNeverReadTheClockOrARandomSource()
        {
            var violations = PureTypes()
                .Where(t => t.Namespace!.StartsWith("Jube.Service.Extensions", StringComparison.Ordinal))
                .SelectMany(t => Violations(t, [.. nonDeterministicMembers, .. forbiddenMembers]))
                .ToList();

            violations.Should()
                .BeEmpty(
                    "extension facades must return the same answer for the same input; the clock lives in Helpers");
        }

        [Fact]
        public void TheScanCoversEveryToolGroup()
        {
            var namespaces = PureTypes().Select(t => t.Namespace).Distinct().ToList();

            namespaces.Should()
                .Contain(["Jube.Service.Extensions.Geospatial", "Jube.Service.Helpers.ClockAndIdentifiers"]);

            PureTypes().Count().Should().BeGreaterThan(40);
        }

        [Fact]
        public void TheScannerWouldCatchAFileWrite()
        {
            Violations(typeof(DeliberatelyImpure), forbiddenMembers).Should().NotBeEmpty();
        }

        private static IEnumerable<Type> PureTypes()
        {
            var service = typeof(FunctionToolAttribute).Assembly.GetTypes()
                .Where(t => t.Namespace is not null &&
                            FunctionToolDecorationTests.RootNamespaces.Any(root =>
                                t.Namespace == root || t.Namespace.StartsWith(root + ".", StringComparison.Ordinal)));

            return service;
        }

        private static Type Resolve(string typeName)
        {
            return PureTypes().Single(t => t.FullName == typeName);
        }

        private static IEnumerable<string> Violations(Type type, string[] forbiddenMembers)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                   BindingFlags.Instance |
                                                   BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (IlReferenceScanner.IsPlatformInvoke(method))
                {
                    yield return $"{type.FullName}.{method.Name} is a platform invoke.";
                }
            }

            foreach (var (caller, reference) in IlReferenceScanner.References(type))
            {
                var owner = reference as Type ?? reference.DeclaringType;
                var ownerName = owner?.FullName ?? "";
                var memberName = ownerName + "." + reference.Name;

                if (forbiddenNamespacePrefixes.Any(prefix => ownerName.StartsWith(prefix, StringComparison.Ordinal)) &&
                    !allowedIoTypes.Contains(ownerName))
                {
                    yield return $"{type.FullName}.{caller.Name} references {memberName}.";
                }

                if (forbiddenMembers.Any(entry => entry.EndsWith('.')
                        ? memberName.StartsWith(entry, StringComparison.Ordinal)
                        : memberName == entry))
                {
                    yield return $"{type.FullName}.{caller.Name} references {memberName}.";
                }
            }
        }

        private static class DeliberatelyImpure
        {
            public static void Write()
            {
                File.WriteAllText("never-called.txt", "x");
            }
        }
    }
}