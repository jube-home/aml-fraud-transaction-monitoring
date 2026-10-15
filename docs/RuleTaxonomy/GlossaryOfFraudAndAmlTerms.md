---
layout: default
title: Glossary of Fraud and AML Terms
parent: Rule Taxonomy
nav_order: 14
---

# Glossary of Fraud and AML Terms

## G1. Money Laundering Typologies and Concepts

### Bust-out — see G2.

### Cash-intensive business abuse

**Definition:** Criminal cash is mixed with legitimate takings of a cash-heavy business (restaurant, car wash, vending,
laundromat, nail bar, taxi) so it appears as revenue.

**Data signature:** Deposits inconsistent with the business's size, sector peers, footfall or card/cash mix; very
regular, round or just-below-threshold deposits; minimal supplier payments relative to revenue.

**Detection rules:** deposits vs declared turnover and peer norms (4); cash-in share and deposit-to-supplier-outflow
ratios (4); sub-threshold deposit bands (9); sudden step-up in deposits (4); multiple branches (2); deposits by
non-signatories (2/3).

In Jube: class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #9
Structuring or Threshold Avoidance, using threshold extensions IsJustBelowThreshold and IsRoundAmount, repeated through
class 1 or 2; class #2 Cardinality (Same and Different), using an Abstraction Rule with a Same or Distinct Count; class
#3 Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait.
Read [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Structuring or Threshold Avoidance](RuleTaxonomyTypes/StructuringOrThresholdAvoidance.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html).

### Correspondent banking abuse / nested accounts

**Definition:** A respondent bank passes transactions of its own customers (sometimes unknown to the correspondent)
through its account.

**Signature:** High-volume, high-value flows in many currencies; payments with stripped originator data; payments on
behalf of unnamed downstream customers.

**Rules:** missing originator/beneficiary information (6); cover payment anomalies (6); high-risk respondent country
(6); sudden volume growth vs baseline (4); many distinct ultimate beneficiaries per respondent (2).

In Jube: class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists; class #4 Behavioral
Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #2 Cardinality (Same and Different),
using an Abstraction Rule with a Same or Distinct Count.
Read [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html).

Check: the high-risk jurisdiction and respondent country lists must come from an authoritative source your obligations
recognise. A human must confirm the source.

### Funnel account

**Definition:** An account in one geography collecting cash deposits from many locations and withdrawing the money
elsewhere, often across state/country lines.

**Signature:** Deposits in many branches/cities followed by withdrawals far away; many depositor locations; rapid debit
after credit.

**Rules:** distinct deposit locations (2); deposits in one place and withdrawals far away (8); rapid in-out turnover
(1/4); sub-threshold deposits (9).

In Jube: class #2 Cardinality (Same and Different), using an Abstraction Rule with a Same or Distinct Count; class #8
Geospatial and Context, using Double geospatial extensions on Payload coordinates; class #1 Velocity and Volume, using a
TTL Counter; class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #9
Structuring or Threshold Avoidance, using threshold extensions IsJustBelowThreshold and IsRoundAmount, repeated through
class 1 or 2.
Read [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Geospatial and Context](RuleTaxonomyTypes/GeospatialAndContext.html), [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Structuring or Threshold Avoidance](RuleTaxonomyTypes/StructuringOrThresholdAvoidance.html).

### Hawala and informal value transfer systems (IVTS)

**Definition:** Value moved through trusted brokers with settlement by offset, trade or cash couriers, bypassing the
formal system.

**Signature:** Business accounts with many small inbound credits from unrelated individuals and outbound payments to
trade counterparties or in bulk to a few overseas accounts; narratives referencing "hawala".

**Rules:** fan-in then bulk fan-out (3); narrative terms (7); unlicensed MSB indicators (6); payments to corridor
countries (6); high count of third-party credits (1/2).

In Jube: class #3 Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #7
Unstructured Text Screening, using String extensions against a List of terms; class #6 Static and Match Screening, using
a List, or a Sanction for regulatory lists; class #1 Velocity and Volume, using a TTL Counter; class #2 Cardinality
(Same and Different), using an Abstraction Rule with a Same or Distinct Count.
Read [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Unstructured Text Screening](RuleTaxonomyTypes/UnstructuredTextScreening.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html).

Check: money service business licensing and registration requirements, and what counts as an unlicensed provider, depend
on jurisdiction. A human must confirm them.

### Integration

**Definition:** The final laundering stage, making funds appear legitimate (property, luxury goods, business investment,
loans against deposits).

**Rules:** property/high-value goods purchases with third-party funds (5); early loan repayment from third parties (4);
loans to shell companies (3); investment in assets inconsistent with profile (4).

In Jube: class #5 Event Sequencing, using in-model Dictionary state or a TTL Counter read across linked models; class #4
Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #3 Link and Network, using
an Abstraction Rule with the Search Key inverted to the shared trait.
Read [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html).

### Layering

**Definition:** Moving funds through multiple transactions, accounts, jurisdictions and instruments to obscure origin.

**Rules:** pass-through (4 ratio ~1); chain sequences with multiple hops (5); circular flows (3); multi-jurisdiction
routing (8); rapid conversion across currencies/assets (2/5).

In Jube: class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #1
Velocity and Volume, using a TTL Counter; class #5 Event Sequencing, using in-model Dictionary state or a TTL Counter
read across linked models; class #3 Link and Network, using an Abstraction Rule with the Search Key inverted to the
shared trait; class #8 Geospatial and Context, using Double geospatial extensions on Payload coordinates; class #2
Cardinality (Same and Different), using an Abstraction Rule with a Same or Distinct Count.
Read [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Geospatial and Context](RuleTaxonomyTypes/GeospatialAndContext.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html).

### Loan-back scheme

**Definition:** The launderer lends their own illicit funds to themselves through an offshore entity and repays the
"loan" with interest to legitimise proceeds.

**Rules:** loan from offshore related party (6/3); repayments by third parties (4); loan proceeds that match prior
deposits (5); no commercial rationale (7).

In Jube: class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists; class #3 Link and
Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #4 Behavioral Baseline and
Ratio, using an Abstraction Rule and Abstraction Calculation; class #5 Event Sequencing, using in-model Dictionary state
or a TTL Counter read across linked models; class #7 Unstructured Text Screening, using String extensions against a List
of terms.
Read [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Unstructured Text Screening](RuleTaxonomyTypes/UnstructuredTextScreening.html).

### Money mule — see G2. (Fraud-related and AML-related mules overlap.)

### Nominee / straw person

**Definition:** A person who holds accounts or companies on behalf of the beneficial owner (to hide control).

**Signature:** Authorised-signer or power-of-attorney activity by a third party; account activity inconsistent with
nominee's profile; devices/IPs shared with the true controller.

**Rules:** shared device/IP between customers (3); activity inconsistent with age/income (4); signatory controlling many
accounts (3); document/attribute reuse (3).

In Jube: class #3 Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #4
Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation.
Read [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html).

### Placement

**Definition:** Introduction of illicit cash into the financial system.

**Rules:** large cash deposits vs baseline (4); structuring bands (9); cash-intensive deposits vs peers (4); purchase of
monetary instruments with cash (1/9); casino chip buy-in without play (5).

In Jube: class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #9
Structuring or Threshold Avoidance, using threshold extensions IsJustBelowThreshold and IsRoundAmount, repeated through
class 1 or 2; class #1 Velocity and Volume, using a TTL Counter; class #5 Event Sequencing, using in-model Dictionary
state or a TTL Counter read across linked models.
Read [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Structuring or Threshold Avoidance](RuleTaxonomyTypes/StructuringOrThresholdAvoidance.html), [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html).

### Pass-through / "flow-through" account

**Definition:** Account used only to receive and rapidly forward funds.

**Signature:** Credits and debits mirror within hours; closing balance near zero; very low retained balance.

**Rules:** debit-to-credit ratio near 1 (4); rapid in-out sequence (5); low retained balance (4); many distinct senders
and recipients (2/3).

In Jube: class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #5 Event
Sequencing, using in-model Dictionary state or a TTL Counter read across linked models; class #2 Cardinality (Same and
Different), using an Abstraction Rule with a Same or Distinct Count; class #3 Link and Network, using an Abstraction
Rule with the Search Key inverted to the shared trait.
Read [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html).

### Payable-through accounts (PTA)

**Definition:** Correspondent accounts where foreign bank customers can transact directly.

**Rules:** sub-account volume anomalies (4); direct customer transactions by foreign respondents' users (3); high-risk
jurisdiction share (4).

In Jube: class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #3 Link
and Network, using an Abstraction Rule with the Search Key inverted to the shared trait.
Read [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html).

### Round-tripping

**Definition:** Funds leave and return to the originator via a series of transactions, often to create false revenue or
move funds without apparent source.

**Rules:** circular payment flows (3); matched out/in amounts within window (5); same counterparties in both directions
(2/3); trade invoices with no goods movement (7/6).

In Jube: class #3 Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #5
Event Sequencing, using in-model Dictionary state or a TTL Counter read across linked models; class #2 Cardinality (Same
and Different), using an Abstraction Rule with a Same or Distinct Count; class #7 Unstructured Text Screening, using
String extensions against a List of terms; class #6 Static and Match Screening, using a List, or a Sanction for
regulatory lists.
Read [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Unstructured Text Screening](RuleTaxonomyTypes/UnstructuredTextScreening.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html).

### Shell company / front company

**Definition:** Legal entity with no real operations used to hold assets, receive/pass funds or hide ownership. A front
company has some operations that mask illicit activity.

**Signature:** Newly incorporated; virtual-office or registered-agent address; nominee directors; no employees or
payroll; large payments with vague descriptions; many entities at one address.

**Rules:** company age (6/C1); address shared by many entities (3); nominee director flags (6); no payroll/supplier
pattern (4); turnover inconsistent with profile (4); payment descriptions vague (7); high-risk jurisdiction ownership
(6).

In Jube: class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists; cross-cutting C1
Identity and Demographic, using derived inputs such as age and tenure from Payload and DateTime extensions (not a
numbered class); class #3 Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait;
class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #7 Unstructured
Text Screening, using String extensions against a List of terms.
Read [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Identity and Demographic](index.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Unstructured Text Screening](RuleTaxonomyTypes/UnstructuredTextScreening.html).

