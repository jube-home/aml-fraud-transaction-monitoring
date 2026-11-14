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
using Jube.Charting.Interfaces;
using Jube.Charting.Diagrams;
using Jube.Charting.Diagrams.Models;

namespace Jube.Test.Charting.Diagrams
{
    public static class DiagramSamples
    {
        private static readonly string[] pcaFeatures =
        [
            "amount_z", "velocity_1h", "merchant_risk", "geo_mismatch", "account_age", "night_time", "channel_atm",
            "balance_ratio"
        ];

        private static readonly string[] features =
            ["amount_z", "velocity_1h", "channel", "merchant_risk", "account_age", "geo_mismatch"];

        public static readonly DateTimeOffset Epoch = new(2026, 3, 2, 9, 0, 0, TimeSpan.Zero);

        public static IReadOnlyDictionary<string, ISvgChart> All()
        {
            return new SortedDictionary<string, ISvgChart>(StringComparer.Ordinal)
            {
                ["network-xor"] = Xor(),
                ["tree-fraud"] = FraudTree(),
                ["graph-tan"] = Tan(),
                ["graph-hmm"] = Hmm(),
                ["coefficients-logistic"] = Logistic(),
                ["forest"] = Forest(),
                ["distributions"] = Distributions(4),
                ["fuzzy"] = Fuzzy(),
                ["components-pca"] = Pca(),
                ["components-clusters"] = Clusters(),
                ["graph-force-ring"] = ForceRing(),
                ["graph-radial-ring"] = RadialRing(),
                ["graph-flow-chain"] = FlowChain(),
                ["graph-combos"] = Combos()
            };
        }

        public static GraphDiagramBuilder ForceRing()
        {
            var b = Diagram.Graph().Title("Account ring").Subtitle("Eight accounts cycling funds through one hub")
                .Layout(GraphLayoutKind.Force).SizeBy(GraphMeasure.Degree).WeightName("Amount")
                .GroupOrder("Accounts", "Devices", "Network")
                .Node("hub", "Hub account", GraphNodeShape.Hexagon, group: "Accounts",
                    badges: [new GraphBadge(GraphBadgeKind.Hub), new GraphBadge(GraphBadgeKind.OpenCase)]);

            for (var i = 0; i < 8; i++)
            {
                b.Node("acct-" + i, "Account " + i, GraphNodeShape.Circle, group: "Accounts");
                b.Edge("acct-" + i, "hub", Epoch.AddHours(i), Epoch.AddHours(i + 2), 1 + i % 3, 1000 + 250 * i,
                    "t" + i);

                if (i > 0)
                {
                    b.Edge("acct-" + (i - 1), "acct-" + i, Epoch.AddHours(12 + i), amount: 400 + 50 * i);
                }
            }

            b.Node("dev-1", "Device 7f3a", GraphNodeShape.Square, group: "Devices").Edge("dev-1", "acct-2")
                .Edge("dev-1", "acct-5");
            b.Node("ip-1", "10.4.0.9", GraphNodeShape.Diamond, group: "Network").Edge("ip-1", "acct-1");
            b.Node("cp-1", "Counterparty", GraphNodeShape.Triangle, group: "Accounts",
                badges: [new GraphBadge(GraphBadgeKind.Confirmed)]).Edge("hub", "cp-1", 9800);

            return b;
        }

