// Accord.NET Framework (originally)
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
// Relicensed to MIT per the author's own explicit grant made when the
// upstream project was archived (2020-11-19):
// https://github.com/accord-net/framework
// "I am also willing to change the license of any file where I am the
// single author... to MIT so people can reuse individual pieces of code
// more easily." -- Cesar De Souza
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using Accord.Math;
using Accord.Statistics.Testing;
using Accord.Statistics.Visualizations;

namespace Accord.Statistics.Analysis
{
    /// <summary>
    ///     Methods for computing the area under
    ///     <see cref="ReceiverOperatingCharacteristic">
    ///         Receiver-Operating Characteristic
    ///     </see>
    ///     (ROC) curves (also known as the ROC AUC).
    /// </summary>
    public enum RocAreaMethod
    {
        /// <summary>
        ///     Method of DeLong, E. R., D. M. DeLong, and D. L. Clarke-Pearson. 1988. Comparing
        ///     the areas under two or more correlated receiver operating characteristic curves:
        ///     a nonparametric approach. Biometrics 44:837–845.
        /// </summary>
        DeLong,

        /// <summary>
        ///     Method of Hanley, J.A. and McNeil, B.J. 1983. A method of comparing the areas under
        ///     receiver operating characteristic curves derived from the same cases. Radiology 148:839-843.
        /// </summary>
        HanleyMcNeil
    }

    /// <summary>
    ///     Receiver Operating Characteristic (ROC) Curve.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In signal detection theory, a receiver operating characteristic (ROC), or simply
    ///         ROC curve, is a graphical plot of the sensitivity vs. (1 − specificity) for a
    ///         binary classifier system as its discrimination threshold is varied.
    ///     </para>
    ///     <para>
    ///         This package does not attempt to fit a curve to the obtained points. It just
    ///         computes the area under the ROC curve directly using the trapezoidal rule.
    ///     </para>
    ///     <para>
    ///         Also note that the curve construction algorithm uses the convention that a
    ///         higher test value represents a positive for a condition while computing
    ///         sensitivity and specificity values.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Wikipedia, The Free Encyclopedia. Receiver Operating Characteristic. Available on:
    ///                     http://en.wikipedia.org/wiki/Receiver_operating_characteristic
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Anaesthesist. The magnificent ROC. Available on:
    ///                     http://www.anaesthetist.com/mnm/stats/roc/Findex.htm
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The following example shows how to measure the accuracy
    ///         of a binary classifier using a ROC curve.
    ///     </para>
    ///     <code>
    /// // This example shows how to measure the accuracy of a 
    /// // binary classifier using a ROC curve. For this example,
    /// // we will be creating a Support Vector Machine trained
    /// // on the following training instances:
    /// 
    /// double[][] inputs =
    /// {
    ///     // Those are from class -1
    ///     new double[] { 2, 4, 0 },
    ///     new double[] { 5, 5, 1 },
    ///     new double[] { 4, 5, 0 },
    ///     new double[] { 2, 5, 5 },
    ///     new double[] { 4, 5, 1 },
    ///     new double[] { 4, 5, 0 },
    ///     new double[] { 6, 2, 0 },
    ///     new double[] { 4, 1, 0 },
    /// 
    ///     // Those are from class +1
    ///     new double[] { 1, 4, 5 },
    ///     new double[] { 7, 5, 1 },
    ///     new double[] { 2, 6, 0 },
    ///     new double[] { 7, 4, 7 },
    ///     new double[] { 4, 5, 0 },
    ///     new double[] { 6, 2, 9 },
    ///     new double[] { 4, 1, 6 },
    ///     new double[] { 7, 2, 9 },
    /// };
    /// 
    /// int[] outputs =
    /// {
    ///     -1, -1, -1, -1, -1, -1, -1, -1, // fist eight from class -1
    ///     +1, +1, +1, +1, +1, +1, +1, +1  // last eight from class +1
    /// };
    /// 
    /// // Next, we create a linear Support Vector Machine with 4 inputs
    /// SupportVectorMachine machine = new SupportVectorMachine(inputs: 3);
    /// 
    /// // Create the sequential minimal optimization learning algorithm
    /// var smo = new SequentialMinimalOptimization(machine, inputs, outputs);
    /// 
    /// // We learn the machine
    /// double error = smo.Run();
    /// 
    /// // And then extract its predicted labels
    /// double[] predicted = new double[inputs.Length];
    /// for (int i = 0; i &lt; predicted.Length; i++)
    ///     predicted[i] = machine.Compute(inputs[i]);
    /// 
    /// // At this point, the output vector contains the labels which
    /// // should have been assigned by the machine, and the predicted
    /// // vector contains the labels which have been actually assigned.
    /// 
    /// // Create a new ROC curve to assess the performance of the model
    /// var roc = new ReceiverOperatingCharacteristic(outputs, predicted);
    /// roc.Compute(100); // Compute a ROC curve with 100 cut-off points
    /// 
    /// // Generate a connected scatter plot for the ROC curve and show it on-screen
    /// ScatterplotBox.Show(roc.GetScatterplot(includeRandom: true), nonBlocking: true)
    /// 
    ///     .SetSymbolSize(0)      // do not display data points
    ///     .SetLinesVisible(true) // show lines connecting points
    ///     .SetScaleTight(true)   // tighten the scale to points
    ///     .WaitForClose();
    /// </code>
    ///     <para>
    ///         The resulting graph is shown below.
    ///     </para>
    ///     <img src="..\images\roc-curve.png" />
    /// </example>
    [Serializable]
    public class ReceiverOperatingCharacteristic
    {
        // The collection to hold our curve point information
        private double dfalse;

