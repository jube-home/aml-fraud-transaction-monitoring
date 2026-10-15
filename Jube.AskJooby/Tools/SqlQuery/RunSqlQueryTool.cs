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

using System.ComponentModel;
using System.Text;
using Jube.AskJooby.DataCache;
using Jube.AskJooby.DataCache.Models;
using Jube.AskJooby.DataCache.Interfaces;
using Jube.AskJooby.Observability;
using Jube.AskJooby.Security;
using Jube.AskJooby.Security.Models;
using Jube.AskJooby.Security.Interfaces;
using Jube.AskJooby.Tools.SqlQuery.Interfaces;
using Jube.AskJooby.SqlQuery.Models;

namespace Jube.AskJooby.Tools.SqlQuery
{
    public sealed class RunSqlQueryTool(
        ISqlQueryClient queryClient,
        IToolResultCache cache,
        IAgentGuardRejectionLedger? guardRejectionLedger = null,
        IReadOnlyList<AgentGuardRuleSetting>? agentGuardRules = null)
    {
        private const int PreviewRowCount = 3;
        private const string ResultGuardPipelineName = "askjooby-sql-row-withheld";
        private const int PlanLineCount = 25;
        private const long LargeTableRowCount = 1000000;
        private const int SkewMultiple = 10;
        private const double MinimumCacheHitPercentage = 85;
        private const int BusyCallCount = 100;
        private const int WideReturnRowCount = 5000;

        [Description(
            "Lists every table you are allowed to query with RunSqlQuery, for this caller and this " +
            "tenant, each with its own documentation comment attached -- the same description a human " +
            "reading the schema directly would see, often enough on its own to settle which of two " +
            "similarly named tables (CaseWorkflow versus Case, say) actually holds what you are after, " +
            "without a separate Pasture search. Call this BEFORE writing any SQL rather than guessing a " +
            "table name -- a name that is not on this list fails the whole query, and a name you " +
            "invented will not be on it. Pass nameContains to narrow it to the tables you actually care " +
            "about right now; leave it unset for the full list with every comment, which is longer but " +
            "still cheap next to a round trip spent guessing wrong. The list reflects your own " +
            "permissions, so a table absent from it is one you are not entitled to read, not one that " +
            "does not exist. Prefer a dedicated tool over SQL where one exists for what you are after; " +
            "this is the fallback, not the first reach.")]
        public async Task<string> ListQueryableTables(
            [Description(
                "Optional substring to narrow the list to the tables whose names contain it, case "
                + "insensitive -- e.g. \"entityanalysismodel\", \"case\", \"exhaustive\", \"pg_stat\". "
                + "Leave unset to see every table you may query, each with its comment.")]
            string? nameContains = null,
            CancellationToken token = default)
        {
            var tables = queryClient.ListQueryableTables();

            if (tables.Count == 0)
            {
                return "You cannot query any table. Use the dedicated tools instead, or ask the caller "
                       + "to grant the permission the data sits behind.";
            }

            var matches = string.IsNullOrWhiteSpace(nameContains)
                ? tables
                : tables.Where(table => table.Contains(nameContains.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ToList();

            if (matches.Count == 0)
            {
                return $"None of the {tables.Count} tables you may query contain \"{nameContains}\". Try a "
                       + "shorter or different fragment, or omit the filter to see all of them. Do not "
                       + "guess a name that was not listed -- it will fail the whole query.";
            }

            var comments = await queryClient.TableCommentsAsync(matches, token).ConfigureAwait(false);

            var lines = matches.Select(table => comments.TryGetValue(table, out var comment)
                ? $"{table} -- {comment}"
                : $"{table} (no documentation comment recorded)");

            var header = string.IsNullOrWhiteSpace(nameContains)
                ? $"{matches.Count} tables you may query:"
                : $"{matches.Count} of {tables.Count} tables you may query match \"{nameContains}\":";

            return header + "\n" + string.Join("\n\n", lines);
        }

        [Description(
            "Checks a SELECT without running it: the statement is planned, which resolves every table, " +
            "column, function and type in it, and then thrown away, so nothing is read and nothing is " +
            "cached. Use it when you have written SQL against a schema you worked out rather than one you " +
            "have already queried successfully -- a misspelled column or a type that will not compare comes " +
            "back here as the exact database error, with no round trip spent fetching rows you were never " +
            "going to get. It cannot tell you whether the query answers your question, only that the " +
            "database understands it. An unregistered table is refused here exactly as it would be on " +
            "execution.")]
        public async Task<string> ValidateSqlQuery(
            [Description(
                "A single, complete, read-only SELECT statement, written exactly as you would pass it to " +
                "RunSqlQuery.")]
            string sql)
        {
            SqlValidation validation;

            try
            {
                validation = await queryClient.ValidateAsync(sql);
            }
            catch (InvalidOperationException ex)
            {
                return $"Not valid: {ex.Message}";
            }

            if (validation.IsValid)
            {
                return "Valid. Every table, column and type in the statement resolves. This says nothing "
                       + "about whether it returns what you want, or about what it will cost -- "
                       + "ExplainSqlQuery tells you the cost before you run it.";
            }

            var state = validation.SqlState is null ? string.Empty : $" [{validation.SqlState}]";

            return $"Not valid{state}: {validation.Error}";
        }

        [Description(
            "Returns the database's own plan and cost estimate for a SELECT without running it, so you can " +
            "sense check a query before paying for it. Worth a call before any query you expect to be " +
            "expensive: a wide date range, a join across several areas, an aggregate over a table you have " +
            "not filtered hard. Read the result as a sanity check against what you intended -- an estimated " +
            "row count far above what the question implies usually means a join condition is missing or a " +
            "filter is not selective, and the fix is a better WHERE clause rather than a bigger query. A " +
            "sequential scan is not automatically wrong, and on a small table it is the right plan, but a " +
            "sequential scan over something large is worth saying out loud in your answer so the caller " +
            "knows the number took real work to produce. The costs are the planner\'s arbitrary units, not " +
            "seconds, so compare them between two phrasings of the same query rather than reading one on " +
            "its own.")]
        public async Task<string> ExplainSqlQuery(
            [Description(
                "A single, complete, read-only SELECT statement, written exactly as you would pass it to " +
                "RunSqlQuery.")]
            string sql)
        {
            SqlPlanSummary plan;

            try
            {
                plan = await queryClient.ExplainAsync(sql);
            }
            catch (InvalidOperationException ex)
            {
                return $"Cannot plan this query: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Cannot plan this query: {ex.Message}. Try ValidateSqlQuery to see what the "
                       + "database objects to.";
            }

            var builder = new StringBuilder();
            builder.Append($"Estimated total cost {plan.TotalCost:0.##} (startup {plan.StartupCost:0.##}), ");
            builder.Append($"estimated {plan.EstimatedRows:0.##} row(s) returned. ");

            if (plan.SequentialScans.Count > 0)
            {
                builder.Append($"Sequential scan on {string.Join(", ", plan.SequentialScans)}. ");
            }

            if (plan.IndexScans.Count > 0)
            {
                builder.Append($"Index scan on {string.Join(", ", plan.IndexScans)}. ");
            }

            if (plan.SequentialScans.Count == 0 && plan.IndexScans.Count == 0)
            {
                builder.Append("No table scan in the plan at all. ");
            }

            builder.Append("Plan:\n");
            builder.Append(string.Join("\n", plan.PlanLines.Take(PlanLineCount)));

            if (plan.PlanLines.Count > PlanLineCount)
            {
                builder.Append($"\n... {plan.PlanLines.Count - PlanLineCount} further plan line(s) omitted.");
            }

            return builder.ToString();
        }

        [Description(
            "Reports how big each table your SELECT touches actually is, and whether the system has "
            + "historically reached it by index or by scanning it end to end. Call it when you are about "
            + "to query somewhere you have not queried before, and always before anything unfiltered or "
            + "aggregated: the log, counter, metric and archive tables in this system run to millions of "
            + "rows while most configuration tables hold tens, and the same query shape is trivial "
            + "against one and ruinous against the other. A table whose sequential scans vastly outnumber "
            + "its index scans is one nothing has ever managed to reach selectively, which is a warning "
            + "about the table rather than about your query. The row counts are the planner\'s running "
            + "estimate, not a COUNT, so treat them as an order of magnitude. Tables you are not "
            + "permitted to query are left out rather than reported as empty.")]
        public async Task<string> InspectTableStatistics(
            [Description(
                "A single, complete, read-only SELECT statement. Its table references are read out of "
                + "the statement for you, so pass the query you are considering rather than a list of "
                + "names.")]
            string sql)
        {
            IReadOnlyList<SqlTableStatistic> statistics;

            try
            {
                statistics = await queryClient.TableStatisticsAsync(sql);
            }
            catch (InvalidOperationException ex)
            {
                return $"Cannot read statistics for this query: {ex.Message}";
            }

            if (statistics.Count == 0)
            {
                return "No statistics are available for the tables in this statement. Either it "
                       + "references nothing you may query, or those tables have never been read or "
                       + "written since the statistics were last reset.";
            }

            var builder = new StringBuilder();

            foreach (var statistic in statistics)
            {
                builder.Append($"{statistic.TableName}: approximately {statistic.EstimatedRows:N0} row(s), ");
                builder.Append($"{statistic.IndexScans:N0} index scan(s), ");
                builder.Append($"{statistic.SequentialScans:N0} sequential scan(s). ");

                if (statistic.EstimatedRows >= LargeTableRowCount)
                {
                    builder.Append("This is a large table: do not read it unfiltered, and say so in your "
                                   + "answer if a scan of it is unavoidable. ");
                }
            }

            return builder.ToString();
        }

        [Description(
            "Reports what the database has already measured for statements against the same tables: how "
            + "often they are called, their average and worst case execution time, how many rows they "
            + "typically return, and how much of what they read came from memory rather than disk. Use it "
            + "before running anything you expect to be heavy, and read it like this. An average execution "
            + "time close to the worst case means the shape behaves predictably; a worst case more than "
            + "ten times the average means some parameter values fall off a cliff, so narrow the filter "
            + "before running and warn the caller that the timing is not dependable. A cache hit rate "
            + "below about eighty five percent over a meaningful number of calls means these tables are "
            + "being read from disk rather than memory, so check ExplainSqlQuery for an index before "
            + "adding to the load. An average return of thousands of rows is a sign the shape is "
            + "habitually used without a tight enough filter, so put a LIMIT on yours unless you genuinely "
            + "need every row. The numbers describe the tables, not your exact statement, and they are "
            + "cumulative since the statistics were last reset. When nothing has been recorded, fall "
            + "through to ExplainSqlQuery and judge the cold cost from the plan instead.")]
        public async Task<string> AnalyseQueryHistory(
            [Description(
                "A single, complete, read-only SELECT statement. Its table references are read out of the "
                + "statement for you.")]
            string sql)
        {
            SqlQueryHistory history;

            try
            {
                history = await queryClient.QueryHistoryAsync(sql);
            }
            catch (InvalidOperationException ex)
            {
                return $"Cannot read query history: {ex.Message}";
            }

            if (!history.HistoricalDataFound)
            {
                return "No history has been recorded for these tables. Use ExplainSqlQuery to judge the "
                       + "cold start cost from the plan instead.";
            }

            var builder = new StringBuilder();
            builder.Append($"{history.Calls:N0} recorded call(s) against these tables. ");
            builder.Append($"Average {history.AverageLatencyMilliseconds:0.##} ms, ");
            builder.Append($"worst {history.MaximumLatencyMilliseconds:0.##} ms, ");
            builder.Append($"average {history.AverageRowsReturned:0.##} row(s) returned, ");
            builder.Append($"{history.CacheHitPercentage:0.##}% of blocks served from cache. ");

            if (history.AverageLatencyMilliseconds > 0
                && history.MaximumLatencyMilliseconds > history.AverageLatencyMilliseconds * SkewMultiple)
            {
                builder.Append("The worst case is more than ten times the average, so some values on this "
                               + "shape are far slower than others: narrow the filter and do not promise "
                               + "the caller a quick answer. ");
            }

            if (history.CacheHitPercentage < MinimumCacheHitPercentage && history.Calls > BusyCallCount)
            {
                builder.Append("Less than eighty five percent of reads are being served from cache on a "
                               + "busy set of tables, so this is going to disk: check ExplainSqlQuery for "
                               + "an index before adding to it. ");
            }

            if (history.AverageRowsReturned > WideReturnRowCount)
            {
                builder.Append("Queries on these tables habitually return thousands of rows: put an "
                               + "explicit LIMIT on yours unless you genuinely need all of them. ");
            }

            return builder.ToString();
        }

        [Description(
            "A FALLBACK, not your first reach. Where a bound operation exists for what you want -- one of " +
            "the several hundred the specialist Joobriks hold, which read the same data through the " +
            "platform's own typed query methods -- use that instead, or delegate to the Joobrik that " +
            "holds it. A typed operation already knows the shape it is reading, applies the same scoping, " +
            "and costs you nothing in schema guesswork, where getting here means first listing tables and " +
            "working out columns before you have read anything at all. Reach for SQL when nothing bound " +
            "covers it: an ad hoc aggregate, a join across areas, a question no operation was written " +
            "for. " +
            "Runs a single read-only SQL SELECT against Jube's own operational data and caches the " +
            "result -- " +
            "afterwards use InspectDataSchema/PreviewDataRows/QueryCachedData with the data_pointer this " +
            "returns to actually look at what came back; never assume you can see the raw rows here. " +
            "Only a fixed allow-list of tables can be referenced -- most tenant-scoped tables in the " +
            "system. Referencing anything unregistered fails the whole query with a clear " +
            "error naming the table; don't guess at a substitute name, ask instead. Every row you get " +
            "back is already scoped to exactly what you're " +
            "permitted to see -- you never need to, and never can, filter by tenant or user yourself, " +
            "and there is no way to widen what you can see beyond that from here. " +
            "Soft deletion in Jube is a nullable column: a row that was never deleted usually has " +
            "\"Deleted\" NULL rather than 0, so write (\"Deleted\" = 0 OR \"Deleted\" IS NULL) and never " +
            "\"Deleted\" = 0 on its own, which silently returns nothing on perfectly live data. The same " +
            "goes for \"Locked\" and \"Active\". If a query you expected to return rows returns none, " +
            "suspect this before concluding the data is not there. " +
            "When a query works, lay it down at the Salt Lick with Remember: the statement verbatim, what " +
            "question it answers, and when it applies. Reaching here at all means no bound operation " +
            "covered the question, so a recorded query is both a shortcut next time and the evidence that " +
            "an operation is missing.")]
        public async Task<string> RunSqlQuery(
            [Description(
                "A single, complete, read-only SELECT statement. Literal values only -- no parameters, " +
                "no semicolons, no DDL/DML/CTEs-that-modify-data of any kind.")]
            string sql,
            [Description("A short label for what this query is for -- shown back to you later as a hint.")]
            string? label = null,
            CancellationToken token = default)
        {
            IReadOnlyList<IReadOnlyDictionary<string, object?>> rows;

            try
            {
                rows = await queryClient.ExecuteAsync(sql, token);
            }
            catch (InvalidOperationException ex)
            {
                return $"Query rejected: {ex.Message}";
            }

            var columns = BuildColumns(rows);
            var clean = new List<IReadOnlyDictionary<string, object?>>();
            var withheldFindings = new List<AskJoobyGuardFinding>();

            foreach (var row in rows)
            {
                var finding = await AskJoobyInjectionGuard.CheckToolContentAsync(
                    CachedRowText.Format(columns, row), agentGuardRules);

                if (finding is null)
                {
                    clean.Add(row);
                }
                else
                {
                    withheldFindings.Add(finding);
                }
            }

            if (guardRejectionLedger is not null
                && AgentGuardRejection.ForWithheldContent(
                    ResultGuardPipelineName, "row", "rows", withheldFindings) is { } rejection)
            {
                await guardRejectionLedger.RecordAsync(rejection, token).ConfigureAwait(false);
            }

            var pointer = await cache.StoreAsync(new CachedTable(columns, clean), label, token: token);

            var preview = clean.Take(PreviewRowCount)
                .Select(row => CachedRowText.Format(columns, row));

            var withheld = withheldFindings.Count;

            var withheldNote = withheld > 0
                ? $" {withheld} row{(withheld == 1 ? "" : "s")} withheld: they matched prompt-injection " +
                  "screening."
                : string.Empty;

            return $"{clean.Count} row(s), {columns.Count} column(s).{withheldNote} data_pointer: \"{pointer}\". " +
                   $"Columns: {string.Join(", ", columns.Select(c => c.Name))}. " +
                   $"First {Math.Min(PreviewRowCount, clean.Count)}: {string.Join(" | ", preview)}. " +
                   "Use InspectDataSchema/PreviewDataRows/QueryCachedData with this data_pointer to explore " +
                   "further. InspectDataSchema also reports what reading the whole table would cost " +
                   "in tokens, so check it before deciding how much of this you actually need.";
        }

        private static List<CachedColumn> BuildColumns(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
        {
            if (rows.Count == 0)
            {
                return [];
            }

            return rows[0].Keys.Select(key => new CachedColumn(key, InferType(rows, key))).ToList();
        }

        private static string InferType(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows, string key)
        {
            foreach (var row in rows)
            {
                if (row.TryGetValue(key, out var value) && value is not null)
                {
                    return value.GetType().Name;
                }
            }

            return "unknown";
        }
    }
}