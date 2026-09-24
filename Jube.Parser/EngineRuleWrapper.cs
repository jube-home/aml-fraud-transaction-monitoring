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
    using System.Linq;
    using System.Text;

    public sealed record EngineWrappedRule(string Text, string ClassName, int LineOffset, int CharOffset);

    public static class EngineRuleWrapper
    {
        private static readonly string[] imports =
        [
            "System.IO", "log4net", "System.Net", "System.Collections.Generic", "Jube.Dictionary",
            "Jube.Dictionary.Extensions", "System"
        ];

        public static EngineWrappedRule GatewayRule(string ruleText, bool tryCatch = true)
        {
            return Build(imports, "GatewayRule",
                "Public Shared Function Match(Data As DictionaryNoBoxing(Of String),List As Dictionary(Of String, List(Of String)),KVP As PooledDictionary(Of String, Double),Log as ILog) As Boolean",
                "Dim Matched as Boolean", ruleText, tryCatch);
        }

        public static EngineWrappedRule AbstractionRule(string ruleText, bool tryCatch = true)
        {
            return Build(imports, "AbstractionRule",
                "Public Shared Function Match(Data As DictionaryNoBoxing(Of String),List as Dictionary(Of String,List(Of String)),KVP as PooledDictionary(of String,Double),Log as ILog) As Boolean",
                "Dim Matched as Boolean", ruleText, tryCatch);
        }

        public static EngineWrappedRule ActivationRule(string ruleText, bool tryCatch = true)
        {
            return Build([.. imports, "Jube.HttpAdaptationProtocol"], "ActivationRule",
                "Public Shared Function Match(Data As DictionaryNoBoxing(Of String),TTLCounter As PooledDictionary(Of String, Double),Abstraction As PooledDictionary(Of string,double),HttpAdaptation As PooledDictionary(Of String, Adaptation),ExhaustiveAdaptation As PooledDictionary(Of String, Double),List as Dictionary(Of String,List(Of String)),Calculation As PooledDictionary(Of String, Double),Sanctions As PooledDictionary(Of String, Double),KVP As PooledDictionary(Of String, Double),Activation as ICollection(Of String),Log as ILog) As Boolean",
                "Dim Matched as Boolean", ruleText, tryCatch);
        }

        public static EngineWrappedRule InlineFunction(string ruleText, bool tryCatch = true)
        {
            return Build(imports, "InlineFunction",
                "Public Shared Function Match(Data As DictionaryNoBoxing(Of String),List As Dictionary(Of String, List(Of String)),KVP As PooledDictionary(Of String, Double),Log as ILog) As Object",
                "Dim Matched as Object = Nothing", ruleText, tryCatch);
        }

        public static EngineWrappedRule ReprocessingRule(string ruleText, bool tryCatch = true)
        {
            return Build(imports, "GatewayRule",
                "Public Shared Function Match(Data As DictionaryNoBoxing(Of String), List As Dictionary(Of String, List(Of String)),KVP As PooledDictionary(Of String, DictionaryNoBoxing(Of String)),Log As ILog) As Boolean",
                "Dim Matched As Boolean", ruleText, tryCatch);
        }

        private static EngineWrappedRule Build(string[] importNames, string className, string signature,
            string declaration, string ruleText, bool tryCatch)
        {
            var sb = new StringBuilder();
            foreach (var name in importNames)
            {
                sb.Append("Imports ").Append(name).Append("\r\n");
            }

            sb.Append("Public Class ").Append(className).Append("\r\n");
            sb.Append(signature).Append("\r\n");
            sb.Append(declaration).Append("\r\n");
            if (tryCatch)
            {
                sb.Append("Try\r\n");
            }

            var charOffset = sb.Length;
            var lineOffset = sb.ToString().Count(c => c == '\n');
            sb.Append(ruleText).Append("\r\n");
            if (tryCatch)
            {
                sb.Append("Catch ex As Exception\r\n");
                sb.Append("Log.Info(ex.ToString)\r\n");
                sb.Append("End Try\r\n");
            }

            sb.Append("Return Matched\r\n");
            sb.Append("\r\n");
            sb.Append("End Function\r\n");
            sb.Append("End Class\r\n");
            return new EngineWrappedRule(sb.ToString(), className, lineOffset, charOffset);
        }
    }
}