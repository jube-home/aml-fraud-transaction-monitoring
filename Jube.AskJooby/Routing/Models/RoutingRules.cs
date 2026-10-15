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

using System.Text.RegularExpressions;

namespace Jube.AskJooby.Routing.Models
{
    public static class RoutingRules
    {
        public const double EmbeddingConfidenceThreshold = 0.80;

        public static readonly IReadOnlyList<RoutingRule> All =
        [
            new(
                "model-admin",
                [],
                [
                    "Show me an entity analysis model",
                    "What activation rules are configured",
                    "Add a gateway rule that blocks transactions over ten thousand",
                    "How is the request payload mapped for this model",
                    "Set up a TTL counter on card number",
                    "What abstraction rules does the fraud model have"
                ],
                "Authoring Jube's own rule-based detection model (an entity analysis model) and the " +
                "material it is built from: the request payload contract and XPath mapping, abstraction " +
                "rules and calculations, gateway and activation rules, TTL counters and aggregations. " +
                "Anything about what a RULE-BASED detection model IS or how it DECIDES. NOT a statistical " +
                "or machine-learning model fitted from data -- building, training or fitting a classifier, " +
                "regressor, clusterer or anomaly detector (logistic regression, decision tree, random " +
                "forest, neural network, k-means and the like) belongs to model-risk even when the caller " +
                "calls it a \"model\"."),

            new(
                "model-risk",
                [],
                [
                    "How well is the model performing",
                    "Run an exhaustive search on this model",
                    "Why did the model score this transaction so highly",
                    "Show me the backtest results",
                    "What variables matter most in the promoted model",
                    "Set up an HTTP adaptation endpoint",
                    "Make me a logistic regression model using this dataset",
                    "Build a classifier from this data",
                    "Train a decision tree on these columns",
                    "Fit a clustering model over this demo dataset"
                ],
                "Building, training, validating, promoting and explaining a STATISTICAL OR " +
                "MACHINE-LEARNING model fitted from a dataset: Exhaustive search, and Advanced Analytics " +
                "(logistic regression, decision trees, random forests, neural networks, k-means and other " +
                "clustering, anomaly detection, and more), plus backtesting, model performance and " +
                "sensitivity, variable importance, integrity checks and HTTP adaptation. Anything about " +
                "MAKING a model out of data, or whether a model is any GOOD, rather than Jube's own " +
                "rule-based detection model."),

            new(
                "model-support",
                [],
                [
                    "Add a value to the high risk countries list",
                    "What dictionaries are defined",
                    "Reprocess the archive against this rule",
                    "Suppress alerts for this account",
                    "Upload a CSV into a list"
                ],
                "The lookup and operational scaffolding a detection model leans on: lists and list values, " +
                "dictionaries and key value pairs, suppressions and overrides, reprocessing, and the " +
                "configuration navigation tree."),

            new(
                "case-analyst",
                [],
                [
                    "Show me the open cases",
                    "What is on case 4432",
                    "Add a note to this case",
                    "Which alerts fired overnight",
                    "Did this name hit the sanctions list",
                    "Attach this document to the case",
                    "Give me a narrative for this case",
                    "Summarise this case for me",
                    "Write up what happened on this case"
                ],
                "Working live alert and case material as an investigator would: individual cases, their " +
                "notes, files, events and transaction journal, the activation watcher, and sanctions " +
                "screening results. The case CONTENT, not the rules that shape case behaviour."),

            new(
                "case-workflow-admin",
                [],
                [
                    "Add a new case status",
                    "Change which role can see this case filter",
                    "Set up a form on the case workflow",
                    "Define a macro for investigators",
                    "How is case routing configured"
                ],
                "The definitions that SHAPE how cases behave, rather than the cases themselves: case " +
                "workflows, statuses, routing filters, XPath extraction, forms, actions, macros and the role " +
                "grants over each."),

            new(
                "data-analyst",
                [],
                [
                    "Build me a dashboard",
                    "What visualisations exist",
                    "Add a datasource to this report",
                    "Change the parameters on this visualisation",
                    "Chart activations by day"
                ],
                "The visualisation registry: dashboards and reports, their datasources, series and " +
                "parameters, and the role grants over them."),

            new(
                "access-admin",
                [],
                [
                    "Add a new user",
                    "What permissions does this role have",
                    "Create an API key",
                    "Which tenant is this user in",
                    "Lock this account out",
                    "Who can see the preservation page"
                ],
                "Identity and access: users, roles, permissions, API keys, tenancy and user observability."),

            new(
                "platform-ops",
                [],
                [
                    "Is the database healthy",
                    "Why is the engine slow",
                    "Check Redis",
                    "Show me the Postgres connection count",
                    "Is Patroni failing over",
                    "What do the logs say about errors in the last hour",
                    "How many transactions is the engine processing"
                ],
                "The operational health of the platform itself: Postgres, Redis, etcd and Patroni, Docker and " +
                "the overlay network, OpenTelemetry, logs, and engine throughput counters. Infrastructure, " +
                "not configuration."),

            new(
                "assistant-admin",
                [],
                [
                    "What conversations have I had with you",
                    "Clear what you remember about me",
                    "Re-index the documentation",
                    "What is in your retrieval corpus"
                ],
                "The assistant's own stored conversations, memories and retrieval corpus. Only when the " +
                "subject is Jooby itself, never ordinary Jube configuration."),

            new(
                "researcher",
                [
                    new Regex(
                        @"\b(look up|research|find out|dig into)\b|what does (the )?(pasture|documentation|docs) say",
                        RegexOptions.IgnoreCase | RegexOptions.Compiled)
                ],
                [
                    "Look up what the documentation says about abstraction rules",
                    "Research how TTL counters work in Jube",
                    "Find out what the Pasture has on sanctions list matching",
                    "Dig into how activation rules relate to gateway rules"
                ],
                "Only when the task explicitly asks to look something up or research a topic in " +
                "Jube's own documentation or knowledge base, as a self-contained question with no " +
                "further context needed. A general question that can be answered directly, or one " +
                "that is really about doing something (not finding something out), is NOT this.")
        ];
    }
}