        public static GraphDiagramBuilder RadialRing()
        {
            var b = Diagram.Graph().Title("Neighbourhood of account 0").Subtitle("Two hops, rings by distance")
                .Layout(GraphLayoutKind.Radial).Seed("acct-0").GroupOrder("Accounts", "Devices", "Network")
                .Node("acct-0", "Account 0", GraphNodeShape.Circle, group: "Accounts",
                    badges: [new GraphBadge(GraphBadgeKind.OpenCase)]);

            for (var i = 1; i <= 4; i++)
            {
                b.Node("acct-" + i, "Account " + i, GraphNodeShape.Circle, group: "Accounts")
                    .Edge("acct-0", "acct-" + i, 500 * i);
            }

            b.Node("dev-1", "Device a1", GraphNodeShape.Square, group: "Devices").Edge("acct-1", "dev-1")
                .Edge("acct-2", "dev-1");
            b.Node("dev-2", "Device b2", GraphNodeShape.Square, group: "Devices").Edge("acct-3", "dev-2");
            b.Node("ip-1", "10.0.0.1", GraphNodeShape.Diamond, group: "Network").Edge("acct-4", "ip-1");
            b.Node("ip-2", "10.0.0.2", GraphNodeShape.Diamond, group: "Network").Edge("acct-4", "ip-2");
            b.Node("cp-1", "Merchant", GraphNodeShape.Triangle, group: "Accounts").Edge("acct-1", "cp-1", 120);
            b.Node("cp-2", "Exchange", GraphNodeShape.Triangle, group: "Accounts",
                badges: [new GraphBadge(GraphBadgeKind.Confirmed)]).Edge("acct-2", "cp-2", 2400);

            return b;
        }

        public static GraphDiagramBuilder FlowChain()
        {
            var b = Diagram.Graph().Title("Funds flow").Subtitle("Lanes by institution, columns by first movement")
                .Layout(GraphLayoutKind.Flow).WeightName("Amount").GroupOrder("Bank A", "Bank B", "Mules")
                .Node("a1", "Victim", GraphNodeShape.Circle, group: "Bank A",
                    badges: [new GraphBadge(GraphBadgeKind.Records, "3")])
                .Node("a2", "Account A2", GraphNodeShape.Circle, group: "Bank A")
                .Node("b1", "Account B1", GraphNodeShape.Circle, group: "Bank B")
                .Node("b2", "Account B2", GraphNodeShape.Circle, group: "Bank B")
                .Node("m1", "Mule 1", GraphNodeShape.Circle, group: "Mules",
                    badges: [new GraphBadge(GraphBadgeKind.Confirmed)])
                .Node("m2", "Mule 2", GraphNodeShape.Circle, group: "Mules")
                .Node("x1", "Exchange", GraphNodeShape.Triangle, group: "Mules")
                .Time("a1", Epoch)
                .Edge("a1", "a2", Epoch, Epoch.AddMinutes(5), 3, 4500, "p1")
                .Edge("a2", "b1", Epoch.AddMinutes(20), Epoch.AddMinutes(20), 1, 3000, "p2")
                .Edge("a2", "b2", Epoch.AddMinutes(25), Epoch.AddMinutes(25), 1, 1400, "p3")
                .Edge("b1", "m1", Epoch.AddHours(1), Epoch.AddHours(1), 1, 2900, "p4")
                .Edge("b1", "m1", Epoch.AddHours(2), Epoch.AddHours(2), 1, 100, "p5")
                .Edge("b2", "m2", Epoch.AddHours(2).AddMinutes(10), Epoch.AddHours(2).AddMinutes(10), 1, 1350, "p6")
                .Edge("m1", "x1", Epoch.AddHours(3), Epoch.AddHours(3), 2, 2950, "p7")
                .Edge("m2", "x1", Epoch.AddHours(3).AddMinutes(30), Epoch.AddHours(3).AddMinutes(30), 1, 1300, "p8")
                .Edge("m2", "m1", Epoch.AddHours(4), Epoch.AddHours(4), 1, 50, "p9");

            return b;
        }

