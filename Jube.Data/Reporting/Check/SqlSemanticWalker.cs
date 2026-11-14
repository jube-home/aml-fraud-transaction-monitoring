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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using PgSqlParser;

namespace Jube.Data.Reporting.Check
{
    internal sealed class SqlSemanticWalker(SqlSchemaSnapshot snapshot, SqlCheckOptions options,
        IReadOnlyList<string> parameterOrder, SqlFindingSink sink)
    {
        private static readonly OneofDescriptor nodeOneof = Node.Descriptor.Oneofs[0];

        private static readonly HashSet<string> comparisonOperators = ["=", "<>", "!=", "<", ">", "<=", ">="];

        private static readonly HashSet<string> arithmeticOperators = ["+", "-", "*", "/", "%", "^"];

        private static readonly HashSet<string> textOnlyFunctions =
        [
            "lower", "upper", "initcap", "ltrim", "rtrim", "btrim", "replace", "split_part", "left", "right", "md5"
        ];

        private static readonly HashSet<string> numericOnlyFunctions =
            ["abs", "round", "ceil", "ceiling", "floor", "sqrt", "power", "trunc"];

        private static readonly HashSet<string> numericAggregates =
            ["sum", "avg", "stddev", "stddev_pop", "stddev_samp", "variance", "var_pop", "var_samp"];

        private static readonly HashSet<string> timeFieldFunctions = ["date_trunc", "extract", "date_part"];

        public SqlDerivedColumns CheckQuery(SelectStmt statement, SqlScope parent)
        {
            var outer = parent;

            if (statement.WithClause != null)
            {
                outer = RegisterCommonTableExpressions(statement.WithClause, parent);
            }

            if (statement.Op != SetOperation.SetopNone)
            {
                var left = CheckQuery(statement.Larg, outer);
                var right = CheckQuery(statement.Rarg, outer);

                if (!left.Partial && !right.Partial && left.Columns.Count != right.Columns.Count)
                {
                    sink.Add(SqlCheckMessages.SetOperationColumns, -1, left.Columns.Count, right.Columns.Count);
                }

                var limitContext = new SqlContext(new SqlScope(outer), SqlClause.Other, false);
                CheckLimit(statement.LimitOffset, "OFFSET", limitContext);
                CheckLimit(statement.LimitCount, "LIMIT", limitContext);

                return left;
            }

            if (statement.ValuesLists.Count > 0)
            {
                var valuesContext = new SqlContext(new SqlScope(outer), SqlClause.Other, false);

                foreach (var row in statement.ValuesLists)
                {
                    Walk(row, valuesContext);
                }

                var width = statement.ValuesLists[0].List?.Items.Count ?? 0;

                return new SqlDerivedColumns(
                    Enumerable.Range(1, width)
                        .Select(i => new SqlColumnInfo("column" + i, SqlTypeCategory.Unknown, string.Empty)).ToList(),
                    false);
            }

            return CheckSimpleSelect(statement, outer);
        }

        private SqlScope RegisterCommonTableExpressions(WithClause with, SqlScope parent)
        {
            var scope = new SqlScope(parent);

            foreach (var node in with.Ctes)
            {
                var cte = node.CommonTableExpr;
                var renames = cte.Aliascolnames.Select(n => n.String?.Sval).Where(n => n != null).ToList();

                if (with.Recursive)
                {
                    scope.CommonTableExpressions[cte.Ctename] = new SqlDerivedColumns(
                        renames.Select(n => new SqlColumnInfo(n, SqlTypeCategory.Unknown, string.Empty)).ToList(),
                        renames.Count == 0);
                }

                var derived = cte.Ctequery?.SelectStmt != null
                    ? CheckQuery(cte.Ctequery.SelectStmt, scope)
                    : new SqlDerivedColumns([], true);

                scope.CommonTableExpressions[cte.Ctename] = Rename(derived, renames);
            }

            return scope;
        }

        private static SqlDerivedColumns Rename(SqlDerivedColumns derived, IReadOnlyList<string> names)
        {
            if (names.Count == 0)
            {
                return derived;
            }

            var columns = derived.Columns.Select((c, i) => i < names.Count ? c with { Name = names[i] } : c).ToList();

            return new SqlDerivedColumns(columns, derived.Partial);
        }

        private SqlDerivedColumns CheckSimpleSelect(SelectStmt statement, SqlScope outer)
        {
            var scope = new SqlScope(outer);
            var joinConditions = new List<Node>();

            foreach (var item in statement.FromClause)
            {
                AddSource(item, scope, joinConditions);
            }

            var context = new SqlContext(scope, SqlClause.Select, false);

            foreach (var condition in joinConditions)
            {
                CheckCondition(condition, context.In(SqlClause.Join));
            }

            foreach (var distinct in statement.DistinctClause)
            {
                Walk(distinct, context.In(SqlClause.Other));
            }

            foreach (var target in statement.TargetList)
            {
                var name = target.ResTarget?.Name;

                if (string.IsNullOrEmpty(name))
                {
                    name = target.ResTarget?.Val?.ColumnRef is { } reference
                        ? reference.Fields.LastOrDefault()?.String?.Sval
                        : null;
                }

                if (!string.IsNullOrEmpty(name))
                {
                    scope.OutputNames.Add(name);
                }
            }

            var columns = new List<SqlColumnInfo>();
            var partial = false;
            var skipGrouping = false;
            var targetRanges = new List<(int Start, int End, bool Bare)>();

            foreach (var target in statement.TargetList)
            {
                var resTarget = target.ResTarget;
                var value = resTarget.Val;
                var start = scope.Uses.Count;

                if (value?.ColumnRef != null && value.ColumnRef.Fields.Any(f => f.AStar != null))
                {
                    skipGrouping = true;
                    ExpandStar(value.ColumnRef, scope, columns, ref partial);
                    targetRanges.Add((start, scope.Uses.Count, false));

                    continue;
                }

                var type = Walk(value, context.In(SqlClause.Select));
                var named = OutputName(resTarget);

                if (named == null)
                {
                    partial = true;
                }

                columns.Add(new SqlColumnInfo(named ?? "?column?", type.Category, type.TypeName ?? string.Empty));
                targetRanges.Add((start, scope.Uses.Count, value?.ColumnRef is { Fields.Count: 1 }));
            }

            if (statement.WhereClause != null)
            {
                CheckCondition(statement.WhereClause, context.In(SqlClause.Where));
            }

            var exact = new HashSet<(SqlSource, string)>();
            var covered = new HashSet<(SqlSource, string)>();
            var targetCount = partial || skipGrouping ? -1 : columns.Count;

            foreach (var item in statement.GroupClause)
            {
                CheckGroupItem(item, context, scope, targetRanges, targetCount, exact, covered, ref skipGrouping);
            }

            if (statement.HavingClause != null)
            {
                CheckCondition(statement.HavingClause, context.In(SqlClause.Having));
            }

            foreach (var window in statement.WindowClause)
            {
                WalkWindow(window.WindowDef, context.In(SqlClause.Other));
            }

            foreach (var sort in statement.SortClause)
            {
                CheckSortItem(sort.SortBy, context.In(SqlClause.OrderBy), scope, targetCount);
            }

            CheckLimit(statement.LimitOffset, "OFFSET", context.In(SqlClause.Other));
            CheckLimit(statement.LimitCount, "LIMIT", context.In(SqlClause.Other));

            CheckGrouping(statement, scope, exact, covered, skipGrouping);

            return new SqlDerivedColumns(columns, partial);
        }

        private void CheckGroupItem(Node item, SqlContext context, SqlScope scope,
            List<(int Start, int End, bool Bare)> targetRanges, int targetCount,
            HashSet<(SqlSource, string)> exact, HashSet<(SqlSource, string)> covered, ref bool skipGrouping)
        {
            if (item.NodeCase == Node.NodeOneofCase.GroupingSet)
            {
                skipGrouping = true;
                Walk(item, context.In(SqlClause.GroupBy));

                return;
            }

            if (item.AConst is { ValCase: A_Const.ValOneofCase.Ival } position)
            {
                var number = position.Ival.Ival;

                if (targetCount >= 0 && (number < 1 || number > targetCount))
                {
                    sink.Add(SqlCheckMessages.OrdinalOutOfRange, position.Location, "GROUP BY", number);

                    return;
                }

                if (number >= 1 && number <= targetRanges.Count)
                {
                    var range = targetRanges[number - 1];

                    foreach (var use in scope.Uses.Skip(range.Start).Take(range.End - range.Start))
                    {
                        (range.Bare ? exact : covered).Add((use.Source, use.Column));
                    }
                }

                return;
            }

            if (item.ColumnRef is { Fields.Count: 1 } bare && bare.Fields[0].String != null)
            {
                var name = bare.Fields[0].String.Sval;

                if (!HasInputColumn(name, scope) && scope.OutputNames.Contains(name))
                {
                    skipGrouping = true;

                    return;
                }

                var before = scope.Uses.Count;
                Walk(item, context.In(SqlClause.GroupBy));

                foreach (var use in scope.Uses.Skip(before))
                {
                    exact.Add((use.Source, use.Column));
                }

                return;
            }

            var start = scope.Uses.Count;
            Walk(item, context.In(SqlClause.GroupBy));

            if (item.ColumnRef != null)
            {
                foreach (var use in scope.Uses.Skip(start))
                {
                    exact.Add((use.Source, use.Column));
                }

                return;
            }

            foreach (var use in scope.Uses.Skip(start))
            {
                covered.Add((use.Source, use.Column));
            }
        }

        private void CheckSortItem(SortBy sort, SqlContext context, SqlScope scope, int targetCount)
        {
            var node = sort.Node;

            if (node.AConst is { ValCase: A_Const.ValOneofCase.Ival } position)
            {
                if (targetCount >= 0 && (position.Ival.Ival < 1 || position.Ival.Ival > targetCount))
                {
                    sink.Add(SqlCheckMessages.OrdinalOutOfRange, position.Location, "ORDER BY", position.Ival.Ival);
                }

                return;
            }

            if (node.ColumnRef is { Fields.Count: 1 } bare && bare.Fields[0].String != null)
            {
                var name = bare.Fields[0].String.Sval;

                if (!HasInputColumn(name, scope) && scope.OutputNames.Contains(name))
                {
                    return;
                }
            }

            Walk(node, context);
        }

        private static bool HasInputColumn(string name, SqlScope scope)
        {
            for (var current = scope; current != null; current = current.Parent)
            {
                if (current.Sources.Any(s => s.Find(name) != null || s.Uncertain))
                {
                    return true;
                }
            }

            return false;
        }

        private void CheckGrouping(SelectStmt statement, SqlScope scope, HashSet<(SqlSource, string)> exact,
            HashSet<(SqlSource, string)> covered, bool skip)
        {
            var grouped = statement.GroupClause.Count > 0 || statement.HavingClause != null || scope.HasAggregate;

            if (!grouped || skip)
            {
                return;
            }

            var dependent = scope.Sources
                .Where(s => s.Relation is { PrimaryKey.Count: > 0 } && s.Relation.PrimaryKey.All(k => exact.Contains((s, k))))
                .ToHashSet();

            foreach (var use in scope.Uses.Where(u =>
                         u.Clause is SqlClause.Select or SqlClause.Having or SqlClause.OrderBy && !u.InAggregate))
            {
                if (exact.Contains((use.Source, use.Column)) || covered.Contains((use.Source, use.Column)) ||
                    dependent.Contains(use.Source))
                {
                    continue;
                }

                sink.Add(SqlCheckMessages.ColumnNotGrouped, use.Location, use.Name);
            }
        }

        private void CheckLimit(Node node, string clause, SqlContext context)
        {
            if (node == null)
            {
                return;
            }

            if (node.AConst is { } constant)
            {
                switch (constant.ValCase)
                {
                    case A_Const.ValOneofCase.Ival when constant.Ival.Ival < 0:
                        sink.Add(SqlCheckMessages.LimitNegative, constant.Location, clause);
                        break;
                    case A_Const.ValOneofCase.Fval when constant.Fval.Fval.StartsWith('-'):
                        sink.Add(SqlCheckMessages.LimitNegative, constant.Location, clause);
                        break;
                    case A_Const.ValOneofCase.Sval when SqlTypeRules.LiteralInvalid(constant.Sval.Sval, "int8"):
                        sink.Add(SqlCheckMessages.LimitType, constant.Location, clause, "text");
                        break;
                    case A_Const.ValOneofCase.Boolval:
                        sink.Add(SqlCheckMessages.LimitType, constant.Location, clause, "boolean");
                        break;
                }

                return;
            }

            var type = Walk(node, context);

            if (ContainsColumnReference(node))
            {
                sink.Add(SqlCheckMessages.LimitVariables, LocationOf(node), clause);
            }
            else if (type.IsKnownScalar && type.Category != SqlTypeCategory.Numeric)
            {
                sink.Add(SqlCheckMessages.LimitType, LocationOf(node), clause, SqlTypeRules.Name(type.Category));
            }
        }

        private static bool ContainsColumnReference(IMessage message)
        {
            if (message is Node { NodeCase: Node.NodeOneofCase.ColumnRef })
            {
                return true;
            }

            if (message is Node { NodeCase: Node.NodeOneofCase.SubLink } or Node
                {
                    NodeCase: Node.NodeOneofCase.SelectStmt
                })
            {
                return false;
            }

            foreach (var field in message.Descriptor.Fields.InFieldNumberOrder())
            {
                var value = field.Accessor.GetValue(message);

                switch (value)
                {
                    case IMessage child when ContainsColumnReference(child):
                        return true;
                    case IList list when list.OfType<IMessage>().Any(ContainsColumnReference):
                        return true;
                }
            }

            return false;
        }

        private static string OutputName(ResTarget target)
        {
            if (!string.IsNullOrEmpty(target.Name))
            {
                return target.Name;
            }

            return NameOf(target.Val);
        }

        private static string NameOf(Node value)
        {
            switch (value?.NodeCase)
            {
                case Node.NodeOneofCase.ColumnRef:
                    return value.ColumnRef.Fields.LastOrDefault()?.String?.Sval;
                case Node.NodeOneofCase.FuncCall:
                    return value.FuncCall.Funcname.LastOrDefault()?.String?.Sval;
                case Node.NodeOneofCase.TypeCast:
                    return NameOf(value.TypeCast.Arg);
                case Node.NodeOneofCase.CoalesceExpr:
                    return "coalesce";
                case Node.NodeOneofCase.CaseExpr:
                    return "case";
                case Node.NodeOneofCase.MinMaxExpr:
                    return value.MinMaxExpr.Op == MinMaxOp.IsGreatest ? "greatest" : "least";
                default:
                    return null;
            }
        }

        private void ExpandStar(ColumnRef reference, SqlScope scope, List<SqlColumnInfo> columns, ref bool partial)
        {
            var sources = scope.Sources.ToList();

            if (reference.Fields.Count == 2 && reference.Fields[0].String != null)
            {
                var alias = reference.Fields[0].String.Sval;
                var found = scope.Sources.FirstOrDefault(s => s.Alias == alias);

                if (found == null)
                {
                    ReportUnknownAlias(alias, reference.Location, scope);
                    partial = true;

                    return;
                }

                sources = [found];
            }

            foreach (var source in sources)
            {
                if (source.Uncertain)
                {
                    partial = true;
                }

                columns.AddRange(source.Columns);
            }
        }

        private void AddSource(Node item, SqlScope scope, List<Node> joinConditions)
        {
            switch (item.NodeCase)
            {
                case Node.NodeOneofCase.RangeVar:
                    AddRangeVar(item.RangeVar, scope);
                    break;
                case Node.NodeOneofCase.RangeSubselect:
                    AddSubselect(item.RangeSubselect, scope);
                    break;
                case Node.NodeOneofCase.JoinExpr:
                    AddJoin(item.JoinExpr, scope, joinConditions);
                    break;
                case Node.NodeOneofCase.RangeFunction:
                    AddFunction(item.RangeFunction, scope);
                    break;
                case Node.NodeOneofCase.RangeTableSample:
                    AddSource(item.RangeTableSample.Relation, scope, joinConditions);
                    break;
                default:
                    scope.Sources.Add(new SqlSource(string.Empty, [], true, false, null));
                    break;
            }
        }

        private void AddJoin(JoinExpr join, SqlScope scope, List<Node> joinConditions)
        {
            AddSource(join.Larg, scope, joinConditions);
            AddSource(join.Rarg, scope, joinConditions);

            if (join.IsNatural)
            {
                scope.Natural = true;
            }

            foreach (var name in join.UsingClause.Select(u => u.String?.Sval).Where(n => n != null))
            {
                scope.MergedNames.Add(name);
            }

            if (join.Quals != null)
            {
                joinConditions.Add(join.Quals);
            }

            if (join.Alias != null && !string.IsNullOrEmpty(join.Alias.Aliasname))
            {
                scope.Sources.Add(new SqlSource(join.Alias.Aliasname, [], true, false, null));
            }
        }

        private void AddFunction(RangeFunction function, SqlScope scope)
        {
            var context = new SqlContext(scope, SqlClause.From, false);
            var firstName = string.Empty;

            foreach (var entry in function.Functions)
            {
                var call = entry.List?.Items.FirstOrDefault();

                if (call == null)
                {
                    continue;
                }

                Walk(call, context);

                if (firstName.Length == 0 && call.FuncCall != null)
                {
                    firstName = call.FuncCall.Funcname.LastOrDefault()?.String?.Sval ?? string.Empty;
                }
            }

            var alias = function.Alias?.Aliasname is { Length: > 0 } named ? named : firstName;
            var names = function.Alias?.Colnames.Select(n => n.String?.Sval).Where(n => n != null).ToList() ?? [];

            if (names.Count > 0 && !function.Ordinality && function.Functions.Count == 1)
            {
                scope.Sources.Add(new SqlSource(alias,
                    names.Select(n => new SqlColumnInfo(n, SqlTypeCategory.Unknown, string.Empty)).ToList(), false,
                    false, null));

                return;
            }

            scope.Sources.Add(new SqlSource(alias, [], true, false, null));
        }

        private void AddSubselect(RangeSubselect subselect, SqlScope scope)
        {
            var derived = subselect.Subquery?.SelectStmt != null
                ? CheckQuery(subselect.Subquery.SelectStmt, subselect.Lateral ? scope : scope.Parent)
                : new SqlDerivedColumns([], true);

            var renames = subselect.Alias?.Colnames.Select(n => n.String?.Sval).Where(n => n != null).ToList() ?? [];
            derived = Rename(derived, renames);

            scope.Sources.Add(new SqlSource(subselect.Alias?.Aliasname ?? string.Empty, derived.Columns, false,
                derived.Partial, null));
        }

        private void AddRangeVar(RangeVar variable, SqlScope scope)
        {
            var alias = variable.Alias?.Aliasname is { Length: > 0 } named ? named : variable.Relname;
            var renames = variable.Alias?.Colnames.Select(n => n.String?.Sval).Where(n => n != null).ToList() ?? [];

            if (string.IsNullOrEmpty(variable.Schemaname))
            {
                var cte = scope.FindCommonTableExpression(variable.Relname);

                if (cte != null)
                {
                    var renamed = Rename(cte, renames);
                    scope.Sources.Add(new SqlSource(alias, renamed.Columns, false, renamed.Partial, null));

                    return;
                }
            }
            else if (variable.Schemaname != "public")
            {
                scope.Sources.Add(new SqlSource(alias, [], true, false, null));

                return;
            }

            var relation = snapshot.FindRelation(variable.Relname);

            if (options.AllowedTables != null)
            {
                if (!options.AllowedTables.TryGetValue(variable.Relname, out var template))
                {
                    var suggestion = SqlSuggestion.Nearest(variable.Relname, options.AllowedTables.Keys);

                    if (suggestion == null)
                    {
                        sink.Add(SqlCheckMessages.UnknownTable, variable.Location, variable.Relname);
                    }
                    else
                    {
                        sink.Add(SqlCheckMessages.UnknownTableSuggest, variable.Location, variable.Relname,
                            suggestion);
                    }

                    scope.Sources.Add(new SqlSource(alias, [], true, false, null));

                    return;
                }

                if (template != null)
                {
                    var derived = Rename(TemplateColumns(template), renames);
                    scope.Sources.Add(new SqlSource(alias, derived.Columns, false, derived.Partial,
                        relation));

                    return;
                }
            }

            if (relation == null)
            {
                if (options.AllowedTables == null)
                {
                    sink.Add(SqlCheckMessages.UnknownTable, variable.Location, variable.Relname);
                }

                scope.Sources.Add(new SqlSource(alias, [], true, false, null));

                return;
            }

            var columns = relation.Columns.ToList();

            for (var i = 0; i < renames.Count && i < columns.Count; i++)
            {
                columns[i] = columns[i] with { Name = renames[i] };
            }

            scope.Sources.Add(new SqlSource(alias, columns, false, false, relation));
        }

        private SqlDerivedColumns TemplateColumns(string template)
        {
            return snapshot.TemplateColumns.GetOrAdd(template, text =>
            {
                try
                {
                    var parsed = Parser.Parse(text.Replace("{{ESCAPED_USER}}", "'x'")
                        .Replace("{{CONVERSATION_ID}}", "0"));

                    if (parsed.Error != null || parsed.Value.Stmts.Count != 1 ||
                        parsed.Value.Stmts[0].Stmt?.SelectStmt == null)
                    {
                        return new SqlDerivedColumns([], true);
                    }

                    var quiet = new SqlSemanticWalker(snapshot, new SqlCheckOptions(), [],
                        new SqlFindingSink(string.Empty, null, true));

                    return quiet.CheckQuery(parsed.Value.Stmts[0].Stmt.SelectStmt, null);
                }
                catch (Exception e) when (e is InvalidProtocolBufferException or InvalidOperationException)
                {
                    return new SqlDerivedColumns([], true);
                }
            });
        }

        private void WalkWindow(WindowDef window, SqlContext context)
        {
            if (window == null)
            {
                return;
            }

            foreach (var node in window.PartitionClause.Concat(window.OrderClause))
            {
                Walk(node, context);
            }

            if (window.StartOffset != null)
            {
                Walk(window.StartOffset, context);
            }

            if (window.EndOffset != null)
            {
                Walk(window.EndOffset, context);
            }
        }

        private void CheckCondition(Node node, SqlContext context)
        {
            var type = Walk(node, context);

            if (type.IsKnownScalar && type.Category != SqlTypeCategory.Bool)
            {
                sink.Add(SqlCheckMessages.ConditionType, LocationOf(node), SqlTypeRules.Name(type.Category));
            }
        }

        private SqlExprType Walk(Node node, SqlContext context)
        {
            if (node == null)
            {
                return SqlExprType.Unknown;
            }

            switch (node.NodeCase)
            {
                case Node.NodeOneofCase.None:
                    return SqlExprType.Unknown;
                case Node.NodeOneofCase.ColumnRef:
                    return WalkColumnRef(node.ColumnRef, context);
                case Node.NodeOneofCase.AConst:
                    return WalkConstant(node.AConst);
                case Node.NodeOneofCase.AExpr:
                    return WalkOperator(node.AExpr, context);
                case Node.NodeOneofCase.BoolExpr:
                    foreach (var argument in node.BoolExpr.Args)
                    {
                        CheckCondition(argument, context);
                    }

                    return new SqlExprType(SqlTypeCategory.Bool, "bool");
                case Node.NodeOneofCase.FuncCall:
                    return WalkFunction(node.FuncCall, context);
                case Node.NodeOneofCase.TypeCast:
                    return WalkCast(node.TypeCast, context);
                case Node.NodeOneofCase.SubLink:
                    return WalkSubLink(node.SubLink, context);
                case Node.NodeOneofCase.ParamRef:
                    return WalkParameter(node.ParamRef);
                case Node.NodeOneofCase.NullTest:
                    Walk(node.NullTest.Arg, context);

                    return new SqlExprType(SqlTypeCategory.Bool, "bool");
                case Node.NodeOneofCase.CoalesceExpr:
                    return Unify(node.CoalesceExpr.Args.Select(a => Walk(a, context)).ToList(),
                        node.CoalesceExpr.Location);
                case Node.NodeOneofCase.MinMaxExpr:
                    return Unify(node.MinMaxExpr.Args.Select(a => Walk(a, context)).ToList(),
                        node.MinMaxExpr.Location);
                case Node.NodeOneofCase.CaseExpr:
                    return WalkCase(node.CaseExpr, context);
                case Node.NodeOneofCase.SqlvalueFunction:
                    return node.SqlvalueFunction.Op switch
                    {
                        SQLValueFunctionOp.SvfopCurrentRole or SQLValueFunctionOp.SvfopCurrentUser
                            or SQLValueFunctionOp.SvfopUser or SQLValueFunctionOp.SvfopSessionUser
                            or SQLValueFunctionOp.SvfopCurrentCatalog or SQLValueFunctionOp.SvfopCurrentSchema =>
                            new SqlExprType(SqlTypeCategory.Text, "text"),
                        _ => new SqlExprType(SqlTypeCategory.DateTime, "timestamptz")
                    };
                case Node.NodeOneofCase.SelectStmt:
                    CheckQuery(node.SelectStmt, context.Scope);

                    return SqlExprType.Unknown;
                case Node.NodeOneofCase.List:
                    foreach (var item in node.List.Items)
                    {
                        Walk(item, context);
                    }

                    return SqlExprType.Unknown;
                case Node.NodeOneofCase.SortBy:
                    Walk(node.SortBy.Node, context);

                    return SqlExprType.Unknown;
                case Node.NodeOneofCase.ResTarget:
                    return Walk(node.ResTarget.Val, context);
                default:
                    WalkChildren(ActiveMessage(node), context);

                    return SqlExprType.Unknown;
            }
        }

        private static IMessage ActiveMessage(Node node)
        {
            var field = nodeOneof.Accessor.GetCaseFieldDescriptor(node);

            return field?.Accessor.GetValue(node) as IMessage;
        }

        private void WalkChildren(IMessage message, SqlContext context)
        {
            if (message == null)
            {
                return;
            }

            foreach (var field in message.Descriptor.Fields.InFieldNumberOrder())
            {
                switch (field.Accessor.GetValue(message))
                {
                    case Node child:
                        Walk(child, context);
                        break;
                    case IMessage other:
                        WalkChildren(other, context);
                        break;
                    case IList list:
                        foreach (var entry in list)
                        {
                            if (entry is Node node)
                            {
                                Walk(node, context);
                            }
                            else if (entry is IMessage inner)
                            {
                                WalkChildren(inner, context);
                            }
                        }

                        break;
                }
            }
        }

        private static int LocationOf(Node node)
        {
            return node?.NodeCase switch
            {
                Node.NodeOneofCase.ColumnRef => node.ColumnRef.Location,
                Node.NodeOneofCase.AConst => node.AConst.Location,
                Node.NodeOneofCase.AExpr => node.AExpr.Location,
                Node.NodeOneofCase.FuncCall => node.FuncCall.Location,
                Node.NodeOneofCase.TypeCast => node.TypeCast.Location,
                Node.NodeOneofCase.ParamRef => node.ParamRef.Location,
                Node.NodeOneofCase.BoolExpr => node.BoolExpr.Location,
                Node.NodeOneofCase.SubLink => node.SubLink.Location,
                _ => -1
            };
        }

        private SqlExprType WalkConstant(A_Const constant)
        {
            return constant.ValCase switch
            {
                A_Const.ValOneofCase.Ival => new SqlExprType(SqlTypeCategory.Numeric, "int4"),
                A_Const.ValOneofCase.Fval => new SqlExprType(SqlTypeCategory.Numeric, "numeric"),
                A_Const.ValOneofCase.Boolval => new SqlExprType(SqlTypeCategory.Bool, "bool"),
                A_Const.ValOneofCase.Sval => new SqlExprType(SqlTypeCategory.UntypedString, null, null,
                    constant.Sval.Sval),
                A_Const.ValOneofCase.Bsval => new SqlExprType(SqlTypeCategory.Other),
                _ => SqlExprType.Unknown
            };
        }

        private SqlExprType WalkParameter(ParamRef reference)
        {
            if (reference.Number < 1 || reference.Number > parameterOrder.Count)
            {
                return SqlExprType.Unknown;
            }

            var name = parameterOrder[reference.Number - 1];

            if (options.ParameterNames != null && !options.ParameterNames.Contains(name))
            {
                sink.Add(SqlCheckMessages.ParameterUnknown, reference.Location, name);
            }

            return options.ParameterCategories != null && options.ParameterCategories.TryGetValue(name, out var category)
                ? new SqlExprType(category, null, name)
                : new SqlExprType(SqlTypeCategory.Unknown, null, name);
        }

        private SqlExprType WalkColumnRef(ColumnRef reference, SqlContext context)
        {
            var fields = reference.Fields;

            if (fields.Any(f => f.AStar != null))
            {
                if (fields.Count == 2 && fields[0].String != null)
                {
                    var alias = fields[0].String.Sval;

                    if (!AliasExists(alias, context.Scope))
                    {
                        ReportUnknownAlias(alias, reference.Location, context.Scope);
                    }
                }

                return SqlExprType.Unknown;
            }

            var names = fields.Select(f => f.String?.Sval).ToList();

            if (names.Any(n => n == null))
            {
                return SqlExprType.Unknown;
            }

            return names.Count switch
            {
                1 => ResolveBare(names[0], reference.Location, context),
                2 => ResolveQualified(names[0], names[1], reference.Location, context),
                3 when names[0] == "public" => ResolveQualified(names[1], names[2], reference.Location, context),
                _ => SqlExprType.Unknown
            };
        }

        private static bool AliasExists(string alias, SqlScope scope)
        {
            for (var current = scope; current != null; current = current.Parent)
            {
                if (current.Sources.Any(s => s.Alias == alias))
                {
                    return true;
                }
            }

            return false;
        }

        private void ReportUnknownAlias(string alias, int location, SqlScope scope)
        {
            var candidates = new List<string>();

            for (var current = scope; current != null; current = current.Parent)
            {
                candidates.AddRange(current.Sources.Select(s => s.Alias).Where(a => !string.IsNullOrEmpty(a)));
            }

            var suggestion = SqlSuggestion.Nearest(alias, candidates);

            if (suggestion == null)
            {
                sink.Add(SqlCheckMessages.UnknownAlias, location, alias);
            }
            else
            {
                sink.Add(SqlCheckMessages.UnknownAliasSuggest, location, alias, suggestion);
            }
        }

        private static SqlExprType TypeOf(SqlColumnInfo column)
        {
            return new SqlExprType(column.Category, string.IsNullOrEmpty(column.TypeName) ? null : column.TypeName);
        }

        private SqlExprType ResolveBare(string name, int location, SqlContext context)
        {
            for (var scope = context.Scope; scope != null; scope = scope.Parent)
            {
                var matches = scope.Sources.Where(s => s.Find(name) != null).ToList();

                if (matches.Count > 0)
                {
                    if (matches.Count > 1 && !scope.Natural && !scope.MergedNames.Contains(name))
                    {
                        sink.Add(SqlCheckMessages.AmbiguousColumn, location, name);
                    }

                    var source = matches[0];

                    if (scope == context.Scope)
                    {
                        scope.Uses.Add(new SqlColumnUse(location, name, source, name, context.Clause,
                            context.InAggregate));
                    }

                    return TypeOf(source.Find(name));
                }

                if (scope.Sources.Any(s => s.Uncertain))
                {
                    return SqlExprType.Unknown;
                }
            }

            if (AliasExists(name, context.Scope))
            {
                return SqlExprType.Unknown;
            }

            if (context.Clause is SqlClause.GroupBy or SqlClause.OrderBy && context.Scope.OutputNames.Contains(name))
            {
                return SqlExprType.Unknown;
            }

            var candidates = new List<string>();

            for (var current = context.Scope; current != null; current = current.Parent)
            {
                candidates.AddRange(current.Sources.SelectMany(s => s.Columns.Select(c => c.Name)));
            }

            ReportUnknownColumn(name, location, candidates);

            return SqlExprType.Unknown;
        }

        private void ReportUnknownColumn(string display, int location, IEnumerable<string> candidates, string bare = null)
        {
            var suggestion = SqlSuggestion.Nearest(bare ?? display, candidates.Distinct());

            if (suggestion == null)
            {
                sink.Add(SqlCheckMessages.UnknownColumn, location, display);
            }
            else
            {
                sink.Add(SqlCheckMessages.UnknownColumnSuggest, location, display, suggestion);
            }
        }

        private SqlExprType ResolveQualified(string alias, string column, int location, SqlContext context)
        {
            for (var scope = context.Scope; scope != null; scope = scope.Parent)
            {
                var source = scope.Sources.FirstOrDefault(s => s.Alias == alias);

                if (source == null)
                {
                    continue;
                }

                var found = source.Find(column);

                if (found != null)
                {
                    if (scope == context.Scope)
                    {
                        scope.Uses.Add(new SqlColumnUse(location, alias + "." + column, source, column,
                            context.Clause, context.InAggregate));
                    }

                    return TypeOf(found);
                }

                if (source.Uncertain)
                {
                    return SqlExprType.Unknown;
                }

                ReportUnknownColumn(alias + "." + column, location, source.Columns.Select(c => c.Name), column);

                return SqlExprType.Unknown;
            }

            for (var scope = context.Scope; scope != null; scope = scope.Parent)
            {
                if (scope.Sources.Any(s => s.Uncertain || s.Find(alias) is { Category: not SqlTypeCategory.Numeric
                        and not SqlTypeCategory.Text and not SqlTypeCategory.Bool and not SqlTypeCategory.DateTime
                        and not SqlTypeCategory.Uuid }))
                {
                    return SqlExprType.Unknown;
                }
            }

            ReportUnknownAlias(alias, location, context.Scope);

            return SqlExprType.Unknown;
        }

        private SqlExprType WalkOperator(A_Expr expression, SqlContext context)
        {
            var op = expression.Name.LastOrDefault()?.String?.Sval ?? string.Empty;
            var boolean = new SqlExprType(SqlTypeCategory.Bool, "bool");

            switch (expression.Kind)
            {
                case A_Expr_Kind.AexprOp:
                {
                    var left = expression.Lexpr != null ? Walk(expression.Lexpr, context) : SqlExprType.Unknown;
                    var right = Walk(expression.Rexpr, context);

                    if (comparisonOperators.Contains(op))
                    {
                        Compare(left, right, expression.Location);

                        return boolean;
                    }

                    if (arithmeticOperators.Contains(op))
                    {
                        return Arithmetic(op, left, right, expression.Location);
                    }

                    return SqlExprType.Unknown;
                }
                case A_Expr_Kind.AexprDistinct:
                case A_Expr_Kind.AexprNotDistinct:
                {
                    Compare(Walk(expression.Lexpr, context), Walk(expression.Rexpr, context), expression.Location);

                    return boolean;
                }
                case A_Expr_Kind.AexprNullif:
                {
                    var left = Walk(expression.Lexpr, context);
                    Compare(left, Walk(expression.Rexpr, context), expression.Location);

                    return left;
                }
                case A_Expr_Kind.AexprIn:
                case A_Expr_Kind.AexprBetween:
                case A_Expr_Kind.AexprNotBetween:
                case A_Expr_Kind.AexprBetweenSym:
                case A_Expr_Kind.AexprNotBetweenSym:
                {
                    var left = Walk(expression.Lexpr, context);

                    if (expression.Rexpr?.List != null)
                    {
                        foreach (var item in expression.Rexpr.List.Items)
                        {
                            Compare(left, Walk(item, context), expression.Location);
                        }
                    }
                    else
                    {
                        Walk(expression.Rexpr, context);
                    }

                    return boolean;
                }
                case A_Expr_Kind.AexprLike:
                case A_Expr_Kind.AexprIlike:
                case A_Expr_Kind.AexprSimilar:
                {
                    var left = Walk(expression.Lexpr, context);
                    Walk(expression.Rexpr, context);

                    if (left.IsKnownScalar && left.Category != SqlTypeCategory.Text)
                    {
                        sink.Add(SqlCheckMessages.OperatorType, expression.Location, "LIKE",
                            SqlTypeRules.Name(left.Category));
                    }

                    return boolean;
                }
                default:
                    if (expression.Lexpr != null)
                    {
                        Walk(expression.Lexpr, context);
                    }

                    Walk(expression.Rexpr, context);

                    return boolean;
            }
        }

        private void Compare(SqlExprType left, SqlExprType right, int location)
        {
            if (left.Category == SqlTypeCategory.UntypedString || right.Category == SqlTypeCategory.UntypedString)
            {
                var literal = left.Category == SqlTypeCategory.UntypedString ? left : right;
                var other = left.Category == SqlTypeCategory.UntypedString ? right : left;

                CheckLiteral(literal, other, location);

                return;
            }

            if (!SqlTypeRules.Clash(left, right))
            {
                return;
            }

            if (left.ParameterName != null || right.ParameterName != null)
            {
                var parameter = left.ParameterName != null ? left : right;
                var column = left.ParameterName != null ? right : left;

                sink.Add(SqlCheckMessages.ParameterType, location, parameter.ParameterName,
                    SqlTypeRules.Name(parameter.Category), SqlTypeRules.Name(column.Category));

                return;
            }

            sink.Add(SqlCheckMessages.TypeMismatch, location, SqlTypeRules.Name(left.Category),
                SqlTypeRules.Name(right.Category));
        }

        private void CheckLiteral(SqlExprType literal, SqlExprType other, int location)
        {
            if (literal.Literal != null && other.IsKnownScalar && other.Category != SqlTypeCategory.Text &&
                !string.IsNullOrEmpty(other.TypeName) && SqlTypeRules.LiteralInvalid(literal.Literal, other.TypeName))
            {
                sink.Add(SqlCheckMessages.LiteralInvalid, location, literal.Literal,
                    SqlTypeRules.DisplayName(other.TypeName));
            }
        }

        private SqlExprType Arithmetic(string op, SqlExprType left, SqlExprType right, int location)
        {
            foreach (var side in new[] { left, right })
            {
                if (side.Category is SqlTypeCategory.Text or SqlTypeCategory.Bool or SqlTypeCategory.Uuid)
                {
                    sink.Add(SqlCheckMessages.OperatorType, location, op, SqlTypeRules.Name(side.Category));

                    return SqlExprType.Unknown;
                }
            }

            if (left.Category == SqlTypeCategory.Numeric && right.Category == SqlTypeCategory.UntypedString)
            {
                CheckLiteral(right, left, location);
            }
            else if (right.Category == SqlTypeCategory.Numeric && left.Category == SqlTypeCategory.UntypedString)
            {
                CheckLiteral(left, right, location);
            }

            return left.Category == SqlTypeCategory.Numeric &&
                   right.Category is SqlTypeCategory.Numeric or SqlTypeCategory.UntypedString
                ? new SqlExprType(SqlTypeCategory.Numeric, left.TypeName)
                : SqlExprType.Unknown;
        }

        private SqlExprType Unify(IReadOnlyList<SqlExprType> types, int location)
        {
            var first = types.FirstOrDefault(t => t.IsKnownScalar);

            foreach (var type in types.Where(t => t.IsKnownScalar && t.Category != first.Category))
            {
                sink.Add(SqlCheckMessages.TypesUnmatched, location, SqlTypeRules.Name(first.Category),
                    SqlTypeRules.Name(type.Category));

                return SqlExprType.Unknown;
            }

            return first;
        }

        private SqlExprType WalkCase(CaseExpr expression, SqlContext context)
        {
            var subject = expression.Arg != null ? Walk(expression.Arg, context) : SqlExprType.Unknown;
            var results = new List<SqlExprType>();

            foreach (var clause in expression.Args)
            {
                var when = clause.CaseWhen;

                if (expression.Arg != null)
                {
                    Compare(subject, Walk(when.Expr, context), expression.Location);
                }
                else
                {
                    CheckCondition(when.Expr, context);
                }

                results.Add(Walk(when.Result, context));
            }

            if (expression.Defresult != null)
            {
                results.Add(Walk(expression.Defresult, context));
            }

            var known = results.Where(r => r.IsKnownScalar).Select(r => r.Category).Distinct().ToList();

            return known.Count == 1 && results.All(r => r.IsKnownScalar || r.Category == SqlTypeCategory.Unknown)
                ? results.First(r => r.IsKnownScalar)
                : SqlExprType.Unknown;
        }

        private SqlExprType WalkSubLink(SubLink link, SqlContext context)
        {
            if (link.Testexpr != null)
            {
                Walk(link.Testexpr, context);
            }

            var derived = link.Subselect?.SelectStmt != null
                ? CheckQuery(link.Subselect.SelectStmt, context.Scope)
                : new SqlDerivedColumns([], true);

            switch (link.SubLinkType)
            {
                case SubLinkType.ExistsSublink:
                case SubLinkType.AnySublink:
                case SubLinkType.AllSublink:
                    return new SqlExprType(SqlTypeCategory.Bool, "bool");
                case SubLinkType.ArraySublink:
                    return new SqlExprType(SqlTypeCategory.Array);
                case SubLinkType.ExprSublink:
                    if (!derived.Partial && derived.Columns.Count != 1)
                    {
                        sink.Add(SqlCheckMessages.SubqueryColumns, link.Location);

                        return SqlExprType.Unknown;
                    }

                    return derived.Columns.Count == 1 ? TypeOf(derived.Columns[0]) : SqlExprType.Unknown;
                default:
                    return SqlExprType.Unknown;
            }
        }

        private SqlExprType WalkCast(TypeCast cast, SqlContext context)
        {
            var argument = Walk(cast.Arg, context);
            var names = cast.TypeName.Names.Select(n => n.String?.Sval).Where(n => n != null).ToList();

            if (names.Count == 0)
            {
                return SqlExprType.Unknown;
            }

            var typeName = names[^1];

            if (names.Count == 1 && !snapshot.HasType(typeName))
            {
                sink.Add(SqlCheckMessages.UnknownType, cast.TypeName.Location, typeName);

                return SqlExprType.Unknown;
            }

            if (cast.TypeName.ArrayBounds.Count > 0)
            {
                return new SqlExprType(SqlTypeCategory.Array);
            }

            var category = SqlTypeRules.CategoryOfTypeName(typeName);

            if (argument.Category == SqlTypeCategory.UntypedString && argument.Literal != null &&
                category != SqlTypeCategory.Text && SqlTypeRules.LiteralInvalid(argument.Literal, typeName))
            {
                sink.Add(SqlCheckMessages.LiteralInvalid, LocationOf(cast.Arg), argument.Literal,
                    SqlTypeRules.DisplayName(typeName));
            }

            return new SqlExprType(category, typeName);
        }

        private SqlExprType WalkFunction(FuncCall call, SqlContext context)
        {
            var parts = call.Funcname.Select(n => n.String?.Sval).Where(n => n != null).ToList();
            var name = parts.LastOrDefault() ?? string.Empty;
            var qualified = parts.Count > 1;
            var windowed = call.Over != null;

            SqlFunctionInfo info = null;

            if (!qualified && snapshot.FindFunction(name) == null && snapshot.HasType(name))
            {
                foreach (var argument in call.Args)
                {
                    Walk(argument, context);
                }

                return new SqlExprType(SqlTypeRules.CategoryOfTypeName(name), name);
            }

            if (!qualified || parts[0] is "pg_catalog" or "public")
            {
                info = snapshot.FindFunction(name);

                if (info == null && (!qualified || parts[0] == "pg_catalog" || parts[0] == "public"))
                {
                    var suggestion = SqlSuggestion.Nearest(name, snapshot.FunctionNames);

                    if (suggestion == null)
                    {
                        sink.Add(SqlCheckMessages.UnknownFunction, call.Location, name);
                    }
                    else
                    {
                        sink.Add(SqlCheckMessages.UnknownFunctionSuggest, call.Location, name, suggestion);
                    }
                }
            }

            var aggregate = info is { IsAggregate: true } && !windowed;

            if (info != null)
            {
                if (windowed && !info.IsWindowCapable)
                {
                    sink.Add(SqlCheckMessages.OverNotWindow, call.Location, name);
                }
                else if (windowed && context.Clause is SqlClause.Where or SqlClause.Join or SqlClause.GroupBy
                             or SqlClause.Having)
                {
                    sink.Add(SqlCheckMessages.WindowNotAllowed, call.Location, name);
                }
                else if (!windowed && info.IsWindow)
                {
                    sink.Add(SqlCheckMessages.WindowNeedsOver, call.Location, name);
                }
                else if (aggregate)
                {
                    ReportAggregateMisplaced(name, call.Location, context);
                    context.Scope.HasAggregate = true;
                }

                var count = call.Args.Count + (call.AggWithinGroup ? call.AggOrder.Count : 0);

                if (!call.FuncVariadic && !info.AcceptsArguments(count))
                {
                    sink.Add(SqlCheckMessages.FunctionArity, call.Location, name, count, info.ExpectedArguments());
                }
            }

            var inner = aggregate ? context.InsideAggregate() : context;
            var types = call.Args.Select(a => Walk(a.NamedArgExpr?.Arg ?? a, inner)).ToList();

            foreach (var order in call.AggOrder)
            {
                Walk(order, inner);
            }

            if (call.AggFilter != null)
            {
                Walk(call.AggFilter, inner);
            }

            if (windowed)
            {
                WalkWindow(call.Over, context);
            }

            CheckArgumentTypes(name, types, call.Location);

            return ReturnType(name, types);
        }

        private void ReportAggregateMisplaced(string name, int location, SqlContext context)
        {
            if (context.InAggregate)
            {
                sink.Add(SqlCheckMessages.AggregateNested, location, name);

                return;
            }

            switch (context.Clause)
            {
                case SqlClause.Where:
                    sink.Add(SqlCheckMessages.AggregateInWhere, location, name);
                    break;
                case SqlClause.Join:
                    sink.Add(SqlCheckMessages.AggregateInJoin, location, name);
                    break;
                case SqlClause.GroupBy:
                    sink.Add(SqlCheckMessages.AggregateInGroupBy, location, name);
                    break;
            }
        }

        private void CheckArgumentTypes(string name, IReadOnlyList<SqlExprType> types, int location)
        {
            if (types.Count == 0)
            {
                return;
            }

            var first = types[0];
            var bad = first.IsKnownScalar && first.Category != SqlTypeCategory.Text;

            if (numericAggregates.Contains(name) && first.Category is SqlTypeCategory.Text or SqlTypeCategory.Bool
                    or SqlTypeCategory.Uuid or SqlTypeCategory.DateTime)
            {
                sink.Add(SqlCheckMessages.AggregateArgumentType, location, name, SqlTypeRules.Name(first.Category));
            }
            else if (textOnlyFunctions.Contains(name) && bad)
            {
                sink.Add(SqlCheckMessages.FunctionArgumentType, location, name, SqlTypeRules.Name(first.Category));
            }
            else if (numericOnlyFunctions.Contains(name) && first.IsKnownScalar &&
                     first.Category != SqlTypeCategory.Numeric)
            {
                sink.Add(SqlCheckMessages.FunctionArgumentType, location, name, SqlTypeRules.Name(first.Category));
            }
            else if (timeFieldFunctions.Contains(name) && types.Count > 1 && types[1].Category is
                         SqlTypeCategory.Text or SqlTypeCategory.Numeric or SqlTypeCategory.Bool
                         or SqlTypeCategory.Uuid)
            {
                sink.Add(SqlCheckMessages.FunctionArgumentType, location, name,
                    SqlTypeRules.Name(types[1].Category));
            }
        }

        private static SqlExprType ReturnType(string name, IReadOnlyList<SqlExprType> types)
        {
            switch (name)
            {
                case "lower" or "upper" or "initcap" or "ltrim" or "rtrim" or "btrim" or "replace" or "concat"
                    or "left" or "right" or "to_char" or "md5" or "split_part" or "lpad" or "rpad" or "format"
                    or "string_agg" or "substr" or "substring":
                    return new SqlExprType(SqlTypeCategory.Text, "text");
                case "count" or "length" or "char_length" or "extract" or "date_part":
                    return new SqlExprType(SqlTypeCategory.Numeric, "int8");
                case "sum" or "avg" or "abs" or "round" or "ceil" or "ceiling" or "floor" or "sqrt" or "power"
                    or "trunc":
                    return new SqlExprType(SqlTypeCategory.Numeric, "numeric");
                case "now" or "clock_timestamp" or "date_trunc":
                    return new SqlExprType(SqlTypeCategory.DateTime, "timestamptz");
                case "min" or "max":
                    return types.Count > 0 ? types[0] with { ParameterName = null } : SqlExprType.Unknown;
                case "array_agg":
                    return new SqlExprType(SqlTypeCategory.Array);
                case "to_json" or "to_jsonb" or "jsonb_build_object" or "json_build_object" or "jsonb_agg"
                    or "json_agg":
                    return new SqlExprType(SqlTypeCategory.Json);
                default:
                    return SqlExprType.Unknown;
            }
        }
    }
}
