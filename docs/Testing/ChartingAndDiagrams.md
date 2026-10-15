---
layout: default
title: Charting and Model Diagrams
nav_order: 7
parent: Testing
---

# Charting and Model Diagrams

Tests of the SVG charting library and the model diagram renderers built on it.

## Charting

### ChartBehaviourTests

`Jube.Test.Charting.ChartBehaviourTests` — 43 tests.

| Test                                                    | Kind   | Verifies                                                        |
|---------------------------------------------------------|--------|-----------------------------------------------------------------|
| `NaNValuesInALineBecomeGapsNotInvalidPathData`          | Fact   | Na n values in a line become gaps not invalid path data.        |
| `InfinityAndNaNInEveryPositionNeverLeakIntoOutput`      | Fact   | Infinity and na n in every position never leak into output.     |
| `AllNaNSeriesRenderANoDataChart`                        | Fact   | All na n series render a no data chart.                         |
| `EmptyChartsRenderWithoutThrowing`                      | Fact   | Empty charts render without throwing.                           |
| `IsolatedPointsBetweenGapsAreStillDrawn`                | Fact   | Isolated points between gaps are still drawn.                   |
| `PointCapThrowsAClearException`                         | Fact   | Point cap throws a clear exception.                             |
| `PointCapAppliesAcrossSeries`                           | Fact   | Point cap applies across series.                                |
| `MaximumPointsRenderWithinTheOutputCap`                 | Fact   | Maximum points render within the output cap.                    |
| `SeriesCategoryAndScoredObservationCapsThrow`           | Fact   | Series category and scored observation caps throw.              |
| `HeatmapCellCapThrows`                                  | Fact   | Heatmap cell cap throws.                                        |
| `MismatchedInputsThrowSpecificationExceptions`          | Fact   | Mismatched inputs throw specification exceptions.               |
| `LegendIsShownForManySeriesAndHiddenForOne`             | Fact   | Legend is shown for many series and hidden for one.             |
| `LegendPositionsAllRender`                              | Theory | Legend positions all render.                                    |
| `ManyLegendEntriesWrapOntoRows`                         | Fact   | Many legend entries wrap onto rows.                             |
| `NegativeValuesDrawBarsBelowTheBaseline`                | Fact   | Negative values draw bars below the baseline.                   |
| `StackedColumnsShowTotalsInTooltips`                    | Fact   | Stacked columns show totals in tooltips.                        |
| `ErrorBarsAreDrawnWhenSupplied`                         | Fact   | Error bars are drawn when supplied.                             |
| `PieFoldsTheTailIntoOther`                              | Fact   | Pie folds the tail into other.                                  |
| `SingleSliceDonutRendersAFullRing`                      | Fact   | Single slice donut renders a full ring.                         |
| `HistogramFromRawValuesCountsEveryValue`                | Fact   | Histogram from raw values counts every value.                   |
| `HistogramEdgeValuesLandInTheLastBin`                   | Fact   | Histogram edge values land in the last bin.                     |
| `HistogramOverlaysTwoSeriesWithSharedEdges`             | Fact   | Histogram overlays two series with shared edges.                |
| `PrecomputedHistogramBinsRender`                        | Fact   | Precomputed histogram bins render.                              |
| `HeatmapUsesSequentialAndDivergingClasses`              | Fact   | Heatmap uses sequential and diverging classes.                  |
| `HeatmapLegendBarHasOneBlockPerBin`                     | Fact   | Heatmap legend bar has one block per bin.                       |
| `ConfusionMatrixTooltipsCarryRowAndColumnPercentages`   | Fact   | Confusion matrix tooltips carry row and column percentages.     |
| `ConfusionMatrixFromPredictionsCounts`                  | Fact   | Confusion matrix from predictions counts.                       |
| `ConfusionMatrixMustBeSquare`                           | Fact   | Confusion matrix must be square.                                |
| `RocLegendCarriesAucAndChanceDiagonalAndThresholdMarks` | Fact   | Roc legend carries auc and chance diagonal and threshold marks. |
| `PrecisionRecallCarriesAverageAndNoSkillBaseline`       | Fact   | Precision recall carries average and no skill baseline.         |
| `CalibrationHasIdealDiagonalAndCountStrip`              | Fact   | Calibration has ideal diagonal and count strip.                 |
| `LearningCurveHasCrosshairBandsAndBestMarker`           | Fact   | Learning curve has crosshair bands and best marker.             |
| `PredictedVersusActualDrawsTheIdentityLine`             | Fact   | Predicted versus actual draws the identity line.                |
| `LogAxesRenderDecades`                                  | Fact   | Log axes render decades.                                        |
| `LogAxisDropsNonPositiveValuesAsGaps`                   | Fact   | Log axis drops non positive values as gaps.                     |
| `PointLabelsAppearInTooltips`                           | Fact   | Point labels appear in tooltips.                                |
| `PerSeriesColoursOverrideThePalette`                    | Fact   | Per series colours override the palette.                        |
| `SummaryDescribesTheChart`                              | Fact   | Summary describes the chart.                                    |
| `CustomDescriptionOverridesTheSummary`                  | Fact   | Custom description overrides the summary.                       |
| `CurveMathMatchesHandComputedAnswers`                   | Fact   | Curve math matches hand computed answers.                       |
| `CurveMathRejectsSingleClassAndMismatchedInput`         | Fact   | Curve math rejects single class and mismatched input.           |
| `CalibrationBinsAndEceAreCorrect`                       | Fact   | Calibration bins and ece are correct.                           |
| `LargeScoredInputsAreThinnedToBoundedCurves`            | Fact   | Large scored inputs are thinned to bounded curves.              |

### ChartFamilyTests

`Jube.Test.Charting.ChartFamilyTests` — 34 tests.

| Test                                                    | Kind | Verifies                                                        |
|---------------------------------------------------------|------|-----------------------------------------------------------------|
| `SplineLinesUseCubicSegmentsAndStraightLinesDoNot`      | Fact | Spline lines use cubic segments and straight lines do not.      |
| `DataLabelsAppearOnLinesAndStackedTotalsOnColumns`      | Fact | Data labels appear on lines and stacked totals on columns.      |
| `SplineDoesNotOvershootMonotoneData`                    | Fact | Spline does not overshoot monotone data.                        |
| `SplineBreaksAtGaps`                                    | Fact | Spline breaks at gaps.                                          |
| `SecondaryAxisRendersItsOwnLabelsAndTitle`              | Fact | Secondary axis renders its own labels and title.                |
| `BandsLinesAndAnnotationsAreDrawn`                      | Fact | Bands lines and annotations are drawn.                          |
| `VerticalBandsAndLinesWorkOnNumericAxes`                | Fact | Vertical bands and lines work on numeric axes.                  |
| `PercentStackedAreaHasAHundredPercentAxis`              | Fact | Percent stacked area has a hundred percent axis.                |
| `PercentStackedColumnsShowSharesAndAHundredPercentAxis` | Fact | Percent stacked columns show shares and a hundred percent axis. |
| `RangeBarsSpanFromTo`                                   | Fact | Range bars span from to.                                        |
| `PerPointColoursAreAppliedToBars`                       | Fact | Per point colours are applied to bars.                          |
| `ComboCombinesColumnsLinesAndASecondaryAxis`            | Fact | Combo combines columns lines and a secondary axis.              |
| `ComboSeriesMustMatchCategories`                        | Fact | Combo series must match categories.                             |
| `TrendLinesCarryTheRegressionAndSeriesColour`           | Fact | Trend lines carry the regression and series colour.             |
| `BoxPlotQuartilesMatchKnownAnswers`                     | Fact | Box plot quartiles match known answers.                         |
| `BoxPlotWhiskersStopAtTheFencesAndOutliersAreCounted`   | Fact | Box plot whiskers stop at the fences and outliers are counted.  |
| `BoxPlotWithNoValuesRenders`                            | Fact | Box plot with no values renders.                                |
| `CandlesSplitRisingAndFalling`                          | Fact | Candles split rising and falling.                               |
| `OhlcDrawsTicksInsteadOfBodies`                         | Fact | Ohlc draws ticks instead of bodies.                             |
| `WaterfallRunningTotalsAreCorrect`                      | Fact | Waterfall running totals are correct.                           |
| `FunnelShowsConversionAgainstFirstAndPreviousStage`     | Fact | Funnel shows conversion against first and previous stage.       |
| `RadarNeedsThreeAxesAndRejectsMismatchedSeries`         | Fact | Radar needs three axes and rejects mismatched series.           |
| `RadarDrawsRingsSpokesAndFilledPolygons`                | Fact | Radar draws rings spokes and filled polygons.                   |
| `PolarConvertsAnglesClockwiseFromTop`                   | Fact | Polar converts angles clockwise from top.                       |
| `GaugeShowsValueTargetAndClampsOutOfRangeValues`        | Fact | Gauge shows value target and clamps out of range values.        |
| `GaugeWithNonFiniteValueFallsBackToTheMinimum`          | Fact | Gauge with non finite value falls back to the minimum.          |
| `BulletDrawsRangesBarsAndTargets`                       | Fact | Bullet draws ranges bars and targets.                           |
| `LinearGaugeIsAThickBullet`                             | Fact | Linear gauge is a thick bullet.                                 |
| `SparklineGapsBreakTheLine`                             | Fact | Sparkline gaps break the line.                                  |
| `SparklineIsCompactAndTransparentByDefault`             | Fact | Sparkline is compact and transparent by default.                |
| `SquarifyConservesAreaAndProportions`                   | Fact | Squarify conserves area and proportions.                        |
| `TreemapTooltipsShowShares`                             | Fact | Treemap tooltips show shares.                                   |
| `GanttMilestonesAndProgressRender`                      | Fact | Gantt milestones and progress render.                           |
| `EveryFamilyRendersWithEmptyInput`                      | Fact | Every family renders with empty input.                          |

### ChartGoldenTests

`Jube.Test.Charting.ChartGoldenTests` — 2 tests.

| Test                              | Kind   | Verifies                              |
|-----------------------------------|--------|---------------------------------------|
| `SnapshotMatchesTheApprovedSvg`   | Theory | Snapshot matches the approved svg.    |
| `EverySnapshotResourceHasASample` | Fact   | Every snapshot resource has a sample. |

### ChartSafetyTests

`Jube.Test.Charting.ChartSafetyTests` — 14 tests.

| Test                                               | Kind   | Verifies                                                 |
|----------------------------------------------------|--------|----------------------------------------------------------|
| `OutputIsWellFormedAndSafe`                        | Theory | Output is well formed and safe.                          |
| `IdenticalSpecificationsRenderIdenticalBytes`      | Theory | Identical specifications render identical bytes.         |
| `EveryChartCarriesBothThemesAndResponsiveMetadata` | Theory | Every chart carries both themes and responsive metadata. |
| `EveryChartHasHoverCssTooltipsAndKeyboardFocus`    | Theory | Every chart has hover css tooltips and keyboard focus.   |
| `StylesAreScopedToTheChartClass`                   | Theory | Styles are scoped to the chart class.                    |
| `SparklinesAreFocusableWithNativeTitles`           | Fact   | Sparklines are focusable with native titles.             |
| `LegendAndMarkSharedSeriesClassesAreLinked`        | Fact   | Legend and mark shared series classes are linked.        |
| `DifferentChartsOnOnePageGetDifferentClasses`      | Fact   | Different charts on one page get different classes.      |
| `ExplicitIdIsUsedAsClassAndPrefix`                 | Fact   | Explicit id is used as class and prefix.                 |
| `InvalidIdIsRejected`                              | Fact   | Invalid id is rejected.                                  |
| `ResponsiveOmitsSizeAndFixedSetsIt`                | Fact   | Responsive omits size and fixed sets it.                 |
| `ExplicitThemeSetsTheDataThemeAttribute`           | Fact   | Explicit theme sets the data theme attribute.            |
| `NonInteractiveChartsCarryNoTooltipsOrFocus`       | Fact   | Non interactive charts carry no tooltips or focus.       |
| `TooltipsStayInsideTheChart`                       | Fact   | Tooltips stay inside the chart.                          |

