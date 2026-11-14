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
    public static class ValidateFirst
    {
        public const string Instructions =
            "Every artefact you write that has a validator -- SQL, an R script, a rule expression -- is checked " +
            "before it is run, and that check is the most reliable source of truth you have. VALIDATE FIRST, " +
            "ALWAYS: call ValidateSqlQuery on a SELECT, or ValidateRScript on an R script, before you run it, and " +
            "loop -- fix what the validator names, validate again, and only when it comes back clean run the " +
            "statement. A run tool refuses a statement that was not validated in this conversation, so skipping " +
            "the validator only costs a round trip. FINDINGS ARE AUTHORITATIVE. A finding names a code, a " +
            "message, the line and column it occurred at, and where there is one a \"did you mean\" taken from " +
            "the names you are permitted to use. Fix exactly what it names: do not guess at other names, do not " +
            "retry the same statement hoping for a different answer, and do not ask the caller about a fault " +
            "the validator has already described. NEVER EXPECT DATABASE TEXT. A database rejection carries only " +
            "a SQLSTATE and means the validator missed something; report it plainly with its SQLSTATE, do not " +
            "speculate about table or column names, and do not try to infer the schema from it. The schema you " +
            "may use is the one ListQueryableTables gives you and the one the validator checks against.";
    }
}