        // The values which represent positive and negative values in our
        //  measurement data (such as presence or absence of some disease)
        private double dtrue;
        private double max;

        // The minimum and maximum values in the prediction data (such
        // as categorical rankings collected from test subjects)
        private double min;

        // The real number of positives and negatives in the measured (true) data

        /// <summary>
        ///     Constructs a new Receiver Operating Characteristic model
        /// </summary>
        /// <param name="expected">
        ///     An array of binary values. Typically represented as 0 and 1, or -1 and 1,
        ///     indicating negative and positive cases, respectively. The maximum value
        ///     will be treated as the positive case, and the lowest as the negative.
        /// </param>
        /// <param name="actual">
        ///     An array of continuous values trying to approximate the measurement array.
        /// </param>
        public ReceiverOperatingCharacteristic(bool[] expected, int[] actual)
        {
            // Initial argument checking
            if (expected.Length != actual.Length)
            {
                throw new ArgumentException("The size of the measurement and prediction arrays must match.");
            }

            var dexpected = new double[expected.Length];
            for (var i = 0; i < dexpected.Length; i++)
            {
                dexpected[i] = expected[i] ? 1 : 0;
            }

            var dactual = new double[actual.Length];
            for (var i = 0; i < dactual.Length; i++)
            {
                dactual[i] = actual[i];
            }

            init(dexpected, dactual);
        }

        /// <summary>
        ///     Constructs a new Receiver Operating Characteristic model
        /// </summary>
        /// <param name="expected">
        ///     An array of binary values. Typically represented as 0 and 1, or -1 and 1,
        ///     indicating negative and positive cases, respectively. The maximum value
        ///     will be treated as the positive case, and the lowest as the negative.
        /// </param>
        /// <param name="actual">
        ///     An array of continuous values trying to approximate the measurement array.
        /// </param>
        public ReceiverOperatingCharacteristic(int[] expected, double[] actual)
        {
            // Initial argument checking
            if (expected.Length != actual.Length)
            {
                throw new ArgumentException("The size of the measurement and prediction arrays must match.");
            }

            init(expected.ToDouble(), actual);
        }

        /// <summary>
        ///     Constructs a new Receiver Operating Characteristic model
        /// </summary>
        /// <param name="expected">
        ///     An array of binary values. Typically represented as 0 and 1, or -1 and 1,
        ///     indicating negative and positive cases, respectively. The maximum value
        ///     will be treated as the positive case, and the lowest as the negative.
        /// </param>
        /// <param name="actual">
        ///     An array of continuous values trying to approximate the measurement array.
        /// </param>
        public ReceiverOperatingCharacteristic(double[] expected, double[] actual)
        {
            // Initial argument checking
            if (expected.Length != actual.Length)
            {
                throw new ArgumentException("The size of the measurement and prediction arrays must match.");
            }

            init(expected, actual);
        }

        private void init(double[] expected, double[] actual)
        {
            Expected = expected;
            Actual = actual;

            // Determine which numbers correspond to each binary category
            dtrue = dfalse = expected[0];
            for (var i = 1; i < expected.Length; i++)
            {
                if (dtrue < expected[i])
                {
                    dtrue = expected[i];
                }

                if (dfalse > expected[i])
                {
                    dfalse = expected[i];
                }
            }

            // Count the real number of positive and negative cases
            for (var i = 0; i < expected.Length; i++)
            {
                if (expected[i] == dtrue)
                {
                    Positives++;
                }
            }

            min = actual.Min();
            max = actual.Max();

            // Negative cases is just the number of cases minus the number of positives
            Negatives = Expected.Length - Positives;

            calculatePlacements();
        }

