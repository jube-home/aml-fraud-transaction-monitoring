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

namespace Jube.Engine.EntityAnalysisModelInvoke.Extraction
{
    using System;
    using System.Buffers;
    using System.IO;
    using System.Text;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public sealed class FastPathRequestReader
    {
        private const int StackDepth = 72;

        private readonly string[] values;
        private readonly object[] tokens;
        private readonly Outcome[] outcomes;
        private readonly FastPathPlan plan;

        private FastPathRequestReader(FastPathPlan plan)
        {
            this.plan = plan;
            values = new string[plan.Count];
            tokens = new object[plan.Count];
            outcomes = new Outcome[plan.Count];
        }

        public static FastPathRequestReader TryRead(byte[] bytes, FastPathPlan plan)
        {
            var reader = new FastPathRequestReader(plan);
            var stack = ArrayPool<Frame>.Shared.Rent(StackDepth);

            try
            {
                using var json = new JsonTextReader(new StreamReader(new MemoryStream(bytes), Encoding.UTF8))
                {
                    DateParseHandling = DateParseHandling.None
                };

                return reader.Walk(json, stack) ? reader : null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException and not OutOfMemoryException)
            {
                return null;
            }
            finally
            {
                Array.Clear(stack, 0, stack.Length);
                ArrayPool<Frame>.Shared.Return(stack);
            }
        }

        public bool TrySelect(string xPath, out string value)
        {
            value = null;

            if (!plan.TryGetIdentifier(xPath, out var identifier))
            {
                return false;
            }

            switch (outcomes[identifier])
            {
                case Outcome.Missing:
                    return true;
                case Outcome.Null:
                    value = string.Empty;

                    return true;
                case Outcome.String:
                    value = values[identifier];

                    return true;
                case Outcome.Integer:
                case Outcome.Float:
                case Outcome.Boolean:
                    value = new JValue(tokens[identifier]).ToString();

                    return true;
                default:
                    return false;
            }
        }

        public bool TryGetInt32(string xPath, out int value)
        {
            value = 0;

            if (!plan.TryGetIdentifier(xPath, out var identifier) || outcomes[identifier] != Outcome.Integer ||
                tokens[identifier] is not long integer || integer < int.MinValue || integer > int.MaxValue)
            {
                return false;
            }

            value = (int)integer;

            return true;
        }

        public bool TryGetDouble(string xPath, out double value)
        {
            value = 0;

            if (!plan.TryGetIdentifier(xPath, out var identifier))
            {
                return false;
            }

            switch (tokens[identifier])
            {
                case long integer when outcomes[identifier] == Outcome.Integer:
                    value = integer;

                    return true;
                case double real when outcomes[identifier] == Outcome.Float && double.IsFinite(real):
                    value = real;

                    return true;
                default:
                    return false;
            }
        }

        public bool TryGetBoolean(string xPath, out bool value)
        {
            value = false;

            if (!plan.TryGetIdentifier(xPath, out var identifier) || outcomes[identifier] != Outcome.Boolean ||
                tokens[identifier] is not bool flag)
            {
                return false;
            }

            value = flag;

            return true;
        }

        public bool TrySelectString(string xPath, out string value)
        {
            value = null;

            if (!plan.TryGetIdentifier(xPath, out var identifier))
            {
                return false;
            }

            switch (outcomes[identifier])
            {
                case Outcome.Missing:
                case Outcome.Null:
                    return true;
                case Outcome.String:
                    value = values[identifier];

                    return true;
                default:
                    return false;
            }
        }

        private bool Walk(JsonTextReader json, Frame[] stack)
        {
            var depth = -1;

            while (json.Read())
            {
                switch (json.TokenType)
                {
                    case JsonToken.Comment:
                        continue;
                    case JsonToken.StartObject:
                    case JsonToken.StartArray:
                        if (depth < 0 && json.TokenType != JsonToken.StartObject)
                        {
                            return false;
                        }

                        var node = ResolveNode(stack, depth, plan);

                        MarkDeferred(node);

                        depth++;
                        stack[depth] = new Frame(node, json.TokenType == JsonToken.StartArray);

                        break;
                    case JsonToken.PropertyName:
                        if (!SelectChild(ref stack[depth], (string)json.Value))
                        {
                            return false;
                        }

                        break;
                    case JsonToken.EndObject:
                    case JsonToken.EndArray:
                        depth--;

                        break;
                    default:
                        if (depth < 0)
                        {
                            return false;
                        }

                        Record(ResolveNode(stack, depth, plan), json);

                        break;
                }

                if (depth < 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static FastPathPlan.PlanNode ResolveNode(Frame[] stack, int depth, FastPathPlan plan)
        {
            if (depth < 0)
            {
                return plan.Root;
            }

            ref var frame = ref stack[depth];

            if (!frame.IsArray)
            {
                return frame.Child;
            }

            frame.Index++;

            return frame.Node != null && frame.Node.TryGetIndexed(frame.Index, out var indexed) ? indexed : null;
        }

        private static bool SelectChild(ref Frame frame, string name)
        {
            frame.Child = null;

            if (frame.Node == null || !frame.Node.TryGetNamed(name, out var child))
            {
                return true;
            }

            var bit = 1UL << child.Ordinal;

            if ((frame.Seen & bit) != 0)
            {
                return false;
            }

            frame.Seen |= bit;
            frame.Child = child;

            return true;
        }

        private void MarkDeferred(FastPathPlan.PlanNode node)
        {
            if (node == null)
            {
                return;
            }

            foreach (var identifier in node.Terminals)
            {
                outcomes[identifier] = Outcome.Deferred;
            }
        }

        private void Record(FastPathPlan.PlanNode node, JsonTextReader json)
        {
            if (node == null)
            {
                return;
            }

            Outcome outcome;
            string value = null;
            object token = null;

            switch (json.TokenType)
            {
                case JsonToken.String:
                    outcome = Outcome.String;
                    value = (string)json.Value;

                    break;
                case JsonToken.Integer:
                    outcome = Outcome.Integer;
                    token = json.Value;

                    break;
                case JsonToken.Float:
                    outcome = Outcome.Float;
                    token = json.Value;

                    break;
                case JsonToken.Boolean:
                    outcome = Outcome.Boolean;
                    token = json.Value;

                    break;
                case JsonToken.Null:
                    outcome = Outcome.Null;

                    break;
                default:
                    outcome = Outcome.Deferred;

                    break;
            }

            foreach (var identifier in node.Terminals)
            {
                outcomes[identifier] = outcome;
                values[identifier] = value;
                tokens[identifier] = token;
            }
        }

        private enum Outcome : byte
        {
            Missing = 0,
            String,
            Null,
            Integer,
            Float,
            Boolean,
            Deferred
        }

        private struct Frame(FastPathPlan.PlanNode node, bool isArray)
        {
            internal FastPathPlan.PlanNode Node = node;
            internal FastPathPlan.PlanNode Child;
            internal readonly bool IsArray = isArray;
            internal int Index = -1;
            internal ulong Seen;
        }
    }
}
