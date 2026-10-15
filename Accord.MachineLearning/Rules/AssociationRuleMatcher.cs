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
using System.Linq;
using Accord.Math;

namespace Accord.MachineLearning.Rules
{
    /// <summary>
    ///     Association ruler matcher.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    [Serializable]
    public class AssociationRuleMatcher<T> :
        IMulticlassRefScoreClassifier<T[], T[][]>,
        IMulticlassRefScoreClassifier<SortedSet<T>, SortedSet<T>[]>,
        ITransform<T[], T[][]>,
        ITransform<SortedSet<T>, SortedSet<T>[]>
    {
        private int items;
        private AssociationRule<T>[] rules;
        private double threshold;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AssociationRuleMatcher{T}" /> class.
        /// </summary>
        /// <param name="items">The number of distinct items in the dataset.</param>
        /// <param name="rules">The association rules between items of the dataset.</param>
        public AssociationRuleMatcher(int items, AssociationRule<T>[] rules)
        {
            this.items = items;
            this.rules = rules;
        }

        /// <summary>
        ///     Gets the number of classes expected and recognized by the classifier.
        /// </summary>
        /// <value>The number of classes.</value>
        public int NumberOfClasses
        {
            get => rules.Length;
            set => throw new InvalidOperationException("This property is read-only.");
        }

        /// <summary>
        ///     Gets or sets the association rules in this model.
        /// </summary>
        public AssociationRule<T>[] Rules
        {
            get => rules;
            set => rules = value;
        }

        /// <summary>
        ///     Gets or sets the minimum confidence threshold used to
        ///     determine whether a rule applies to an input or not.
        /// </summary>
        public double Threshold
        {
            get => threshold;
            set => threshold = value;
        }

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning a
        ///     numerical score measuring the strength of association of the input vector
        ///     to each of the possible classes.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">
        ///     The class labels associated with each input
        ///     vector, as predicted by the classifier. If passed as null, the classifier
        ///     will create a new array.
        /// </param>
        public double[] Scores(SortedSet<T> input, ref SortedSet<T>[] decision)
        {
            var matches = new Dictionary<SortedSet<T>, double>(new Apriori<T>.SetComparer());
            foreach (var rule in rules)
            {
                if (rule.Matches(input))
                {
                    if (!rule.Y.IsSubsetOf(input) && rule.Confidence > threshold)
                    {
                        if (matches.ContainsKey(rule.Y))
                        {
                            matches[rule.Y] += rule.Confidence;
                        }
                        else
                        {
                            matches[rule.Y] = rule.Confidence;
                        }
                    }
                }
            }

            decision = new SortedSet<T>[matches.Count];
            var scores = new double[matches.Count];

            var i = 0;
            foreach (var pair in matches)
            {
                decision[i] = pair.Key;
                scores[i] = pair.Value;
                i++;
            }

            Array.Sort(scores, decision);
            Array.Reverse(scores);
            Array.Reverse(decision);

            return scores;
        }

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning a
        ///     numerical score measuring the strength of association of the input vector
        ///     to each of the possible classes.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">
        ///     The class labels associated with each input
        ///     vector, as predicted by the classifier. If passed as null, the classifier
        ///     will create a new array.
        /// </param>
        /// <param name="result">
        ///     An array where the distances will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[] Scores(SortedSet<T> input, ref SortedSet<T>[] decision, double[] result)
        {
            var r = Scores(input, ref decision);
            Array.Copy(r, result, r.Length);
            return result;
        }

        /// <summary>
        ///     Predicts a class label vector for each input vector, returning a
        ///     numerical score measuring the strength of association of the input vector
        ///     to each of the possible classes.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">
        ///     The class labels associated with each input
        ///     vector, as predicted by the classifier. If passed as null, the classifier
        ///     will create a new array.
        /// </param>
        public double[][] Scores(SortedSet<T>[] input, ref SortedSet<T>[][] decision)
        {
            return Scores(input, ref decision, new double[input.Length][]);
        }

        /// <summary>
        ///     Predicts a class label vector for each input vector, returning a
        ///     numerical score measuring the strength of association of the input vector
        ///     to each of the possible classes.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">
        ///     The class labels associated with each input
        ///     vector, as predicted by the classifier. If passed as null, the classifier
        ///     will create a new array.
        /// </param>
        /// <param name="result">
        ///     An array where the distances will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[][] Scores(SortedSet<T>[] input, ref SortedSet<T>[][] decision, double[][] result)
        {
            var r = Scores(input, ref decision);
            r.CopyTo(result);
            return r;
        }

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning a
        ///     numerical score measuring the strength of association of the input vector
        ///     to each of the possible classes.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">
        ///     The class labels associated with each input
        ///     vector, as predicted by the classifier. If passed as null, the classifier
        ///     will create a new array.
        /// </param>
        public double[] Scores(T[] input, ref T[][] decision)
        {
            SortedSet<T>[] d = null;
            var r = Scores(new SortedSet<T>(input), ref d);
            decision = d.Apply(x => x.ToArray());
            return r;
        }

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning a
        ///     numerical score measuring the strength of association of the input vector
        ///     to each of the possible classes.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">
        ///     The class labels associated with each input
        ///     vector, as predicted by the classifier. If passed as null, the classifier
        ///     will create a new array.
        /// </param>
        /// <param name="result">
        ///     An array where the distances will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[] Scores(T[] input, ref T[][] decision, double[] result)
        {
            SortedSet<T>[] d = null;
            var r = Scores(new SortedSet<T>(input), ref d);
            decision = d.Apply(x => x.ToArray());
            Array.Copy(r, result, r.Length);
            return r;
        }

        /// <summary>
        ///     Predicts a class label vector for each input vector, returning a
        ///     numerical score measuring the strength of association of the input vector
        ///     to each of the possible classes.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">
        ///     The class labels associated with each input
        ///     vector, as predicted by the classifier. If passed as null, the classifier
        ///     will create a new array.
        /// </param>
        public double[][] Scores(T[][] input, ref T[][][] decision)
        {
            return Scores(input, ref decision, new double[input.Length][]);
        }

        /// <summary>
        ///     Predicts a class label vector for each input vector, returning a
        ///     numerical score measuring the strength of association of the input vector
        ///     to each of the possible classes.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">
        ///     The class labels associated with each input
        ///     vector, as predicted by the classifier. If passed as null, the classifier
        ///     will create a new array.
        /// </param>
        /// <param name="result">
        ///     An array where the distances will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[][] Scores(T[][] input, ref T[][][] decision, double[][] result)
        {
            for (var i = 0; i < input.Length; i++)
            {
                result[i] = Scores(input[i], ref decision[i]);
            }

            return result;
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public SortedSet<T>[] Transform(SortedSet<T> input)
        {
            return Decide(input);
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public SortedSet<T>[][] Transform(SortedSet<T>[] input)
        {
            return Decide(input);
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <param name="result">
        ///     An array where the distances will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public SortedSet<T>[][] Transform(SortedSet<T>[] input, SortedSet<T>[][] result)
        {
            return Decide(input);
        }

        /// <summary>
        ///     Gets the number of items seen by the model during training.
        /// </summary>
        public int NumberOfInputs
        {
            get => items;
            set => throw new InvalidOperationException("This property is read-only.");
        }

        /// <summary>
        ///     Gets the number of association rules seen by the model.
        /// </summary>
        public int NumberOfOutputs
        {
            get => NumberOfClasses;
            set => throw new InvalidOperationException("This property is read-only.");
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public T[][] Transform(T[] input)
        {
            return Decide(input);
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public T[][][] Transform(T[][] input)
        {
            return Decide(input);
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <param name="result"></param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public T[][][] Transform(T[][] input, T[][][] result)
        {
            return Decide(input, result);
        }

        /// <summary>
        ///     Computes a class-label decision for a given <paramref name="input" />.
        /// </summary>
        /// <param name="input">
        ///     The input vector that should be classified into
        ///     one of the <see cref="ITransform.NumberOfOutputs" /> possible classes.
        /// </param>
        /// <returns>
        ///     A class-label that best described <paramref name="input" /> according
        ///     to this classifier.
        /// </returns>
        public SortedSet<T>[] Decide(SortedSet<T> input)
        {
            SortedSet<T>[] decision = null;
            Scores(input, ref decision);
            return decision;
        }

        // TODO: Move the below functionality to a base class in the
        // MachineLearning namespace (BaseScoreClassifier)

        /// <summary>
        ///     Computes a class-label decision for a given <paramref name="input" />.
        /// </summary>
        /// <param name="input">
        ///     The input vector that should be classified into
        ///     one of the <see cref="ITransform.NumberOfOutputs" /> possible classes.
        /// </param>
        /// <returns>
        ///     A class-label that best described <paramref name="input" /> according
        ///     to this classifier.
        /// </returns>
        public SortedSet<T>[][] Decide(SortedSet<T>[] input)
        {
            return Decide(input, new SortedSet<T>[input.Length][]);
        }

        /// <summary>
        ///     Computes a class-label decision for a given <paramref name="input" />.
        /// </summary>
        /// <param name="input">
        ///     The input vector that should be classified into
        ///     one of the <see cref="ITransform.NumberOfOutputs" /> possible classes.
        /// </param>
        /// <param name="result">
        ///     An array where the distances will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        /// <returns>
        ///     A class-label that best described <paramref name="input" /> according
        ///     to this classifier.
        /// </returns>
        public SortedSet<T>[][] Decide(SortedSet<T>[] input, SortedSet<T>[][] result)
        {
            for (var i = 0; i < input.Length; i++)
            {
                result[i] = Decide(input[i]);
            }

            return result;
        }

        /// <summary>
        ///     Computes a class-label decision for a given <paramref name="input" />.
        /// </summary>
        /// <param name="input">
        ///     The input vector that should be classified into
        ///     one of the <see cref="ITransform.NumberOfOutputs" /> possible classes.
        /// </param>
        /// <returns>
        ///     A class-label that best described <paramref name="input" /> according
        ///     to this classifier.
        /// </returns>
        public T[][] Decide(T[] input)
        {
            return Decide(new SortedSet<T>(input)).Apply(x => x.ToArray());
        }

        /// <summary>
        ///     Computes a class-label decision for a given <paramref name="input" />.
        /// </summary>
        /// <param name="input">
        ///     The input vector that should be classified into
        ///     one of the <see cref="ITransform.NumberOfOutputs" /> possible classes.
        /// </param>
        /// <returns>
        ///     A class-label that best described <paramref name="input" /> according
        ///     to this classifier.
        /// </returns>
        public T[][][] Decide(T[][] input)
        {
            return Decide(input, new T[input.Length][][]);
        }

        /// <summary>
        ///     Computes a class-label decision for a given <paramref name="input" />.
        /// </summary>
        /// <param name="input">
        ///     The input vector that should be classified into
        ///     one of the <see cref="ITransform.NumberOfOutputs" /> possible classes.
        /// </param>
        /// <param name="result">
        ///     An array where the distances will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        /// <returns>
        ///     A class-label that best described <paramref name="input" /> according
        ///     to this classifier.
        /// </returns>
        public T[][][] Decide(T[][] input, T[][][] result)
        {
            for (var i = 0; i < input.Length; i++)
            {
                result[i] = Decide(input[i]);
            }

            return result;
        }
    }
}