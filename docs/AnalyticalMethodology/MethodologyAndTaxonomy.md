---
layout: default
title: Methodology and Taxonomy
nav_order: 1
parent: Analytical Methodology
---

# Methodology and Taxonomy

The original guide opened with a module of its own before any R procedure: a taxonomy for what kind of analytics work is
being attempted, a general project methodology, and a case for approaching the work quantitatively rather than by
intuition. None of that is a technique with an R script to re-run, so it has no home among the procedure-and-equivalent
pages that follow. This page states it once, up front, and points at the pages where the rest of the guide already
carries it out in practice.

## Descriptive, predictive and prescriptive analytics

The original guide framed a data-driven organisation as a chain: data, then reporting, then analysis, then action, then
value, staffed along the way by roles from data engineer through statistician to data scientist. The analysis step
itself was split into six kinds — descriptive, exploratory, inferential, predictive, causal and mechanistic — of which
this guide, like the original, is overwhelmingly concerned with the predictive kind, with descriptive and prescriptive
as the other two poles actually built out here.

<svg viewBox="0 0 1020 440" xmlns="http://www.w3.org/2000/svg" role="img" aria-label="A data-driven organisation: data flows through reporting, analysis and action to value, with analysis split into six kinds of technique and staffed by six roles.">
<defs><marker id="arrow" markerWidth="8" markerHeight="8" refX="6" refY="3" orient="auto"><path d="M0,0 L6,3 L0,6 Z" fill="#2e3192"/></marker></defs>
<text x="510.0" y="26.0" font-family="Helvetica, Arial, sans-serif" font-size="18" font-weight="bold" fill="#1f2937" text-anchor="middle">A data-driven organisation</text>
<line x1="90.0" y1="112" x2="930.0" y2="112" stroke="#94a3b8" stroke-width="1.5"/>
<line x1="90.0" y1="96" x2="90.0" y2="112" stroke="#94a3b8" stroke-width="1.5"/>
<line x1="258.0" y1="96" x2="258.0" y2="112" stroke="#94a3b8" stroke-width="1.5"/>
<line x1="426.0" y1="96" x2="426.0" y2="112" stroke="#94a3b8" stroke-width="1.5"/>
<line x1="594.0" y1="96" x2="594.0" y2="112" stroke="#94a3b8" stroke-width="1.5"/>
<line x1="762.0" y1="96" x2="762.0" y2="112" stroke="#94a3b8" stroke-width="1.5"/>
<line x1="930.0" y1="96" x2="930.0" y2="112" stroke="#94a3b8" stroke-width="1.5"/>
<line x1="510.0" y1="112" x2="510.0" y2="210" stroke="#94a3b8" stroke-width="1.5" marker-end="url(#arrow)"/>
<rect x="22.0" y="54" width="136" height="42" rx="8" fill="#eef1fb" stroke="#2e3192" stroke-width="1.5"/>
<text x="90.0" y="80.0" font-family="Helvetica, Arial, sans-serif" font-size="12.5" font-weight="normal" fill="#1f2937" text-anchor="middle">Descriptive</text>
<rect x="190.0" y="54" width="136" height="42" rx="8" fill="#eef1fb" stroke="#2e3192" stroke-width="1.5"/>
<text x="258.0" y="80.0" font-family="Helvetica, Arial, sans-serif" font-size="12.5" font-weight="normal" fill="#1f2937" text-anchor="middle">Exploratory</text>
<rect x="358.0" y="54" width="136" height="42" rx="8" fill="#eef1fb" stroke="#2e3192" stroke-width="1.5"/>
<text x="426.0" y="80.0" font-family="Helvetica, Arial, sans-serif" font-size="12.5" font-weight="normal" fill="#1f2937" text-anchor="middle">Inferential</text>
<rect x="526.0" y="54" width="136" height="42" rx="8" fill="#eef1fb" stroke="#2e3192" stroke-width="1.5"/>
<text x="594.0" y="80.0" font-family="Helvetica, Arial, sans-serif" font-size="12.5" font-weight="normal" fill="#1f2937" text-anchor="middle">Predictive</text>
<rect x="694.0" y="54" width="136" height="42" rx="8" fill="#eef1fb" stroke="#2e3192" stroke-width="1.5"/>
<text x="762.0" y="80.0" font-family="Helvetica, Arial, sans-serif" font-size="12.5" font-weight="normal" fill="#1f2937" text-anchor="middle">Causal</text>
<rect x="862.0" y="54" width="136" height="42" rx="8" fill="#eef1fb" stroke="#2e3192" stroke-width="1.5"/>
<text x="930.0" y="80.0" font-family="Helvetica, Arial, sans-serif" font-size="12.5" font-weight="normal" fill="#1f2937" text-anchor="middle">Mechanistic</text>
<rect x="16.0" y="210" width="148" height="68" rx="10" fill="#4753a8" stroke="#1f2461" stroke-width="1.5"/>
<text x="90.0" y="250.0" font-family="Helvetica, Arial, sans-serif" font-size="15" font-weight="bold" fill="#ffffff" text-anchor="middle">Data</text>
<line x1="170.0" y1="244.0" x2="220.0" y2="244.0" stroke="#2e3192" stroke-width="2" marker-end="url(#arrow)"/>
<rect x="226.0" y="210" width="148" height="68" rx="10" fill="#4753a8" stroke="#1f2461" stroke-width="1.5"/>
<text x="300.0" y="250.0" font-family="Helvetica, Arial, sans-serif" font-size="15" font-weight="bold" fill="#ffffff" text-anchor="middle">Reporting</text>
<line x1="380.0" y1="244.0" x2="430.0" y2="244.0" stroke="#2e3192" stroke-width="2" marker-end="url(#arrow)"/>
<rect x="436.0" y="210" width="148" height="68" rx="10" fill="#2e3192" stroke="#1f2461" stroke-width="1.5"/>
<text x="510.0" y="250.0" font-family="Helvetica, Arial, sans-serif" font-size="15" font-weight="bold" fill="#ffffff" text-anchor="middle">Analysis</text>
<line x1="590.0" y1="244.0" x2="640.0" y2="244.0" stroke="#2e3192" stroke-width="2" marker-end="url(#arrow)"/>
<rect x="646.0" y="210" width="148" height="68" rx="10" fill="#4753a8" stroke="#1f2461" stroke-width="1.5"/>
<text x="720.0" y="250.0" font-family="Helvetica, Arial, sans-serif" font-size="15" font-weight="bold" fill="#ffffff" text-anchor="middle">Action</text>
<line x1="800.0" y1="244.0" x2="850.0" y2="244.0" stroke="#2e3192" stroke-width="2" marker-end="url(#arrow)"/>
<rect x="856.0" y="210" width="148" height="68" rx="10" fill="#4753a8" stroke="#1f2461" stroke-width="1.5"/>
<text x="930.0" y="250.0" font-family="Helvetica, Arial, sans-serif" font-size="15" font-weight="bold" fill="#ffffff" text-anchor="middle">Value</text>
<line x1="90.0" y1="330" x2="930.0" y2="330" stroke="#94a3b8" stroke-width="1.5"/>
<line x1="90.0" y1="330" x2="90.0" y2="346" stroke="#94a3b8" stroke-width="1.5"/>
<line x1="258.0" y1="330" x2="258.0" y2="346" stroke="#94a3b8" stroke-width="1.5"/>
<line x1="426.0" y1="330" x2="426.0" y2="346" stroke="#94a3b8" stroke-width="1.5"/>
<line x1="594.0" y1="330" x2="594.0" y2="346" stroke="#94a3b8" stroke-width="1.5"/>
<line x1="762.0" y1="330" x2="762.0" y2="346" stroke="#94a3b8" stroke-width="1.5"/>
<line x1="930.0" y1="330" x2="930.0" y2="346" stroke="#94a3b8" stroke-width="1.5"/>
<line x1="510.0" y1="278" x2="510.0" y2="330" stroke="#94a3b8" stroke-width="1.5" marker-end="url(#arrow)"/>
<rect x="17.0" y="346" width="146" height="42" rx="8" fill="#f4f5fa" stroke="#6b7280" stroke-width="1.5"/>
<text x="90.0" y="372.0" font-family="Helvetica, Arial, sans-serif" font-size="12" font-weight="normal" fill="#1f2937" text-anchor="middle">Data Analyst</text>
<rect x="185.0" y="346" width="146" height="42" rx="8" fill="#f4f5fa" stroke="#6b7280" stroke-width="1.5"/>
<text x="258.0" y="372.0" font-family="Helvetica, Arial, sans-serif" font-size="12" font-weight="normal" fill="#1f2937" text-anchor="middle">Statistician</text>
<rect x="353.0" y="346" width="146" height="42" rx="8" fill="#f4f5fa" stroke="#6b7280" stroke-width="1.5"/>
<text x="426.0" y="372.0" font-family="Helvetica, Arial, sans-serif" font-size="12" font-weight="normal" fill="#1f2937" text-anchor="middle">Data Engineer</text>
<rect x="521.0" y="346" width="146" height="42" rx="8" fill="#f4f5fa" stroke="#6b7280" stroke-width="1.5"/>
<text x="594.0" y="372.0" font-family="Helvetica, Arial, sans-serif" font-size="12" font-weight="normal" fill="#1f2937" text-anchor="middle">Business Analyst</text>
<rect x="689.0" y="346" width="146" height="42" rx="8" fill="#f4f5fa" stroke="#6b7280" stroke-width="1.5"/>
<text x="762.0" y="372.0" font-family="Helvetica, Arial, sans-serif" font-size="12" font-weight="normal" fill="#1f2937" text-anchor="middle">Data Scientist</text>
<rect x="857.0" y="346" width="146" height="42" rx="8" fill="#f4f5fa" stroke="#6b7280" stroke-width="1.5"/>
<text x="930.0" y="372.0" font-family="Helvetica, Arial, sans-serif" font-size="12" font-weight="normal" fill="#1f2937" text-anchor="middle">Quants</text>
<text x="510.0" y="412.0" font-family="Helvetica, Arial, sans-serif" font-size="12.5" font-weight="normal" fill="#4b5563" text-anchor="middle">Analysis splits into six kinds of technique; the chain is staffed by six roles along the way</text>
</svg>

