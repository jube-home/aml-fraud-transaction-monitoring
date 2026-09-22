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
using FluentAssertions;
using Jube.Dictionary;
using Jube.Dictionary.Extensions;
using Jube.Migrations.Branches.AbstractionCalculationCoderOnly;
using Jube.Parser;
using Jube.Parser.Compiler;
using Jube.Test.Infrastructure;
using Xunit;
using Calculation = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelAbstractionCalculation;
using Migration = Jube.Migrations.Branches.AbstractionCalculationCoderOnly.ConvertArithmeticAbstractionCalculationsToCoder;

namespace Jube.Test.Migrations
{
    [Trait("Category", "Unit")]
    [Collection("EvalExpressionRegistry")]
    public sealed class AbstractionCalculationCoderOnlyMigrationTests
    {
        [Theory]
        [InlineData(1, "Matched = Abstraction.Left.Plus(Abstraction.Right).ZeroIfUndefined()")]
        [InlineData(2, "Matched = Abstraction.Left.Minus(Abstraction.Right).ZeroIfUndefined()")]
        [InlineData(3, "Matched = Abstraction.Left.RatioOf(Abstraction.Right).ZeroIfUndefined()")]
        [InlineData(4, "Matched = Abstraction.Left.Times(Abstraction.Right).ZeroIfUndefined()")]
        public void EachArithmeticTypeBecomesItsFluentMethod(int typeId, string expected)
        {
            AbstractionCalculationScriptBuilder.TryBuild(typeId, "Left", "Right", out var script).Should().BeTrue();

            script.Should().Be(expected);
        }

        [Fact]
        public void SpacesInAnAbstractionNameBecomeUnderscoresAsTheEngineDoes()
        {
            AbstractionCalculationScriptBuilder.TryBuild(3, "Refunds Sum", "Total Sales Sum", out var script)
                .Should().BeTrue();

            script.Should().Be("Matched = Abstraction.Refunds_Sum.RatioOf(Abstraction.Total_Sales_Sum).ZeroIfUndefined()");
        }

        [Fact]
        public void SurroundingWhiteSpaceIsTrimmed()
        {
            AbstractionCalculationScriptBuilder.TryBuild(1, "  Left ", "Right  ", out var script).Should().BeTrue();

            script.Should().Contain("Abstraction.Left.Plus(Abstraction.Right)");
        }

        [Theory]
        [InlineData(null, 5)]
        [InlineData(0, 5)]
        [InlineData(5, 5)]
        [InlineData(6, 5)]
        [InlineData(-1, 5)]
        public void OnlyTheFourArithmeticTypesAreConverted(int? typeId, int unused)
        {
            AbstractionCalculationScriptBuilder.TryBuild(typeId, "Left", "Right", out var script).Should().BeFalse();

            script.Should().BeEmpty();
            unused.Should().Be(5);
        }

        [Theory]
        [InlineData(null, "Right")]
        [InlineData("Left", null)]
        [InlineData("", "Right")]
        [InlineData("Left", "  ")]
        [InlineData("Left-Name", "Right")]
        [InlineData("Left", "Right.Name")]
        [InlineData("Le\"ft", "Right")]
        [InlineData("Left", "Right)")]
        [InlineData("Left;", "Right")]
        [InlineData("Left", "Right\nMatched = 1")]
        [InlineData("Left", "Right' comment")]
        [InlineData("Lëft", "Right")]
        public void ANameThatCannotBeATokenIsNotConvertedSoNothingCanBeInjected(string? left, string? right)
        {
            AbstractionCalculationScriptBuilder.TryBuild(1, left, right, out var script).Should().BeFalse();

            script.Should().BeEmpty();
        }

        [Fact]
        public void APlanConvertsWhatItCanAndKeepsAPlaceholderForWhatItCannot()
        {
            var plan = Migration.Plan(
            [
                new Migration.Row(1, 3, "Refunds Sum", "Total Sales Sum"),
                new Migration.Row(2, 1, "Bad-Name", "Right"),
                new Migration.Row(3, 4, null, "Right"),
                new Migration.Row(4, 2, "A", "B")
            ]);

            plan.Should().HaveCount(4);
            plan[0].Converted.Should().BeTrue();
            plan[0].Script.Should().Contain("RatioOf");
            plan[1].Converted.Should().BeFalse();
            plan[1].Script.Should().Be("' Converted from Add: Bad-Name + Right. Rewrite this as a function, then activate it.");
            plan[2].Converted.Should().BeFalse();
            plan[2].Script.Should().Contain("(none) * Right");
            plan[3].Converted.Should().BeTrue();
            plan[3].Script.Should().Contain("Minus");
        }

