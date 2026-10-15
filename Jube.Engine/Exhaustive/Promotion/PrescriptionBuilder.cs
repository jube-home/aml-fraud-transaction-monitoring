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
using Accord.Math;
using Accord.Neuro;
using Accord.Statistics;
using Accord.Statistics.Visualizations;
using Jube.Data.Poco;
using Jube.Engine.Exhaustive.Algorithms.Models;

namespace Jube.Engine.Exhaustive.Promotion
{
    internal static class PrescriptionBuilder
    {
        internal static List<ExhaustiveSearchInstancePromotedTrialInstanceVariable> BuildPrescriptions(
            int simulationsCount, Dictionary<int, TrialVariable> trialVariables, ActivationNetwork topologyNetwork)
        {
            return
            [
                .. BuildPrescriptionsWithHistograms(simulationsCount, trialVariables, topologyNetwork)
                    .Select(p => p.Prescription)
            ];
        }

        internal static List<(ExhaustiveSearchInstancePromotedTrialInstanceVariable Prescription,
                List<ExhaustiveSearchInstancePrescriptionHistogram> Bins)>
            BuildPrescriptionsWithHistograms(
                int simulationsCount, Dictionary<int, TrialVariable> trialVariables, ActivationNetwork topologyNetwork)
        {
            return BuildPrescriptionsWithHistograms(simulationsCount, trialVariables,
                x => topologyNetwork.Compute(x)[0]);
        }

        internal static List<(ExhaustiveSearchInstancePromotedTrialInstanceVariable Prescription,
                List<ExhaustiveSearchInstancePrescriptionHistogram> Bins)>
            BuildPrescriptionsWithHistograms(
                int simulationsCount, Dictionary<int, TrialVariable> trialVariables, Func<double[], double> score)
        {
            var prescriptions =
                new List<(ExhaustiveSearchInstancePromotedTrialInstanceVariable,
                    List<ExhaustiveSearchInstancePrescriptionHistogram>)>();

            var positiveActivations = new List<double[]>();
            var negativeActivations = new List<double[]>();

            for (var j = 0; j < simulationsCount; j++)
            {
                var simulationZScore = new double[trialVariables.Count];

                for (var k = 0; k < trialVariables.Count; k++)
                {
                    try
                    {
                        simulationZScore[k] = trialVariables.ElementAt(k).Value.TriangularDistribution.Generate();

                        if (trialVariables.ElementAt(k).Value.NormalisationTypeId == 1)
                        {
                            simulationZScore[k] = simulationZScore[k] > 0.5 ? 1 : 0;
                        }
                    }
                    catch
                    {
                        simulationZScore[k] = 0;
                    }
                }

                if (score(simulationZScore) > 0.5)
                {
                    positiveActivations.Add(simulationZScore);
                }
                else
                {
                    negativeActivations.Add(simulationZScore);
                }
            }

            AppendClassPrescriptions(prescriptions, positiveActivations, trialVariables, true);
            AppendClassPrescriptions(prescriptions, negativeActivations, trialVariables, false);

            return prescriptions;
        }

        private static void AppendClassPrescriptions(
            List<(ExhaustiveSearchInstancePromotedTrialInstanceVariable,
                List<ExhaustiveSearchInstancePrescriptionHistogram>)> prescriptions,
            List<double[]> activations, Dictionary<int, TrialVariable> trialVariables, bool positive)
        {
            if (activations.Count <= 0)
            {
                return;
            }

            var activationsArray = activations.ToArray();

            for (var j = 0; j < trialVariables.Count; j++)
            {
                var prescription = new ExhaustiveSearchInstancePromotedTrialInstanceVariable
                {
                    ExhaustiveSearchInstanceTrialInstanceVariableId =
                        trialVariables.ElementAt(j).Value.ExhaustiveSearchInstanceTrialInstanceVariableId,
                    Positive = positive
                };

                var outputs = activationsArray.GetColumn(j);

                for (var i = 0; i < outputs.Length; i++)
                {
                    outputs[i] = trialVariables.ElementAt(j).Value.ReverseZScore(outputs[i]);
                }

                prescription.Mean = outputs.Mean();
                prescription.Maximum = outputs.Max();
                prescription.Minimum = outputs.Min();
                prescription.StandardDeviation = outputs.StandardDeviation();
                prescription.Iqr = InterquartileRange(outputs);

                var bins = new List<ExhaustiveSearchInstancePrescriptionHistogram>();

                if (prescription.Maximum - prescription.Minimum != 0)
                {
                    var histogram = new Histogram();
                    histogram.Compute(outputs);

                    var binIndex = 0;

                    foreach (var histogramBin in histogram.Bins)
                    {
                        bins.Add(new ExhaustiveSearchInstancePrescriptionHistogram
                        {
                            Frequency = histogramBin.Value,
                            BinIndex = binIndex,
                            BinRangeStart = histogramBin.Range.Min,
                            BinRangeEnd = histogramBin.Range.Max
                        });

                        binIndex += 1;
                    }
                }

                prescriptions.Add((prescription, bins));
            }
        }

        internal static double InterquartileRange(double[] values)
        {
            values.Quartiles(out var q1, out var q3);

            return q3 - q1;
        }
    }
}