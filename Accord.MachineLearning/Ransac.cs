// Accord Machine Learning Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
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
// This work has been inspired by the original work of Peter Kovesi,
// shared under a permissive MIT license. Details are given below:
//
//   Copyright (c) 1995-2010 Peter Kovesi
//   Centre for Exploration Targeting
//   School of Earth and Environment
//   The University of Western Australia
//
//   Permission is hereby granted, free of charge, to any person obtaining a copy
//   of this software and associated documentation files (the "Software"), to deal
//   in the Software without restriction, including without limitation the rights 
//   to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
//   of the Software, and to permit persons to whom the Software is furnished to do
//   so, subject to the following conditions:
//   
//   The above copyright notice and this permission notice shall be included in all
//   copies or substantial portions of the Software.
//   
//   The software is provided "as is", without warranty of any kind, express or
//   implied, including but not limited to the warranties of merchantability, 
//   fitness for a particular purpose and noninfringement. In no event shall the
//   authors or copyright holders be liable for any claim, damages or other liability,
//   whether in an action of contract, tort or otherwise, arising from, out of or in
//   connection with the software or the use or other dealings in the software.
//   

using System;
using Accord.Math;

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Multipurpose RANSAC algorithm.
    /// </summary>
    /// <typeparam name="TModel">The model type to be trained by RANSAC.</typeparam>
    /// <remarks>
    ///     <para>
    ///         RANSAC is an abbreviation for "RANdom SAmple Consensus". It is an iterative
    ///         method to estimate parameters of a mathematical model from a set of observed
    ///         data which contains outliers. It is a non-deterministic algorithm in the sense
    ///         that it produces a reasonable result only with a certain probability, with this
    ///         probability increasing as more iterations are allowed.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     P. D. Kovesi. MATLAB and Octave Functions for Computer Vision and Image Processing.
    ///                     School of Computer Science and Software Engineering, The University of Western Australia.
    ///                     Available in: http://www.csse.uwa.edu.au/~pk/research/matlabfns
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Wikipedia, The Free Encyclopedia. RANSAC. Available on:
    ///                     http://en.wikipedia.org/wiki/RANSAC
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\RansacTest.cs" region="doc_learn" />
    /// </example>
    public class RANSAC<TModel> where TModel : class
    // TODO: Implement ISupervisedLearning interface
    {
        // RANSAC functions

        // RANSAC parameters

        /// <summary>
        ///     Constructs a new RANSAC algorithm.
        /// </summary>
        /// <param name="minSamples">
        ///     The minimum number of samples from the data
        ///     required by the fitting function to fit a model.
        /// </param>
        public RANSAC(int minSamples)
        {
            Samples = minSamples;
        }

        /// <summary>
        ///     Constructs a new RANSAC algorithm.
        /// </summary>
        /// <param name="minSamples">
        ///     The minimum number of samples from the data
        ///     required by the fitting function to fit a model.
        /// </param>
        /// <param name="threshold">
        ///     The minimum distance between a data point and
        ///     the model used to decide whether the point is
        ///     an inlier or not.
        /// </param>
        public RANSAC(int minSamples, double threshold)
        {
            Samples = minSamples;
            Threshold = threshold;
        }

        /// <summary>
        ///     Constructs a new RANSAC algorithm.
        /// </summary>
        /// <param name="minSamples">
        ///     The minimum number of samples from the data
        ///     required by the fitting function to fit a model.
        /// </param>
        /// <param name="threshold">
        ///     The minimum distance between a data point and
        ///     the model used to decide whether the point is
        ///     an inlier or not.
        /// </param>
        /// <param name="probability">
        ///     The probability of obtaining a random sample of
        ///     the input points that contains no outliers.
        /// </param>
        public RANSAC(int minSamples, double threshold, double probability)
        {
            if (minSamples < 0)
            {
                throw new ArgumentOutOfRangeException("minSamples");
            }

            if (threshold < 0)
            {
                throw new ArgumentOutOfRangeException("threshold");
            }

            if (probability > 1.0 || probability < 0.0)
            {
                throw new ArgumentException("Probability should be a value between 0 and 1", "probability");
            }

            Samples = minSamples;
            Threshold = threshold;
            this.Probability = probability;
        }

        /// <summary>
        ///     Model fitting function.
        /// </summary>
        public Func<int[], TModel> Fitting { get; set; }

        /// <summary>
        ///     Degenerative set detection function.
        /// </summary>
        public Func<int[], bool> Degenerate { get; set; }

        /// <summary>
        ///     Distance function.
        /// </summary>
        public Func<TModel, double, int[]> Distances { get; set; }

        /// <summary>
        ///     Gets or sets the minimum distance between a data point and
        ///     the model used to decide whether the point is an inlier or not.
        /// </summary>
        public double Threshold { get; set; }

        /// <summary>
        ///     Gets or sets the minimum number of samples from the data
        ///     required by the fitting function to fit a model.
        /// </summary>
        public int Samples { get; set; }

        /// <summary>
        ///     Maximum number of attempts to select a
        ///     non-degenerate data set. Default is 100.
        /// </summary>
        public int MaxSamplings { get; set; } = 100;

        /// <summary>
        ///     Maximum number of trials. Default is 1000.
        /// </summary>
        public int MaxEvaluations { get; set; } = 1000;

        /// <summary>
        ///     Gets the current estimate of trials needed.
        /// </summary>
        public int TrialsNeeded { get; private set; }

        /// <summary>
        ///     Gets the current number of trials performed.
        /// </summary>
        public int TrialsPerformed { get; private set; }

        /// <summary>
        ///     Gets or sets the probability of obtaining a random
        ///     sample of the input points that contains no outliers.
        ///     Default is 0.99.
        /// </summary>
        public double Probability { get; set; } = 0.99;

        /// <summary>
        ///     Computes the model using the RANSAC algorithm.
        /// </summary>
        /// <param name="size">The total number of points in the data set.</param>
        public TModel Compute(int size)
        {
            return Compute(size, out _);
        }

        /// <summary>
        ///     Computes the model using the RANSAC algorithm.
        /// </summary>
        /// <param name="size">The total number of points in the data set.</param>
        /// <param name="inliers">The indexes of the outlier points in the data set.</param>
        public TModel Compute(int size, out int[] inliers)
        {
            // We are going to find the best model (which fits
            //  the maximum number of inlier points as possible).
            TModel bestModel = null;
            int[] bestInliers = null;
            var maxInliers = 0;

            var r = System.Math.Min(size, Samples);

            // For this we are going to search for random samples
            //  of the original points which contains no outliers.

            TrialsPerformed = 0; // Total number of trials performed
            TrialsNeeded = MaxEvaluations; // Estimative of number of trials needed.

            // While the number of trials is less than our estimative,
            //   and we have not surpassed the maximum number of trials
            while (TrialsPerformed < TrialsNeeded && TrialsPerformed < MaxEvaluations)
            {
                TModel model = null;
                var samplings = 0;

                // While the number of samples attempted is less
                //   than the maximum limit of attempts
                while (samplings < MaxSamplings)
                {
                    // Select at random s data points to form a trial model.
                    var sample = Vector.Sample(r, size);

                    // If the sampled points are not in a degenerate configuration,
                    if (Degenerate == null || !Degenerate(sample))
                    {
                        // Fit model using the random selection of points
                        model = Fitting(sample);
                        break; // Exit the while loop.
                    }

                    samplings++; // Increase the samplings counter
                }

                if (model == null)
                {
                    throw new ConvergenceException("A model could not be inferred from the data points");
                }

                // Now, evaluate the distances between total points and the model returning the
                //  indices of the points that are inliers (according to a distance threshold t).
                inliers = Distances(model, Threshold);

                // Check if the model was the model which highest number of inliers:
                if (bestInliers == null || inliers.Length > maxInliers)
                {
                    // Yes, this model has the highest number of inliers.

                    maxInliers = inliers.Length; // Set the new maximum,
                    bestModel = model; // This is the best model found so far,
                    bestInliers = inliers; // Store the indices of the current inliers.

                    // Update estimate of N, the number of trials to ensure we pick, 
                    //   with probability p, a data set with no outliers.
                    var pInlier = inliers.Length / (double)size;
                    var pNoOutliers = 1.0 - System.Math.Pow(pInlier, Samples);

                    var num = System.Math.Log(1.0 - Probability);
                    var den = System.Math.Log(pNoOutliers);
                    if (den == 0)
                    {
                        TrialsNeeded = num == 0 ? 0 : MaxEvaluations;
                    }
                    else
                    {
                        TrialsNeeded = (int)(num / den);
                    }
                }

                TrialsPerformed++; // Increase the trial counter.
            }

            inliers = bestInliers;
            return bestModel;
        }
    }
}