        [Theory]
        [InlineData(1, "Add", "+")]
        [InlineData(2, "Subtract", "-")]
        [InlineData(3, "Divide", "/")]
        [InlineData(4, "Multiply", "*")]
        public void ThePlaceholderRecordsWhatTheCalculationWasSoDroppingTheColumnsLosesNothing(int typeId, string label,
            string symbol)
        {
            AbstractionCalculationScriptBuilder.BuildPlaceholder(typeId, "Refunds-Sum", "Total.Sales")
                .Should().Be($"' Converted from {label}: Refunds-Sum {symbol} Total.Sales. Rewrite this as a function, then activate it.");
        }

        [Fact]
        public void ThePlaceholderIsAlwaysASingleCommentLineSoANameCannotInjectCode()
        {
            var script = AbstractionCalculationScriptBuilder.BuildPlaceholder(1, "Left\r\nMatched = 1",
                "Right\nMatched = 2");

            script.Should().NotContain("\n");
            script.Should().NotContain("\r");
            script.Should().StartWith("'");
        }

        [Fact]
        public void ThePlaceholderCapsVeryLongNames()
        {
            var script = AbstractionCalculationScriptBuilder.BuildPlaceholder(1, new string('a', 500), "b");

            script.Length.Should().BeLessThan(300);
        }

        [Fact]
        public void APlaceholderIsNotAValidFunctionSoItCannotBeActivatedByAccident()
        {
            var parser = new Parser.Parser(TestLog.NoOp, ["Matched"]);
            var placeholder = AbstractionCalculationScriptBuilder.BuildPlaceholder(3, "Bad-Name", "Right");

            var rule = parser.TranslateFromDotNotation(new ParsedRule
            {
                ErrorSpans = [],
                OriginalRuleText = placeholder,
                ParsedRuleText = placeholder
            });

            parser.Parse(rule).ErrorSpans.Should().NotBeEmpty();
        }

        [Fact]
        public void APlanIgnoresRowsThatAreAlreadyCoderOrHaveNoType()
        {
            var plan = Migration.Plan(
            [
                new Migration.Row(1, 5, "Left", "Right"),
                new Migration.Row(2, null, "Left", "Right"),
                new Migration.Row(3, 0, "Left", "Right")
            ]);

            plan.Should().BeEmpty();
        }

        [Fact]
        public void APlanOfNothingIsEmpty()
        {
            Migration.Plan([]).Should().BeEmpty();
        }

        [Fact]
        public void TheMigrationCarriesAVersionAndHasNoDownStep()
        {
            var attribute = (FluentMigrator.MigrationAttribute)Attribute.GetCustomAttribute(typeof(Migration),
                typeof(FluentMigrator.MigrationAttribute))!;

            attribute.Version.Should().Be(20260921130000);
            typeof(Migration).GetMethod("Down")!.DeclaringType.Should().Be(typeof(Migration));
        }

        private static Calculation.Match CompileScript(string scriptText, params string[] abstractionNames)
        {
            var parser = new Parser.Parser(TestLog.NoOp, ["Matched"])
            {
                EntityAnalysisModelsAbstractionRule = [.. abstractionNames]
            };
            var rule = parser.TranslateFromDotNotation(new ParsedRule
            {
                ErrorSpans = [],
                OriginalRuleText = scriptText,
                ParsedRuleText = scriptText
            });
            rule.ErrorSpans.Should().BeEmpty();
            rule = parser.Parse(rule);
            rule.ErrorSpans.Should().BeEmpty();
            rule = parser.WrapAbstractionCalculation(rule, true);

            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => a.Location)
                .ToArray();
            var compile = new Compile();
            compile.CompileCode(rule.ParsedRuleText, TestLog.NoOp, references, Compile.Language.Vb);
            compile.Success.Should().BeTrue(compile.ErrorsSummary);