### Smurfing

**Definition:** Use of many individuals ("smurfs") to make many small deposits or transactions under thresholds.

**Rules:** many distinct depositors into one account (2); sub-threshold bands (9); same-day deposits across branches
(9); linked depositors via device/address (3).

In Jube: class #2 Cardinality (Same and Different), using an Abstraction Rule with a Same or Distinct Count; class #9
Structuring or Threshold Avoidance, using threshold extensions IsJustBelowThreshold and IsRoundAmount, repeated through
class 1 or 2; class #3 Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait.
Read [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Structuring or Threshold Avoidance](RuleTaxonomyTypes/StructuringOrThresholdAvoidance.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html).

Check: the thresholds that smurfing is defined against are set by your regulator and jurisdiction. A human must confirm
the values before any rule uses them.

### Structuring

**Definition:** Breaking up transactions to avoid reporting or monitoring thresholds. See class 9.

In Jube: class #9 Structuring or Threshold Avoidance, using threshold extensions IsJustBelowThreshold and IsRoundAmount,
repeated through class 1 or 2.
Read [Structuring or Threshold Avoidance](RuleTaxonomyTypes/StructuringOrThresholdAvoidance.html).

Check: the reporting and monitoring thresholds that structuring is defined against are set by your regulator and
jurisdiction. A human must confirm the values before any rule uses them.

### Trade-based money laundering (TBML)

**Definition:** Moving value through trade transactions by misrepresenting price, quantity, quality or description of
goods, or via fictitious trade.

**Typologies:** over-invoicing, under-invoicing, multiple invoicing of the same shipment, over/under-shipment, phantom
shipment, falsely described goods, black-market peso exchange.

**Rules:** price versus market-benchmark ratio (4); invoice vs shipping quantity ratio (4); multiple invoices per
shipment (2); vague goods text (7); dual-use goods and high-risk corridors (6); counterparties in transshipment hubs
(6/8); payments by third parties not in the trade chain (3); repeated amendments to LCs (1/5).

In Jube: class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #2
Cardinality (Same and Different), using an Abstraction Rule with a Same or Distinct Count; class #7 Unstructured Text
Screening, using String extensions against a List of terms; class #6 Static and Match Screening, using a List, or a
Sanction for regulatory lists; class #8 Geospatial and Context, using Double geospatial extensions on Payload
coordinates; class #3 Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait;
class #1 Velocity and Volume, using a TTL Counter; class #5 Event Sequencing, using in-model Dictionary state or a TTL
Counter read across linked models.
Read [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Unstructured Text Screening](RuleTaxonomyTypes/UnstructuredTextScreening.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Geospatial and Context](RuleTaxonomyTypes/GeospatialAndContext.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html).

### Transshipment / third-country routing

**Definition:** Goods or payments routed through a third country to evade sanctions or controls.

**Rules:** third-country hubs for controlled goods (6); payer in country other than buyer (3/8); newly created
counterparties near sanctioned borders (8).

In Jube: class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists; class #3 Link and
Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #8 Geospatial and Context,
using Double geospatial extensions on Payload coordinates.
Read [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Geospatial and Context](RuleTaxonomyTypes/GeospatialAndContext.html).

Check: the controlled-goods and sanctions lists that the hub rules depend on, and your obligations to keep them current,
are set by the relevant authorities. A human must confirm them.

### Underground banking — see Hawala.

### Wash trading (securities/NFT/crypto)

**Definition:** Buying and selling the same asset between related parties to create artificial volume or move value.

**Rules:** buyer and seller linked by device/address (3); same asset with repeated back-and-forth (2/5); price deviation
with no market move (4); account pairs with consistent profit/loss asymmetry (4).

In Jube: class #3 Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #2
Cardinality (Same and Different), using an Abstraction Rule with a Same or Distinct Count; class #5 Event Sequencing,
using in-model Dictionary state or a TTL Counter read across linked models; class #4 Behavioral Baseline and Ratio,
using an Abstraction Rule and Abstraction Calculation.
Read [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html).

## G2. Mule, Account and Identity Fraud Typologies

### Account takeover (ATO)

**Definition:** A fraudster gains control of a legitimate customer's account.

**Vectors:** credential stuffing, phishing, malware, SIM swap, social engineering, data breach, session hijack.

**Data signature:** new device, new IP/ISP/country, failed logins then success, credential change, new payee, high-value
or draining outflow, notification settings changed.

**Rules:** class 5 sequences (credential change → payee → payment); first-seen device/IP (2); failed-login velocity (1);
impossible travel (8); behavioural-biometric deviation (4); blacklisted device/IP (6); amount z-score (4).

In Jube: class #2 Cardinality (Same and Different), using an Abstraction Rule with a Same or Distinct Count; class #1
Velocity and Volume, using a TTL Counter; class #8 Geospatial and Context, using Double geospatial extensions on Payload
coordinates; class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #6
Static and Match Screening, using a List, or a Sanction for regulatory lists; class #5 Event Sequencing, using in-model
Dictionary state or a TTL Counter read across linked models.
Read [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Geospatial and Context](RuleTaxonomyTypes/GeospatialAndContext.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html).

### Application fraud

**Definition:** False information or identity used to open accounts or obtain credit.

**Subtypes:** first-party (misrepresentation of own details), third-party (stolen identity), synthetic, bust-out, mule
account opening.

**Rules:** shared device/address/phone across applications (3); applications per device/IP velocity (1); document reuse
(2/3); inconsistent identity attributes (6/7); consortium matches (6); form-fill speed (1); employer/address
implausibility (7).

In Jube: class #3 Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #1
Velocity and Volume, using a TTL Counter; class #2 Cardinality (Same and Different), using an Abstraction Rule with a
Same or Distinct Count; class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists; class #7
Unstructured Text Screening, using String extensions against a List of terms.
Read [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Unstructured Text Screening](RuleTaxonomyTypes/UnstructuredTextScreening.html).

### Authorised push payment (APP) scam

**Definition:** The customer is deceived into authorising a payment to a criminal (romance, investment, invoice,
impersonation, purchase, advance-fee, CEO fraud).

**Data signature:** new beneficiary; unusual size relative to history; savings drawn down; escalating payments; customer
on a call; remote-access tool; crypto purchases; narrative scam terms; payee flagged as mule.

**Rules:** new payee + high value (2/4); credit-then-out sequence (5); escalating ladder (4/5); beneficiary on mule list
(6); multiple victims to one beneficiary (3); scam phrases (7); remote-access detected (6); older-customer unusual
transfers (4).

In Jube: class #2 Cardinality (Same and Different), using an Abstraction Rule with a Same or Distinct Count; class #4
Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #5 Event Sequencing, using
in-model Dictionary state or a TTL Counter read across linked models; class #6 Static and Match Screening, using a List,
or a Sanction for regulatory lists; class #3 Link and Network, using an Abstraction Rule with the Search Key inverted to
the shared trait; class #7 Unstructured Text Screening, using String extensions against a List of terms.
Read [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Unstructured Text Screening](RuleTaxonomyTypes/UnstructuredTextScreening.html).

### Bank-card testing / BIN attack / enumeration

**Definition:** Fraudsters generate or test card numbers using small transactions or incremental card-number guesses
against merchants.

**Signature:** many declines in short window; sequential PANs within one BIN; small-value approvals; same IP/device
across many cards.

**Rules:** declines per merchant/BIN velocity (1); distinct cards per device/IP (2); small-amount approvals followed by
large (5); micro-payments count (1).

In Jube: class #1 Velocity and Volume, using a TTL Counter; class #2 Cardinality (Same and Different), using an
Abstraction Rule with a Same or Distinct Count; class #5 Event Sequencing, using in-model Dictionary state or a TTL
Counter read across linked models.
Read [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html).

### Bust-out fraud

**Definition:** A borrower builds credit then maxes out all lines with no intention to repay, often with synthetic
identities.

**Signature:** normal behaviour followed by rapid utilisation across products; cash advances; large payments from
third-party/just-bounced cheques; multiple new credit enquiries.

**Rules:** utilisation ratio acceleration (4); credit enquiry velocity (1); limit-increase then cash-out (5); payment
returned after credit restored (5); new accounts opened across lenders (6 consortium); balance drawdown trend (4).

In Jube: class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #1
Velocity and Volume, using a TTL Counter; class #5 Event Sequencing, using in-model Dictionary state or a TTL Counter
read across linked models; class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists.
Read [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html).

### Business email compromise (BEC) / CEO fraud / invoice redirection

**Definition:** Impersonation of executives or suppliers via email to redirect payments.

**Signature:** change of supplier bank details; urgent payments; first payment to new account for an existing supplier;
payment outside normal cycle.

**Rules:** supplier bank change then payment (5); new beneficiary for known vendor (2); amount deviation vs history (4);
domain-lookalike text (7); payment just under approval limits (9); beneficiary in high-fraud jurisdiction (6).

In Jube: class #5 Event Sequencing, using in-model Dictionary state or a TTL Counter read across linked models; class #2
Cardinality (Same and Different), using an Abstraction Rule with a Same or Distinct Count; class #4 Behavioral Baseline
and Ratio, using an Abstraction Rule and Abstraction Calculation; class #7 Unstructured Text Screening, using String
extensions against a List of terms; class #9 Structuring or Threshold Avoidance, using threshold extensions
IsJustBelowThreshold and IsRoundAmount, repeated through class 1 or 2; class #6 Static and Match Screening, using a
List, or a Sanction for regulatory lists.
Read [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Unstructured Text Screening](RuleTaxonomyTypes/UnstructuredTextScreening.html), [Structuring or Threshold Avoidance](RuleTaxonomyTypes/StructuringOrThresholdAvoidance.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html).

### Card-not-present (CNP) fraud

**Definition:** Fraud using card details online or by phone.

**Rules:** shipping/billing mismatch (6); high-risk MCC (6); device/IP mismatch (8); velocity per card/device/IP (1);
new device + high value (2); distance/time anomalies (8); BIN risk (6); email risk (6).