        public static GraphDiagramBuilder Combos()
        {
            var b = Diagram.Graph().Title("Collapsed clusters").Subtitle("Stacks stand for groups over the node cap")
                .Layout(GraphLayoutKind.Force).SizeBy(GraphMeasure.Amount).GroupOrder("Accounts", "Clusters")
                .Node("seed", "Seed account", GraphNodeShape.Circle, 1200, "Accounts",
                    [new GraphBadge(GraphBadgeKind.OpenCase), new GraphBadge(GraphBadgeKind.Pattern)])
                .Node("c1", "Mule cluster", GraphNodeShape.Stack, 24000, "Clusters",
                    [new GraphBadge(GraphBadgeKind.Cluster, "12")])
                .Node("c2", "Merchant cluster", GraphNodeShape.Stack, 8000, "Clusters",
                    [new GraphBadge(GraphBadgeKind.Cluster, "40")])
                .Node("p1", "Payee", GraphNodeShape.Circle, 900, "Accounts",
                    [new GraphBadge(GraphBadgeKind.Confirmed), new GraphBadge(GraphBadgeKind.Records, "2")])
                .Node("p2", "Employer", GraphNodeShape.Circle, 3000, "Accounts")
                .Edge(new GraphEdge("seed", "c1") { Weight = 24000, Count = 12, Directed = true })
                .Edge(new GraphEdge("seed", "c2") { Weight = 8000, Count = 40, Directed = true })
                .Edge(new GraphEdge("seed", "p1") { Weight = 900, Directed = true })
                .Edge(new GraphEdge("p2", "seed") { Weight = 3000, Directed = true, Dashed = true, Label = "salary" })
                .Edge(new GraphEdge("p1", "c1") { Dashed = true, Directed = false, Label = "shared device" });

            return b;
        }

        public static IReadOnlyDictionary<string, ISvgChart> Showcase()
        {
            var all = new SortedDictionary<string, ISvgChart>(StringComparer.Ordinal)
            {
                ["network-wide"] = Wide(),
                ["coefficients-naive-bayes"] = NaiveBayesContributions(),
                ["forest-large"] = Forest(6, 4),
                ["distributions-large"] = Distributions(12),
                ["coefficients-odds-log"] = OddsRatios(),
                ["graph-naive-bayes"] = NaiveBayes(),
                ["graph-diamond"] = Diamond(),
                ["graph-chain"] = Chain(5),
                ["graph-latent"] = LatentTree(),
                ["graph-cycle"] = CyclicGraph(),
                ["tree-deep"] = DeepTree(),
                ["tree-regression"] = RegressionTree()
            };

            foreach (var pair in All())
            {
                all[pair.Key] = pair.Value;
            }

            return all;
        }

        public static double[][] Rows(params double[][] rows)
        {
            return rows;
        }

        public static TreeNode BalancedNode(int depth, double samples = 1024)
        {
            if (depth == 0)
            {
                return TreeNode.Leaf(samples, [samples * 0.6, samples * 0.4]);
            }

            return TreeNode.Split("f" + depth, depth, BalancedNode(depth - 1, samples / 2),
                BalancedNode(depth - 1, samples / 2), samples, [samples * 0.6, samples * 0.4], 0.48);
        }

        public static TreeDiagramBuilder Balanced(int depth)
        {
            return Diagram.Tree().Classes(["legitimate", "fraud"]).Root(BalancedNode(depth)).MaxDepth(depth + 1);
        }

        public static TreeDiagramBuilder FraudTree()
        {
            var root = TreeNode.Split("amount_z", 1.8,
                TreeNode.Split("velocity_1h", 4.5,
                    TreeNode.Leaf(310, [140, 170], impurity: 0.5),
                    TreeNode.SplitOn("channel", ["ecommerce", "atm", "wire"],
                        TreeNode.Leaf(120, [35, 85], impurity: 0.41), TreeNode.Leaf(70, [58, 12], impurity: 0.28), 190,
                        [93, 97], 0.5),
                    500, [233, 267], 0.498),
                TreeNode.Split("account_age_days", 90,
                    TreeNode.Leaf(240, [190, 50], impurity: 0.33),
                    TreeNode.Leaf(1260, [1240, 20], impurity: 0.026), 1500, [1430, 70], 0.089),
                2000, [1663, 337], 0.28);

            return Diagram.Tree().Title("Fraud decision tree").Subtitle("Gini impurity, depth 3")
                .Classes(["legitimate", "fraud"]).ImpurityName("gini").Root(root);
        }