This guide's own structure already follows that three-way split, page by page rather than by declaring it:

- **Descriptive** — [Summary Statistics](SummaryStatistics.md) (distribution, spread, shape) and
  [Charting](Charting.md) (visual description) cover what already happened in a dataset.
- **Predictive** — every page from [Linear Regression](LinearRegression.md) through
  [Neural Networks](NeuralNetworks.md), plus the twenty-two families in
  [Other Model Types](OtherModelTypes/index.md), scores what a given record is likely to do.
- **Prescriptive** — [Prescriptive Analytics](PrescriptiveAnalytics/index.md) (Monte Carlo simulation and
  Bayesian-network intervention) asks what should change and what follows if it does, across a simulated population
  rather than one record.

## Quantitative reasoning over intuition

The original guide made a deliberate case for preferring what the data says over what an analyst assumes it says, naming
a working list of the cognitive biases that make unaided intuition unreliable: the conjunction and disjunction
fallacies, the planning fallacy, the gambler's fallacy, availability and representativeness heuristics, anchoring and
adjustment, confirmation bias, hindsight bias, optimism bias and status quo bias among them. The point of naming them is
not to memorise the list, but to justify the guide's own working discipline: a claim about a technique's behaviour is
only as good as an actually-run result. That is also why every R snippet elsewhere in this guide is a genuine, captured
console output rather than a plausible-looking transcription, and why every `AdvancedAnalytics` equivalent names a real
class or tool rather than a plausible-sounding one — the same discipline the original guide was arguing for in its own
first module, applied here to how the guide itself is written.