### InjectionTests

`Jube.Test.Charting.InjectionTests` — 8 tests.

| Test                                           | Kind   | Verifies                                               |
|------------------------------------------------|--------|--------------------------------------------------------|
| `HostileTextNeverProducesMarkup`               | Theory | Hostile text never produces markup.                    |
| `ScriptTextAppearsOnlyEscaped`                 | Fact   | Script text appears only escaped.                      |
| `CdataTerminatorIsEscaped`                     | Fact   | Cdata terminator is escaped.                           |
| `ControlCharactersAreRemovedFromText`          | Fact   | Control characters are removed from text.              |
| `RightToLeftTextIsKeptButOverridesAreStripped` | Fact   | Right to left text is kept but overrides are stripped. |
| `InvalidColoursAreRejected`                    | Theory | Invalid colours are rejected.                          |
| `ValidColoursAreNormalisedIntoTheStylesheet`   | Fact   | Valid colours are normalised into the stylesheet.      |
| `LongTextIsTruncatedWithAnEllipsis`            | Fact   | Long text is truncated with an ellipsis.               |

### ScaleTests

`Jube.Test.Charting.ScaleTests` — 9 tests.

| Test                                          | Kind   | Verifies                                            |
|-----------------------------------------------|--------|-----------------------------------------------------|
| `LinearMapsAndInvertsBack`                    | Theory | Linear maps and inverts back.                       |
| `LinearMapsEndpoints`                         | Fact   | Linear maps endpoints.                              |
| `DegenerateDomainIsPadded`                    | Fact   | Degenerate domain is padded.                        |
| `NonFiniteBoundsAreRejected`                  | Fact   | Non finite bounds are rejected.                     |
| `LogRoundTrips`                               | Theory | Log round trips.                                    |
| `LogPlacesDecadesEvenlyAndRejectsNonPositive` | Fact   | Log places decades evenly and rejects non positive. |
| `TimeScaleRoundTrips`                         | Fact   | Time scale round trips.                             |
| `BandScaleLaysOutBandsAndInverts`             | Fact   | Band scale lays out bands and inverts.              |
| `BandScaleNeedsABand`                         | Fact   | Band scale needs a band.                            |

### TextMetricsTests

`Jube.Test.Charting.TextMetricsTests` — 5 tests.

| Test                                            | Kind | Verifies                                             |
|-------------------------------------------------|------|------------------------------------------------------|
| `WidthGrowsWithLengthAndFontSize`               | Fact | Width grows with length and font size.               |
| `NarrowGlyphsAreNarrowerThanWideOnes`           | Fact | Narrow glyphs are narrower than wide ones.           |
| `TruncationAddsAnEllipsisAndRespectsTheWidth`   | Fact | Truncation adds an ellipsis and respects the width.  |
| `TruncationNeverSplitsSurrogatePairsOrClusters` | Fact | Truncation never splits surrogate pairs or clusters. |
| `TinyWidthsGiveEmptyOrEllipsis`                 | Fact | Tiny widths give empty or ellipsis.                  |

### ThemeTests

`Jube.Test.Charting.ThemeTests` — 7 tests.

| Test                                           | Kind   | Verifies                                            |
|------------------------------------------------|--------|-----------------------------------------------------|
| `TextTokensMeetContrastOnTheirSurfaces`        | Theory | Text tokens meet contrast on their surfaces.        |
| `OnColourTextMeetsContrastOnEveryRampCell`     | Theory | On colour text meets contrast on every ramp cell.   |
| `CategoricalSeriesAreVisibleAgainstTheSurface` | Theory | Categorical series are visible against the surface. |
| `PalettesHaveTheDocumentedShape`               | Fact   | Palettes have the documented shape.                 |
| `SequentialRampsAreMonotonicInLightness`       | Fact   | Sequential ramps are monotonic in lightness.        |
| `DivergingArmsMeetInANeutralMidpoint`          | Fact   | Diverging arms meet in a neutral midpoint.          |
| `ColourParsingAcceptsOnlyHex`                  | Fact   | Colour parsing accepts only hex.                    |

### TickTests

`Jube.Test.Charting.TickTests` — 12 tests.

| Test                                    | Kind   | Verifies                                    |
|-----------------------------------------|--------|---------------------------------------------|
| `ExtendedWilkinsonKnownAnswers`         | Theory | Extended wilkinson known answers.           |
| `LooseTicksAlwaysCoverTheData`          | Theory | Loose ticks always cover the data.          |
| `TicksAreEvenlySpacedAndIncreasing`     | Fact   | Ticks are evenly spaced and increasing.     |
| `ReversedAndDegenerateBoundsAreHandled` | Fact   | Reversed and degenerate bounds are handled. |
| `NonFiniteBoundsAreRejected`            | Fact   | Non finite bounds are rejected.             |
| `LogTicksAreDecades`                    | Fact   | Log ticks are decades.                      |
| `TimeTicksAlignToCalendarMonths`        | Fact   | Time ticks align to calendar months.        |
| `TimeTicksUseClockLabelsWithinADay`     | Fact   | Time ticks use clock labels within a day.   |
| `TimeTicksUseYearsForLongSpans`         | Fact   | Time ticks use years for long spans.        |
| `AxisLabelsUseCompactSuffixes`          | Theory | Axis labels use compact suffixes.           |
| `ValueFormattingIsInvariantAndBounded`  | Theory | Value formatting is invariant and bounded.  |
| `ValueFormattingIgnoresCurrentCulture`  | Fact   | Value formatting ignores current culture.   |

## Charting diagrams

### CoefficientDiagramTests

`Jube.Test.Charting.Diagrams.CoefficientDiagramTests` — 13 tests.

| Test                                                              | Kind | Verifies                                                                     |
|-------------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `RowsAreSortedByMagnitudeByDefault`                               | Fact | Rows are sorted by magnitude by default.                                     |
| `SortModesOrderRowsAsDeclared`                                    | Fact | Sort modes order rows as declared.                                           |
| `SignificanceComesFromPValuesOrTheIntervalOrIsAssumedWhenUnknown` | Fact | Significance comes from p values or the interval or is assumed when unknown. |
| `IntervalsAreDerivedFromStandardErrors`                           | Fact | Intervals are derived from standard errors.                                  |
| `OddsRatioColumnExponentiatesTheCoefficients`                     | Fact | Odds ratio column exponentiates the coefficients.                            |
| `LogScaleDrawsTheReferenceAtOne`                                  | Fact | Log scale draws the reference at one.                                        |
| `FilledAndHollowMarkersFollowSignificance`                        | Fact | Filled and hollow markers follow significance.                               |
| `MarkersAreOrderedLikeTheirEstimates`                             | Fact | Markers are ordered like their estimates.                                    |
| `LargeInputsAreElidedToTheRowCapWithANote`                        | Fact | Large inputs are elided to the row cap with a note.                          |
| `EnormousInputThrowsDiagramTooLarge`                              | Fact | Enormous input throws diagram too large.                                     |
| `MalformedSpecificationsAreRejected`                              | Fact | Malformed specifications are rejected.                                       |
| `HoverRowsTooltipsAndFocusAreEmitted`                             | Fact | Hover rows tooltips and focus are emitted.                                   |
| `RenderingIsDeterministic`                                        | Fact | Rendering is deterministic.                                                  |

### ComponentsDiagramTests

`Jube.Test.Charting.Diagrams.ComponentsDiagramTests` — 9 tests.

| Test                                                                  | Kind | Verifies                                                                        |
|-----------------------------------------------------------------------|------|---------------------------------------------------------------------------------|
| `ScreeBarsAreProportionalToExplainedVariance`                         | Fact | Scree bars are proportional to explained variance.                              |
| `LoadingsHeatmapHasOneCellPerFeatureAndComponentWithSymmetricColours` | Fact | Loadings heatmap has one cell per feature and component with symmetric colours. |
| `FeaturesKeepTheirDeclaredOrder`                                      | Fact | Features keep their declared order.                                             |
| `HoverLinksTheScreeBarsAndTheHeatmapColumns`                          | Fact | Hover links the scree bars and the heatmap columns.                             |
| `ManyFeaturesAndComponentsAreElidedWithNotes`                         | Fact | Many features and components are elided with notes.                             |
| `ClustersAreSortedBySizeWithProportionalBars`                         | Fact | Clusters are sorted by size with proportional bars.                             |
| `ClusterFeatureColumnsAreCappedToTheWidestSpread`                     | Fact | Cluster feature columns are capped to the widest spread.                        |
| `InvalidCombinationsAndLimitsAreReported`                             | Fact | Invalid combinations and limits are reported.                                   |
| `RenderingIsDeterministic`                                            | Fact | Rendering is deterministic.                                                     |

### DiagramGoldenTests

`Jube.Test.Charting.Diagrams.DiagramGoldenTests` — 2 tests.

| Test                              | Kind   | Verifies                              |
|-----------------------------------|--------|---------------------------------------|
| `SnapshotMatchesTheApprovedSvg`   | Theory | Snapshot matches the approved svg.    |
| `EverySnapshotResourceHasASample` | Fact   | Every snapshot resource has a sample. |

### DiagramInjectionTests

`Jube.Test.Charting.Diagrams.DiagramInjectionTests` — 5 tests.

| Test                                                                 | Kind   | Verifies                                                                     |
|----------------------------------------------------------------------|--------|------------------------------------------------------------------------------|
| `HostileTextNeverProducesMarkup`                                     | Theory | Hostile text never produces markup.                                          |
| `ScriptTextAppearsOnlyEscapedInEveryDiagram`                         | Fact   | Script text appears only escaped in every diagram.                           |
| `CdataTerminatorsAndQuotesAreEscapedInEveryDiagram`                  | Fact   | Cdata terminators and quotes are escaped in every diagram.                   |
| `ControlCharactersAndBidirectionalOverridesAreRemovedInEveryDiagram` | Fact   | Control characters and bidirectional overrides are removed in every diagram. |
| `StylesheetBreakoutAttemptsCannotEscapeTheStyleElement`              | Theory | Stylesheet breakout attempts cannot escape the style element.                |

### DiagramSafetyTests

`Jube.Test.Charting.Diagrams.DiagramSafetyTests` — 7 tests.

| Test                                                            | Kind   | Verifies                                                                 |
|-----------------------------------------------------------------|--------|--------------------------------------------------------------------------|
| `EverySampleIsWellFormedSafeAndDeterministic`                   | Theory | Every sample is well formed safe and deterministic.                      |
| `EverySampleCarriesAccessibleTitleDescriptionAndBothThemes`     | Theory | Every sample carries accessible title description and both themes.       |
| `EverySampleHasKeyboardFocusableMarksWithTooltipsAndHoverRules` | Theory | Every sample has keyboard focusable marks with tooltips and hover rules. |
| `NonInteractiveOutputDropsMarksAndTooltipsButStaysValid`        | Fact   | Non interactive output drops marks and tooltips but stays valid.         |
| `ChartIdsAreValidatedAndIdenticalDiagramsShareAClass`           | Fact   | Chart ids are validated and identical diagrams share a class.            |
| `DarkAndLightOverridesWorkOnEveryFamily`                        | Fact   | Dark and light overrides work on every family.                           |
| `SizeIsResponsiveByDefaultAndFixedOnRequest`                    | Fact   | Size is responsive by default and fixed on request.                      |

### DistributionPanelsTests

`Jube.Test.Charting.Diagrams.DistributionPanelsTests` — 7 tests.