In Jube: class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists; class #8 Geospatial and
Context, using Double geospatial extensions on Payload coordinates; class #1 Velocity and Volume, using a TTL Counter;
class #2 Cardinality (Same and Different), using an Abstraction Rule with a Same or Distinct Count.
Read [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Geospatial and Context](RuleTaxonomyTypes/GeospatialAndContext.html), [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html).

### Card-present counterfeit/skimming

**Definition:** Cloned cards used at POS/ATMs after data capture.

**Rules:** impossible travel (8); chip-fallback usage (5); ATM hotspots (8); cash-out at multiple ATMs (1/2); common
point-of-purchase analysis (3).

In Jube: class #8 Geospatial and Context, using Double geospatial extensions on Payload coordinates; class #5 Event
Sequencing, using in-model Dictionary state or a TTL Counter read across linked models; class #1 Velocity and Volume,
using a TTL Counter; class #2 Cardinality (Same and Different), using an Abstraction Rule with a Same or Distinct Count;
class #3 Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait.
Read [Geospatial and Context](RuleTaxonomyTypes/GeospatialAndContext.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html).

### Common point of purchase (CPP) analysis

**Definition:** Identifying a compromised merchant/ATM by finding the shared location among cards later reported as
fraudulent.

**Rules:** merchants/terminals shared by cards with confirmed fraud (3); spikes in fraud reports per merchant (1/4).

In Jube: class #3 Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #1
Velocity and Volume, using a TTL Counter; class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and
Abstraction Calculation.
Read [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html).

### Credential stuffing

**Definition:** Automated login attempts using breached username/password pairs.

**Rules:** failed logins per IP/device (1); distinct usernames per IP (2); failed:success ratio (4); headless/automation
signatures (6); API rate (1); same password across accounts (2).

In Jube: class #1 Velocity and Volume, using a TTL Counter; class #2 Cardinality (Same and Different), using an
Abstraction Rule with a Same or Distinct Count; class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and
Abstraction Calculation; class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists.
Read [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html).

### Dormant account takeover

**Definition:** Fraudster targets inactive accounts, which receive little attention.

**Rules:** dormancy then login and contact change (5); first outflow after long inactivity (4/5); new payee (2).

In Jube: class #5 Event Sequencing, using in-model Dictionary state or a TTL Counter read across linked models; class #4
Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #2 Cardinality (Same and
Different), using an Abstraction Rule with a Same or Distinct Count.
Read [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html).

Check: the number of days after which an account counts as dormant is set by regulation or policy and differs by
jurisdiction. A human must confirm it.

### Drop address / reshipper

**Definition:** Address used to receive goods bought with stolen cards.

**Rules:** many customers/cards per delivery address (2/3); address on freight-forwarder list (6); delivery far from
billing (8).

In Jube: class #2 Cardinality (Same and Different), using an Abstraction Rule with a Same or Distinct Count; class #3
Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #6 Static and Match
Screening, using a List, or a Sanction for regulatory lists; class #8 Geospatial and Context, using Double geospatial
extensions on Payload coordinates.
Read [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Geospatial and Context](RuleTaxonomyTypes/GeospatialAndContext.html).

### Elder financial abuse

**Definition:** Exploiting older or vulnerable customers (including by relatives or carers).

**Rules:** changes in pattern for age band (4); new third-party access/POA (5); sudden large cash withdrawals (4);
payments to new payees by vulnerable-flagged customers (2/C1); ATM withdrawals with multiple cards (3).

In Jube: class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #5 Event
Sequencing, using in-model Dictionary state or a TTL Counter read across linked models; class #2 Cardinality (Same and
Different), using an Abstraction Rule with a Same or Distinct Count; cross-cutting C1 Identity and Demographic, using
derived inputs such as age and tenure from Payload and DateTime extensions (not a numbered class); class #3 Link and
Network, using an Abstraction Rule with the Search Key inverted to the shared trait.
Read [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Identity and Demographic](index.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html).

Check: the definitions of an older or vulnerable customer, and any age bands, follow regulator guidance and your own
policy. A human must confirm them.

### Friendly fraud / first-party chargeback fraud

**Definition:** A customer disputes a legitimate purchase.

**Rules:** chargeback rate per customer (1/4); purchase → dispute → repeat purchase at same merchant (5); delivery
confirmation versus dispute (6); refund-and-chargeback combined (5).

In Jube: class #1 Velocity and Volume, using a TTL Counter; class #4 Behavioral Baseline and Ratio, using an Abstraction
Rule and Abstraction Calculation; class #5 Event Sequencing, using in-model Dictionary state or a TTL Counter read
across linked models; class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists.
Read [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html).

### Identity theft (true-name fraud)

**Definition:** Use of someone's real identity data to open or access accounts.

**Rules:** mismatched device/phone/email vs identity (3); new contact details quickly changed (5); credit enquiries (1);
consortium hits (6); deceased check (6).

In Jube: class #3 Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #5
Event Sequencing, using in-model Dictionary state or a TTL Counter read across linked models; class #1 Velocity and
Volume, using a TTL Counter; class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists.
Read [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html).

### Money mule

**Definition:** A person (witting, negligent or unwitting) whose account moves criminal proceeds, often recruited via
job or romance scams, social media or by "account renting".

**Types:** *Witting* (knows), *Negligent* (suspects but ignores), *Unwitting* (deceived), *Complicit professional*,
*Synthetic mule* (account opened with fake identity), *Account-takeover mule* (hijacked account used as mule).

**Lifecycle:** recruit → account opened/borrowed → funded by scams/fraud proceeds → rapid forwarding (often crypto or
cash) → account closed/abandoned.

**Data signatures:**

- Young or newly opened account; very low prior activity; sudden high-value inflows from unrelated parties.

- Credits from many unrelated senders (including victims of APP scams); inbound payments with scam-related references.

- Immediate outflow (hours) by cash, crypto, wallets or international transfer; near-zero retained balance.

- Access from the same device/IP as other mules; multiple new accounts from one device; shared phone/address.

- Account opened and funded shortly after job-offer-type contact; remote-access tool usage.

**Detection rules:**

- Fan-in of many first-time senders (3); distinct senders velocity (2); unrelated sender mix.

- Rapid in-out and debit-to-credit ratio ≈ 1 (4/5); retained balance ratio (4).

- New account high activity (4); activity inconsistent with age/income (4/C1).

- Device/IP/phone/address shared across accounts (3); blacklisted mule device or IBAN (6).

- Payment to or from a known mule account (6/3); beneficiary receiving from multiple victims (3).

- Credit followed by crypto purchase or cash withdrawal (5); cash-out at multiple ATMs (2).

- Narrative scam phrases or mule-recruitment text (7).

- Inbound from consumer accounts that raised APP scam claims (3/6).

- Account sells/rents indicators: login from new device then high turnover with no consumer behaviour (4/5).

- Consortium / inter-bank mule intelligence (6).

In Jube: class #3 Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #2
Cardinality (Same and Different), using an Abstraction Rule with a Same or Distinct Count; class #4 Behavioral Baseline
and Ratio, using an Abstraction Rule and Abstraction Calculation; class #5 Event Sequencing, using in-model Dictionary
state or a TTL Counter read across linked models; cross-cutting C1 Identity and Demographic, using derived inputs such
as age and tenure from Payload and DateTime extensions (not a numbered class); class #6 Static and Match Screening,
using a List, or a Sanction for regulatory lists; class #7 Unstructured Text Screening, using String extensions against
a List of terms.
Read [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Identity and Demographic](index.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Unstructured Text Screening](RuleTaxonomyTypes/UnstructuredTextScreening.html).

### Mule herder / mule network

**Definition:** Organiser who recruits and controls mules and moves funds onwards.

**Rules:** common beneficiary across many mule accounts (3); shared devices across mule logins (3); coordinated opening
and funding on the same day (5); same payment-reference patterns (2/7).

In Jube: class #3 Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #5
Event Sequencing, using in-model Dictionary state or a TTL Counter read across linked models; class #2 Cardinality (Same
and Different), using an Abstraction Rule with a Same or Distinct Count; class #7 Unstructured Text Screening, using
String extensions against a List of terms.
Read [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Unstructured Text Screening](RuleTaxonomyTypes/UnstructuredTextScreening.html).

### Phishing / smishing / vishing

**Definition:** Fraudulent emails, SMS or calls to obtain credentials, OTPs or authorisation.

**Rules:** login following suspicious referral (5); OTP request velocity (1); SIM change then login (5); call-session
then payment (5); OTP shared (customer reading OTP) indicators (5); remote-access detected (6).

In Jube: class #5 Event Sequencing, using in-model Dictionary state or a TTL Counter read across linked models; class #1
Velocity and Volume, using a TTL Counter; class #6 Static and Match Screening, using a List, or a Sanction for
regulatory lists.
Read [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html).

### Remote-access tool (RAT) scam / tech-support scam

**Definition:** Victim is persuaded to install software enabling the fraudster to control the device.

**Rules:** remote-control app detected (6); mouse/keyboard automation signals (4); session while screen-share active
(5); new payee + payment on same session (5); odd cursor/typing biometrics (4).

In Jube: class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists; class #4 Behavioral
Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #5 Event Sequencing, using in-model
Dictionary state or a TTL Counter read across linked models.
Read [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html).

### Romance scam

**Definition:** Long-term deception to extract money from a victim through a fake relationship.

**Rules:** escalating transfers to several new payees (4/5); payments to crypto or overseas (6); gift cards (1);
narrative terms (7); older customers (4).

In Jube: class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #5 Event
Sequencing, using in-model Dictionary state or a TTL Counter read across linked models; class #6 Static and Match
Screening, using a List, or a Sanction for regulatory lists; class #1 Velocity and Volume, using a TTL Counter; class #7
Unstructured Text Screening, using String extensions against a List of terms.
Read [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Unstructured Text Screening](RuleTaxonomyTypes/UnstructuredTextScreening.html).

### SIM swap

**Definition:** Fraudster transfers victim's number to another SIM to capture OTPs.

