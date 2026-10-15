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

using System;
using System.Collections.Generic;
using System.Linq;
using Jube.Charting;
using Jube.Charting.Charts.Models;
using Jube.Charting.Interfaces;

namespace Jube.Test.Charting
{
    public static class ChartSamples
    {
        public static IReadOnlyDictionary<string, ISvgChart> All()
        {
            var samples = new SortedDictionary<string, ISvgChart>(StringComparer.Ordinal)
            {
                ["line"] = Line(),
                ["line-time"] = LineTime(),
                ["area-overlap"] = Area(false),
                ["area-stacked"] = Area(true),
                ["column-grouped"] = Column(false),
                ["column-stacked"] = Column(true),
                ["bar-horizontal"] = BarHorizontal(),
                ["scatter"] = Scatter(),
                ["bubble"] = Bubble(),
                ["histogram"] = Histogram(),
                ["pie"] = Pie(),
                ["donut"] = Donut(),
                ["heatmap"] = Heatmap(),
                ["correlation"] = Correlation(),
                ["roc"] = Roc(),
                ["precision-recall"] = PrecisionRecall(),
                ["calibration"] = Calibration(),
                ["confusion-counts"] = Confusion(false),
                ["confusion-rates"] = Confusion(true),
                ["learning-curve"] = LearningCurve(),
                ["predicted-actual"] = PredictedActual(),
                ["line-spline-secondary"] = LineSecondary(),
                ["area-percent"] = AreaPercent(),
                ["column-percent"] = ColumnPercent(),
                ["bar-range"] = BarRange(),
                ["combo"] = Combo(),
                ["scatter-trend"] = ScatterTrend(),
                ["box-plot"] = BoxPlot(),
                ["candlestick"] = Candles(false),
                ["ohlc"] = Candles(true),
                ["waterfall"] = Waterfall(),
                ["funnel"] = Funnel(),
                ["radar"] = Radar(),
                ["polar"] = Polar(),
                ["gauge-radial"] = Gauge(false),
                ["gauge-ranges"] = Gauge(true),
                ["bullet"] = Bullet(),
                ["gauge-linear"] = LinearGauge(),
                ["sparkline-line"] = Spark(SparklineKind.Line),
                ["sparkline-area"] = Spark(SparklineKind.Area),
                ["sparkline-column"] = Spark(SparklineKind.Column),
                ["sparkline-winloss"] = Spark(SparklineKind.WinLoss),
                ["treemap-flat"] = TreemapFlat(),
                ["treemap-grouped"] = TreemapGrouped(),
                ["gantt"] = Gantt()
            };

            return samples;
        }

        public static IReadOnlyList<double> Values(int seed, int count, double start, double drift, double noise)
        {
            var random = new Random(seed);
            var result = new double[count];
            var v = start;

            for (var i = 0; i < count; i++)
            {
                v += drift + (random.NextDouble() - 0.5) * noise;
                result[i] = v;
            }

            return result;
        }

        public static IEnumerable<double> Gaussian(int seed, int count, double mean, double sd)
        {
            var random = new Random(seed);

            for (var i = 0; i < count; i++)
            {
                var u1 = 1.0 - random.NextDouble();
                var u2 = random.NextDouble();
                yield return mean + sd * Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            }
        }

        public static ISvgChart Line()
        {
            return Chart.Line().Title("Daily alert volume").Subtitle("Alerts raised per day by channel")
                .XAxisTitle("Day").YAxisTitle("Alerts")
                .Series("Card present", Values(1, 30, 120, 1.5, 30))
                .Series("Card not present", Values(2, 30, 90, 3, 40))
                .Series("Transfer", Values(3, 30, 60, 0.5, 20));
        }

        public static ISvgChart LineTime()
        {
            var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var times = Enumerable.Range(0, 90).Select(i => start.AddDays(i)).ToArray();
            var values = Values(4, 90, 50, 0.4, 12).ToArray();
            values[40] = double.NaN;
            values[41] = double.NaN;
            values[42] = double.NaN;

            return Chart.Line().Title("Fraud rate over time").YAxisTitle("Rate per 10,000")
                .TimeSeries("Fraud rate", times, values);
        }