        private static TreeNode Chain(int depth, int seed)
        {
            var random = new Random(seed);

            TreeNode Grow(int d, double samples)
            {
                if (d == 0 || samples < 12)
                {
                    return TreeNode.Leaf(samples, [samples * 0.7, samples * 0.3]);
                }

                var share = 0.3 + 0.4 * random.NextDouble();

                return TreeNode.Split("feature_" + (d + seed) % 13, Math.Round(random.NextDouble() * 10, 2),
                    Grow(d - 1, Math.Round(samples * share)), Grow(d - 1, Math.Round(samples * (1 - share))),
                    samples, [samples * 0.7, samples * 0.3], 0.3);
            }

            return Grow(depth, 6000);
        }

        public static TreeDiagramBuilder DeepTree()
        {
            return Diagram.Tree().Title("Deep tree").Subtitle("Collapsed below depth 4")
                .Classes(["legitimate", "fraud"])
                .Root(Chain(9, 3)).MaxDepth(4).ImpurityName("gini");
        }

        public static TreeDiagramBuilder RegressionTree()
        {
            var root = TreeNode.Split("income", 52000,
                TreeNode.Split("tenure_months", 18, TreeNode.Leaf(400, value: 212.4, impurity: 44.1),
                    TreeNode.Leaf(300, value: 380.9, impurity: 61.7), 700, value: 285.2, impurity: 90.5),
                TreeNode.Split("balance", 1200, TreeNode.Leaf(500, value: 610.0, impurity: 72.3),
                    TreeNode.Leaf(300, value: 902.2, impurity: 120.0), 800, value: 725.0, impurity: 140.2),
                1500, value: 520.6, impurity: 260.4);

            return Diagram.Tree().Title("Expected monthly spend").Subtitle("Regression tree").ImpurityName("variance")
                .ValueName("spend").Root(root);
        }

        public static ComponentsDiagramBuilder Pca()
        {
            return Diagram.Components().Title("Principal components")
                .Subtitle("Scree plot and loadings of the transaction features")
                .Features(pcaFeatures)
                .Component("PC1", 0.34, [0.52, 0.47, 0.41, 0.28, -0.22, 0.12, 0.05, -0.41])
                .Component("PC2", 0.21, [0.10, -0.18, 0.22, 0.55, 0.61, -0.30, 0.25, 0.08])
                .Component("PC3", 0.14, [-0.30, 0.25, -0.05, 0.12, 0.20, 0.72, -0.35, 0.36])
                .Component("PC4", 0.10, [0.05, 0.10, -0.45, 0.30, -0.15, 0.20, 0.78, 0.05])
                .Component("PC5", 0.08, [0.22, -0.55, 0.40, -0.10, 0.35, 0.10, 0.12, 0.55])
                .Component("PC6", 0.06, [-0.42, 0.28, 0.51, -0.22, 0.25, -0.02, -0.28, -0.20]);
        }

        public static ComponentsDiagramBuilder Clusters()
        {
            var features = new[]
            {
                "amount_z", "velocity_1h", "merchant_risk", "geo_mismatch", "account_age", "night_time", "channel_atm",
                "balance_ratio", "tenure"
            };

            return Diagram.Components().Title("Customer segments")
                .Subtitle("Standardised centroids of five k-means clusters").Features(features)
                .Cluster("Everyday spenders", 5200, [-0.2, -0.3, -0.4, -0.5, 0.6, -0.4, -0.2, 0.1, 0.5])
                .Cluster("High rollers", 1800, [1.6, 0.4, 0.2, 0.3, 0.9, -0.1, 0.5, 1.2, 1.0])
                .Cluster("New and active", 1500, [0.2, 1.4, 0.6, 0.4, -1.3, 0.3, 0.2, -0.4, -1.1])
                .Cluster("Night owls", 900, [0.3, 0.6, 0.5, 0.2, -0.2, 1.9, 0.9, -0.1, -0.2])
                .Cluster("Suspicious", 240, [1.1, 2.1, 1.8, 2.3, -1.1, 1.0, 1.4, -0.7, -0.9]);
        }

