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
using System.Text;
using System.Text.RegularExpressions;
using Jube.Dictionary;
using Jube.GraphRecallProtocol;
using Jube.Parser.Models;
using log4net;
using Jube.Svirfd;

namespace Jube.Parser
{
    public class Parser
    {
        private readonly ILog log;
        private readonly List<string> ruleScriptTokens;
        private readonly Lazy<TokenIndex> tokenIndex;
        public List<string> EntityAnalysisModelAbstractionCalculations;
        public Dictionary<string, int> EntityAnalysisModelInlineScriptProperties;
        public Dictionary<string, EntityAnalysisModelRequestXPath> EntityAnalysisModelRequestXPaths;
        public List<string> EntityAnalysisModelsAbstractionRule;
        public List<string> EntityAnalysisModelsActivationRules;
        public List<string> EntityAnalysisModelsAdvancedAnalyticsAdaptations;
        public List<string> EntityAnalysisModelsDictionaries;
        public List<string> EntityAnalysisModelsExhaustiveAdaptations;
        public List<string> EntityAnalysisModelsGraph;
        public List<string> EntityAnalysisModelsSvirfd;
        public List<string> EntityAnalysisModelsHttpAdaptations;
        public Dictionary<string, int> EntityAnalysisModelsInlineFunctions;
        public List<string> EntityAnalysisModelsLists;
        public List<string> EntityAnalysisModelsSanctions;
        public List<string> EntityAnalysisModelsTtlCounters;

        public Parser(ILog log,
            List<string> ruleScriptTokens
        )
        {
            this.log = log;
            this.ruleScriptTokens = ruleScriptTokens ?? [];
            tokenIndex = new Lazy<TokenIndex>(() => TokenIndex.Build(this.ruleScriptTokens));

            if (!this.ruleScriptTokens.Contains("Return"))
            {
                this.ruleScriptTokens.Add("return");
            }

            if (!this.ruleScriptTokens.Contains("Matched"))
            {
                this.ruleScriptTokens.Add("Matched");
            }

            if (!this.ruleScriptTokens.Contains("If"))
            {
                this.ruleScriptTokens.Add("if");
            }

            if (!this.ruleScriptTokens.Contains("Then"))
            {
                this.ruleScriptTokens.Add("then");
            }

            if (!this.ruleScriptTokens.Contains("End If"))
            {
                this.ruleScriptTokens.Add("End If");
            }

            if (!this.ruleScriptTokens.Contains("False"))
            {
                this.ruleScriptTokens.Add("False");
            }

            if (!this.ruleScriptTokens.Contains("true"))
            {
                this.ruleScriptTokens.Add("true");
            }

            if (!this.ruleScriptTokens.Contains("Payload"))
            {
                this.ruleScriptTokens.Add("Payload");
            }

            if (!this.ruleScriptTokens.Contains("Abstraction"))
            {
                this.ruleScriptTokens.Add("Abstraction");
            }

            if (!this.ruleScriptTokens.Contains("Activation"))
            {
                this.ruleScriptTokens.Add("Activation");
            }

            if (!this.ruleScriptTokens.Contains("ExhaustiveAdaptation"))
            {
                this.ruleScriptTokens.Add("ExhaustiveAdaptation");
            }

            if (!this.ruleScriptTokens.Contains("HttpAdaptation"))
            {
                this.ruleScriptTokens.Add("HttpAdaptation");
            }

            if (!this.ruleScriptTokens.Contains("Value"))
            {
                this.ruleScriptTokens.Add("Value");
            }

            if (!this.ruleScriptTokens.Contains("?"))
            {
                this.ruleScriptTokens.Add("?");
            }

            if (!this.ruleScriptTokens.Contains("Else"))
            {
                this.ruleScriptTokens.Add("Else");
            }

            if (!this.ruleScriptTokens.Contains("ElseIf"))
            {
                this.ruleScriptTokens.Add("ElseIf");
            }

            if (!this.ruleScriptTokens.Contains("AdvancedAnalyticsAdaptation"))
            {
                this.ruleScriptTokens.Add("AdvancedAnalyticsAdaptation");
            }

            if (!this.ruleScriptTokens.Contains("Graph"))
            {
                this.ruleScriptTokens.Add("Graph");
            }

            foreach (var graphToken in GraphEntityMembers.Tokens.Where(t => !this.ruleScriptTokens.Contains(t)))
            {
                this.ruleScriptTokens.Add(graphToken);
            }

            if (!this.ruleScriptTokens.Contains("Svirfd"))
            {
                this.ruleScriptTokens.Add("Svirfd");
            }

            foreach (var svirfdToken in SvirfdMembers.Tokens.Where(t => !this.ruleScriptTokens.Contains(t)))
            {
                this.ruleScriptTokens.Add(svirfdToken);
            }

            if (!this.ruleScriptTokens.Contains("Select"))
            {
                this.ruleScriptTokens.Add("Select");
            }

            if (!this.ruleScriptTokens.Contains("Case"))
            {
                this.ruleScriptTokens.Add("Case");
            }

            if (!this.ruleScriptTokens.Contains("End Select"))
            {
                this.ruleScriptTokens.Add("End Select");
            }

            if (!this.ruleScriptTokens.Contains("Contains"))
            {
                this.ruleScriptTokens.Add("Contains");
            }

            if (!this.ruleScriptTokens.Contains("StartsWith"))
            {
                this.ruleScriptTokens.Add("StartsWith");
            }

            if (!this.ruleScriptTokens.Contains("EndsWith"))
            {
                this.ruleScriptTokens.Add("EndsWith");
            }

            if (!this.ruleScriptTokens.Contains("Sanctions"))
            {
                this.ruleScriptTokens.Add("Sanctions");
            }

            if (!this.ruleScriptTokens.Contains("KVP"))
            {
                this.ruleScriptTokens.Add("KVP");
            }

            if (!this.ruleScriptTokens.Contains("List"))
            {
                this.ruleScriptTokens.Add("List");
            }

            if (!this.ruleScriptTokens.Contains("TTLCounter"))
            {
                this.ruleScriptTokens.Add("TTLCounter");
            }

            if (!this.ruleScriptTokens.Contains("String"))
            {
                this.ruleScriptTokens.Add("String");
            }

            if (!this.ruleScriptTokens.Contains("Double"))
            {
                this.ruleScriptTokens.Add("Double");
            }

            if (!this.ruleScriptTokens.Contains("Integer"))
            {
                this.ruleScriptTokens.Add("Integer");
            }

            if (!this.ruleScriptTokens.Contains("DateTime"))
            {
                this.ruleScriptTokens.Add("DateTime");
            }

            if (!this.ruleScriptTokens.Contains("AsString"))
            {
                this.ruleScriptTokens.Add("AsString");
            }

            if (!this.ruleScriptTokens.Contains("AsInt"))
            {
                this.ruleScriptTokens.Add("AsInt");
            }

            if (!this.ruleScriptTokens.Contains("AsDouble"))
            {
                this.ruleScriptTokens.Add("AsDouble");
            }

            if (!this.ruleScriptTokens.Contains("AsDateTime"))
            {
                this.ruleScriptTokens.Add("AsDateTime");
            }

            if (!this.ruleScriptTokens.Contains("AsBool"))
            {
                this.ruleScriptTokens.Add("AsBool");
            }

            if (!this.ruleScriptTokens.Contains("Data"))
            {
                this.ruleScriptTokens.Add("Data");
            }

            if (!this.ruleScriptTokens.Contains("Calculation"))
            {
                this.ruleScriptTokens.Add("Calculation");
            }

            if (!this.ruleScriptTokens.Contains("Not"))
            {
                this.ruleScriptTokens.Add("Not");
            }

            if (!this.ruleScriptTokens.Contains("AND"))
            {
                this.ruleScriptTokens.Add("AND");
            }

            if (!this.ruleScriptTokens.Contains("OR"))
            {
                this.ruleScriptTokens.Add("OR");
            }

            if (!this.ruleScriptTokens.Contains("GetValueOrThrow"))
            {
                this.ruleScriptTokens.Add("GetValueOrThrow");
            }

            var type = typeof(Dictionary.Extensions.Extensions);

            var methods = type.GetMethods(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Static
            );

            foreach (var method in methods)
            {
                if (method.Name == "Eval")
                {
                    continue;
                }

                if (!this.ruleScriptTokens.Contains(method.Name))
                {
                    this.ruleScriptTokens.Add(method.Name);
                }
            }

            if (!EvalExpressionRegistry.Enabled)
            {
                return;
            }

            foreach (var name in EvalExpressionRegistry.Entries.Keys)
            {
                if (!this.ruleScriptTokens.Contains(name))
                {
                    this.ruleScriptTokens.Add(name);
                }
            }
        }

