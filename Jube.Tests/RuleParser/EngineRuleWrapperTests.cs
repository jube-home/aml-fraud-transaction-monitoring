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
using Jube.Parser;
using Xunit;

namespace Jube.Test.RuleParser
{
    [Trait("Category", "Unit")]
    public sealed class EngineRuleWrapperTests
    {
        private const string Body = "Matched = Data(\"Amount\").AsDouble() > 1";

        private const string Imports =
            "Imports System.IO\r\nImports log4net\r\nImports System.Net\r\nImports System.Collections.Generic\r\n" +
            "Imports Jube.Dictionary\r\nImports Jube.Dictionary.Extensions\r\nImports System\r\n";

        private const string TryCatchTail =
            "Catch ex As Exception\r\nLog.Info(ex.ToString)\r\nEnd Try\r\nReturn Matched\r\n\r\nEnd Function\r\nEnd Class\r\n";

        [Fact]
        public void TheGatewayWrapperIsTheEnginesTextExactly()
        {
            EngineRuleWrapper.GatewayRule(Body).Text.Should().Be(Imports + "Public Class GatewayRule\r\n" +
                                                                 "Public Shared Function Match(Data As DictionaryNoBoxing(Of String),List As Dictionary(Of String, List(Of String)),KVP As PooledDictionary(Of String, Double),Log as ILog) As Boolean\r\n" +
                                                                 "Dim Matched as Boolean\r\nTry\r\n" + Body + "\r\n" +
                                                                 TryCatchTail);
        }

        [Fact]
        public void TheAbstractionWrapperIsTheEnginesTextExactly()
        {
            EngineRuleWrapper.AbstractionRule(Body).Text.Should().Be(Imports + "Public Class AbstractionRule\r\n" +
                                                                     "Public Shared Function Match(Data As DictionaryNoBoxing(Of String),List as Dictionary(Of String,List(Of String)),KVP as PooledDictionary(of String,Double),Log as ILog) As Boolean\r\n" +
                                                                     "Dim Matched as Boolean\r\nTry\r\n" + Body +
                                                                     "\r\n" + TryCatchTail);
        }

        [Fact]
        public void TheActivationWrapperIsTheEnginesTextExactly()
        {
            EngineRuleWrapper.ActivationRule(Body).Text.Should().Be(Imports +
                                                                    "Imports Jube.HttpAdaptationProtocol\r\nPublic Class ActivationRule\r\n" +
                                                                    "Public Shared Function Match(Data As DictionaryNoBoxing(Of String),TTLCounter As PooledDictionary(Of String, Double),Abstraction As PooledDictionary(Of string,double),HttpAdaptation As PooledDictionary(Of String, Adaptation),ExhaustiveAdaptation As PooledDictionary(Of String, Double),List as Dictionary(Of String,List(Of String)),Calculation As PooledDictionary(Of String, Double),Sanctions As PooledDictionary(Of String, Double),KVP As PooledDictionary(Of String, Double),Activation as ICollection(Of String),Log as ILog) As Boolean\r\n" +
                                                                    "Dim Matched as Boolean\r\nTry\r\n" + Body +
                                                                    "\r\n" + TryCatchTail);
        }

        [Fact]
        public void TheInlineFunctionWrapperIsTheEnginesTextExactly()
        {
            EngineRuleWrapper.InlineFunction(Body).Text.Should().Be(Imports + "Public Class InlineFunction\r\n" +
                                                                    "Public Shared Function Match(Data As DictionaryNoBoxing(Of String),List As Dictionary(Of String, List(Of String)),KVP As PooledDictionary(Of String, Double),Log as ILog) As Object\r\n" +
                                                                    "Dim Matched as Object = Nothing\r\nTry\r\n" +
                                                                    Body + "\r\n" + TryCatchTail);
        }

        [Fact]
        public void TheReprocessingWrapperIsTheEnginesTextExactly()
        {
            EngineRuleWrapper.ReprocessingRule(Body).Text.Should().Be(Imports + "Public Class GatewayRule\r\n" +
                                                                      "Public Shared Function Match(Data As DictionaryNoBoxing(Of String), List As Dictionary(Of String, List(Of String)),KVP As PooledDictionary(Of String, DictionaryNoBoxing(Of String)),Log As ILog) As Boolean\r\n" +
                                                                      "Dim Matched As Boolean\r\nTry\r\n" + Body +
                                                                      "\r\n" + TryCatchTail);
        }

        [Fact]
        public void WithoutTryCatchTheBodyIsBareSoRuntimeErrorsSurface()
        {
            var wrapped = EngineRuleWrapper.GatewayRule(Body, false);

            wrapped.Text.Should().NotContain("Try").And.NotContain("Catch");
            wrapped.Text.Substring(wrapped.CharOffset, Body.Length).Should().Be(Body);
            wrapped.Text.Split('\n')[wrapped.LineOffset].TrimEnd('\r').Should().Be(Body);
        }
    }
}