        public static ISvgChart Area(bool stacked)
        {
            return Chart.Area().Title(stacked ? "Stacked alert mix" : "Overlapping alert volumes")
                .Categories(["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"])
                .Stacked(stacked)
                .Series("Rules", [120.0, 150, 140, 170, 200, 90, 60])
                .Series("Models", [80.0, 90, 110, 100, 130, 70, 40])
                .Series("Sanctions", [20.0, 25, 15, 30, 22, 10, 12]);
        }

        public static ISvgChart Column(bool stacked)
        {
            return Chart.Column().Title(stacked ? "Alerts by channel, stacked" : "Alerts by channel and outcome")
                .Categories(["Card present", "Card not present", "Transfer", "ATM", "Cheque"])
                .Stacked(stacked)
                .YAxisTitle("Alerts")
                .Series("Confirmed", [120.0, 260, 90, 40, 15], null, stacked ? null : new[] { 12.0, 20, 9, 5, 3 })
                .Series("False positive", [340.0, 410, 150, 90, 60]);
        }

        public static ISvgChart BarHorizontal()
        {
            return Chart.Bar().Title("Variable prescriptions").Subtitle("Sensitivity of the winning model")
                .Categories([
                    "AmountBase", "MerchantCategoryRisk", "TransactionCountLastHour", "CountryMismatch", "DeviceAge",
                    "NewPayee", "OverseasFlag", "TimeOfDay"
                ])
                .Series("Sensitivity", [0.42, 0.31, 0.27, 0.19, 0.11, -0.04, -0.09, -0.18])
                .ShowValues();
        }

        public static ISvgChart Scatter()
        {
            var g = Gaussian(5, 120, 50, 12).ToArray();
            var h = Gaussian(6, 120, 60, 15).ToArray();
            var f = Gaussian(7, 30, 90, 10).ToArray();
            var g2 = Gaussian(8, 120, 400, 90).ToArray();
            var h2 = Gaussian(9, 120, 300, 80).ToArray();
            var f2 = Gaussian(10, 30, 900, 120).ToArray();

            return Chart.Scatter().Title("Amount versus velocity").XAxisTitle("Velocity").YAxisTitle("Amount")
                .Series("Genuine", g, g2).Series("Fraud", f, f2).Series("Review", h, h2);
        }

        public static ISvgChart Bubble()
        {
            return Chart.Scatter().Title("Merchant risk").XAxisTitle("Chargeback rate").YAxisTitle("Fraud rate")
                .Bubbles("Merchants", [0.5, 1.2, 2.5, 3.1, 4.4, 0.8], [0.1, 0.4, 0.9, 1.6, 2.4, 0.3],
                    [1200.0, 5400, 800, 15000, 2200, 300]);
        }

        public static ISvgChart Histogram()
        {
            return Chart.Histogram().Title("Score distribution").Subtitle("Fraud versus genuine, normalised")
                .XAxisTitle("Score").YAxisTitle("Share of series").Normalized()
                .Values("Genuine", Gaussian(11, 4000, 0.25, 0.12))
                .Values("Fraud", Gaussian(12, 300, 0.7, 0.15));
        }

        public static ISvgChart Pie()
        {
            return Chart.Pie().Title("Alerts by disposition")
                .Slice("Confirmed", 320).Slice("False positive", 1450).Slice("Under review", 210).Slice("Escalated", 90)
                .Slice("Closed", 40);
        }

        public static ISvgChart Donut()
        {
            return Chart.Donut().Title("Cases by team").CenterLabel("2,110")
                .Slice("Fraud operations", 900).Slice("AML", 700).Slice("Sanctions", 300).Slice("Disputes", 210);
        }