**Rules:** telco SIM-change flag then login/payment (5); device change with same number (3/5); OTP request velocity (1);
multiple IMSI per number (2).

In Jube: class #5 Event Sequencing, using in-model Dictionary state or a TTL Counter read across linked models; class #3
Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #1 Velocity and
Volume, using a TTL Counter; class #2 Cardinality (Same and Different), using an Abstraction Rule with a Same or
Distinct Count.
Read [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html).

### Social engineering / vishing

**Definition:** Manipulation of customers or staff to bypass controls.

**Rules:** call-centre contact then limit change and payment (5); coached customer text (7); safe-account narratives
(7); override of warning (5).

In Jube: class #5 Event Sequencing, using in-model Dictionary state or a TTL Counter read across linked models; class #7
Unstructured Text Screening, using String extensions against a List of terms.
Read [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Unstructured Text Screening](RuleTaxonomyTypes/UnstructuredTextScreening.html).

### Synthetic identity fraud

**Definition:** A fabricated identity combining real and fake data (e.g. real national ID with false name/DOB) used to
build credit and bust out.

**Types:** manipulated synthetics, fabricated, frankenstein.

**Data signature:** thin or no credit history; authorised-user piggy-backing; multiple identities sharing
phone/address/device; newly issued SSN-like identifiers; inconsistent age and ID issue date; fast credit growth.

**Rules:** shared attributes across identities (3); many identities per device/address (3); mismatched age-vs-document
issue (C1/6); thin-file with high limit request (4); piggy-back authorised users (3); consortium/registry (6); bust-out
sequences (5).

In Jube: class #3 Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait;
cross-cutting C1 Identity and Demographic, using derived inputs such as age and tenure from Payload and DateTime
extensions (not a numbered class); class #6 Static and Match Screening, using a List, or a Sanction for regulatory
lists; class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #5 Event
Sequencing, using in-model Dictionary state or a TTL Counter read across linked models.
Read [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Identity and Demographic](index.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html).

Check: national identifier formats differ by jurisdiction, and the SSN-like examples are not universal. A human must
confirm the formats your rules check.

### Triangulation fraud (e-commerce)

**Definition:** Fake storefront takes orders, fulfils from a real merchant using stolen card data.

**Rules:** shipping address mismatching billing (6); many recipients per merchant buyer (2); merchant with unusual
price/volume (4); chargebacks cluster (1).

In Jube: class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists; class #2 Cardinality
(Same and Different), using an Abstraction Rule with a Same or Distinct Count; class #4 Behavioral Baseline and Ratio,
using an Abstraction Rule and Abstraction Calculation; class #1 Velocity and Volume, using a TTL Counter.
Read [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html).

### Transaction laundering / factoring / undisclosed payment aggregation

**Definition:** A legitimate merchant account processes payments for another (often illicit) business.

**Rules:** mismatch between merchant MCC/descriptor and actual transactions (6); sudden ticket-size or time-of-day
shifts (4); URL mismatch in transaction data (6/7); high-risk-card geography (8).

In Jube: class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists; class #4 Behavioral
Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #7 Unstructured Text Screening, using
String extensions against a List of terms; class #8 Geospatial and Context, using Double geospatial extensions on
Payload coordinates.
Read [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Unstructured Text Screening](RuleTaxonomyTypes/UnstructuredTextScreening.html), [Geospatial and Context](RuleTaxonomyTypes/GeospatialAndContext.html).

### Triangular / reverse-wire and invoice fraud

**Definition:** Paying fake invoices or redirecting genuine ones.

**Rules:** duplicate invoices (2); bank-account change (5); vendor equals employee attributes (3); round or
sub-threshold invoices (9).

In Jube: class #2 Cardinality (Same and Different), using an Abstraction Rule with a Same or Distinct Count; class #5
Event Sequencing, using in-model Dictionary state or a TTL Counter read across linked models; class #3 Link and Network,
using an Abstraction Rule with the Search Key inverted to the shared trait; class #9 Structuring or Threshold Avoidance,
using threshold extensions IsJustBelowThreshold and IsRoundAmount, repeated through class 1 or 2.
Read [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Structuring or Threshold Avoidance](RuleTaxonomyTypes/StructuringOrThresholdAvoidance.html).

### Vishing — see Phishing.

### Wire fraud — see APP scam/BEC.

## G3. Financial-Crime Predicates and Programmes

### Bribery and corruption

**Signature:** payments to officials/PEPs; unusual commissions to intermediaries; consultancy payments with vague
descriptions; payments to jurisdictions with high corruption risk.

**Rules:** PEP screening (6); high-corruption-country corridors (6); commission/gift text (7); intermediary round
payments (9); payments just below approval limits (9).

In Jube: class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists; class #7 Unstructured
Text Screening, using String extensions against a List of terms; class #9 Structuring or Threshold Avoidance, using
threshold extensions IsJustBelowThreshold and IsRoundAmount, repeated through class 1 or 2.
Read [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Unstructured Text Screening](RuleTaxonomyTypes/UnstructuredTextScreening.html), [Structuring or Threshold Avoidance](RuleTaxonomyTypes/StructuringOrThresholdAvoidance.html).

### Cyber-enabled fraud proceeds / ransomware

**Signature:** inflows from unrelated parties to crypto purchases; fast purchase of crypto then outflow to wallets
flagged; narrative ransom terms.

**Rules:** wallet address match (6); crypto purchase velocity (1); sequence from credit to crypto (5); ransom text (7).

In Jube: class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists; class #1 Velocity and
Volume, using a TTL Counter; class #5 Event Sequencing, using in-model Dictionary state or a TTL Counter read across
linked models; class #7 Unstructured Text Screening, using String extensions against a List of terms.
Read [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Unstructured Text Screening](RuleTaxonomyTypes/UnstructuredTextScreening.html).

### Drug trafficking proceeds

**Signature:** cash deposits in border towns/major hubs; funnel accounts; drug slang in narratives; use of prepaid cards
and couriers.

**Rules:** funnel patterns (2/8); sub-threshold deposits (9); text terms (7); high-risk corridors (6).

In Jube: class #2 Cardinality (Same and Different), using an Abstraction Rule with a Same or Distinct Count; class #8
Geospatial and Context, using Double geospatial extensions on Payload coordinates; class #9 Structuring or Threshold
Avoidance, using threshold extensions IsJustBelowThreshold and IsRoundAmount, repeated through class 1 or 2; class #7
Unstructured Text Screening, using String extensions against a List of terms; class #6 Static and Match Screening, using
a List, or a Sanction for regulatory lists.
Read [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Geospatial and Context](RuleTaxonomyTypes/GeospatialAndContext.html), [Structuring or Threshold Avoidance](RuleTaxonomyTypes/StructuringOrThresholdAvoidance.html), [Unstructured Text Screening](RuleTaxonomyTypes/UnstructuredTextScreening.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html).

### Environmental crime / illegal wildlife and logging

**Rules:** shipping route and goods text (7); high-risk jurisdictions (6); front-company patterns (3); trade invoice
mismatches (4).

In Jube: class #7 Unstructured Text Screening, using String extensions against a List of terms; class #6 Static and
Match Screening, using a List, or a Sanction for regulatory lists; class #3 Link and Network, using an Abstraction Rule
with the Search Key inverted to the shared trait; class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and
Abstraction Calculation.
Read [Unstructured Text Screening](RuleTaxonomyTypes/UnstructuredTextScreening.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html).

### Human trafficking and modern slavery

**Signature:** hotel/travel spend; many-adult accommodation; multiple customers at one address; cash deposits;
classified-ad payments; low-value payments to many individuals.

**Rules:** many customers at one address (3); hotel and adult-service MCC spend (6); cash deposits via funnel (2);
narrative terms (7); one device used for many accounts (3).

In Jube: class #3 Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #6
Static and Match Screening, using a List, or a Sanction for regulatory lists; class #2 Cardinality (Same and Different),
using an Abstraction Rule with a Same or Distinct Count; class #7 Unstructured Text Screening, using String extensions
against a List of terms.
Read [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Unstructured Text Screening](RuleTaxonomyTypes/UnstructuredTextScreening.html).

### Fraud against government schemes (benefits, tax refund, COVID loan abuse)

**Rules:** multiple applications per device/address (3); bank accounts shared across claimants (3); funds immediately
forwarded (5); shell business claims (6).

In Jube: class #3 Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #5
Event Sequencing, using in-model Dictionary state or a TTL Counter read across linked models; class #6 Static and Match
Screening, using a List, or a Sanction for regulatory lists.
Read [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html).

### Insider trading and market abuse (as financial-crime predicate)

**Rules:** trades ahead of news (5); account connections to insiders (3); abnormal profit ratios (4).

In Jube: class #5 Event Sequencing, using in-model Dictionary state or a TTL Counter read across linked models; class #3
Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #4 Behavioral
Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation.
Read [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html).

### Proliferation financing

**Signature:** dual-use goods, front companies, transshipment hubs, sanctioned-country links.

**Rules:** dual-use text and HS codes (6/7); routing via hubs (8); counterparties linked to listed entities (3/6);
round-value trade payments (9).

In Jube: class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists; class #7 Unstructured
Text Screening, using String extensions against a List of terms; class #8 Geospatial and Context, using Double
geospatial extensions on Payload coordinates; class #3 Link and Network, using an Abstraction Rule with the Search Key
inverted to the shared trait; class #9 Structuring or Threshold Avoidance, using threshold extensions
IsJustBelowThreshold and IsRoundAmount, repeated through class 1 or 2.
Read [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Unstructured Text Screening](RuleTaxonomyTypes/UnstructuredTextScreening.html), [Geospatial and Context](RuleTaxonomyTypes/GeospatialAndContext.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Structuring or Threshold Avoidance](RuleTaxonomyTypes/StructuringOrThresholdAvoidance.html).

Check: the dual-use and export-control lists, and the HS code scope, are set by export-control authorities. A human must
confirm which lists apply.

### Sanctions evasion

**Signature:** shell companies in third countries; stripped payment messages; gold/crypto/commodity routes; ship-to-ship
transfers; ownership layers just under 50%.

