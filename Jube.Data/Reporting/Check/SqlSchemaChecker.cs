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

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Google.Protobuf;
using PgSqlParser;

namespace Jube.Data.Reporting.Check
{
    public static class SqlSchemaChecker
    {
        private static readonly Regex positionalPattern = new(@"\$\d+", RegexOptions.Compiled);

        public static IReadOnlyList<SqlFinding> Check(string sql, SqlSchemaSnapshot snapshot, SqlCheckOptions options)
        {
            if (string.IsNullOrWhiteSpace(sql) || snapshot == null || positionalPattern.IsMatch(sql))
            {
                return [];
            }

            options ??= new SqlCheckOptions();

            var substitution = SqlParameterSubstitution.Apply(sql);
            var map = new SqlPositionMap(sql, substitution.Text, substitution.Replacements);
            var sink = new SqlFindingSink(sql, map, false);

            Result<ParseResult> parsed;

            try
            {
                parsed = Parser.Parse(substitution.Text);
            }
            catch (InvalidProtocolBufferException)
            {
                return [];
            }

            if (parsed.Error != null)
            {
                var cursor = Math.Max(0, parsed.Error.CursorPos - 1);
                var atEnd = parsed.Error.Message.Contains("end of input", StringComparison.OrdinalIgnoreCase);

                sink.Add(SqlCheckMessages.SyntaxError, cursor, atEnd ? "end of statement" : NearToken(substitution.Text, cursor));

                return sink.Findings;
            }

            if (parsed.Value.Stmts.Count != 1 || parsed.Value.Stmts[0].Stmt?.SelectStmt == null)
            {
                return [];
            }

            try
            {
                new SqlSemanticWalker(snapshot, options, substitution.Names, sink)
                    .CheckQuery(parsed.Value.Stmts[0].Stmt.SelectStmt, null);
            }
            catch (InvalidProtocolBufferException)
            {
                return [];
            }

            return sink.Findings;
        }

        private static string NearToken(string text, int byteOffset)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            var start = Math.Min(byteOffset, bytes.Length);
            var end = start;

            while (end < bytes.Length && end - start < 24 && !char.IsWhiteSpace((char)bytes[end]))
            {
                end++;
            }

            var token = System.Text.Encoding.UTF8.GetString(bytes, start, end - start);

            return token.Length == 0 ? "end of statement" : token;
        }
    }
}