            var method = compile.CompiledAssembly.GetType("CalculationRule")!.GetMethod("Match")!;
            return (Calculation.Match)Delegate.CreateDelegate(typeof(Calculation.Match), method);
        }

        private static double Run(Calculation.Match match, params (string, double)[] abstractions)
        {
            var dictionary = new PooledDictionary<string, double>();
            foreach (var (name, value) in abstractions)
            {
                dictionary.TryAdd(name, value);
            }

            return match(new DictionaryNoBoxing<string>(), new PooledDictionary<string, double>(), dictionary,
                new Dictionary<string, List<string>>(), new PooledDictionary<string, double>(),
                new PooledDictionary<string, double>(), new PooledDictionary<string, double>(), TestLog.NoOp);
        }

        private static Calculation.Match Migrated(int typeId, string left, string right)
        {
            AbstractionCalculationScriptBuilder.TryBuild(typeId, left, right, out var script).Should().BeTrue();

            return CompileScript(script, left.Trim().Replace(" ", "_"), right.Trim().Replace(" ", "_"));
        }

        [Theory]
        [InlineData(1, 6)]
        [InlineData(2, -2)]
        [InlineData(3, 0.5)]
        [InlineData(4, 8)]
        public void AMigratedCalculationGivesTheSameResultAsTheOldArithmetic(int typeId, double expected)
        {
            Run(Migrated(typeId, "Left", "Right"), ("Left", 2), ("Right", 4)).Should().Be(expected);
        }

        [Fact]
        public void ADivisionByZeroStillStoresZeroNotNaNOrInfinity()
        {
            Run(Migrated(3, "Left", "Right"), ("Left", 5), ("Right", 0)).Should().Be(0);
            Run(Migrated(3, "Left", "Right"), ("Left", 0), ("Right", 0)).Should().Be(0);
        }

        [Fact]
        public void AnOverflowStillStoresZero()
        {
            Run(Migrated(4, "Left", "Right"), ("Left", double.MaxValue), ("Right", double.MaxValue)).Should().Be(0);
        }

        [Fact]
        public void AMissingAbstractionStillReadsAsZeroRatherThanAnException()
        {
            Run(Migrated(1, "Left", "Right"), ("Left", 5)).Should().Be(5);
            Run(Migrated(1, "Left", "Right")).Should().Be(0);
            Run(Migrated(3, "Left", "Right"), ("Right", 4)).Should().Be(0);
            Run(Migrated(3, "Left", "Right"), ("Left", 4)).Should().Be(0);
        }

        [Fact]
        public void NamesWithSpacesResolveAgainstTheUnderscoredAbstractionKeys()
        {
            var match = Migrated(3, "Refunds Sum", "Total Sales Sum");

            Run(match, ("Refunds_Sum", 10), ("Total_Sales_Sum", 40)).Should().Be(0.25);
        }

        [Fact]
        public void AMigratedScriptPassesTheSameParseTheEngineAppliesAtSync()
        {
            var parser = new Parser.Parser(TestLog.NoOp, ["Matched"])
            {
                EntityAnalysisModelsAbstractionRule = ["Left", "Right"]
            };
            AbstractionCalculationScriptBuilder.TryBuild(3, "Left", "Right", out var script);

            var rule = parser.TranslateFromDotNotation(new ParsedRule
            {
                ErrorSpans = [],
                OriginalRuleText = script,
                ParsedRuleText = script
            });

            parser.Parse(rule).ErrorSpans.Should().BeEmpty();
        }

        [Fact]
        public void AMigratedScriptNamingAnAbstractionThatDoesNotExistIsRefusedAtTranslation()
        {
            var parser = new Parser.Parser(TestLog.NoOp, ["Matched"]) { EntityAnalysisModelsAbstractionRule = ["Other"] };
            AbstractionCalculationScriptBuilder.TryBuild(1, "Left", "Right", out var script);

            var rule = parser.TranslateFromDotNotation(new ParsedRule
            {
                ErrorSpans = [],
                OriginalRuleText = script,
                ParsedRuleText = script
            });

            rule.ErrorSpans.Should().NotBeEmpty();
        }

