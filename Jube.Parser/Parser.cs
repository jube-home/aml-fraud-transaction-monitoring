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
using System.Text;
using System.Text.RegularExpressions;
using log4net;

namespace Jube.Parser;

public class Parser
{
    private readonly ILog _log;
    private readonly List<string> _ruleScriptTokens;
    public List<string> EntityAnalysisModelAbstractionCalculations;
    public Dictionary<string, EntityAnalysisModelRequestXPath> EntityAnalysisModelRequestXPaths;
    public List<string> EntityAnalysisModelsAbstractionRule;
    public List<string> EntityAnalysisModelsAdaptations;
    public List<string> EntityAnalysisModelsDictionaries;
    public List<string> EntityAnalysisModelsLists;
    public List<string> EntityAnalysisModelsSanctions;
    public List<string> EntityAnalysisModelsTtlCounters;

    public Parser(ILog log,
        List<string> ruleScriptTokens
    )
    {
        _log = log;
        _ruleScriptTokens = ruleScriptTokens ?? [];

        if (!_ruleScriptTokens.Contains("Return")) _ruleScriptTokens.Add("return");
        if (!_ruleScriptTokens.Contains("If")) _ruleScriptTokens.Add("if");
        if (!_ruleScriptTokens.Contains("Then")) _ruleScriptTokens.Add("then");
        if (!_ruleScriptTokens.Contains("End If")) _ruleScriptTokens.Add("End If");
        if (!_ruleScriptTokens.Contains("False")) _ruleScriptTokens.Add("False");
        if (!_ruleScriptTokens.Contains("true")) _ruleScriptTokens.Add("true");
        if (!_ruleScriptTokens.Contains("Payload")) _ruleScriptTokens.Add("Payload");
        if (!_ruleScriptTokens.Contains("Abstraction")) _ruleScriptTokens.Add("Abstraction");
        if (!_ruleScriptTokens.Contains("Activation")) _ruleScriptTokens.Add("Activation");
        if (!_ruleScriptTokens.Contains("Select")) _ruleScriptTokens.Add("Select");
        if (!_ruleScriptTokens.Contains("Case")) _ruleScriptTokens.Add("Case");
        if (!_ruleScriptTokens.Contains("End Select")) _ruleScriptTokens.Add("End Select");
        if (!_ruleScriptTokens.Contains("Contains")) _ruleScriptTokens.Add("Contains");
        if (!_ruleScriptTokens.Contains("Sanctions")) _ruleScriptTokens.Add("Sanctions");
        if (!_ruleScriptTokens.Contains("KVP")) _ruleScriptTokens.Add("KVP");
        if (!_ruleScriptTokens.Contains("List")) _ruleScriptTokens.Add("List");
        if (!_ruleScriptTokens.Contains("TTLCounter")) _ruleScriptTokens.Add("TTLCounter");
        if (!_ruleScriptTokens.Contains("String")) _ruleScriptTokens.Add("String");
        if (!_ruleScriptTokens.Contains("Double")) _ruleScriptTokens.Add("Double");
        if (!_ruleScriptTokens.Contains("Integer")) _ruleScriptTokens.Add("Integer");
        if (!_ruleScriptTokens.Contains("DateTime")) _ruleScriptTokens.Add("DateTime");
        if (!_ruleScriptTokens.Contains("CType")) _ruleScriptTokens.Add("CType");
        if (!_ruleScriptTokens.Contains("Boolean")) _ruleScriptTokens.Add("Boolean");
        if (!_ruleScriptTokens.Contains("Data")) _ruleScriptTokens.Add("Data");
        if (!_ruleScriptTokens.Contains("Calculation")) _ruleScriptTokens.Add("Calculation");
        if (!_ruleScriptTokens.Contains("Not")) _ruleScriptTokens.Add("Not");
    }

