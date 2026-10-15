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

using Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival
{
    internal static class ContingencyMath
    {
        public static ContingencyAssociationResult Analyse(double[][] table, string[] rowLabels, string[] columnLabels)
        {
            var rows = table.Length;
            var columns = table[0].Length;
            var rowTotals = new double[rows];
            var columnTotals = new double[columns];
            double total = 0;

            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < columns; j++)
                {
                    rowTotals[i] += table[i][j];
                    columnTotals[j] += table[i][j];
                    total += table[i][j];
                }
            }

            if (total <= 0d)
            {
                throw new ArgumentException("The table has no observations: every count is zero.", nameof(table));
            }

            for (var i = 0; i < rows; i++)
            {
                if (rowTotals[i] <= 0d)
                {
                    throw new ArgumentException(
                        $"Row {i} ('{rowLabels[i]}') has no observations, so its expected counts are zero. Remove that row and call again.",
                        nameof(table));
                }
            }

            for (var j = 0; j < columns; j++)
            {
                if (columnTotals[j] <= 0d)
                {
                    throw new ArgumentException(
                        $"Column {j} ('{columnLabels[j]}') has no observations, so its expected counts are zero. Remove that column and call again.",
                        nameof(table));
                }
            }

            var expected = new double[rows][];
            var pearson = new double[rows][];
            var adjusted = new double[rows][];
            var contribution = new double[rows][];
            double chi = 0;
            double g2 = 0;
            double yates = 0;
            var smallest = double.MaxValue;
            var below = 0;

            for (var i = 0; i < rows; i++)
            {
                expected[i] = new double[columns];
                pearson[i] = new double[columns];
                adjusted[i] = new double[columns];
                contribution[i] = new double[columns];

                for (var j = 0; j < columns; j++)
                {
                    var e = rowTotals[i] * columnTotals[j] / total;
                    var o = table[i][j];
                    expected[i][j] = e;
                    pearson[i][j] = (o - e) / Math.Sqrt(e);
                    var variance = e * (1d - rowTotals[i] / total) * (1d - columnTotals[j] / total);
                    adjusted[i][j] = variance > 0d ? (o - e) / Math.Sqrt(variance) : 0d;
                    contribution[i][j] = (o - e) * (o - e) / e;
                    chi += contribution[i][j];

                    if (o > 0d)
                    {
                        g2 += 2d * o * Math.Log(o / e);
                    }

                    var corrected = Math.Max(0d, Math.Abs(o - e) - 0.5);
                    yates += corrected * corrected / e;
                    smallest = Math.Min(smallest, e);

                    if (e < 5d)
                    {
                        below++;
                    }
                }
            }

            var df = (rows - 1) * (columns - 1);
            var minimumDimension = Math.Min(rows - 1, columns - 1);
            var phi2 = chi / total;
            double vBias;

            if (total > 1d)
            {
                var phi2Corrected = Math.Max(0d, phi2 - (rows - 1d) * (columns - 1d) / (total - 1d));
                var rowsCorrected = rows - (rows - 1d) * (rows - 1d) / (total - 1d);
                var columnsCorrected = columns - (columns - 1d) * (columns - 1d) / (total - 1d);
                var denominator = Math.Min(rowsCorrected - 1d, columnsCorrected - 1d);
                vBias = denominator > 0d ? Math.Sqrt(phi2Corrected / denominator) : double.NaN;
            }
            else
            {
                vBias = double.NaN;
            }

            var v = Math.Sqrt(phi2 / minimumDimension);
            var signedPhi = double.NaN;
            var yatesPValue = double.NaN;
            var yatesStatistic = double.NaN;

            if (rows == 2 && columns == 2)
            {
                var a = table[0][0];
                var b = table[0][1];
                var c = table[1][0];
                var d = table[1][1];

                signedPhi = (a * d - b * c) /
                            Math.Sqrt(rowTotals[0] * rowTotals[1] * columnTotals[0] * columnTotals[1]);

                yatesStatistic = yates;
                yatesPValue = CategoricalLibrary.ChiSquareSurvival(yates, 1);
            }

            var cells = rows * columns;
            var fraction = below / (double)cells;
            var warnings = new List<string>();

            if (fraction > 0.2 || smallest < 1d)
            {
                warnings.Add(
                    "More than 20 percent of cells have an expected count below 5 or one is below 1, so the chi-square approximation is unreliable; merge sparse categories or use an exact test.");
            }

            if (total < 20d)
            {
                warnings.Add("Fewer than 20 observations in total; treat every p-value as indicative only.");
            }

            return new ContingencyAssociationResult
            {
                RowLabels = rowLabels,
                ColumnLabels = columnLabels,
                Total = total,
                RowTotals = rowTotals,
                ColumnTotals = columnTotals,
                Expected = expected,
                PearsonResiduals = pearson,
                AdjustedResiduals = adjusted,
                CellChiSquare = contribution,
                ChiSquare = chi,
                DegreesOfFreedom = df,
                PValue = CategoricalLibrary.ChiSquareSurvival(chi, df),
                LikelihoodRatioChiSquare = g2,
                LikelihoodRatioPValue = CategoricalLibrary.ChiSquareSurvival(g2, df),
                YatesChiSquare = yatesStatistic,
                YatesPValue = yatesPValue,
                CramersV = v,
                CramersVBiasCorrected = vBias,
                Phi = Math.Sqrt(phi2),
                SignedPhi = signedPhi,
                ContingencyCoefficient = Math.Sqrt(chi / (chi + total)),
                TschuprowT = Math.Sqrt(phi2 / Math.Sqrt((rows - 1d) * (columns - 1d))),
                EffectSizeLabel = EffectSize(v, minimumDimension),
                SmallestExpectedCount = smallest,
                FractionOfCellsExpectedBelowFive = fraction,
                Warnings = warnings.ToArray()
            };
        }

        private static string EffectSize(double v, int minimumDimension)
        {
            double small;
            double medium;
            double large;

            if (minimumDimension <= 1)
            {
                small = 0.1;
                medium = 0.3;
                large = 0.5;
            }
            else if (minimumDimension == 2)
            {
                small = 0.07;
                medium = 0.21;
                large = 0.35;
            }
            else
            {
                small = 0.06;
                medium = 0.17;
                large = 0.29;
            }

            if (v < small)
            {
                return "negligible";
            }

            if (v < medium)
            {
                return "small";
            }

            return v < large ? "medium" : "large";
        }

        public static GoodmanKruskalLambdaResult Lambda(double[][] table)
        {
            var rows = table.Length;
            var columns = table[0].Length;
            var rowTotals = new double[rows];
            var columnTotals = new double[columns];
            double n = 0;

            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < columns; j++)
                {
                    rowTotals[i] += table[i][j];
                    columnTotals[j] += table[i][j];
                    n += table[i][j];
                }
            }

            if (n <= 0d)
            {
                throw new ArgumentException("The table has no observations: every count is zero.", nameof(table));
            }

            var rowMaxima = new double[rows];
            var rowMaxColumn = new int[rows];

            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < columns; j++)
                {
                    if (j == 0 || table[i][j] > rowMaxima[i])
                    {
                        rowMaxima[i] = table[i][j];
                        rowMaxColumn[i] = j;
                    }
                }
            }

            var columnMaxima = new double[columns];
            var columnMaxRow = new int[columns];

            for (var j = 0; j < columns; j++)
            {
                for (var i = 0; i < rows; i++)
                {
                    if (i == 0 || table[i][j] > columnMaxima[j])
                    {
                        columnMaxima[j] = table[i][j];
                        columnMaxRow[j] = i;
                    }
                }
            }

            var maxColumnTotal = 0d;
            var maxColumnIndex = 0;

            for (var j = 0; j < columns; j++)
            {
                if (columnTotals[j] > maxColumnTotal)
                {
                    maxColumnTotal = columnTotals[j];
                    maxColumnIndex = j;
                }
            }

            var maxRowTotal = 0d;
            var maxRowIndex = 0;

            for (var i = 0; i < rows; i++)
            {
                if (rowTotals[i] > maxRowTotal)
                {
                    maxRowTotal = rowTotals[i];
                    maxRowIndex = i;
                }
            }

            var sumRowMax = 0d;
            var sumRowMaxInModal = 0d;

            for (var i = 0; i < rows; i++)
            {
                sumRowMax += rowMaxima[i];

                if (rowMaxColumn[i] == maxColumnIndex)
                {
                    sumRowMaxInModal += rowMaxima[i];
                }
            }

            var sumColumnMax = 0d;
            var sumColumnMaxInModal = 0d;

            for (var j = 0; j < columns; j++)
            {
                sumColumnMax += columnMaxima[j];

                if (columnMaxRow[j] == maxRowIndex)
                {
                    sumColumnMaxInModal += columnMaxima[j];
                }
            }

            double columnGivenRow;
            double columnGivenRowSe;
            var denominatorColumn = n - maxColumnTotal;

            if (denominatorColumn <= 0d)
            {
                columnGivenRow = double.NaN;
                columnGivenRowSe = double.NaN;
            }
            else
            {
                columnGivenRow = (sumRowMax - maxColumnTotal) / denominatorColumn;

                columnGivenRowSe = Math.Sqrt(Math.Max(0d,
                    (n - sumRowMax) * (sumRowMax + maxColumnTotal - 2d * sumRowMaxInModal) /
                    Math.Pow(denominatorColumn, 3)));
            }

            double rowGivenColumn;
            double rowGivenColumnSe;
            var denominatorRow = n - maxRowTotal;

            if (denominatorRow <= 0d)
            {
                rowGivenColumn = double.NaN;
                rowGivenColumnSe = double.NaN;
            }
            else
            {
                rowGivenColumn = (sumColumnMax - maxRowTotal) / denominatorRow;

                rowGivenColumnSe = Math.Sqrt(Math.Max(0d,
                    (n - sumColumnMax) * (sumColumnMax + maxRowTotal - 2d * sumColumnMaxInModal) /
                    Math.Pow(denominatorRow, 3)));
            }

            var symmetricDenominator = 2d * n - maxRowTotal - maxColumnTotal;

            var symmetric = symmetricDenominator <= 0d
                ? double.NaN
                : (sumRowMax + sumColumnMax - maxRowTotal - maxColumnTotal) / symmetricDenominator;

            return new GoodmanKruskalLambdaResult
            {
                LambdaColumnGivenRow = columnGivenRow,
                StandardErrorColumnGivenRow = columnGivenRowSe,
                LambdaRowGivenColumn = rowGivenColumn,
                StandardErrorRowGivenColumn = rowGivenColumnSe,
                LambdaSymmetric = symmetric,
                Total = n
            };
        }

        public static OrdinalAssociationResult Ordinal(double[][] table, double level)
        {
            var rows = table.Length;
            var columns = table[0].Length;
            double n = 0;
            var rowTotals = new double[rows];
            var columnTotals = new double[columns];

            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < columns; j++)
                {
                    n += table[i][j];
                    rowTotals[i] += table[i][j];
                    columnTotals[j] += table[i][j];
                }
            }

            if (n < 2d)
            {
                throw new ArgumentException("At least two observations are needed to form pairs.", nameof(table));
            }

            var concordantCells = new double[rows, columns];
            var discordantCells = new double[rows, columns];
            double doubleConcordant = 0;
            double doubleDiscordant = 0;

            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < columns; j++)
                {
                    double a = 0;
                    double d = 0;

                    for (var k = 0; k < rows; k++)
                    {
                        for (var l = 0; l < columns; l++)
                        {
                            if ((k < i && l < j) || (k > i && l > j))
                            {
                                a += table[k][l];
                            }
                            else if ((k < i && l > j) || (k > i && l < j))
                            {
                                d += table[k][l];
                            }
                        }
                    }

                    concordantCells[i, j] = a;
                    discordantCells[i, j] = d;
                    doubleConcordant += table[i][j] * a;
                    doubleDiscordant += table[i][j] * d;
                }
            }

            var p = doubleConcordant / 2d;
            var q = doubleDiscordant / 2d;

            double tiedRow = 0;
            double tiedColumn = 0;

            for (var i = 0; i < rows; i++)
            {
                tiedRow += (rowTotals[i] * rowTotals[i] - SumOfSquares(table[i])) / 2d;
            }

            for (var j = 0; j < columns; j++)
            {
                double squares = 0;

                for (var i = 0; i < rows; i++)
                {
                    squares += table[i][j] * table[i][j];
                }

                tiedColumn += (columnTotals[j] * columnTotals[j] - squares) / 2d;
            }

            var gamma = p + q > 0d ? (p - q) / (p + q) : double.NaN;
            var gammaSe = double.NaN;

            if (p + q > 0d)
            {
                double sum = 0;

                for (var i = 0; i < rows; i++)
                {
                    for (var j = 0; j < columns; j++)
                    {
                        var term = doubleDiscordant * concordantCells[i, j] - doubleConcordant * discordantCells[i, j];
                        sum += table[i][j] * term * term;
                    }
                }

                gammaSe = 4d * Math.Sqrt(sum) / Math.Pow(doubleConcordant + doubleDiscordant, 2d);
            }

            var z = CategoricalGuards.TwoSidedZ(level);
            var tauBDenominator = Math.Sqrt((p + q + tiedRow) * (p + q + tiedColumn));
            var minimumDimension = Math.Min(rows, columns);

            return new OrdinalAssociationResult
            {
                ConcordantPairs = p,
                DiscordantPairs = q,
                PairsTiedOnRowOnly = tiedRow,
                PairsTiedOnColumnOnly = tiedColumn,
                Gamma = gamma,
                GammaStandardError = gammaSe,
                GammaLower = Math.Max(-1d, gamma - z * gammaSe),
                GammaUpper = Math.Min(1d, gamma + z * gammaSe),
                KendallTauB = tauBDenominator > 0d ? (p - q) / tauBDenominator : double.NaN,
                StuartTauC = 2d * minimumDimension * (p - q) / (n * n * (minimumDimension - 1d)),
                SomersDColumnDependent = p + q + tiedColumn > 0d ? (p - q) / (p + q + tiedColumn) : double.NaN,
                SomersDRowDependent = p + q + tiedRow > 0d ? (p - q) / (p + q + tiedRow) : double.NaN,
                SomersDSymmetric = p + q + (tiedRow + tiedColumn) / 2d > 0d
                    ? (p - q) / (p + q + (tiedRow + tiedColumn) / 2d)
                    : double.NaN
            };
        }

        private static double SumOfSquares(double[] values)
        {
            double sum = 0;

            foreach (var v in values)
            {
                sum += v * v;
            }

            return sum;
        }

        public static InformationTheoryAssociationResult InformationTheory(double[][] table, bool useBits)
        {
            var rows = table.Length;
            var columns = table[0].Length;
            double n = 0;
            var rowTotals = new double[rows];
            var columnTotals = new double[columns];

            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < columns; j++)
                {
                    n += table[i][j];
                    rowTotals[i] += table[i][j];
                    columnTotals[j] += table[i][j];
                }
            }

            if (n <= 0d)
            {
                throw new ArgumentException("The table has no observations: every count is zero.", nameof(table));
            }

            var scale = useBits ? 1d / Math.Log(2d) : 1d;
            double hRow = 0;
            double hColumn = 0;
            double hJoint = 0;

            foreach (var r in rowTotals)
            {
                if (r > 0d)
                {
                    hRow -= r / n * Math.Log(r / n);
                }
            }

            foreach (var c in columnTotals)
            {
                if (c > 0d)
                {
                    hColumn -= c / n * Math.Log(c / n);
                }
            }

            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < columns; j++)
                {
                    if (table[i][j] > 0d)
                    {
                        hJoint -= table[i][j] / n * Math.Log(table[i][j] / n);
                    }
                }
            }

            hRow *= scale;
            hColumn *= scale;
            hJoint *= scale;
            var mi = Math.Max(0d, hRow + hColumn - hJoint);

            return new InformationTheoryAssociationResult
            {
                RowEntropy = hRow,
                ColumnEntropy = hColumn,
                JointEntropy = hJoint,
                MutualInformation = mi,
                NormalisedMutualInformation = hRow > 0d && hColumn > 0d ? mi / Math.Sqrt(hRow * hColumn) : double.NaN,
                UncertaintyCoefficientRowGivenColumn = hRow > 0d ? mi / hRow : double.NaN,
                UncertaintyCoefficientColumnGivenRow = hColumn > 0d ? mi / hColumn : double.NaN,
                UncertaintyCoefficientSymmetric = hRow + hColumn > 0d ? 2d * mi / (hRow + hColumn) : double.NaN,
                Unit = useBits ? "bits" : "nats"
            };
        }
    }
}