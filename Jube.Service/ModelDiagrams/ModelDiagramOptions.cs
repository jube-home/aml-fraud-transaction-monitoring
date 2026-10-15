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

using Jube.Charting.Core.Models;
using Jube.Charting.Diagrams.Models;
using Jube.Service.ModelDiagrams.Exceptions;

namespace Jube.Service.ModelDiagrams
{
    public sealed record ModelDiagramOptions
    {
        public const int DefaultMaximumJsonCharacters = 8 * 1024 * 1024;
        public const int DefaultMaximumJsonDepth = 128;
        public const int DefaultMaximumElements = 5000;
        public const int DefaultMaximumNetworkElements = 100_000;
        public string? Title { get; init; }
        public double Width { get; init; } = 720;
        public double Height { get; init; } = 480;
        public ThemeMode Theme { get; init; } = ThemeMode.Auto;
        public int MaximumElements { get; init; } = DefaultMaximumElements;
        public int MaximumNetworkElements { get; init; } = DefaultMaximumNetworkElements;
        public NetworkDetail NetworkDetail { get; init; } = NetworkDetail.Auto;
        public IReadOnlyList<string>? VariableNames { get; init; }
        public bool Interactive { get; init; } = true;
        public int MaximumJsonCharacters { get; init; } = DefaultMaximumJsonCharacters;
        public int MaximumJsonDepth { get; init; } = DefaultMaximumJsonDepth;

        public ChartOptions ToChartOptions(string defaultTitle, string? subtitle = null, string? description = null)
        {
            return new ChartOptions
            {
                Title = Title ?? defaultTitle,
                Subtitle = subtitle,
                Description = description,
                Width = Width,
                Height = Height,
                Theme = Theme,
                Interactive = Interactive
            };
        }

        internal void Validate()
        {
            if (!(Width >= 40 && Width <= 4000) || !(Height >= 20 && Height <= 4000))
            {
                throw new ArgumentException("Width must be 40 to 4000 and height 20 to 4000.", nameof(Width));
            }

            if (MaximumNetworkElements < 1)
            {
                throw new ArgumentException("MaximumNetworkElements must be at least 1.",
                    nameof(MaximumNetworkElements));
            }

            if (MaximumElements < 1)
            {
                throw new ArgumentException("MaximumElements must be at least 1.", nameof(MaximumElements));
            }

            if (MaximumJsonCharacters < 2 || MaximumJsonDepth < 1)
            {
                throw new ArgumentException("The JSON caps must be positive.", nameof(MaximumJsonCharacters));
            }
        }

        internal void RequireNetworkWithin(long count)
        {
            if (count > MaximumNetworkElements)
            {
                throw new ModelDiagramNotSupportedException(
                    "This model does not visualise well as a diagram: it has " + count +
                    " network nodes and the maximum is " +
                    MaximumNetworkElements + ". " + ModelDiagramNotSupportedException.SummaryAdvice);
            }
        }

        internal void RequireWithin(long count, string what)
        {
            if (count > MaximumElements)
            {
                throw new ModelDiagramNotSupportedException(
                    "This model does not visualise well as a diagram: it has " + count + " " + what +
                    " and the maximum is " + MaximumElements + ". " + ModelDiagramNotSupportedException.SummaryAdvice);
            }
        }

        internal IReadOnlyList<string> Variables(IReadOnlyList<string>? stored, int count, string prefix = "x")
        {
            if (VariableNames is { } names)
            {
                if (names.Count != count)
                {
                    throw new ArgumentException(
                        "The variable names override has " + names.Count + " names but the model has " + count +
                        " inputs.", nameof(VariableNames));
                }

                return names;
            }

            if (stored is not null && stored.Count == count)
            {
                return stored;
            }

            return Enumerable.Range(1, count).Select(i => prefix + i).ToArray();
        }
    }
}