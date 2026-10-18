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
using Accord.Math;

namespace Accord.Statistics.Testing
{
    /// <summary>
    ///     One-way Analysis of Variance (ANOVA).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The one-way ANOVA is a way to test for the equality of three or more means at the same
    ///         time by using variances. In its simplest form ANOVA provides a statistical test of whether
    ///         or not the means of several groups are all equal, and therefore generalizes t-test to more
    ///         than two groups.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Analysis_of_variance">
    ///                         Wikipedia, The Free Encyclopedia. Analysis of variance.
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/F_test">
    ///                         Wikipedia, The Free Encyclopedia. F-Test.
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/One-way_ANOVA">
    ///                         Wikipedia, The Free Encyclopedia. One-way ANOVA.
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The following is the same example given in Wikipedia's page for the
    ///         F-Test [1]. Suppose one would like to test the effect of three levels
    ///         of a fertilizer on plant growth.
    ///     </para>
    ///     <para>
    ///         To achieve this goal, an experimenter has divided a set of 18 plants on
    ///         three groups, 6 plants each. Each group has received different levels of
    ///         the fertilizer under question.
    ///     </para>
    ///     <para>
    ///         After some months, the experimenter registers the growth for each plant:
    ///     </para>
    ///     <code>
    /// double[][] samples =
    /// {
    ///     new double[] {  6,  8,  4,  5,  3,  4 }, // records for the first group
    ///     new double[] {  8, 12,  9, 11,  6,  8 }, // records for the second group
    ///     new double[] { 13,  9, 11,  8,  7, 12 }, // records for the third group
    /// };
    /// </code>
    ///     <para>
    ///         Now, he would like to test whether the different fertilizer levels has
    ///         indeed caused any effect in plant growth. In other words, he would like
    ///         to test if the three groups are indeed significantly different.
    ///     </para>
    ///     <code>
    /// // To do it, he runs an ANOVA test:
    /// OneWayAnova anova = new OneWayAnova(samples);
    /// </code>
    ///     <para>
    ///         After the Anova object has been created, one can display its findings
    ///         in the form of a standard ANOVA table by binding anova.Table to a
    ///         DataGridView or any other display object supporting data binding. To
    ///         illustrate, we could use Accord.NET's DataGridBox to inspect the
    ///         table's contents.
    ///     </para>
    ///     <code>
    ///   DataGridBox.Show(anova.Table);
    /// </code>
    ///     <para>Result in:</para>
    ///     <img src="..\images\one-way-anova.png" />
    ///     <para>
    ///         The p-level for the analysis is about 0.002, meaning the test is
    ///         significant at the 5% significance level. The experimenter would
    ///         thus reject the null hypothesis, concluding there is a strong
    ///         evidence that the three groups are indeed different. Assuming the
    ///         experiment was correctly controlled, this would be an indication
    ///         that the fertilizer does indeed affect plant growth.
    ///     </para>
    ///     <para>
    ///         [1] http://en.wikipedia.org/wiki/F_test
    ///     </para>
    /// </example>
    [Serializable]
    public class OneWayAnova : IAnova
    {
        private int DFb; // Between-group degrees-of-freedom
        private int DFt; // Total degrees-of-freedom
        private int DFw; // Within-group degrees-of-freedom

        private double MSb; // Between-group mean square
        private double MSw; // Within-group mean square

        private double SSb; // Between-group sum of squares
        private double SSt; // Total sum of squares
        private double SSw; // Within-group sum of squares
        private int groupCount;

        private double[] means;

        private int[] sizes;
        private double totalMean;
        private int totalSize;

        /// <summary>
        ///     Creates a new one-way ANOVA test.
        /// </summary>
        /// <param name="samples">The sampled values.</param>
        /// <param name="labels">The independent, nominal variables.</param>
        public OneWayAnova(double[] samples, int[] labels)
        {
            totalSize = samples.Length;
            groupCount = labels.Max() + 1;

            sizes = new int[groupCount];

            var groups = new double[groupCount][];
            for (var i = 0; i < groups.Length; i++)
            {
                var groupLabel = i;
                var idx = labels.Find(label => label == groupLabel);
                var group = samples.Get(idx);

                groups[i] = group;
                sizes[i] = group.Length;
            }

            initialize(groups);
        }

        /// <summary>
        ///     Creates a new one-way ANOVA test.
        /// </summary>
        /// <param name="samples">The grouped sampled values.</param>
        public OneWayAnova(params double[][] samples)
        {
            sizes = new int[samples.Length];

            groupCount = samples.Length;
            for (var i = 0; i < samples.Length; i++)
            {
                totalSize += sizes[i] = samples[i].Length;
            }

            initialize(samples);
        }

        /// <summary>
        ///     Gets the F-Test produced by this one-way ANOVA.
        /// </summary>
        public FTest FTest { get; private set; }

        /// <summary>
        ///     Gets the ANOVA results in the form of a table.
        /// </summary>
        public AnovaSourceCollection Table { get; private set; }

        private void initialize(double[][] samples)
        {
            DFb = groupCount - 1;
            DFw = totalSize - groupCount;
            DFt = totalSize - 1;

            // Step 1. Calculate the mean within each group
            means = samples.Mean(1);

            // Step 2. Calculate the overall mean
            totalMean = Measures.GrandMean(means, sizes);


            // Step 3. Calculate the "between-group" sum of squares
            for (var i = 0; i < samples.Length; i++)
            {
                //  between-group sum of squares
                var u = means[i] - totalMean;
                SSb += sizes[i] * u * u;
            }


            // Step 4. Calculate the "within-group" sum of squares
            for (var i = 0; i < samples.Length; i++)
            {
                for (var j = 0; j < samples[i].Length; j++)
                {
                    var u = samples[i][j] - means[i];
                    SSw += u * u;
                }
            }

            SSt = SSb + SSw; // total sum of squares


            // Step 5. Calculate the F statistic
            MSb = SSb / DFb; // between-group mean square
            MSw = SSw / DFw; // within-group mean square
            FTest = new FTest(MSb / MSw, DFb, DFw);


            // Step 6. Create the ANOVA table
            var table = new List<AnovaVariationSource>();
            table.Add(new AnovaVariationSource(this, "Between-Groups", SSb, DFb, FTest));
            table.Add(new AnovaVariationSource(this, "Within-Groups", SSw, DFw, null));
            table.Add(new AnovaVariationSource(this, "Total", SSt, DFt, null));
            Table = new AnovaSourceCollection(table);
        }
    }
}