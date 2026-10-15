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

namespace Jube.Analytics.Datasets
{
    public static class DatasetProcessingTypes
    {
        public const int Payload = 1;
        public const int Dictionary = 2;
        public const int TtlCounter = 3;
        public const int Sanction = 4;
        public const int Abstraction = 5;
        public const int AbstractionCalculation = 6;

        public static bool IsKnown(int value)
        {
            return value is Payload or Dictionary or TtlCounter or Sanction or Abstraction or AbstractionCalculation;
        }

        public static string Prefix(int value)
        {
            return value switch
            {
                Payload => "Payload",
                Dictionary => "Dictionary",
                TtlCounter => "TtlCounter",
                Sanction => "Sanction",
                AbstractionCalculation => "AbstractionCalculation",
                _ => "Abstraction"
            };
        }
    }
}