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


using Jube.Dto.Query.CaseKeyJournal;
using Newtonsoft.Json.Linq;

namespace Jube.Service.Query.CaseKeyJournal
{
    public static class JournalActivationCards
    {
        public const int TopDrivers = 4;

        private const string RuleKind = "rule";
        private const string ModelKind = "model";

        private static readonly Dictionary<string, (string Facet, string Group)> facets =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Payload"] = ("payload", "payload"),
                ["TTLCounter"] = ("ttlCounter", "counter"),
                ["Abstraction"] = ("abstraction", "abstraction"),
                ["AbstractionCalculation"] = ("abstractionCalculation", "calculation"),
                ["Dictionary"] = ("dictionary", "dictionary"),
                ["Sanction"] = ("sanction", "sanction")
            };

        public static List<JournalActivationCardDto> Build(IEnumerable<JournalActivationRow> rows,
            ISet<string> visibleNames, IReadOnlyDictionary<string, IReadOnlyList<JournalRuleInput>> ruleInputs)
        {
            ArgumentNullException.ThrowIfNull(rows);
            ArgumentNullException.ThrowIfNull(visibleNames);
            ArgumentNullException.ThrowIfNull(ruleInputs);

            var rules = new Dictionary<string, Rule>(StringComparer.Ordinal);
            var models = new Dictionary<(string Source, string Name), Model>();

            foreach (var row in rows.OrderByDescending(r => r.Id))
            {
                foreach (var activation in JournalTransactionReader.Activations(row.Json))
                {
                    if (!rules.TryGetValue(activation.Name, out var rule))
                    {
                        rule = new Rule(row, activation.Forced, activation.Shadow);
                        rules[activation.Name] = rule;
                    }

                    rule.Fired++;
                }

                foreach (var adaptation in JournalTransactionReader.Adaptations(row.Json, visibleNames))
                {
                    if (adaptation.Result?.Activated != true)
                    {
                        continue;
                    }

                    var key = (adaptation.Source, adaptation.Name);

                    if (!models.TryGetValue(key, out var model))
                    {
                        model = new Model(row, adaptation);
                        models[key] = model;
                    }

                    model.Fired++;
                }
            }

            var cards = models.Values.Select(Card).OrderByDescending(card => card.Value ?? 0)
                .ThenBy(card => card.Name, StringComparer.Ordinal).ToList();

            cards.AddRange(rules.Select(pair => Card(pair.Key, pair.Value, visibleNames,
                    ruleInputs.GetValueOrDefault(pair.Key) ?? []))
                .OrderByDescending(card => card.Fired).ThenBy(card => card.Name, StringComparer.Ordinal));

            return cards;
        }

        public static (List<JournalActivationDriverDto> Drivers, int Other) DriversOf(JournalAdaptationDto adaptation)
        {
            ArgumentNullException.ThrowIfNull(adaptation);

            var visible = (adaptation.Contribution?.Items ?? [])
                .Where(item => !item.Hidden && item.Signed.HasValue
                                            && !string.IsNullOrEmpty(item.HumanLabel ?? item.Name))
                .ToList();

            var top = visible.OrderByDescending(item => Math.Abs(item.Signed!.Value)).Take(TopDrivers).ToList();

            if (top.Count == TopDrivers && top.All(item => item.Signed > 0)
                && visible.Where(item => item.Signed < 0).OrderByDescending(item => Math.Abs(item.Signed!.Value))
                    .FirstOrDefault() is { } negative)
            {
                top[^1] = negative;
            }

            return (top.OrderByDescending(item => Math.Abs(item.Signed!.Value)).Select(item =>
                new JournalActivationDriverDto { Label = item.HumanLabel ?? item.Name!, Signed = item.Signed!.Value })
                .ToList(), visible.Count - top.Count);
        }

        private static JournalActivationCardDto Card(Model model)
        {
            var adaptation = model.Adaptation;
            var card = new JournalActivationCardDto
            {
                Kind = ModelKind,
                Source = adaptation.Source,
                Name = adaptation.Name,
                HumanLabel = adaptation.HumanLabel,
                Fired = model.Fired,
                AnchorId = model.Row.Id,
                AnchorDate = Utc(model.Row.At),
                Value = adaptation.Value,
                Threshold = adaptation.Result?.Threshold,
                Calibrated = JournalScoreBands.IsCalibratedProbability(adaptation),
                BandId = (int?)JournalScoreBands.Of(adaptation),
                ModelName = adaptation.Model?.Name,
                ModelVersion = adaptation.Model?.Version
            };

            if (card.Calibrated && JournalScoreBands.Holding(adaptation) is { } band)
            {
                card.ObservedRate = band.Observed;
                card.ExpectedRate = band.Expected;
                card.BandSample = band.Count;
            }

            var (drivers, other) = DriversOf(adaptation);

            card.Drivers = drivers;
            card.OtherDrivers = other;

            return card;
        }

        private static JournalActivationCardDto Card(string name, Rule rule, ISet<string> visibleNames,
            IReadOnlyList<JournalRuleInput> inputs)
        {
            var card = new JournalActivationCardDto
            {
                Kind = RuleKind,
                Name = name,
                Fired = rule.Fired,
                Forced = rule.Forced,
                Shadow = rule.Shadow,
                AnchorId = rule.Row.Id,
                AnchorDate = Utc(rule.Row.At)
            };

            foreach (var input in inputs.DistinctBy(i => (i.Namespace.ToUpperInvariant(), i.Name)))
            {
                if (!facets.TryGetValue(input.Namespace, out var facet))
                {
                    continue;
                }

                if (facet.Group == "payload" && !visibleNames.Contains(input.Name))
                {
                    card.HiddenPath++;

                    continue;
                }

                if (rule.Row.Json[facet.Facet] is JObject source
                    && source.GetValue(input.Name, StringComparison.OrdinalIgnoreCase) is
                    { Type: JTokenType.Integer or JTokenType.Float or JTokenType.Boolean or JTokenType.String
                        or JTokenType.Date } token)
                {
                    card.Path.Add(new JournalActivationPathDto
                    {
                        Group = facet.Group, Name = input.Name, Value = JournalLensReader.Typed(token)
                    });
                }
            }

            return card;
        }

        private static DateTimeOffset? Utc(DateTime? at)
        {
            return at is null ? null : new DateTimeOffset(DateTime.SpecifyKind(at.Value, DateTimeKind.Utc));
        }

        private sealed class Rule(JournalActivationRow row, bool forced, bool shadow)
        {
            public JournalActivationRow Row { get; } = row;
            public bool Forced { get; } = forced;
            public bool Shadow { get; } = shadow;
            public int Fired { get; set; }
        }

        private sealed class Model(JournalActivationRow row, JournalAdaptationDto adaptation)
        {
            public JournalActivationRow Row { get; } = row;
            public JournalAdaptationDto Adaptation { get; } = adaptation;
            public int Fired { get; set; }
        }
    }
}