<svg viewBox="0 0 920 480" xmlns="http://www.w3.org/2000/svg" role="img" aria-label="Cognitive biases against quantitative reasoning, grouped into three branches: fallacy, heuristics and bias, each radiating from unaided intuition.">
<text x="460.0" y="26.0" font-family="Helvetica, Arial, sans-serif" font-size="17" font-weight="bold" fill="#1f2937" text-anchor="middle">Cognitive biases against quantitative reasoning</text>
<line x1="365.0" y1="210.0" x2="320.0" y2="150.0" stroke="#94a3b8" stroke-width="1.5"/>
<line x1="365.0" y1="210.0" x2="320.0" y2="340.0" stroke="#94a3b8" stroke-width="1.5"/>
<line x1="555.0" y1="210.0" x2="600.0" y2="145.0" stroke="#94a3b8" stroke-width="1.5"/>
<rect x="60.0" y="80.0" width="260.0" height="140.0" rx="10" fill="#f4f5fa" stroke="#6b7280" stroke-width="1.5"/>
<text x="190.0" y="104.0" font-family="Helvetica, Arial, sans-serif" font-size="14" font-weight="bold" fill="#2e3192" text-anchor="middle">Fallacy</text>
<text x="190.0" y="124.0" font-family="Helvetica, Arial, sans-serif" font-size="12" font-weight="normal" fill="#374151" text-anchor="middle">Conjunction</text>
<text x="190.0" y="144.0" font-family="Helvetica, Arial, sans-serif" font-size="12" font-weight="normal" fill="#374151" text-anchor="middle">Disjunction</text>
<text x="190.0" y="164.0" font-family="Helvetica, Arial, sans-serif" font-size="12" font-weight="normal" fill="#374151" text-anchor="middle">Planning</text>
<text x="190.0" y="184.0" font-family="Helvetica, Arial, sans-serif" font-size="12" font-weight="normal" fill="#374151" text-anchor="middle">Gambler's</text>
<text x="190.0" y="204.0" font-family="Helvetica, Arial, sans-serif" font-size="12" font-weight="normal" fill="#374151" text-anchor="middle">Insensitivity to sample size</text>
<rect x="60.0" y="290.0" width="260.0" height="100.0" rx="10" fill="#f4f5fa" stroke="#6b7280" stroke-width="1.5"/>
<text x="190.0" y="314.0" font-family="Helvetica, Arial, sans-serif" font-size="14" font-weight="bold" fill="#2e3192" text-anchor="middle">Heuristics</text>
<text x="190.0" y="334.0" font-family="Helvetica, Arial, sans-serif" font-size="12" font-weight="normal" fill="#374151" text-anchor="middle">Availability</text>
<text x="190.0" y="354.0" font-family="Helvetica, Arial, sans-serif" font-size="12" font-weight="normal" fill="#374151" text-anchor="middle">Representativeness</text>
<text x="190.0" y="374.0" font-family="Helvetica, Arial, sans-serif" font-size="12" font-weight="normal" fill="#374151" text-anchor="middle">Anchoring and adjustment</text>
<rect x="600.0" y="65.0" width="260.0" height="160.0" rx="10" fill="#f4f5fa" stroke="#6b7280" stroke-width="1.5"/>
<text x="730.0" y="89.0" font-family="Helvetica, Arial, sans-serif" font-size="14" font-weight="bold" fill="#2e3192" text-anchor="middle">Bias</text>
<text x="730.0" y="109.0" font-family="Helvetica, Arial, sans-serif" font-size="12" font-weight="normal" fill="#374151" text-anchor="middle">Confirmation</text>
<text x="730.0" y="129.0" font-family="Helvetica, Arial, sans-serif" font-size="12" font-weight="normal" fill="#374151" text-anchor="middle">Hindsight</text>
<text x="730.0" y="149.0" font-family="Helvetica, Arial, sans-serif" font-size="12" font-weight="normal" fill="#374151" text-anchor="middle">Optimism</text>
<text x="730.0" y="169.0" font-family="Helvetica, Arial, sans-serif" font-size="12" font-weight="normal" fill="#374151" text-anchor="middle">Status quo</text>
<text x="730.0" y="189.0" font-family="Helvetica, Arial, sans-serif" font-size="12" font-weight="normal" fill="#374151" text-anchor="middle">Durability</text>
<text x="730.0" y="209.0" font-family="Helvetica, Arial, sans-serif" font-size="12" font-weight="normal" fill="#374151" text-anchor="middle">Conservatism</text>
<rect x="365.0" y="186.0" width="190.0" height="48.0" rx="24" fill="#2e3192" stroke="#1f2461" stroke-width="1.5"/>
<text x="460.0" y="215.0" font-family="Helvetica, Arial, sans-serif" font-size="14.5" font-weight="bold" fill="#ffffff" text-anchor="middle">Intuition</text>
<text x="460.0" y="464.0" font-family="Helvetica, Arial, sans-serif" font-size="12.5" font-weight="normal" fill="#4b5563" text-anchor="middle">Quantitative reasoning is preferred because each of these runs unchecked without it</text>
</svg>