| Test                                                       | Kind | Verifies                                                             |
|------------------------------------------------------------|------|----------------------------------------------------------------------|
| `NormalDensityPeaksAtTheMeanAndIntegratesToOne`            | Fact | Normal density peaks at the mean and integrates to one.              |
| `EveryPanelGetsHoverColumnsWithAllSeriesInTheTooltip`      | Fact | Every panel gets hover columns with all series in the tooltip.       |
| `HistogramSeriesReportTheHeightOfTheBinUnderTheCursor`     | Fact | Histogram series report the height of the bin under the cursor.      |
| `ColumnsPerPanelShrinkAsPanelsGrowSoMarksStayWithinBudget` | Fact | Columns per panel shrink as panels grow so marks stay within budget. |
| `TooManyPanelsSeriesOrPointsThrowDiagramTooLarge`          | Fact | Too many panels series or points throw diagram too large.            |
| `MalformedSpecificationsAreRejected`                       | Fact | Malformed specifications are rejected.                               |
| `RenderingIsDeterministic`                                 | Fact | Rendering is deterministic.                                          |

### ForestOfTreesTests

`Jube.Test.Charting.Diagrams.ForestOfTreesTests` — 6 tests.

| Test                                              | Kind | Verifies                                                 |
|---------------------------------------------------|------|----------------------------------------------------------|
| `OnlyTheFirstTreesAreDrawnInCompactMode`          | Fact | Only the first trees are drawn in compact mode.          |
| `EachTreeGetsItsOwnPanelAndHoverScope`            | Fact | Each tree gets its own panel and hover scope.            |
| `ImportanceBarsAreSortedAndProportional`          | Fact | Importance bars are sorted and proportional.             |
| `ImportancesBeyondTheCapAreTruncatedToTheLargest` | Fact | Importances beyond the cap are truncated to the largest. |
| `ForestRenderingIsDeterministic`                  | Fact | Forest rendering is deterministic.                       |
| `InvalidForestsAreRejected`                       | Fact | Invalid forests are rejected.                            |

### GraphDiagramTests

`Jube.Test.Charting.Diagrams.GraphDiagramTests` — 16 tests.

| Test                                                                  | Kind | Verifies                                                                        |
|-----------------------------------------------------------------------|------|---------------------------------------------------------------------------------|
| `DiamondHasThreeLevelsAndNoCrossings`                                 | Fact | Diamond has three levels and no crossings.                                      |
| `ChainIsAStraightVerticalLine`                                        | Fact | Chain is a straight vertical line.                                              |
| `NaiveBayesPutsTheClassAboveEveryFeature`                             | Fact | Naive bayes puts the class above every feature.                                 |
| `TreeAugmentedStructureKeepsEdgesBetweenDeclaredNodesAndDoesNotCross` | Fact | Tree augmented structure keeps edges between declared nodes and does not cross. |
| `CyclesAreBrokenByReversalAndStillDrawn`                              | Fact | Cycles are broken by reversal and still drawn.                                  |
| `CrossingReductionFindsTheOrderWithoutCrossings`                      | Fact | Crossing reduction finds the order without crossings.                           |
| `CompleteBipartiteGraphHasTheMinimumPossibleCrossings`                | Fact | Complete bipartite graph has the minimum possible crossings.                    |
| `LongEdgesRouteThroughIntermediateLevels`                             | Fact | Long edges route through intermediate levels.                                   |
| `StateGraphUsesACircleWithSelfLoopsAndCurvedEdges`                    | Fact | State graph uses a circle with self loops and curved edges.                     |
| `EveryLayoutPointIsFiniteAndInsideTheCanvas`                          | Fact | Every layout point is finite and inside the canvas.                             |
| `UndirectedGraphsHaveNoArrowheads`                                    | Fact | Undirected graphs have no arrowheads.                                           |
| `HoverRulesTooltipsAndRolesAreEmitted`                                | Fact | Hover rules tooltips and roles are emitted.                                     |
| `RenderingIsDeterministic`                                            | Fact | Rendering is deterministic.                                                     |
| `TooManyNodesOrTooWideALevelThrowsDiagramTooLarge`                    | Fact | Too many nodes or too wide a level throws diagram too large.                    |
| `MalformedSpecificationsAndUnknownLayoutsAreRejected`                 | Fact | Malformed specifications and unknown layouts are rejected.                      |
| `IsolatedNodesAndSingleNodeGraphsRender`                              | Fact | Isolated nodes and single node graphs render.                                   |

### MembershipFunctionsTests

`Jube.Test.Charting.Diagrams.MembershipFunctionsTests` — 8 tests.

| Test                                                  | Kind   | Verifies                                                      |
|-------------------------------------------------------|--------|---------------------------------------------------------------|
| `TriangleMembershipMatchesTheDefinition`              | Theory | Triangle membership matches the definition.                   |
| `ShouldersAndOtherShapesEvaluateCorrectly`            | Fact   | Shoulders and other shapes evaluate correctly.                |
| `EverySetIsDrawnWithFuzzificationColumnsAndRuleLinks` | Fact   | Every set is drawn with fuzzification columns and rule links. |
| `DeterministicOutput`                                 | Fact   | Deterministic output.                                         |
| `InvalidParametersAreSpecificationErrors`             | Fact   | Invalid parameters are specification errors.                  |
| `RulesMustReferToDeclaredSets`                        | Fact   | Rules must refer to declared sets.                            |
| `UnknownShapesAreNotSupported`                        | Fact   | Unknown shapes are not supported.                             |
| `TooManyVariablesSetsOrRulesThrowDiagramTooLarge`     | Fact   | Too many variables sets or rules throw diagram too large.     |

### NetworkDetailTests

`Jube.Test.Charting.Diagrams.NetworkDetailTests` — 18 tests.

| Test                                                                              | Kind   | Verifies                                                                                        |
|-----------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------------|
| `AutoChoosesTheDetailFromTheSize`                                                 | Theory | Auto chooses the detail from the size.                                                          |
| `AnUnweightedHugeNetworkIsElidedNeverADensityViewBecauseThereAreNoMagnitudes`     | Fact   | An unweighted huge network is elided never a density view because there are no magnitudes.      |
| `TheLibraryDefaultIsUnchangedSoExistingDiagramsAreByteIdentical`                  | Fact   | The library default is unchanged so existing diagrams are byte identical.                       |
| `ARaisedCapShowsEveryNodeOfAModerateNetworkWithoutAggregatesOrWeightLabels`       | Fact   | A raised cap shows every node of a moderate network without aggregates or weight labels.        |
| `ElidedKeepsTheStrongestConnectionsAndSaysWhatWasLeftOut`                         | Fact   | Elided keeps the strongest connections and says what was left out.                              |
| `TheDensityViewStatesLayersElementsWeightsAndDensityAndDrawsAHeatmapPerLayerPair` | Fact   | The density view states layers elements weights and density and draws a heatmap per layer pair. |
| `DensityCellsAggregateMeanMagnitudeAndStrongestWeightExactly`                     | Fact   | Density cells aggregate mean magnitude and strongest weight exactly.                            |
| `DensityCellsAreAggregatedWhenTheLayersAreWide`                                   | Fact   | Density cells are aggregated when the layers are wide.                                          |
| `LargeNetworksRenderQuicklyAndWithinTheOutputCaps`                                | Theory | Large networks render quickly and within the output caps.                                       |
| `TheDensityViewIsInteractiveEvenWhenTheNetworkIsHuge`                             | Fact   | The density view is interactive even when the network is huge.                                  |
| `ANetworkWithNoFiniteWeightsIsStillRefusedInTheDensityView`                       | Fact   | A network with no finite weights is still refused in the density view.                          |
| `TooManyLayersIsStillTooLargeInTheDensityView`                                    | Fact   | Too many layers is still too large in the density view.                                         |
| `AnUnweightedNetworkInTheDensityViewShowsTheBlocksWithoutHeatmaps`                | Fact   | An unweighted network in the density view shows the blocks without heatmaps.                    |
| `EachDetailCanBeForcedAndTheSmallNetworkStillRenders`                             | Fact   | Each detail can be forced and the small network still renders.                                  |
| `HostileLayerNamesAndLabelsCannotInjectMarkupInTheDensityView`                    | Fact   | Hostile layer names and labels cannot inject markup in the density view.                        |
| `DensityOutputIsDeterministic`                                                    | Fact   | Density output is deterministic.                                                                |
| `EmphasisShadesInputsAddsAKeyAndHoverDetail`                                      | Fact   | Emphasis shades inputs adds a key and hover detail.                                             |
| `EmphasisWithoutATitleOrOutsideZeroToOneIsRejectedOrIgnored`                      | Fact   | Emphasis without a title or outside zero to one is rejected or ignored.                         |

### NetworkDiagramTests

`Jube.Test.Charting.Diagrams.NetworkDiagramTests` — 18 tests.

| Test                                                           | Kind | Verifies                                                                |
|----------------------------------------------------------------|------|-------------------------------------------------------------------------|
| `XorNetworkHasTheDeclaredShape`                                | Fact | Xor network has the declared shape.                                     |
| `XorRendersWellFormedSafeSvgWithLabelsAndBiases`               | Fact | Xor renders well formed safe svg with labels and biases.                |
| `EdgeTooltipsExplainWhatTheColourMeans`                        | Fact | Edge tooltips explain what the colour means.                            |
| `RenderingIsDeterministic`                                     | Fact | Rendering is deterministic.                                             |
| `LayersAreOrderedLeftToRightAndNodesKeepDeclaredOrder`         | Fact | Layers are ordered left to right and nodes keep declared order.         |
| `NoNodesOverlapAndEdgesConnectDeclaredNodes`                   | Fact | No nodes overlap and edges connect declared nodes.                      |
| `WideNetworkElidesToTheCapsWithAggregateNodes`                 | Fact | Wide network elides to the caps with aggregate nodes.                   |
| `StrongestConnectionsSurviveElision`                           | Fact | Strongest connections survive elision.                                  |
| `UnweightedNetworksAreThinnedToTheEdgeBudgetByShrinkingLayers` | Fact | Unweighted networks are thinned to the edge budget by shrinking layers. |
| `TooManyLayersThrowsDiagramTooLarge`                           | Fact | Too many layers throws diagram too large.                               |
| `EnormousInputThrowsDiagramTooLarge`                           | Fact | Enormous input throws diagram too large.                                |
| `ANetworkWithNoFiniteWeightsIsNotSupported`                    | Fact | A network with no finite weights is not supported.                      |
| `MalformedSpecificationsThrowSpecificationExceptions`          | Fact | Malformed specifications throw specification exceptions.                |
| `HoverCssTooltipsAndFocusAreEmitted`                           | Fact | Hover css tooltips and focus are emitted.                               |
| `NodesCarryTheirNeighboursForHighlighting`                     | Fact | Nodes carry their neighbours for highlighting.                          |
| `ThemeCanBeForced`                                             | Fact | Theme can be forced.                                                    |
| `NonInteractiveOutputHasNoMarksOrTooltips`                     | Fact | Non interactive output has no marks or tooltips.                        |
| `RolesAreDistinguishedByColourClassAndLegend`                  | Fact | Roles are distinguished by colour class and legend.                     |

### TreeDiagramTests

`Jube.Test.Charting.Diagrams.TreeDiagramTests` — 14 tests.