        [OnDeserialized]
        private void onDeserialized(StreamingContext context)
        {
            min = dfalse;
            max = dtrue;

            calculatePlacements();
        }

        [OnDeserializing]
        private void onDeserializing(StreamingContext context)
        {
            min = max = 0;
        }

        /// <summary>
        ///     Loads a curve from a stream.
        /// </summary>
        /// <param name="stream">The stream from which the curve is to be deserialized.</param>
        /// <returns>The deserialized curve.</returns>
        public static ReceiverOperatingCharacteristic Load(Stream stream)
        {
            /*BinaryFormatter b = new BinaryFormatter();
            return (ReceiverOperatingCharacteristic)b.Deserialize(stream);*/
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Loads a curve from a file.
        /// </summary>
        /// <param name="path">The path to the file from which the curve is to be deserialized.</param>
        /// <returns>The deserialized curve.</returns>
        public static ReceiverOperatingCharacteristic Load(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open))
            {
                return Load(fs);
            }
        }

        #region Properties

        /// <summary>
        ///     Gets the points of the curve.
        /// </summary>
        public ReceiverOperatingCharacteristicPointCollection Points { get; private set; }

        /// <summary>
        ///     Gets the number of actual positive cases.
        /// </summary>
        public int Positives { get; private set; }

        /// <summary>
        ///     Gets the number of actual negative cases.
        /// </summary>
        public int Negatives { get; private set; }

        /// <summary>
        ///     Gets the number of cases (observations) being analyzed.
        /// </summary>
        public int Observations => Expected.Length;

        /// <summary>
        ///     Gets the area under this curve (AUC).
        /// </summary>
        public double Area { get; private set; }

        /// <summary>
        ///     Gets the standard error for the <see cref="Area" />.
        /// </summary>
        public double StandardError { get; private set; }

        /// <summary>
        ///     Gets the variance of the curve's <see cref="Area" />.
        /// </summary>
        public double Variance => StandardError * StandardError;

        /// <summary>
        ///     Gets the ground truth values, or the values
        ///     which should have been given by the test if
        ///     it was perfect.
        /// </summary>
        public double[] Expected { get; private set; }

        /// <summary>
        ///     Gets the actual values given by the test.
        /// </summary>
        public double[] Actual { get; private set; }

        /// <summary>
        ///     Gets the actual test results for subjects
        ///     which should have been labeled as positive.
        /// </summary>
        public double[] PositiveResults { get; private set; }

        /// <summary>
        ///     Gets the actual test results for subjects
        ///     which should have been labeled as negative.
        /// </summary>
        public double[] NegativeResults { get; private set; }

        /// <summary>
        ///     Gets DeLong's pseudoaccuracies for the <see cref="PositiveResults">positive subjects</see>.
        /// </summary>
        public double[] PositiveAccuracies { get; private set; }

        /// <summary>
        ///     Gets DeLong's pseudoaccuracies for the <see cref="NegativeResults">negative subjects</see>
        /// </summary>
        public double[] NegativeAccuracies { get; private set; }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Computes a n-points ROC curve.
        /// </summary>
        /// <remarks>
        ///     Each point in the ROC curve will have a threshold increase of
        ///     1/npoints over the previous point, starting at zero.
        /// </remarks>
        /// <param name="points">The number of points for the curve.</param>
        public void Compute(int points)
        {
            // use a default increment of 1 if the increment would be 0
            var increment = max == min ? 1 : (max - min) / points;
            Compute(increment);
        }

        /// <summary>
        ///     Computes a ROC curve with 1/increment points
        /// </summary>
        /// <param name="increment">The increment over the previous point for each point in the curve.</param>
        public void Compute(double increment)
        {
            Compute(increment, true);
        }

        /// <summary>
        ///     Computes a ROC curve with 1/increment points
        /// </summary>
        /// <param name="increment">The increment over the previous point for each point in the curve.</param>
        /// <param name="forceOrigin">True to force the inclusion of the (0,0) point, false otherwise. Default is false.</param>
        public void Compute(double increment, bool forceOrigin)
        {
            if (increment <= 0)
            {
                throw new ArgumentException("Increment must be positive.", "increment");
            }

            var points = new List<ReceiverOperatingCharacteristicPoint>();
            double cutoff;

            // Create the curve, computing a point for each cutoff value
            for (cutoff = min; cutoff <= max; cutoff += increment)
            {
                points.Add(ComputePoint(cutoff));
            }


            // Sort the curve by descending specificity
            points.Sort(order);

            if (forceOrigin)
            {
                var last = points[points.Count - 1];
                if (last.FalsePositiveRate != 0.0 || last.Sensitivity != 0.0)
                {
                    points.Add(ComputePoint(double.PositiveInfinity));
                }
            }


            // Create the point collection
            Points = new ReceiverOperatingCharacteristicPointCollection(points.ToArray());

            // Calculate area and error associated with this curve
            Area = calculateAreaUnderCurve();
            StandardError = calculateStandardError();
        }

