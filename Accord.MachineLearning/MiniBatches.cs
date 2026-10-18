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

using System.Collections;
using System.Collections.Generic;
using Accord.MachineLearning.Performance;
using Accord.Math;

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Mini-batch data shuffling options.
    /// </summary>
    public enum ShuffleMethod
    {
        /// <summary>
        ///     Do not perform any shuffling.
        /// </summary>
        None,

        /// <summary>
        ///     Shuffle the data only once, before any batches are created.
        /// </summary>
        OnlyOnce,

        /// <summary>
        ///     Re-shuffles the data after every epoch.
        /// </summary>
        EveryEpoch
    }

    /// <summary>
    ///     Utility class for preparing mini-batches of data.
    /// </summary>
    public static class MiniBatches
    {
        /// <summary>
        ///     Creates a method to partition a given dataset into mini-batches of equal size.
        /// </summary>
        /// <typeparam name="TInput">The type of the input data.</typeparam>
        /// <typeparam name="TOutput">The type of the output output.</typeparam>
        /// <param name="input">The input data to be partitioned into mini-batches.</param>
        /// <param name="output">The output data to be partitioned into mini-batches.</param>
        /// <param name="weights">The weights for the data to be partitioned into mini-batches.</param>
        /// <param name="batchSize">The size of the batch.</param>
        /// <param name="maxIterations">The maximum number of mini-batches that should be created until the method stops.</param>
        /// <param name="maxEpochs">The maximum number of epochs that should be run until the method stops.</param>
        /// <param name="shuffle">The data shuffling options.</param>
        public static Batches<TInput, TOutput> Create<TInput, TOutput>(
            TInput[] input, TOutput[] output, double[] weights = null,
            int batchSize = 32, int maxIterations = 0, int maxEpochs = 0,
            ShuffleMethod shuffle = ShuffleMethod.EveryEpoch)
        {
            return new Batches<TInput, TOutput>(input, output, weights)
            {
                MiniBatchSize = batchSize,
                MaxIterations = maxIterations,
                MaxEpochs = maxEpochs,
                Shuffle = shuffle
            };
        }
    }

    /// <summary>
    ///     Utility class for preparing mini-batches of data.
    /// </summary>
    public class MiniBatches<TInput> : BaseBatches<DataSubset<TInput>, TInput>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MiniBatches{TInput}" /> class.
        /// </summary>
        /// <param name="inputs">The input data that should be divided into batches.</param>
        /// <param name="weights">The weight for the data that should be divided into batches.</param>
        public MiniBatches(TInput[] inputs, double[] weights)
            : base(inputs, weights)
        {
        }

        /// <summary>
        ///     Inheritors should use this method to create a new instance of a mini-batch object.
        ///     You can use this method to create mini-batches containing different objects related
        ///     to the mini-batch, such as auxiliary data, privileged information, etc).
        /// </summary>
        /// <returns>DataSubset&lt;TInput&gt;.</returns>
        protected override DataSubset<TInput> Init()
        {
            return new DataSubset<TInput>(MiniBatchSize, NumberOfSamples);
        }
    }

    /// <summary>
    ///     Utility class for preparing mini-batches of data.
    /// </summary>
    public class Batches<TInput, TOutput> : BaseBatches<DataSubset<TInput, TOutput>, TInput>
    {
        private TOutput[] shuffledOutputs;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Batches{TBatch, TInput}" /> class.
        /// </summary>
        /// <param name="inputs">The input data that should be divided into batches.</param>
        /// <param name="outputs">The output for the data that should be divided into batches.</param>
        /// <param name="weights">The weight for the data that should be divided into batches.</param>
        public Batches(TInput[] inputs, TOutput[] outputs, double[] weights)
            : base(inputs, weights)
        {
            if (inputs.Length != outputs.Length)
            {
                throw new DimensionMismatchException();
            }

            Outputs = outputs;
        }

        /// <summary>
        ///     Gets or sets the output associated with each input instances
        ///     that should be divided among the mini-batches at every epoch.
        /// </summary>
        public TOutput[] Outputs { get; set; }

        /// <summary>
        ///     Inheritors should use this method to create a new instance of a mini-batch object.
        ///     You can use this method to create mini-batches containing different objects related
        ///     to the mini-batch, such as auxiliary data, privileged information, etc).
        /// </summary>
        protected override DataSubset<TInput, TOutput> Init()
        {
            return new DataSubset<TInput, TOutput>(MiniBatchSize, NumberOfSamples);
        }

        /// <summary>
        ///     Inheritors should use this method to put the current sample in the
        ///     given mini-batch at the given position.
        /// </summary>
        /// <param name="batch">The mini-batch being constructed.</param>
        /// <param name="positionInMiniBatch">The position in mini-batch where the current sample must be put.</param>
        protected override void PutCurrentSampleInMiniBatch(DataSubset<TInput, TOutput> batch, int positionInMiniBatch)
        {
            base.PutCurrentSampleInMiniBatch(batch, positionInMiniBatch);
            batch.Outputs[positionInMiniBatch] = shuffledOutputs[CurrentSample];
        }

        /// <summary>
        ///     Inheritors should use this method to prepare the data for the next batches,
        ///     for example by reshuffling according to the ordering passed as argument.
        /// </summary>
        /// <param name="idx">The ordering for the samples in the new batches.</param>
        protected override void PrepareBatch(int[] idx)
        {
            base.PrepareBatch(idx);
            shuffledOutputs = Outputs.Get(idx);
        }
    }

    /// <summary>
    ///     Utility class for preparing mini-batches of data.
    /// </summary>
    public abstract class BaseBatches<TBatch, TInput> : IEnumerable<TBatch>
        where TBatch : DataSubset<TInput>
    {
        private int[] shuffledIndices;
        private TInput[] shuffledInputs;
        private double[] shuffledWeights;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseBatches{TBatch, TInput}" /> class.
        /// </summary>
        /// <param name="inputs">The input data that should be divided into batches.</param>
        /// <param name="weights">The weight for the data that should be divided into batches.</param>
        public BaseBatches(TInput[] inputs, double[] weights)
        {
            if (weights != null)
            {
                if (inputs.Length != weights.Length)
                {
                    throw new DimensionMismatchException();
                }
            }
            else
            {
                weights = Vector.Ones(inputs.Length);
            }

            Inputs = inputs;
            Weights = weights;
            MiniBatchSize = 32;
            Shuffle = ShuffleMethod.OnlyOnce;
            NumberOfMiniBatches = (int)System.Math.Ceiling(Inputs.Length / (double)MiniBatchSize);
        }

        /// <summary>
        ///     Gets or sets the input instances that should be divided among the mini-batches at every epoch.
        /// </summary>
        public TInput[] Inputs { get; set; }

        /// <summary>
        ///     Gets or sets the weights associated with each data instance
        ///     that should be divided among the mini-batches at every epoch.
        /// </summary>
        public double[] Weights { get; set; }

        /// <summary>
        ///     Gets or sets the size of the mini-batch that will be generated.
        /// </summary>
        public int MiniBatchSize { get; set; }

        /// <summary>
        ///     Gets or sets options about how and when data should be shuffled.
        /// </summary>
        public ShuffleMethod Shuffle { get; set; }

        /// <summary>
        ///     Gets the number of samples in each epoch.
        /// </summary>
        public int NumberOfSamples => Inputs.Length;

        /// <summary>
        ///     Gets or sets the number of mini-batches that are going to be generated for each epoch.
        /// </summary>
        public int NumberOfMiniBatches { get; }

        /// <summary>
        ///     Gets or sets the maximum number of iterations for which mini-batches should be generated.
        /// </summary>
        public int MaxIterations { get; set; }

        /// <summary>
        ///     Gets or sets the maximum number of epochs for which mini-batches should be generated.
        /// </summary>
        public int MaxEpochs { get; set; }

        /// <summary>
        ///     Gets or sets the current iteration counter.
        /// </summary>
        /// <value>The index of the current iteration.</value>
        public int CurrentIteration { get; set; }

        /// <summary>
        ///     Gets or sets the current epoch counter.
        /// </summary>
        /// <value>The index of the current epoch.</value>
        public int CurrentEpoch { get; set; }

        /// <summary>
        ///     Gets or sets the current sample counter.
        /// </summary>
        /// <value>The index of the current sample.</value>
        public int CurrentSample { get; set; }

        /// <summary>
        ///     Gets or sets the current mini-batch counter.
        /// </summary>
        /// <value>The index of the current mini-batch.</value>
        public int CurrentMiniBatch { get; set; }

        /// <summary>
        ///     Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<TBatch> GetEnumerator()
        {
            var batch = Init();

            if (Shuffle == ShuffleMethod.None)
            {
                PrepareBatch(Vector.Range(Inputs.Length));
            }
            else // if (Shuffle == ShuffleMethod.OnlyOnce || ShuffleMethod.EveryEpoch)
            {
                PrepareBatch(Vector.Sample(Inputs.Length));
            }

            CurrentIteration = 0;
            CurrentEpoch = 0;
            CurrentSample = 0;
            CurrentMiniBatch = 0;

            while (true)
            {
                for (var i = 0; i < batch.Inputs.Length; i++)
                {
                    PutCurrentSampleInMiniBatch(batch, i);

                    CurrentSample++;

                    if (CurrentSample >= NumberOfSamples)
                    {
                        CurrentEpoch++;
                        CurrentSample = 0;

                        if (Shuffle == ShuffleMethod.EveryEpoch)
                        {
                            PrepareBatch(Vector.Sample(Inputs.Length));
                        }
                    }
                }

                batch.Index = CurrentMiniBatch;

                yield return batch;

                CurrentMiniBatch++;
                if (CurrentMiniBatch >= NumberOfMiniBatches)
                {
                    CurrentMiniBatch = 0;
                }

                CurrentIteration++;

                if (MaxEpochs > 0 && CurrentEpoch > MaxEpochs)
                {
                    yield break;
                }

                if (MaxIterations > 0 && CurrentIteration > MaxIterations)
                {
                    yield break;
                }
            }
        }

        /// <summary>
        ///     Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     Inheritors should use this method to create a new instance of a mini-batch object.
        ///     You can use this method to create mini-batches containing different objects related
        ///     to the mini-batch, such as auxiliary data, privileged information, etc).
        /// </summary>
        protected abstract TBatch Init();

        /// <summary>
        ///     Inheritors should use this method to put the current sample in the
        ///     given mini-batch at the given position.
        /// </summary>
        /// <param name="batch">The mini-batch being constructed.</param>
        /// <param name="positionInMiniBatch">The position in mini-batch where the current sample must be put.</param>
        protected virtual void PutCurrentSampleInMiniBatch(TBatch batch, int positionInMiniBatch)
        {
            batch.Inputs[positionInMiniBatch] = shuffledInputs[CurrentSample];
            batch.Weights[positionInMiniBatch] = shuffledWeights[CurrentSample];
            batch.Indices[positionInMiniBatch] = shuffledIndices[CurrentSample];
        }

        /// <summary>
        ///     Inheritors should use this method to prepare the data for the next batches,
        ///     for example by reshuffling according to the ordering passed as argument.
        /// </summary>
        /// <param name="idx">The ordering for the samples in the new batches.</param>
        protected virtual void PrepareBatch(int[] idx)
        {
            shuffledInputs = Inputs.Get(idx);
            shuffledWeights = Weights.Get(idx);
            shuffledIndices = idx;
        }
    }
}