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

using System.ComponentModel;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.EntityAnalysisModelBacktest
{
    [Description("A backtest: which archived transactions to run over (the filter), the rule to test and what " +
                 "counts as positive (the class), each written as query builder JSON or rule text.")]
    [MessagePackObject]
    public class BacktestRequestDto
    {
        [Description("The model whose archived transactions the backtest runs over.")]
        [Key(0)]
        public int EntityAnalysisModelId { get; set; }

        [Description("The kind of rule: GatewayRule, ActivationRule or ReprocessingRule.")]
        [Key(1)]
        public string RuleType { get; set; } = "ActivationRule";

        [Description("The saved rule the backtest was started from, if any; instances are listed against it.")]
        [Key(2)]
        public int? RuleId { get; set; }

        [Description("The rule text to test, exactly as it would be saved in CoderRuleScript or BuilderRuleScript.")]
        [Key(3)]
        public string? RuleText { get; set; }

        [Description("Query builder JSON choosing which archived transactions to run over, using the fields of a " +
                     "reprocessing rule (EntityAnalysisModelBacktestFields FilterFields); empty runs over every " +
                     "transaction.")]
        [Key(4)]
        public string? FilterJson { get; set; }

        [Description("Instead of FilterJson, reprocessing rule text choosing the transactions; it wins when both " +
                     "are given.")]
        [Key(5)]
        public string? FilterRuleText { get; set; }

        [Description("Query builder JSON defining a positive transaction, using the class fields " +
                     "(EntityAnalysisModelBacktestFields ClassFields), which include Tag.<Name> for each of the " +
                     "model's tags, e.g. Tag.Fraud = True. Empty counts only firing.")]
        [Key(6)]
        public string? ClassJson { get; set; }

        [Description("Only transactions with a reference date at or after this instant; use the time window tools.")]
        [Key(7)]
        public DateTime? From { get; set; }

        [Description("Only transactions with a reference date at or before this instant.")]
        [Key(8)]
        public DateTime? To { get; set; }

        [Description("The most archived transactions to read, newest first; capped by BacktestOnlineMaxRows when " +
                     "run immediately and by BacktestMaxRows when submitted.")]
        [Key(9)]
        public long Limit { get; set; } = 1000;

        [Description("How many example transactions to return for each outcome; at most 50.")]
        [Key(10)]
        public int SampleSize { get; set; } = 10;
    }
}