        public static MembershipFunctionsBuilder Fuzzy()
        {
            var b = Diagram.Fuzzy().Title("Fuzzy fraud risk").Subtitle("Mamdani system: amount and velocity give risk")
                .Variable("amount", 0, 5000, FuzzySet.Trapezoidal("low", 0, 0, 300, 1200),
                    FuzzySet.Triangular("medium", 500, 1800, 3200),
                    FuzzySet.Trapezoidal("high", 2400, 3600, 5000, 5000))
                .Variable("velocity", 0, 20, FuzzySet.Gaussian("slow", 2, 2), FuzzySet.Gaussian("normal", 8, 2.5),
                    FuzzySet.Sigmoid("fast", 0.9, 12.5))
                .Variable("risk", 0, 100, FuzzySet.Triangular("low", 0, 15, 40),
                    FuzzySet.Triangular("medium", 25, 50, 75), FuzzySet.Triangular("high", 60, 85, 100));

            b.Rule(new FuzzyTerm("risk", "high"), 1, true, new FuzzyTerm("amount", "high"),
                new FuzzyTerm("velocity", "fast"));
            b.Rule(new FuzzyTerm("risk", "medium"), 0.8, true, new FuzzyTerm("amount", "medium"),
                new FuzzyTerm("velocity", "normal"));

            b.Rule(new FuzzyTerm("risk", "medium"), 1, false, new FuzzyTerm("amount", "high"),
                new FuzzyTerm("velocity", "normal"));

            b.Rule(new FuzzyTerm("risk", "low"), 1, true, new FuzzyTerm("amount", "low"),
                new FuzzyTerm("velocity", "slow"));

            return b;
        }

        public static DistributionPanelsBuilder Distributions(int panels)
        {
            var b = Diagram.Distributions().Title("Naive Bayes feature distributions")
                .Subtitle("Per-class densities of each numeric feature");

            var means = new[] { (0.0, 2.1), (1.5, 4.2), (30.0, 9.0), (120.0, 340.0), (0.4, 0.7), (5.0, 8.0) };
            var sds = new[] { (1.0, 1.3), (1.2, 1.9), (8.0, 14.0), (60.0, 220.0), (0.1, 0.25), (2.0, 3.5) };

            var names = new[]
                { "amount_z", "velocity_1h", "account_age_days", "balance", "merchant_risk", "distance_km" };

            for (var i = 0; i < panels; i++)
            {
                var k = i % 6;

                if (i % 5 == 4)
                {
                    b.Panel(names[k] + (i >= 6 ? " (bins)" : string.Empty),
                        DistributionSeries.Histogram("legitimate", [0, 1, 2, 3, 4, 5, 6],
                            [0.05, 0.16, 0.34, 0.26, 0.13, 0.06]),
                        DistributionSeries.Histogram("fraud", [0, 1, 2, 3, 4, 5, 6],
                            [0.02, 0.05, 0.1, 0.22, 0.31, 0.3]));
                }
                else
                {
                    b.Panel(names[k] + (i >= 6 ? " (v" + (i / 6 + 1) + ")" : string.Empty),
                        DistributionSeries.Normal("legitimate", means[k].Item1, sds[k].Item1),
                        DistributionSeries.Normal("fraud", means[k].Item2, sds[k].Item2));
                }
            }

            return b;
        }

        public static ForestOfTreesBuilder Forest(int trees = 4, int depth = 3)
        {
            var b = Diagram.Forest().Title("Random forest")
                .Subtitle("First " + trees + " of 100 trees, depth capped at " + depth)
                .Classes(["legitimate", "fraud"]).TotalTrees(100).ImportanceName("Mean decrease in impurity")
                .TreeDepth(depth)
                .MaxTrees(trees);

            for (var i = 0; i < trees; i++)
            {
                b.Tree(Chain(4 + i % 2, 20 + i));
            }

            var values = new[] { 0.31, 0.22, 0.17, 0.11, 0.08, 0.06, 0.03, 0.02 };

            var names = new[]
            {
                "amount_z", "velocity_1h", "merchant_risk", "geo_mismatch", "account_age", "channel", "night_time",
                "card_present"
            };

            for (var i = 0; i < values.Length; i++)
            {
                b.Importance(names[i], values[i]);
            }

            return b;
        }