        private static readonly Regex StringLiteral = new("\"(?:[^\"]|\"\")*\"", RegexOptions.None,
            TimeSpan.FromSeconds(5));

        private static readonly TokenIndex cursorTokenIndex = TokenIndex.Build(
        [
            "Cursor", "Index", "Position", "Size", "Matches", "IsFirst", "IsLast", "IsCurrent", "Result",
            "Exit For", "Continue For"
        ]);

        public const string RestrictedTokenCode = "RestrictedToken";

        public static IReadOnlyList<string> CursorTokens =>
            [.. cursorTokenIndex.SingleWords.Order(StringComparer.Ordinal), .. cursorTokenIndex.MultiWords];

        public IReadOnlyList<string> RuleScriptTokens => ruleScriptTokens;

        private static string NormaliseVisualBasicQuotes(string text)
        {
            return text.Replace('\u201C', '"').Replace('\u201D', '"').Replace('\uFF02', '"');
        }

        private static int CursorTokenLength(string[] tokens, int index)
        {
            if (cursorTokenIndex.SingleWords.Contains(tokens[index]))
            {
                return 1;
            }

            return cursorTokenIndex.MultiWords
                .Select(cursorToken => cursorToken.Split(' '))
                .Where(words => index + words.Length <= tokens.Length && words
                    .Select((word, offset) =>
                        string.Equals(word, tokens[index + offset], StringComparison.OrdinalIgnoreCase))
                    .All(equal => equal))
                .Select(words => words.Length)
                .FirstOrDefault();
        }

        public ParsedRule Parse(ParsedRule parsedRule, bool allowCursor = false)
        {
            if (!allowCursor || parsedRule.ErrorSpans == null)
            {
                return ParseTokens(parsedRule, allowCursor, null);
            }

            var translated = NormaliseVisualBasicQuotes(parsedRule.ParsedRuleText ?? string.Empty);
            var errorsBefore = parsedRule.ErrorSpans.Count;
            var usedCursorBefore = parsedRule.UsesCursor;

            ParseTokens(parsedRule, true, null);

            if (parsedRule.ErrorSpans.Count == errorsBefore || !RuleVariableDeclarations.Any(translated))
            {
                return parsedRule;
            }

            parsedRule.ErrorSpans.RemoveRange(errorsBefore, parsedRule.ErrorSpans.Count - errorsBefore);
            parsedRule.UsesCursor = usedCursorBefore;

            var reading = RuleVariableDeclarations.Read(translated);

            foreach (var error in reading.Errors)
            {
                Add(parsedRule, error);
            }

            parsedRule.ParsedRuleText = reading.Text;

            var declared = reading.Variables.Select(v => v.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            ParseTokens(parsedRule, true, declared);

            if (parsedRule.ErrorSpans.Count != errorsBefore)
            {
                return parsedRule;
            }

            var declaredOnly = declared.Where(name =>
                !tokenIndex.Value.SingleWords.Contains(name) &&
                !cursorTokenIndex.SingleWords.Contains(name)).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var error in RuleVariableUsage.Check(parsedRule.ParsedRuleText, reading.Variables,
                         declaredOnly))
            {
                Add(parsedRule, error);
            }

            if (parsedRule.ErrorSpans.Count != errorsBefore)
            {
                return parsedRule;
            }

            parsedRule.Variables = reading.Variables;
            parsedRule.UsesCursor |= RuleVariableEmitter.HasScan(reading.Variables);

            return parsedRule;
        }

        private static void Add(ParsedRule parsedRule, ErrorSpan error)
        {
            if (error.Column.HasValue && error.EndColumn.HasValue)
            {
                parsedRule.Locate(error.Line, error.Column.Value, error.EndColumn.Value, error);
            }

            parsedRule.ErrorSpans.Add(error);
        }