**Rules:** name/alias/owner screening (6); stripped messages (6); transshipment hubs (6/8); ownership layering (6/3);
vessel/AIS-gap indicators (8); crypto exposure to sanctioned wallets (6).

In Jube: class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists; class #8 Geospatial and
Context, using Double geospatial extensions on Payload coordinates; class #3 Link and Network, using an Abstraction Rule
with the Search Key inverted to the shared trait.
Read [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Geospatial and Context](RuleTaxonomyTypes/GeospatialAndContext.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html).

Check: the 50 percent ownership threshold and any aggregation rule that applies to it depend on the sanctions regime you
are subject to. A human must confirm the threshold and its scope.

### Tax evasion

**Signature:** offshore transfers; cash-based undeclared income; round-tripping; third-party payments; false invoicing.

**Rules:** offshore corridors (6); unreported cash patterns (4); invoice anomalies (4/7); narrative terms (7).

In Jube: class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists; class #4 Behavioral
Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #7 Unstructured Text Screening, using
String extensions against a List of terms.
Read [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Unstructured Text Screening](RuleTaxonomyTypes/UnstructuredTextScreening.html).

### Terrorist financing (TF)

**Signature:** many low-value transfers; NPO/charity channels; crowdfunding; travel and equipment purchases; payments to
conflict-zone adjacents; use of hawala and prepaid cards.

**Rules:** low-value, many-recipient fan-out (1/2); conflict-region geofence (8); TF watchlists (6); NPO payments
inconsistent with purpose (4); crypto micro-donations (1); text terms (7).

In Jube: class #1 Velocity and Volume, using a TTL Counter; class #2 Cardinality (Same and Different), using an
Abstraction Rule with a Same or Distinct Count; class #8 Geospatial and Context, using Double geospatial extensions on
Payload coordinates; class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists; class #4
Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #7 Unstructured Text
Screening, using String extensions against a List of terms.
Read [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Geospatial and Context](RuleTaxonomyTypes/GeospatialAndContext.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Unstructured Text Screening](RuleTaxonomyTypes/UnstructuredTextScreening.html).

Check: which terrorist financing watchlists apply to you, and your screening obligations against them, must be confirmed
by a human.

## G4. Digital and Technical Fraud Concepts

### Behavioural biometrics

**Definition:** Passive analysis of how users interact (typing rhythm, mouse, touch pressure, swipe, device
orientation).

**Rules:** score deviation (4/C3); sudden shift across sessions (4); non-human cadence (4); copy-paste of credentials
(4); remote-control indicators (6).

In Jube: class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; cross-cutting C3
Generalisation, Adaptation and Model Thresholds, using a model score thresholded in an Activation Rule (not a numbered
class); class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists.
Read [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Generalisation, Adaptation and Model Thresholds](GeneralisationAdaptationAndModelThresholds.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html).

### Bot / automation traffic

**Rules:** API/request rate (1); user-agent patterns (6); form-fill time (1); identical timings (2); headless browser
flag (6).

In Jube: class #1 Velocity and Volume, using a TTL Counter; class #6 Static and Match Screening, using a List, or a
Sanction for regulatory lists; class #2 Cardinality (Same and Different), using an Abstraction Rule with a Same or
Distinct Count.
Read [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html).

### Device binding and fingerprinting

**Definition:** Associating a device with an account and tracking hardware/software attributes.

**Rules:** first-use of device (2); devices per account (2); accounts per device (3); fingerprint blacklist (6);
fingerprint change at same device (4).

In Jube: class #2 Cardinality (Same and Different), using an Abstraction Rule with a Same or Distinct Count; class #3
Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #6 Static and Match
Screening, using a List, or a Sanction for regulatory lists; class #4 Behavioral Baseline and Ratio, using an
Abstraction Rule and Abstraction Calculation.
Read [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html).

### Emulator / rooted / jailbroken device

**Rules:** environment flags (6); emulator signature across customers (3); sensor anomalies (8).

In Jube: class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists; class #3 Link and
Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #8 Geospatial and Context,
using Double geospatial extensions on Payload coordinates.
Read [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Geospatial and Context](RuleTaxonomyTypes/GeospatialAndContext.html).

### IP intelligence (proxy, VPN, TOR, hosting, residential proxy)

**Rules:** TOR/VPN/proxy flags (6); hosting ASN (6); IP/GPS mismatch (8); IP reputation (6); IP shared across customers
(3).

In Jube: class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists; class #8 Geospatial and
Context, using Double geospatial extensions on Payload coordinates; class #3 Link and Network, using an Abstraction Rule
with the Search Key inverted to the shared trait.
Read [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Geospatial and Context](RuleTaxonomyTypes/GeospatialAndContext.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html).

### Impossible travel — see class 8.

### Overlay and screen-capture malware (banking trojans)

**Rules:** overlay indicators from app SDK (6); sudden remote sessions (6); atypical click patterns (4); payments after
malware detection signal (5).

In Jube: class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists; class #4 Behavioral
Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #5 Event Sequencing, using in-model
Dictionary state or a TTL Counter read across linked models.
Read [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html).

### Session hijacking / token theft

**Rules:** same session across IPs/devices (2); user-agent change mid-session (4); location jump mid-session (8).

In Jube: class #2 Cardinality (Same and Different), using an Abstraction Rule with a Same or Distinct Count; class #4
Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #8 Geospatial and Context,
using Double geospatial extensions on Payload coordinates.
Read [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Geospatial and Context](RuleTaxonomyTypes/GeospatialAndContext.html).

### Strong customer authentication (SCA) abuse and exemption gaming

**Rules:** payments just below exemption limits (9); abuse of trusted-beneficiary exemption (5); recurring exemptions
after payee addition (5).

In Jube: class #9 Structuring or Threshold Avoidance, using threshold extensions IsJustBelowThreshold and IsRoundAmount,
repeated through class 1 or 2; class #5 Event Sequencing, using in-model Dictionary state or a TTL Counter read across
linked models.
Read [Structuring or Threshold Avoidance](RuleTaxonomyTypes/StructuringOrThresholdAvoidance.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html).

Check: the SCA exemption amounts and conditions are set by the applicable regulation. A human must confirm the current
values for your jurisdiction.

### Mobile wallet / token provisioning fraud

**Rules:** provisioning velocity (1); provisioning from new device (2/5); provisioning then high spend (5); OTP via SMS
to unauthorised numbers (5).

In Jube: class #1 Velocity and Volume, using a TTL Counter; class #2 Cardinality (Same and Different), using an
Abstraction Rule with a Same or Distinct Count; class #5 Event Sequencing, using in-model Dictionary state or a TTL
Counter read across linked models.
Read [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html).

### Open-banking and API abuse

**Rules:** third-party provider velocity (1); new consent then payment (5); consent from new device/IP (2); unusual
scopes (6).

In Jube: class #1 Velocity and Volume, using a TTL Counter; class #5 Event Sequencing, using in-model Dictionary state
or a TTL Counter read across linked models; class #2 Cardinality (Same and Different), using an Abstraction Rule with a
Same or Distinct Count; class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists.
Read [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html).

### Instant payment fraud

**Rules:** irrevocable payment velocity (1); first-time payee (2); credit-and-forward (5); recipient confirmation
mismatch (6).

In Jube: class #1 Velocity and Volume, using a TTL Counter; class #2 Cardinality (Same and Different), using an
Abstraction Rule with a Same or Distinct Count; class #5 Event Sequencing, using in-model Dictionary state or a TTL
Counter read across linked models; class #6 Static and Match Screening, using a List, or a Sanction for regulatory
lists.
Read [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html).

## G5. Customer and Entity Risk Concepts

### Beneficial owner (UBO)

**Rules:** UBO not disclosed (6); UBO on sanctions/PEP (6); same UBO across many entities (3); ownership layering (3).

In Jube: class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists; class #3 Link and
Network, using an Abstraction Rule with the Search Key inverted to the shared trait.
Read [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html).

### Customer due diligence (CDD) / enhanced due diligence (EDD)

**Rules:** transactions exceeding EDD thresholds without EDD completed (6/5); expired KYC (6); high-risk rating drift
(4).

In Jube: class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists; class #5 Event
Sequencing, using in-model Dictionary state or a TTL Counter read across linked models; class #4 Behavioral Baseline and
Ratio, using an Abstraction Rule and Abstraction Calculation.
Read [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html).

Check: EDD triggers, thresholds and KYC refresh periods are set by your regulator and policy. A human must confirm them.

### Dormant account

**Rules:** days since last activity (C1); reactivation sequence (5); first-large-transaction (4).

In Jube: cross-cutting C1 Identity and Demographic, using derived inputs such as age and tenure from Payload and
DateTime extensions (not a numbered class); class #5 Event Sequencing, using in-model Dictionary state or a TTL Counter
read across linked models; class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction
Calculation.
Read [Identity and Demographic](index.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html).

Check: the number of days after which an account counts as dormant is set by regulation or policy and differs by
jurisdiction. A human must confirm it.

### Expected activity profile (KYC profile)

**Rules:** activity vs declared turnover, countries, products (4/6).

In Jube: class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #6 Static
and Match Screening, using a List, or a Sanction for regulatory lists.
Read [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html).

### High-risk customer

**Rules:** risk rating used as conditional input in all classes (C1); stricter thresholds for high-risk (all).

In Jube: cross-cutting C1 Identity and Demographic, using derived inputs such as age and tenure from Payload and
DateTime extensions (not a numbered class). Read [Identity and Demographic](index.html).

Check: the high-risk criteria and rating scales follow your regulator's guidance and your own policy. A human must
confirm them.

### Know your transaction (KYT) / transaction monitoring

**Rules:** set of all classes applied in real-time or batch.

In Jube: all nine classes, #1 to #9, as one rule set, applied to each invocation in real time or to archived records by
Reprocessing in batch. Read [Rule Taxonomy](index.html)
and [Reprocessing](../Configuration/Reprocessing/index.html).

### Politically exposed person (PEP)

**Rules:** PEP/RCA screening (6); thresholds scaled by tier (C1); payments to/from PEP-linked entities (3/6);
source-of-wealth plausibility (4).

