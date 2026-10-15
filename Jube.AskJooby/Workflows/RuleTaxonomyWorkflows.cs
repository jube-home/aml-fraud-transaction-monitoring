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

using Jube.AskJooby.Workflows.Models;

namespace Jube.AskJooby.Workflows
{
    public static class RuleTaxonomyWorkflows
    {
        private const string RequestXPathAuthor = "entity-analysis-model-request-x-path-author";
        private const string TtlCounterAuthor = "entity-analysis-model-ttl-counter-author";
        private const string AbstractionRuleAuthor = "entity-analysis-model-abstraction-rule-author";
        private const string AbstractionCalculationAuthor = "entity-analysis-model-abstraction-calculation-author";
        private const string ActivationRuleAuthor = "entity-analysis-model-activation-rule-author";
        private const string ListAuthor = "entity-analysis-model-list-author";
        private const string ListValueAuthor = "entity-analysis-model-list-value-author";
        private const string DictionaryAuthor = "entity-analysis-model-dictionary-author";
        private const string DictionaryKvpAuthor = "entity-analysis-model-dictionary-kvp-author";
        private const string SanctionAuthor = "entity-analysis-model-sanction-author";
        private const string RequestXPathInspector = "entity-analysis-model-request-x-path-inspector-1";
        private const string TtlCounterInspector = "entity-analysis-model-ttl-counter-inspector";
        private const string AbstractionRuleInspector = "entity-analysis-model-abstraction-rule-inspector";

        private const string AbstractionCalculationInspector =
            "entity-analysis-model-abstraction-calculation-inspector";

        private const string ActivationRuleInspector = "entity-analysis-model-activation-rule-inspector";
        private const string ActivationRuleTestBench = "entity-analysis-model-activation-rule-test-bench";
        private const string ListInspector = "entity-analysis-model-list-inspector";
        private const string DictionaryInspector = "entity-analysis-model-dictionary-inspector";
        private const string SanctionInspector = "entity-analysis-model-sanction-inspector";
        private const string SynchronisationSchedule = "entity-analysis-model-synchronisation-schedule";

        private static GraphWorkflowNode ReuseCheck(string componentLabel, string agent,
            IReadOnlyList<string> tools, string produces, string sameness, string id = "reuse-check")
        {
            return new GraphWorkflowNode(
                id,
                $"Check whether a {componentLabel} already does this",
                $"List what the model already has and compare functionally, not by name: {sameness} Where "
                + "one matches, carry its name forward and create nothing. Where one is close but not "
                + "identical, say so and build the new one rather than editing the old -- every rule "
                + "already reading it would change meaning without anyone asking.",
                agent, tools,
                [produces],
                [],
                $"A duplicate {componentLabel} is not harmless. It costs the same work on every event for "
                + "the life of the model, and two components computing one number drift apart the moment "
                + "either is tuned, after which two rules disagree and neither is wrong.");
        }

        private static GraphWorkflowNode Synchronise(params string[] requires)
        {
            return new GraphWorkflowNode(
                "synchronise",
                "Synchronise the model",
                "Schedule synchronisation and confirm it ran. Nothing written in this workflow is in the engine "
                + "until it has, and every rule fragment is compiled here and nowhere else.",
                SynchronisationSchedule,
                [
                    "EntityAnalysisModelSynchronisationScheduleCreate",
                    "EntityAnalysisModelSynchronisationScheduleGetCurrent"
                ],
                ["Synchronised"],
                requires,
                "Until the model has synchronised, every object created above exists only in the database and "
                + "the engine is still running the rule set it started with.");
        }

        private static GraphWorkflowNode ConfirmCompiled()
        {
            return new GraphWorkflowNode(
                "confirm-compiled",
                "Confirm every fragment compiled",
                "Re-read each rule created above and check Compiled is set and CompileError is null. Report the "
                + "rule as built only once that holds for every fragment.",
                ActivationRuleInspector,
                ["EntityAnalysisModelActivationRuleGet"],
                ["CompiledRule"],
                ["Synchronised"],
                "A fragment that fails to compile is saved happily and simply never matches. Nobody is told. "
                + "The rule then looks like missing data for as long as it takes someone to check the flag.");
        }

        private static GraphWorkflowNode ValidateFragment(string id, string subject, params string[] requires)
        {
            return new GraphWorkflowNode(
                id,
                $"Validate the {subject} fragment before saving it",
                $"Compile the {subject} fragment on its own, with the real component and field names it will "
                + "read, and correct it until it validates. Validations are gold: they are free, they are "
                + "unambiguous, and they are the only statement about correctness available before "
                + "synchronisation. Spend one before the fragment is embedded in anything harder to pull apart.",
                ActivationRuleTestBench,
                ["EntityAnalysisModelActivationRuleValidate"],
                ["ValidatedFragment"],
                requires,
                "An unvalidated fragment saves without complaint and is compiled only at synchronisation, where "
                + "failure is silent: the rule never matches, and downstream it reads as missing data rather "
                + "than as a broken rule.");
        }

