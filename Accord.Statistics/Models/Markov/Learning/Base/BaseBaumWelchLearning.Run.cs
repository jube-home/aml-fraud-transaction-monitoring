using System;
using System.Linq;
using Accord.Diagnostics;
using Accord.Math;

#pragma warning disable 612, 618

namespace Accord.Statistics.Models.Markov.Learning
{
    public abstract partial class BaseBaumWelchLearning
    {
        private sealed class BaumWelchRun
        {
            private readonly double[][] logA;
            private readonly double logN;
            private readonly double[] logP;
            private readonly Array[] observations;
            private readonly BaseBaumWelchLearning owner;

            private readonly int states;
            private double[,] lnBwd;

            private double[,] lnFwd;
            private double newLogLikelihood;

            public BaumWelchRun(BaseBaumWelchLearning owner, Array[] observations)
            {
                this.owner = owner;
                this.observations = observations;

                // Grab model information
                states = owner.model.States;
                logA = owner.model.LogTransitions;
                logP = owner.model.LogInitial;


                // Initialize the algorithm
                var N = observations.Length;
                logN = System.Math.Log(N);
                owner.LogKsi = new double[N][][,];
                owner.LogGamma = new double[N][,];


                for (var i = 0; i < observations.Length; i++)
                {
                    var T = observations[i].Length;

                    owner.LogKsi[i] = new double[T][,];
                    owner.LogGamma[i] = new double[T, states];

                    for (var t = 0; t < owner.LogKsi[i].Length; t++)
                    {
                        owner.LogKsi[i][t] = new double[states, states];
                    }
                }
            }

            public double Execute()
            {
                var stop = false;

                var TMax = observations.Max(x => x.Length);
                lnFwd = new double[TMax, states];
                lnBwd = new double[TMax, states];

                // Initialize the model log-likelihoods
                newLogLikelihood = double.NegativeInfinity;
                owner.convergence.NewValue = double.NegativeInfinity;


                do // Until convergence or max iterations is reached
                {
                    expectation();


                    // Average the likelihood for all sequences
                    newLogLikelihood /= observations.Length;
                    owner.convergence.NewValue = newLogLikelihood;

                    // Check for convergence
                    if (!owner.convergence.HasConverged)
                    {
                        // We haven't converged yet

                        // 3. Continue with parameter re-estimation
                        newLogLikelihood = double.NegativeInfinity;

                        reestimateInitial();

                        reestimateTransitions();

                        // 3.3 Re-estimation of emission probabilities
                        owner.UpdateEmissions(); // discrete and continuous
                    }
                    else
                    {
                        stop = true; // The model has converged.
                    }
                } while (!stop);


                // Returns the model average log-likelihood
                return newLogLikelihood;
            }

            private void expectation()
            {
                // For each sequence in the observations input
                for (var i = 0; i < observations.Length; i++)
                {
                    var T = observations[i].Length;
                    var logGamma = owner.LogGamma[i];
                    var w = owner.LogWeights[i];


                    // 1st step - Calculating the forward probability and the
                    //            backward probability for each HMM state.
                    owner.ComputeForwardBackward(i, lnFwd, lnBwd);


                    // 2nd step - Determining the frequency of the transition-emission pair values
                    //            and dividing it by the probability of the entire string.

                    // Calculate gamma values for next computations
                    for (var t = 0; t < T; t++)
                    {
                        var lnsum = double.NegativeInfinity;
                        for (var k = 0; k < states; k++)
                        {
                            logGamma[t, k] = lnFwd[t, k] + lnBwd[t, k] + w;
                            lnsum = Special.LogSum(lnsum, logGamma[t, k]);
                        }

                        Debug.Assert(!double.IsNaN(lnsum));

                        // Normalize if different from zero
                        if (lnsum != double.NegativeInfinity)
                        {
                            for (var k = 0; k < states; k++)
                            {
                                logGamma[t, k] = logGamma[t, k] - lnsum;
                            }
                        }
                    }

                    // Calculate ksi values for next computations
                    owner.ComputeKsi(i, lnFwd, lnBwd);

                    // Compute log-likelihood for the given sequence
                    for (var j = 0; j < states; j++)
                    {
                        newLogLikelihood = Special.LogSum(newLogLikelihood, lnFwd[T - 1, j]);
                    }
                }
            }

            private void reestimateInitial()
            {
                // 3.1 Re-estimation of initial state probabilities
                for (var i = 0; i < logP.Length; i++)
                {
                    var lnsum = double.NegativeInfinity;
                    for (var k = 0; k < owner.LogGamma.Length; k++)
                    {
                        lnsum = Special.LogSum(lnsum, owner.LogGamma[k][0, i]);
                    }

                    logP[i] = lnsum - logN;
                }
            }

            private void reestimateTransitions()
            {
                // 3.2 Re-estimation of transition probabilities 
                for (var i = 0; i < states; i++)
                {
                    for (var j = 0; j < states; j++)
                    {
                        var lnnum = double.NegativeInfinity;
                        var lnden = double.NegativeInfinity;

                        for (var k = 0; k < owner.LogGamma.Length; k++)
                        {
                            var T = observations[k].Length;

                            for (var t = 0; t < T - 1; t++)
                            {
                                lnnum = Special.LogSum(lnnum, owner.LogKsi[k][t][i, j]);
                                lnden = Special.LogSum(lnden, owner.LogGamma[k][t, i]);
                            }
                        }

                        logA[i][j] = lnnum == lnden ? 0 : lnnum - lnden;

                        Debug.Assert(!double.IsNaN(logA[i][j]));
                    }
                }
            }
        }
    }
}