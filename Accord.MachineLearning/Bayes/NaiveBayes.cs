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
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using Accord.IO;
using Accord.Math;
using Accord.Math.Optimization.Losses;
using Accord.Statistics.Distributions;
using Accord.Statistics.Distributions.Univariate;

namespace Accord.MachineLearning.Bayes
{
#if !MONO
    /// <summary>
    ///     Naïve Bayes Classifier.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A naive Bayes classifier is a simple probabilistic classifier based on applying Bayes' theorem
    ///         with strong (naive) independence assumptions. A more descriptive term for the underlying probability
    ///         model would be "independent feature model".
    ///     </para>
    ///     <para>
    ///         In simple terms, a naive Bayes classifier assumes that the presence (or absence) of a particular
    ///         feature of a class is unrelated to the presence (or absence) of any other feature, given the class
    ///         variable. In spite of their naive design and apparently over-simplified assumptions, naive Bayes
    ///         classifiers have worked quite well in many complex real-world situations.
    ///     </para>
    ///     <para>
    ///         This class implements a discrete (integer-valued) Naive-Bayes classifier. There is also a
    ///         special
    ///         <see cref="Normal(int, int)">
    ///             named constructor to create classifiers assuming normal
    ///             distributions for each variable
    ///         </see>
    ///         . For arbitrary distribution classifiers, please see
    ///         <see cref="NaiveBayes{TDistribution}" />.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Wikipedia contributors. "Naive Bayes classifier." Wikipedia, The Free Encyclopedia.
    ///                     Wikipedia, The Free Encyclopedia, 16 Dec. 2011. Web. 5 Jan. 2012.
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         In this example, we will be using the famous Play Tennis example by Tom Mitchell (1998).
    ///         In Mitchell's example, one would like to infer if a person would play tennis or not
    ///         based solely on four input variables. Those variables are all categorical, meaning that
    ///         there is no order between the possible values for the variable (i.e. there is no order
    ///         relationship between Sunny and Rain, one is not bigger nor smaller than the other, but are
    ///         just distinct). Moreover, the rows, or instances presented below represent days on which the
    ///         behavior of the person has been registered and annotated, pretty much building our set of
    ///         observation instances for learning:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\Bayes\NaiveBayesTest.cs" region="doc_mitchell" />
    ///     <para>
    ///         <i>
    ///             Obs: The DataTable representation is not required, and instead the NaiveBayes could
    ///             also be trained directly on integer arrays containing the integer codewords.
    ///         </i>
    ///     </para>
    ///     <para>
    ///         In order to estimate a discrete Naive Bayes, we will first convert this problem to a more simpler
    ///         representation. Since all variables are categories, it does not matter if they are represented
    ///         as strings, or numbers, since both are just symbols for the event they represent. Since numbers
    ///         are more easily representable than text strings, we will convert the problem to use a discrete
    ///         alphabet through the use of a <see cref="Statistics.Filters.Codification">codebook</see>.
    ///     </para>
    ///     <para>
    ///         A codebook effectively transforms any distinct possible value for a variable into an integer
    ///         symbol. For example, “Sunny” could as well be represented by the integer label 0, “Overcast”
    ///         by “1”, Rain by “2”, and the same goes by for the other variables. So:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\Bayes\NaiveBayesTest.cs" region="doc_codebook" />
    ///     <para>
    ///         Now that we already have our learning input/output pairs, we should specify our
    ///         Bayes model. We will be trying to build a model to predict the last column, entitled
    ///         “PlayTennis”. For this, we will be using the “Outlook”, “Temperature”, “Humidity” and
    ///         “Wind” as predictors (variables which will we will use for our decision). Since those
    ///         are categorical, we must specify, at the moment of creation of our Bayes model, the
    ///         number of each possible symbols for those variables.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\Bayes\NaiveBayesTest.cs" region="doc_learn" />
    ///     <para>
    ///         Now that we have created and estimated our classifier, we
    ///         can query the classifier for new input samples through the
    ///         Decide method.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\Bayes\NaiveBayesTest.cs" region="doc_test" />
    ///     <para>
    ///         Please note that, while the example uses a DataTable to exemplify how data stored into tables
    ///         can be loaded in the framework, it is not necessary at all to use DataTables in your own, final
    ///         code. For example, please consider the same example shown above, but without DataTables:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\Bayes\NaiveBayesTest.cs" region="doc_mitchell_no_datatable" />
    ///     <para>
    ///         In this second example, we will be creating a simple multi-class
    ///         classification problem using integer vectors and learning a discrete
    ///         Naive Bayes on those vectors.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\Bayes\NaiveBayesTest.cs" region="doc_multiclass" />
    ///     <para>
    ///         Like all other learning algorithms in the framework, it is also possible to obtain a better measure
    ///         of the performance of the Naive Bayes algorithm using cross-validation, as shown in the example below:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\Bayes\NaiveBayesTest.cs" region="doc_cross_validation" />
    /// </example>
    /// <seealso cref="NaiveBayesLearning" />
    /// <seealso cref="NaiveBayes{TDistribution}" />
    /// <seealso cref="NaiveBayes{TDistribution, TInput}" />
    [Serializable]
    [SerializationBinder(typeof(NaiveBayesBinder))]
    public class NaiveBayes : NaiveBayes<GeneralDiscreteDistribution, int>
    {
        /// <summary>
        ///     Constructs a new Naïve Bayes Classifier.
        /// </summary>
        /// <param name="classes">The number of output classes.</param>
        /// <param name="symbols">The number of symbols for each input variable.</param>
        public NaiveBayes(int classes, params int[] symbols)
            : base(classes, symbols.Length,
                j => new GeneralDiscreteDistribution(logarithm: false, symbols: System.Math.Max(symbols[j], 2)))
        {
            if (classes < 2)
            {
                throw new ArgumentOutOfRangeException("classes");
            }

            if (symbols == null)
            {
                throw new ArgumentNullException("symbols");
            }

            this.NumberOfSymbols = symbols;
        }