In Jube: class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists; cross-cutting C1
Identity and Demographic, using derived inputs such as age and tenure from Payload and DateTime extensions (not a
numbered class); class #3 Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait;
class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation.
Read [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Identity and Demographic](index.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html).

Check: the PEP and relative or close associate definitions and tiers are set by your jurisdiction. A human must confirm
them.

### Risk-based approach (RBA)

**Definition:** Calibrating controls to risk; implemented as segmented thresholds.

**Rules:** segment-based thresholds (4/C1).

In Jube: class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; cross-cutting C1
Identity and Demographic, using derived inputs such as age and tenure from Payload and DateTime extensions (not a
numbered class).
Read [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Identity and Demographic](index.html).

### Segment / peer group

**Rules:** baselines per peer group (4); alert tuning per segment (C3).

In Jube: class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; cross-cutting C3
Generalisation, Adaptation and Model Thresholds, using a model score thresholded in an Activation Rule (not a numbered
class).
Read [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Generalisation, Adaptation and Model Thresholds](GeneralisationAdaptationAndModelThresholds.html).

### Source of funds / source of wealth

**Rules:** credits inconsistent with declared source (4); large third-party credits (3/4); narrative vagueness (7).

In Jube: class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #3 Link
and Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #7 Unstructured Text
Screening, using String extensions against a List of terms.
Read [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Unstructured Text Screening](RuleTaxonomyTypes/UnstructuredTextScreening.html).

## G6. Monitoring, Detection and Rule-Engineering Terms

### Abstraction rule

**Definition:** Rule that aggregates data over a window by key (count, sum, average, distinct count) producing a derived
variable for downstream rules.

**Used by:** classes 1, 2, 4, 8.

In Jube: the Abstraction Rules construct, used for classes #1, #2, #3, #4 and #8.
Read [Abstraction Rules](../Configuration/Models/AbstractionRules/index.html) and
the [Rule Taxonomy](index.html).

### Alert, case, SAR/STR

**Definition:** Rule trigger → investigation unit → regulatory report (suspicious activity/transaction report).

**Rule engineering:** every rule should map to a typology, a risk, and an expected disposition.

In Jube: no numbered class. Triggered alerts become cases in Case Management.
Read [Case Management](../Configuration/CaseManagement/index.html).

Check: suspicious activity or transaction report obligations, deadlines, form and receiving authority are set by your
regulator. A human must confirm them before any filing workflow relies on them.

### Allow-list / whitelist and suppression

**Use:** reducing false positives for known-good parties/devices/merchants. Class 6.

In Jube: class #6 reference data, plus the Override surface, which suppresses an Activation Rule for a chosen key, with
an optional Delete Expiry Date, and does not suppress TTL Counter increments.
Read [Activation Rules Override](../Configuration/Models/ActivationRulesOverride/index.html)
and [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html).

### Baseline

**Definition:** Expected behaviour of an entity (or peer) over a lookback window. Class 4.

In Jube: class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation.
Read [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html).

### Behavioural profiling

**Definition:** Entity's historic profile of velocity, amounts, counterparties, channels.

**Rules:** deviation rules (4).

In Jube: class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation.
Read [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html).

### Below-the-line testing / above-the-line testing

**Definition:** Testing thresholds below and above the live setting to assess missed risk versus workload.

**Use:** threshold tuning (C3).

In Jube: cross-cutting C3, not a numbered class. Backtest runs a rule as it stands in the editor over archived
transactions, which is where a threshold is tried before it goes live.
Read [Backtest](../Configuration/Models/Backtest/index.html).

### Challenge / step-up authentication

**Definition:** Response code asking for extra verification (OTP, biometrics, call-back) rather than hard decline.

**Use:** rule outcomes for medium-risk fraud.

In Jube: no numbered class. The docs name no challenge outcome: a rule reports a numeric Response Elevation, and
Response Elevation Content can carry the customer message.
Read [Activation Rules with Response Elevation](../Configuration/Models/ResponseElevation/index.html).

### Champion/challenger

**Definition:** Testing a new rule/model alongside current one in shadow.

In Jube: no numbered class. Challenger models are trained inside Exhaustive Adaptation
(ExhaustiveChallengerModelBaseProbability), and a rule can run alongside the live rule with Response Elevation switched
off (see Shadow mode). Read [Exhaustive Adaptation Concepts](../Configuration/ExhaustiveAdaptation/Concepts/index.html).

### Cooling-off period

**Definition:** Delay (hours/days) before a new payee/device can be used for large payments. Rule input: days since
beneficiary added (C1).

In Jube: cross-cutting C1 for the days-since-added input, a derived DateTime value.
Read [Rule Taxonomy](index.html).

### Decay / lookback window

**Definition:** Window length and weight given to old events (TTL counter).

In Jube: class #1, the TTL Counter window. An Online Aggregation counter covers the window ending at the transaction's
Reference Date, other counters keep a running count, and no decay weighting is applied.
Read [TTL Counters](../Configuration/Models/TTLCounters/index.html).

### Detection rate, false-positive rate, alert-to-case ratio, value detection rate (VDR), precision

**Definition:** Effectiveness metrics for tuning rules.

### Entity key / search key

**Definition:** The attribute against which an abstraction is computed (account, card, device, IP, beneficiary). Link
rules invert the key to the shared trait (class 3).

In Jube: the Search Key of an Abstraction Rule, defined in the Request XPath. Class #2 keys on the entity and class #3
inverts the key to the shared trait. Read [Abstraction Rules](../Configuration/Models/AbstractionRules/index.html).

### First-seen / new-to-entity flag

**Definition:** Whether attribute value (device, IP, payee, country) was seen before for the entity. Class 2.

In Jube: class #2, where a first-seen check is a Cardinality rule over the entity key.
Read [Cardinality](RuleTaxonomyTypes/Cardinality.html).

### Hotlist / blocklist (dynamic)

**Definition:** Entities placed on a list with expiry following a rule hit (e.g. session or device). Class 6 reference
data.

In Jube: class #6 reference data. Lists take a per-value Delete Expiry Date and are maintained outside the rule, by the
Lists page, CSV, database or API. A time-limited hit that events write themselves is a TTL Counter.
Read [Lists](../Configuration/Models/Lists/index.html)
and [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html).

### Link analysis / graph analytics

**Definition:** Network connection analysis; rules in class 3 with scoring in C3.

In Jube: class #3 Link and Network, with model scoring under cross-cutting C3.
Read [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html).

### Look-alike / fuzzy matching

**Definition:** Approximate string comparison (edit distance, phonetic, token-set). Class 6/7.

In Jube: classes #6 and #7. Sanction matching uses Levenshtein distance.
Read [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Unstructured Text Screening](RuleTaxonomyTypes/UnstructuredTextScreening.html)
and [Sanctions](../Configuration/Sanctions/index.html).

### Model (supervised, unsupervised, graph)

**Definition:** Statistical scores; thresholded in rules (C3).

In Jube: cross-cutting C3, not a numbered class. Exhaustive, HTTP Adaptation and Advanced Analytics scores are
thresholded in an Activation Rule.
Read [Generalisation, Adaptation and Model Thresholds](GeneralisationAdaptationAndModelThresholds.html).

### Near-time vs real-time vs batch

**Definition:** Real-time (pre-authorisation, in-flight), near-time (minutes after), batch (daily/periodic reviews, e.g.
KYC, rescreening).

In Jube: Activation Rules run on each invocation, and Reprocessing re-runs rules over archived records as a batch. The
docs name no separate near-time mode. Read [Reprocessing](../Configuration/Reprocessing/index.html)
and [Activation Rules](../Configuration/Models/BasicActivationRules/index.html).

### Scenario / typology coverage mapping

**Definition:** Matrix linking rules to typologies, channels, products and risks to demonstrate regulatory coverage.

### Threshold tuning (statistical, percentile-based)

**Definition:** Choosing thresholds using distribution percentiles, standard deviations, or ROC/precision-recall
analysis.

In Jube: cross-cutting C3, not a numbered class. Backtest runs a rule over archived transactions to show how a threshold
would have done. Read [Backtest](../Configuration/Models/Backtest/index.html).

### Velocity

**Definition:** Count or value of events in a time window. Class 1.

In Jube: class #1, built on TTL Counters incremented from an Activation Rule.
Read [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html)
and [TTL Counters](../Configuration/Models/TTLCounters/index.html).

### Cardinality

**Definition:** Count of distinct (or identical) values. Class 2.

In Jube: class #2, built on Abstraction Rules with a Same or Distinct Count rather than TTL Counters.
Read [Cardinality](RuleTaxonomyTypes/Cardinality.html).

### Time-to-live (TTL) counter

**Definition:** Counter that expires entries after the window; basis of velocity and cardinality rules.

In Jube: the TTL Counters construct, used by classes #1, #5 and #9.
Read [TTL Counters](../Configuration/Models/TTLCounters/index.html).

### Typology

**Definition:** A recognised method/pattern of committing financial crime; rules are examples of a typology expressed in
data.

## G7. Additional Typologies and Terms (second pass)

### Affiliate fraud

**Definition:** Affiliates generate fake sign-ups, deposits or clicks to earn commission.

**Rules:** sign-ups per affiliate sharing device/IP (3); low wagering/activity after bonus (4); refunds/chargebacks per
affiliate (1/4).

In Jube: class #3 Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #4
Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #1 Velocity and Volume,
using a TTL Counter.
Read [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html).

### Airtime laundering

**Definition:** Converting cash to airtime credit, transferring and reselling it as a value-transfer channel.

**Rules:** airtime top-up and transfer velocity (1); airtime share of wallet activity (4); transfers to many numbers
(2).

In Jube: class #1 Velocity and Volume, using a TTL Counter; class #4 Behavioral Baseline and Ratio, using an Abstraction
Rule and Abstraction Calculation; class #2 Cardinality (Same and Different), using an Abstraction Rule with a Same or
Distinct Count.
Read [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html).

### Authorised-user piggybacking

**Definition:** Adding a synthetic or third party as an authorised user to inherit credit history.

