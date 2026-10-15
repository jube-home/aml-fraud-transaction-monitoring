/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using AgentGuard.Core.Abstractions;
using AgentGuard.Core.Builders;
using AgentGuard.Core.Guardrails;
using AgentGuard.Core.Ledger;
using AgentGuard.Core.Rules.Secrets;
using AgentGuard.Pii;
using Jube.AskJooby.Security.Models;
using Microsoft.Extensions.Logging.Abstractions;
using TasmanianDevil;

namespace Jube.Service.AskJoobyMemory
{
    public static class AskJoobySensitiveDataGuard
    {
        private const string PipelineName = "askjooby-memory-sensitive-data";

        private static readonly GuardrailPipeline pipeline = new(
            new GuardrailPolicyBuilder(PipelineName)
                .DetectSecrets(new SecretsDetectionOptions
                {
                    Categories = SecretCategory.All,
                    Action = SecretAction.Block
                })
                .RedactPii(new PiiOptions
                {
                    Countries = [PiiCountries.Uk, PiiCountries.De, PiiCountries.In, PiiCountries.It, PiiCountries.Es]
                })
                .Build(),
            NullLogger<GuardrailPipeline>.Instance,
            new HashChainLedger(jsonlFilePath: null));

        public static async Task<AskJoobyGuardFinding?> CheckAsync(string content)
        {
            var result = await pipeline.RunAsync(new GuardrailContext
            {
                Text = content,
                Phase = GuardrailPhase.Output
            }).ConfigureAwait(false);

            if (result.IsBlocked)
            {
                var rule = result.BlockingResult?.RuleName ?? "sensitive data";

                var message = $"Not remembered: AgentGuard's '{rule}' rule found sensitive data. Keep personal " +
                              "or secret details out of memories and remember the general point instead.";

                return new AskJoobyGuardFinding(
                    "Output", PipelineName, rule, result.BlockingResult?.Reason, "Blocked", message);
            }

            if (result.WasModified)
            {
                var message = "Not remembered: AgentGuard's PII rule found personal data. Keep personal details " +
                              "out of memories and remember the general point instead.";

                return new AskJoobyGuardFinding("Output", PipelineName, "pii-redaction", null, "Modified", message,
                    result.FinalText);
            }

            return null;
        }

        public static async Task<string?> FindAsync(string content)
        {
            var finding = await CheckAsync(content).ConfigureAwait(false);

            return finding?.Message;
        }
    }
}