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

namespace Jube.Parser
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Compiler;
    using Data.Query.Models;
    using log4net;

    public static class RuleParse
    {
        public const int InlineFunction = 1;
        public const int GatewayRule = 2;
        public const int AbstractionRule = 3;
        public const int AbstractionCalculation = 4;
        public const int ActivationRule = 5;

        public const int MaximumRuleTextLength = 65536;

        public static string ClassName(int ruleParseType)
        {
            return ruleParseType switch
            {
                InlineFunction => "InlineFunction",
                GatewayRule => "GatewayRule",
                AbstractionRule => "GatewayRule",
                AbstractionCalculation => "CalculationRule",
                ActivationRule => "ActivationRule",
                _ => null
            };
        }

        public static string[] DefaultReferences()
        {
            var binaryPath = Path.GetDirectoryName(typeof(RuleParse).Assembly.Location);
            var frameworkPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

            if (binaryPath == null || frameworkPath == null)
            {
                return null;
            }

            return
            [
                Path.Combine(frameworkPath, "mscorlib.dll"), Path.Combine(frameworkPath, "System.dll"),
                Path.Combine(frameworkPath, "Microsoft.VisualBasic.dll"),
                Path.Combine(frameworkPath, "System.Xml.dll"), Path.Combine(binaryPath, "log4net.dll"),
                Path.Combine(binaryPath, "Jube.Dictionary.dll"),
                Path.Combine(frameworkPath, "System.Collections.dll"),
                Path.Combine(binaryPath, "Jube.HttpAdaptationProtocol.dll")
            ];
        }

        public static string[] EngineReferences(int ruleParseType)
        {
            var binaryPath = Path.GetDirectoryName(typeof(RuleParse).Assembly.Location);
            if (binaryPath == null)
            {
                return null;
            }

            var references = new List<string>
            {
                Path.Combine(binaryPath, "log4net.dll"), Path.Combine(binaryPath, "Jube.Dictionary.dll")
            };

            if (ruleParseType == ActivationRule)
            {
                references.Add(Path.Combine(binaryPath, "Jube.HttpAdaptationProtocol.dll"));
            }

            return [.. references];
        }

        private static string WrapForEngine(Parser parser, ParsedRule parsedRule, int ruleParseType,
            bool tryCatchWrap, bool reprocessing)
        {
            if (ruleParseType == AbstractionCalculation)
            {
                parser.WrapAbstractionCalculation(parsedRule, tryCatchWrap);
                return "CalculationRule";
            }

            var wrapped = ruleParseType switch
            {
                InlineFunction => EngineRuleWrapper.InlineFunction(parsedRule.ParsedRuleText, tryCatchWrap),
                GatewayRule when reprocessing =>
                    EngineRuleWrapper.ReprocessingRule(parsedRule.ParsedRuleText, tryCatchWrap),
                GatewayRule => EngineRuleWrapper.GatewayRule(parsedRule.ParsedRuleText, tryCatchWrap),
                AbstractionRule => EngineRuleWrapper.AbstractionRule(parsedRule.ParsedRuleText, tryCatchWrap),
                ActivationRule => EngineRuleWrapper.ActivationRule(parsedRule.ParsedRuleText, tryCatchWrap),
                _ => null
            };

            if (wrapped == null)
            {
                return null;
            }

            parsedRule.ParsedRuleText = wrapped.Text;
            parsedRule.LineOffset = wrapped.LineOffset;
            parsedRule.CharOffset = wrapped.CharOffset;
            return wrapped.ClassName;
        }

        public static RuleParseResult Execute(string ruleText, int ruleParseType,
            RuleParseEnvironmentDto environment, ILog log, string[] references,
            bool tryCatchWrap = false, bool? showOnlyCacheForPayload = null, bool engineWrap = false,
            bool reprocessing = false)
        {
            ArgumentNullException.ThrowIfNull(environment);

            if (ruleText is { Length: > MaximumRuleTextLength })
            {
                return new RuleParseResult { Message = "Error" };
            }

            var parser = new Parser(log, [.. environment.Tokens])
            {
                EntityAnalysisModelRequestXPaths = environment.RequestXPaths.ToDictionary(k => k.Key,
                    v => new EntityAnalysisModelRequestXPath
                    {
                        DataTypeId = v.Value.DataTypeId,
                        DefaultValue = v.Value.DefaultValue,
                        Cache = v.Value.Cache
                    }),
                EntityAnalysisModelInlineScriptProperties = environment.InlineScriptProperties,
                EntityAnalysisModelAbstractionCalculations = environment.AbstractionCalculations,
                EntityAnalysisModelsAbstractionRule = environment.AbstractionRules,
                EntityAnalysisModelsTtlCounters = environment.TtlCounters,
                EntityAnalysisModelsSanctions = environment.Sanctions,
                EntityAnalysisModelsLists = environment.Lists,
                EntityAnalysisModelsDictionaries = environment.Dictionaries,
                EntityAnalysisModelsHttpAdaptations = environment.HttpAdaptations,
                EntityAnalysisModelsExhaustiveAdaptations = environment.ExhaustiveAdaptations,
                EntityAnalysisModelsActivationRules = environment.ActivationRules,
                EntityAnalysisModelsInlineFunctions = environment.InlineFunctions
            };

            var errorSpans = new List<ErrorSpan>();
            var parsedRule = new ParsedRule
            {
                ErrorSpans = errorSpans,
                OriginalRuleText = ruleText
            };
            parsedRule = parser.TranslateFromDotNotation(parsedRule,
                showOnlyCacheForPayload ?? ruleParseType == AbstractionRule);
            parsedRule = parser.Parse(parsedRule);

            var sb = new StringBuilder();
            foreach (var softParseErrorSpan in parsedRule.ErrorSpans)
            {
                sb.AppendLine(softParseErrorSpan.Message);
            }

            var className = ClassName(ruleParseType);
            if (engineWrap)
            {
                className = WrapForEngine(parser, parsedRule, ruleParseType, tryCatchWrap, reprocessing);
            }
            else
            {
                parsedRule = ruleParseType switch
                {
                    InlineFunction => parser.WrapInlineFunction(parsedRule, tryCatchWrap),
                    GatewayRule => parser.WrapGatewayRule(parsedRule, tryCatchWrap),
                    AbstractionRule => parser.WrapAbstractionRule(parsedRule, tryCatchWrap),
                    AbstractionCalculation => parser.WrapAbstractionCalculation(parsedRule, tryCatchWrap),
                    ActivationRule => parser.WrapActivationRule(parsedRule, tryCatchWrap),
                    _ => parsedRule
                };
            }

            byte[] compiledBinary = null;
            if (references != null)
            {
                var compile = new Compile();
                compile.CompileCode(parsedRule.ParsedRuleText, log, references, Compile.Language.Vb, false);

                if (!compile.Success)
                {
                    foreach (var err in compile.Errors)
                    {
                        var line = err.Location.GetLineSpan().StartLinePosition.Line - parsedRule.LineOffset;
                        var message = $"Line {line + 1}: {err.GetMessage()}";
                        sb.AppendLine(message);

                        errorSpans.Add(new ErrorSpan
                        {
                            Message = message,
                            Start = err.Location.SourceSpan.Start - parsedRule.CharOffset,
                            Length = err.Location.SourceSpan.Length,
                            Line = line
                        });
                    }

                    return new RuleParseResult
                    {
                        Message = sb.ToString(),
                        ErrorSpans = errorSpans,
                        ParsedRuleText = parsedRule.ParsedRuleText,
                        References = Distinct(parsedRule.References)
                    };
                }

                compiledBinary = compile.CompiledAssemblyBinary;
            }

            if (errorSpans.Count > 0)
            {
                return new RuleParseResult
                {
                    Message = "Error",
                    ErrorSpans = errorSpans,
                    ParsedRuleText = parsedRule.ParsedRuleText,
                    References = Distinct(parsedRule.References)
                };
            }

            return new RuleParseResult
            {
                Compiled = references != null,
                Message = "Compiled",
                ParsedRuleText = parsedRule.ParsedRuleText,
                ClassName = className,
                CompiledBinary = compiledBinary,
                References = Distinct(parsedRule.References)
            };
        }

        private static List<RuleReference> Distinct(IEnumerable<RuleReference> references)
        {
            return references
                .GroupBy(r => (r.Namespace, r.Name))
                .Select(g => g.OrderBy(r => r.Line).First())
                .ToList();
        }
    }
}