**Rules:** many authorised users per primary (2); authorised users sharing attributes with other synthetics (3); rapid
credit requests after addition (5).

In Jube: class #2 Cardinality (Same and Different), using an Abstraction Rule with a Same or Distinct Count; class #3
Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #5 Event Sequencing,
using in-model Dictionary state or a TTL Counter read across linked models.
Read [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html).

### Black-market peso exchange (BMPE)

**Definition:** Trade-based scheme where brokers use drug dollars to pay for goods exported to a cartel's home country,
settling in local currency.

**Rules:** third-party payers for trade invoices (3); payments from unrelated US entities to exporters (6/3);
invoice-to-goods mismatches (4); structured cash deposits upstream (9).

In Jube: class #3 Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #6
Static and Match Screening, using a List, or a Sanction for regulatory lists; class #4 Behavioral Baseline and Ratio,
using an Abstraction Rule and Abstraction Calculation; class #9 Structuring or Threshold Avoidance, using threshold
extensions IsJustBelowThreshold and IsRoundAmount, repeated through class 1 or 2.
Read [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Structuring or Threshold Avoidance](RuleTaxonomyTypes/StructuringOrThresholdAvoidance.html).

Check: the reporting obligations and the jurisdiction-specific framing of this scheme must be confirmed by a human
against the regulations that apply to you.

### Bonus abuse / promo abuse

**Definition:** Exploiting sign-up or deposit bonuses via multiple accounts or minimal play.

**Rules:** accounts per device/IP/payment method (2/3); bonus claim velocity (1); minimal wagering sequence (5); win
rate anomalies (4).

In Jube: class #2 Cardinality (Same and Different), using an Abstraction Rule with a Same or Distinct Count; class #3
Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #1 Velocity and
Volume, using a TTL Counter; class #5 Event Sequencing, using in-model Dictionary state or a TTL Counter read across
linked models; class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation.
Read [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html).

### Card cracking / card testing — see BIN attack (G2).

### Chargeback fraud (merchant-side)

**Definition:** Merchant manipulates chargeback ratios (MID cycling, refund abuse, load balancing) to stay under scheme
programmes.

**Rules:** MID cycling (2/9); ratio hugging the threshold (9); refund-to-sales ratio (4).

In Jube: class #2 Cardinality (Same and Different), using an Abstraction Rule with a Same or Distinct Count; class #9
Structuring or Threshold Avoidance, using threshold extensions IsJustBelowThreshold and IsRoundAmount, repeated through
class 1 or 2; class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation.
Read [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Structuring or Threshold Avoidance](RuleTaxonomyTypes/StructuringOrThresholdAvoidance.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html).

Check: card scheme chargeback and fraud monitoring thresholds change over time. A human must confirm the current values
with the scheme before a ratio rule is set.

### Cheque kiting

**Definition:** Exploiting float by depositing cheques between accounts to create artificial balances.

**Rules:** deposit → withdrawal before clearing → return (5); cheque deposits between linked accounts (3);
returned-cheque velocity (1).

In Jube: class #5 Event Sequencing, using in-model Dictionary state or a TTL Counter read across linked models; class #3
Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #1 Velocity and
Volume, using a TTL Counter.
Read [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html).

### Chip dumping

**Definition:** Deliberately losing chips to another player in poker to transfer value.

**Rules:** repeated losses to one opponent (2/5); opponent withdraws immediately (5); shared device/IP between the
players (3); win rate implausible (4).

In Jube: class #2 Cardinality (Same and Different), using an Abstraction Rule with a Same or Distinct Count; class #5
Event Sequencing, using in-model Dictionary state or a TTL Counter read across linked models; class #3 Link and Network,
using an Abstraction Rule with the Search Key inverted to the shared trait; class #4 Behavioral Baseline and Ratio,
using an Abstraction Rule and Abstraction Calculation.
Read [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html).

### Clipper malware

**Definition:** Malware that swaps a copied crypto address with the attacker's.

**Rules:** clipboard address differs from pasted (context/8); first-time address with high value (2); device compromise
flags (6).

In Jube: class #8 Geospatial and Context, using Double geospatial extensions on Payload coordinates; class #2
Cardinality (Same and Different), using an Abstraction Rule with a Same or Distinct Count; class #6 Static and Match
Screening, using a List, or a Sanction for regulatory lists.
Read [Geospatial and Context](RuleTaxonomyTypes/GeospatialAndContext.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html).

### Collusion fraud (platforms)

**Definition:** Buyers and sellers, drivers and riders, or players coordinating to extract value or launder.

**Rules:** shared device/address between counterparties (3); bipartite clusters (3); implausible trip/fare ratios (4);
self-purchase (3).

In Jube: class #3 Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #4
Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation.
Read [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html).

### Dusting attack

**Definition:** Sending tiny crypto amounts to many addresses to de-anonymise wallets.

**Rules:** micro inbound count (1); subsequent spend consolidating dust (5).

In Jube: class #1 Velocity and Volume, using a TTL Counter; class #5 Event Sequencing, using in-model Dictionary state
or a TTL Counter read across linked models.
Read [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html).

### Dark-web / darknet market exposure

**Definition:** Wallet proximity to darknet marketplaces.

**Rules:** address attribution (6); N-hop exposure (3); tainted-share ratio (4).

In Jube: class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists; class #3 Link and
Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #4 Behavioral Baseline and
Ratio, using an Abstraction Rule and Abstraction Calculation.
Read [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html).

### Deepfake / injection attack

**Definition:** Synthetic media or injected camera feed used to defeat identity verification or voice authentication.

**Rules:** liveness and injection detection (6); same face/voice across identities (3); selfie-document mismatch (6).

In Jube: class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists; class #3 Link and
Network, using an Abstraction Rule with the Search Key inverted to the shared trait.
Read [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html).

### Ghost employee / ghost vendor

**Definition:** Fictitious employee or vendor created to divert payments.

**Rules:** shared bank accounts across employees/vendors (2/3); vendor equals employee attributes (3); payments to
vendors not in master (6); vendor with no PO history (4).

In Jube: class #2 Cardinality (Same and Different), using an Abstraction Rule with a Same or Distinct Count; class #3
Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #6 Static and Match
Screening, using a List, or a Sanction for regulatory lists; class #4 Behavioral Baseline and Ratio, using an
Abstraction Rule and Abstraction Calculation.
Read [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html).

### Gift-card laundering / gift-card draining

**Definition:** Converting illicit value via gift cards; or stealing card balances through enumeration/tampering.

**Rules:** gift-card purchase velocity (1); balance-check velocity (1); same device buying gift cards across accounts
(3); purchase then redemption sequence (5).

In Jube: class #1 Velocity and Volume, using a TTL Counter; class #3 Link and Network, using an Abstraction Rule with
the Search Key inverted to the shared trait; class #5 Event Sequencing, using in-model Dictionary state or a TTL Counter
read across linked models.
Read [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html).

### Interactive-voice / IVR fraud, IRSF (International revenue share fraud)

**Definition:** Telecom fraud generating calls to premium numbers to earn revenue share.

**Rules:** premium-rate call velocity (1); SIM activation per retailer (1); calls from newly activated SIMs (5).

In Jube: class #1 Velocity and Volume, using a TTL Counter; class #5 Event Sequencing, using in-model Dictionary state
or a TTL Counter read across linked models.
Read [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html).

### Item-not-received (INR) / return fraud

**Definition:** False claims of non-delivery or returning empty/incorrect goods for refunds.

**Rules:** claim rate vs baseline (4); INR velocity (1); reorder after claim (5); return address shared (3).

In Jube: class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #1
Velocity and Volume, using a TTL Counter; class #5 Event Sequencing, using in-model Dictionary state or a TTL Counter
read across linked models; class #3 Link and Network, using an Abstraction Rule with the Search Key inverted to the
shared trait.
Read [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html).

### Loyalty / rewards fraud

**Definition:** Account takeover or abuse to drain points, or earning points via fake transactions.

**Rules:** redemption velocity after login from new device (5); points earned via refunded purchases (5); multiple
accounts per device (3).

In Jube: class #5 Event Sequencing, using in-model Dictionary state or a TTL Counter read across linked models; class #3
Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait.
Read [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html).

### Matched betting / arbitrage abuse

**Definition:** Exploiting free bets and odds differences; often a bonus-abuse variant.

**Rules:** bet patterns covering all outcomes across accounts (3); minimal net wagering (4); bonus-driven deposits (5).

In Jube: class #3 Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #4
Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #5 Event Sequencing, using
in-model Dictionary state or a TTL Counter read across linked models.
Read [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html).

### Multi-accounting (gnoming)

**Definition:** One person operating many accounts on a platform.

**Rules:** accounts per device/IP/fingerprint (2/3); identical behaviour timings (2); shared payment methods (3).

In Jube: class #2 Cardinality (Same and Different), using an Abstraction Rule with a Same or Distinct Count; class #3
Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait.
Read [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html).

### NFT wash trading

**Definition:** Selling NFTs between controlled wallets to inflate price or move value.

**Rules:** buyer-seller wallet linkage (3); repeated round-trips of the same token (2/5); price out of line with
collection (4).

In Jube: class #3 Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #2
Cardinality (Same and Different), using an Abstraction Rule with a Same or Distinct Count; class #5 Event Sequencing,
using in-model Dictionary state or a TTL Counter read across linked models; class #4 Behavioral Baseline and Ratio,
using an Abstraction Rule and Abstraction Calculation.
Read [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html).

### Pump-and-dump

**Definition:** Promoting a thinly traded asset then selling into the hype.

**Rules:** promotional text correlated with buys (7); concentrated holdings then sells (4/5); accounts linked to
promoter (3); certificate deposits (1).

In Jube: class #7 Unstructured Text Screening, using String extensions against a List of terms; class #4 Behavioral
Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #5 Event Sequencing, using in-model
Dictionary state or a TTL Counter read across linked models; class #3 Link and Network, using an Abstraction Rule with
the Search Key inverted to the shared trait; class #1 Velocity and Volume, using a TTL Counter.
Read [Unstructured Text Screening](RuleTaxonomyTypes/UnstructuredTextScreening.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html).