## The data / abstraction / model / activation / feedback loop

The original guide described predictive analytics as a five-stage loop — data, abstraction, model, activation,
feedback — and that loop is not just a historical framing device: it is the same loop Jube's own rule and model pipeline
runs today, invocation order and all.

<svg viewBox="0 0 920 282" xmlns="http://www.w3.org/2000/svg" role="img" aria-label="The data, abstraction, model, activation and feedback loop: a chain of five stages, with feedback looping back from feedback to data for the next pass.">
<defs><marker id="arrow-dark" markerWidth="8" markerHeight="8" refX="6" refY="3" orient="auto"><path d="M0,0 L6,3 L0,6 Z" fill="#2e3192"/></marker><marker id="arrow-light" markerWidth="8" markerHeight="8" refX="6" refY="3" orient="auto"><path d="M0,0 L6,3 L0,6 Z" fill="#8892c9"/></marker></defs>
<text x="460.0" y="30.0" font-family="Helvetica, Arial, sans-serif" font-size="16.5" font-weight="bold" fill="#1f2937" text-anchor="middle">The data / abstraction / model / activation / feedback loop</text>
<rect x="9.0" y="90" width="152" height="72" rx="10" fill="#2e3192" stroke="#1f2461" stroke-width="1.5"/>
<text x="85.0" y="132.0" font-family="Helvetica, Arial, sans-serif" font-size="14" font-weight="bold" fill="#ffffff" text-anchor="middle">Data</text>
<line x1="167.0" y1="126.0" x2="190.5" y2="126.0" stroke="#2e3192" stroke-width="2" marker-end="url(#arrow-dark)"/>
<rect x="196.5" y="90" width="152" height="72" rx="10" fill="#2e3192" stroke="#1f2461" stroke-width="1.5"/>
<text x="272.5" y="132.0" font-family="Helvetica, Arial, sans-serif" font-size="14" font-weight="bold" fill="#ffffff" text-anchor="middle">Abstraction</text>
<line x1="354.5" y1="126.0" x2="378.0" y2="126.0" stroke="#2e3192" stroke-width="2" marker-end="url(#arrow-dark)"/>
<rect x="384.0" y="90" width="152" height="72" rx="10" fill="#2e3192" stroke="#1f2461" stroke-width="1.5"/>
<text x="460.0" y="132.0" font-family="Helvetica, Arial, sans-serif" font-size="14" font-weight="bold" fill="#ffffff" text-anchor="middle">Model</text>
<line x1="542.0" y1="126.0" x2="565.5" y2="126.0" stroke="#2e3192" stroke-width="2" marker-end="url(#arrow-dark)"/>
<rect x="571.5" y="90" width="152" height="72" rx="10" fill="#2e3192" stroke="#1f2461" stroke-width="1.5"/>
<text x="647.5" y="132.0" font-family="Helvetica, Arial, sans-serif" font-size="14" font-weight="bold" fill="#ffffff" text-anchor="middle">Activation</text>
<line x1="729.5" y1="126.0" x2="753.0" y2="126.0" stroke="#2e3192" stroke-width="2" marker-end="url(#arrow-dark)"/>
<rect x="759.0" y="90" width="152" height="72" rx="10" fill="#2e3192" stroke="#1f2461" stroke-width="1.5"/>
<text x="835.0" y="132.0" font-family="Helvetica, Arial, sans-serif" font-size="14" font-weight="bold" fill="#ffffff" text-anchor="middle">Feedback</text>
<line x1="835.0" y1="162.0" x2="835.0" y2="232.0" stroke="#8892c9" stroke-width="2"/>
<line x1="835.0" y1="232.0" x2="85.0" y2="232.0" stroke="#8892c9" stroke-width="2"/>
<line x1="85.0" y1="232.0" x2="85.0" y2="162.0" stroke="#8892c9" stroke-width="2" marker-end="url(#arrow-light)"/>
<text x="460.0" y="254.0" font-family="Helvetica, Arial, sans-serif" font-size="12.5" font-weight="normal" fill="#4b5563" text-anchor="middle">a labelled outcome feeds back into the next pass over the data</text>
</svg>