        /// <summary>
        ///     Gets the number of symbols for each input in the model.
        /// </summary>
        public int[] NumberOfSymbols { get; private set; }

        /// <summary>
        ///     Gets the probability distributions for each class and input.
        /// </summary>
        /// <value>
        ///     A TDistribution[,] array in with each row corresponds to a
        ///     class, each column corresponds to an input variable. Each element
        ///     of this double[,] array is a probability distribution modeling
        ///     the occurrence of the input variable in the corresponding class.
        /// </value>
        public new double[,][] Distributions
        {
            // For backwards compatibility
            get
            {
                var freqs = new double[NumberOfOutputs, NumberOfSymbols.Length][];
                for (var i = 0; i < base.Distributions.Length; i++)
                {
                    for (var j = 0; j < base.Distributions[i].Components.Length; j++)
                    {
                        freqs[i, j] = base.Distributions[i][j].Frequencies;
                    }
                }

                return freqs;
            }
        }

        /// <summary>
        ///     Constructs a new Naïve Bayes Classifier.
        /// </summary>
        /// <param name="classes">The number of output classes.</param>
        /// <param name="inputs">The number of input variables.</param>
        public static NaiveBayes<NormalDistribution> Normal(int classes, int inputs)
        {
            return Normal(classes, inputs, NormalDistribution.Standard);
        }

        /// <summary>
        ///     Constructs a new Naïve Bayes Classifier.
        /// </summary>
        /// <param name="classes">The number of output classes.</param>
        /// <param name="inputs">The number of input variables.</param>
        /// <param name="initial">
        ///     An initial distribution to be used to initialized all independent
        ///     distribution components of this Naive Bayes. This distribution will
        ///     be cloned and made available in the <see cref="Distributions" /> property.
        /// </param>
        public static NaiveBayes<NormalDistribution> Normal(int classes, int inputs, NormalDistribution initial)
        {
            return new NaiveBayes<NormalDistribution>(classes, inputs, initial);
        }

        /// <summary>
        ///     Constructs a new Naïve Bayes Classifier.
        /// </summary>
        /// <param name="classes">The number of output classes.</param>
        /// <param name="inputs">The number of input variables.</param>
        /// <param name="classPriors">The prior probabilities for each output class.</param>
        public static NaiveBayes<NormalDistribution> Normal(int classes, int inputs, double[] classPriors)
        {
            return new NaiveBayes<NormalDistribution>(classes, inputs, NormalDistribution.Standard)
            {
                Priors = classPriors
            };
        }

        /// <summary>
        ///     Constructs a new Naïve Bayes Classifier.
        /// </summary>
        /// <param name="classes">The number of output classes.</param>
        /// <param name="inputs">The number of input variables.</param>
        /// <param name="initial">
        ///     An initial distribution to be used to initialized all independent
        ///     distribution components of this Naive Bayes. This distribution will
        ///     be cloned and made available in the <see cref="Distributions" /> property.
        /// </param>
        public static NaiveBayes<NormalDistribution> Normal(int classes, int inputs, NormalDistribution[] initial)
        {
            return new NaiveBayes<NormalDistribution>(classes, inputs, initial);
        }

