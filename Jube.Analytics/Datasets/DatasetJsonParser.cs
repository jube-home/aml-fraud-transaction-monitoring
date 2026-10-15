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

using System.Text;
using System.Text.Json;
using Jube.Analytics.Datasets.Exceptions;
using Jube.Analytics.Datasets.Models;

namespace Jube.Analytics.Datasets
{
    public static class DatasetJsonParser
    {
        public const int MaxDepth = 8;
        private const int BufferSize = 32768;
        private const int FlushRows = 256;
        private const string MaxExactIntegerText = "9007199254740992";

        public static async Task<DatasetParseReport> ParseAsync(Stream input, DatasetLimits limits,
            Func<DatasetRow, CancellationToken, ValueTask>? sink, CancellationToken token = default)
        {
            var report = new DatasetParseReport();
            var counting = new DatasetCountingStream(input, limits.MaxBytes);
            var scanner = new Scanner(limits, report);
            var pending = new List<DatasetRow>(FlushRows);

            try
            {
                var buffer = new byte[BufferSize];
                var filled = 0;
                var eof = false;

                var readerState = new JsonReaderState(new JsonReaderOptions
                {
                    MaxDepth = MaxDepth, CommentHandling = JsonCommentHandling.Disallow, AllowTrailingCommas = false
                });

                var sniffed = false;

                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    if (!eof && filled < buffer.Length)
                    {
                        var read = await counting.ReadAsync(buffer.AsMemory(filled), token).ConfigureAwait(false);

                        if (read == 0)
                        {
                            eof = true;
                        }

                        filled += read;
                    }

                    if (!sniffed)
                    {
                        if (filled < 8 && !eof)
                        {
                            continue;
                        }

                        sniffed = true;
                        var rejected = Sniff(buffer, filled);

                        if (rejected != null)
                        {
                            report.Errors.Add(rejected);

                            return report;
                        }
                    }

                    if (scanner.Done)
                    {
                        if (!OnlyWhitespace(buffer, 0, filled))
                        {
                            report.Errors.Add(new DatasetParseError("TrailingContent",
                                "There is content after the end of the JSON document."));

                            return report;
                        }

                        filled = 0;

                        if (eof)
                        {
                            break;
                        }

                        continue;
                    }

                    var consumedTotal = 0;

                    do
                    {
                        pending.Clear();

                        var consumed = Scan(buffer, consumedTotal, filled - consumedTotal, eof, ref readerState,
                            scanner,
                            pending);

                        consumedTotal += consumed;

                        foreach (var row in pending)
                        {
                            if (sink != null)
                            {
                                await sink(row, token).ConfigureAwait(false);
                            }
                        }

                        if (scanner.Failed)
                        {
                            return report;
                        }
                    } while (scanner.StoppedForFlush && !scanner.Done);

                    if (scanner.Done)
                    {
                        if (!OnlyWhitespace(buffer, consumedTotal, filled - consumedTotal))
                        {
                            report.Errors.Add(new DatasetParseError("TrailingContent",
                                "There is content after the end of the JSON document."));

                            return report;
                        }

                        filled = 0;

                        if (eof)
                        {
                            break;
                        }

                        continue;
                    }

                    if (eof)
                    {
                        report.Errors.Add(new DatasetParseError("Truncated",
                            "The JSON document ends before it is complete."));

                        return report;
                    }

                    if (consumedTotal == 0 && filled == buffer.Length)
                    {
                        report.Errors.Add(new DatasetParseError("TokenTooLong",
                            "A single JSON value or name is larger than the permitted size."));

                        return report;
                    }

                    if (consumedTotal > 0)
                    {
                        Buffer.BlockCopy(buffer, consumedTotal, buffer, 0, filled - consumedTotal);
                        filled -= consumedTotal;
                    }
                }

                if (!scanner.Done)
                {
                    report.Errors.Add(new DatasetParseError("Empty", "The content is empty."));

                    return report;
                }

                scanner.Finish();
                report.Success = report.Errors.Count == 0;

                return report;
            }
            catch (DatasetSizeLimitException)
            {
                report.Errors.Add(new DatasetParseError("TooLarge",
                    $"The content exceeds the permitted size of {limits.MaxBytes} bytes."));

                return report;
            }
            finally
            {
                report.BytesRead = counting.BytesRead;
            }
        }