        /// <summary>
        ///     Computes a ROC curve with the given increment points
        /// </summary>
        public void Compute(params double[] cutpoints)
        {
            if (cutpoints.Length == 0)
            {
                throw new ArgumentException(
                    "Cutpoints array must have at least one value.", "cutpoints");
            }

            var points = new List<ReceiverOperatingCharacteristicPoint>();

            // Create the curve, computing a point for each cut-point
            for (var i = 0; i < cutpoints.Length; i++)
            {
                points.Add(ComputePoint(cutpoints[i]));
            }


            // Sort the curve by descending specificity
            points.Sort(order);


            // Create the point collection
            Points = new ReceiverOperatingCharacteristicPointCollection(points.ToArray());

            // Calculate area and error associated with this curve
            Area = calculateAreaUnderCurve();
            StandardError = calculateStandardError();
        }

        private static int order(ReceiverOperatingCharacteristicPoint a, ReceiverOperatingCharacteristicPoint b)
        {
            // First order by descending specificity
            var c = a.Specificity.CompareTo(b.Specificity);

            if (c == 0) // then order by ascending sensitivity
            {
                return -a.Sensitivity.CompareTo(b.Sensitivity);
            }

            return c;
        }

        /// <summary>
        ///     Computes a single point of a ROC curve using the given cutoff value.
        /// </summary>
        public ReceiverOperatingCharacteristicPoint ComputePoint(double threshold)
        {
            var truePositives = 0;
            var trueNegatives = 0;

            for (var i = 0; i < Expected.Length; i++)
            {
                var actual = Expected[i] == dtrue;
                var predicted = Actual[i] >= threshold;


                // If the prediction equals the true measured value
                if (predicted == actual)
                {
                    // We have a hit. Now we have to see
                    //  if it was a positive or negative hit
                    if (predicted)
                    {
                        truePositives++; // Positive hit
                    }
                    else
                    {
                        trueNegatives++; // Negative hit
                    }
                }
            }

            // The other values can be computed from available variables
            var falsePositives = Negatives - trueNegatives;
            var falseNegatives = Positives - truePositives;

            return new ReceiverOperatingCharacteristicPoint(threshold,
                truePositives, falseNegatives,
                falsePositives, trueNegatives);
        }