        /// <summary>
        ///     Constructs a new Naïve Bayes Classifier.
        /// </summary>
        /// <param name="classes">The number of output classes.</param>
        /// <param name="inputs">The number of input variables.</param>
        /// <param name="initial">
        ///     An initial distribution to be used to initialized all independent
        ///     distribution components of this Naive Bayes. This distribution will
        ///     be cloned and made available in the <see cref="Distributions" /> property.
        /// </param>
        /// <param name="classPriors">The prior probabilities for each output class.</param>
        public static NaiveBayes<NormalDistribution> Normal(int classes, int inputs, NormalDistribution[] initial,
            double[] classPriors)
        {
            return new NaiveBayes<NormalDistribution>(classes, inputs, initial)
            {
                Priors = classPriors
            };
        }

        /// <summary>
        ///     Constructs a new Naïve Bayes Classifier.
        /// </summary>
        /// <param name="classes">The number of output classes.</param>
        /// <param name="inputs">The number of input variables.</param>
        /// <param name="initial">
        ///     An initial distribution to be used to initialized all independent
        ///     distribution components of this Naive Bayes. This distribution will
        ///     be cloned and made available in the <see cref="Distributions" /> property.
        /// </param>
        public static NaiveBayes<NormalDistribution> Normal(int classes, int inputs, NormalDistribution[,] initial)
        {
            return new NaiveBayes<NormalDistribution>(classes, inputs, initial);
        }

        #region Obsolete

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete("Please specify the prior by writing to the Priors property.")]
        public NaiveBayes(int classes, double[] priors, params int[] symbols)
            : this(classes, symbols)
        {
            if (priors == null)
            {
                throw new ArgumentNullException("priors");
            }

            Priors = priors;
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete("Please use NaiveBayesLearning instead.")]
        public double Estimate(int[][] inputs, int[] outputs,
            bool empirical = true, double regularization = 1e-5)
        {
            var teacher = new NaiveBayesLearning
            {
                Model = this
            };
            teacher.Empirical = empirical;
            teacher.Options.InnerOption.Regularization = regularization;
            teacher.Options.InnerOption.Minimum = 0;
#if DEBUG
            teacher.ParallelOptions.MaxDegreeOfParallelism = 1;
#endif
            var result = teacher.Learn(inputs, outputs);
            var b = result as NaiveBayes<GeneralDiscreteDistribution, int>;
            base.Distributions = b.Distributions;
            Priors = b.Priors;
            NumberOfSymbols = result.NumberOfSymbols;
            return new ZeroOneLoss(outputs) { Mean = true }.Loss(Decide(inputs));
        }

        /// <summary>
        ///     Gets the number of symbols for each input in the model.
        /// </summary>
        [Obsolete("Please use NumberOfSymbols instead.")]
        public int[] SymbolCount => NumberOfSymbols;

        /// <summary>
        ///     Gets the number of possible output classes.
        /// </summary>
        [Obsolete("Please use NumberOfOutputs instead.")]
        public int ClassCount => NumberOfOutputs;

        /// <summary>
        ///     Gets the number of inputs in the model.
        /// </summary>
        [Obsolete("Please use NumberOfInputs instead.")]
        public int InputCount => NumberOfInputs;

        /// <summary>
        ///     Computes the most likely class for a given instance.
        /// </summary>
        /// <param name="input">The input instance.</param>
        /// <returns>The most likely class for the instance.</returns>
        [Obsolete("Please use Decide instead.")]
        public int Compute(int[] input)
        {
            return Decide(input);
        }

        /// <summary>
        ///     Computes the most likely class for a given instance.
        /// </summary>
        /// <param name="input">The input instance.</param>
        /// <param name="logLikelihood">The log-likelihood for the instance.</param>
        /// <param name="responses">The response probabilities for each class.</param>
        /// <returns>The most likely class for the instance.</returns>
        [Obsolete("Please use Decide or LogLikelihood instead.")]
        public int Compute(int[] input, out double logLikelihood, out double[] responses)
        {
            var ll = LogLikelihoods(input);
            int imax;
            logLikelihood = ll.Max(out imax);
            responses = Special.Softmax(ll);
            return imax;
        }

#if !NETSTANDARD1_4
        /// <summary>
        ///     Saves the Naïve Bayes model to a stream.
        /// </summary>
        /// <param name="stream">The stream to which the Naïve Bayes model is to be serialized.</param>
        [Obsolete("Please use Accord.IO.Serializer.Save(stream) instead (or use it as an extension method).")]
        public virtual void Save(Stream stream)
        {
            Serializer.Save(this, stream);
        }

