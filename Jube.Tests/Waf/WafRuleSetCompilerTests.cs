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

using FluentAssertions;
using Jube.App.Code.Waf;
using Jube.Data.Poco;
using Xunit;

namespace Jube.Test.Waf
{
    [Trait("Category", "Unit")]
    public sealed class WafRuleSetCompilerTests
    {
        private static WafSignature Signature(string? pattern, int? scope = 7, int? timeout = 50)
        {
            return new WafSignature
            {
                Id = 1, Name = "S", Category = "C", Pattern = pattern, TargetScope = scope,
                MatchTimeoutMilliseconds = timeout, Drop = 1, Active = 1
            };
        }

        [Fact]
        public void AValidSignatureCompiles()
        {
            var ruleSet = WafRuleSetCompiler.Compile([Signature(@"(?i)<\s*script\b")], [], 100);

            ruleSet.Signatures.Should().ContainSingle();
            ruleSet.Signatures[0].Pattern.IsMatch("<script>").Should().BeTrue();
        }

        [Fact]
        public void ASignatureWithAnInvalidPatternIsSkipped()
        {
            var ruleSet = WafRuleSetCompiler.Compile([Signature("(unclosed")], [], 100);

            ruleSet.Signatures.Should().BeEmpty();
        }

        [Fact]
        public void ASignatureWithNoPatternIsSkipped()
        {
            var ruleSet = WafRuleSetCompiler.Compile([Signature(null)], [], 100);

            ruleSet.Signatures.Should().BeEmpty();
        }

        [Theory]
        [InlineData(null, WafTargetScope.All)]
        [InlineData(0, WafTargetScope.All)]
        [InlineData(1, WafTargetScope.Path)]
        [InlineData(4, WafTargetScope.Body)]
        [InlineData(7, WafTargetScope.All)]
        public void TargetScopeIsMappedFromTheStoredInteger(int? stored, WafTargetScope expected)
        {
            var ruleSet = WafRuleSetCompiler.Compile([Signature(".", stored)], [], 100);

            ruleSet.Signatures[0].Scope.Should().Be(expected);
        }

        [Fact]
        public void ARequestedTimeoutAboveTheMaximumIsCappedNotHonoured()
        {
            var ruleSet = WafRuleSetCompiler.Compile([Signature(".", 7, 100000)], [], 100);

            ruleSet.Signatures[0].Pattern.MatchTimeout.Should()
                .BeLessThanOrEqualTo(System.TimeSpan.FromMilliseconds(100));
        }

        [Fact]
        public void AnExceptionWithoutARouteRegexIsSkipped()
        {
            var ruleSet = WafRuleSetCompiler.Compile([],
            [
                new WafException { Id = 1, Name = "blank", RouteRegex = null, Active = 1 }
            ], 100);

            ruleSet.Exceptions.Should().BeEmpty();
        }

        [Fact]
        public void AnExceptionWithAnInvalidRouteRegexIsSkipped()
        {
            var ruleSet = WafRuleSetCompiler.Compile([],
            [
                new WafException { Id = 1, Name = "bad", RouteRegex = "(unclosed", Active = 1 }
            ], 100);

            ruleSet.Exceptions.Should().BeEmpty();
        }

        [Fact]
        public void AnExceptionWithAnInvalidFieldRegexIsSkipped()
        {
            var ruleSet = WafRuleSetCompiler.Compile([],
            [
                new WafException
                {
                    Id = 1, Name = "bad-field", RouteRegex = "^/api", FieldRegex = "(unclosed", Active = 1
                }
            ], 100);

            ruleSet.Exceptions.Should().BeEmpty();
        }
    }
}