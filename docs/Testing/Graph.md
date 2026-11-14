---
layout: default
title: Graph
nav_order: 26
parent: Testing
---

# Graph

Tests of `Jube.Graph`, the pure graph algorithm library behind network analysis: compressed graphs, edge weights,
communities, centralities, bounded search, motif detectors and relevance.

## Jube.Graph

### BoundedBfsTests

`Jube.Test.Graph.BoundedBfsTests` — 3 tests.
Category: Unit.

| Test                                   | Kind | Verifies                                    |
|----------------------------------------|------|---------------------------------------------|
| `ConfirmedNodeGivesDistancesOneAndTwo` | Fact | Confirmed node gives distances one and two. |
| `CapHidesNodesBeyondIt`                | Fact | Cap hides nodes beyond it.                  |
| `MultipleSeedsAreCountedSeparately`    | Fact | Multiple seeds are counted separately.      |

### CompressedGraphBuilderTests

`Jube.Test.Graph.CompressedGraphBuilderTests` — 3 tests.
Category: Unit.

| Test                                                 | Kind | Verifies                                                   |
|------------------------------------------------------|------|------------------------------------------------------------|
| `MapsExternalIdsToDenseIndicesInFirstSeenOrder`      | Fact | Maps external ids to dense indices in first seen order.    |
| `UndirectedAddsBothDirectionsAndMergesParallelEdges` | Fact | Undirected adds both directions and merges parallel edges. |
| `EgonetFromSeedRecordsDepth`                         | Fact | Egonet from seed records depth.                            |

### DeterminismTests

`Jube.Test.Graph.DeterminismTests` — 2 tests.
Category: Unit.

| Test                                           | Kind | Verifies                                            |
|------------------------------------------------|------|-----------------------------------------------------|
| `EveryAlgorithmRunTwiceGivesIdenticalArrays`   | Fact | Every algorithm run twice gives identical arrays.   |
| `EveryDetectorRunTwiceGivesIdenticalInstances` | Fact | Every detector run twice gives identical instances. |

### EdgeWeightTests

`Jube.Test.Graph.EdgeWeightTests` — 5 tests.
Category: Unit.

| Test                                           | Kind | Verifies                                                 |
|------------------------------------------------|------|----------------------------------------------------------|
| `OneHalfLifeAgoContributesExactlyHalf`         | Fact | One half life ago contributes exactly half.              |
| `SpecificityFallsWithCounterparts`             | Fact | Specificity falls with counterparts.                     |
| `WeightFollowsTheFormula`                      | Fact | Weight follows the formula.                              |
| `FoldDecaysEveryEventToTheAnchor`              | Fact | Fold decays every event to the anchor.                   |
| `FoldAnchorsOnTheLatestEventWhenItIsAfterAsOf` | Fact | Fold anchors on the latest event when it is after as of. |

### GreedyPeelingTests

`Jube.Test.Graph.GreedyPeelingTests` — 2 tests.
Category: Unit.

| Test                                  | Kind | Verifies                                  |
|---------------------------------------|------|-------------------------------------------|
| `FindsThePlantedDenseBlock`           | Fact | Finds the planted dense block.            |
| `UniformColumnWeightsGiveEdgeDensity` | Fact | Uniform column weights give edge density. |

### HitsTests

`Jube.Test.Graph.HitsTests` — 2 tests.
Category: Unit.

| Test                         | Kind | Verifies                        |
|------------------------------|------|---------------------------------|
| `HubsPointAtAuthorities`     | Fact | Hubs point at authorities.      |
| `EmptyGraphGivesEmptyArrays` | Fact | Empty graph gives empty arrays. |

### InterestPropagationTests

`Jube.Test.Graph.InterestPropagationTests` — 3 tests.
Category: Unit.

| Test                                                       | Kind | Verifies                                                              |
|------------------------------------------------------------|------|-----------------------------------------------------------------------|
| `SeedIsOneNeighboursOutrankFarNodesAndTheTailEndIsDropped` | Fact | Seed is one neighbours outrank far nodes and the tail end is dropped. |
| `BoostKeepsAConfirmedFarNode`                              | Fact | Boost keeps aconfirmed far node.                                      |
| `ZeroHopsKeepsOnlyTheSeed`                                 | Fact | Zero hops keeps only the seed.                                        |

### JohnsonCyclesTests

`Jube.Test.Graph.JohnsonCyclesTests` — 2 tests.
Category: Unit.

| Test                                        | Kind | Verifies                                          |
|---------------------------------------------|------|---------------------------------------------------|
| `FindsTheSingleFourCycleAndIgnoresTheTail`  | Fact | Finds the single four cycle and ignores the tail. |
| `RespectsTheMaximumLengthAndFindsTwoCycles` | Fact | Respects the maximum length and finds two cycles. |