        public static CoefficientDiagramBuilder Logistic()
        {
            return Diagram.Coefficients().Title("Fraud logistic regression")
                .Subtitle("Log-odds coefficients with 95% confidence intervals")
                .OddsRatios().EstimateName("Log-odds")
                .Item("amount_z", 1.42, 1.18, 1.66, 0.00001)
                .Item("velocity_1h", 0.88, 0.61, 1.15, 0.0001)
                .Item("merchant_risk", 0.64, 0.30, 0.98, 0.0004)
                .Item("geo_mismatch", 0.51, 0.02, 1.00, 0.041)
                .Item("night_time", 0.18, -0.12, 0.48, 0.24)
                .Item("channel_atm", -0.09, -0.52, 0.34, 0.68)
                .Item("card_present", -0.77, -1.05, -0.49, 0.00002)
                .Item("account_age_days", -1.05, -1.30, -0.80, 0.000001)
                .Item("known_device", -1.38, -1.75, -1.01, 0.000001)
                .Item("salary_deposit_regular", -0.34, -0.71, 0.03, 0.07);
        }

        public static CoefficientDiagramBuilder OddsRatios()
        {
            return Diagram.Coefficients().Title("Odds ratios").Subtitle("Log scale, reference line at 1").LogScale()
                .EstimateName("Odds ratio").Sort(CoefficientSort.Estimate)
                .Item("amount_z", 4.1, 3.2, 5.3, 0.00001).Item("velocity_1h", 2.4, 1.8, 3.2, 0.0001)
                .Item("night_time", 1.2, 0.89, 1.6, 0.24).Item("card_present", 0.46, 0.35, 0.61, 0.00002)
                .Item("known_device", 0.25, 0.17, 0.36, 0.000001);
        }

        public static CoefficientDiagramBuilder NaiveBayesContributions()
        {
            var b = Diagram.Coefficients().Title("Naive Bayes evidence")
                .Subtitle("Log-odds contribution of each feature value to fraud")
                .EstimateName("Log-odds contribution").Sort(CoefficientSort.Estimate);

            foreach (var (n, v) in new[]
                     {
                         ("velocity_1h > 4", 1.9), ("amount_z > 2", 1.3), ("channel = wire", 0.7),
                         ("geo mismatch", 0.45), ("night_time", 0.12), ("card_present", -0.6), ("known_device", -1.2),
                         ("account_age > 365", -1.6)
                     })
            {
                b.Item(n, v);
            }

            return b;
        }

        public static GraphDiagramBuilder NaiveBayes()
        {
            var b = Diagram.Graph().Title("Naive Bayes structure").Subtitle("Features independent given the class")
                .Node("class", "fraud", GraphNodeRole.Class);

            foreach (var f in features)
            {
                b.Node(f, f).Edge("class", f);
            }

            return b;
        }

        public static GraphDiagramBuilder Tan()
        {
            var b = NaiveBayes().Title("Tree-augmented naive Bayes")
                .Subtitle("Each feature has the class and at most one other feature as parents");

            b.Edge("amount_z", "velocity_1h", 0.31).Edge("amount_z", "channel", 0.12)
                .Edge("channel", "merchant_risk", 0.27).Edge("velocity_1h", "account_age", 0.44)
                .Edge("channel", "geo_mismatch", 0.18);

            return b.WeightName("Conditional mutual information");
        }

        public static GraphDiagramBuilder Diamond()
        {
            return Diagram.Graph().Title("Diamond").Node("a", "A", GraphNodeRole.Class).Node("b", "B").Node("c", "C")
                .Node("d", "D").Edge("a", "b").Edge("a", "c").Edge("b", "d").Edge("c", "d");
        }

