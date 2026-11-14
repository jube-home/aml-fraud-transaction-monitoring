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


using Jube.Dto.Options;
using Jube.Dto.Query.CaseKeyJournal;

namespace Jube.Service.Query.CaseKeyJournal
{
    public static class JournalScoreBands
    {
        public static bool IsCalibratedProbability(JournalAdaptationDto adaptation)
        {
            ArgumentNullException.ThrowIfNull(adaptation);

            return adaptation is { Value: >= 0 and <= 1, Calibration: { Calibrated: true, Space: { } space } }
                   && string.Equals(space, "Probability", StringComparison.OrdinalIgnoreCase);
        }

        public static JournalScoreBand? Of(JournalAdaptationDto adaptation)
        {
            if (!IsCalibratedProbability(adaptation))
            {
                return null;
            }

            return adaptation.Value switch
            {
                >= 0.75 => JournalScoreBand.VeryLikely,
                >= 0.5 => JournalScoreBand.Likely,
                >= 0.25 => JournalScoreBand.Possible,
                _ => JournalScoreBand.Unlikely
            };
        }

        public static JournalCalibrationBandDto? Holding(JournalAdaptationDto adaptation)
        {
            ArgumentNullException.ThrowIfNull(adaptation);

            if (adaptation.Value is not { } value || adaptation.Calibration is null)
            {
                return null;
            }

            var bands = adaptation.Calibration.Bands
                .Where(band => band is { Lower: not null, Upper: not null, Observed: not null }).ToList();

            return bands.FirstOrDefault(band => value >= band.Lower && value < band.Upper)
                   ?? bands.OrderByDescending(band => band.Upper).FirstOrDefault(band => value >= band.Upper);
        }

        public static string Word(JournalScoreBand band)
        {
            return band switch
            {
                JournalScoreBand.VeryLikely => "Very likely",
                JournalScoreBand.Likely => "Likely",
                JournalScoreBand.Possible => "Possible",
                _ => "Unlikely"
            };
        }
    }
}