        [Theory]
        [InlineData(1d, 1d)]
        [InlineData(0d, 0d)]
        [InlineData(-2.5, -2.5)]
        [InlineData(double.NaN, 0d)]
        [InlineData(double.PositiveInfinity, 0d)]
        [InlineData(double.NegativeInfinity, 0d)]
        public void ZeroIfUndefinedKeepsFiniteNumbersAndZeroesTheRest(double input, double expected)
        {
            input.ZeroIfUndefined().Should().Be(expected);
        }

        [Fact]
        public void ZeroIfUndefinedCompletesAFluentCalculationSoTheOldStoredZeroIsAChoice()
        {
            5d.RatioOf(0).ZeroIfUndefined().Should().Be(0);
            double.IsNaN(5d.RatioOf(0)).Should().BeTrue();
        }

        [Fact]
        public void TheDtoNoLongerExposesTheArithmeticSurface()
        {
            var names = typeof(Jube.Dto.EntityAnalysisModelAbstractionCalculation.EntityAnalysisModelAbstractionCalculationDto)
                .GetProperties().Select(p => p.Name).ToList();

            names.Should().NotContain("AbstractionCalculationTypeId");
            names.Should().NotContain("EntityAnalysisModelAbstractionNameLeft");
            names.Should().NotContain("EntityAnalysisModelAbstractionNameRight");
            names.Should().Contain("FunctionScript");
        }

        [Fact]
        public void ThePocoNoLongerMapsTheRemovedColumnsSoLinq2DbNeverSelectsThem()
        {
            foreach (var type in new[]
                     {
                         typeof(Jube.Data.Poco.EntityAnalysisModelAbstractionCalculation),
                         typeof(Jube.Data.Poco.EntityAnalysisModelAbstractionCalculationVersion)
                     })
            {
                type.GetProperty("AbstractionCalculationTypeId").Should().BeNull();
                type.GetProperty("EntityAnalysisModelAbstractionNameLeft").Should().BeNull();
                type.GetProperty("EntityAnalysisModelAbstractionNameRight").Should().BeNull();
            }
        }

        private static Dictionary<string, int> Keys(Type type)
        {
            return type.GetProperties()
                .Select(p => (p.Name, Attribute: (MessagePack.KeyAttribute?)Attribute.GetCustomAttribute(p, typeof(MessagePack.KeyAttribute))))
                .Where(x => x.Attribute?.IntKey != null)
                .ToDictionary(x => x.Name, x => x.Attribute!.IntKey!.Value);
        }

        [Fact]
        public void MessagePackKeysOfTheRemainingPropertiesAreUnchangedSoTheRemovedSlotsAreReserved()
        {
            var calculation = Keys(typeof(Jube.Data.Poco.EntityAnalysisModelAbstractionCalculation));

            calculation["Id"].Should().Be(0);
            calculation["EntityAnalysisModelId"].Should().Be(1);
            calculation["Name"].Should().Be(2);
            calculation["Locked"].Should().Be(5);
            calculation["Active"].Should().Be(6);
            calculation["ResponsePayload"].Should().Be(14);
            calculation["ReportTable"].Should().Be(16);
            calculation["FunctionScript"].Should().Be(17);
            calculation["Guid"].Should().Be(19);
            calculation["CompileError"].Should().Be(24);
            calculation.Values.Should().NotContain([3, 4, 15]);
            calculation.Values.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public void MessagePackKeysOfTheVersionPocoAreUnchangedSoTheRemovedSlotsAreReserved()
        {
            var version = Keys(typeof(Jube.Data.Poco.EntityAnalysisModelAbstractionCalculationVersion));

            version["Id"].Should().Be(0);
            version["Name"].Should().Be(3);
            version["Locked"].Should().Be(6);
            version["Active"].Should().Be(7);
            version["ResponsePayload"].Should().Be(15);
            version["ReportTable"].Should().Be(17);
            version["FunctionScript"].Should().Be(18);
            version["Guid"].Should().Be(19);
            version.Values.Should().NotContain([4, 5, 16]);
            version.Values.Should().OnlyHaveUniqueItems();
        }
    }
}
