/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

namespace Jube.Charting.Diagrams
{
    public static class Diagram
    {
        public static NetworkDiagramBuilder Network()
        {
            return new NetworkDiagramBuilder();
        }

        public static TreeDiagramBuilder Tree()
        {
            return new TreeDiagramBuilder();
        }

        public static GraphDiagramBuilder Graph()
        {
            return new GraphDiagramBuilder();
        }

        public static CoefficientDiagramBuilder Coefficients()
        {
            return new CoefficientDiagramBuilder();
        }

        public static ForestOfTreesBuilder Forest()
        {
            return new ForestOfTreesBuilder();
        }

        public static DistributionPanelsBuilder Distributions()
        {
            return new DistributionPanelsBuilder();
        }

        public static MembershipFunctionsBuilder Fuzzy()
        {
            return new MembershipFunctionsBuilder();
        }

        public static ComponentsDiagramBuilder Components()
        {
            return new ComponentsDiagramBuilder();
        }
    }
}