| Test                                                  | Kind | Verifies                                                 |
|-------------------------------------------------------|------|----------------------------------------------------------|
| `BalancedBinaryTreeIsSymmetric`                       | Fact | Balanced binary tree is symmetric.                       |
| `ParentsAreCentredOverTheirChildren`                  | Fact | Parents are centred over their children.                 |
| `LayersAreOrderedByDepthAndNodesNeverOverlap`         | Fact | Layers are ordered by depth and nodes never overlap.     |
| `LeftRightOrientationFlowsHorizontally`               | Fact | Left right orientation flows horizontally.               |
| `DepthCapCollapsesSubtreesIntoCountedTriangles`       | Fact | Depth cap collapses subtrees into counted triangles.     |
| `TooManyDrawnNodesReducesTheDepthGracefully`          | Fact | Too many drawn nodes reduces the depth gracefully.       |
| `EdgesCarryYesNoLabelsAndSampleFractions`             | Fact | Edges carry yes no labels and sample fractions.          |
| `RegressionTreesShowValuesWithoutALegend`             | Fact | Regression trees show values without a legend.           |
| `ASingleLeafTreeRenders`                              | Fact | A single leaf tree renders.                              |
| `HoverPathRulesTooltipsAndFocusAreEmitted`            | Fact | Hover path rules tooltips and focus are emitted.         |
| `RenderingIsDeterministic`                            | Fact | Rendering is deterministic.                              |
| `HugeInputThrowsDiagramTooLarge`                      | Fact | Huge input throws diagram too large.                     |
| `ImpossibleBranchingAndClassCountsAreNotSupported`    | Fact | Impossible branching and class counts are not supported. |
| `MalformedSpecificationsThrowSpecificationExceptions` | Fact | Malformed specifications throw specification exceptions. |

## Charting external

### ChartStylesheetTests

`Jube.Test.Charting.External.ChartStylesheetTests` — 12 tests.

| Test                                                            | Kind | Verifies                                                                   |
|-----------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `StylesheetMatchesTheApprovedGolden`                            | Fact | Stylesheet matches the approved golden.                                    |
| `TheCommittedAppStylesheetHasNotDriftedFromTheGenerator`        | Fact | The committed app stylesheet has not drifted from the generator.           |
| `TheLayoutIncludesTheStylesheetOnceWithTheVersion`              | Fact | The layout includes the stylesheet once with the version.                  |
| `EveryClassUsedByEveryExternalSampleIsASelectorInTheStylesheet` | Fact | Every class used by every external sample is a selector in the stylesheet. |
| `EveryHostileTextChartOnlyUsesKnownClasses`                     | Fact | Every hostile text chart only uses known classes.                          |
| `VersionIsStableAndEightHexDigits`                              | Fact | Version is stable and eight hex digits.                                    |
| `MinifiedAndReadableVariantsHaveTheSameRules`                   | Fact | Minified and readable variants have the same rules.                        |
| `StylesheetIsDeterministic`                                     | Fact | Stylesheet is deterministic.                                               |
| `StylesheetContainsNoMarkupImportsOrUrls`                       | Fact | Stylesheet contains no markup imports or urls.                             |
| `EveryCustomPropertyWithoutAFallbackIsDeclared`                 | Fact | Every custom property without a fallback is declared.                      |
| `ThemingHooksAreAllInTheSharedFile`                             | Fact | Theming hooks are all in the shared file.                                  |
| `RulesNeverLeakBetweenChartsOnAPage`                            | Fact | Rules never leak between charts on a page.                                 |

### ExternalGoldenTests

`Jube.Test.Charting.External.ExternalGoldenTests` — 2 tests.

| Test                              | Kind   | Verifies                              |
|-----------------------------------|--------|---------------------------------------|
| `SnapshotMatchesTheApprovedSvg`   | Theory | Snapshot matches the approved svg.    |
| `EverySnapshotResourceHasASample` | Fact   | Every snapshot resource has a sample. |

### ExternalInjectionTests

`Jube.Test.Charting.External.ExternalInjectionTests` — 3 tests.

| Test                                           | Kind   | Verifies                                             |
|------------------------------------------------|--------|------------------------------------------------------|
| `HostileTextNeverProducesMarkupInExternalMode` | Theory | Hostile text never produces markup in external mode. |
| `HostileIdsAreStillRejectedInExternalMode`     | Fact   | Hostile ids are still rejected in external mode.     |
| `InvalidColoursAreRejectedInExternalMode`      | Theory | Invalid colours are rejected in external mode.       |

### ExternalStyleTests

`Jube.Test.Charting.External.ExternalStyleTests` — 16 tests.

| Test                                                       | Kind   | Verifies                                                          |
|------------------------------------------------------------|--------|-------------------------------------------------------------------|
| `EverySampleRendersWellFormedWithoutAStyleElement`         | Theory | Every sample renders well formed without a style element.         |
| `OutputIsDeterministic`                                    | Theory | Output is deterministic.                                          |
| `ClassesAreStableAndNeverHashed`                           | Theory | Classes are stable and never hashed.                              |
| `OutputIsSmallerThanInline`                                | Theory | Output is smaller than inline.                                    |
| `SizesAcrossAllSamplesAreMateriallySmaller`                | Fact   | Sizes across all samples are materially smaller.                  |
| `InlineIsTheDefaultAndKeepsItsStyleElement`                | Fact   | Inline is the default and keeps its style element.                |
| `ExternalStylesShortcutMatchesTheOption`                   | Fact   | External styles shortcut matches the option.                      |
| `EveryBuilderAndDiagramAcceptsTheStyleMode`                | Fact   | Every builder and diagram accepts the style mode.                 |
| `DefaultPaletteChartsCarryNoStyleAttribute`                | Fact   | Default palette charts carry no style attribute.                  |
| `CustomSeriesColoursAreCustomPropertiesOnTheRootOnly`      | Fact   | Custom series colours are custom properties on the root only.     |
| `PerPointColoursUseTheSeriesCustomPropertyOnTheMark`       | Fact   | Per point colours use the series custom property on the mark.     |
| `ExplicitIdsKeepTheTitleAndDescriptionIdsUniquePerChart`   | Fact   | Explicit ids keep the title and description ids unique per chart. |
| `ExplicitThemeStaysAnAttributeOnTheRoot`                   | Fact   | Explicit theme stays an attribute on the root.                    |
| `FacetPanelsAreNestedExternalCharts`                       | Fact   | Facet panels are nested external charts.                          |
| `EveryFamilyClassIsDocumentedByTheStylesheet`              | Fact   | Every family class is documented by the stylesheet.               |
| `SeriesClassesBeyondEightStillResolveAgainstTheStylesheet` | Fact   | Series classes beyond eight still resolve against the stylesheet. |

## Charting more

### BandChartTests

`Jube.Test.Charting.More.BandChartTests` — 11 tests.

| Test                                              | Kind   | Verifies                                                 |
|---------------------------------------------------|--------|----------------------------------------------------------|
| `RegressionMatchesTheIndependentReference`        | Fact   | Regression matches the independent reference.            |
| `RegressionIntervalsMatchTheIndependentReference` | Theory | Regression intervals match the independent reference.    |
| `QuadraticRegressionRecoversExactCoefficients`    | Fact   | Quadratic regression recovers exact coefficients.        |
| `RegressionRejectsBadInput`                       | Fact   | Regression rejects bad input.                            |
| `ForecastIsDashedAndCarriesBothBandsAndMarkers`   | Fact   | Forecast is dashed and carries both bands and markers.   |
| `NaNValuesBreakLinesAndBandsIntoGaps`             | Fact   | Na n values break lines and bands into gaps.             |
| `RangeSelectorClipsTheMainPlotAndShowsTheWindow`  | Fact   | Range selector clips the main plot and shows the window. |
| `ErrorBarsAppearForPointsAndTooltipsCarryThem`    | Fact   | Error bars appear for points and tooltips carry them.    |
| `StepSeriesRenderHorizontalThenVerticalSegments`  | Fact   | Step series render horizontal then vertical segments.    |
| `BadSpecificationsAndCapsAreEnforced`             | Fact   | Bad specifications and caps are enforced.                |
| `EmptyChartsRenderNoData`                         | Fact   | Empty charts render no data.                             |

### CalendarTests

`Jube.Test.Charting.More.CalendarTests` — 6 tests.

| Test                                                               | Kind | Verifies                                                                     |
|--------------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `EveryDayOfEveryYearGetsACellAndDataDaysGetMarks`                  | Fact | Every day of every year gets a cell and data days get marks.                 |
| `CellsAreLaidOutByWeekColumnAndWeekdayRow`                         | Fact | Cells are laid out by week column and weekday row.                           |
| `DuplicateDatesAreAggregatedAndMissingOrNonFiniteValuesAreSkipped` | Fact | Duplicate dates are aggregated and missing or non finite values are skipped. |
| `DomainAndLegendReflectTheRange`                                   | Fact | Domain and legend reflect the range.                                         |
| `CapsSpecsAndEmptyAreHandled`                                      | Fact | Caps specs and empty are handled.                                            |
| `HeightGrowsWithTheNumberOfYears`                                  | Fact | Height grows with the number of years.                                       |

### DistributionTests

`Jube.Test.Charting.More.DistributionTests` — 13 tests.

| Test                                                       | Kind   | Verifies                                                             |
|------------------------------------------------------------|--------|----------------------------------------------------------------------|
| `SilvermanBandwidthMatchesTheIndependentReference`         | Fact   | Silverman bandwidth matches the independent reference.               |
| `QuantilesUseTheLinearInterpolationDefinition`             | Fact   | Quantiles use the linear interpolation definition.                   |
| `KernelDensityMatchesTheIndependentReference`              | Theory | Kernel density matches the independent reference.                    |
| `DensityIntegratesToOneAndBandwidthOverrideIsUsed`         | Fact   | Density integrates to one and bandwidth override is used.            |
| `BinnedEstimateForLargeSamplesTracksTheExactOne`           | Fact   | Binned estimate for large samples tracks the exact one.              |
| `EcdfStepsAreCumulativeSharesWithTies`                     | Fact   | Ecdf steps are cumulative shares with ties.                          |
| `DegenerateSamplesStillGetAPositiveBandwidth`              | Fact   | Degenerate samples still get a positive bandwidth.                   |
| `DensityChartShowsBandwidthAndKeepsTheFillOutOfTheTooltip` | Fact   | Density chart shows bandwidth and keeps the fill out of the tooltip. |
| `EcdfChartHasBandsAndComplementaryForm`                    | Fact   | Ecdf chart has bands and complementary form.                         |
| `ViolinDrawsOneShapeBoxAndMedianPerGroup`                  | Fact   | Violin draws one shape box and median per group.                     |
| `ViolinHandlesEmptyAndSingleValueGroups`                   | Fact   | Violin handles empty and single value groups.                        |
| `RidgelineDrawsOneRidgePerGroupAndValidatesOverlap`        | Fact   | Ridgeline draws one ridge per group and validates overlap.           |
| `CapsAreEnforcedAcrossDistributionCharts`                  | Fact   | Caps are enforced across distribution charts.                        |

### DotSlopeTests

`Jube.Test.Charting.More.DotSlopeTests` — 9 tests.

| Test                                                     | Kind | Verifies                                                         |
|----------------------------------------------------------|------|------------------------------------------------------------------|
| `DotPlotSortsByTheChosenSeriesAndDrawsOneDotPerValue`    | Fact | Dot plot sorts by the chosen series and draws one dot per value. |
| `LollipopDrawsStemsAndDumbbellDrawsConnectorsWithChange` | Fact | Lollipop draws stems and dumbbell draws connectors with change.  |
| `VerticalDotPlotsAndMissingValuesRender`                 | Fact | Vertical dot plots and missing values render.                    |
| `DotChartsRejectBadSpecifications`                       | Fact | Dot charts reject bad specifications.                            |
| `RanksUseCompetitionRankingAndSkipMissingValues`         | Fact | Ranks use competition ranking and skip missing values.           |
| `LabelSpreadingEnforcesTheGapAndStaysInBounds`           | Fact | Label spreading enforces the gap and stays in bounds.            |
| `BumpChartShowsRankInsideEveryPointAndSmoothCurves`      | Fact | Bump chart shows rank inside every point and smooth curves.      |
| `SlopeChartLabelsBothEndsAndTooltipCarriesChange`        | Fact | Slope chart labels both ends and tooltip carries change.         |
| `MissingPointsBreakTheLineAndBadSpecsAreRejected`        | Fact | Missing points break the line and bad specs are rejected.        |

### FacetChartTests

`Jube.Test.Charting.More.FacetChartTests` — 7 tests.