        /// <summary>
        ///     Saves the Naïve Bayes model to a stream.
        /// </summary>
        /// <param name="path">The path to the file to which the Naïve Bayes model is to be serialized.</param>
        [Obsolete("Please use Accord.IO.Serializer.Save(path) instead (or use it as an extension method).")]
        public void Save(string path)
        {
            Serializer.Save(this, path);
        }

        /// <summary>
        ///     Loads a machine from a stream.
        /// </summary>
        /// <param name="stream">The stream from which the Naïve Bayes model is to be deserialized.</param>
        /// <returns>The deserialized machine.</returns>
        [Obsolete("Please use Accord.IO.Serializer.Load<NaiveBayes>(stream) instead.")]
        public static NaiveBayes Load(Stream stream)
        {
            return Serializer.Load<NaiveBayes>(stream);
        }

        /// <summary>
        ///     Loads a machine from a file.
        /// </summary>
        /// <param name="path">The path to the file from which the Naïve Bayes model is to be deserialized.</param>
        /// <returns>The deserialized machine.</returns>
        [Obsolete("Please use Accord.IO.Serializer.Load<NaiveBayes>(path) instead.")]
        public static NaiveBayes Load(string path)
        {
            return Serializer.Load<NaiveBayes>(path);
        }

        /// <summary>
        ///     Loads a machine from a stream.
        /// </summary>
        /// <param name="stream">The stream from which the Naïve Bayes model is to be deserialized.</param>
        /// <returns>The deserialized machine.</returns>
        [Obsolete("Please use Accord.IO.Serializer.Load<NaiveBayes<TDist>>(stream) instead.")]
        public static NaiveBayes<TDist> Load<TDist>(Stream stream)
            where TDist : IFittableDistribution<double>, IUnivariateDistribution<double>,
            IUnivariateDistribution
        {
            return Serializer.Load<NaiveBayes<TDist>>(stream);
        }

        /// <summary>
        ///     Loads a machine from a file.
        /// </summary>
        /// <param name="path">The path to the file from which the Naïve Bayes model is to be deserialized.</param>
        /// <returns>The deserialized machine.</returns>
        [Obsolete("Please use Accord.IO.Serializer.Load<NaiveBayes<TDist>>(path) instead.")]
        public static NaiveBayes<TDist> Load<TDist>(string path)
            where TDist : IFittableDistribution<double>, IUnivariateDistribution<double>,
            IUnivariateDistribution
        {
            return Serializer.Load<NaiveBayes<TDist>>(path);
        }
#endif

        #endregion

        #region Serialization backwards compatibility

        internal class NaiveBayesBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                var name = new AssemblyName(assemblyName);

                if (name.Version < new Version(3, 1, 0))
                {
                    if (typeName == "Accord.MachineLearning.Bayes.NaiveBayes")
                    {
                        return typeof(NaiveBayes_2_13);
                    }
                }

                return null;
            }
        }

#pragma warning disable 0169
#pragma warning disable 0649

        [Serializable]
        private class NaiveBayes_2_13
        {
            private int classCount;
            private double[] priors;
            private double[,][] probabilities;
            private int[] symbols;

            public static implicit operator NaiveBayes(NaiveBayes_2_13 obj)
            {
                var result = new NaiveBayes(obj.priors.Length, obj.symbols);
                var nb = result as NaiveBayes<GeneralDiscreteDistribution, int>;
                nb.Priors = obj.priors;
                for (var i = 0; i < nb.Distributions.Length; i++)
                {
                    for (var j = 0; j < nb.Distributions[i].Components.Length; j++)
                    {
                        obj.probabilities[i, j].CopyTo(nb.Distributions[i].Components[j].Frequencies, 0);
                    }
                }

                return result;
            }
        }

#pragma warning restore 0169
#pragma warning restore 0649

        #endregion
    }

#else
        /// <summary>
        ///   This class is currently not supported in Mono due to
        ///   a bug in the Mono compiler.
        /// </summary>
        /// 
        [Obsolete("This class is not supported in Mono.")]
    public class NaiveBayes
    {
        /// <summary>
        ///   Not supported in Mono.
        /// </summary>
        /// 
        [Obsolete("This method is not supported in Mono.")]
        public int Normal(int a, int b)
        {
            return a + b;
        }
    }
#endif
}