        public static GraphDiagramBuilder Chain(int length)
        {
            var b = Diagram.Graph().Title("Chain");

            for (var i = 0; i < length; i++)
            {
                b.Node("n" + i, "N" + i);

                if (i > 0)
                {
                    b.Edge("n" + (i - 1), "n" + i);
                }
            }

            return b;
        }

        public static GraphDiagramBuilder LatentTree()
        {
            var b = Diagram.Graph().Title("Latent tree model").Subtitle("Hidden variables group correlated features")
                .Node("y", "fraud", GraphNodeRole.Class).Node("z1", "Z1 spend behaviour", GraphNodeRole.Latent)
                .Node("z2", "Z2 device behaviour", GraphNodeRole.Latent).Edge("y", "z1").Edge("y", "z2");

            foreach (var f in new[] { "amount_z", "velocity_1h", "merchant_risk" })
            {
                b.Node(f, f).Edge("z1", f);
            }

            foreach (var f in new[] { "device_age", "geo_mismatch", "ip_risk" })
            {
                b.Node(f, f).Edge("z2", f);
            }

            return b;
        }

        public static GraphDiagramBuilder CyclicGraph()
        {
            return Diagram.Graph().Title("Feedback structure")
                .Subtitle("The cycle is broken by reversing one edge for layout")
                .Node("a", "Alert").Node("b", "Review").Node("c", "Decision").Node("d", "Rule change")
                .Edge("a", "b", 0.9).Edge("b", "c", 0.6).Edge("c", "d", 0.4)
                .Edge("d", "a", -0.3).Edge("a", "c", 0.2).WeightName("Influence");
        }

        public static GraphDiagramBuilder Hmm()
        {
            return Diagram.Graph().Title("Account behaviour states")
                .Subtitle("Hidden Markov model transition probabilities")
                .Node("n", "Normal", GraphNodeRole.State, "Typical spending")
                .Node("s", "Suspicious", GraphNodeRole.State, "Unusual velocity")
                .Node("c", "Compromised", GraphNodeRole.State, "Takeover suspected")
                .Edge("n", "n", probability: 0.92).Edge("n", "s", probability: 0.07).Edge("n", "c", probability: 0.01)
                .Edge("s", "n", probability: 0.35).Edge("s", "s", probability: 0.45).Edge("s", "c", probability: 0.20)
                .Edge("c", "s", probability: 0.10).Edge("c", "c", probability: 0.90);
        }

        public static NetworkDiagramBuilder Xor()
        {
            return Diagram.Network().Title("XOR network").Subtitle("2-2-1, tanh hidden layer")
                .Input(["x1", "x2"])
                .Hidden(["h1", "h2"], Rows([5.1, -5.2], [-4.8, 4.9]), "tanh", [-2.6, -2.4])
                .Output(["xor"], Rows([6.3, 6.1]), "sigmoid", [-3.0]);
        }

        public static NetworkDiagramBuilder Wide()
        {
            var random = new Random(7);

            double[][] Matrix(int rows, int cols)
            {
                return
                [
                    .. Enumerable.Range(0, rows)
                        .Select(_ =>
                            Enumerable.Range(0, cols).Select(_ => Math.Round(random.NextDouble() * 4 - 2, 2)).ToArray())
                ];
            }

            return Diagram.Network().Title("Fraud scorer").Subtitle("40 inputs, two hidden layers, 2 classes")
                .Input(Enumerable.Range(1, 40).Select(i => "feature_" + i))
                .Hidden(Enumerable.Range(1, 60).Select(i => "h" + i), Matrix(60, 40), "relu")
                .Hidden(Enumerable.Range(1, 12).Select(i => "g" + i), Matrix(12, 60), "relu")
                .Output(["legitimate", "fraud"], Matrix(2, 12), "softmax");
        }
    }
}