// AForge Neural Net Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright � AForge.NET, 2007-2012
// contacts@aforgenet.com
//

using System;
using System.IO;
using Accord.IO;

namespace Accord.Neuro
{
    /// <summary>
    ///     Base neural network class.
    /// </summary>
    /// <remarks>
    ///     This is a base neural netwok class, which represents
    ///     collection of neuron's layers.
    /// </remarks>
    [Serializable]
    public abstract class Network
    {
        /// <summary>
        ///     Network's inputs count.
        /// </summary>
        protected int inputsCount;

        /// <summary>
        ///     Network's layers.
        /// </summary>
        protected Layer[] layers;

        /// <summary>
        ///     Network's layers count.
        /// </summary>
        protected int layersCount;

        /// <summary>
        ///     Network's output vector.
        /// </summary>
        protected double[] output;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Network" /> class.
        /// </summary>
        /// <param name="inputsCount">Network's inputs count.</param>
        /// <param name="layersCount">Network's layers count.</param>
        /// <remarks>
        ///     Protected constructor, which initializes <see cref="inputsCount" />,
        ///     <see cref="layersCount" /> and <see cref="layers" /> members.
        /// </remarks>
        protected Network(int inputsCount, int layersCount)
        {
            this.inputsCount = System.Math.Max(1, inputsCount);
            this.layersCount = System.Math.Max(1, layersCount);
            // create collection of layers
            layers = new Layer[this.layersCount];
        }

        /// <summary>
        ///     Network's inputs count.
        /// </summary>
        public int InputsCount
        {
            get => inputsCount;
            set => inputsCount = value;
        }

        /// <summary>
        ///     Network's layers.
        /// </summary>
        public int LayersCount
        {
            get => layersCount;
            set => layersCount = value;
        }

        public Layer[] Layers
        {
            get => layers;
            set => layers = value;
        }

        /// <summary>
        ///     Network's output vector.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The calculation way of network's output vector is determined by
        ///         layers, which comprise the network.
        ///     </para>
        ///     <para>
        ///         <note>
        ///             The property is not initialized (equals to <see langword="null" />) until
        ///             <see cref="Compute" /> method is called.
        ///         </note>
        ///     </para>
        /// </remarks>
        public double[] Output
        {
            get => output;
            set => output = value;
        }

        /// <summary>
        ///     Compute output vector of the network.
        /// </summary>
        /// <param name="input">Input vector.</param>
        /// <returns>Returns network's output vector.</returns>
        /// <remarks>
        ///     <para>
        ///         The actual network's output vecor is determined by layers,
        ///         which comprise the layer - represents an output vector of the last layer
        ///         of the network. The output vector is also stored in <see cref="Output" /> property.
        ///     </para>
        ///     <para>
        ///         <note>
        ///             The method may be called safely from multiple threads to compute network's
        ///             output value for the specified input values. However, the value of
        ///             <see cref="Output" /> property in multi-threaded environment is not predictable,
        ///             since it may hold network's output computed from any of the caller threads. Multi-threaded
        ///             access to the method is useful in those cases when it is required to improve performance
        ///             by utilizing several threads and the computation is based on the immediate return value
        ///             of the method, but not on network's output property.
        ///         </note>
        ///     </para>
        /// </remarks>
        public virtual double[] Compute(double[] input)
        {
            // local variable to avoid mutlithread conflicts
            var output = input;

            // compute each layer
            for (var i = 0; i < layers.Length; i++)
            {
                output = layers[i].Compute(output);
            }

            // assign output property as well (works correctly for single threaded usage)
            this.output = output;

            return output;
        }

        /// <summary>
        ///     Randomize layers of the network.
        /// </summary>
        /// <remarks>
        ///     Randomizes network's layers by calling <see cref="Layer.Randomize" /> method
        ///     of each layer.
        /// </remarks>
        public virtual void Randomize()
        {
            foreach (var layer in layers)
            {
                layer.Randomize();
            }
        }

#if !NO_BINARY_SERIALIZATION
        /// <summary>
        ///     Obsolete. Please use <see cref="Accord.IO.Serializer.Save{T}(T, string)" /> instead.
        /// </summary>
        [Obsolete("Please use Accord.IO.Serializer.Save(network, fileName) instead.")]
        public void Save(string fileName)
        {
            Serializer.Save(this, fileName);
        }

        /// <summary>
        ///     Obsolete. Please use <see cref="Accord.IO.Serializer.Save{T}(T, Stream, IO.SerializerCompression)" /> instead.
        /// </summary>
        [Obsolete("Please use Accord.IO.Serializer.Save(network, stream) instead.")]
        public void Save(Stream stream)
        {
            Serializer.Save(this, stream);
        }

        /// <summary>
        ///     Obsolete. Please use <see cref="Accord.IO.Serializer.Load{T}(string)" /> instead.
        /// </summary>
        [Obsolete("Please use Accord.IO.Serializer.Load<Network>(fileName) instead.")]
        public static Network Load(string fileName)
        {
            return Serializer.Load<Network>(fileName);
        }

        /// <summary>
        ///     Obsolete. Please use <see cref="Accord.IO.Serializer.Load{T}(Stream, IO.SerializerCompression)" /> instead.
        /// </summary>
        [Obsolete("Please use Accord.IO.Serializer.Load<Network>(stream) instead.")]
        public static Network Load(Stream stream)
        {
            return Serializer.Load<Network>(stream);
        }
#endif
    }
}