### Rug pull

**Definition:** Crypto project developers abandon the project and remove liquidity.

**Rules:** liquidity removal event (5); token on scam list (6); concentration of supply in few wallets (4/3).

In Jube: class #5 Event Sequencing, using in-model Dictionary state or a TTL Counter read across linked models; class #6
Static and Match Screening, using a List, or a Sanction for regulatory lists; class #4 Behavioral Baseline and Ratio,
using an Abstraction Rule and Abstraction Calculation; class #3 Link and Network, using an Abstraction Rule with the
Search Key inverted to the shared trait.
Read [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html).

### SIM-box / bypass fraud

**Definition:** Routing international calls through local SIMs to avoid interconnect fees.

**Rules:** bulk SIM activations (1); SIM usage patterns (4); IMEI with many SIMs (2).

In Jube: class #1 Velocity and Volume, using a TTL Counter; class #4 Behavioral Baseline and Ratio, using an Abstraction
Rule and Abstraction Calculation; class #2 Cardinality (Same and Different), using an Abstraction Rule with a Same or
Distinct Count.
Read [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html).

### Spoofing / layering (market abuse)

**Definition:** Placing orders with intent to cancel to mislead the market.

**Rules:** order-to-trade ratio (4); cancel velocity (1); order placement/cancel sequences near the touch (5).

In Jube: class #4 Behavioral Baseline and Ratio, using an Abstraction Rule and Abstraction Calculation; class #1
Velocity and Volume, using a TTL Counter; class #5 Event Sequencing, using in-model Dictionary state or a TTL Counter
read across linked models.
Read [Behavioral Baseline and Ratio](RuleTaxonomyTypes/BehavioralBaselineAndRatio.html), [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html).

### Staged accident / crash-for-cash

**Definition:** Deliberately caused collisions to claim insurance.

**Rules:** shared participants across claims (3); claim shortly after inception (1/5); narrative templates (7); location
hotspots (8).

In Jube: class #3 Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait; class #1
Velocity and Volume, using a TTL Counter; class #5 Event Sequencing, using in-model Dictionary state or a TTL Counter
read across linked models; class #7 Unstructured Text Screening, using String extensions against a List of terms; class
#8 Geospatial and Context, using Double geospatial extensions on Payload coordinates.
Read [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html), [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Unstructured Text Screening](RuleTaxonomyTypes/UnstructuredTextScreening.html), [Geospatial and Context](RuleTaxonomyTypes/GeospatialAndContext.html).

### Stacking (BNPL/loan stacking)

**Definition:** Taking many loans or BNPL plans across lenders simultaneously.

**Rules:** enquiry/application velocity (1); accounts per device (2); consortium hits (6); first instalment then default
(5).

In Jube: class #1 Velocity and Volume, using a TTL Counter; class #2 Cardinality (Same and Different), using an
Abstraction Rule with a Same or Distinct Count; class #6 Static and Match Screening, using a List, or a Sanction for
regulatory lists; class #5 Event Sequencing, using in-model Dictionary state or a TTL Counter read across linked models.
Read [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html).

### Telematics manipulation

**Definition:** Altering GPS/device data to defeat insurance or platform controls.

**Rules:** GPS spoof flags (8); trace inconsistencies (8); sensor anomalies (8).

In Jube: class #8 Geospatial and Context, using Double geospatial extensions on Payload coordinates.
Read [Geospatial and Context](RuleTaxonomyTypes/GeospatialAndContext.html).

### Triangulation fraud — see G2.

### Vendor-master fraud

**Definition:** Manipulating supplier records (bank details) to divert payments.

**Rules:** bank-change then payment (5); changes by one user velocity (1); lookalike vendor names (7); change outside
business hours (C1).

In Jube: class #5 Event Sequencing, using in-model Dictionary state or a TTL Counter read across linked models; class #1
Velocity and Volume, using a TTL Counter; class #7 Unstructured Text Screening, using String extensions against a List
of terms; cross-cutting C1 Identity and Demographic, using derived inputs such as age and tenure from Payload and
DateTime extensions (not a numbered class).
Read [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Velocity and Volume](RuleTaxonomyTypes/VelocityAndVolume.html), [Unstructured Text Screening](RuleTaxonomyTypes/UnstructuredTextScreening.html), [Identity and Demographic](index.html).

### Wallet tier gaming

**Definition:** Keeping e-money/mobile wallet activity below verification tiers.

**Rules:** cap-hugging (9); sweeps to keep balance under ceiling (9/5); many wallets per person (2/3).

In Jube: class #9 Structuring or Threshold Avoidance, using threshold extensions IsJustBelowThreshold and IsRoundAmount,
repeated through class 1 or 2; class #5 Event Sequencing, using in-model Dictionary state or a TTL Counter read across
linked models; class #2 Cardinality (Same and Different), using an Abstraction Rule with a Same or Distinct Count; class
#3 Link and Network, using an Abstraction Rule with the Search Key inverted to the shared trait.
Read [Structuring or Threshold Avoidance](RuleTaxonomyTypes/StructuringOrThresholdAvoidance.html), [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html), [Cardinality (Same and Different)](RuleTaxonomyTypes/Cardinality.html), [Link and Network](RuleTaxonomyTypes/LinkAndNetwork.html).

Check: e-money wallet balance ceilings and verification tiers are set by your regulator. A human must confirm the
current values.

## G8. Rule-Outcome and Response Terms

### Accept / Challenge / Decline / Hold / Review

**Definition:** Standard real-time outcomes. Challenge variants: OTP, push, biometric, call-back, device confirmation.
Hold: delay payment pending review (common for APP scam risk). Review: route to a queue without blocking.

In Jube: no numbered class. The docs express outcomes as a numeric Response Elevation on Activation Rules, which the
calling channel maps to accept, challenge, decline, hold or review.
Read [Activation Rules with Response Elevation](../Configuration/Models/ResponseElevation/index.html).

### Alert suppression / de-duplication

**Definition:** Avoiding repeat alerts for the same entity and pattern within a cooling window; implemented via hotlists
or dictionaries holding last alert time.

In Jube: no numbered class. Override suppression with an optional Delete Expiry Date, and TTL Counter increments are not
suppressed. Read [Activation Rules Override](../Configuration/Models/ActivationRulesOverride/index.html).

### Case priority / triage score

**Definition:** Ordering of alerts by risk and value; usually a weighted sum of rule severities plus model score (C3).

In Jube: no numbered class. Case workflow actions carry a priority of High, Medium or Low, and activations are listed by
Response Elevation, highest first. Severity-weighted sums are built with pipeline AddScore.
Read [Creating Case Workflow Action](../Configuration/CaseManagement/CreatingCaseWorkflowAction/index.html), [Working Activations](../Configuration/CaseManagement/WorkingActivations/index.html)
and [Rule Authoring Methodology](RuleAuthoringMethodology.html).

### Confirmation of payee (CoP) / name matching at payment

**Definition:** Verifying beneficiary name against account; mismatch is a class 6 screening signal and a scam indicator.

In Jube: class #6 Static and Match Screening, using a List, or a Sanction for regulatory lists.
Read [Static and Match Screening](RuleTaxonomyTypes/StaticAndMatchScreening.html).

### Customer warning / interstitial

**Definition:** Scam-education prompt before payment; "acknowledged warning then paid" is a class 5 input.

In Jube: class #5 input, with the warning text carried as Response Elevation Content and an optional Response Elevation
Redirect. Read [Basic Activation Rules](../Configuration/Models/BasicActivationRules/index.html)
and [Event Sequencing](RuleTaxonomyTypes/EventSequencing.html).

### Hotlist expiry

**Definition:** Time-bound listing of a device/session/account after a hit (e.g. 2 hours) to tighten subsequent rules.

In Jube: class #6 reference data whose values take a Delete Expiry Date, and an Override switch takes one too.
Read [Lists](../Configuration/Models/Lists/index.html)
and [Activation Rules Override](../Configuration/Models/ActivationRulesOverride/index.html).

### Payment delay / cooling period for new payees

**Definition:** Holding the first payment to a new payee or after credential change for a period; the delay state is a
C1 variable.

In Jube: cross-cutting C1 for the delay-state input. Read [Rule Taxonomy](index.html).

### Queue / workflow

**Definition:** Investigation destinations (fraud ops, AML, mule, sanctions, EDD); each rule maps to one.

In Jube: no numbered class. Case workflows are the closest construct to a queue.
Read [Creating Case Workflows](../Configuration/CaseManagement/CreatingCaseWorkflows/index.html).

### Response code mapping

**Definition:** Translating rule outcomes into channel-specific codes (auth response, non-monetary event response).

In Jube: no numbered class. The Response Elevation value is returned in the response payload with its Content and
Redirect, and mapping it to channel codes is left to the caller.
Read [Activation Rules with Response Elevation](../Configuration/Models/ResponseElevation/index.html).

### Rule priority / tie-break

**Definition:** When several rules fire, the highest priority determines the outcome (decline beats challenge beats
accept).

In Jube: no numbered class. Priority on Basic Activation Rules orders evaluation for Activation Rule chaining. Response
Elevation can only be raised, so the highest value wins when several rules match.
Read [Basic Activation Rules](../Configuration/Models/BasicActivationRules/index.html)
and [Activation Rules with Response Elevation](../Configuration/Models/ResponseElevation/index.html).

### Severity weighting / score aggregation

**Definition:** Summing weighted rule hits into an overall score with bands; allows soft signals to combine.

In Jube: no numbered class. Pipelines accumulate scores with AddScore and CapScore, and decide on them with
MatchIfScoreAtLeast. Read [Rule Authoring Methodology](RuleAuthoringMethodology.html).

### Shadow mode / silent rule

**Definition:** Rule runs and logs but does not act; used for tuning and champion/challenger.

In Jube: no numbered class. An Activation Rule with Response Elevation switched off still matches, but raises no
Response Elevation, and the Basic Activation Rules page gives the example where it drives no Case Creation,
Notifications or TTL Counters either.
Read [Basic Activation Rules](../Configuration/Models/BasicActivationRules/index.html).