        private ParsedRule ParseTokens(ParsedRule parsedRule, bool allowCursor, HashSet<string> declared)
        {
            try
            {
                parsedRule.ParsedRuleText = NormaliseVisualBasicQuotes(parsedRule.ParsedRuleText);
                var lines = parsedRule.ParsedRuleText.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
                var softParseFailed = false;
                var i = 0;

                foreach (var line in lines)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        string[] separator =
                            [",", " ", "(", ")", "=", ">", "<", ">=", "<=", "<>", ".", "_", "+", "-", "/", "*", "&"];

                        var tokens = StringLiteral.Replace(line, "")
                            .Split(separator, StringSplitOptions.RemoveEmptyEntries);

                        var masked = MaskLiterals(line);
                        var cursor = 0;
                        int j;

                        for (j = 0; j < tokens.Length; j++)
                        {
                            var tokenAt = masked.IndexOf(tokens[j], cursor, StringComparison.Ordinal);

                            if (tokenAt >= 0)
                            {
                                cursor = tokenAt + tokens[j].Length;
                            }

                            var valid = false;

                            if (NumericHelper.IsNumeric(tokens[j]))
                            {
                                valid = true;
                            }
                            else if (tokenIndex.Value.SingleWords.Contains(tokens[j]))
                            {
                                valid = true;
                            }
                            else
                                //Loop around the multi word permissions, matching up with the tokens that have been found in the rule text.
                            {
                                foreach (var ruleScriptToken in tokenIndex.Value.MultiWords)
                                {
                                    //There is a curious case of valid language tokens such as "End If," which is logically a single token, but would be read as two tokens.  As seen above, it is possible to store such logical tokens in the registry.
                                    var permissionTokensCount = ruleScriptToken.Split(" ".ToCharArray()).Length;
                                    //Find out how many tokens exist inside this token.

                                    var testToken = "";
                                    int f;

                                    //This joins up the token for matching based on the next number of tokens
                                    for (f = 0; f < permissionTokensCount; f++)
                                    {
                                        if (f > 0)
                                        {
                                            testToken += " ";
                                        }

                                        var extend = j + f; //We are adding the tokens that come after.

                                        if (extend < tokens.Length)
                                        {
                                            testToken += tokens[extend];
                                        }
                                        //A new test token has been constructed, for example, End If
                                    }

                                    if (string.Equals(ruleScriptToken, testToken,
                                            StringComparison.CurrentCultureIgnoreCase))
                                        //Check fof the test token (perhaps derived) matches.
                                    {
                                        valid = true;
                                    }
                                }
                            }

                            if (!valid && allowCursor)
                            {
                                var consumed = CursorTokenLength(tokens, j);

                                if (consumed > 0)
                                {
                                    valid = true;
                                    parsedRule.UsesCursor = true;

                                    for (var skipped = 1; skipped < consumed; skipped++)
                                    {
                                        j++;

                                        var skippedAt = masked.IndexOf(tokens[j], cursor, StringComparison.Ordinal);

                                        if (skippedAt >= 0)
                                        {
                                            cursor = skippedAt + tokens[j].Length;
                                        }
                                    }
                                }
                            }

                            if (!valid && declared != null && declared.Contains(tokens[j]))
                            {
                                valid = true;
                            }

                            if (valid)
                            {
                                continue; //This would be enough to kill the routine, return false.
                            }

                            softParseFailed = true;

                            var restricted = new ErrorSpan
                            {
                                Code = RestrictedTokenCode,
                                Line = i,
                                Message =
                                    $"Line {i + 1}: Security restricted token '{tokens[j]}'  has been discovered in user code."
                            };

                            if (tokenAt >= 0)
                            {
                                parsedRule.Locate(i, tokenAt, tokenAt + tokens[j].Length, restricted);
                            }

                            parsedRule.ErrorSpans.Add(restricted);
                        }
                    }

                    i += 1;
                }

