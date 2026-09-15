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
using FluentAssertions;
using Jube.Dictionary;
using Jube.Dictionary.Extensions;
using Xunit;

namespace Jube.Test.Dictionary
{
    [Trait("Category", "Unit")]
    [Collection("EvalExpressionRegistry")]
    public sealed class EvalExpressionRegistryTests : IDisposable
    {
        public EvalExpressionRegistryTests()
        {
            EvalExpressionRegistry.Clear();
            Environment.SetEnvironmentVariable("EnableDynamicEval", null);
        }

        public void Dispose()
        {
            EvalExpressionRegistry.Clear();
            Environment.SetEnvironmentVariable("EnableDynamicEval", null);
        }

        [Fact]
        public void WhenEnabledEvalReturnsTheCompiledResultForARegisteredBooleanExpression()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");
            EvalExpressionRegistry.Register("IsCorporateEmail", "!value.EndsWith(\"gmail.com\")", 5);

            "richard@jube.io".Eval<bool>("IsCorporateEmail").Should().BeTrue();
            "richard@gmail.com".Eval<bool>("IsCorporateEmail").Should().BeFalse();
        }

        [Fact]
        public void WhenEnabledEvalReturnsTheCompiledResultForARegisteredStringExpression()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");
            EvalExpressionRegistry.Register("Shout", "value.ToUpper()", 1);

