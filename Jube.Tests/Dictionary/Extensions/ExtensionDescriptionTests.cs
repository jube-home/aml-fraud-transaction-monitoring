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

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Xunit;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class ExtensionDescriptionTests
    {
        private static readonly Dictionary<string, string> prefixPhrases = new()
        {
            ["Match"] = "Decides matched",
            ["Reject"] = "Decides rejected",
            ["Break"] = "Decides broken",
            ["Require"] = "otherwise decides rejected",
            ["Ensure"] = "otherwise decides broken"
        };

        private static List<MethodInfo> Methods()
        {
            return
            [
                .. typeof(Jube.Dictionary.Extensions.Extensions)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.IsDefined(typeof(ExtensionAttribute), false) && !m.IsSpecialName &&
                                !m.Name.Contains('<') && !m.Name.Contains('$'))
            ];
        }

        private static string? DescriptionOf(ICustomAttributeProvider provider)
        {
            return provider.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .OfType<DescriptionAttribute>().FirstOrDefault()?.Description;
        }

        private static string Label(MethodInfo method)
        {
            return
                $"{method.GetParameters()[0].ParameterType.Name}.{method.Name}({method.GetParameters().Length - 1})";
        }

        [Fact]
        public void ThereAreThousandsOfExtensionMethods()
        {
            Methods().Should().HaveCountGreaterThan(4000);
        }

        [Fact]
        public void EveryExtensionMethodSaysWhatItDoesInASentence()
        {
            var undescribed = Methods()
                .Where(m => string.IsNullOrWhiteSpace(DescriptionOf(m)))
                .Select(Label).ToList();

            undescribed.Should().BeEmpty("every extension method is offered to rule authors with its description");

            var malformed = Methods()
                .Where(m => DescriptionOf(m) is { } d && (d.Length < 16 || !d.EndsWith('.') || d.Contains('`') ||
                                                          d.Contains("<br>") || d.Trim() != d))
                .Select(m => $"{Label(m)}: {DescriptionOf(m)}").ToList();

            malformed.Should().BeEmpty();
        }

        [Fact]
        public void EveryParameterSaysWhatItIsForAndTheReceiverIsNeverDescribed()
        {
            var parameters = Methods().SelectMany(m => m.GetParameters().Skip(1)).ToList();

            parameters.Should().HaveCountGreaterThan(5000);

            parameters.Where(p => DescriptionOf(p) is not { } d || d.Length < 4 || !d.EndsWith('.') || d.Trim() != d)
                .Select(p => $"{p.Member.Name}.{p.Name}: {DescriptionOf(p)}").Should().BeEmpty();

            Methods().Where(m => DescriptionOf(m.GetParameters()[0]) != null).Select(Label).Should().BeEmpty();
        }

        [Fact]
        public void PipelineStepsSayWhatTheirPrefixDecides()
        {
            var steps = Methods()
                .Where(m => m.ReturnType.IsGenericType && m.ReturnType.Name.StartsWith("Flow`"))
                .Where(m => prefixPhrases.Keys.Any(k => m.Name.StartsWith(k) && m.Name.Length > k.Length))
                .Where(m => m.Name is not ("MatchWhen" or "RejectWhen" or "BreakWhen" or "RequireThat" or
                    "EnsureThat" or "MatchIfScoreAtLeast" or "BreakIfScoreAtLeast" or "RejectIfScoreBelow" or
                    "RequireScoreAtLeast"))
                .ToList();

            steps.Should().HaveCountGreaterThan(3000);

            steps.Where(m => DescriptionOf(m)?.Contains(prefixPhrases[prefixPhrases.Keys.First(m.Name.StartsWith)]) != true)
                .Select(m => $"{Label(m)}: {DescriptionOf(m)}").Should().BeEmpty();

            steps.Where(m => DescriptionOf(m)?.Contains("True when: ") != true)
                .Select(m => $"{Label(m)}: {DescriptionOf(m)}").Should().BeEmpty();
        }

        [Fact]
        public void AStepOnAPlainValueSaysThatItBeginsAPipeline()
        {
            var steps = Methods()
                .Where(m => m.ReturnType.IsGenericType && m.ReturnType.Name.StartsWith("Flow`") &&
                            !m.GetParameters()[0].ParameterType.IsGenericType)
                .ToList();

            steps.Should().HaveCountGreaterThan(1500);

            steps.Where(m => DescriptionOf(m)?.StartsWith("Begins a pipeline from the value") != true)
                .Select(m => $"{Label(m)}: {DescriptionOf(m)}").Should().BeEmpty();
        }

        [Fact]
        public void AKnownMethodCarriesTheDocumentedText()
        {
            var isMatch = Methods().Single(m => m.Name == "IsMatch" && m.GetParameters().Length == 2);

            DescriptionOf(isMatch).Should()
                .Be("Checks if the string matches a regular expression; matching is limited to 250 ms.");

            DescriptionOf(isMatch.GetParameters()[1]).Should().Be("The regular expression pattern to match.");
        }
    }
}
