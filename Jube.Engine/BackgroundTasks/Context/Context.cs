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

namespace Jube.Engine.BackgroundTasks.Context
{
    using GraphRecall;
    using Helpers;
    using Models;

    public class Context
    {
        public bool Ready { get; set; }

        public JsonSerializationHelper JsonSerializationHelper
        {
            get;
            set;
        } = new JsonSerializationHelper();

        public Services Services
        {
            get;
        } = new Services();

        public ConcurrentQueues ConcurrentQueues
        {
            get;
        } = new ConcurrentQueues();

        public Sanctions Sanctions
        {
            get;
        } = new Sanctions();

        public GraphRecallCache GraphRecall
        {
            get;
        } = new GraphRecallCache();

        public Tasks Tasks
        {
            get;
        } = new Tasks();

        public Counters Counters
        {
            get;
        } = new Counters();
    }
}