| Test                                                      | Kind | Verifies                                                             |
|-----------------------------------------------------------|------|----------------------------------------------------------------------|
| `EveryPanelIsANestedChartWithItsOwnHoverAndUniqueIds`     | Fact | Every panel is a nested chart with its own hover and unique ids.     |
| `SharedScalesGiveEveryPanelTheSameAxisAndFreeScalesDoNot` | Fact | Shared scales give every panel the same axis and free scales do not. |
| `GridGrowsTheCanvasWhenPanelsWouldBeTooSmall`             | Fact | Grid grows the canvas when panels would be too small.                |
| `GenericPanelsHistogramAndColumnPanelsWork`               | Fact | Generic panels histogram and column panels work.                     |
| `ForeignPanelImplementationsAreRejected`                  | Fact | Foreign panel implementations are rejected.                          |
| `BadSpecificationsCapsAndEmptyAreHandled`                 | Fact | Bad specifications caps and empty are handled.                       |
| `OutputSizeCapStopsRunawayFacets`                         | Fact | Output size cap stops runaway facets.                                |

### MatrixTests

`Jube.Test.Charting.More.MatrixTests` — 9 tests.

| Test                                                                | Kind | Verifies                                                                      |
|---------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `HierarchicalOrderMatchesTheHandWorkedTinyMatrix`                   | Fact | Hierarchical order matches the hand worked tiny matrix.                       |
| `OrderIsAPermutationAndDeterministicForLargerInput`                 | Fact | Order is a permutation and deterministic for larger input.                    |
| `TinyAndInvalidInputsAreHandled`                                    | Fact | Tiny and invalid inputs are handled.                                          |
| `RectangularRowsClusterTheirNearestNeighboursTogether`              | Fact | Rectangular rows cluster their nearest neighbours together.                   |
| `CorrelationMatrixIsClusteredIntoBlocksAndKeepsGivenOrderWhenAsked` | Fact | Correlation matrix is clustered into blocks and keeps given order when asked. |
| `CorrelationCellsCarryPearsonValues`                                | Fact | Correlation cells carry pearson values.                                       |
| `AdjacencyIsSymmetricUnlessDirectedAndClustersCommunities`          | Fact | Adjacency is symmetric unless directed and clusters communities.              |
| `MatrixRejectsBadInputAndEnforcesCaps`                              | Fact | Matrix rejects bad input and enforces caps.                                   |
| `RectangularMatrixClustersRowsAndColumnsIndependently`              | Fact | Rectangular matrix clusters rows and columns independently.                   |

### MoreChartTests

`Jube.Test.Charting.More.MoreChartTests` — 4 tests.

| Test                                         | Kind   | Verifies                                            |
|----------------------------------------------|--------|-----------------------------------------------------|
| `EverySampleIsWellFormedAndSafe`             | Theory | Every sample is well formed and safe.               |
| `EverySampleIsDeterministic`                 | Theory | Every sample is deterministic.                      |
| `EverySampleHasHoverCssKeyboardFocusAndAria` | Theory | Every sample has hover css keyboard focus and aria. |
| `EverySampleRendersInBothForcedThemes`       | Theory | Every sample renders in both forced themes.         |

### MoreGoldenTests

`Jube.Test.Charting.More.MoreGoldenTests` — 2 tests.

| Test                              | Kind   | Verifies                              |
|-----------------------------------|--------|---------------------------------------|
| `SnapshotMatchesTheApprovedSvg`   | Theory | Snapshot matches the approved svg.    |
| `EverySnapshotResourceHasASample` | Fact   | Every snapshot resource has a sample. |

### MoreInjectionTests

`Jube.Test.Charting.More.MoreInjectionTests` — 5 tests.

| Test                                                     | Kind   | Verifies                                                            |
|----------------------------------------------------------|--------|---------------------------------------------------------------------|
| `HostileTextNeverProducesMarkup`                         | Theory | Hostile text never produces markup.                                 |
| `ScriptTextAppearsOnlyEscapedInEveryTextSlotOfANewChart` | Fact   | Script text appears only escaped in every text slot of a new chart. |
| `InvalidColoursAreRejectedByNewCharts`                   | Theory | Invalid colours are rejected by new charts.                         |
| `ChartIdsAreValidatedAndForcedThemesAreApplied`          | Fact   | Chart ids are validated and forced themes are applied.              |
| `IdenticalChartsShareAClassAndDifferentChartsDoNot`      | Fact   | Identical charts share a class and different charts do not.         |

### ParallelChartTests

`Jube.Test.Charting.More.ParallelChartTests` — 6 tests.

| Test                                                 | Kind | Verifies                                                      |
|------------------------------------------------------|------|---------------------------------------------------------------|
| `EveryRowIsAHoverableLineWithTooltipAndDimmingRules` | Fact | Every row is a hoverable line with tooltip and dimming rules. |
| `EachColumnIsScaledIndependently`                    | Fact | Each column is scaled independently.                          |
| `CategoricalColumnsAreOrderedAndNaNBreaksLines`      | Fact | Categorical columns are ordered and na n breaks lines.        |
| `InvertedAxesFlipTheDirection`                       | Fact | Inverted axes flip the direction.                             |
| `ManyRowsFallBackToPlainLinesWithoutMarks`           | Fact | Many rows fall back to plain lines without marks.             |
| `BadSpecificationsAndCapsAreEnforced`                | Fact | Bad specifications and caps are enforced.                     |

### SankeyTests

`Jube.Test.Charting.More.SankeyTests` — 8 tests.

| Test                                                              | Kind | Verifies                                                                     |
|-------------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `NodeTotalsAreTheSumsOfTheirLinks`                                | Fact | Node totals are the sums of their links.                                     |
| `NodeHoverHighlightsTheWholeFlowThroughItAndNoOtherLinks`         | Fact | Node hover highlights the whole flow through it and no other links.          |
| `LinkHoverHighlightsItsUpstreamAndDownstreamLinks`                | Fact | Link hover highlights its upstream and downstream links.                     |
| `LargeGraphsFallBackToDirectLinkHighlighting`                     | Fact | Large graphs fall back to direct link highlighting.                          |
| `NonPositiveAndNonFiniteLinksAreSkippedAndJustifyMovesSinksRight` | Fact | Non positive and non finite links are skipped and justify moves sinks right. |
| `CyclesSelfLinksAndBadSpecsAreRejected`                           | Fact | Cycles self links and bad specs are rejected.                                |
| `CapsAreEnforcedAndEmptyRenders`                                  | Fact | Caps are enforced and empty renders.                                         |
| `NodesCanBeDeclaredWithColoursAndLabelsAreTruncated`              | Fact | Nodes can be declared with colours and labels are truncated.                 |

### ScatterMatrixTests

`Jube.Test.Charting.More.ScatterMatrixTests` — 7 tests.

| Test                                        | Kind | Verifies                                           |
|---------------------------------------------|------|----------------------------------------------------|
| `PearsonMatchesKnownAnswers`                | Fact | Pearson matches known answers.                     |
| `EveryOffDiagonalAndDiagonalCellIsDrawn`    | Fact | Every off diagonal and diagonal cell is drawn.     |
| `HoveringARowLinksItAcrossEveryScatterCell` | Fact | Hovering a row links it across every scatter cell. |
| `UpperTriangleModesAndNaNAreHandled`        | Fact | Upper triangle modes and na n are handled.         |
| `DiagonalHistogramCountsEveryFiniteValue`   | Fact | Diagonal histogram counts every finite value.      |
| `BadSpecificationsAndCapsAreEnforced`       | Fact | Bad specifications and caps are enforced.          |
| `ManyRowsRenderPlainPointsWithoutMarks`     | Fact | Many rows render plain points without marks.       |

### StreamLegendTests

`Jube.Test.Charting.More.StreamLegendTests` — 10 tests.

| Test                                                          | Kind | Verifies                                                                   |
|---------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `SilhouetteBaselineCentresTheStack`                           | Fact | Silhouette baseline centres the stack.                                     |
| `WiggleBaselineMatchesTheHandWorkedAlgorithm`                 | Fact | Wiggle baseline matches the hand worked algorithm.                         |
| `InsideOutOrderPutsEarlyPeaksOnOneSideAndLatePeaksOnTheOther` | Fact | Inside out order puts early peaks on one side and late peaks on the other. |
| `StreamChartsDrawOneShapePerSeriesWithSlotMarks`              | Fact | Stream charts draw one shape per series with slot marks.                   |
| `PercentStreamsShowAPercentAxisAndSharesSumToOne`             | Fact | Percent streams show a percent axis and shares sum to one.                 |
| `StreamsRejectBadInputAndTreatNaNAsZero`                      | Fact | Streams reject bad input and treat na n as zero.                           |
| `LegendWrapsIntoAtMostTheRequestedRowsWithAMoreItem`          | Fact | Legend wraps into at most the requested rows with a more item.             |
| `UnlimitedLegendsAreUnchangedByDefaultAndAutoCapsAtThreeRows` | Fact | Unlimited legends are unchanged by default and auto caps at three rows.    |
| `RightAndBottomLegendsAlsoOverflowIntoAMoreItem`              | Fact | Right and bottom legends also overflow into a more item.                   |
| `TheMoreItemIsNotLinkedToAnySeries`                           | Fact | The more item is not linked to any series.                                 |

### SurvivalTests

`Jube.Test.Charting.More.SurvivalTests` — 10 tests.

| Test                                                               | Kind   | Verifies                                                                   |
|--------------------------------------------------------------------|--------|----------------------------------------------------------------------------|
| `KaplanMeierEstimateAndGreenwoodErrorMatchTheIndependentReference` | Theory | Kaplan meier estimate and greenwood error match the independent reference. |
| `ConfidenceIntervalsMatchTheIndependentReference`                  | Theory | Confidence intervals match the independent reference.                      |
| `TiesCensoringAndMedianAreHandled`                                 | Fact   | Ties censoring and median are handled.                                     |
| `LogRankMatchesTheIndependentReference`                            | Fact   | Log rank matches the independent reference.                                |
| `NormalAndStudentQuantilesMatchKnownValues`                        | Fact   | Normal and student quantiles match known values.                           |
| `BadSpecificationsAreRejected`                                     | Fact   | Bad specifications are rejected.                                           |
| `ObservationCapIsEnforced`                                         | Fact   | Observation cap is enforced.                                               |
| `ChartCarriesBandsCensorTicksAtRiskAndMedian`                      | Fact   | Chart carries bands censor ticks at risk and median.                       |
| `ManyGroupsAreCappedAndEmptyChartsRender`                          | Fact   | Many groups are capped and empty charts render.                            |
| `CumulativeIncidenceFlipsTheCurve`                                 | Fact   | Cumulative incidence flips the curve.                                      |

## Charting performance

### GainsChartAbsoluteModeTests

`Jube.Test.Charting.Performance.GainsChartAbsoluteModeTests` — 3 tests.

| Test                                                                   | Kind | Verifies                                                                          |
|------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `AbsoluteModeStoresTheRawPositiveTotalOnTheCurve`                      | Fact | Absolute mode stores the raw positive total on the curve.                         |
| `AbsoluteModeRendersWithoutThrowingAndIncludesTheRawTotalInTheSummary` | Fact | Absolute mode renders without throwing and includes the raw total in the summary. |
| `NonAbsoluteModeIsUnaffectedByTheNewFlag`                              | Fact | Non absolute mode is unaffected by the new flag.                                  |

### PerformanceChartTests

`Jube.Test.Charting.Performance.PerformanceChartTests` — 25 tests.

