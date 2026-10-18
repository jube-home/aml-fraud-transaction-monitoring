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

using Jube.Charting.Charts;
using Jube.Charting.More;
using Jube.Charting.More.Models;
using Jube.Charting.Performance;

namespace Jube.Charting
{
    public static class Chart
    {
        public static LineChartBuilder Line()
        {
            return new LineChartBuilder();
        }

        public static AreaChartBuilder Area()
        {
            return new AreaChartBuilder();
        }

        public static ScatterChartBuilder Scatter()
        {
            return new ScatterChartBuilder();
        }

        public static BarChartBuilder Column()
        {
            return new BarChartBuilder();
        }

        public static BarChartBuilder Bar()
        {
            return new BarChartBuilder().Horizontal();
        }

        public static HistogramChartBuilder Histogram()
        {
            return new HistogramChartBuilder();
        }

        public static PieChartBuilder Pie()
        {
            return new PieChartBuilder();
        }

        public static PieChartBuilder Donut(double innerRadiusRatio = 0.6)
        {
            return new PieChartBuilder().Donut(innerRadiusRatio);
        }

        public static HeatmapChartBuilder Heatmap()
        {
            return new HeatmapChartBuilder();
        }

        public static RocChartBuilder Roc()
        {
            return new RocChartBuilder();
        }

        public static PrecisionRecallChartBuilder PrecisionRecall()
        {
            return new PrecisionRecallChartBuilder();
        }

        public static CalibrationChartBuilder Calibration()
        {
            return new CalibrationChartBuilder();
        }

        public static ConfusionMatrixChartBuilder ConfusionMatrix()
        {
            return new ConfusionMatrixChartBuilder();
        }

        public static LearningCurveChartBuilder LearningCurve()
        {
            return new LearningCurveChartBuilder();
        }

        public static ComboChartBuilder Combo()
        {
            return new ComboChartBuilder();
        }

        public static BoxPlotChartBuilder BoxPlot()
        {
            return new BoxPlotChartBuilder();
        }

        public static CandlestickChartBuilder Candlestick()
        {
            return new CandlestickChartBuilder();
        }

        public static WaterfallChartBuilder Waterfall()
        {
            return new WaterfallChartBuilder();
        }

        public static FunnelChartBuilder Funnel()
        {
            return new FunnelChartBuilder();
        }

        public static RadarChartBuilder Radar()
        {
            return new RadarChartBuilder();
        }

        public static PolarChartBuilder Polar()
        {
            return new PolarChartBuilder();
        }

        public static GaugeChartBuilder Gauge()
        {
            return new GaugeChartBuilder();
        }

        public static BulletChartBuilder Bullet()
        {
            return new BulletChartBuilder();
        }

        public static BulletChartBuilder LinearGauge()
        {
            return new BulletChartBuilder().Thick();
        }

        public static SparklineChartBuilder Sparkline()
        {
            return new SparklineChartBuilder();
        }

        public static TreemapChartBuilder Treemap()
        {
            return new TreemapChartBuilder();
        }

        public static GanttChartBuilder Gantt()
        {
            return new GanttChartBuilder();
        }

        public static SurvivalChartBuilder Survival()
        {
            return new SurvivalChartBuilder();
        }

        public static BandChartBuilder TimeSeries()
        {
            return new BandChartBuilder().TimeAxis().Kind("Time series chart");
        }

        public static BandChartBuilder StepChart()
        {
            return new BandChartBuilder().Kind("Step chart").DefaultStyle(BandSeriesStyle.Step);
        }

        public static BandChartBuilder ErrorBars()
        {
            return new BandChartBuilder().Kind("Error bar chart");
        }

        public static RegressionChartBuilder Regression()
        {
            return new RegressionChartBuilder();
        }

        public static ParallelChartBuilder ParallelCoordinates()
        {
            return new ParallelChartBuilder();
        }

        public static FacetChartBuilder Facets()
        {
            return new FacetChartBuilder();
        }

        public static ScatterMatrixChartBuilder ScatterMatrix()
        {
            return new ScatterMatrixChartBuilder();
        }

        public static DensityChartBuilder Density()
        {
            return new DensityChartBuilder();
        }

        public static EcdfChartBuilder Ecdf()
        {
            return new EcdfChartBuilder();
        }

        public static ViolinChartBuilder Violin()
        {
            return new ViolinChartBuilder();
        }

        public static RidgelineChartBuilder Ridgeline()
        {
            return new RidgelineChartBuilder();
        }

        public static DotChartBuilder DotPlot()
        {
            return new DotChartBuilder();
        }

        public static DotChartBuilder Lollipop()
        {
            return new DotChartBuilder().Style(DotStyle.Lollipop);
        }

        public static DotChartBuilder Dumbbell()
        {
            return new DotChartBuilder().Style(DotStyle.Dumbbell);
        }

        public static SlopeChartBuilder Slope()
        {
            return new SlopeChartBuilder();
        }

        public static SlopeChartBuilder Bump()
        {
            return new SlopeChartBuilder().Mode(SlopeMode.Ranks).Smooth();
        }

        public static CorrelationMatrixChartBuilder CorrelationMatrix()
        {
            return new CorrelationMatrixChartBuilder();
        }

        public static MatrixChartBuilder Matrix()
        {
            return new MatrixChartBuilder();
        }

        public static MatrixChartBuilder Adjacency()
        {
            return new MatrixChartBuilder().ShowValues(false);
        }

        public static CalendarChartBuilder CalendarHeatmap()
        {
            return new CalendarChartBuilder();
        }

        public static SankeyChartBuilder Sankey()
        {
            return new SankeyChartBuilder();
        }

        public static StreamChartBuilder Stream()
        {
            return new StreamChartBuilder();
        }

        public static StreamChartBuilder PercentStream()
        {
            return new StreamChartBuilder().Offset(StreamOffset.Expand).Order(StreamOrder.None).Smooth(false);
        }

        public static GainsChartBuilder Gains()
        {
            return new GainsChartBuilder();
        }

        public static LiftChartBuilder Lift()
        {
            return new LiftChartBuilder();
        }

        public static KsChartBuilder Ks()
        {
            return new KsChartBuilder();
        }

        public static ThresholdSweepChartBuilder ThresholdSweep()
        {
            return new ThresholdSweepChartBuilder();
        }

        public static ScoreDistributionChartBuilder ScoreDistribution()
        {
            return new ScoreDistributionChartBuilder();
        }

        public static MetricsPanelChartBuilder MetricsPanel()
        {
            return new MetricsPanelChartBuilder();
        }

        public static PromotionHistoryChartBuilder PromotionHistory()
        {
            return new PromotionHistoryChartBuilder();
        }
    }
}