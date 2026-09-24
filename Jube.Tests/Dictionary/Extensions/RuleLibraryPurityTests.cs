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
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;
using Jube.Test.Dictionary.Extensions.Models;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class RuleLibraryPurityTests
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

        public static IEnumerable<object[]> ScannedTypes()
        {
            return PureTypes().Select(t => new object[] { t.FullName! });
        }

        [Theory]
        [MemberData(nameof(ScannedTypes))]
        public void PureCodeReachesNoFileNetworkProcessOrNativeInterop(string typeName)
        {
            var violations = Violations(Resolve(typeName), forbiddenMembers).ToList();

            violations.Should().BeEmpty("rule functions must stay free of IO, OS access and platform invoke");
        }

        [Fact]
        public void TheScanCoversTheRuleLibrary()
        {
            var namespaces = PureTypes().Select(t => t.Namespace).Distinct().ToList();

            namespaces.Should().Contain(["Jube.Dictionary.Extensions", "Jube.Dictionary.Fuzzy"]);
            PureTypes().Count().Should().BeGreaterThan(40);
        }

        [Fact]
        public void TheScannerWouldCatchAFileWrite()
        {
            Violations(typeof(DeliberatelyImpure), forbiddenMembers).Should().NotBeEmpty();
        }

        private static IEnumerable<Type> PureTypes()
        {
            var rules = typeof(Jube.Dictionary.Extensions.Extensions).Assembly.GetTypes()
                .Where(t => t.Namespace is "Jube.Dictionary.Extensions" or "Jube.Dictionary.Fuzzy");
            return rules;
        }

        private static Type Resolve(string typeName)
        {
            return PureTypes().Single(t => t.FullName == typeName);
        }

        private static IEnumerable<string> Violations(Type type, string[] forbiddenMemberList)
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

                if (forbiddenMemberList.Any(entry => entry.EndsWith('.')
                        ? memberName.StartsWith(entry, StringComparison.Ordinal)
                        : memberName == entry))
                {
                    yield return $"{type.FullName}.{caller.Name} references {memberName}.";
                }
            }
        }
    }
}