| Test                                                                    | Kind   | Verifies                                                                           |
|-------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------|
| `InlineOutputIsWellFormedSafeAndDeterministic`                          | Theory | Inline output is well formed safe and deterministic.                               |
| `ExternalOutputHasNoStyleElementOrAttributeAndCarriesTheJcClass`        | Theory | External output has no style element or attribute and carries the jc class.        |
| `EveryChartHasATitleADescriptionAndKeyboardReachableMarks`              | Theory | Every chart has a title a description and keyboard reachable marks.                |
| `GainsShowsTheRandomBaselineThePerfectCurveAndTheDecileHover`           | Fact   | Gains shows the random baseline the perfect curve and the decile hover.            |
| `GainsWithoutBaselinesOmitsThem`                                        | Fact   | Gains without baselines omits them.                                                |
| `GainsRejectsMismatchedCurves`                                          | Fact   | Gains rejects mismatched curves.                                                   |
| `LiftShowsTenDecilesTheBaseRateLineAndBothSeries`                       | Fact   | Lift shows ten deciles the base rate line and both series.                         |
| `LiftCumulativeSeriesCanBeSwitchedOff`                                  | Fact   | Lift cumulative series can be switched off.                                        |
| `KsMarksTheMaximumSeparationAndTheThreshold`                            | Fact   | Ks marks the maximum separation and the threshold.                                 |
| `ThresholdSweepDrawsFourMeasuresBothThresholdsAndTheBestF1`             | Fact   | Threshold sweep draws four measures both thresholds and the best f 1.              |
| `ThresholdSweepWithoutMccOrMinimumRiskOmitsThem`                        | Fact   | Threshold sweep without mcc or minimum risk omits them.                            |
| `ScoreDistributionOverlaysBothClassesWithTheThreshold`                  | Fact   | Score distribution overlays both classes with the threshold.                       |
| `MetricsPanelShowsEveryMeasureWithHoverDetailAndNotAvailableForNaN`     | Fact   | Metrics panel shows every measure with hover detail and not available for na n.    |
| `PromotionHistoryShowsScoreBestSoFarComplexityAndTheActivePromotion`    | Fact   | Promotion history shows score best so far complexity and the active promotion.     |
| `PromotionHistoryBestSoFarIsMonotoneWhateverTheScoreDoes`               | Fact   | Promotion history best so far is monotone whatever the score does.                 |
| `PromotionHistoryWithoutComplexityOmitsTheSecondaryAxisAndDropsNaNRows` | Fact   | Promotion history without complexity omits the secondary axis and drops na n rows. |
| `PromotionHistoryOrdersPromotionsByDateWhateverTheInputOrder`           | Fact   | Promotion history orders promotions by date whatever the input order.              |
| `MetricsPanelGrowsWithTheNumberOfRows`                                  | Fact   | Metrics panel grows with the number of rows.                                       |
| `MetricsPanelLaysTilesOutOnAGridWithOneYPerRow`                         | Fact   | Metrics panel lays tiles out on a grid with one y per row.                         |
| `HostileTextNeverProducesMarkupInEitherMode`                            | Theory | Hostile text never produces markup in either mode.                                 |
| `HostileIdsAreRejected`                                                 | Fact   | Hostile ids are rejected.                                                          |
| `EmptyDataRendersAnEmptyStateForEveryChart`                             | Fact   | Empty data renders an empty state for every chart.                                 |
| `TooManyObservationsAreRefusedByTheCap`                                 | Fact   | Too many observations are refused by the cap.                                      |
| `LargeInputsStayWithinTheOutputAndPointCaps`                            | Fact   | Large inputs stay within the output and point caps.                                |
| `BuiltChartsSupportStyleModeSwitching`                                  | Fact   | Built charts support style mode switching.                                         |

### PerformanceGoldenTests

`Jube.Test.Charting.Performance.PerformanceGoldenTests` — 2 tests.

| Test                              | Kind   | Verifies                              |
|-----------------------------------|--------|---------------------------------------|
| `SnapshotMatchesTheApprovedSvg`   | Theory | Snapshot matches the approved svg.    |
| `EverySnapshotResourceHasASample` | Fact   | Every snapshot resource has a sample. |

### PerformanceMathTests

`Jube.Test.Charting.Performance.PerformanceMathTests` — 16 tests.

| Test                                                                        | Kind | Verifies                                                                                  |
|-----------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------|
| `GainsMatchesThePythonReferenceAtEveryDistinctScore`                        | Fact | Gains matches the python reference at every distinct score.                               |
| `GainsIsMonotoneAndAValidCurveWhateverTheTies`                              | Fact | Gains is monotone and a valid curve whatever the ties.                                    |
| `DecilesMatchThePythonReferenceIncludingFractionalTies`                     | Fact | Deciles match the python reference including fractional ties.                             |
| `DecilePositivesAlwaysSumToTheTotalPositives`                               | Fact | Decile positives always sum to the total positives.                                       |
| `KsMatchesThePythonReference`                                               | Fact | Ks matches the python reference.                                                          |
| `KsAgreesWithTheMaximumOfTprMinusFprFromTheRocCurve`                        | Fact | Ks agrees with the maximum of tpr minus fpr from the roc curve.                           |
| `KsThinningKeepsTheMaximumSeparationPoint`                                  | Fact | Ks thinning keeps the maximum separation point.                                           |
| `KsWithUniformWeightsMatchesTheUnweightedResult`                            | Fact | Ks with uniform weights matches the unweighted result.                                    |
| `KsWeightingByValueCanShiftTheMaximumSeparationPoint`                       | Fact | Ks weighting by value can shift the maximum separation point.                             |
| `SweepMatchesThePythonReferenceAtSelectedThresholds`                        | Fact | Sweep matches the python reference at selected thresholds.                                |
| `SweepBestF1IsTheMaximumOverTheGrid`                                        | Fact | Sweep best f 1 is the maximum over the grid.                                              |
| `DistributionBinsEveryObservationOnceAndPutsTheTopScoreInTheLastBin`        | Fact | Distribution bins every observation once and puts the top score in the last bin.          |
| `ScoresOutsideTheRangeAreClampedIntoTheEndBinsAndNonFiniteScoresAreDropped` | Fact | Scores outside the range are clamped into the end bins and non finite scores are dropped. |
| `DistributionWeightsScaleTheBinsAndTotalsInsteadOfCountingRows`             | Fact | Distribution weights scale the bins and totals instead of counting rows.                  |
| `SingleClassOrEmptyInputGivesEmptyResultsRatherThanThrowing`                | Fact | Single class or empty input gives empty results rather than throwing.                     |
| `MismatchedLengthsAreRejected`                                              | Fact | Mismatched lengths are rejected.                                                          |

## Model diagrams

### AnalyticsPageModelDiagramTests

`Jube.Test.ModelDiagrams.AnalyticsPageModelDiagramTests` — 7 tests.

| Test                                                                               | Kind   | Verifies                                                                                      |
|------------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------------|
| `TheModelDiagramTabDrawsEveryKindInsteadOfTheEmptyState`                           | Theory | The model diagram tab draws every kind instead of the empty state.                            |
| `AMultipleLinearRegressionVersionPicksUpItsEvidenceSideCar`                        | Fact   | A multiple linear regression version picks up its evidence side car.                          |
| `ATransformedVariableShowsItsDecoratedNameWhileAnUntransformedVariableIsUnchanged` | Fact   | A transformed variable shows its decorated name while an untransformed variable is unchanged. |
| `ATransformedVariablesEmbeddedEvidenceCoefficientLabelIsAlsoDecorated`             | Fact   | A transformed variables embedded evidence coefficient label is also decorated.                |
| `NoVariablesJsonLeavesEveryLabelExactlyAsStoredInTheModelJson`                     | Fact   | No variables json leaves every label exactly as stored in the model json.                     |
| `AllVariablesUntransformedLeavesEveryLabelExactlyAsStoredInTheModelJson`           | Fact   | All variables untransformed leaves every label exactly as stored in the model json.           |
| `MalformedVariablesJsonIsIgnoredAndTheDiagramStillRendersFromTheRawStoredNames`    | Fact   | Malformed variables json is ignored and the diagram still renders from the raw stored names.  |

### BayesStructureAdapterTests

`Jube.Test.ModelDiagrams.BayesStructureAdapterTests` — 21 tests.

| Test                                                                                 | Kind | Verifies                                                                                          |
|--------------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------------|
| `ATanParentArrayProducesExactlyTheClassEdgesAndTheTreeEdges`                         | Fact | A tan parent array produces exactly the class edges and the tree edges.                           |
| `ATanEdgeWeightIsTheChildsRowOfTheInformationMatrix`                                 | Fact | A tan edge weight is the childs row of the information matrix.                                    |
| `ATanGraphNamesTheClassAndItsPriors`                                                 | Fact | A tan graph names the class and its priors.                                                       |
| `ATanCycleIsMalformed`                                                               | Fact | A tan cycle is malformed.                                                                         |
| `ATanSelfParentIsMalformed`                                                          | Fact | A tan self parent is malformed.                                                                   |
| `ATanInformationMatrixOfTheWrongSizeIsMalformed`                                     | Fact | A tan information matrix of the wrong size is malformed.                                          |
| `TheRealTanFixtureHasOneClassEdgePerFeatureAndOneTreeEdgePerParent`                  | Fact | The real tan fixture has one class edge per feature and one tree edge per parent.                 |
| `AkDependenceParentListsProduceEveryFeatureToFeatureEdge`                            | Fact | Ak dependence parent lists produce every feature to feature edge.                                 |
| `AkDependenceOrderThatIsNotAPermutationIsMalformed`                                  | Fact | Ak dependence order that is not a permutation is malformed.                                       |
| `AkDependenceParentOutOfRangeIsMalformed`                                            | Fact | Ak dependence parent out of range is malformed.                                                   |
| `TheRealKDependenceFixtureKeepsEveryStoredParent`                                    | Fact | The real k dependence fixture keeps every stored parent.                                          |
| `AveragedOneDependenceDrawsEachSuperParentToEveryOtherFeatureWithItsEstimatorWeight` | Fact | Averaged one dependence draws each super parent to every other feature with its estimator weight. |
| `AveragedOneDependenceWithNoSupportedEstimatorDoesNotVisualise`                      | Fact | Averaged one dependence with no supported estimator does not visualise.                           |
| `AveragedOneDependenceWithTooManyFeaturesIsTooLarge`                                 | Fact | Averaged one dependence with too many features is too large.                                      |
| `HiddenNaiveBayesDrawsColumnJAsAParentOfRowIWithTheRowIColumnJWeight`                | Fact | Hidden naive bayes draws column j as a parent of row i with the row i column j weight.            |
| `HiddenNaiveBayesIgnoresTheDiagonal`                                                 | Fact | Hidden naive bayes ignores the diagonal.                                                          |
| `HiddenNaiveBayesWithANonSquareMatrixIsMalformed`                                    | Fact | Hidden naive bayes with a non square matrix is malformed.                                         |
| `ALatentTreeHasLatentNodesFeatureNodesAndDependenceStrengthOnTheEdges`               | Fact | A latent tree has latent nodes feature nodes and dependence strength on the edges.                |
| `ALatentTreeParentThatComesLaterIsMalformed`                                         | Fact | A latent tree parent that comes later is malformed.                                               |
| `TheRealLatentTreeFixtureNamesEveryFeatureAndAtLeastOneHiddenVariable`               | Fact | The real latent tree fixture names every feature and at least one hidden variable.                |
| `ABayesGraphBeyondTheGraphLimitIsTooLarge`                                           | Fact | A bayes graph beyond the graph limit is too large.                                                |

### CoefficientAdapterTests

`Jube.Test.ModelDiagrams.CoefficientAdapterTests` — 12 tests.