        public static ISvgChart Heatmap()
        {
            var rows = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            var cols = Enumerable.Range(0, 12).Select(i => (i * 2).ToString("00") + "h").ToArray();
            var random = new Random(13);

            var values = rows.Select((_, r) => cols.Select((_, c) =>
                    Math.Round(
                        10 + 40 * Math.Exp(-Math.Pow(c - 8, 2) / 8) + random.NextDouble() * 15 + (r > 4 ? 10 : 0)))
                .ToArray()).ToArray();

            return Chart.Heatmap().Title("Alerts by hour and weekday").Rows(rows).Columns(cols).Values(values)
                .Decimals(0);
        }

        public static ISvgChart Correlation()
        {
            var labels = new[] { "Amount", "Velocity", "Age", "Distance", "Hour" };

            var m = new[,]
            {
                { 1, 0.62, -0.21, 0.35, 0.05 },
                { 0.62, 1, -0.11, 0.28, 0.12 },
                { -0.21, -0.11, 1, -0.4, 0.02 },
                { 0.35, 0.28, -0.4, 1, -0.08 },
                { 0.05, 0.12, 0.02, -0.08, 1 }
            };

            return Chart.Heatmap().Title("Variable correlation").Correlation(labels, m);
        }

        public static (bool[] Actual, double[] Scores) Scored(int seed, int n, double separation)
        {
            var random = new Random(seed);
            var actual = new bool[n];
            var scores = new double[n];

            for (var i = 0; i < n; i++)
            {
                actual[i] = random.NextDouble() < 0.08;
                var u1 = 1.0 - random.NextDouble();
                var u2 = random.NextDouble();
                var z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
                var raw = (actual[i] ? separation : 0) + z;
                scores[i] = 1 / (1 + Math.Exp(-(raw - 1.8)));
            }

            return (actual, scores);
        }

        public static ISvgChart Roc()
        {
            var (a1, s1) = Scored(14, 3000, 2.2);
            var (a2, s2) = Scored(15, 3000, 1.3);

            return Chart.Roc().Title("Model discrimination").Subtitle("Held-out validation")
                .FromScores("Neural network", a1, s1).FromScores("Logistic", a2, s2);
        }

        public static ISvgChart PrecisionRecall()
        {
            var (a1, s1) = Scored(16, 3000, 2.2);
            var (a2, s2) = Scored(17, 3000, 1.3);

            return Chart.PrecisionRecall().Title("Precision and recall")
                .FromScores("Neural network", a1, s1).FromScores("Logistic", a2, s2);
        }

        public static ISvgChart Calibration()
        {
            var (a, s) = Scored(18, 5000, 2.0);

            return Chart.Calibration().Title("Calibration").FromProbabilities("Neural network", a, s);
        }

        public static ISvgChart Confusion(bool rates)
        {
            return Chart.ConfusionMatrix().Title(rates ? "Confusion matrix, row rates" : "Confusion matrix")
                .Binary(9120, 180, 45, 155, "Genuine", "Fraud").Rates(rates);
        }

        public static ISvgChart LearningCurve()
        {
            var train = Enumerable.Range(1, 80).Select(i => 0.9 * Math.Exp(-i / 18.0) + 0.05).ToArray();

            var validation = Enumerable.Range(1, 80)
                .Select(i => 0.9 * Math.Exp(-i / 14.0) + 0.12 + Math.Max(0, i - 45) * 0.0025).ToArray();

            return Chart.LearningCurve().Title("Learning curve").Metric("Log loss").Train(train).Validation(validation);
        }

        public static ISvgChart PredictedActual()
        {
            var xs = Gaussian(19, 80, 50, 15).ToArray();
            var noise = Gaussian(20, 80, 0, 6).ToArray();
            var ys = xs.Select((x, i) => x + noise[i]).ToArray();

            return Chart.Scatter().Title("Predicted versus actual").XAxisTitle("Actual").YAxisTitle("Predicted")
                .Series("Held-out", xs, ys).Diagonal("Perfect prediction");
        }

        public static ISvgChart LineSecondary()
        {
            var alerts = Values(21, 24, 200, 2, 40).ToArray();
            var rate = Values(22, 24, 3, 0.02, 0.4).ToArray();

            return Chart.Line().Title("Alert volume and confirmation rate").Smooth()
                .XAxisTitle("Hour").YAxisTitle("Alerts").SecondaryYAxisTitle("Confirmation rate")
                .Series("Alerts", alerts).Series("Rate", rate).OnSecondaryAxis()
                .HorizontalBand(240, 300, "Capacity warning").HorizontalLine(220, "Target")
                .Annotate(12, alerts[12], "Campaign start");
        }

