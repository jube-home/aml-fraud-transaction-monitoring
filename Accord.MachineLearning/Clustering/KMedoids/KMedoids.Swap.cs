// Accord Machine Learning Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © César Souza <cesarsouza at gmail.com>
// and other contributors, 2009-2017.
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Accord.MachineLearning
{
    public partial class KMedoids<T>
    {
        private void assignPointsToClusters(T[][] x, int[] labels, int[] currentMedoidIndicesArray,
            double[] secondClusterDistance)
        {
            // Assing points to clusters
            Parallel.For(0, x.Length, ParallelOptions, pointIndex =>
            {
                //int secondMinCostClusterIndex = -1;
                var secondMinCost = double.PositiveInfinity;
                var minCostClusterIndex = 0;
                var medoidIndex = currentMedoidIndicesArray[minCostClusterIndex];
                var minCost = Distance.Distance(x[pointIndex], x[medoidIndex]);

                for (var i = 1; i < currentMedoidIndicesArray.Length; i++)
                {
                    var medoidPointIndex = currentMedoidIndicesArray[i];
                    var cost = Distance.Distance(x[pointIndex], x[medoidPointIndex]);

                    if (cost < minCost)
                    {
                        secondMinCost = minCost;
                        minCost = cost;
                        //secondMinCostClusterIndex = minCostClusterIndex;
                        minCostClusterIndex = i;
                    }
                    else if (cost < secondMinCost)
                    {
                        secondMinCost = cost;
                        //secondMinCostClusterIndex = medoidPointIndex;
                    }
                }

                labels[pointIndex] = minCostClusterIndex;
                secondClusterDistance[pointIndex] = secondMinCost;
            });
        }

        private void findBestSwap(T[][] x, int[] labels, HashSet<int> currentMedoidIndices,
            int[] currentMedoidIndicesArray, double[] secondClusterDistance,
            out int minI, out int minH, out double minTih)
        {
            var localMinI = -1;
            var localMinH = -1;
            var localMinTih = double.PositiveInfinity;
            var lockObj = new object();

            Parallel.For(0, x.Length, ParallelOptions,
                () => new { i = -1, h = -1, tih = double.PositiveInfinity },
                (h, _, partialMin) =>
                {
                    // Skip current medoids
                    if (currentMedoidIndices.Contains(h))
                    {
                        return partialMin;
                    }

                    var partialMinI = partialMin.i;
                    var partialMinH = partialMin.h;
                    var partialMinTih = partialMin.tih;

                    for (var i = 0; i < currentMedoidIndicesArray.Length; i++)
                    {
                        var pointIIndex = currentMedoidIndicesArray[i];

                        // Compute T[i, h]
                        var tih = computeSwapCost(x, labels, currentMedoidIndices, currentMedoidIndicesArray,
                            secondClusterDistance, h, pointIIndex);

                        if (tih < partialMinTih)
                        {
                            partialMinI = i;
                            partialMinH = h;
                            partialMinTih = tih;
                        }
                    }

                    return new { i = partialMinI, h = partialMinH, tih = partialMinTih };
                },
                partialMin =>
                {
                    if (partialMin.tih < localMinTih)
                    {
                        lock (lockObj)
                        {
                            if (partialMin.tih < localMinTih)
                            {
                                localMinI = partialMin.i;
                                localMinH = partialMin.h;
                                localMinTih = partialMin.tih;
                            }
                        }
                    }
                }
            );

            minI = localMinI;
            minH = localMinH;
            minTih = localMinTih;
        }

        private double computeSwapCost(T[][] x, int[] labels, HashSet<int> currentMedoidIndices,
            int[] currentMedoidIndicesArray, double[] secondClusterDistance, int h, int pointIIndex)
        {
            var tih = 0.0;

            Parallel.For(0, x.Length, ParallelOptions,
                () => 0.0,
                (j, _, partialSum) =>
                {
                    // Skip current medoids and point #I
                    if (j == h || currentMedoidIndices.Contains(j))
                    {
                        return partialSum;
                    }

                    // Compute Kijh
                    var m = currentMedoidIndicesArray[labels[j]];
                    var dj = Distance.Distance(x[j], x[m]);
                    var djh = Distance.Distance(x[j], x[h]);
                    var dji = Distance.Distance(x[j], x[pointIIndex]);
                    var kjih = dji == dj
                        ? djh < secondClusterDistance[j] ? djh - dj : secondClusterDistance[j] - dj
                        : djh < dj
                            ? djh - dj
                            : 0.0;
                    return partialSum + kjih;
                },
                partialSum => { InterlockedEx.Add(ref tih, partialSum); }
            );

            return tih;
        }
    }
}