    public ParsedRule Parse(ParsedRule parsedRule)
    {
        //var value = new List<ErrorSpan>();
        try
        {
            var lines = parsedRule.ParsedRuleText.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
            var softParseFailed = false;
            var i = 0;
            foreach (var line in lines)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    //'Remove strings as they are allowed to have special characters
                    var matches = Regex.Matches(line, "\"(?:[^\"\\\\]|\\\\.)*\"");

                    string[] separator =
                        [",", " ", "(", ")", "=", ">", "<", ">=", "<=", "<>", ".", "_", "+", "-", "/", "*", "&"];
                    var tokens = matches.Aggregate(line, (current, match) => current.Replace(match.Value, ""))
                        .Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    int j;
                    for (j = 0; j < tokens.Length; j++)
                    {
                        var valid = false;
                        if (NumericHelper.IsNumeric(tokens[j]))
                            valid = true;
                        else
                            //'Loop around all permissions,  matching up with the tokens that have been found in the rule text.
                            foreach (var ruleScriptToken in _ruleScriptTokens)
                            {
                                //'There is a curious case of valid language tokens such a "End If" which is logically a single token,  but would be read as two tokens.  As seen above,  it is possible to store such logical tokens in the registry.
                                var permissionTokensCount = ruleScriptToken.Split(" ".ToCharArray()).Length;
                                //'Find out how many tokens exist inside this token.

                                var testToken = "";
                                int f;
                                //'This joins up the a token for matching based the next number of tokens
                                for (f = 0; f < permissionTokensCount; f++)
                                {
                                    if (f > 0) testToken += " ";
                                    var extend = j + f; //'We are adding the tokens that come after.
                                    if (extend < tokens.Length) testToken += tokens[extend];
                                    //'A new test token has been constructed,  for example End If
                                }

                                if (string.Equals(ruleScriptToken, testToken,
                                        StringComparison.CurrentCultureIgnoreCase))
                                    //'Check fof the test token (perhaps derived) matches.
                                    valid = true;
                            }

                        if (valid) continue; //'This would be enough to kill the routine, return false.
                        softParseFailed = true;

                        parsedRule.ErrorSpans.Add(new ErrorSpan
                        {
                            Line = i,
                            Message =
                                $"Line {i + 1}: Security restricted token '{tokens[j]}'  has been discovered in user code."
                        });
                    }
                }

                i += 1;
            }

