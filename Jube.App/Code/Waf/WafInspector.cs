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
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Jube.App.Code.Waf.Models;

namespace Jube.App.Code.Waf
{
    public sealed class WafInspector(WafRegistry registry, DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
    {
        private const int MaxMatchedValueLength = 512;
        private const int DefaultMaxInspectionMilliseconds = 200;

        public WafInspectionResult Inspect(WafInspectionRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var ruleSet = registry.Current;
            if (ruleSet.Signatures.Count == 0 || request.Fields.Count == 0)
            {
                return WafInspectionResult.None;
            }

            List<WafMatch> matches = null;
            var blocked = false;
            var budget = MaxInspectionMilliseconds();
            var elapsed = Stopwatch.StartNew();

            foreach (var field in request.Fields)
            {
                if (string.IsNullOrEmpty(field.Value))
                {
                    continue;
                }

                foreach (var signature in ruleSet.Signatures)
                {
                    if (elapsed.ElapsedMilliseconds > budget)
                    {
                        return matches == null ? WafInspectionResult.None : new WafInspectionResult(blocked, matches);
                    }

                    if ((signature.Scope & field.Scope) == 0)
                    {
                        continue;
                    }

                    if (!SafeMatch(signature.Pattern, field.Value))
                    {
                        continue;
                    }

                    if (IsSuppressed(ruleSet, request.Route, field.Field, signature.Id))
                    {
                        continue;
                    }

                    matches ??= [];
                    var match = new WafMatch(signature.Id, signature.Name, signature.Category, field.Field,
                        Truncate(field.Value), signature.Drop);
                    matches.Add(match);
                    blocked |= signature.Drop;

                    WafAttackCapture.Enqueue(new WafAttackCaptureEntry
                    {
                        CreatedDate = DateTime.UtcNow,
                        Transport = request.Transport,
                        Route = request.Route,
                        Method = request.Method,
                        RemoteIp = request.RemoteIp,
                        UserName = request.UserName,
                        SignatureId = signature.Id,
                        SignatureName = signature.Name,
                        Category = signature.Category,
                        MatchedField = field.Field,
                        MatchedValue = match.Value,
                        Action = signature.Drop ? "Dropped" : "Detected",
                        CorrelationId = request.CorrelationId
                    });
                }
            }

            return matches == null ? WafInspectionResult.None : new WafInspectionResult(blocked, matches);
        }

        private static bool IsSuppressed(WafRuleSet ruleSet, string route, string field, int signatureId)
        {
            foreach (var exception in ruleSet.Exceptions)
            {
                if (exception.Suppresses(route, field, signatureId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool SafeMatch(Regex regex, string input)
        {
            try
            {
                return regex.IsMatch(input);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        private static string Truncate(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= MaxMatchedValueLength)
            {
                return value;
            }

            return value[..MaxMatchedValueLength];
        }

        private int MaxInspectionMilliseconds()
        {
            return int.TryParse(dynamicEnvironment.AppSettings("WafMaxInspectionMilliseconds"), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var value) && value > 0
                ? value
                : DefaultMaxInspectionMilliseconds;
        }
    }
}