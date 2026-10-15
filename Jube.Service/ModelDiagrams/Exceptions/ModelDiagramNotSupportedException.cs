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

using Jube.Charting.Exceptions;

namespace Jube.Service.ModelDiagrams.Exceptions
{
    public sealed class ModelDiagramNotSupportedException : ModelDiagramException
    {
        public const string SummaryAdvice = "Ask for a summary of the model instead of a diagram.";

        public ModelDiagramNotSupportedException(string message) : base(message)
        {
        }

        public ModelDiagramNotSupportedException(string message, Exception inner) : base(message, inner)
        {
        }

        internal static ModelDiagramNotSupportedException Wrap(ChartingException inner)
        {
            return new ModelDiagramNotSupportedException(
                "This model does not visualise well as a diagram: " + inner.Message + " " + SummaryAdvice, inner);
        }
    }
}