            if (softParseFailed) //'Any error causes this, although all issues are in the logs.x
                _log.Info($"Soft Parser: User code has failed a soft parse: {parsedRule.OriginalRuleText}");
        }
        catch (Exception ex)
        {
            _log.Info(
                $"Soft Parser: User code has failed a soft parse: {parsedRule.OriginalRuleText} and error {ex}");
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
        sb.AppendLine("Imports System.Collections.Generic");

        countLine += 1;
        sb.AppendLine("Imports System");

        countLine += 1;
        sb.AppendLine("Imports MongoDB.Bson");

        countLine += 1;
        sb.AppendLine("Imports MongoDB.Bson.IO");

        countLine += 1;
        sb.AppendLine("Imports MongoDB.Bson.Serialization");

        countLine += 1;
        sb.AppendLine("Public Class GatewayRule");

        countLine += 1;
        sb.AppendLine(
            "Public Shared Function Match(Data As Dictionary(Of String, Object),List As Dictionary(Of String, List(Of String)), KVP As Dictionary(Of String, Double),Log as ILog) As Boolean");

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
        sb.AppendLine("Imports System.Collections.Generic");

        countLine += 1;
        sb.AppendLine("Imports System");

        countLine += 1;
        sb.AppendLine("Imports MongoDB.Bson");

        countLine += 1;
        sb.AppendLine("Imports MongoDB.Bson.IO");

        countLine += 1;
        sb.AppendLine("Imports MongoDB.Bson.Serialization");

        countLine += 1;
        sb.AppendLine("Public Class GatewayRule");

        countLine += 1;
        sb.AppendLine(
            "Public Shared Function Match(Data As Dictionary(Of String, Object),List As Dictionary(Of String, List(Of String)), KVP As Dictionary(Of String, Double),Log as ILog) As Boolean");

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
        sb.AppendLine("Imports System");

        countLine += 1;
        sb.AppendLine("Imports MongoDB.Bson");

        countLine += 1;
        sb.AppendLine("Imports MongoDB.Bson.IO");

        countLine += 1;
        sb.AppendLine("Imports MongoDB.Bson.Serialization");

        countLine += 1;
        sb.AppendLine("Public Class GatewayRule");

        countLine += 1;
        sb.AppendLine(
            "Public Shared Function Match(Data As Dictionary(Of String, Object),TTLCounter As Dictionary(Of String, Integer),Abstraction As Dictionary(Of String, Double),Adaptation As Dictionary(Of String, Double),List as Dictionary(Of String,List(Of String)),Deviation as Dictionary(Of String, Double),Calculation As Dictionary(Of String, Double),Sanctions As Dictionary(Of String, Double),KVP As Dictionary(Of String, Double),Log as ILog) As Boolean");

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
        sb.AppendLine("Imports System");

        countLine += 1;
        sb.AppendLine("Imports MongoDB.Bson");

        countLine += 1;
        sb.AppendLine("Imports MongoDB.Bson.IO");

        countLine += 1;
        sb.AppendLine("Imports MongoDB.Bson.Serialization");

        countLine += 1;
        sb.AppendLine("Public Class GatewayRule");

        countLine += 1;
        sb.AppendLine(
            "Public Shared Function Match(Data As Dictionary(Of String, Object),TTLCounter As Dictionary(Of String, Integer),Abstraction As Dictionary(Of String, Double),List as Dictionary(Of String,List(Of String)),Deviation as Dictionary(Of String, Double),Calculation As Dictionary(Of String, Double),Sanctions As Dictionary(Of String, Double),KVP As Dictionary(Of String, Double),Log as ILog) As Boolean");

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
        sb.AppendLine("Imports System");

        countLine += 1;
        sb.AppendLine("Imports MongoDB.Bson");

        countLine += 1;
        sb.AppendLine("Imports MongoDB.Bson.IO");

        countLine += 1;
        sb.AppendLine("Imports MongoDB.Bson.Serialization");

        countLine += 1;
        sb.AppendLine("Public Class InlineFunction");

        countLine += 1;
        sb.AppendLine(
            "Public Shared Function Match(Data As Dictionary(Of String, Object),TTLCounter as Dictionary(Of String,Integer),List as Dictionary(Of String,List(Of String)),KVP As Dictionary(Of String, Double),Log as ILog) As Boolean");

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

    public ParsedRule TranslateFromDotNotation(ParsedRule parsedRule)
    {
        var sb = new StringBuilder();
        var lines = parsedRule.OriginalRuleText.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        for (var i = 0; i < lines.Length; i++)
        {
            var originalLine = lines[i];
            var dictionaryFindReplace = new Dictionary<string, string>();
            var separators = new[] { " ", ",", "=", ">", "(", ")", "<", ">=", "<=", "<>", "not" };
            var specialTokens = new[] { "in", "like" };

            var tokens = lines[i].Split(separators, StringSplitOptions.RemoveEmptyEntries);
            foreach (var t in tokens)
            {
                var replaceString = string.Empty;
                var findString = string.Empty;

                if (specialTokens.Contains(t, StringComparer.OrdinalIgnoreCase)) continue;

                var elements = t.Split(".", StringSplitOptions.RemoveEmptyEntries);

                if (elements.Length <= 0) continue;

                var firstString = elements[0];

                for (var k = 1; k < elements.Length; k++)
                    if (k == 1)
                    {
                        if (string.Equals("payload", firstString, StringComparison.OrdinalIgnoreCase))
                        {
                            findString = firstString + "." + elements[k];

                            var asFunction = "String";
                            var databaseCast = "";
                            var defaultValue = "";
                            if (EntityAnalysisModelRequestXPaths != null)
                            {
                                if (EntityAnalysisModelRequestXPaths.ContainsKey(elements[k]))
                                {
                                    asFunction = EntityAnalysisModelRequestXPaths[elements[k]].DataTypeId switch
                                    {
                                        1 => "String",
                                        2 => "Integer",
                                        3 => "Double",
                                        4 => "DateTime",
                                        5 => "Boolean",
                                        6 => "Double",
                                        7 => "Double",
                                        _ => "String"
                                    };

                                    switch (EntityAnalysisModelRequestXPaths[elements[k]].DataTypeId)
                                    {
                                        case 2:
                                            databaseCast = "::int";
                                            defaultValue = EntityAnalysisModelRequestXPaths[elements[k]]
                                                .DefaultValue;

                                            if (!int.TryParse(defaultValue, out _)) defaultValue = "0";

                                            break;
                                        case 3:
                                            databaseCast = "::float8";
                                            defaultValue = EntityAnalysisModelRequestXPaths[elements[k]]
                                                .DefaultValue;

                                            if (!double.TryParse(defaultValue, out _)) defaultValue = "0";

                                            break;
                                        case 4:
                                            databaseCast = "::timestamp";
                                            defaultValue = EntityAnalysisModelRequestXPaths[elements[k]]
                                                .DefaultValue;

                                            if (!DateTime.TryParse(defaultValue, out _))
                                                defaultValue = "'" + DateTime.Now.ToString("O") + "'";

                                            break;
                                        case 5:
                                            databaseCast = "::boolean";
                                            defaultValue = EntityAnalysisModelRequestXPaths[elements[k]]
                                                .DefaultValue;

                                            if (!bool.TryParse(defaultValue, out _)) defaultValue = "false";

                                            break;
                                        case 6:
                                            databaseCast = "::float8";

                                            if (!double.TryParse(defaultValue, out _))

                                                defaultValue = EntityAnalysisModelRequestXPaths[elements[k]]
                                                    .DefaultValue;
                                            break;
                                        case 7:
                                            databaseCast = "::float8";
                                            defaultValue = EntityAnalysisModelRequestXPaths[elements[k]]
                                                .DefaultValue;

                                            if (!double.TryParse(defaultValue, out _)) defaultValue = "0";

                                            break;
                                        default:
                                            defaultValue = "'" + EntityAnalysisModelRequestXPaths[elements[k]]
                                                .DefaultValue + "'";
                                            break;
                                    }
                                }
                                else
                                {
                                    var errorSpan = new ErrorSpan
                                    {
                                        Message =
                                            $"Line {i + 1}: Request XPath does not exist for {elements[k]}.",
                                        Line = i
                                    };
                                    parsedRule.ErrorSpans.Add(errorSpan);

                                    asFunction = "String";
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

                            replaceString = replaceString + "CType(Data(\"" + elements[k] + "\")," +
                                            asFunction + ")";
                        }
                        else if (string.Equals("TTLCounter", firstString,
                                     StringComparison.OrdinalIgnoreCase))
                        {
                            findString = firstString + "." + elements[k];

                            if (EntityAnalysisModelsTtlCounters != null)
                                if (EntityAnalysisModelsTtlCounters.All(w => w != elements[k]))
                                {
                                    var errorSpan = new ErrorSpan
                                    {
                                        Message =
                                            $"Line {i + 1}: TTL Counter does not exist for {elements[k]}.",
                                        Line = i
                                    };
                                    parsedRule.ErrorSpans.Add(errorSpan);
                                }

                            replaceString = replaceString + "TTLCounter(\"" + elements[k] + "\")";
                        }
                        else if (string.Equals("abstraction", firstString,
                                     StringComparison.OrdinalIgnoreCase))
                        {
                            findString = firstString + "." + elements[k];

                            if (EntityAnalysisModelsAbstractionRule != null)
                                if (EntityAnalysisModelsAbstractionRule.All(w => w != elements[k]))
                                {
                                    var errorSpan = new ErrorSpan
                                    {
                                        Message =
                                            $"Line {i + 1}: Abstraction does not exist for {elements[k]}.",
                                        Line = i
                                    };
                                    parsedRule.ErrorSpans.Add(errorSpan);
                                }

                            replaceString = replaceString + "Abstraction(\"" + elements[k] +
                                            "\")";
                        }
                        else if (string.Equals("dictionary", firstString,
                                     StringComparison.OrdinalIgnoreCase))
                        {
                            findString = firstString + "." + elements[k];

                            if (EntityAnalysisModelsDictionaries != null)
                                if (EntityAnalysisModelsDictionaries.All(w => w != elements[k]))
                                {
                                    var errorSpan = new ErrorSpan
                                    {
                                        Message =
                                            $"Line {i + 1}: Dictionary does not exist for {elements[k]}.",
                                        Line = i
                                    };
                                    parsedRule.ErrorSpans.Add(errorSpan);
                                }

                            replaceString = replaceString + "KVP(\"" + elements[k] + "\")";
                        }
                        else if (string.Equals("Sanction", firstString,
                                     StringComparison.OrdinalIgnoreCase))
                        {
                            findString = firstString + "." + elements[k];

                            if (EntityAnalysisModelsSanctions != null)
                                if (EntityAnalysisModelsSanctions.All(w => w != elements[k]))
                                {
                                    var errorSpan = new ErrorSpan
                                    {
                                        Message =
                                            $"Line {i + 1}: Dictionary does not exist for {elements[k]}.",
                                        Line = i
                                    };
                                    parsedRule.ErrorSpans.Add(errorSpan);
                                }

                            replaceString = replaceString + "Sanctions(\"" + elements[k] + "\")";
                        }
                        else if (string.Equals("abstractionCalculation", firstString,
                                     StringComparison.OrdinalIgnoreCase))
                        {
                            findString = firstString + "." + elements[k];

                            if (EntityAnalysisModelAbstractionCalculations != null)
                                if (EntityAnalysisModelAbstractionCalculations.All(w => w != elements[k]))
                                {
                                    var errorSpan = new ErrorSpan
                                    {
                                        Message =
                                            $"Line {i + 1}: Dictionary does not exist for {elements[k]}.",
                                        Line = i
                                    };
                                    parsedRule.ErrorSpans.Add(errorSpan);
                                }

                            replaceString = replaceString + "Calculation(\"" + elements[k] + "\")";
                        }
                        else if (string.Equals("adaptation", firstString,
                                     StringComparison.OrdinalIgnoreCase))
                        {
                            findString = firstString + "." + elements[k];

                            if (EntityAnalysisModelsAdaptations != null)
                                if (EntityAnalysisModelsAdaptations.All(w => w != elements[k]))
                                {
                                    var errorSpan = new ErrorSpan
                                    {
                                        Message =
                                            $"Line {i + 1}: Dictionary does not exist for {elements[k]}.",
                                        Line = i
                                    };
                                    parsedRule.ErrorSpans.Add(errorSpan);
                                }

                            replaceString = replaceString + "Adaptation(\"" + elements[k] + "\")";
                        }
                        else if (string.Equals("list", firstString,
                                     StringComparison.OrdinalIgnoreCase))
                        {
                            findString = firstString + "." + elements[k];

                            if (EntityAnalysisModelsLists != null)
                                if (EntityAnalysisModelsLists.All(w => w != elements[k]))
                                {
                                    var errorSpan = new ErrorSpan
                                    {
                                        Message =
                                            $"Line {i + 1}: Dictionary does not exist for {elements[k]}.",
                                        Line = i
                                    };
                                    parsedRule.ErrorSpans.Add(errorSpan);
                                }

                            replaceString = replaceString + "List(\"" + elements[k] + "\")";
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

                if (string.IsNullOrEmpty(findString)) continue;

                if (!dictionaryFindReplace.ContainsKey(findString))
                    dictionaryFindReplace.Add(elements[0] + "." + elements[1], replaceString);
            }

            var newLine = dictionaryFindReplace.Aggregate(originalLine,
                (current, kvp) => current.Replace(kvp.Key, kvp.Value));
            sb.AppendLine(newLine);
        }

        parsedRule.ParsedRuleText = sb.ToString();
        return parsedRule;
    }
}