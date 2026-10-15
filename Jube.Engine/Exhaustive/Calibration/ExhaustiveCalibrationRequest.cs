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

namespace Jube.Engine.Exhaustive.Calibration
{
    public static class ExhaustiveCalibrationRequest
    {
        public const string Automatic = "automatic";
        public const string Platt = ExhaustiveCalibration.MethodPlatt;
        public const string Isotonic = ExhaustiveCalibration.MethodIsotonic;
        public const string Beta = ExhaustiveCalibration.MethodBeta;

        public static bool IsKnown(string value)
        {
            return value is Automatic or Platt or Isotonic or Beta;
        }

        internal static string OrDefault(string requested)
        {
            return IsKnown(requested) ? requested : Automatic;
        }
    }
}