            "hello".Eval<string>("Shout").Should().Be("HELLO");
        }

        [Fact]
        public void WhenEnabledEvalReturnsTheCompiledResultForARegisteredDoubleExpression()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");
            EvalExpressionRegistry.Register("HalfLength", "value.Length / 2.0", 3);

            "12345678".Eval<double>("HalfLength").Should().Be(4.0);
        }

        [Fact]
        public void WhenEnabledEvalReturnsTheCompiledResultForARegisteredIntegerExpression()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");
            EvalExpressionRegistry.Register("Length", "value.Length", 2);

            "12345678".Eval<int>("Length").Should().Be(8);
        }

        [Fact]
        public void WhenEnabledEvalReturnsTheCompiledResultForARegisteredDateTimeExpression()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");
            EvalExpressionRegistry.Register("ParseAsDate", "DateTime.Parse(value)", 4);

            "2024-01-01".Eval<DateTime>("ParseAsDate").Should().Be(new DateTime(2024, 1, 1));
        }

        [Fact]
        public void EvalThrowsForAnUnregisteredNameWhenEnabled()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");

            var act = () => "richard@jube.io".Eval<bool>("DoesNotExist");

            act.Should().Throw<KeyNotFoundException>();
        }

        [Fact]
        public void EvalThrowsWhenDisabledEvenForARegisteredName()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");
            EvalExpressionRegistry.Register("IsCorporateEmail", "!value.EndsWith(\"gmail.com\")", 5);

            Environment.SetEnvironmentVariable("EnableDynamicEval", "False");

            var act = () => "richard@jube.io".Eval<bool>("IsCorporateEmail");

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void EvalThrowsWhenEnableDynamicEvalIsUnset()
        {
            EvalExpressionRegistry.Register("Shout", "value.ToUpper()", 1);

            var act = () => "hello".Eval<string>("Shout");

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void RegisterThrowsForAnUnsupportedResultTypeId()
        {
            var act = () => EvalExpressionRegistry.Register("Bad", "value", 99);

            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void RegisterThrowsForANullOrEmptyName(string? name)
        {
            var act = () => EvalExpressionRegistry.Register(name!, "value", 1);

            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void RegisterThrowsForANullOrEmptyExpression(string? expression)
        {
            var act = () => EvalExpressionRegistry.Register("Bad", expression!, 1);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void RegisterThrowsForInvalidExpressionSyntax()
        {
            var act = () => EvalExpressionRegistry.Register("Bad", "value.", 1);

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void EntriesReflectsEveryRegisteredNameAndItsResultTypeId()
        {
            EvalExpressionRegistry.Register("IsCorporateEmail", "!value.EndsWith(\"gmail.com\")", 5);
            EvalExpressionRegistry.Register("Shout", "value.ToUpper()", 1);

            EvalExpressionRegistry.Entries.Should().Contain(new KeyValuePair<string, int>("IsCorporateEmail", 5));
            EvalExpressionRegistry.Entries.Should().Contain(new KeyValuePair<string, int>("Shout", 1));
        }

        [Fact]
        public void EntriesIsEmptyAfterClear()
        {
            EvalExpressionRegistry.Register("Shout", "value.ToUpper()", 1);

            EvalExpressionRegistry.Clear();

            EvalExpressionRegistry.Entries.Should().BeEmpty();
        }

        [Fact]
        public void RegisteringTheSameNameTwiceOverwritesThePreviousExpression()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");
            EvalExpressionRegistry.Register("Transform", "value.ToUpper()", 1);
            "hello".Eval<string>("Transform").Should().Be("HELLO");

            EvalExpressionRegistry.Register("Transform", "value.ToLower()", 1);

            "HELLO".Eval<string>("Transform").Should().Be("hello");
        }

        [Fact]
        public void MultipleNamesCanBeRegisteredAndInvokedIndependently()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");
            EvalExpressionRegistry.Register("Shout", "value.ToUpper()", 1);
            EvalExpressionRegistry.Register("Whisper", "value.ToLower()", 1);

            "Hello".Eval<string>("Shout").Should().Be("HELLO");
            "Hello".Eval<string>("Whisper").Should().Be("hello");
        }

        [Fact]
        public void EvalThrowsWhenTheRequestedResultTypeDoesNotMatchTheRegisteredResultTypeId()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");
            EvalExpressionRegistry.Register("IsCorporateEmail", "!value.EndsWith(\"gmail.com\")", 5);

            var act = () => "richard@jube.io".Eval<string>("IsCorporateEmail");

            act.Should().Throw<KeyNotFoundException>();
        }

        [Theory]
        [InlineData("True")]
        [InlineData("true")]
        [InlineData("TRUE")]
        public void EnabledIsTrueForAnyCasingOfTrue(string value)
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", value);

            EvalExpressionRegistry.Enabled.Should().BeTrue();
        }

        [Theory]
        [InlineData("False")]
        [InlineData("false")]
        [InlineData("0")]
        [InlineData("")]
        [InlineData(null)]
        public void EnabledIsFalseForAnythingOtherThanTrue(string? value)
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", value);

            EvalExpressionRegistry.Enabled.Should().BeFalse();
        }

        [Fact]
        public void NullInputFlowsThroughToTheCompiledExpression()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");
            EvalExpressionRegistry.Register("IsNull", "value == null", 5);

            ((string?)null).Eval<bool>("IsNull").Should().BeTrue();
            "not null".Eval<bool>("IsNull").Should().BeFalse();
        }

        [Fact]
        public void MathAndConvertAreReachableAsCommonTypes()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");
            EvalExpressionRegistry.Register("AbsLengthMinusTen", "Math.Abs(value.Length - 10)", 2);
            EvalExpressionRegistry.Register("AsInteger", "Convert.ToInt32(value)", 2);

            "123".Eval<int>("AbsLengthMinusTen").Should().Be(7);
            "42".Eval<int>("AsInteger").Should().Be(42);
        }

        [Fact]
        public void PlainGetTypeMetadataIsAllowedButGrantsNoFurtherReflectionAccess()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");
            EvalExpressionRegistry.Register("TypeName", "value.GetType().Name", 1);

            "hello".Eval<string>("TypeName").Should().Be("String");
        }

        [Fact]
        public void ReflectionEscapeViaTypeAssemblyIsBlockedBySandbox()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");

            Action act = () => EvalExpressionRegistry.Register("Reflect", "value.GetType().Assembly", 1);

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void ReflectionEscapeViaGetMethodsIsBlockedBySandbox()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");

            Action act = () =>
                EvalExpressionRegistry.Register("Reflect", "value.GetType().GetMethods().Length", 2);

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void FilesystemAccessIsUnreachableFromTheSandbox()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");

            Action act = () =>
            {
                EvalExpressionRegistry.Register("ReadFile", "System.IO.File.Exists(value)", 5);
                "x".Eval<bool>("ReadFile");
            };

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void ProcessAccessIsUnreachableFromTheSandbox()
        {
            Environment.SetEnvironmentVariable("EnableDynamicEval", "True");

            Action act = () =>
            {
                EvalExpressionRegistry.Register("StartProcess", "System.Diagnostics.Process.Start(value) != null",
                    5);
                "x".Eval<bool>("StartProcess");
            };

            act.Should().Throw<Exception>();
        }
    }
}