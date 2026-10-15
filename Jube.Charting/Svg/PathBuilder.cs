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

using System.Text;

namespace Jube.Charting.Svg
{
    internal sealed class PathBuilder
    {
        private readonly StringBuilder builder = new();
        private bool penDown;
        public bool IsEmpty => builder.Length == 0;

        public PathBuilder Move(double x, double y)
        {
            if (!Finite(x, y))
            {
                penDown = false;

                return this;
            }

            Append("M", x, y);
            penDown = true;

            return this;
        }

        public PathBuilder Line(double x, double y)
        {
            if (!Finite(x, y))
            {
                penDown = false;

                return this;
            }

            if (!penDown)
            {
                return Move(x, y);
            }

            Append("L", x, y);

            return this;
        }

        public PathBuilder Cubic(double c1X, double c1Y, double c2X, double c2Y, double x, double y)
        {
            if (!Finite(c1X, c1Y) || !Finite(c2X, c2Y) || !Finite(x, y))
            {
                return Line(x, y);
            }

            if (!penDown)
            {
                return Move(x, y);
            }

            builder.Append('C').Append(Num.F(c1X)).Append(' ').Append(Num.F(c1Y)).Append(' ').Append(Num.F(c2X))
                .Append(' ').Append(Num.F(c2Y)).Append(' ').Append(Num.F(x)).Append(' ').Append(Num.F(y));

            return this;
        }

        public PathBuilder Gap()
        {
            penDown = false;

            return this;
        }

        public PathBuilder Arc(double radius, bool largeArc, bool sweep, double x, double y)
        {
            if (!double.IsFinite(radius) || !Finite(x, y))
            {
                return Line(x, y);
            }

            if (!penDown)
            {
                return Move(x, y);
            }

            builder.Append('A').Append(Num.F(radius)).Append(' ').Append(Num.F(radius)).Append(" 0 ")
                .Append(largeArc ? '1' : '0').Append(' ').Append(sweep ? '1' : '0').Append(' ').Append(Num.F(x))
                .Append(' ')
                .Append(Num.F(y));

            return this;
        }

        public PathBuilder Close()
        {
            builder.Append('Z');
            penDown = false;

            return this;
        }

        public override string ToString()
        {
            return builder.ToString();
        }

        private static bool Finite(double x, double y)
        {
            return double.IsFinite(x) && double.IsFinite(y);
        }

        private void Append(string command, double x, double y)
        {
            builder.Append(command).Append(Num.F(x)).Append(' ').Append(Num.F(y));
        }
    }
}