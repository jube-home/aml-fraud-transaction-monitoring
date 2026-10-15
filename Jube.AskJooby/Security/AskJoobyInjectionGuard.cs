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
using AgentGuard.Core.Rules.LLM;
using AgentGuard.Core.Rules.Secrets;
using AgentGuard.Onnx;
using Jube.AskJooby.Security.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jube.AskJooby.Security
{
    public static class AskJoobyInjectionGuard
    {
        private const string PipelineName = "askjooby-input-injection";

        private static readonly string[] allowedTopics =
        [
            "Operating, configuring, authoring, training, evaluating or querying anything on " +
            "Jube's fraud and AML transaction-monitoring platform -- rules of every kind " +
            "(activation, abstraction, gateway, reprocessing, overrides), lists, dictionaries, " +
            "counters, models and their training or evaluation (Advanced Analytics, Exhaustive " +
            "search, R scripts run in the platform's own sandbox), cases and case workflows, " +
            "users, roles and permissions, visualisations and dashboards, and the platform's own " +
            "documentation, database schema and codebase -- including a request that uses only " +
            "generic business, statistical or technical vocabulary (a country, a threshold, a " +
            "classifier family, an algorithm) and names no Jube-specific term at all, since " +
            "ordinary platform work routinely reads exactly like that in isolation",
            "the account, transaction, and case data AskJooby reasons about on this platform",
            "AskJooby's own procedural narration about how it is approaching a request on this " +
            "platform -- what it already checked, why, what it still needs before it can act, or " +
            "what it is about to do next -- even when that sentence alone names no specific rule, " +
            "transaction or table, since this is how the agent routinely talks about its own work " +
            "rather than content unrelated to the platform"
        ];

        private const string PlatformOperationsTopic =
            "this platform's own live operational infrastructure -- Postgres, Redis, Docker, etcd " +
            "and Patroni health, metrics, logs and administration -- reachable only by a Landlord, " +
            "since every tool over it is Landlord-gated";

        private static readonly string[] allowedTopicsForLandlord = [.. allowedTopics, PlatformOperationsTopic];
        private const string OutputPipelineName = "askjooby-output-guard";

        private const string OutputTopicBoundarySystemPrompt =
            "You are a topic classification system. Your task is to determine whether an AI agent's " +
            "outgoing reply falls within the allowed topics for this agent, considering the full " +
            "conversation context when one is given.\n\n" +
            "Allowed topics: {topics}\n\n" +
            "Interpret the allowed topics broadly - if the reply is reasonably related to any allowed " +
            "topic, it should be considered on-topic. Only flag a reply that is clearly unrelated to " +
            "all allowed topics.\n\n" +
            "Some replies are not conversational prose at all: AskJooby runs an internal validator " +
            "agent that checks another agent's draft reply against Jube's own rules and data, and " +
            "answers strictly in the terse form \"STATUS: PASSED\" or \"STATUS: FAILED\\nREASON: " +
            "...\\nKIND: OMISSION\" or \"ERROR\". Treat a reply in that STATUS/REASON/KIND shape as " +
            "on-topic whenever it concerns Jube's own rules, configuration, documentation, schema, " +
            "codebase, or the account/transaction/case data above -- including a bare \"STATUS: " +
            "PASSED\" with no REASON at all, which is the normal, correct shape of a clean pass and " +
            "never evidence of being off-topic by itself.\n\n" +
            "Important: evaluate the reply in the context of the conversation history when one is " +
            "given.\n\n{history}\n\n" +
            "Respond with exactly one word:\n" +
            "- ON_TOPIC if the reply is related to any of the allowed topics\n" +
            "- OFF_TOPIC if the reply is clearly unrelated to all allowed topics\n\n" +
            "Do not explain your reasoning. Respond with only ON_TOPIC or OFF_TOPIC.";

        private const string InputTopicBoundarySystemPrompt =
            "You are a topic classification system. Your task is to determine whether a caller's " +
            "incoming message to an AI agent falls within the allowed topics for this agent.\n\n" +
            "Allowed topics: {topics}\n\n" +
            "Interpret the allowed topics broadly - if the message is reasonably related to any " +
            "allowed topic, or could plausibly be a step toward platform work (even phrased only in " +
            "generic business, statistical or technical vocabulary, naming no Jube-specific term at " +
            "all), it is on-topic. Only flag a message that is clearly asking for something with no " +
            "plausible connection to any allowed topic -- for example content, instructions or advice " +
            "wholly unrelated to this platform, regardless of how it is phrased.\n\n" +
            "Respond with exactly one word:\n" +
            "- ON_TOPIC if the message is related to, or could plausibly lead toward, any of the " +
            "allowed topics\n" +
            "- OFF_TOPIC if the message is clearly unrelated to all allowed topics\n\n" +
            "Do not explain your reasoning. Respond with only ON_TOPIC or OFF_TOPIC.";

        private const string TopicBoundaryInputRuleId = "TopicBoundaryInput";
        private const string TopicBoundaryOutputRuleId = "TopicBoundaryOutput";
        private const string ToolContentRuleId = "ToolContentScreening";
        private const string ChatInputPromptInjectionRuleId = "ChatInputPromptInjection";
        private const string ContentPipelineName = "askjooby-tool-content-guard";

        private static AskJoobyInjectionGuardSettings settings = new();

        public static void Configure(AskJoobyInjectionGuardSettings newSettings)
        {
            settings = newSettings;
        }

        private static readonly GuardrailPipeline contentPipeline = BuildContentPipeline();

        private static GuardrailPipeline BuildContentPipeline()
        {
            var builder = new GuardrailPolicyBuilder(ContentPipelineName)
                .NormalizeInput()
                .BlockPromptInjection()
                .BlockPromptInjectionWithDefender()
                .DetectSecrets(new SecretsDetectionOptions
                {
                    Categories = SecretCategory.All,
                    Action = SecretAction.Block
                });

            builder = AddOpirIfConfigured(builder);

            return new GuardrailPipeline(builder.Build(), NullLogger<GuardrailPipeline>.Instance,
                new HashChainLedger(jsonlFilePath: null));
        }

        private static GuardrailPipeline BuildPipeline(
            IChatClient? topicBoundaryJudge, string[] topics, float? promptInjectionMainThreshold)
        {
            var builder = new GuardrailPolicyBuilder(PipelineName)
                .NormalizeInput()
                .BlockPromptInjection();

            if (promptInjectionMainThreshold is { } mainThreshold)
            {
                builder = builder.BlockPromptInjectionWithDefender(mainThreshold);
            }

            builder = builder.DetectSecrets(new SecretsDetectionOptions
            {
                Categories = SecretCategory.All,
                Action = SecretAction.Block
            });

            if (topicBoundaryJudge is not null)
            {
                builder = builder.EnforceTopicBoundaryWithLlm(topicBoundaryJudge, new LlmTopicGuardrailOptions
                {
                    AllowedTopics = topics,
                    SystemPrompt = InputTopicBoundarySystemPrompt
                });
            }

            builder = AddOpirIfConfigured(builder);

            return new GuardrailPipeline(builder.Build(), NullLogger<GuardrailPipeline>.Instance,
                new HashChainLedger(jsonlFilePath: null));
        }

        private static GuardrailPipeline BuildOutputPipeline(IChatClient? topicBoundaryJudge, string[] topics)
        {
            var builder = new GuardrailPolicyBuilder(OutputPipelineName);

            if (topicBoundaryJudge is not null)
            {
                builder = builder.EnforceTopicBoundaryWithLlm(topicBoundaryJudge, new LlmTopicGuardrailOptions
                {
                    AllowedTopics = topics,
                    SystemPrompt = OutputTopicBoundarySystemPrompt
                });
            }

            builder = AddOpirIfConfigured(builder);

            return new GuardrailPipeline(builder.Build(), NullLogger<GuardrailPipeline>.Instance,
                new HashChainLedger(jsonlFilePath: null));
        }

        private static GuardrailPolicyBuilder AddOpirIfConfigured(GuardrailPolicyBuilder builder)
        {
            var modelPath = Environment.GetEnvironmentVariable("AGENTGUARD_OPIR_MODEL_PATH");
            var tokenizerPath = Environment.GetEnvironmentVariable("AGENTGUARD_OPIR_TOKENIZER_PATH");
            var prefixPath = Environment.GetEnvironmentVariable("AGENTGUARD_OPIR_PREFIX_PATH");

            if (string.IsNullOrWhiteSpace(modelPath) || string.IsNullOrWhiteSpace(tokenizerPath)
                                                     || string.IsNullOrWhiteSpace(prefixPath) || !File.Exists(modelPath)
                                                     || !File.Exists(tokenizerPath) || !File.Exists(prefixPath))
            {
                return builder;
            }

            return builder.BlockUnsafeContentWithOpir(modelPath, tokenizerPath, prefixPath);
        }

        private static (bool Enabled, string[] Topics) ResolveRule(
            IReadOnlyList<AgentGuardRuleSetting>? rules, string ruleId, bool isLandlord, bool enabledByDefault)
        {
            var row = rules?.FirstOrDefault(rule => rule.RuleId == ruleId);

            var enabled = row?.Enabled ?? enabledByDefault;

            if (string.IsNullOrWhiteSpace(row?.GuidanceText))
            {
                return (enabled, isLandlord ? allowedTopicsForLandlord : allowedTopics);
            }

            string[] topics = isLandlord ? [row.GuidanceText, PlatformOperationsTopic] : [row.GuidanceText];

            return (enabled, topics);
        }

        private static float? ResolveChatInputPromptInjectionMainThreshold(IReadOnlyList<AgentGuardRuleSetting>? rules)
        {
            var row = rules?.FirstOrDefault(rule => rule.RuleId == ChatInputPromptInjectionRuleId);

            return row is { Enabled: false } ? null : settings.ChatInputPromptInjectionMainThreshold;
        }

        public static async Task<AskJoobyGuardFinding?> CheckAsync(
            string content, bool isLandlord = false, IReadOnlyList<AgentGuardRuleSetting>? rules = null)
        {
            var (enabled, topics) = ResolveRule(
                rules, TopicBoundaryInputRuleId, isLandlord, settings.ChatInputTopicBoundaryEnabledByDefault);

            var promptInjectionMainThreshold = ResolveChatInputPromptInjectionMainThreshold(rules);

            var builtPipeline = BuildPipeline(
                enabled ? settings.TopicBoundaryJudge : null, topics, promptInjectionMainThreshold);

            var result = await builtPipeline.RunAsync(new GuardrailContext
            {
                Text = content,
                Phase = GuardrailPhase.Input
            }).ConfigureAwait(false);

            if (!result.IsBlocked)
            {
                return null;
            }

            var rule = result.BlockingResult?.RuleName ?? "prompt-injection rule";
            var message = $"AgentGuard's '{rule}' rule matched {DescribeScreening(rule)}";

            return new AskJoobyGuardFinding(
                "Input", PipelineName, rule, result.BlockingResult?.Reason, "Blocked", message);
        }

        private static string DescribeScreening(string ruleName)
        {
            if (ruleName.Contains("secret", StringComparison.OrdinalIgnoreCase))
            {
                return "secrets screening";
            }

            if (ruleName.Contains("topic", StringComparison.OrdinalIgnoreCase))
            {
                return "topic-boundary screening";
            }

            return "prompt-injection screening";
        }

        public static async Task<string?> FindAsync(
            string content, bool isLandlord = false, IReadOnlyList<AgentGuardRuleSetting>? rules = null)
        {
            var finding = await CheckAsync(content, isLandlord, rules).ConfigureAwait(false);

            return finding?.Message;
        }

        public static async Task<AskJoobyGuardFinding?> CheckToolContentAsync(
            string content, IReadOnlyList<AgentGuardRuleSetting>? rules = null)
        {
            var enabled = rules?.FirstOrDefault(rule => rule.RuleId == ToolContentRuleId)?.Enabled ?? true;

            if (!enabled)
            {
                return null;
            }

            var result = await contentPipeline.RunAsync(new GuardrailContext
            {
                Text = content,
                Phase = GuardrailPhase.Input
            }).ConfigureAwait(false);

            if (!result.IsBlocked)
            {
                return null;
            }

            var rule = result.BlockingResult?.RuleName ?? "prompt-injection rule";
            var message = $"AgentGuard's '{rule}' rule matched {DescribeScreening(rule)}";

            return new AskJoobyGuardFinding(
                "ToolContent", ContentPipelineName, rule, result.BlockingResult?.Reason, "Withheld", message);
        }

        public static async Task<string?> FindInToolContentAsync(
            string content, IReadOnlyList<AgentGuardRuleSetting>? rules = null)
        {
            var finding = await CheckToolContentAsync(content, rules).ConfigureAwait(false);

            return finding?.Message;
        }

        public static async Task<AskJoobyGuardFinding?> CheckOutputAsync(
            string content, bool isLandlord = false, IReadOnlyList<AgentGuardRuleSetting>? rules = null)
        {
            var (enabled, topics) = ResolveRule(rules, TopicBoundaryOutputRuleId, isLandlord, enabledByDefault: true);

            var builtPipeline = BuildOutputPipeline(enabled ? settings.TopicBoundaryJudge : null, topics);

            var result = await builtPipeline.RunAsync(new GuardrailContext
            {
                Text = content,
                Phase = GuardrailPhase.Input
            }).ConfigureAwait(false);

            if (!result.IsBlocked)
            {
                return null;
            }

            var rule = result.BlockingResult?.RuleName ?? "agentguard-rule";

            var message = $"AgentGuard's '{rule}' rule matched topic-boundary or content-safety screening "
                          + "on the model's own reply";

            return new AskJoobyGuardFinding(
                "Output", OutputPipelineName, rule, result.BlockingResult?.Reason, "Blocked", message);
        }
    }
}