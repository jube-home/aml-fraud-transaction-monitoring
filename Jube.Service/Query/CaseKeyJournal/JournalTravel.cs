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


using Jube.Dto.Query.CaseKeyJournal;

namespace Jube.Service.Query.CaseKeyJournal
{
    public static class JournalTravel
    {
        public const double LimitKmh = 900;
        public const double MinimumKilometres = 50;
        public const int Worst = 5;
        public const double MaximumLimitKmh = 10000;
        public const double MaximumKilometres = 20000;

        private const double EarthKilometres = 6371.0088;

        public sealed record Point(Guid Transaction, DateTimeOffset At, double Latitude, double Longitude, string Lane);

        public static bool Valid(double? limitKmh, double? minimumKilometres)
        {
            return (limitKmh is null || (double.IsFinite(limitKmh.Value) && limitKmh.Value is >= 1 and <= MaximumLimitKmh))
                   && (minimumKilometres is null || (double.IsFinite(minimumKilometres.Value)
                                                     && minimumKilometres.Value is >= 0 and <= MaximumKilometres));
        }

        public static double Kilometres(double fromLatitude, double fromLongitude, double toLatitude,
            double toLongitude)
        {
            static double Radians(double degrees) => degrees * Math.PI / 180;

            var dLatitude = Radians(toLatitude - fromLatitude);
            var dLongitude = Radians(toLongitude - fromLongitude);
            var a = Math.Pow(Math.Sin(dLatitude / 2), 2) +
                    Math.Cos(Radians(fromLatitude)) * Math.Cos(Radians(toLatitude)) *
                    Math.Pow(Math.Sin(dLongitude / 2), 2);

            return 2 * EarthKilometres * Math.Asin(Math.Min(1, Math.Sqrt(a)));
        }

        public static bool Located(double? latitude, double? longitude)
        {
            return latitude is { } lat && longitude is { } lon && double.IsFinite(lat) && double.IsFinite(lon)
                   && Math.Abs(lat) <= 90 && Math.Abs(lon) <= 180 && !(lat == 0 && lon == 0);
        }

        public static CaseKeyJournalTravelSummaryDto Summarise(IEnumerable<Point> points,
            double limitKmh = LimitKmh, double minimumKilometres = MinimumKilometres)
        {
            var legs = new List<CaseKeyJournalTravelLegDto>();

            foreach (var lane in points.GroupBy(point => point.Lane, StringComparer.Ordinal))
            {
                Point? previous = null;

                foreach (var point in lane.OrderBy(item => item.At).ThenBy(item => item.Transaction))
                {
                    if (previous is not null)
                    {
                        legs.Add(Leg(previous, point, limitKmh, minimumKilometres));
                    }

                    previous = point;
                }
            }

            return new CaseKeyJournalTravelSummaryDto
            {
                Legs = legs.Count,
                Impossible = legs.Count(leg => leg.Impossible),
                LimitKmh = limitKmh,
                MinimumKilometres = minimumKilometres,
                Worst = legs.Where(leg => leg.Impossible)
                    .OrderByDescending(leg => leg.SpeedKmh ?? double.PositiveInfinity)
                    .ThenByDescending(leg => leg.Kilometres).Take(Worst).ToList()
            };
        }

        private static CaseKeyJournalTravelLegDto Leg(Point from, Point to, double limitKmh, double minimumKilometres)
        {
            var kilometres = Kilometres(from.Latitude, from.Longitude, to.Latitude, to.Longitude);
            var hours = Math.Max(0, (to.At - from.At).TotalHours);
            var speed = hours > 0 ? kilometres / hours : kilometres > 0 ? double.PositiveInfinity : 0;

            return new CaseKeyJournalTravelLegDto
            {
                Lane = to.Lane,
                FromTransaction = from.Transaction,
                ToTransaction = to.Transaction,
                FromDate = from.At,
                ToDate = to.At,
                Kilometres = Math.Round(kilometres, 1),
                Hours = Math.Round(hours, 3),
                SpeedKmh = double.IsFinite(speed) ? Math.Round(speed, 1) : null,
                Impossible = kilometres >= minimumKilometres && speed > limitKmh
            };
        }
    }
}