        public static ISvgChart AreaPercent()
        {
            return Chart.Area().Title("Channel mix").PercentStacked().Smooth()
                .Categories(["Q1", "Q2", "Q3", "Q4"])
                .Series("Card", [40.0, 45, 50, 42]).Series("Transfer", [30.0, 28, 25, 33])
                .Series("Cash", [30.0, 27, 25, 25]);
        }

        public static ISvgChart ColumnPercent()
        {
            return Chart.Column().Title("Outcome mix by segment").PercentStacked()
                .Categories(["Retail", "Business", "Private"])
                .Series("Confirmed", [12.0, 30, 5]).Series("False positive", [80.0, 50, 15])
                .Series("Pending", [8.0, 20, 5]);
        }

        public static ISvgChart BarRange()
        {
            return Chart.Bar().Title("Investigation duration range").Subtitle("Days from open to close")
                .Categories(["Fraud", "AML", "Sanctions", "Disputes"])
                .Range("Duration", [1.0, 3, 0.5, 2], [6.0, 21, 3, 14])
                .Band(7, 14, "Service level breach").Line(10, "Target");
        }

        public static ISvgChart Combo()
        {
            return Chart.Combo().Title("Alerts and confirmation rate")
                .Categories(["Jan", "Feb", "Mar", "Apr", "May", "Jun"])
                .Columns("Alerts", [420.0, 380, 510, 470, 530, 490])
                .Columns("Confirmed", [60.0, 52, 90, 71, 95, 80])
                .Line("Confirmation rate", [0.14, 0.137, 0.176, 0.151, 0.179, 0.163], null, true)
                .SecondaryYAxisTitle("Rate").SecondaryFormat(v => Math.Round(v * 100) + "%").YAxisTitle("Alerts");
        }

        public static ISvgChart ScatterTrend()
        {
            var xs = Gaussian(23, 60, 50, 15).ToArray();
            var noise = Gaussian(24, 60, 0, 12).ToArray();
            var ys = xs.Select((x, i) => 2 * x + 10 + noise[i]).ToArray();

            return Chart.Scatter().Title("Amount by customer age").XAxisTitle("Age").YAxisTitle("Amount")
                .Series("Customers", xs, ys).TrendLines();
        }

        public static ISvgChart BoxPlot()
        {
            return Chart.BoxPlot().Title("Amount by channel").YAxisTitle("Amount")
                .FromValues("Card present", Gaussian(25, 300, 80, 25).Concat([220.0, 260]))
                .FromValues("Card not present", Gaussian(26, 300, 120, 40).Concat([330.0]))
                .FromValues("Transfer", Gaussian(27, 300, 400, 120))
                .FromValues("ATM", Gaussian(28, 300, 200, 30));
        }

        public static ISvgChart Candles(bool ohlc)
        {
            var random = new Random(29);
            var price = 100.0;
            var chart = Chart.Candlestick().Title(ohlc ? "Daily volume index OHLC" : "Daily volume index").Ohlc(ohlc);
            var start = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

            for (var i = 0; i < 30; i++)
            {
                var open = price;
                var close = open + (random.NextDouble() - 0.45) * 6;
                var high = Math.Max(open, close) + random.NextDouble() * 3;
                var low = Math.Min(open, close) - random.NextDouble() * 3;

                chart.Candle(start.AddDays(i), Math.Round(open, 2), Math.Round(high, 2), Math.Round(low, 2),
                    Math.Round(close, 2));

                price = close;
            }

            return chart;
        }

        public static ISvgChart Waterfall()
        {
            return Chart.Waterfall().Title("Loss bridge").Subtitle("From gross fraud to net loss")
                .Total("Gross fraud", 1000).Step("Blocked", -420).Step("Recovered", -180).Step("Chargebacks", 90)
                .Step("Fees", 35).Total("Net loss");
        }