| Stage       | What it means                                                   | Where it lives in this guide and the product                                                                                                                  |
|-------------|-----------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Data        | The raw records available to reason over                        | [Loading, Shaping and Merging Data](LoadingShapingMerging.md), [Data Frames vs the Packed Array](DataFramesVsAccordArrays.md)                                 |
| Abstraction | Deriving variables from raw data before modelling               | [Abstraction](AbstractionHarmonised.md); Jube's own Abstraction Rules and Abstraction Calculations                                                            |
| Model       | Fitting a regression, classifier, clusterer or anomaly detector | Every page from [Linear Regression](LinearRegression.md) onward, and [Other Model Types](OtherModelTypes/index.md)                                            |
| Activation  | Turning a model's output into a decision                        | Activation Rules, downstream of Abstraction in the fixed invocation order `AbstractionHarmonised.md` describes                                                |
| Feedback    | Recording the real outcome so it can be learned from again      | A labelled disposition recorded against a case, which is where a training dependent variable actually comes from (see `AbstractionHarmonised.md` Procedure 1) |

Reading any one page in this guide against this loop places it: most pages sit in "Model," a handful sit in
"Data" or "Abstraction," and Prescriptive Analytics is a deliberate detour off the loop — simulating many hypothetical
passes through Model rather than running one real record through it.