| Test                                                        | Kind | Verifies                                                             |
|-------------------------------------------------------------|------|----------------------------------------------------------------------|
| `LinearRegressionWeightsBecomePlainCoefficients`            | Fact | Linear regression weights become plain coefficients.                 |
| `TheRealLinearFixtureMapsItsWeights`                        | Fact | The real linear fixture maps its weights.                            |
| `ALogisticModelIsRecognisedByItsLinkAndShowsOddsRatios`     | Fact | A logistic model is recognised by its link and shows odds ratios.    |
| `ALogisticTypeMarkerAloneIsEnoughToLabelTheModelLogistic`   | Fact | A logistic type marker alone is enough to label the model logistic.  |
| `StandardErrorsWithTheInterceptFirstAreAlignedToTheWeights` | Fact | Standard errors with the intercept first are aligned to the weights. |
| `StaleStandardErrorsOfTheWrongLengthAreIgnored`             | Fact | Stale standard errors of the wrong length are ignored.               |
| `AllZeroStandardErrorsAreIgnored`                           | Fact | All zero standard errors are ignored.                                |
| `ARealModelThatAnEnvelopeWrapsIsNotAlsoClaimedHere`         | Fact | A real model that an envelope wraps is not also claimed here.        |
| `ANonFiniteWeightDoesNotVisualise`                          | Fact | A non finite weight does not visualise.                              |
| `MoreCoefficientsThanTheChartCanHoldIsTooLarge`             | Fact | More coefficients than the chart can hold is too large.              |
| `TheElementCapAppliesToCoefficients`                        | Fact | The element cap applies to coefficients.                             |
| `ManyCoefficientsWithinTheLimitsAreElidedToTheStrongest`    | Fact | Many coefficients within the limits are elided to the strongest.     |

### DistributionAndTreeAdapterTests

`Jube.Test.ModelDiagrams.DistributionAndTreeAdapterTests` — 24 tests.

| Test                                                            | Kind | Verifies                                                                   |
|-----------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `GaussianNaiveBayesMakesOnePanelPerFeatureWithOneCurvePerClass` | Fact | Gaussian naive bayes makes one panel per feature with one curve per class. |
| `EachCurveIsCentredOnItsMeanWithItsDeviation`                   | Fact | Each curve is centred on its mean with its deviation.                      |
| `AZeroVarianceFeatureDoesNotVisualise`                          | Fact | A zero variance feature does not visualise.                                |
| `PriorsThatDoNotMatchTheClassesAreMalformed`                    | Fact | Priors that do not match the classes are malformed.                        |
| `MoreFeaturesThanPanelsIsTooLarge`                              | Fact | More features than panels is too large.                                    |
| `ThresholdsSitOnTheNodesThatSplitOnThem`                        | Fact | Thresholds sit on the nodes that split on them.                            |
| `HostileAttributeAndClassNamesInTheJsonAreEscaped`              | Fact | Hostile attribute and class names in the json are escaped.                 |
| `TheLessThanSideIsTheYesBranch`                                 | Fact | The less than side is the yes branch.                                      |
| `ReversedBranchOrderStillPutsTheLessThanSideOnYes`              | Fact | Reversed branch order still puts the less than side on yes.                |
| `StrictLessThanIsShownAsAStrictCondition`                       | Fact | Strict less than is shown as a strict condition.                           |
| `NumericComparisonCodesAreUnderstood`                           | Fact | Numeric comparison codes are understood.                                   |
| `EqualityBranchesBecomeLabelledMultiwaySplits`                  | Fact | Equality branches become labelled multiway splits.                         |
| `VariableNamesOverrideTheStoredAttributeNames`                  | Fact | Variable names override the stored attribute names.                        |
| `ClassNamesAreCarriedToTheDiagram`                              | Fact | Class names are carried to the diagram.                                    |
| `ASplitOnAnAttributeThatDoesNotExistIsMalformed`                | Fact | A split on an attribute that does not exist is malformed.                  |
| `ALeafWithoutAnOutputIsMalformed`                               | Fact | A leaf without an output is malformed.                                     |
| `TheRealTreeFixtureKeepsEveryStoredThreshold`                   | Fact | The real tree fixture keeps every stored threshold.                        |
| `AVeryLargeTreeBeyondTheElementCapDoesNotVisualise`             | Fact | A very large tree beyond the element cap does not visualise.               |
| `ATreeDeeperThanTheChartTakesIsCollapsedNotRejected`            | Fact | A tree deeper than the chart takes is collapsed not rejected.              |
| `AForestShowsTheFirstTreesAndSplitShareImportances`             | Fact | A forest shows the first trees and split share importances.                |
| `ImportancesCountEverySplitNotJustTheDrawnTrees`                | Fact | Importances count every split not just the drawn trees.                    |
| `ALargeEnsembleBeyondTheCapDoesNotVisualise`                    | Fact | A large ensemble beyond the cap does not visualise.                        |
| `AForestWithMoreThanEightClassesDoesNotVisualise`               | Fact | A forest with more than eight classes does not visualise.                  |
| `TheRealForestFixtureDrawsEveryStoredTree`                      | Fact | The real forest fixture draws every stored tree.                           |

### MarkovAndComponentsAdapterTests

`Jube.Test.ModelDiagrams.MarkovAndComponentsAdapterTests` — 20 tests.

| Test                                                            | Kind | Verifies                                                                 |
|-----------------------------------------------------------------|------|--------------------------------------------------------------------------|
| `TransitionProbabilitiesLandOnTheRightEdges`                    | Fact | Transition probabilities land on the right edges.                        |
| `StatesAreNamedFromTheOptionsAndCarryInitialAndEmissionDetail`  | Fact | States are named from the options and carry initial and emission detail. |
| `LogProbabilitiesAreExponentiated`                              | Fact | Log probabilities are exponentiated.                                     |
| `HostileNamesInTheJsonAreEscaped`                               | Fact | Hostile names in the json are escaped.                                   |
| `ImpossibleTransitionsAreNotDrawn`                              | Fact | Impossible transitions are not drawn.                                    |
| `NormalEmissionsAreDescribedByTheirMeanAndDeviation`            | Fact | Normal emissions are described by their mean and deviation.              |
| `ARowThatIsNotAProbabilityDistributionIsMalformed`              | Fact | A row that is not a probability distribution is malformed.               |
| `EmissionsThatDoNotMatchTheStatesAreMalformed`                  | Fact | Emissions that do not match the states are malformed.                    |
| `TheRealDiscreteFixtureMatchesItsStoredLogTransitions`          | Fact | The real discrete fixture matches its stored log transitions.            |
| `ManyStatesBeyondTheGraphLimitAreTooLarge`                      | Fact | Many states beyond the graph limit are too large.                        |
| `ClusterCentroidsAreStandardisedAcrossClustersWithTheirSizes`   | Fact | Cluster centroids are standardised across clusters with their sizes.     |
| `ClustersInAnObjectWrapperAreAlsoUnderstood`                    | Fact | Clusters in an object wrapper are also understood.                       |
| `CentroidsOfDifferentLengthsAreMalformed`                       | Fact | Centroids of different lengths are malformed.                            |
| `TheRealClusterFixtureHasItsStoredProportions`                  | Fact | The real cluster fixture has its stored proportions.                     |
| `MoreClustersThanTheChartHoldsIsTooLarge`                       | Fact | More clusters than the chart holds is too large.                         |
| `ExplainedVarianceIsTheEigenvalueShareWhenNoProportionIsStored` | Fact | Explained variance is the eigenvalue share when no proportion is stored. |
| `AStoredProportionIsPreferred`                                  | Fact | A stored proportion is preferred.                                        |
| `TheRealPcaFixtureUsesItsStoredEigenvectorsAsLoadings`          | Fact | The real pca fixture uses its stored eigenvectors as loadings.           |
| `EigenvectorsOfDifferentLengthsAreMalformed`                    | Fact | Eigenvectors of different lengths are malformed.                         |
| `MoreFeaturesThanTheChartHoldsIsTooLarge`                       | Fact | More features than the chart holds is too large.                         |

### ModelDiagramDetectionTests

`Jube.Test.ModelDiagrams.ModelDiagramDetectionTests` — 20 tests.

| Test                                                       | Kind   | Verifies                                                            |
|------------------------------------------------------------|--------|---------------------------------------------------------------------|
| `EveryFixtureIsClaimedByExactlyOneAdapter`                 | Theory | Every fixture is claimed by exactly one adapter.                    |
| `TheRendererSelectsTheExpectedAdapter`                     | Theory | The renderer selects the expected adapter.                          |
| `EveryAdapterClaimsAtLeastOneFixture`                      | Fact   | Every adapter claims at least one fixture.                          |
| `AdapterNamesAreUnique`                                    | Fact   | Adapter names are unique.                                           |
| `EmptyInputIsNotRecognised`                                | Theory | Empty input is not recognised.                                      |
| `GarbageAndTruncatedJsonAreNotRecognised`                  | Theory | Garbage and truncated json are not recognised.                      |
| `ValidJsonThatIsNotAModelIsNotRecognised`                  | Theory | Valid json that is not a model is not recognised.                   |
| `TheTruncatedFixtureIsNotRecognised`                       | Fact   | The truncated fixture is not recognised.                            |
| `AHugelyNestedDocumentHitsTheDepthCap`                     | Fact   | A hugely nested document hits the depth cap.                        |
| `TheDepthCapIsConfigurable`                                | Fact   | The depth cap is configurable.                                      |
| `ADocumentOverTheSizeCapIsNotSupported`                    | Fact   | A document over the size cap is not supported.                      |
| `TheDefaultSizeCapAppliesBeforeParsing`                    | Fact   | The default size cap applies before parsing.                        |
| `ARealModelJustUnderACustomCapRenders`                     | Fact   | A real model just under a custom cap renders.                       |
| `NullJsonIsAnArgumentError`                                | Fact   | Null json is an argument error.                                     |
| `TwoAdaptersClaimingTheSameModelEquallyIsAnError`          | Fact   | Two adapters claiming the same model equally is an error.           |
| `ThePreferredAdapterWinsWhenConfidencesDiffer`             | Fact   | The preferred adapter wins when confidences differ.                 |
| `AnAdapterThatReturnsNothingOrZeroDoesNotClaim`            | Fact   | An adapter that returns nothing or zero does not claim.             |
| `AMalformedModelThatAnAdapterClaimedIsReportedAsMalformed` | Fact   | A malformed model that an adapter claimed is reported as malformed. |
| `PropertyNamesAreMatchedWithoutRegardToCase`               | Fact   | Property names are matched without regard to case.                  |
| `NonFiniteNumbersWrittenAsStringsAreUnderstood`            | Fact   | Non finite numbers written as strings are understood.               |

### ModelDiagramGoldenTests

`Jube.Test.ModelDiagrams.ModelDiagramGoldenTests` — 3 tests.

| Test                              | Kind   | Verifies                              |
|-----------------------------------|--------|---------------------------------------|
| `SnapshotMatchesTheApprovedSvg`   | Theory | Snapshot matches the approved svg.    |
| `EverySnapshotResourceHasASample` | Fact   | Every snapshot resource has a sample. |
| `EveryAdapterHasASnapshotSample`  | Fact   | Every adapter has a snapshot sample.  |

### ModelDiagramRenderingTests

`Jube.Test.ModelDiagrams.ModelDiagramRenderingTests` — 15 tests.

| Test                                                   | Kind   | Verifies                                                       |
|--------------------------------------------------------|--------|----------------------------------------------------------------|
| `EveryFixtureRendersOneWellFormedSafeSvg`              | Theory | Every fixture renders one well formed safe svg.                |
| `EveryFixtureRendersIdenticalBytesEveryTime`           | Theory | Every fixture renders identical bytes every time.              |
| `EveryFixtureCarriesHoverDetailUnlessAskedNotTo`       | Theory | Every fixture carries hover detail unless asked not to.        |
| `TheTitleOverrideIsUsed`                               | Theory | The title override is used.                                    |
| `TheThemeOptionSelectsTheDataTheme`                    | Theory | The theme option selects the data theme.                       |
| `TheSizeOptionSetsTheMinimumCanvas`                    | Theory | The size option sets the minimum canvas.                       |
| `ASizeOutsideTheChartRangeIsRejected`                  | Theory | A size outside the chart range is rejected.                    |
| `HostileVariableNamesAreEscapedInEveryAdapter`         | Theory | Hostile variable names are escaped in every adapter.           |
| `HostileVariableNamesAreEscapedInTheNetwork`           | Theory | Hostile variable names are escaped in the network.             |
| `HostileLabelsFromTheJsonAreEscaped`                   | Theory | Hostile labels from the json are escaped.                      |
| `HostileEvidenceAndVariableNamesInASnapshotAreEscaped` | Fact   | Hostile evidence and variable names in a snapshot are escaped. |
| `TheVariableNameOverrideMustMatchTheModel`             | Fact   | The variable name override must match the model.               |
| `ManyThreadsRenderTheSameBytes`                        | Fact   | Many threads render the same bytes.                            |
| `DifferentOptionsOnDifferentThreadsDoNotLeak`          | Fact   | Different options on different threads do not leak.            |
| `ANetworkBeyondTheElementCapIsNotSupported`            | Fact   | A network beyond the element cap is not supported.             |