        /// <summary>
        ///     Generates a <see cref="Scatterplot" /> representing the ROC curve.
        /// </summary>
        /// <param name="includeRandom">
        ///     True to include a plot of the random curve (a diagonal line
        ///     going from lower left to upper right); false otherwise.
        /// </param>
        public Scatterplot GetScatterplot(bool includeRandom = false)
        {
            var plot = new Scatterplot("Area under the ROC curve");

            plot.XAxisTitle = "1 - Specificity";
            plot.YAxisTitle = "Sensitivity";

            var x = Points.GetOneMinusSpecificity();
            var y = Points.GetSensitivity();

            if (includeRandom)
            {
                var points = x.Length;
                var newx = new double[points + 2];
                var newy = new double[points + 2];
                var labels = new int[points + 2];

                Array.Copy(x, newx, x.Length);
                Array.Copy(y, newy, y.Length);

                newx[points + 0] = 0;
                newy[points + 0] = 0;
                labels[points + 0] = 1;

                newx[points + 1] = 1;
                newy[points + 1] = 1;
                labels[points + 1] = 1;

                plot.Compute(newx, newy, labels);
                plot.Classes[0].Text = "Curve";
                plot.Classes[1].Text = "Random";
            }
            else
            {
                plot.Compute(x, y);
            }

            return plot;
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this curve.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this curve.
        /// </returns>
        public override string ToString()
        {
            // TODO: Implement support for custom formats
            return string.Format(CultureInfo.CurrentCulture,
                "AUC:{0:0.0} Error:{1:0.0} Positives:{2} Negatives:{3}",
                Area, StandardError, Positives, Negatives);
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Calculates the area under the ROC curve using the trapezium method.
        /// </summary>
        /// <remarks>
        ///     The area under a ROC curve can never be less than 0.50. If the area is first calculated as
        ///     less than 0.50, the definition of abnormal will be reversed from a higher test value to a
        ///     lower test value.
        /// </remarks>
        private double calculateAreaUnderCurve()
        {
            var sum = 0.0;
            double tpz;

            for (var i = 0; i < Points.Count - 1; i++)
            {
                // Obs: False Positive Rate = (1-specificity)
                tpz = Points[i].Sensitivity + Points[i + 1].Sensitivity;
                tpz = tpz * (Points[i].FalsePositiveRate - Points[i + 1].FalsePositiveRate) / 2.0;
                sum += tpz;
            }

            if (sum < 0.5)
            {
                return 1.0 - sum;
            }

            return sum;
        }

        private double calculateStandardError()
        {
            return System.Math.Sqrt(ReceiverOperatingCurveTest.DeLongVariance(PositiveAccuracies, NegativeAccuracies));
        }

        private void calculatePlacements()
        {
            // Get ratings for true positives
            var positiveIndices = Expected.Find(x => x == dtrue);
            var X = Actual.Get(positiveIndices);

            var negativeIndices = Expected.Find(x => x == dfalse);
            var Y = Actual.Get(negativeIndices);

            PositiveResults = X;
            NegativeResults = Y;

            // Transform into V variables
            PositiveAccuracies = X.Apply(x => v(x, Y));
            NegativeAccuracies = Y.Apply(y => 1.0 - v(y, X));
        }

        private static double v(double x, double[] y)
        {
            double sum = 0;
            for (var i = 0; i < y.Length; i++)
            {
                if (y[i] < x)
                {
                    sum += 1;
                }

                if (y[i] == x)
                {
                    sum += 0.5;
                }
            }

            return sum / y.Length;
        }

        #endregion
    }

    /// <summary>
    ///     Object to hold information about a Receiver Operating Characteristic Curve Point
    /// </summary>
    [Serializable]
    public class ReceiverOperatingCharacteristicPoint : ConfusionMatrix
    {
        // Discrimination threshold (cutoff value)

        /// <summary>
        ///     Constructs a new Receiver Operating Characteristic point.
        /// </summary>
        internal ReceiverOperatingCharacteristicPoint(double cutoff,
            int truePositives, int falseNegatives,
            int falsePositives, int trueNegatives)
            : base(truePositives, falseNegatives,
                falsePositives, trueNegatives)
        {
            this.Cutoff = cutoff;
        }

        /// <summary>
        ///     Gets the cutoff value (discrimination threshold) for this point.
        /// </summary>
        public double Cutoff { get; }

        /// <summary>
        ///     Returns a System.String that represents the current ReceiverOperatingCharacteristicPoint.
        /// </summary>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture,
                "({0}; {1})", Sensitivity, FalsePositiveRate);
        }
    }

    /// <summary>
    ///     Represents a Collection of Receiver Operating Characteristic (ROC) Curve points.
    ///     This class cannot be instantiated.
    /// </summary>
    [Serializable]
    public class
        ReceiverOperatingCharacteristicPointCollection : ReadOnlyCollection<ReceiverOperatingCharacteristicPoint>
    {
        internal ReceiverOperatingCharacteristicPointCollection(ReceiverOperatingCharacteristicPoint[] points)
            : base(points)
        {
        }

        /// <summary>
        ///     Gets the (1-specificity, sensitivity) values as (x,y) coordinates.
        /// </summary>
        /// <returns>
        ///     An jagged double array where each element is a double[] vector
        ///     with two positions; the first is the value for 1-specificity (x)
        ///     and the second the value for sensitivity (y).
        /// </returns>
        public double[][] GetXYValues()
        {
            var x = new double[Count];
            var y = new double[Count];

            for (var i = 0; i < Count; i++)
            {
                x[i] = 1.0 - this[i].Specificity;
                y[i] = this[i].Sensitivity;
            }

            var points = new double[Count][];
            for (var i = 0; i < points.Length; i++)
            {
                points[i] = new[] { x[i], y[i] };
            }

            return points;
        }

        /// <summary>
        ///     Gets an array containing (1-specificity)
        ///     values for each point in the curve.
        /// </summary>
        public double[] GetOneMinusSpecificity()
        {
            var x = new double[Count];
            for (var i = 0; i < x.Length; i++)
            {
                x[i] = 1.0 - this[i].Specificity;
            }

            return x;
        }

        /// <summary>
        ///     Gets an array containing (sensitivity)
        ///     values for each point in the curve.
        /// </summary>
        public double[] GetSensitivity()
        {
            var y = new double[Count];
            for (var i = 0; i < y.Length; i++)
            {
                y[i] = this[i].Sensitivity;
            }

            return y;
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Count.ToString();
        }
    }
}