        public static readonly IReadOnlyList<GraphWorkflow> All =
        [
            new(
                1,
                "velocity-and-volume",
                "Velocity and Volume",
                "Raw frequency or magnitude of events: how much, or how often, is itself the signal.",
                "A TTL Counter keyed on the entity and windowed, incremented by one Activation Rule and read "
                + "by another, with Priority deciding whether the current event is counted.",
                "Use when the question is how much or how often. If it is how many different, that is "
                + "Cardinality; if it is whether this is normal for this entity, that is Behavioral Baseline.",
                [
                    "more than five card payments in an hour",
                    "over 7 transactions where the amount is greater than 100",
                    "total outbound value above 50,000 in a day",
                    "a burst of logins from one account",
                    "velocity rule", "volume rule", "too many transactions"
                ],
                [
                    ReuseCheck("TTL Counter", TtlCounterInspector,
                        [
                            "EntityAnalysisModelTtlCounterList",
                            "EntityAnalysisModelTtlCounterGetByEntityAnalysisModelId"
                        ],
                        "ExistingTtlCounter",
                        "same Data Name, same interval and value, same Sum setting and Data Value, and the same filter on whichever Activation Rule increments it."),
                    new GraphWorkflowNode(
                        "counter",
                        "Define the TTL Counter",
                        "Answer the four design questions before creating it: counted for whom (Data Name, the "
                        + "payload key that groups it), over how long (Interval Type and Value), counting events "
                        + "or value (Sum off for velocity, on with a Data Value for volume), and how precisely it "
                        + "washes off (Online Aggregation, Live Forever, Resolution Interval). Name it for what it "
                        + "means including the window, because a rule reads it as TTLCounter.<Name>.",
                        TtlCounterAuthor,
                        ["EntityAnalysisModelTtlCounterCreate"],
                        ["TtlCounter"],
                        ["ExistingTtlCounter"],
                        "A counter cannot be re-windowed retrospectively, so the window is a decision not a "
                        + "default. A counter whose Data Name is absent from a payload is silently skipped for "
                        + "that event."),
                    new GraphWorkflowNode(
                        "incrementing-rule",
                        "Create the Activation Rule that increments the counter",
                        "A counter is not moved by a payload arriving; it is moved as the consequence of an "
                        + "Activation Rule matching, with Increment TTL Counter ticked and the counter selected. "
                        + "The condition on this rule is the count's filter, so filter here and not in the "
                        + "reading rule, and use Matched = True to count everything.",
                        ActivationRuleAuthor,
                        ["EntityAnalysisModelActivationRuleCreate"],
                        ["IncrementingRule"],
                        ["TtlCounter"],
                        "The counter must already exist: a fragment naming a counter that does not is refused, "
                        + "and the increment has nothing to select."),
                    ValidateFragment("validate-reading-rule", "reading rule", "TtlCounter"),
                    new GraphWorkflowNode(
                        "reading-rule",
                        "Create the Activation Rule that thresholds the counter",
                        "Read it as TTLCounter.<Name>, a number, and threshold it with the numeric pipeline. Keep "
                        + "this separate from the incrementing rule: a rule that both reads and increments "
                        + "increments only after it has matched, so it never sees its own increment.",
                        ActivationRuleAuthor,
                        ["EntityAnalysisModelActivationRuleCreate"],
                        ["ReadingRule"],
                        ["TtlCounter", "ValidatedFragment"],
                        "A pipeline that ends without deciding does not match."),
                    new GraphWorkflowNode(
                        "priority",
                        "Order the two rules by Priority",
                        "For a velocity or volume rule put the incrementing rule ahead of the reading rule, so "
                        + "the count includes the current event and \"the sixth payment in the hour\" means the "
                        + "sixth. State the convention in the rule name or description.",
                        ActivationRuleAuthor,
                        ["EntityAnalysisModelActivationRuleUpdate"],
                        ["PriorityOrder"],
                        ["IncrementingRule", "ReadingRule"],
                        "The off-by-one between \"prior events\" and \"including this one\" is the single most "
                        + "common disagreement in review, and nothing in the saved objects records which was "
                        + "intended."),
                    new GraphWorkflowNode(
                        "test",
                        "Test with a sequence, not a payload",
                        "Post N events for one entity and assert the alert on the event that crosses the "
                        + "threshold, then post an event for a different entity to prove the key isolates counts. "
                        + "Swap the two Priorities and confirm the alert moves one event later.",
                        ActivationRuleTestBench,
                        ["EntityAnalysisModelActivationRuleValidate"],
                        ["Evidence"],
                        ["PriorityOrder"],
                        "A single payload cannot exercise a velocity rule at all, so a test that posts one "
                        + "proves nothing."),
                    Synchronise("ReadingRule", "IncrementingRule"),
                    ConfirmCompiled()
                ],
                "The incrementing rule goes ahead of the reading rule in Priority, or the current event is not "
                + "counted."),

            new(
                2,
                "cardinality",
                "Cardinality (Same and Different)",
                "How many distinct, or how many identical, values an entity has produced for an attribute.",
                "An Abstraction Rule with Search on, Search Key the entity, and Function Type Distinct Count or "
                + "Same Count over the attribute, thresholded in an Activation Rule.",
                "Use when variety is the signal rather than volume. A plain count built as a Distinct Count is "
                + "a Velocity rule wearing the wrong and much heavier mechanism.",
                [
                    "how many different devices has this account used",
                    "the same beneficiary paid repeatedly",
                    "distinct countries in a week",
                    "first time this customer has used this country",
                    "cardinality", "distinct count", "same count"
                ],
                [
                    ReuseCheck("Abstraction Rule", AbstractionRuleInspector,
                        [
                            "EntityAnalysisModelAbstractionRuleList",
                            "EntityAnalysisModelAbstractionRuleGetByEntityAnalysisModelId"
                        ],
                        "ExistingAbstraction",
                        "same Search Key, same Search Interval and Value, same Function Type and Function Key, and an equivalent rule fragment."),
                    new GraphWorkflowNode(
                        "search-key",
                        "Make the entity a Search Key",
                        "The entity being profiled has to be a Search Key in Request XPath. A Search Key drives "
                        + "one keyed retrieval from the cache per invocation, and abstraction rules sharing a key "
                        + "share the retrieval.",
                        RequestXPathAuthor,
                        ["EntityAnalysisModelRequestXPathUpdate"],
                        ["SearchKey"],
                        [],
                        "Without this the abstraction returns zero for ever, with no error anywhere. It is the "
                        + "first thing to check when a cardinality value is always 0."),
                    new GraphWorkflowNode(
                        "abstraction-rule",
                        "Define the Abstraction Rule",
                        "Search on; Search Key the entity; Search Interval the window; the fragment a Boolean "
                        + "filter selecting eligible cached events; Function Type Distinct Count for how many "
                        + "different, or Same Count for how many times this current value has been seen; "
                        + "Function Key the field whose values are counted.",
                        AbstractionRuleAuthor,
                        ["EntityAnalysisModelAbstractionRuleCreate"],
                        ["Abstraction"],
                        ["SearchKey", "ExistingAbstraction"],
                        "The Search Interval is applied after retrieval, so the model's fetch limit truncates a "
                        + "long window before the interval is considered and the count comes back low."),
                    ValidateFragment("validate-activation-rule", "activation rule", "Abstraction"),
                    new GraphWorkflowNode(
                        "activation-rule",
                        "Threshold the abstraction in an Activation Rule",
                        "Read it as Abstraction.<Name>, a number, and apply the numeric pipeline.",
                        ActivationRuleAuthor,
                        ["EntityAnalysisModelActivationRuleCreate"],
                        ["ReadingRule"],
                        ["Abstraction", "ValidatedFragment"],
                        "The binding is by exact abstraction name and a fragment naming one that does not exist "
                        + "is refused, so the abstraction is built first."),
                    new GraphWorkflowNode(
                        "test",
                        "Test with a sequence of differing values",
                        "Post several events for one entity with differing values of the attribute and assert on "
                        + "the event that crosses. Use a two event case to establish on this instance whether the "
                        + "current event is included, which decides whether the threshold is greater than 3 or at "
                        + "least 3.",
                        ActivationRuleTestBench,
                        ["EntityAnalysisModelActivationRuleValidate"],
                        ["Evidence"],
                        ["ReadingRule"],
                        "Whether the current event counts is a property of the instance, not of the rule, so it "
                        + "has to be observed rather than assumed."),
                    Synchronise("ReadingRule"),
                    ConfirmCompiled()
                ],
                "The field must be a Search Key before the abstraction will return anything but zero."),

            new(
                3,
                "link-and-network",
                "Link and Network",
                "The cardinality question asked relationally: how many distinct entities have converged on one "
                + "shared value.",
                "The same Abstraction Rule as Cardinality with the two roles swapped -- Search Key the shared "
                + "trait, Function Key the entity.",
                "Use when the risk is relational rather than about one entity's own behaviour. It detects one "
                + "hop only; it does not traverse a graph.",
                [
                    "how many customers share this device",
                    "several accounts paying the same beneficiary",
                    "one IP used by many users",
                    "link analysis", "network rule", "shared device"
                ],
                [
                    ReuseCheck("Abstraction Rule", AbstractionRuleInspector,
                        [
                            "EntityAnalysisModelAbstractionRuleList",
                            "EntityAnalysisModelAbstractionRuleGetByEntityAnalysisModelId"
                        ],
                        "ExistingAbstraction",
                        "same Search Key on the shared trait, same Function Key on the entity, same window and function. Note that a Cardinality rule on the same two fields with the roles the other way round is NOT the same rule and must not be reused."),
                    new GraphWorkflowNode(
                        "search-key",
                        "Make the shared trait a Search Key",
                        "The shared attribute, not the entity, becomes the Search Key in Request XPath. The same "
                        + "field may also be a Search Key for a Cardinality rule where it plays the entity role.",
                        RequestXPathAuthor,
                        ["EntityAnalysisModelRequestXPathUpdate"],
                        ["SearchKey"],
                        [],
                        "Keying on the entity instead of the trait is the defining error of this type, and it "
                        + "shows as an abstraction value that is always 0 or 1."),
                    new GraphWorkflowNode(
                        "abstraction-rule",
                        "Define the Abstraction Rule with the roles inverted",
                        "Search on; Search Key the shared trait; Function Type Distinct Count; Function Key the "
                        + "entity; Search Interval the window in which convergence means something.",
                        AbstractionRuleAuthor,
                        ["EntityAnalysisModelAbstractionRuleCreate"],
                        ["Abstraction"],
                        ["SearchKey", "ExistingAbstraction"],
                        "The recall returns the history of every entity that presented the trait, so a popular "
                        + "trait such as a public network returns very many records and the fetch limit truncates "
                        + "them."),
                    new GraphWorkflowNode(
                        "exemptions",
                        "Exempt known good shared traits",
                        "A List of traits that are shared innocently -- an office network, a shared kiosk -- "
                        + "rejected before the signal is read. Optional, but a common trait without it produces "
                        + "counts that are high and meaningless.",
                        ListAuthor,
                        ["EntityAnalysisModelListCreate"],
                        ["ExemptionList"],
                        [],
                        "Where the shared trait is known bad in advance this is not a network signal at all but "
                        + "a membership check, which is Static and Match Screening."),
                    new GraphWorkflowNode(
                        "exemption-values",
                        "Load the exempt traits into the List",
                        "The office networks, shared kiosks and corporate proxies that are shared innocently. "
                        + "Upload them as CSV where the set is large enough to be worth maintaining that way.",
                        ListValueAuthor,
                        ["EntityAnalysisModelListValueCreate", "EntityAnalysisModelListValueUploadCsv"],
                        ["ExemptionValues"],
                        ["ExemptionList"],
                        "An empty exemption List guards nothing, and the rule reads exactly as it would with no "
                        + "exemption at all -- which is the state it is most often left in."),
                    ValidateFragment("validate-activation-rule", "activation rule", "Abstraction"),
                    new GraphWorkflowNode(
                        "activation-rule",
                        "Threshold in an Activation Rule",
                        "Read as Abstraction.<Name> and threshold, optionally guarded by the exemption List.",
                        ActivationRuleAuthor,
                        ["EntityAnalysisModelActivationRuleCreate"],
                        ["ReadingRule"],
                        ["Abstraction", "ValidatedFragment"]),
                    new GraphWorkflowNode(
                        "test",
                        "Test with several different entities on one value",
                        "Post events from several different entities presenting one shared value and assert on "
                        + "the crossing event, then post events for one entity across several values to prove it "
                        + "counts entities and not values.",
                        ActivationRuleTestBench,
                        ["EntityAnalysisModelActivationRuleValidate"],
                        ["Evidence"],
                        ["ReadingRule"],
                        "Testing it the Cardinality way passes against a rule with the keys the wrong way round."),
                    Synchronise("ReadingRule"),
                    ConfirmCompiled()
                ],
                "Search Key is the shared trait and Function Key is the entity -- the inverse of Cardinality."),

            new(
                4,
                "behavioral-baseline-and-ratio",
                "Behavioral Baseline and Ratio",
                "Is this normal for this entity, or is this proportion right.",
                "An Abstraction Rule per statistic over a long window, combined into an Abstraction Calculation, "
                + "thresholded in an Activation Rule behind a cold start guard.",
                "Use for self-referential deviation or any proportion-of-two-counters question. If the comparison "
                + "is against a fixed number rather than the entity's own history, it is Velocity and Volume.",
                [
                    "unusual for this customer",
                    "ten times their normal amount",
                    "refunds as a share of sales",
                    "z-score against their own history",
                    "deviation", "baseline", "ratio", "proportion"
                ],
                [
                    ReuseCheck("baseline measure", AbstractionRuleInspector,
                        [
                            "EntityAnalysisModelAbstractionRuleList",
                            "EntityAnalysisModelAbstractionRuleGetByEntityAnalysisModelId"
                        ],
                        "ExistingBaseline",
                        "same Search Key, same window, same statistic and the same measured field."),
                    ReuseCheck("derived calculation", AbstractionCalculationInspector,
                        [
                            "EntityAnalysisModelAbstractionCalculationList",
                            "EntityAnalysisModelAbstractionCalculationGetByEntityAnalysisModelId"
                        ],
                        "ExistingCalculation",
                        "an equivalent fragment over the same operands, whatever it is called -- a ratio computed once is already readable by every rule and every model that wants it.",
                        "reuse-check-calculation"),
                    new GraphWorkflowNode(
                        "search-key",
                        "Make the entity a Search Key",
                        "As Cardinality: the entity whose baseline is being built has to be a Search Key.",
                        RequestXPathAuthor,
                        ["EntityAnalysisModelRequestXPathUpdate"],
                        ["SearchKey"],
                        []),
                    new GraphWorkflowNode(
                        "baseline",
                        "Build the baseline measure, one Abstraction Rule per statistic",
                        "Entity as Search Key, a Search Interval long enough to be representative, Function Type "
                        + "the statistic (Average, Median, Standard Deviation, Max, Min) and Function Key the "
                        + "field measured. One rule per statistic. Anything needing only a count or a sum belongs "
                        + "in a TTL Counter instead, which is far lighter.",
                        AbstractionRuleAuthor,
                        ["EntityAnalysisModelAbstractionRuleCreate"],
                        ["Baseline"],
                        ["SearchKey", "ExistingBaseline"],
                        "A Search Interval that is too short, or truncated by the fetch limit, gives a baseline "
                        + "that moves about and a rule that fires on its own instability."),
                    new GraphWorkflowNode(
                        "calculation",
                        "Create the derived value as an Abstraction Calculation",
                        "The ratio, deviation or growth is defined once as a named Abstraction Calculation "
                        + "fragment, available to every rule and to models as AbstractionCalculation.<Name>, so "
                        + "the rule and the model agree on what it is. End the fragment in ZeroIfUndefined where "
                        + "an undefined result should store zero.",
                        AbstractionCalculationAuthor,
                        ["EntityAnalysisModelAbstractionCalculationCreate"],
                        ["Calculation"],
                        ["Baseline", "ExistingCalculation"],
                        "An undefined result is NaN, and NaN fails every comparison silently, so a rule over a "
                        + "zero denominator never fires and looks merely quiet."),
                    new GraphWorkflowNode(
                        "cold-start-guard",
                        "Guard the cold start",
                        "An entity with no history has no baseline, so require a minimum count of prior events "
                        + "before the deviation is allowed to match. Without it every new customer deviates.",
                        ActivationRuleTestBench,
                        ["EntityAnalysisModelActivationRuleValidate"],
                        ["ColdStartGuard"],
                        ["Calculation"],
                        "This is the single most common fault of the type: the rule is correct and the population "
                        + "it fires on is everybody who just arrived."),
                    new GraphWorkflowNode(
                        "activation-rule",
                        "Threshold the stored value in an Activation Rule",
                        "The rule only compares; the arithmetic was done in the calculation.",
                        ActivationRuleAuthor,
                        ["EntityAnalysisModelActivationRuleCreate"],
                        ["ReadingRule"],
                        ["Calculation", "ColdStartGuard"]),
                    Synchronise("ReadingRule"),
                    ConfirmCompiled()
                ],
                "Where the baseline is a TTL Counter, increment it AFTER the reading rule, or the current event "
                + "is inside the baseline it is being compared against."),

            new(
                5,
                "event-sequencing",
                "Event Sequencing",
                "A chain of states or events that matters only together, within one model or across linked "
                + "models.",
                "A TTL Counter per link, defined in the model that reads it and incremented from the model where "
                + "that event happens, each incrementing rule gated on the previous link's counter. Within one "
                + "model, a Dictionary holding last state compared to the current payload.",
                "Covers lifecycle chains, channel hopping and takeover chains alike -- these are domain dressing "
                + "on one mechanism, not separate classes.",
                [
                    "failed logins then a new payee then a payment",
                    "account takeover chain",
                    "this happened and then that happened",
                    "dormant account suddenly withdrawing",
                    "sequence", "then", "after"
                ],
                [
                    ReuseCheck("set of link counters", TtlCounterInspector,
                        [
                            "EntityAnalysisModelTtlCounterList",
                            "EntityAnalysisModelTtlCounterGetByEntityAnalysisModelId"
                        ],
                        "ExistingLinkCounters",
                        "a counter per link keyed on the same common entity over a window that bounds the same chain."),
                    new GraphWorkflowNode(
                        "common-key",
                        "Establish the common key across every model in the chain",
                        "The key that joins the steps -- a CustomerId -- must be present in the payload of every "
                        + "model in the chain, and is the Data Name of every counter in it.",
                        RequestXPathInspector,
                        ["EntityAnalysisModelRequestXPathGetByEntityAnalysisModelId"],
                        ["CommonKey"],
                        [],
                        "A model whose payload lacks the key has its increment skipped silently, and the chain "
                        + "simply never completes."),
                    new GraphWorkflowNode(
                        "link-counters",
                        "Define a TTL Counter per link, in the model that reads it",
                        "One counter per step of the chain, keyed on the common entity, with an interval that "
                        + "bounds how long the chain may take.",
                        TtlCounterAuthor,
                        ["EntityAnalysisModelTtlCounterCreate"],
                        ["LinkCounters"],
                        ["CommonKey", "ExistingLinkCounters"],
                        "A counter window shorter than the chain takes means the alert never fires and nothing "
                        + "indicates why."),
                    new GraphWorkflowNode(
                        "gated-increments",
                        "Increment each link from its own model, gated on the previous link",
                        "Each incrementing rule is gated on the preceding counter, which is what makes this a "
                        + "sequence rather than three independent counts. Choose the target model in the "
                        + "Increment TTL Counter options.",
                        ActivationRuleAuthor,
                        ["EntityAnalysisModelActivationRuleCreate"],
                        ["GatedIncrements"],
                        ["LinkCounters"],
                        "An ungated increment turns the chain into three unrelated counters that happen to share "
                        + "a key, and it will fire on events that never occurred in order."),
                    new GraphWorkflowNode(
                        "priority",
                        "Set Priority so each gate is evaluated before its own increment",
                        "The rule that tests the chain must run before the rule that increments the step counter "
                        + "for the current event.",
                        ActivationRuleAuthor,
                        ["EntityAnalysisModelActivationRuleUpdate"],
                        ["PriorityOrder"],
                        ["GatedIncrements"],
                        "Get this backwards and the current event counts as its own predecessor, so a single "
                        + "event satisfies the whole sequence. This is the opposite of the Velocity ordering."),
                    ValidateFragment("validate-alerting-rule", "alerting rule", "PriorityOrder"),
                    new GraphWorkflowNode(
                        "alerting-rule",
                        "Threshold the last counter in the alerting rule",
                        "The final step alerts and increments nothing.",
                        ActivationRuleAuthor,
                        ["EntityAnalysisModelActivationRuleCreate"],
                        ["ReadingRule"],
                        ["PriorityOrder", "ValidatedFragment"]),
                    Synchronise("ReadingRule"),
                    ConfirmCompiled()
                ],
                "Gates are evaluated BEFORE their own increment -- the reverse of Velocity and Volume, and "
                + "getting it wrong lets one event satisfy the entire chain."),

            new(
                6,
                "static-and-match-screening",
                "Static and Match Screening",
                "Deterministic or fuzzy match against a known reference set.",
                "A List for internal reference data, a Dictionary where the reference carries a numeric value, "
                + "or a Sanction definition for regulatory lists, read in an Activation Rule.",
                "Covers device blacklists too. A Sanction returns a distance, not a Boolean -- compare it.",
                [
                    "block these accounts",
                    "is the customer on a sanctions list",
                    "country risk score",
                    "deny list", "allow list", "watchlist", "blacklist", "screening"
                ],
                [
                    ReuseCheck("List", ListInspector,
                        ["EntityAnalysisModelListList", "EntityAnalysisModelListGetByEntityAnalysisModelId"],
                        "ExistingList",
                        "a List already holding the same membership, whatever it is named."),
                    ReuseCheck("Dictionary", DictionaryInspector,
                        [
                            "EntityAnalysisModelDictionaryList",
                            "EntityAnalysisModelDictionaryGetByEntityAnalysisModelId"
                        ],
                        "ExistingDictionary",
                        "a Dictionary already keyed the same way and carrying the same kind of number.",
                        "reuse-check-dictionary"),
                    ReuseCheck("Sanction definition", SanctionInspector,
                        ["EntityAnalysisModelSanctionList", "EntityAnalysisModelSanctionGetByEntityAnalysisModelId"],
                        "ExistingSanction",
                        "a Sanction definition over the same payload field with the same distance controls.",
                        "reuse-check-sanction"),
                    new GraphWorkflowNode(
                        "reference-data-list",
                        "Create the List, where the reference is plain membership",
                        "The default kind, and the one to reach for unless the reference carries a number or is "
                        + "regulatory. A List is maintained outside the rule and synchronised continuously by "
                        + "the engine, so a value added to it applies almost immediately and without the model "
                        + "being synchronised at all.",
                        ListAuthor,
                        ["EntityAnalysisModelListCreate"],
                        ["ReferenceSet"],
                        ["ExistingList"]),
                    new GraphWorkflowNode(
                        "reference-data-list-values",
                        "Load the List with its values",
                        "Add the values one at a time, or upload them as CSV where there are many. A List with "
                        + "no values is not an empty deny list, it is a rule that never matches.",
                        ListValueAuthor,
                        ["EntityAnalysisModelListValueCreate", "EntityAnalysisModelListValueUploadCsv"],
                        ["ReferenceSetValues"],
                        ["ReferenceSet"]),
                    new GraphWorkflowNode(
                        "reference-data-dictionary",
                        "Create the Dictionary instead, where each key carries a number",
                        "An alternative to the List and not an addition to it. Use it where the reference is "
                        + "not membership but a value per key -- a country risk score -- which the rule reads "
                        + "as a number rather than as a yes or no.",
                        DictionaryAuthor,
                        ["EntityAnalysisModelDictionaryCreate"],
                        ["ReferenceDictionary"],
                        ["ExistingDictionary"],
                        "A Dictionary returns zero when the key is absent, so encode risk such that zero is the "
                        + "safe default rather than the dangerous one."),
                    new GraphWorkflowNode(
                        "reference-data-dictionary-values",
                        "Load the Dictionary with its key and value pairs",
                        "Add the pairs one at a time, or upload them as CSV where there are many.",
                        DictionaryKvpAuthor,
                        ["EntityAnalysisModelDictionaryKvpCreate", "EntityAnalysisModelDictionaryKvpUploadCsv"],
                        ["ReferenceDictionaryValues"],
                        ["ReferenceDictionary"]),
                    new GraphWorkflowNode(
                        "reference-data-sanction",
                        "Create the Sanction definition instead, for regulatory lists",
                        "The third alternative. A Sanction definition names the payload multipart string field "
                        + "and its distance controls, and screens against loaded sanctions data rather than "
                        + "against reference data maintained here.",
                        SanctionAuthor,
                        ["EntityAnalysisModelSanctionCreate"],
                        ["ReferenceSanction"],
                        ["ExistingSanction"],
                        "A Sanction yields a distance and not a Boolean, so whichever rule reads it has to "
                        + "compare that distance rather than test it for truth."),
                    ValidateFragment("validate-activation-rule", "activation rule", "ReferenceSet"),
                    new GraphWorkflowNode(
                        "activation-rule",
                        "Read the reference set in an Activation Rule",
                        "List membership through the InList steps, case sensitive or not; a Dictionary value "
                        + "compared as a number; a Sanction compared as a distance where smaller is closer and "
                        + "zero is exact. An allow list belongs in a Gateway Rule, which is the cheapest place to "
                        + "stop or route an event.",
                        ActivationRuleAuthor,
                        ["EntityAnalysisModelActivationRuleCreate"],
                        ["ReadingRule"],
                        ["ReferenceSet", "ValidatedFragment"],
                        "Treating a Sanction as a Boolean inverts the rule. Reading one when there was no match "
                        + "throws, and a faulting fragment does not match -- so a rule shaped as \"alert when "
                        + "there is no sanction hit\" never fires."),
                    Synchronise("ReadingRule"),
                    ConfirmCompiled()
                ],
                "Lists synchronise continuously and need no model synchronisation; the rule that reads them "
                + "still does."),

            new(
                7,
                "unstructured-text-screening",
                "Unstructured Text Screening",
                "The signal is in free text -- a narrative, reference, memo or description -- not a structured "
                + "field.",
                "String pipeline steps against a payload text field, with maintained terms held in a List, "
                + "normalised before matching and optionally matched fuzzily.",
                "Use when a structured check would not see it. If the field is an identifier rather than prose, "
                + "that is Static and Match Screening.",
                [
                    "payment reference mentions crypto",
                    "narrative contains a gift card",
                    "look for words in the memo field",
                    "free text screening", "keyword in narrative"
                ],
                [
                    ReuseCheck("term List", ListInspector,
                        ["EntityAnalysisModelListList"],
                        "ExistingTermList",
                        "a List already holding these terms, or holding the concept they describe."),
                    new GraphWorkflowNode(
                        "identify-field",
                        "Identify the free text field and decide how the terms will be maintained",
                        "Few and stable terms are written inline; many or changing terms belong in a List so "
                        + "analysts can maintain them without editing the rule.",
                        RequestXPathInspector,
                        ["EntityAnalysisModelRequestXPathGetByEntityAnalysisModelId"],
                        ["TextField"],
                        []),
                    new GraphWorkflowNode(
                        "term-list",
                        "Hold maintained terms in a List",
                        "Only where the terms change. A small fixed set is written into the fragment.",
                        ListAuthor,
                        ["EntityAnalysisModelListCreate"],
                        ["TermList"],
                        ["ExistingTermList"]),
                    new GraphWorkflowNode(
                        "term-list-values",
                        "Load the terms into the List",
                        "Write the terms in lower case, because the fragment lowers the text before matching "
                        + "and ContainsAll has no ignore-case form to fall back on.",
                        ListValueAuthor,
                        ["EntityAnalysisModelListValueCreate", "EntityAnalysisModelListValueUploadCsv"],
                        ["TermListValues"],
                        ["TermList"],
                        "A term written with a capital letter never matches lowered text, and nothing reports "
                        + "it: the List looks maintained and the rule is quietly blind to that term."),
                    ValidateFragment("validate-activation-rule", "activation rule", "TextField"),
                    new GraphWorkflowNode(
                        "activation-rule",
                        "Write the Activation Rule, normalising before matching",
                        "Free text is dirty and evasion is deliberate, so transform before testing: lower the "
                        + "text, remove diacritics, then match. Use the ignore-case forms where they exist, and "
                        + "fuzzy steps where misspelling is expected.",
                        ActivationRuleAuthor,
                        ["EntityAnalysisModelActivationRuleCreate"],
                        ["ReadingRule"],
                        ["TextField", "ValidatedFragment"],
                        "ContainsAll has no ignore-case form, so the text must be lowered first and the terms "
                        + "written in lower case. Trim on a plain value throws on null and faults the rule."),
                    Synchronise("ReadingRule"),
                    ConfirmCompiled()
                ],
                "Normalise before matching, not after -- an evaded term is the normal case, not the exception."),

            new(
                8,
                "geospatial-and-context",
                "Geospatial and Context",
                "Physical distance, travel or boundary, and the time and calendar context of an event.",
                "Double geospatial steps directly on payload coordinates for a single comparison, or an "
                + "Abstraction Rule retrieving the previous position when comparing against a pattern. DateTime "
                + "steps for calendar context.",
                "Use for anything location-based. Context is cheap and needs no cache, and is best consumed as "
                + "an input to another type rather than alerting alone.",
                [
                    "transaction a long way from home",
                    "impossible travel between two logins",
                    "outside the branch radius",
                    "out of hours transfer", "weekend activity", "geovelocity"
                ],
                [
                    ReuseCheck("history Abstraction Rule", AbstractionRuleInspector,
                        ["EntityAnalysisModelAbstractionRuleList"],
                        "ExistingPositionAbstraction",
                        "an Actual Value abstraction with an Offset of Last on the same Search Key already retrieving the previous position."),
                    new GraphWorkflowNode(
                        "coordinates",
                        "Establish the coordinates and, for travel, the previous position",
                        "A single comparison needs only the current coordinates in the payload. Impossible travel "
                        + "additionally needs the earlier event's coordinates and the elapsed time, supplied by "
                        + "the sender, produced by an Inline Function, or retrieved from history by an Abstraction "
                        + "Rule with Function Type Actual Value and an Offset of Last.",
                        RequestXPathInspector,
                        ["EntityAnalysisModelRequestXPathGetByEntityAnalysisModelId"],
                        ["Coordinates"],
                        [],
                        "Zero elapsed hours is treated as infinite speed, so two events in different places at "
                        + "the same instant match -- which is right for a clone and wrong for a clock problem."),
                    new GraphWorkflowNode(
                        "history-abstraction",
                        "Retrieve the previous position from history, where it is not in the payload",
                        "An Abstraction Rule keyed on the entity with Function Type Actual Value and Offset Last. "
                        + "Only needed for the travel shape, and it costs a cache retrieval per Search Key.",
                        AbstractionRuleAuthor,
                        ["EntityAnalysisModelAbstractionRuleCreate"],
                        ["PreviousPosition"],
                        ["Coordinates", "ExistingPositionAbstraction"]),
                    ValidateFragment("validate-activation-rule", "activation rule", "Coordinates"),
                    new GraphWorkflowNode(
                        "activation-rule",
                        "Write the Activation Rule",
                        "The pipeline starts on the first latitude and takes the first longitude, the second "
                        + "latitude, the second longitude and then the limit, in that order. Distance is great "
                        + "circle, not road distance.",
                        ActivationRuleAuthor,
                        ["EntityAnalysisModelActivationRuleCreate"],
                        ["ReadingRule"],
                        ["Coordinates", "ValidatedFragment"],
                        "The argument order is easy to transpose and a transposed pair produces a plausible "
                        + "distance rather than an error."),
                    Synchronise("ReadingRule"),
                    ConfirmCompiled()
                ]),
            new(
                9,
                "structuring-or-threshold-avoidance",
                "Structuring or Threshold Avoidance",
                "Values engineered to dodge a known limit, repeated.",
                "A payload predicate recognising the engineered value, which is the incrementing rule for a TTL "
                + "Counter keyed on the entity, and a second rule alerting on the repetition.",
                "Use when the risk is evasion of a specific numeric control. Generic anomaly is Behavioral "
                + "Baseline instead.",
                [
                    "amounts just under ten thousand, again and again",
                    "smurfing", "structuring", "threshold avoidance",
                    "round amounts repeatedly", "splitting deposits to stay under the limit"
                ],
                [
                    ReuseCheck("TTL Counter", TtlCounterInspector,
                        [
                            "EntityAnalysisModelTtlCounterList",
                            "EntityAnalysisModelTtlCounterGetByEntityAnalysisModelId"
                        ],
                        "ExistingTtlCounter",
                        "same Data Name, window and Sum setting, incremented by a rule recognising the same engineered value."),
                    new GraphWorkflowNode(
                        "counter",
                        "Define the TTL Counter for the repetition",
                        "Keyed on the entity the control applies to, windowed to the period the control is "
                        + "measured over.",
                        TtlCounterAuthor,
                        ["EntityAnalysisModelTtlCounterCreate"],
                        ["TtlCounter"],
                        ["ExistingTtlCounter"],
                        "Keying on the account rather than the beneficial owner misses the case the control "
                        + "exists for, which is a limit evaded across several accounts."),
                    new GraphWorkflowNode(
                        "predicate-rule",
                        "Recognise the engineered value, and increment from that rule",
                        "IsJustBelowThreshold with a margin derived from the data, or IsRoundAmount, or "
                        + "IsJustBelowAnyThreshold where several limits apply. This rule is the incrementing "
                        + "rule, so the counter holds exactly the engineered events.",
                        ActivationRuleAuthor,
                        ["EntityAnalysisModelActivationRuleCreate"],
                        ["PredicateRule"],
                        ["TtlCounter"],
                        "The threshold, margin and rounding unit must come from the data and the control in "
                        + "question, never from a list of best practice values."),
                    ValidateFragment("validate-alerting-rule", "alerting rule", "PredicateRule"),
                    new GraphWorkflowNode(
                        "alerting-rule",
                        "Alert on the repetition, not the single event",
                        "A single amount below a limit is weak evidence; the repetition is the signal. Where the "
                        + "repetition must be across different destinations, use a Distinct Count abstraction "
                        + "instead of a count.",
                        ActivationRuleAuthor,
                        ["EntityAnalysisModelActivationRuleCreate"],
                        ["ReadingRule"],
                        ["PredicateRule", "ValidatedFragment"],
                        "Alerting on the predicate alone buries the desk: most amounts below a limit are "
                        + "ordinary."),
                    new GraphWorkflowNode(
                        "priority",
                        "Order the predicate rule ahead of the alerting rule",
                        "As Velocity and Volume: incrementing earlier means the current event is included in the "
                        + "count.",
                        ActivationRuleAuthor,
                        ["EntityAnalysisModelActivationRuleUpdate"],
                        ["PriorityOrder"],
                        ["PredicateRule", "ReadingRule"]),
                    Synchronise("ReadingRule", "PredicateRule"),
                    ConfirmCompiled()
                ],
                "The predicate rule is the incrementing rule, and it goes ahead of the alerting rule in "
                + "Priority.")
        ];

        public static IReadOnlyDictionary<string, GraphWorkflow> ById { get; } =
            All.ToDictionary(workflow => workflow.Id, workflow => workflow, StringComparer.Ordinal);
    }
}