### NetworkTopologyAdapterTests

`Jube.Test.ModelDiagrams.NetworkTopologyAdapterTests` — 16 tests.

| Test                                                     | Kind   | Verifies                                                         |
|----------------------------------------------------------|--------|------------------------------------------------------------------|
| `ATwoTwoOneNetworkPutsEachWeightOnItsEdge`               | Fact   | A two two one network puts each weight on its edge.              |
| `ThresholdsBecomeTheBiasesOfTheirNeurons`                | Fact   | Thresholds become the biases of their neurons.                   |
| `RolesAndLabelsFollowTheLayers`                          | Fact   | Roles and labels follow the layers.                              |
| `InputNamesFromTheOptionsLabelTheInputLayer`             | Fact   | Input names from the options label the input layer.              |
| `TheOverrideBeatsTheStoredAnnotations`                   | Fact   | The override beats the stored annotations.                       |
| `AWrongLengthOverrideIsRejected`                         | Fact   | A wrong length override is rejected.                             |
| `EveryRealTopologyMapsItsWeightsThresholdsAndInputNames` | Theory | Every real topology maps its weights thresholds and input names. |
| `ActivationNamesAreReadFromTheStoredFunctionTypes`       | Theory | Activation names are read from the stored function types.        |
| `MixedActivationsInALayerAreLabelledMixed`               | Fact   | Mixed activations in a layer are labelled mixed.                 |
| `UnusualFunctionTypesFallBackToCustom`                   | Theory | Unusual function types fall back to custom.                      |
| `WeightsThatDoNotMatchThePreviousLayerAreMalformed`      | Fact   | Weights that do not match the previous layer are malformed.      |
| `ADeclaredInputCountThatDisagreesIsMalformed`            | Fact   | A declared input count that disagrees is malformed.              |
| `ANetworkWithNoFiniteWeightsDoesNotVisualise`            | Fact   | A network with no finite weights does not visualise.             |
| `NeuronsWithoutAThresholdAreNotANetwork`                 | Fact   | Neurons without a threshold are not a network.                   |
| `ADeepNetworkBeyondTheChartLimitIsTooLarge`              | Fact   | A deep network beyond the chart limit is too large.              |
| `AWideRealNetworkIsElidedNotRejected`                    | Fact   | A wide real network is elided not rejected.                      |

### PipelineKindAdapterTests

`Jube.Test.ModelDiagrams.PipelineKindAdapterTests` — 30 tests.

| Test                                                                            | Kind   | Verifies                                                                                      |
|---------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------------|
| `MahalanobisDrawsTheCorrelationOfTheInvertedPrecisionOverActiveVariables`       | Fact   | Mahalanobis draws the correlation of the inverted precision over active variables.            |
| `MahalanobisFallsBackToPartialCorrelationWhenThePrecisionIsSingular`            | Fact   | Mahalanobis falls back to partial correlation when the precision is singular.                 |
| `MahalanobisWithOneActiveVariableDrawsItsNormalProfile`                         | Fact   | Mahalanobis with one active variable draws its normal profile.                                |
| `MahalanobisRejectsAPrecisionOfTheWrongSize`                                    | Fact   | Mahalanobis rejects a precision of the wrong size.                                            |
| `NaiveBayesDrawsBothClassDensitiesPerVariableWithPriorsFromTheLogOdds`          | Fact   | Naive bayes draws both class densities per variable with priors from the log odds.            |
| `TreeAugmentedNaiveBayesLinksTheClassToEveryVariableAndEachVariableToItsParent` | Fact   | Tree augmented naive bayes links the class to every variable and each variable to its parent. |
| `TreeAugmentedNaiveBayesRejectsACycle`                                          | Fact   | Tree augmented naive bayes rejects a cycle.                                                   |
| `GaussianMixtureDensityRecoversMarginalDeviationsFromEachComponentPrecision`    | Fact   | Gaussian mixture density recovers marginal deviations from each component precision.          |
| `AMixtureWithMoreComponentsThanColoursDrawsTheHeaviestAndSaysSo`                | Fact   | A mixture with more components than colours draws the heaviest and says so.                   |
| `CentroidClusterersStandardiseEachVariableAcrossClusters`                       | Theory | Centroid clusterers standardise each variable across clusters.                                |
| `ASingleClusterShowsItsOwnValuesInsteadOfADegenerateZScore`                     | Fact   | A single cluster shows its own values instead of a degenerate z score.                        |
| `KModesShowsTheShareOfOtherClustersWhoseModeDiffers`                            | Fact   | K modes shows the share of other clusters whose mode differs.                                 |
| `ADecisionTreeClassifierSplitsLeftAtOrBelowTheThresholdAndCarriesClassMixes`    | Fact   | A decision tree classifier splits left at or below the threshold and carries class mixes.     |
| `ADecisionTreeRegressorCarriesLeafPredictions`                                  | Fact   | A decision tree regressor carries leaf predictions.                                           |
| `ATreeWhoseChildrenLoopBackIsRejected`                                          | Fact   | A tree whose children loop back is rejected.                                                  |
| `ARandomForestDrawsItsFirstTreesAndTheShareOfSplitsOverAllOfThem`               | Fact   | A random forest draws its first trees and the share of splits over all of them.               |
| `AdaBoostWeightsTheShareOfSplitsByEachTreesAlpha`                               | Fact   | Ada boost weights the share of splits by each trees alpha.                                    |
| `AnIsolationForestSplitsStrictlyBelowTheValueAndCountsRows`                     | Fact   | An isolation forest splits strictly below the value and counts rows.                          |
| `GradientBoostingStatesItsBaseScoreAndLearningRate`                             | Fact   | Gradient boosting states its base score and learning rate.                                    |
| `NeuralNetworksDrawInputsHiddenUnitsAndOneOutputWithTheirWeights`               | Theory | Neural networks draw inputs hidden units and one output with their weights.                   |
| `ANetworkWithMismatchedOutputWeightsIsRejected`                                 | Fact   | A network with mismatched output weights is rejected.                                         |
| `MultipleLinearRegressionOmitsConstantsAndAddsMissingIndicators`                | Fact   | Multiple linear regression omits constants and adds missing indicators.                       |
| `MultipleLinearRegressionPrefersEvidenceWithStandardErrors`                     | Fact   | Multiple linear regression prefers evidence with standard errors.                             |
| `ALinearSupportVectorClassifierDrawsItsHyperplaneWeights`                       | Fact   | A linear support vector classifier draws its hyperplane weights.                              |
| `KNearestNeighboursClassifierProjectsReferenceRowsByClass`                      | Fact   | K nearest neighbours classifier projects reference rows by class.                             |
| `KNearestNeighboursRegressorGroupsRowsIntoTargetQuartersAndThinsLargeSets`      | Fact   | K nearest neighbours regressor groups rows into target quarters and thins large sets.         |
| `AKernelSupportVectorClassifierSplitsSupportVectorsByTheClassTheyPullTowards`   | Fact   | A kernel support vector classifier splits support vectors by the class they pull towards.     |
| `AHostileKernelNameIsNotEchoed`                                                 | Fact   | A hostile kernel name is not echoed.                                                          |
| `AOneClassSupportVectorModelDrawsItsSupportVectors`                             | Fact   | A one class support vector model draws its support vectors.                                   |
| `SupportSetsWithRowsOfTheWrongWidthAreRejected`                                 | Fact   | Support sets with rows of the wrong width are rejected.                                       |

### SnapshotEnvelopeAdapterTests

`Jube.Test.ModelDiagrams.SnapshotEnvelopeAdapterTests` — 20 tests.

| Test                                                                   | Kind | Verifies                                                                        |
|------------------------------------------------------------------------|------|---------------------------------------------------------------------------------|
| `AStandardisedPipelineShowsStandardisedLogOddsPerVariable`             | Fact | A standardised pipeline shows standardised log odds per variable.               |
| `ConstantVariablesAreOmittedAndCounted`                                | Fact | Constant variables are omitted and counted.                                     |
| `AnUnstandardisedPipelineIsLabelledOriginalScaleAndKeepsEveryVariable` | Fact | An unstandardised pipeline is labelled original scale and keeps every variable. |
| `TheCaptionCarriesTheInterceptPriorsAndCalibrator`                     | Fact | The caption carries the intercept priors and calibrator.                        |
| `AMissingCalibratorIsSaidSo`                                           | Fact | A missing calibrator is said so.                                                |
| `AnUnknownCalibratorMethodIsNamedCustomNotEchoed`                      | Fact | An unknown calibrator method is named custom not echoed.                        |
| `PayloadStandardErrorsGiveIntervals`                                   | Fact | Payload standard errors give intervals.                                         |
| `EvidenceGivesOddsRatiosWithIntervalsOnALogScale`                      | Fact | Evidence gives odds ratios with intervals on a log scale.                       |
| `EvidenceWithoutAnOddsRatioUsesTheExponentOfTheEstimate`               | Fact | Evidence without an odds ratio uses the exponent of the estimate.               |
| `EvidenceRowsNeedANameAndAnEstimate`                                   | Fact | Evidence rows need a name and an estimate.                                      |
| `VariableNamesFromTheOptionsRelabelThePipeline`                        | Fact | Variable names from the options relabel the pipeline.                           |
| `AWeightCountThatDoesNotMatchTheVariablesIsMalformed`                  | Fact | A weight count that does not match the variables is malformed.                  |
| `AnIndicatorForAVariableThatDoesNotExistIsMalformed`                   | Fact | An indicator for a variable that does not exist is malformed.                   |
| `APipelineWithOnlyConstantVariablesDoesNotVisualise`                   | Fact | A pipeline with only constant variables does not visualise.                     |
| `AnUnknownKindInAValidEnvelopeIsNotSupportedAndListsTheKnownKinds`     | Fact | An unknown kind in a valid envelope is not supported and lists the known kinds. |
| `AHostileKindIsCleanedInTheMessage`                                    | Fact | A hostile kind is cleaned in the message.                                       |
| `KindMatchingIgnoresCase`                                              | Fact | Kind matching ignores case.                                                     |
| `TheOlderRawLogisticKindShowsLogOddsWithStandardErrors`                | Fact | The older raw logistic kind shows log odds with standard errors.                |
| `AFamilyAddsItsKindInOnePlace`                                         | Fact | A family adds its kind in one place.                                            |
| `ANonEnvelopeObjectIsNotClaimed`                                       | Fact | A non envelope object is not claimed.                                           |

### TrainedSnapshotDiagramTests

`Jube.Test.ModelDiagrams.TrainedSnapshotDiagramTests` — 3 tests.

| Test                                                           | Kind   | Verifies                                                                 |
|----------------------------------------------------------------|--------|--------------------------------------------------------------------------|
| `EveryEngineModelKindHasARegisteredDiagram`                    | Fact   | Every engine model kind has a registered diagram.                        |
| `EveryRegisteredKindIsUniqueAndEveryPipelineKindIsTrainedHere` | Fact   | Every registered kind is unique and every pipeline kind is trained here. |
| `ARealTrainedSnapshotOfEveryKindRendersADiagram`               | Theory | A real trained snapshot of every kind renders a diagram.                 |