## Limits of predictive analytics

The original guide was equally direct about what a fitted model cannot do: it cannot understand a problem domain the way
a person with domain experience can, and it cannot critically think or innovate beyond the data it was shown. Its stated
best use is to initiate human action — surfacing a case, a score or a ranked list for a person to act on — not to
replace the judgement behind that action entirely. That is the same reasoning
behind [Abstraction](AbstractionHarmonised.md)'s own claim that feature engineering, not model fitting, is where most of
a predictive analytics project's real effort belongs: a model can only ever be as good as the domain understanding
encoded into the variables it is given.

## Types of predictive analytics

The original guide grouped every technique it taught into a small number of shapes, and this guide's individual pages
are that same taxonomy in practice rather than a new one:

<svg viewBox="0 0 940 560" xmlns="http://www.w3.org/2000/svg" role="img" aria-label="Types of predictive analytics as overlapping classification and numeric prediction circles, with neural networks and the Exhaustive engine spanning both, and Bayesian and clustering methods standing apart.">
<text x="470.0" y="28.0" font-family="Helvetica, Arial, sans-serif" font-size="18" font-weight="bold" fill="#1f2937" text-anchor="middle">Types of predictive analytics</text>
<circle cx="360.0" cy="290.0" r="195.0" fill="#8ea0e0" fill-opacity="0.55" stroke="#2e3192" stroke-width="1.8"/>
<circle cx="580.0" cy="290.0" r="195.0" fill="#f3b26a" fill-opacity="0.55" stroke="#2e3192" stroke-width="1.8"/>
<text x="290.0" y="129.0" font-family="Helvetica, Arial, sans-serif" font-size="15" font-weight="bold" fill="#1f2937" text-anchor="middle">Classification</text>
<text x="650.0" y="129.0" font-family="Helvetica, Arial, sans-serif" font-size="15" font-weight="bold" fill="#1f2937" text-anchor="middle">Numeric prediction</text>
<rect x="195.0" y="234.0" width="180.0" height="32.0" rx="7" fill="#ffffff" stroke="#6b7280" stroke-width="1.3"/>
<text x="285.0" y="255.0" font-family="Helvetica, Arial, sans-serif" font-size="12.5" font-weight="normal" fill="#1f2937" text-anchor="middle">Logistic regression</text>
<rect x="195.0" y="314.0" width="180.0" height="32.0" rx="7" fill="#ffffff" stroke="#6b7280" stroke-width="1.3"/>
<text x="285.0" y="335.0" font-family="Helvetica, Arial, sans-serif" font-size="12.5" font-weight="normal" fill="#1f2937" text-anchor="middle">C5.0 decision tree</text>
<rect x="565.0" y="234.0" width="180.0" height="32.0" rx="7" fill="#ffffff" stroke="#6b7280" stroke-width="1.3"/>
<text x="655.0" y="255.0" font-family="Helvetica, Arial, sans-serif" font-size="12.5" font-weight="normal" fill="#1f2937" text-anchor="middle">Linear regression</text>
<rect x="565.0" y="314.0" width="180.0" height="32.0" rx="7" fill="#ffffff" stroke="#6b7280" stroke-width="1.3"/>
<text x="655.0" y="335.0" font-family="Helvetica, Arial, sans-serif" font-size="12.5" font-weight="normal" fill="#1f2937" text-anchor="middle">Regression tree</text>
<rect x="395.0" y="249.0" width="150.0" height="30.0" rx="7" fill="#ffffff" stroke="#6b7280" stroke-width="1.3"/>
<text x="470.0" y="269.0" font-family="Helvetica, Arial, sans-serif" font-size="12.5" font-weight="normal" fill="#1f2937" text-anchor="middle">Neural network</text>
<rect x="395.0" y="301.0" width="150.0" height="30.0" rx="7" fill="#ffffff" stroke="#6b7280" stroke-width="1.3"/>
<text x="470.0" y="321.0" font-family="Helvetica, Arial, sans-serif" font-size="12.5" font-weight="normal" fill="#1f2937" text-anchor="middle">Exhaustive (Jube)</text>
<rect x="55.0" y="454.0" width="190.0" height="32.0" rx="7" fill="#ffffff" stroke="#6b7280" stroke-width="1.3"/>
<text x="150.0" y="475.0" font-family="Helvetica, Arial, sans-serif" font-size="12.5" font-weight="normal" fill="#1f2937" text-anchor="middle">Bayesian</text>
<rect x="655.0" y="454.0" width="270.0" height="32.0" rx="7" fill="#ffffff" stroke="#6b7280" stroke-width="1.3"/>
<text x="790.0" y="475.0" font-family="Helvetica, Arial, sans-serif" font-size="12.5" font-weight="normal" fill="#1f2937" text-anchor="middle">Clustering and anomaly detection</text>
<text x="470.0" y="538.0" font-family="Helvetica, Arial, sans-serif" font-size="12.5" font-weight="normal" fill="#4b5563" text-anchor="middle">Neural networks and Jube's Exhaustive engine act as universal methods, standing in for either</text>
</svg>

