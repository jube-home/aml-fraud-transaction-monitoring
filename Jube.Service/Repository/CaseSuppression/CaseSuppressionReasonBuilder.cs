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

using System.Globalization;
using Jube.Service.Repository.CaseSuppression.Models;

namespace Jube.Service.Repository.CaseSuppression
{
    public static class CaseSuppressionReasonBuilder
    {
        public const int MaximumNamedTargets = 5;
        private const string DateFormat = "d MMM yyyy";

        public static string Build(CaseSuppressionReasonFacts facts)
        {
            var verb = facts is { AnyForce: true, AnySuppress: false } ? "Force"
                : facts is { AnyForce: true, AnySuppress: true } ? "Suppress or force"
                : "Suppress";

            var targets = facts.Targets.Count <= MaximumNamedTargets
                ? string.Join(", ", facts.Targets)
                : string.Join(", ", facts.Targets.Take(MaximumNamedTargets)) +
                  $" and {facts.Targets.Count - MaximumNamedTargets} more";

            var seen = facts.CountInCase == 1 ? "1 transaction" : $"{facts.CountInCase} transactions";
            var range = facts.FirstSeen.HasValue && facts.LastSeen.HasValue
                ? facts.FirstSeen.Value.Date == facts.LastSeen.Value.Date
                    ? $", on {Format(facts.FirstSeen.Value)}"
                    : $", {Format(facts.FirstSeen.Value)} to {Format(facts.LastSeen.Value)}"
                : string.Empty;

            var caseText = string.IsNullOrWhiteSpace(facts.CaseKey)
                ? $"C-{facts.CaseId}"
                : $"C-{facts.CaseId} ({facts.CaseKey} {facts.CaseKeyValue})";

            var text = $"{verb} {(targets.Length == 0 ? "the chosen models and rules" : targets)} for " +
                       $"{facts.OverrideKey} {facts.OverrideKeyValue}: seen in {seen} on {caseText}{range}; ";

            if (!facts.WiderKnown)
            {
                text += "the wider impact is unknown because the archive is not indexed for this key yet";
            }
            else
            {
                var others = Math.Max(0, facts.WiderKeys - 1);
                var capped = facts.WiderCapped ? "+" : string.Empty;
                var keys = others == 1 ? "1 other case key carries" : $"{others}{capped} other case keys carry";
                var archive = facts.WiderTransactions == 1
                    ? "1 transaction"
                    : $"{facts.WiderTransactions.ToString("N0", CultureInfo.InvariantCulture)}{capped} transactions";

                text += $"{keys} it ({archive} in the model archive)";
            }

            text += facts.Expiry.HasValue ? $". Expires {Format(facts.Expiry.Value)}." : ".";

            return text;
        }

        private static string Format(DateTime value)
        {
            return value.ToString(DateFormat, CultureInfo.InvariantCulture);
        }
    }
}
