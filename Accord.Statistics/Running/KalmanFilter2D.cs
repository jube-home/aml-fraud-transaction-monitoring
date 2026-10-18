// Accord Statistics Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © Pablo Guzman Sanchez, 2013
// pablogsanchez at gmail.com
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
// This code originated as a contribution by Pablo Sanches, originally based on
// Student Dave's tutorial on Object Tracking in Images Using 2D Kalman Filters,
// shared under the LGPL by explicit written permissions from both authors:
//
//   http://studentdavestutorials.weebly.com/object-tracking-2d-kalman-filter.html
//

using System;
using Accord.Math;

namespace Accord.Statistics.Running
{
    /// <summary>
    ///     Kalman filter for 2D coordinate systems.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://studentdavestutorials.weebly.com/object-tracking-2d-kalman-filter.html">
    ///                         Student Dave's tutorial on Object Tracking in Images Using 2D Kalman Filters.
    ///                         Available on: http://studentdavestutorials.weebly.com/object-tracking-2d-kalman-filter.html
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\KalmanFilterTest.cs" region="doc_push" />
    /// </example>
    [Serializable]
    public class KalmanFilter2D : IRunning<DoublePoint>, IRunning<double[]>
    {
        private static readonly double[,] diagonal =
        {
            { 1, 0, 0, 0 },
            { 0, 1, 0, 0 },
            { 0, 0, 1, 0 },
            { 0, 0, 0, 1 }
        };

        private double[,] A;
        private double[,] Aux;
        private double[,] B;
        private double[,] C;
        private double[,] Ex;

        private double[,] Ez;
        private double[,] K;
        private double[,] P;

        private double[,] Q_estimate; // (location_0, location_1, vel_0, vel_1)
        private double accelStdDev = 0.1f;

        private double acceleration = 0.0005f;
        private double samplingRate = 1;

        /// <summary>
        ///     Initializes a new instance of the <see cref="KalmanFilter2D" /> class.
        /// </summary>
        public KalmanFilter2D()
        {
            initialize();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KalmanFilter2D" /> class.
        /// </summary>
        /// <param name="samplingRate">The sampling rate.</param>
        /// <param name="acceleration">The acceleration.</param>
        /// <param name="accelerationStdDev">The acceleration standard deviation.</param>
        public KalmanFilter2D(double samplingRate, double acceleration, double accelerationStdDev)
        {
            this.acceleration = acceleration;
            accelStdDev = accelerationStdDev;
            this.samplingRate = samplingRate;

            initialize();
        }

        /// <summary>
        ///     Gets or sets the current X position of the object.
        /// </summary>
        public double X
        {
            get => Q_estimate[0, 0];
            set => Q_estimate[0, 0] = value;
        }

        /// <summary>
        ///     Gets or sets the current Y position of the object.
        /// </summary>
        public double Y
        {
            get => Q_estimate[1, 0];
            set => Q_estimate[1, 0] = value;
        }

        /// <summary>
        ///     Gets or sets the current object's velocity in the X axis.
        /// </summary>
        public double XAxisVelocity
        {
            get => Q_estimate[2, 0];
            set => Q_estimate[2, 0] = value;
        }

        /// <summary>
        ///     Gets or sets the current object's velocity in the Y axis.
        /// </summary>
        public double YAxisVelocity
        {
            get => Q_estimate[3, 0];
            set => Q_estimate[3, 0] = value;
        }

        /// <summary>
        ///     Gets or sets the observational noise
        ///     of the current object's in the X axis.
        /// </summary>
        public double NoiseX
        {
            get => Ez[0, 0];
            set => Ez[0, 0] = value;
        }

        /// <summary>
        ///     Gets or sets the observational noise
        ///     of the current object's in the Y axis.
        /// </summary>
        public double NoiseY
        {
            get => Ez[1, 1];
            set => Ez[1, 1] = value;
        }

        /// <summary>
        ///     Registers the occurrence of a value.
        /// </summary>
        /// <param name="value">The value to be registered.</param>
        public void Push(DoublePoint value)
        {
            Push(value.X, value.Y);
        }

        /// <summary>
        ///     Clears all measures previously computed.
        /// </summary>
        public void Clear()
        {
            NoiseX = 0;
            NoiseY = 0;

            XAxisVelocity = 0;
            YAxisVelocity = 0;

            X = 0;
            Y = 0;
        }

        /// <summary>
        ///     Registers the occurrence of a value.
        /// </summary>
        /// <param name="value">The value to be registered.</param>
        public void Push(double[] value)
        {
            if (value.Length != 2)
            {
                throw new DimensionMismatchException("value");
            }

            Push(value[0], value[1]);
        }

        private void initialize()
        {
            var dt = samplingRate;

            A = new[,]
            {
                { 1, 0, dt, 0 },
                { 0, 1, 0, dt },
                { 0, 0, 1, 0 },
                { 0, 0, 0, 1 }
            };

            B = new[,]
            {
                { dt * dt / 2 },
                { dt * dt / 2 },
                { dt },
                { dt }
            };

            C = new double[,]
            {
                { 1, 0, 0, 0 },
                { 0, 1, 0, 0 }
            };

            Ez = new[,]
            {
                { 1.0, 0.0 },
                { 0.0, 1.0 }
            };

            var dt2 = dt * dt;
            var dt3 = dt2 * dt;
            var dt4 = dt2 * dt2;

            var aVar = accelStdDev * accelStdDev;

            Ex = new[,]
            {
                { dt4 / 4, 0, dt3 / 2, 0 },
                { 0, dt4 / 4, 0, dt3 / 2 },
                { dt3 / 2, 0, dt2, 0 },
                { 0, dt3 / 2, 0, dt2 }
            };

            Ex.Multiply(aVar, Ex);

            Q_estimate = new double[4, 1];
            P = Matrix.MemberwiseClone(Ex);
        }

        /// <summary>
        ///     Registers the occurrence of a value.
        /// </summary>
        /// <param name="x">The x-coordinate of the value to be registered.</param>
        /// <param name="y">The y-coordinate of the value to be registered.</param>
        public void Push(double x, double y)
        {
            double[,] Qloc = { { x }, { y } };

            // Predict next state
            Q_estimate = A.Dot(Q_estimate).Add(B.Multiply(acceleration));

            // Predict Covariances
            P = A.Dot(P.DotWithTransposed(A)).Add(Ex);

            Aux = C.Dot(P.DotWithTransposed(C)).Add(Ez).PseudoInverse();

            // Kalman Gain
            K = P.Dot(C.TransposeAndDot(Aux));
            Q_estimate = Q_estimate.Add(K.Dot(Qloc.Subtract(C.Dot(Q_estimate))));

            // Update P (Covariances)
            P = diagonal.Subtract(K.Dot(C)).Dot(P);
        }
    }
}