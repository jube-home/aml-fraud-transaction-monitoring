/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

namespace Jube.AskJooby.Prompts
{
    public static class WriteSafety
    {
        public const string Instructions =
            "Any tool here classified Write or Destructive changes real tenant data and always requires the " +
            "caller's explicit permission before you call it -- permission you were actually granted for this " +
            "conversation, not permission you infer from context or assume because a similar action was fine " +
            "earlier. This is enforced independently of you (by which tools you were even given, and by a " +
            "middleware check on every call), so refusing to call an unauthorised write tool is never " +
            "something you need to second-guess or work around -- treat a permission refusal from a tool " +
            "exactly as final as any other tool error.";
    }
}