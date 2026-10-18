// Accord Machine Learning Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © Jube Holdings Limited, 2026
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

using System;

namespace Accord.MachineLearning.LatentTrees
{
    [Serializable]
    internal sealed class LatentTreeShape
    {
        public readonly int[][] Children;
        public readonly int Count;
        public readonly int MaxChildren;
        public readonly int MaxStates;
        public readonly int[] Parents;
        public readonly int[] States;

        public LatentTreeShape(int[] parents, int[] states)
        {
            Count = parents.Length;
            Parents = parents;
            States = states;
            var counts = new int[Count];
            for (var i = 1; i < Count; i++)
            {
                counts[parents[i]]++;
            }

            Children = new int[Count][];
            for (var i = 0; i < Count; i++)
            {
                Children[i] = new int[counts[i]];
                if (counts[i] > MaxChildren)
                {
                    MaxChildren = counts[i];
                }

                if (states[i] > MaxStates)
                {
                    MaxStates = states[i];
                }

                counts[i] = 0;
            }

            for (var i = 1; i < Count; i++)
            {
                var p = parents[i];
                Children[p][counts[p]++] = i;
            }
        }
    }

    internal sealed class LatentTreeWorkspace
    {
        public readonly double[][] Lambda;
        public readonly double[][] Message;
        public readonly int[] Observed;
        public readonly double[] Pair;
        public readonly double[][] Pi;
        public readonly double[][] Prefix;
        public readonly double[][] Suffix;

        public LatentTreeWorkspace(LatentTreeShape shape)
        {
            var n = shape.Count;
            Lambda = new double[n][];
            Message = new double[n][];
            Pi = new double[n][];
            Observed = new int[n];
            for (var i = 0; i < n; i++)
            {
                Lambda[i] = new double[shape.States[i]];
                Pi[i] = new double[shape.States[i]];
                if (i > 0)
                {
                    Message[i] = new double[shape.States[shape.Parents[i]]];
                }
            }

            Prefix = new double[shape.MaxStates][];
            Suffix = new double[shape.MaxStates][];
            for (var s = 0; s < shape.MaxStates; s++)
            {
                Prefix[s] = new double[shape.MaxChildren + 1];
                Suffix[s] = new double[shape.MaxChildren + 1];
            }

            Pair = new double[shape.MaxStates * shape.MaxStates];
        }
    }

    internal static class LatentTreeInference
    {
        public static double Upward(LatentTreeShape shape, double[][][] cpts, LatentTreeWorkspace ws)
        {
            var n = shape.Count;
            var observed = ws.Observed;
            for (var v = 0; v < n; v++)
            {
                var lam = ws.Lambda[v];
                var o = observed[v];
                if (o >= 0)
                {
                    for (var t = 0; t < lam.Length; t++)
                    {
                        lam[t] = 0;
                    }

                    lam[o] = 1;
                }
                else
                {
                    for (var t = 0; t < lam.Length; t++)
                    {
                        lam[t] = 1;
                    }
                }
            }

            double logScale = 0;
            for (var v = n - 1; v >= 1; v--)
            {
                var p = shape.Parents[v];
                var lam = ws.Lambda[v];
                var cpt = cpts[v];
                var msg = ws.Message[v];
                double max = 0;
                for (var s = 0; s < msg.Length; s++)
                {
                    var row = cpt[s];
                    double sum = 0;
                    for (var t = 0; t < lam.Length; t++)
                    {
                        sum += row[t] * lam[t];
                    }

                    msg[s] = sum;
                    if (sum > max)
                    {
                        max = sum;
                    }
                }

                if (!(max > 0))
                {
                    return double.NegativeInfinity;
                }

                var inv = 1.0 / max;
                var plam = ws.Lambda[p];
                for (var s = 0; s < msg.Length; s++)
                {
                    msg[s] *= inv;
                    plam[s] *= msg[s];
                }

                logScale += System.Math.Log(max);
            }

            return logScale;
        }

        public static void Accumulate(LatentTreeShape shape, double[][][] cpts, LatentTreeWorkspace ws,
            int classState, double weight, double[][][] counts)
        {
            var n = shape.Count;
            var pi0 = ws.Pi[0];
            for (var s = 0; s < pi0.Length; s++)
            {
                pi0[s] = 0;
            }

            pi0[classState] = 1;

            for (var v = 0; v < n; v++)
            {
                var ch = shape.Children[v];
                var m = ch.Length;
                if (m == 0)
                {
                    continue;
                }

                var sv = shape.States[v];
                var piV = ws.Pi[v];

                for (var s = 0; s < sv; s++)
                {
                    var pre = ws.Prefix[s];
                    var suf = ws.Suffix[s];
                    pre[0] = 1;
                    for (var j = 0; j < m; j++)
                    {
                        pre[j + 1] = pre[j] * ws.Message[ch[j]][s];
                    }

                    suf[m] = 1;
                    for (var j = m - 1; j >= 0; j--)
                    {
                        suf[j] = suf[j + 1] * ws.Message[ch[j]][s];
                    }
                }

                for (var j = 0; j < m; j++)
                {
                    var u = ch[j];
                    var su = shape.States[u];
                    var cpt = cpts[u];
                    var lam = ws.Lambda[u];
                    var piU = ws.Pi[u];
                    var pair = ws.Pair;
                    for (var t = 0; t < su; t++)
                    {
                        piU[t] = 0;
                    }

                    double z = 0;
                    for (var s = 0; s < sv; s++)
                    {
                        var q = piV[s] * ws.Prefix[s][j] * ws.Suffix[s][j + 1];
                        var row = cpt[s];
                        for (var t = 0; t < su; t++)
                        {
                            var a = q * row[t];
                            piU[t] += a;
                            var b = a * lam[t];
                            pair[s * su + t] = b;
                            z += b;
                        }
                    }

                    if (z > 0)
                    {
                        var f = weight / z;
                        var cu = counts[u];
                        for (var s = 0; s < sv; s++)
                        {
                            var crow = cu[s];
                            for (var t = 0; t < su; t++)
                            {
                                crow[t] += pair[s * su + t] * f;
                            }
                        }
                    }

                    double max = 0;
                    for (var t = 0; t < su; t++)
                    {
                        if (piU[t] > max)
                        {
                            max = piU[t];
                        }
                    }

                    if (max > 0)
                    {
                        var inv = 1.0 / max;
                        for (var t = 0; t < su; t++)
                        {
                            piU[t] *= inv;
                        }
                    }
                }
            }
        }
    }
}