        public static ISvgChart Funnel()
        {
            return Chart.Funnel().Title("Alert to case funnel")
                .Stage("Alerts", 12000).Stage("Triaged", 7400).Stage("Investigated", 2100).Stage("Confirmed", 640)
                .Stage("Reported", 210);
        }

        public static ISvgChart Radar()
        {
            return Chart.Radar().Title("Model comparison")
                .Axes(["Precision", "Recall", "Stability", "Speed", "Explainability", "Coverage"])
                .Series("Neural network", [0.82, 0.9, 0.6, 0.7, 0.3, 0.85])
                .Series("Logistic", [0.7, 0.72, 0.9, 0.95, 0.9, 0.7]);
        }

        public static ISvgChart Polar()
        {
            var angles = Enumerable.Range(0, 24).Select(i => i * 15.0).ToArray();

            return Chart.Polar().Title("Alerts by hour of day").Spokes(8)
                .Series("Weekday", angles,
                    [.. angles.Select(a => 30 + 25 * Math.Sin((a - 100) * Math.PI / 180) + 5)]).Areas();
        }

        public static ISvgChart Gauge(bool ranges)
        {
            var g = Chart.Gauge().Title(ranges ? "Alert queue health" : "Model score")
                .Value(ranges ? 72 : 0.64, ranges ? "Queue load" : "Score").Target(ranges ? 60 : 0.8);

            if (ranges)
            {
                g.Range(0, 100).Band(0, 50, "Healthy").Band(50, 80, "Elevated").Band(80, 100, "Critical");
            }
            else
            {
                g.Range(0, 1);
            }

            return g;
        }

        public static ISvgChart Bullet()
        {
            return Chart.Bullet().Title("Team KPIs").Size(560, 260)
                .Item("Alerts triaged", 108, 100, [60.0, 90, 120])
                .Item("Cases closed", 78, 90, [60.0, 80, 120])
                .Item("SLA met", 96, 98, [90.0, 95, 120]);
        }

        public static ISvgChart LinearGauge()
        {
            return Chart.LinearGauge().Title("Capacity").Size(520, 120)
                .Item("Queue depth", 640, 800, [500.0, 750, 1000]);
        }

        public static ISvgChart Spark(SparklineKind kind)
        {
            var values = kind == SparklineKind.WinLoss
                ? Values(30, 20, 0, 0, 2).Select(v => Math.Round(v)).ToArray()
                : Values(31, 30, 10, 0.3, 6).ToArray();

            return Chart.Sparkline().Kind(kind).Values(values).MarkExtremes(kind == SparklineKind.Line).Name("Trend");
        }

        public static ISvgChart TreemapFlat()
        {
            return Chart.Treemap().Title("Alerts by rule")
                .Item("Velocity", 420).Item("Amount", 310).Item("New payee", 260).Item("Geography", 190)
                .Item("Device", 120)
                .Item("Time of day", 90).Item("Merchant", 70).Item("Dormant", 40).Item("Structuring", 25)
                .Item("Other", 15);
        }

        public static ISvgChart TreemapGrouped()
        {
            return Chart.Treemap().Title("Cases by team and type")
                .Group("Fraud", [("Card", 420.0), ("Account takeover", 260), ("Authorised push", 180)])
                .Group("AML", [("Structuring", 300.0), ("Layering", 150), ("Mule", 120)])
                .Group("Sanctions", [("Name match", 200.0), ("Country", 60)]);
        }

        public static ISvgChart Gantt()
        {
            var d = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);

            return Chart.Gantt().Title("Model release plan").Marker(d.AddDays(30), "Today")
                .Task("Data preparation", d, d.AddDays(21), "Build", 1)
                .Task("Training", d.AddDays(14), d.AddDays(40), "Build", 0.6)
                .Task("Validation", d.AddDays(35), d.AddDays(55), "Governance", 0.1)
                .Task("Approval", d.AddDays(55), d.AddDays(65), "Governance")
                .Milestone("Go live", d.AddDays(70), "Release");
        }
    }
}