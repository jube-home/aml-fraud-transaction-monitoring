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
    public class LatentTreeOptions
    {
        public LatentTreeOptions()
        {
            Seed = 0;
            Bins = 4;
            Missing = MissingValueHandling.Skip;
            MaxLatentStates = 4;
            MaxDepth = 3;
            CandidatesPerRound = 8;
            MinimumGain = 0;
            Smoothing = 0.5;
            Restarts = 2;
            LocalRestarts = 2;
            LocalMaxIterations = 30;
            LocalTolerance = 1e-3;
        }

        public int Seed { get; set; }

        public int Bins { get; set; }

        public MissingValueHandling Missing { get; set; }

        public int MaxLatentStates { get; set; }

        public int MaxDepth { get; set; }

        public int CandidatesPerRound { get; set; }

        public double MinimumGain { get; set; }

        public double Smoothing { get; set; }

        public int Restarts { get; set; }

        public int LocalRestarts { get; set; }

        public int LocalMaxIterations { get; set; }

        public double LocalTolerance { get; set; }
    }
}