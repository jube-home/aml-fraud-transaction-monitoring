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

namespace Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.Reprocessing
{
    public static class ReprocessingPageLimit
    {
        public const int Default = 10000;
        public const int Maximum = 100000;

        public static int Resolve(string setting)
        {
            if (!int.TryParse(setting, out var parsed) || parsed < 1)
            {
                return Default;
            }

            return parsed > Maximum ? Maximum : parsed;
        }
    }
}