        public static bool IsLargeIntegerLiteral(ReadOnlySpan<byte> literal)
        {
            var digits = literal.Length > 0 && literal[0] == (byte)'-' ? literal[1..] : literal;

            foreach (var b in digits)
            {
                if (b is (byte)'.' or (byte)'e' or (byte)'E')
                {
                    return false;
                }
            }

            if (digits.Length != MaxExactIntegerText.Length)
            {
                return digits.Length > MaxExactIntegerText.Length;
            }

            return string.CompareOrdinal(Encoding.ASCII.GetString(digits), MaxExactIntegerText) > 0;
        }

        private static DatasetParseError? Sniff(byte[] head, int length)
        {
            if (length == 0)
            {
                return new DatasetParseError("Empty", "The content is empty.");
            }

            var binary = DatasetContentSniffer.DetectBinary(head, length);

            if (binary != null)
            {
                return new DatasetParseError("NotJson",
                    $"The content is {binary}, not JSON. Compressed and binary uploads are not accepted.");
            }

            if (DatasetContentSniffer.HasUtf8Bom(head, length))
            {
                return new DatasetParseError("ByteOrderMark",
                    "A UTF-8 byte order mark is not permitted; supply JSON without one.");
            }

            for (var i = 0; i < length; i++)
            {
                if (head[i] is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
                {
                    continue;
                }

                if (head[i] is (byte)'{' or (byte)'[')
                {
                    return null;
                }

                return new DatasetParseError("NotJson",
                    "The content is not JSON; the top level must be an array of objects or an object with columns and rows.");
            }

            return length >= 8 ? new DatasetParseError("Empty", "The content is empty.") : null;
        }

        private static bool OnlyWhitespace(byte[] buffer, int offset, int count)
        {
            for (var i = offset; i < offset + count; i++)
            {
                if (buffer[i] is not ((byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n'))
                {
                    return false;
                }
            }

            return true;
        }

        private static int Scan(byte[] buffer, int offset, int count, bool final, ref JsonReaderState state,
            Scanner scanner, List<DatasetRow> pending)
        {
            var reader = new Utf8JsonReader(buffer.AsSpan(offset, count), final, state);

            try
            {
                scanner.Run(ref reader, pending);
            }
            catch (DatasetJsonException ex)
            {
                scanner.Fail(ex.Code, ex.Message);
            }
            catch (JsonException ex)
            {
                var code = ex.Message.Contains("depth", StringComparison.OrdinalIgnoreCase) ? "TooDeep" : "InvalidJson";

                var message = code == "TooDeep"
                    ? $"The JSON is nested deeper than the permitted {MaxDepth} levels."
                    : $"The content is not valid JSON near line {(ex.LineNumber ?? 0) + 1}, position {(ex.BytePositionInLine ?? 0) + 1}.";
                scanner.Fail(code, message);
            }
            catch (InvalidOperationException)
            {
                scanner.Fail("InvalidText",
                    "A name or value is not valid Unicode text (invalid UTF-8 or a lone surrogate).");
            }

            state = reader.CurrentState;

            return (int)reader.BytesConsumed;
        }

        private enum State
        {
            Start,
            TopArray,
            RowObjectKey,
            RowObjectValue,
            TopObjectKey,
            AfterColumnsName,
            ColumnsArray,
            AfterRowsName,
            RowsArray,
            RowArrayValues,
            Done
        }

        private sealed class Scanner(DatasetLimits limits, DatasetParseReport report)
        {
            private readonly List<DatasetColumnAccumulator> accumulators = [];
            private readonly List<string> columnHeaders = [];
            private readonly List<DatasetColumnDefinition> columns = [];
            private readonly Dictionary<string, int> keyIndex = new(StringComparer.Ordinal);
            private readonly List<DatasetCell> rowCells = [];
            private readonly HashSet<string> seenHeaders = new(StringComparer.OrdinalIgnoreCase);
            private readonly HashSet<string> seenKeys = new(StringComparer.Ordinal);
            private readonly HashSet<string> takenNames = new(StringComparer.OrdinalIgnoreCase);
            private int cellIndex;
            private bool columnsSeen;
            private string currentKey = string.Empty;
            private List<DatasetRow>? output;
            private long rowNumber;
            private bool rowsSeen;
            private State state = State.Start;
            public bool Done => state == State.Done && !Failed;
            public bool Failed { get; private set; }
            public bool StoppedForFlush { get; private set; }

            public void Fail(string code, string message)
            {
                if (Failed)
                {
                    return;
                }

                Failed = true;
                report.Errors.Add(new DatasetParseError(code, message, rowNumber > 0 ? rowNumber : null));
            }

            public void Finish()
            {
                if (rowNumber == 0)
                {
                    report.Errors.Add(new DatasetParseError("NoRows", "The JSON has no data rows."));

                    return;
                }

                if (columns.Count == 0)
                {
                    report.Errors.Add(new DatasetParseError("NoColumns", "The JSON has no columns."));

                    return;
                }

                for (var i = 0; i < columns.Count; i++)
                {
                    var a = accumulators[i];
                    a.Missing = rowNumber - a.Numbers - a.Texts;
                }

                DatasetProfile.Finish(columns, accumulators.ToArray(), report);
            }

            public void Run(ref Utf8JsonReader reader, List<DatasetRow> pending)
            {
                output = pending;
                StoppedForFlush = false;

                while (reader.Read())
                {
                    Step(ref reader);

                    if (state == State.Done)
                    {
                        return;
                    }

                    if (pending.Count >= FlushRows)
                    {
                        StoppedForFlush = true;

                        return;
                    }
                }
            }

            private void Step(ref Utf8JsonReader reader)
            {
                var type = reader.TokenType;

                switch (state)
                {
                    case State.Start:
                        if (type == JsonTokenType.StartArray)
                        {
                            state = State.TopArray;
                        }
                        else if (type == JsonTokenType.StartObject)
                        {
                            state = State.TopObjectKey;
                        }
                        else
                        {
                            throw Shape();
                        }

                        break;

                    case State.TopArray:
                        if (type == JsonTokenType.EndArray)
                        {
                            state = State.Done;
                        }
                        else if (type == JsonTokenType.StartObject)
                        {
                            BeginRow();
                            state = State.RowObjectKey;
                        }
                        else
                        {
                            throw new DatasetJsonException("RowNotObject",
                                $"Row {rowNumber + 1} at $[{rowNumber}] is not an object; each element must be a flat object.");
                        }

                        break;

                    case State.RowObjectKey:
                        if (type == JsonTokenType.PropertyName)
                        {
                            ResolveKey(ReadName(ref reader));
                            state = State.RowObjectValue;
                        }
                        else
                        {
                            EmitObjectRow();
                            state = State.TopArray;
                        }

                        break;

                    case State.RowObjectValue:
                        SetCell(ref reader, cellIndex, $"$[{rowNumber - 1}].{Safe(currentKey)}");
                        state = State.RowObjectKey;
                        break;

                    case State.TopObjectKey:
                        if (type == JsonTokenType.PropertyName)
                        {
                            var name = ReadName(ref reader);

                            if (name == "columns")
                            {
                                if (columnsSeen)
                                {
                                    throw new DatasetJsonException("DuplicateKey",
                                        "The key 'columns' appears more than once.");
                                }

                                columnsSeen = true;
                                state = State.AfterColumnsName;
                            }
                            else if (name == "rows")
                            {
                                if (rowsSeen)
                                {
                                    throw new DatasetJsonException("DuplicateKey",
                                        "The key 'rows' appears more than once.");
                                }

                                if (!columnsSeen)
                                {
                                    throw new DatasetJsonException("ColumnsFirst",
                                        "The 'columns' array must come before 'rows'.");
                                }

                                rowsSeen = true;
                                state = State.AfterRowsName;
                            }
                            else
                            {
                                throw new DatasetJsonException("UnexpectedKey",
                                    $"The key '{Safe(name)}' is not allowed; the object form has only 'columns' and 'rows'.");
                            }
                        }
                        else
                        {
                            if (!columnsSeen || !rowsSeen)
                            {
                                throw new DatasetJsonException("BadShape",
                                    "The object form requires both a 'columns' array and a 'rows' array.");
                            }

                            state = State.Done;
                        }

                        break;

                    case State.AfterColumnsName:
                        if (type != JsonTokenType.StartArray)
                        {
                            throw new DatasetJsonException("BadShape", "'columns' must be an array of column names.");
                        }

                        state = State.ColumnsArray;
                        break;

                    case State.ColumnsArray:
                        if (type == JsonTokenType.String)
                        {
                            columnHeaders.Add(ReadString(ref reader, limits.MaxHeaderLength + 1,
                                "$.columns[" + columnHeaders.Count + "]"));

                            if (columnHeaders.Count > limits.MaxColumns)
                            {
                                throw new DatasetJsonException("TooManyColumns",
                                    $"The JSON has more than the permitted {limits.MaxColumns} columns.");
                            }
                        }
                        else if (type == JsonTokenType.EndArray)
                        {
                            BuildColumnarColumns();
                            state = State.TopObjectKey;
                        }
                        else
                        {
                            throw new DatasetJsonException("ColumnNameNotString",
                                $"Column name at $.columns[{columnHeaders.Count}] must be a string.");
                        }

                        break;

                    case State.AfterRowsName:
                        if (type != JsonTokenType.StartArray)
                        {
                            throw new DatasetJsonException("BadShape", "'rows' must be an array of arrays.");
                        }

                        state = State.RowsArray;
                        break;

                    case State.RowsArray:
                        if (type == JsonTokenType.EndArray)
                        {
                            state = State.TopObjectKey;
                        }
                        else if (type == JsonTokenType.StartArray)
                        {
                            BeginRow();
                            cellIndex = 0;
                            state = State.RowArrayValues;
                        }
                        else
                        {
                            throw new DatasetJsonException("RowNotArray",
                                $"Row {rowNumber + 1} at $.rows[{rowNumber}] is not an array of values.");
                        }

                        break;

                    case State.RowArrayValues:
                        if (type == JsonTokenType.EndArray)
                        {
                            if (cellIndex != columns.Count)
                            {
                                throw new DatasetJsonException("RaggedRow",
                                    $"Row {rowNumber} at $.rows[{rowNumber - 1}] has {cellIndex} values but there are {columns.Count} columns.");
                            }

                            EmitColumnarRow();
                            state = State.RowsArray;
                        }
                        else
                        {
                            if (cellIndex >= columns.Count)
                            {
                                throw new DatasetJsonException("RaggedRow",
                                    $"Row {rowNumber} at $.rows[{rowNumber - 1}] has more values than the {columns.Count} columns.");
                            }

                            SetCell(ref reader, cellIndex, $"$.rows[{rowNumber - 1}][{cellIndex}]");
                            cellIndex++;
                        }

                        break;

                    case State.Done:
                        throw Shape();
                }
            }

            private DatasetJsonException Shape()
            {
                return new DatasetJsonException("BadShape",
                    "The top level must be an array of flat objects or an object with 'columns' and 'rows'.");
            }

            private void BeginRow()
            {
                rowNumber++;

                if (rowNumber > limits.MaxRows)
                {
                    throw new DatasetJsonException("TooManyRows",
                        $"The JSON has more than the permitted {limits.MaxRows} data rows.");
                }

                rowCells.Clear();
                seenKeys.Clear();
            }

            private void BuildColumnarColumns()
            {
                if (columnHeaders.Count == 0)
                {
                    throw new DatasetJsonException("NoColumns", "The 'columns' array is empty.");
                }

                var built = DatasetColumnNames.Build(columnHeaders, limits, report);

                if (report.Errors.Count > 0)
                {
                    Failed = true;
                    throw new DatasetJsonException(report.Errors[0].Code, report.Errors[0].Message);
                }

                foreach (var column in built)
                {
                    columns.Add(column);
                    accumulators.Add(new DatasetColumnAccumulator());
                }
            }

            private void ResolveKey(string key)
            {
                if (!seenKeys.Add(key))
                {
                    throw new DatasetJsonException("DuplicateKey",
                        $"The key '{Safe(key)}' appears more than once in the object at $[{rowNumber - 1}].");
                }

                currentKey = key;

                if (keyIndex.TryGetValue(key, out var index))
                {
                    cellIndex = index;

                    return;
                }

                if (columns.Count >= limits.MaxColumns)
                {
                    throw new DatasetJsonException("TooManyColumns",
                        $"The JSON has more than the permitted {limits.MaxColumns} distinct keys.");
                }

                var probe = new DatasetParseReport();

                if (!DatasetColumnNames.TryValidateHeader(key, columns.Count, limits, seenHeaders, probe,
                        out var header))
                {
                    var error = probe.Errors[0];

                    throw new DatasetJsonException(error.Code,
                        error.Code == "DuplicateHeader"
                            ? $"The key '{Safe(key)}' duplicates another key when compared without regard to case."
                            : $"The key at $[{rowNumber - 1}] is not acceptable: {error.Message}");
                }

                index = columns.Count;
                columns.Add(DatasetColumnNames.NewColumn(header, index, takenNames));
                accumulators.Add(new DatasetColumnAccumulator());
                keyIndex[key] = index;
                cellIndex = index;
            }

            private string ReadName(ref Utf8JsonReader reader)
            {
                return ReadString(ref reader, limits.MaxHeaderLength + 1, "the property name");
            }

            private string ReadString(ref Utf8JsonReader reader, int maxLength, string where)
            {
                var text = reader.GetString() ?? string.Empty;

                if (text.Length > maxLength)
                {
                    throw new DatasetJsonException("CellTooLong",
                        $"A value at {where} exceeds the permitted {maxLength} characters.");
                }

                foreach (var c in text)
                {
                    if ((c < 0x20 && c != '\t') || c == 0x7F)
                    {
                        throw new DatasetJsonException("ControlCharacter",
                            $"The text at {where} contains a control character.");
                    }
                }

                return text;
            }

            private void SetCell(ref Utf8JsonReader reader, int index, string path)
            {
                var accumulator = accumulators[index];
                DatasetCell cell;

                switch (reader.TokenType)
                {
                    case JsonTokenType.Number:
                        if (!reader.TryGetDouble(out var value) || !double.IsFinite(value))
                        {
                            throw new DatasetJsonException("NumberInvalid",
                                $"The number at {path} is out of range or not finite.");
                        }

                        cell = IsLargeIntegerLiteral(reader.ValueSpan)
                            ? accumulator.AddLargeInteger(value)
                            : accumulator.AddNumber(value);

                        break;
                    case JsonTokenType.True:
                        cell = accumulator.AddBoolean(true);
                        break;
                    case JsonTokenType.False:
                        cell = accumulator.AddBoolean(false);
                        break;
                    case JsonTokenType.Null:
                        cell = new DatasetCell(DatasetCellKind.Missing, 0, null);
                        break;
                    case JsonTokenType.String:
                        cell = accumulator.AddText(ReadString(ref reader, limits.MaxCellLength, path), limits, true);
                        break;
                    default:
                        throw new DatasetJsonException("NestedValue",
                            $"The value at {path} is a nested object or array; only flat values are accepted and no flattening is offered.");
                }

                while (rowCells.Count <= index)
                {
                    rowCells.Add(new DatasetCell(DatasetCellKind.Missing, 0, null));
                }

                rowCells[index] = cell;
            }

            private void EmitObjectRow()
            {
                var cells = new DatasetCell[columns.Count];

                for (var i = 0; i < cells.Length; i++)
                {
                    cells[i] = i < rowCells.Count ? rowCells[i] : new DatasetCell(DatasetCellKind.Missing, 0, null);
                }

                Emit(cells);
            }

            private void EmitColumnarRow()
            {
                Emit(rowCells.ToArray());
            }

            private void Emit(DatasetCell[] cells)
            {
                for (var i = 0; i < cells.Length; i++)
                {
                    if (cells[i].Kind == DatasetCellKind.Missing)
                    {
                        cells[i] = accumulators[i].AddMissing();
                    }
                }

                report.RowCount = rowNumber;
                output!.Add(new DatasetRow(rowNumber, cells));
            }

            private static string Safe(string text)
            {
                var builder = new StringBuilder();

                foreach (var c in text.Length > 40 ? text[..40] : text)
                {
                    builder.Append(char.IsControl(c) || c is '<' or '>' ? '?' : c);
                }

                return builder.ToString();
            }
        }
    }
}