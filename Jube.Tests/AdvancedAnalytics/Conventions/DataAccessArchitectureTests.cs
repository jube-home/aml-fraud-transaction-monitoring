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
using System.Reflection.Emit;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Service.Agent.AdvancedAnalyticsTools;
using Jube.Service.Query.AdvancedAnalyticsChart;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Conventions
{
    public class DataAccessArchitectureTests
    {
        private static readonly string[] guardedNamespacePrefixes =
        [
            "Jube.Engine.AdvancedAnalytics",
            "Jube.Service.Repository.Analytics",
            "Jube.Service.Query.Analytics"
        ];

        private static readonly string[] guardedTypeNames =
        [
            "Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions.SyncAskJoobyAnalyticsInstancesExtensions"
        ];

        // ReSharper disable once CollectionNeverUpdated.Local
        private static readonly HashSet<string> allowedDirectAccess = [];

        private static readonly Dictionary<short, OpCode> opCodes = typeof(OpCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => (OpCode)f.GetValue(null)!)
            .ToDictionary(o => o.Value);

        private static bool IsDataAccessType(Type? type)
        {
            while (type is { HasElementType: true })
            {
                type = type.GetElementType();
            }

            if (type == null)
            {
                return false;
            }

            if (type.IsGenericType && type.GetGenericArguments().Any(IsDataAccessType))
            {
                return true;
            }

            return (type.Namespace != null && type.Namespace.StartsWith("LinqToDB", StringComparison.Ordinal))
                   || type.FullName == "Jube.Data.Context.DbContext";
        }

        private static string OutermostName(Type type)
        {
            while (type.DeclaringType != null)
            {
                type = type.DeclaringType;
            }

            return type.FullName!;
        }

        private static IEnumerable<Type> GuardedTypes()
        {
            var assemblies = new[]
            {
                typeof(AdvancedAnalyticsToolCatalogue).Assembly,
                typeof(AdvancedAnalyticsChartSource).Assembly
            };

            foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
            {
                var outer = OutermostName(type);
                var ns = outer.Substring(0, Math.Max(0, outer.LastIndexOf('.')));

                if (guardedNamespacePrefixes.Any(p => ns.StartsWith(p, StringComparison.Ordinal))
                    || guardedTypeNames.Contains(outer)
                    || ns == "Jube.Service.Charts")
                {
                    yield return type;
                }
            }
        }

        private static IEnumerable<MemberInfo> ReferencedMembers(MethodBase method)
        {
            byte[]? il;

            try
            {
                il = method.GetMethodBody()?.GetILAsByteArray();
            }
            catch
            {
                yield break;
            }

            if (il == null)
            {
                yield break;
            }

            var typeArguments = method.DeclaringType is { IsGenericType: true }
                ? method.DeclaringType.GetGenericArguments()
                : null;

            var methodArguments = method.IsGenericMethod ? method.GetGenericArguments() : null;
            var i = 0;

            while (i < il.Length)
            {
                var value = (short)il[i++];

                if (value == 0xFE)
                {
                    value = (short)(0xFE00 | il[i++]);
                }

                var op = opCodes[value];

                switch (op.OperandType)
                {
                    case OperandType.InlineMethod:
                    case OperandType.InlineField:
                    case OperandType.InlineTok:
                    case OperandType.InlineType:
                        var token = BitConverter.ToInt32(il, i);
                        i += 4;
                        MemberInfo? member = null;

                        try
                        {
                            member = method.Module.ResolveMember(token, typeArguments, methodArguments);
                        }
                        catch
                        {
                            //ignored
                        }

                        if (member != null)
                        {
                            yield return member;
                        }

                        break;
                    case OperandType.InlineNone:
                        break;
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar:
                        i += 1;
                        break;
                    case OperandType.InlineVar:
                        i += 2;
                        break;
                    case OperandType.InlineI8:
                    case OperandType.InlineR:
                        i += 8;
                        break;
                    case OperandType.InlineSwitch:
                        var count = BitConverter.ToInt32(il, i);
                        i += 4 + 4 * count;
                        break;
                    default:
                        i += 4;
                        break;
                }
            }
        }

        private static bool TouchesDataAccess(MemberInfo member)
        {
            if (member is Type)
            {
                return false;
            }

            if (member.DeclaringType != null && typeof(Delegate).IsAssignableFrom(member.DeclaringType))
            {
                return false;
            }

            if (member.Name == "Dispose" || member.Name == "DisposeAsync")
            {
                return false;
            }

            if (member.DeclaringType != null && IsDataAccessType(member.DeclaringType))
            {
                return true;
            }

            if (member is MethodInfo { IsGenericMethod: true } m && m.GetGenericArguments().Any(IsDataAccessType))
            {
                return true;
            }

            return false;
        }

        [Fact]
        public void TheGuardedNamespacesAreNotEmpty()
        {
            GuardedTypes().Select(OutermostName).Distinct().Count().Should().BeGreaterThan(30);
        }

        private static List<string> ViolationsOf(IEnumerable<Type> types)
        {
            const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                     BindingFlags.Static | BindingFlags.DeclaredOnly;

            var violations = new List<string>();

            foreach (var type in types)
            {
                var owner = OutermostName(type);

                if (allowedDirectAccess.Contains(owner))
                {
                    continue;
                }

                foreach (var method in type.GetMethods(all).Cast<MethodBase>().Concat(type.GetConstructors(all)))
                foreach (var member in ReferencedMembers(method).Where(TouchesDataAccess))
                {
                    violations.Add($"{owner}.{method.Name} uses {member.DeclaringType?.FullName}.{member.Name}");
                }
            }

            return [.. violations.Distinct()];
        }

        [Fact]
        public void TheScannerCatchesDirectTableAccess()
        {
            ViolationsOf([typeof(DirectTableAccessFixture)]).Should().NotBeEmpty();
        }

        [Fact]
        public void NoServiceOrEngineClassTouchesLinqToDbOrTheDbContextDirectly()
        {
            ViolationsOf(GuardedTypes()).Should().BeEmpty(
                "table access belongs in a Jube.Data Repository or Query class");
        }

        private sealed class DirectTableAccessFixture
        {
            // ReSharper disable once UnusedMember.Local
            public static object Read(DbContext dbContext)
            {
                return dbContext.GetTable<CaseNote>();
            }
        }
    }
}