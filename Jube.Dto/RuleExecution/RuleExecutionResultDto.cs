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
using Jube.Dto.Validation;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Jube.Dto.RuleExecution
{
    [Description("The outcome of running a rule against an invocation context exactly as the engine would compile " +
                 "and call it, without storing anything.")]
    public class RuleExecutionResultDto
    {
        [Description("True when the rule text parsed and compiled; when false, Errors says why and nothing ran.")]
        public bool Compiled { get; set; }

        [Description("Parse and compile errors, located in the rule text. Empty when the rule compiled.")]
        public List<ValidationErrorDto> Errors { get; set; } = [];

        [Description("The value the rule returned, as invariant text: true/false for gateway, abstraction, " +
                     "activation and reprocessing rules, a number for abstraction calculations, and the returned " +
                     "value for inline functions. Null when the rule did not complete.")]
        public string? Result { get; set; }

        [Description("The .NET type of the returned value, e.g. Boolean, Double, String; null when none.")]
        public string? ResultType { get; set; }

        [Description("The error the rule raised while running, when it did; otherwise null.")]
        public string? RuntimeError { get; set; }

        [Description("True when the rule did not finish within the time limit and was abandoned.")]
        public bool TimedOut { get; set; }

        [Description("How long the rule took to run, in microseconds.")]
        public long DurationMicroseconds { get; set; }

        [Description("Completion names the rule reads, e.g. Payload.Amount, TTLCounter.PerAccount.")]
        public List<string> NamesRead { get; set; } = [];

        [Description("Names the rule reads that have no value in the context; an engine reads these as empty or " +
                     "zero, so a result that depends on them may be misleading.")]
        public List<string> UnsetNamesRead { get; set; } = [];

        [Description("How the engine itself behaves for this outcome, e.g. that it catches a runtime error, logs it " +
                     "and treats the rule as not matched.")]
        public string? EngineBehaviour { get; set; }
    }
}