---
layout: default
title: Machine Learning and Analytics
nav_order: 5
parent: FAQ
---

# Machine Learning and Analytics

## Training and scoring

**Q: What does the automated trainer do?**

A: It samples tagged and untagged records from the archive, computes statistics, then randomly selects variable subsets
and evolving neural-network topologies, and keeps the best (70/20/10 train/validate/test split). A confusion matrix
shows performance.

**Q: How compute-heavy is scoring?**

A: Very light. Models compile to bytecode with a handful of weights. No GPU is needed, and scoring adds negligible
latency to an invocation.

## Bringing your own model

**Q: How do I use my own model?**

A: Deploy it as a separate microservice (for example R or Python) and configure
[HTTP Adaptation](../Configuration/Models/HTTPAdaptation/index.html). The full invocation payload is posted to your
endpoint, and the result can be used in rules as a plain number. The
[HTTP Adaptation Protocol](../Configuration/Models/HTTPAdaptationProtocol/index.html) (version 1.1) returns a structured
object — Value, Error, Narrative, HumanLabel, ProtocolVersion, Model, Result, Calibration, Contribution and Journey —
with backward compatibility for a bare JSON number, which is always a legal response.

**Q: What happens when an HTTP adaptation endpoint fails or times out?**

A: The adaptation is suppressed: the result carries a null Value and an Error, and the pipeline continues. Rules should
read adaptation values with `OrNaN()` or `OrZero()` so a suppressed adaptation degrades gracefully instead of erroring.
The call is bounded by `HttpAdaptationTimeout`, and mock endpoints (`EnableMockEndpoints`, for development only) let you
exercise the protocol without a real model service.

## Tagging and labels

**Q: How does tagging work?**

A: Tags can be applied via the invoke API and in the Case Key Journal in the UI. They are stored as an array in their
own table, promoted automatically, and are written after the fact, so they are not in the response payload. They exist
for reporting and training rather than for use inside the invocation context.

**Q: Is the case rating feature useful for training models?**

A: Generally no. It is subjective and tends to overfit. Prefer explicit tags grounded in confirmed outcomes.