### KarateClubTests

`Jube.Test.Graph.KarateClubTests` — 6 tests.
Category: Unit.

| Test                                                    | Kind | Verifies                                                        |
|---------------------------------------------------------|------|-----------------------------------------------------------------|
| `GraphHasThirtyFourNodesAndSeventyEightUndirectedEdges` | Fact | Graph has thirty four nodes and seventy eight undirected edges. |
| `UnionFindGivesOneComponent`                            | Fact | Union find gives one component.                                 |
| `LouvainModularityIsInTheKnownRange`                    | Fact | Louvain modularity is in the known range.                       |
| `LouvainRecoversTheTwoFactions`                         | Fact | Louvain recovers the two factions.                              |
| `PageRankRanksThirtyFourFirstAndOneSecond`              | Fact | Page rank ranks thirty four first and one second.               |
| `MaximumCoreNumberIsFour`                               | Fact | Maximum core number is four.                                    |

### MotifDetectorTests

`Jube.Test.Graph.MotifDetectorTests` — 15 tests.
Category: Unit.

| Test                                                         | Kind | Verifies                                                                         |
|--------------------------------------------------------------|------|----------------------------------------------------------------------------------|
| `ADenseBurstOfEventsCannotMakeTheCycleSearchRunWithoutBound` | Fact | A dense burst of events cannot make the temporal cycle search run without bound. |
| `FanOutIsFoundAtTheSource`                                   | Fact | Fan out is found at the source.                                                  |
| `FanInIsFoundAtTheDestination`                               | Fact | Fan in is found at the destination.                                              |
| `FanNeedsTheCounterpartsInsideTheWindow`                     | Fact | Fan needs the counterparts inside the window.                                    |
| `GatherScatterIsFoundAtTheHub`                               | Fact | Gather scatter is found at the hub.                                              |
| `ScatterGatherIsFoundAtTheSource`                            | Fact | Scatter gather is found at the source.                                           |
| `TemporalCycleIsFoundOnce`                                   | Fact | Temporal cycle is found once.                                                    |
| `TemporalCycleRespectsTheWindow`                             | Fact | Temporal cycle respects the window.                                              |
| `PassThroughIsFoundAtTheConduit`                             | Fact | Pass through is found at the conduit.                                            |
| `SmurfTypeOneIsFoundAtTheMiddle`                             | Fact | Smurf type one is found at the middle.                                           |
| `SmurfTypeTwoIsFoundAtTheSource`                             | Fact | Smurf type two is found at the source.                                           |
| `SmurfPreFiltersByAmount`                                    | Fact | Smurf pre filters by amount.                                                     |
| `StructuringWindowCountsDepositsJustUnderTheThreshold`       | Fact | Structuring window counts deposits just under the threshold.                     |
| `DenseBlockIsFound`                                          | Fact | Dense block is found.                                                            |
| `BenignLedgerTripsNoDetector`                                | Fact | Benign ledger trips no detector.                                                 |

### PersonalisedPageRankTests

`Jube.Test.Graph.PersonalisedPageRankTests` — 3 tests.
Category: Unit.

| Test                                              | Kind | Verifies                                                   |
|---------------------------------------------------|------|------------------------------------------------------------|
| `SumsToAboutOneAndRanksTheNeighbourAboveAFarNode` | Fact | Sums to about one and ranks the neighbour above afar node. |
| `DanglingMassReturnsToTheSeeds`                   | Fact | Dangling mass returns to the seeds.                        |
| `GlobalPageRankHandlesDanglingNodes`              | Fact | Global page rank handles dangling nodes.                   |

### TrianglesTests

`Jube.Test.Graph.TrianglesTests` — 2 tests.
Category: Unit.

| Test                                              | Kind | Verifies                                                |
|---------------------------------------------------|------|---------------------------------------------------------|
| `TwoTriangleBridgeGraph`                          | Fact | Two triangle bridge graph.                              |
| `DirectedEdgesAreTreatedAsUndirectedForTriangles` | Fact | Directed edges are treated as undirected for triangles. |

### VertexStatisticsTests

`Jube.Test.Graph.VertexStatisticsTests` — 3 tests.
Category: Unit.

| Test                                        | Kind | Verifies                                         |
|---------------------------------------------|------|--------------------------------------------------|
| `TinyLedgerGivesTheExpectedFigures`         | Fact | Tiny ledger gives the expected figures.          |
| `MomentsOfASymmetricSampleHaveZeroSkewness` | Fact | Moments of asymmetric sample have zero skewness. |
| `RoundAmountsAreMultiplesOfOneHundred`      | Fact | Round amounts are multiples of one hundred.      |
