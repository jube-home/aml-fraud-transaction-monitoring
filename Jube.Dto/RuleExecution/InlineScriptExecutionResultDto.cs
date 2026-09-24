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
using Jube.Dto.Query.EntityAnalysisModelInvocationContext;
using Jube.Dto.Validation;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Jube.Dto.RuleExecution
{
    [Description("The outcome of running an inline script against an invocation context outside the engine: the " +
                 "public properties it would add to the payload, or why it did not run.")]
    public class InlineScriptExecutionResultDto
    {
        [Description("True when the script compiled and exposes a class implementing IInlineScript.")]
        public bool Compiled { get; set; }

        [Description("Why the script did not run: disabled by configuration, not found, or compile errors.")]
        public List<ValidationErrorDto> Errors { get; set; } = [];

        [Description("True when the script's ExecuteAsync returned true, so the engine would use its properties.")]
        public bool Succeeded { get; set; }

        [Description("The public properties the engine would add to the payload, by completion name (Payload.X), " +
                     "ready to set in a context with the Overlay operation.")]
        public List<InvocationContextValueDto> Properties { get; set; } = [];

        [Description("The error the script raised while running, when it did; otherwise null.")]
        public string? RuntimeError { get; set; }

        [Description("True when the script did not finish within the time limit and was abandoned.")]
        public bool TimedOut { get; set; }

        [Description("How long the script took to run, in microseconds.")]
        public long DurationMicroseconds { get; set; }

        [Description("How the engine itself behaves for this outcome.")]
        public string? EngineBehaviour { get; set; }
    }
}