| Shape                            | Covered by                                                                                                                                                                                           |
|----------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Numeric prediction (regression)  | [Linear Regression](LinearRegression.md), the regression tree in [Decision Trees and Boosting](DecisionTreesAndBoosting.md), the regressor families in [Other Model Types](OtherModelTypes/index.md) |
| Classification                   | [Logistic Regression](LogisticRegression.md), the C5.0 tree in [Decision Trees and Boosting](DecisionTreesAndBoosting.md), the classifier families in [Other Model Types](OtherModelTypes/index.md)  |
| Bayesian                         | [Naive Bayes](NaiveBayes.md), [Bayesian Analysis with bnlearn](BayesianAnalysisWithBnlearn.md)                                                                                                       |
| Neural network                   | [Neural Networks](NeuralNetworks.md), described by the original guide as a universal method able to stand in for regression or classification                                                        |
| Clustering and anomaly detection | The clustering and anomaly-detection families in [Other Model Types](OtherModelTypes/index.md)                                                                                                       |

## Problem framing and elicitation

Before any of the above, the original guide's first practical exercise was elicitation: given a business question (its
own example was telecom customer churn), write down an exhaustive list of the reasons the outcome might occur, without
yet worrying about whether a column for each reason actually exists. That list is the raw
material [Abstraction](AbstractionHarmonised.md) turns into Abstraction Rules and Calculations — elicitation supplies
the candidate variables; abstraction is the mechanism that computes them. Skipping elicitation and jumping straight to
whatever columns are already easiest to query is the most common way a predictive analytics project quietly narrows its
own ceiling before modelling has even started.