                if (softParseFailed) //Any error causes this, although all issues are in the logs.x
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Soft Parser: User code of {parsedRule.OriginalRuleText?.Length ?? 0} characters has failed a soft parse.");
                    }
                }
                else if (EvalExpressionRegistry.Enabled)
                {
                    foreach (var (name, resultTypeId) in EvalExpressionRegistry.Entries)
                    {
                        var vbKeyword = EvalExpressionRegistry.ResultTypes[resultTypeId].VbKeyword;
                        var pattern = $@"\.{Regex.Escape(name)}\b(?:\(\s*\))?";

                        parsedRule.ParsedRuleText =
                            Regex.Replace(parsedRule.ParsedRuleText, pattern, $".Eval(Of {vbKeyword})(\"{name}\")");
                    }
                }
            }
            catch (Exception ex)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Soft Parser: User code has failed a soft parse: {parsedRule.OriginalRuleText} and error {ex}");
                }

                parsedRule.ErrorSpans.Add(new ErrorSpan
                {
                    Line = 0,
                    Message = "The rule text could not be parsed for restricted tokens and has been refused."
                });
            }

            return parsedRule;
        }

        public ParsedRule WrapGatewayRule(ParsedRule parsedRule, bool tryCatchWrap)
        {
            var sb = new StringBuilder();

            var countLine = 1;
            sb.AppendLine("Imports System.IO");

            countLine += 1;
            sb.AppendLine("Imports System.Xml");

            countLine += 1;
            sb.AppendLine("Imports log4net");

            countLine += 1;
            sb.AppendLine("Imports System.Net");

            countLine += 1;
            sb.AppendLine("Imports Jube.Dictionary");

            countLine += 1;
            sb.AppendLine("Imports Jube.Dictionary.Extensions");

            countLine += 1;
            sb.AppendLine("Imports System.Collections.Generic");

            countLine += 1;
            sb.AppendLine("Imports System");

            countLine += 1;
            sb.AppendLine("Imports System.Linq");

            countLine += 1;
            sb.AppendLine("Imports Svirfd");

            countLine += 1;
            sb.AppendLine("Imports Jube.Svirfd");

            countLine += 1;
            sb.AppendLine("Public Class GatewayRule");

            countLine += 1;

            sb.AppendLine(
                "Public Shared Function Match(Data As DictionaryNoBoxing(Of String),List As Dictionary(Of String, List(Of String)), KVP As PooledDictionary(Of String, Double),Svirfd As PooledDictionary(Of String, IpResponse),Log as ILog) As Boolean");

            countLine += 1;
            sb.AppendLine("Dim Matched as Boolean");

            if (tryCatchWrap)
            {
                sb.AppendLine("Try");
                countLine += 1;
            }

            parsedRule.CharOffset = sb.Length;
            parsedRule.LineOffset = countLine;
            sb.AppendLine(parsedRule.ParsedRuleText);

            if (tryCatchWrap)
            {
                sb.AppendLine("Catch ex As Exception");
                sb.AppendLine("Log.Info(ex.ToString)");
                sb.AppendLine("End Try");
            }

            sb.AppendLine("Return Matched");
            sb.AppendLine("End Function");
            sb.AppendLine("End Class");
            parsedRule.ParsedRuleText = sb.ToString();

            return parsedRule;
        }

        public ParsedRule WrapAbstractionRule(ParsedRule parsedRule, bool tryCatchWrap)
        {
            var sb = new StringBuilder();

            var countLine = 1;
            sb.AppendLine("Imports System.IO");

            countLine += 1;
            sb.AppendLine("Imports System.Xml");

            countLine += 1;
            sb.AppendLine("Imports log4net");

            countLine += 1;
            sb.AppendLine("Imports System.Net");

            countLine += 1;
            sb.AppendLine("Imports Jube.Dictionary");

            countLine += 1;
            sb.AppendLine("Imports Jube.Dictionary.Extensions");

            countLine += 1;
            sb.AppendLine("Imports System.Collections.Generic");

            countLine += 1;
            sb.AppendLine("Imports System");

            countLine += 1;
            sb.AppendLine("Imports System.Linq");

            countLine += 1;
            sb.AppendLine("Imports Svirfd");

            countLine += 1;
            sb.AppendLine("Imports Jube.Svirfd");

            countLine += 1;
            sb.AppendLine("Public Class GatewayRule");

            const string signature =
                "Public Shared Function Match(Data As DictionaryNoBoxing(Of String),List As Dictionary(Of String, List(Of String)), KVP As PooledDictionary(Of String, Double),Svirfd As PooledDictionary(Of String, IpResponse),Cursor As AbstractionCursor,Log as ILog) As Boolean";

            foreach (var field in RuleVariableEmitter.Fields(parsedRule.Variables))
            {
                countLine += 1;
                sb.AppendLine(field);
            }

            if (RuleVariableEmitter.HasScan(parsedRule.Variables))
            {
                foreach (var line in RuleVariableEmitter.Entry("GatewayRule", signature))
                {
                    countLine += 1;
                    sb.AppendLine(line);
                }
            }
            else
            {
                countLine += 1;
                sb.AppendLine(signature);
            }

            countLine += 1;
            sb.AppendLine("Dim Matched as Boolean");

            countLine += 1;
            sb.AppendLine("Dim CursorPass As Integer");

            foreach (var local in RuleVariableEmitter.Locals(parsedRule.Variables))
            {
                countLine += 1;
                sb.AppendLine(local);
            }

            if (tryCatchWrap)
            {
                sb.AppendLine("Try");
                countLine += 1;
            }

            countLine += 1;
            sb.AppendLine("For CursorPass = 0 To 0");

            parsedRule.CharOffset = sb.Length;
            parsedRule.LineOffset = countLine;
            sb.AppendLine(parsedRule.ParsedRuleText);
            sb.AppendLine("Next");

            if (tryCatchWrap)
            {
                sb.AppendLine("Catch ex As Exception");
                sb.AppendLine("Log.Info(ex.ToString)");
                sb.AppendLine("CursorPass = 1");
                sb.AppendLine("End Try");
            }

            sb.AppendLine("If CursorPass = 0 AndAlso Cursor IsNot Nothing Then Cursor.Stop()");
            sb.AppendLine("Return Matched");
            sb.AppendLine("End Function");
            sb.AppendLine("End Class");
            parsedRule.ParsedRuleText = sb.ToString();

            return parsedRule;
        }

        public ParsedRule WrapActivationRule(ParsedRule parsedRule, bool tryCatchWrap)
        {
            var sb = new StringBuilder();

            var countLine = 1;
            sb.AppendLine("Imports System.IO");

            countLine += 1;
            sb.AppendLine("Imports System.Xml");

            countLine += 1;
            sb.AppendLine("Imports log4net");

            countLine += 1;
            sb.AppendLine("Imports System.Net");

            countLine += 1;
            sb.AppendLine("Imports System.Collections.Generic");

            countLine += 1;
            sb.AppendLine("Imports Jube.Dictionary");

            countLine += 1;
            sb.AppendLine("Imports Jube.Dictionary.Extensions");

            countLine += 1;
            sb.AppendLine("Imports System");

            countLine += 1;
            sb.AppendLine("Imports System.Linq");

            countLine += 1;
            sb.AppendLine("Imports Svirfd");

            countLine += 1;
            sb.AppendLine("Imports Jube.Svirfd");

            countLine += 1;
            sb.AppendLine("Imports Jube.HttpAdaptationProtocol");

            countLine += 1;
            sb.AppendLine("Imports Jube.GraphRecallProtocol");

            countLine += 1;
            sb.AppendLine("Public Class ActivationRule");

            countLine += 1;

            sb.AppendLine(
                "Public Shared Function Match(Data As DictionaryNoBoxing(Of String),TTLCounter As PooledDictionary(Of String, Double),Abstraction As PooledDictionary(Of String, Double),HttpAdaptation As PooledDictionary(Of String, Adaptation),ExhaustiveAdaptation As PooledDictionary(Of String, Double),AdvancedAnalyticsAdaptation As PooledDictionary(Of String, Double),Graph As PooledDictionary(Of String, GraphEntity),List as Dictionary(Of String,List(Of String)),Calculation As PooledDictionary(Of String, Double),Sanctions As PooledDictionary(Of String, Double),KVP As PooledDictionary(Of String, Double),Activation as ICollection(Of String),Svirfd As PooledDictionary(Of String, IpResponse),Log as ILog) As Boolean");

            countLine += 1;
            sb.AppendLine("Dim Matched as Boolean");

            if (tryCatchWrap)
            {
                sb.AppendLine("Try");
                countLine += 1;
            }

            parsedRule.CharOffset = sb.Length;
            parsedRule.LineOffset = countLine;
            sb.AppendLine(parsedRule.ParsedRuleText);

            if (tryCatchWrap)
            {
                sb.AppendLine("Catch ex As Exception");
                sb.AppendLine("Log.Info(ex.ToString)");
                sb.AppendLine("End Try");
            }

            sb.AppendLine("Return Matched");
            sb.AppendLine("End Function");
            sb.AppendLine("End Class");
            parsedRule.ParsedRuleText = sb.ToString();

            return parsedRule;
        }

        public ParsedRule WrapAbstractionCalculation(ParsedRule parsedRule, bool tryCatchWrap)
        {
            var sb = new StringBuilder();

            var countLine = 1;
            sb.AppendLine("Imports System.IO");

            countLine += 1;
            sb.AppendLine("Imports System.Xml");

            countLine += 1;
            sb.AppendLine("Imports log4net");

            countLine += 1;
            sb.AppendLine("Imports System.Net");

            countLine += 1;
            sb.AppendLine("Imports System.Collections.Generic");

            countLine += 1;
            sb.AppendLine("Imports Jube.Dictionary");

            countLine += 1;
            sb.AppendLine("Imports Jube.Dictionary.Extensions");

            countLine += 1;
            sb.AppendLine("Imports System");

            countLine += 1;
            sb.AppendLine("Imports System.Linq");

            countLine += 1;
            sb.AppendLine("Imports Svirfd");

            countLine += 1;
            sb.AppendLine("Imports Jube.Svirfd");

            countLine += 1;
            sb.AppendLine("Public Class CalculationRule");

            countLine += 1;

            sb.AppendLine(
                "Public Shared Function Match(Data As DictionaryNoBoxing(Of String),TTLCounter As PooledDictionary(Of String, Double),Abstraction As PooledDictionary(Of String, Double),List as Dictionary(Of String,List(Of String)),Calculation As PooledDictionary(Of String, Double),Sanctions As PooledDictionary(Of String, Double),KVP As PooledDictionary(Of String, Double),Svirfd As PooledDictionary(Of String, IpResponse),Log as ILog) As Double");

            countLine += 1;
            sb.AppendLine("Dim Matched as Double");

            if (tryCatchWrap)
            {
                sb.AppendLine("Try");
                countLine += 1;
            }

            parsedRule.CharOffset = sb.Length;
            parsedRule.LineOffset = countLine;
            sb.AppendLine(parsedRule.ParsedRuleText);

            if (tryCatchWrap)
            {
                sb.AppendLine("Catch ex As Exception");
                sb.AppendLine("Log.Info(ex.ToString)");
                sb.AppendLine("End Try");
            }

            sb.AppendLine("Return Matched");
            sb.AppendLine("End Function");
            sb.AppendLine("End Class");
            parsedRule.ParsedRuleText = sb.ToString();

            return parsedRule;
        }

        public ParsedRule WrapInlineFunction(ParsedRule parsedRule, bool tryCatchWrap)
        {
            var sb = new StringBuilder();

            var countLine = 1;
            sb.AppendLine("Imports System.IO");

            countLine += 1;
            sb.AppendLine("Imports System.Xml");

            countLine += 1;
            sb.AppendLine("Imports log4net");

            countLine += 1;
            sb.AppendLine("Imports System.Net");

            countLine += 1;
            sb.AppendLine("Imports System.Collections.Generic");

            countLine += 1;
            sb.AppendLine("Imports Jube.Dictionary");

            countLine += 1;
            sb.AppendLine("Imports Jube.Dictionary.Extensions");

            countLine += 1;
            sb.AppendLine("Imports System");

            countLine += 1;
            sb.AppendLine("Imports System.Linq");

            countLine += 1;
            sb.AppendLine("Imports Svirfd");

            countLine += 1;
            sb.AppendLine("Imports Jube.Svirfd");

            countLine += 1;
            sb.AppendLine("Public Class InlineFunction");

            countLine += 1;

            sb.AppendLine(
                "Public Shared Function Match(Data As DictionaryNoBoxing(Of String),TTLCounter as PooledDictionary(Of String,Long),List as Dictionary(Of String,List(Of String)),KVP As PooledDictionary(Of String, Double),Svirfd As PooledDictionary(Of String, IpResponse),Log as ILog) As Object");

            if (tryCatchWrap)
            {
                sb.AppendLine("Try");
                countLine += 1;
            }

            parsedRule.CharOffset = sb.Length;
            parsedRule.LineOffset = countLine;
            sb.AppendLine(parsedRule.ParsedRuleText);

            if (tryCatchWrap)
            {
                sb.AppendLine("Catch ex As Exception");
                sb.AppendLine("Log.Info(ex.ToString)");
                sb.AppendLine("End Try");
            }

            sb.AppendLine("End Function");
            sb.AppendLine("End Class");
            parsedRule.ParsedRuleText = sb.ToString();

            return parsedRule;
        }

        private static int? ColumnOf(string line, string token)
        {
            var index = string.IsNullOrEmpty(token) ? -1 : line.IndexOf(token, StringComparison.Ordinal);

            return index < 0 ? null : index;
        }

        private static int? EndColumnOf(string line, string token)
        {
            var index = ColumnOf(line, token);

            return index == null ? null : index + token.Length;
        }

        private static string MaskLiterals(string line)
        {
            return Regex.Replace(line, "\"(?:[^\"]|\"\")*\"", match => new string(' ', match.Length));
        }

        public ParsedRule TranslateFromDotNotation(ParsedRule parsedRule, bool showOnlyCacheForPayload = false)
        {
            var sb = new StringBuilder();

            var lines = NormaliseVisualBasicQuotes(parsedRule.OriginalRuleText)
                .Split(["\r\n", "\r", "\n"], StringSplitOptions.None);

            for (var i = 0; i < lines.Length; i++)
            {
                var originalLine = lines[i];
                var dictionaryFindReplace = new Dictionary<string, string>();

                var separators = new[]
                {
                    " ", ",", "=", ">", "(", ")", "<", ">=", "<=", "<>", "not"
                };

                var specialTokens = new[]
                {
                    "in", "like"
                };

                var tokens = lines[i].Split(separators, StringSplitOptions.RemoveEmptyEntries);

                foreach (var t in tokens)
                {
                    var replaceString = string.Empty;
                    var findString = string.Empty;

                    if (specialTokens.Contains(t, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var elements = t.Split(".", StringSplitOptions.RemoveEmptyEntries);

                    if (elements.Length <= 0)
                    {
                        continue;
                    }

                    var firstString = elements[0];
                    var replaceKey = elements.Length > 1 ? elements[0] + "." + elements[1] : firstString;

                    for (var k = 1; k < elements.Length; k++)
                    {
                        if (k == 1)
                        {
                            if (string.Equals("payload", firstString, StringComparison.OrdinalIgnoreCase))
                            {
                                findString = firstString + "." + elements[k];

                                var asFunction = "AsString()";
                                var databaseCast = "";
                                var defaultValue = "";

                                if (EntityAnalysisModelRequestXPaths != null ||
                                    EntityAnalysisModelInlineScriptProperties != null)
                                {
                                    if (EntityAnalysisModelRequestXPaths.TryGetValue(elements[k], out var value))
                                    {
                                        if (!EntityAnalysisModelRequestXPaths[elements[k]].Cache &&
                                            showOnlyCacheForPayload)
                                        {
                                            var errorSpan = new ErrorSpan
                                            {
                                                Message =
                                                    $"Line {i + 1}: Request XPath exists but it is not defined as cache for {elements[k]}.",
                                                Line = i,
                                                Column = ColumnOf(originalLine, findString),
                                                EndColumn = EndColumnOf(originalLine, findString)
                                            };

                                            parsedRule.ErrorSpans.Add(errorSpan);
                                            asFunction = "AsString()";
                                        }
                                        else
                                        {
                                            asFunction = value.DataTypeId switch
                                            {
                                                1 => "AsString()",
                                                2 => "AsInt()",
                                                3 => "AsDouble()",
                                                4 => "AsDateTime()",
                                                5 => "AsBool",
                                                6 => "AsDouble()",
                                                7 => "AsDouble()",
                                                8 => "AsString()",
                                                _ => "AsString()"
                                            };

                                            switch (value.DataTypeId)
                                            {
                                                case 2:
                                                    databaseCast = "::int";
                                                    defaultValue = value.DefaultValue;

                                                    if (!int.TryParse(defaultValue, out _))
                                                    {
                                                        defaultValue = "0";
                                                    }

                                                    break;
                                                case 3:
                                                    databaseCast = "::float8";
                                                    defaultValue = value.DefaultValue;

                                                    if (!double.TryParse(defaultValue, out _))
                                                    {
                                                        defaultValue = "0";
                                                    }

                                                    break;
                                                case 4:
                                                    databaseCast = "::timestamp";
                                                    defaultValue = value.DefaultValue;

                                                    if (!DateTime.TryParse(defaultValue, out _))
                                                    {
                                                        defaultValue = "'" + DateTime.UtcNow.ToString("O") + "'";
                                                    }

                                                    break;
                                                case 5:
                                                    databaseCast = "::boolean";
                                                    defaultValue = value.DefaultValue;

                                                    if (!bool.TryParse(defaultValue, out _))
                                                    {
                                                        defaultValue = "false";
                                                    }

                                                    break;
                                                case 6:
                                                    databaseCast = "::float8";

                                                    if (!double.TryParse(defaultValue, out _))

                                                    {
                                                        defaultValue = EntityAnalysisModelRequestXPaths[elements[k]]
                                                            .DefaultValue;
                                                    }

                                                    break;
                                                case 7:
                                                    databaseCast = "::float8";
                                                    defaultValue = value.DefaultValue;

                                                    if (!double.TryParse(defaultValue, out _))
                                                    {
                                                        defaultValue = "0";
                                                    }

                                                    break;
                                                default:
                                                    defaultValue = "'" + value.DefaultValue + "'";
                                                    break;
                                            }
                                        }
                                    }
                                    else if (EntityAnalysisModelInlineScriptProperties.ContainsKey(elements[k]))
                                    {
                                        asFunction = EntityAnalysisModelInlineScriptProperties[elements[k]] switch
                                        {
                                            1 => "AsString()",
                                            2 => "AsInt()",
                                            3 => "AsDouble()",
                                            4 => "AsDateTime()",
                                            5 => "AsBool",
                                            6 => "AsDouble()",
                                            7 => "AsDouble()",
                                            _ => "AsString()"
                                        };

                                        databaseCast = EntityAnalysisModelInlineScriptProperties[elements[k]] switch
                                        {
                                            2 => "::int",
                                            3 => "::float8",
                                            4 => "::timestamp",
                                            5 => "::boolean",
                                            6 or 7 => "::float8",
                                            _ => databaseCast
                                        };
                                    }
                                    else if (EntityAnalysisModelsInlineFunctions.ContainsKey(elements[k]))
                                    {
                                        asFunction = EntityAnalysisModelsInlineFunctions[elements[k]] switch
                                        {
                                            1 => "AsString()",
                                            2 => "AsInt()",
                                            3 => "AsDouble()",
                                            4 => "AsDateTime()",
                                            5 => "AsBool",
                                            6 => "AsDouble()",
                                            7 => "AsDouble()",
                                            _ => "AsString()"
                                        };

                                        databaseCast = EntityAnalysisModelsInlineFunctions[elements[k]] switch
                                        {
                                            2 => "::int",
                                            3 => "::float8",
                                            4 => "::timestamp",
                                            5 => "::boolean",
                                            6 or 7 => "::float8",
                                            _ => databaseCast
                                        };
                                    }
                                    else
                                    {
                                        var errorSpan = new ErrorSpan
                                        {
                                            Message =
                                                $"Line {i + 1}: Request XPath does not exist for {elements[k]}.",
                                            Line = i,
                                            Column = ColumnOf(originalLine, findString),
                                            EndColumn = EndColumnOf(originalLine, findString)
                                        };
                                        parsedRule.ErrorSpans.Add(errorSpan);

                                        asFunction = "String()";
                                    }
                                }

                                if (!parsedRule.SelectedPayloadData.ContainsKey(elements[k]))
                                {
                                    var selectedPayloadData = new SelectedPayloadData
                                    {
                                        Name = elements[k],
                                        DatabaseCast = databaseCast,
                                        DefaultValue = defaultValue
                                    };

                                    parsedRule.SelectedPayloadData.Add(elements[k], selectedPayloadData);
                                }

                                replaceString = replaceString + "Data(\"" + elements[k] + "\")." + asFunction;
                            }
                            else if (string.Equals("TTLCounter", firstString,
                                         StringComparison.OrdinalIgnoreCase))
                            {
                                findString = firstString + "." + elements[k];

                                if (EntityAnalysisModelsTtlCounters != null)
                                {
                                    if (EntityAnalysisModelsTtlCounters.All(w => w != elements[k]))
                                    {
                                        var errorSpan = new ErrorSpan
                                        {
                                            Message =
                                                $"Line {i + 1}: TTL Counter does not exist for {elements[k]}.",
                                            Line = i,
                                            Column = ColumnOf(originalLine, findString),
                                            EndColumn = EndColumnOf(originalLine, findString)
                                        };
                                        parsedRule.ErrorSpans.Add(errorSpan);
                                    }
                                }

                                replaceString = replaceString + "TTLCounter(\"" + elements[k] + "\")";
                            }
                            else if (string.Equals("abstraction", firstString,
                                         StringComparison.OrdinalIgnoreCase))
                            {
                                findString = firstString + "." + elements[k];

                                if (EntityAnalysisModelsAbstractionRule != null)
                                {
                                    if (EntityAnalysisModelsAbstractionRule.All(w => w != elements[k]))
                                    {
                                        var errorSpan = new ErrorSpan
                                        {
                                            Message =
                                                $"Line {i + 1}: Abstraction does not exist for {elements[k]}.",
                                            Line = i,
                                            Column = ColumnOf(originalLine, findString),
                                            EndColumn = EndColumnOf(originalLine, findString)
                                        };
                                        parsedRule.ErrorSpans.Add(errorSpan);
                                    }
                                }

                                replaceString = replaceString + "Abstraction(\"" + elements[k] +
                                                "\")";
                            }
                            else if (string.Equals("dictionary", firstString,
                                         StringComparison.OrdinalIgnoreCase))
                            {
                                findString = firstString + "." + elements[k];

                                if (EntityAnalysisModelsDictionaries != null)
                                {
                                    if (EntityAnalysisModelsDictionaries.All(w => w != elements[k]))
                                    {
                                        var errorSpan = new ErrorSpan
                                        {
                                            Message =
                                                $"Line {i + 1}: Dictionary does not exist for {elements[k]}.",
                                            Line = i,
                                            Column = ColumnOf(originalLine, findString),
                                            EndColumn = EndColumnOf(originalLine, findString)
                                        };
                                        parsedRule.ErrorSpans.Add(errorSpan);
                                    }
                                }

                                replaceString = replaceString + "KVP(\"" + elements[k] + "\")";
                            }
                            else if (string.Equals("Sanction", firstString,
                                         StringComparison.OrdinalIgnoreCase))
                            {
                                findString = firstString + "." + elements[k];

                                if (EntityAnalysisModelsSanctions != null)
                                {
                                    if (EntityAnalysisModelsSanctions.All(w => w != elements[k]))
                                    {
                                        var errorSpan = new ErrorSpan
                                        {
                                            Message =
                                                $"Line {i + 1}: Dictionary does not exist for {elements[k]}.",
                                            Line = i,
                                            Column = ColumnOf(originalLine, findString),
                                            EndColumn = EndColumnOf(originalLine, findString)
                                        };
                                        parsedRule.ErrorSpans.Add(errorSpan);
                                    }
                                }

                                replaceString = replaceString + "Sanctions.GetValueOrThrow(\"" + elements[k] + "\")";
                            }
                            else if (string.Equals("abstractionCalculation", firstString,
                                         StringComparison.OrdinalIgnoreCase))
                            {
                                findString = firstString + "." + elements[k];

                                if (EntityAnalysisModelAbstractionCalculations != null)
                                {
                                    if (EntityAnalysisModelAbstractionCalculations.All(w => w != elements[k]))
                                    {
                                        var errorSpan = new ErrorSpan
                                        {
                                            Message =
                                                $"Line {i + 1}: Dictionary does not exist for {elements[k]}.",
                                            Line = i,
                                            Column = ColumnOf(originalLine, findString),
                                            EndColumn = EndColumnOf(originalLine, findString)
                                        };
                                        parsedRule.ErrorSpans.Add(errorSpan);
                                    }
                                }

                                replaceString = replaceString + "Calculation(\"" + elements[k] + "\")";
                            }
                            else if (string.Equals("exhaustiveAdaptation", firstString,
                                         StringComparison.OrdinalIgnoreCase))
                            {
                                findString = firstString + "." + elements[k];

                                if (EntityAnalysisModelsExhaustiveAdaptations != null)
                                {
                                    if (EntityAnalysisModelsExhaustiveAdaptations.All(w => w != elements[k]))
                                    {
                                        var errorSpan = new ErrorSpan
                                        {
                                            Message =
                                                $"Line {i + 1}: Dictionary does not exist for {elements[k]}.",
                                            Line = i,
                                            Column = ColumnOf(originalLine, findString),
                                            EndColumn = EndColumnOf(originalLine, findString)
                                        };
                                        parsedRule.ErrorSpans.Add(errorSpan);
                                    }
                                }

                                replaceString = replaceString + "ExhaustiveAdaptation(\"" + elements[k] + "\")";
                            }
                            else if (string.Equals("advancedAnalyticsAdaptation", firstString,
                                         StringComparison.OrdinalIgnoreCase))
                            {
                                findString = firstString + "." + elements[k];

                                if (EntityAnalysisModelsAdvancedAnalyticsAdaptations != null)
                                {
                                    if (EntityAnalysisModelsAdvancedAnalyticsAdaptations.All(w => w != elements[k]))
                                    {
                                        var errorSpan = new ErrorSpan
                                        {
                                            Message =
                                                $"Line {i + 1}: Dictionary does not exist for {elements[k]}.",
                                            Line = i,
                                            Column = ColumnOf(originalLine, findString),
                                            EndColumn = EndColumnOf(originalLine, findString)
                                        };
                                        parsedRule.ErrorSpans.Add(errorSpan);
                                    }
                                }

                                replaceString = replaceString + "AdvancedAnalyticsAdaptation(\"" + elements[k] + "\")";
                            }
                            else if (string.Equals("Graph", firstString,
                                         StringComparison.OrdinalIgnoreCase))
                            {
                                findString = firstString + "." + elements[k];

                                if (EntityAnalysisModelsGraph != null)
                                {
                                    if (EntityAnalysisModelsGraph.All(w => w != elements[k]))
                                    {
                                        var errorSpan = new ErrorSpan
                                        {
                                            Message =
                                                $"Line {i + 1}: Graph entity does not exist for {elements[k]}.",
                                            Line = i,
                                            Column = ColumnOf(originalLine, findString),
                                            EndColumn = EndColumnOf(originalLine, findString)
                                        };
                                        parsedRule.ErrorSpans.Add(errorSpan);
                                    }
                                }

                                if (elements.Length > k + 1)
                                {
                                    var member = string.Join(".", elements.Skip(k + 1));
                                    findString = findString + "." + member;

                                    if (!GraphEntityMembers.IsMember(member))
                                    {
                                        var errorSpan = new ErrorSpan
                                        {
                                            Message =
                                                $"Line {i + 1}: Graph member does not exist for {member}.",
                                            Line = i,
                                            Column = ColumnOf(originalLine, findString),
                                            EndColumn = EndColumnOf(originalLine, findString)
                                        };
                                        parsedRule.ErrorSpans.Add(errorSpan);
                                    }

                                    replaceString = GraphEntityMembers.IsMethod(member)
                                        ? replaceString + "Graph(\"" + elements[k] + "\")?." + member
                                        : replaceString + "(Graph(\"" + elements[k] + "\")?." + member + ")";
                                }
                                else
                                {
                                    replaceString = replaceString + "Graph(\"" + elements[k] + "\")";
                                }

                                replaceKey = findString;
                            }
                            else if (string.Equals("Svirfd", firstString,
                                         StringComparison.OrdinalIgnoreCase))
                            {
                                findString = firstString + "." + elements[k];

                                if (EntityAnalysisModelsSvirfd != null)
                                {
                                    if (EntityAnalysisModelsSvirfd.All(w => w != elements[k]))
                                    {
                                        var errorSpan = new ErrorSpan
                                        {
                                            Message =
                                                $"Line {i + 1}: Svirfd entry does not exist for {elements[k]}.",
                                            Line = i,
                                            Column = ColumnOf(originalLine, findString),
                                            EndColumn = EndColumnOf(originalLine, findString)
                                        };
                                        parsedRule.ErrorSpans.Add(errorSpan);
                                    }
                                }

                                var svirfdEntity = "Svirfd(\"" + elements[k] + "\")";

                                if (elements.Length > k + 1)
                                {
                                    var memberPath = string.Join(".", elements.Skip(k + 1));
                                    findString = findString + "." + memberPath;

                                    var bare = SvirfdMembers.IsMethod(memberPath) ? memberPath : memberPath;

                                    if (!SvirfdMembers.IsMember(bare))
                                    {
                                        var errorSpan = new ErrorSpan
                                        {
                                            Message =
                                                $"Line {i + 1}: Svirfd member does not exist for {memberPath}.",
                                            Line = i,
                                            Column = ColumnOf(originalLine, findString),
                                            EndColumn = EndColumnOf(originalLine, findString)
                                        };
                                        parsedRule.ErrorSpans.Add(errorSpan);
                                    }

                                    if (SvirfdMembers.IsMethod(memberPath))
                                    {
                                        replaceString = replaceString + svirfdEntity + "?." + memberPath;
                                    }
                                    else
                                    {
                                        var chain = string.Join("?.",
                                            memberPath.Split('.').Select(SvirfdMembers.VisualBasicSegment));

                                        replaceString = replaceString + "(" + svirfdEntity + "?." + chain + ")";
                                    }
                                }
                                else
                                {
                                    replaceString = replaceString + svirfdEntity;
                                }

                                replaceKey = findString;
                            }
                            else if (string.Equals("HTTPAdaptation", firstString,
                                         StringComparison.OrdinalIgnoreCase))
                            {
                                findString = firstString + "." + elements[k];

                                if (EntityAnalysisModelsHttpAdaptations != null)
                                {
                                    if (EntityAnalysisModelsHttpAdaptations.All(w => w != elements[k]))
                                    {
                                        var errorSpan = new ErrorSpan
                                        {
                                            Message =
                                                $"Line {i + 1}: Dictionary does not exist for {elements[k]}.",
                                            Line = i,
                                            Column = ColumnOf(originalLine, findString),
                                            EndColumn = EndColumnOf(originalLine, findString)
                                        };
                                        parsedRule.ErrorSpans.Add(errorSpan);
                                    }
                                }

                                replaceString = replaceString + "(HTTPAdaptation(\"" + elements[k] + "\")?.Value)";
                            }

                            else if (string.Equals("list", firstString,
                                         StringComparison.OrdinalIgnoreCase))
                            {
                                findString = firstString + "." + elements[k];

                                if (EntityAnalysisModelsLists != null)
                                {
                                    if (EntityAnalysisModelsLists.All(w => w != elements[k]))
                                    {
                                        var errorSpan = new ErrorSpan
                                        {
                                            Message =
                                                $"Line {i + 1}: List does not exist for {elements[k]}.",
                                            Line = i,
                                            Column = ColumnOf(originalLine, findString),
                                            EndColumn = EndColumnOf(originalLine, findString)
                                        };
                                        parsedRule.ErrorSpans.Add(errorSpan);
                                    }
                                }

                                replaceString = replaceString + "List(\"" + elements[k] + "\")";
                            }

                            else if (string.Equals("Activation", firstString,
                                         StringComparison.OrdinalIgnoreCase))
                            {
                                findString = firstString + "." + elements[k];

                                if (EntityAnalysisModelsActivationRules != null)
                                {
                                    if (EntityAnalysisModelsActivationRules.All(w => w != elements[k]))
                                    {
                                        var errorSpan = new ErrorSpan
                                        {
                                            Message =
                                                $"Line {i + 1}: Activation Rule does not exist for {elements[k]}.",
                                            Line = i,
                                            Column = ColumnOf(originalLine, findString),
                                            EndColumn = EndColumnOf(originalLine, findString)
                                        };
                                        parsedRule.ErrorSpans.Add(errorSpan);
                                    }
                                }

                                replaceString = replaceString + "Activation.Contains(\"" + elements[k] + "\")";
                            }

                            else
                            {
                                findString = firstString + "." + elements[k];
                                replaceString = replaceString + firstString + "." + elements[k];
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(findString))
                    {
                        continue;
                    }

                    RuleReference.TryAdd(parsedRule.References, elements[0], elements[1], i);

                    dictionaryFindReplace.TryAdd(replaceKey, replaceString);
                }

                var tracked = new TrackedLine(originalLine);

                foreach (var kvp in dictionaryFindReplace.OrderByDescending(k => k.Key.Length))
                {
                    tracked.Replace(kvp.Key, kvp.Value);
                }

                parsedRule.LineMaps.Add(tracked);
                sb.AppendLine(tracked.Text);
            }

            parsedRule.ParsedRuleText = sb.ToString();

            return parsedRule;
        }
    }
}