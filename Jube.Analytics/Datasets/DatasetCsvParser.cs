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

using System.Globalization;
using System.Text;
using Jube.Analytics.Datasets.Exceptions;
using Jube.Analytics.Datasets.Models;

namespace Jube.Analytics.Datasets
{
    public static class DatasetCsvParser
    {
        private static readonly HashSet<string> missingTokens = new(StringComparer.OrdinalIgnoreCase)
        {
            "", "NA", "N/A", "#N/A", "NULL", "NAN", "NONE"
        };

        public static async Task<DatasetParseReport> ParseAsync(Stream input, DatasetLimits limits,
            Func<DatasetRow, CancellationToken, ValueTask>? sink, CancellationToken token = default)
        {
            var report = new DatasetParseReport();
            var counting = new DatasetCountingStream(input, limits.MaxBytes);

            try
            {
                var head = new byte[8];
                var headLength = await ReadFullyAsync(counting, head, token).ConfigureAwait(false);
                var rejected = DatasetContentSniffer.DetectBinary(head, headLength);

                if (rejected != null)
                {
                    report.Errors.Add(new DatasetParseError("NotCsv",
                        $"The file is {rejected}, not a CSV text file. Compressed and binary uploads are not accepted."));

                    return report;
                }

                if (DatasetContentSniffer.LooksLikeJson(head, headLength))
                {
                    report.Errors.Add(new DatasetParseError("NotCsv",
                        "The content is JSON, not CSV. Upload it as a JSON dataset."));

                    return report;
                }

                var prefixed = new DatasetPrefixStream(head.AsMemory(0, headLength), counting);
                using var reader = new StreamReader(prefixed, new UTF8Encoding(true, true), false, 16384);
                var source = new CharSource(reader);

                var header = await source
                    .ReadFirstLineAsync(limits.MaxColumns * (limits.MaxHeaderLength + 3) + 16, token)
                    .ConfigureAwait(false);

                report.Delimiter = DetectDelimiter(header);
                source.Push(header);

                var tokenizer = new Tokenizer(source, report.Delimiter, limits.MaxCellLength);
                var fields = new List<string>();

                if (!await tokenizer.ReadRecordAsync(fields, token).ConfigureAwait(false))
                {
                    report.Errors.Add(new DatasetParseError("Empty", "The file is empty."));

                    return report;
                }

                var columns = BuildColumns(fields, limits, report);

                if (report.Errors.Count > 0)
                {
                    return report;
                }

                var accumulators = columns.Select(_ => new DatasetColumnAccumulator()).ToArray();
                var cells = new DatasetCell[columns.Count];
                long rowNumber = 0;

                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    fields.Clear();

                    if (!await tokenizer.ReadRecordAsync(fields, token).ConfigureAwait(false))
                    {
                        break;
                    }

                    if (tokenizer.LastRecordWasBlank)
                    {
                        report.BlankLineCount++;
                        continue;
                    }

                    rowNumber++;

                    if (rowNumber > limits.MaxRows)
                    {
                        report.Errors.Add(new DatasetParseError("TooManyRows",
                            $"The file has more than the permitted {limits.MaxRows} data rows.", rowNumber));

                        return report;
                    }

                    if (fields.Count != columns.Count)
                    {
                        report.Errors.Add(new DatasetParseError("RaggedRow",
                            $"Row {rowNumber} (line {tokenizer.LastRecordLine}) has {fields.Count} values but the header has {columns.Count}.",
                            rowNumber, null, tokenizer.LastRecordLine));

                        return report;
                    }

                    for (var c = 0; c < fields.Count; c++)
                    {
                        cells[c] = Classify(fields[c], limits, accumulators[c]);
                    }

                    report.RowCount = rowNumber;

                    if (sink != null)
                    {
                        await sink(new DatasetRow(rowNumber, cells), token).ConfigureAwait(false);
                    }
                }

                if (rowNumber == 0)
                {
                    report.Errors.Add(new DatasetParseError("NoRows", "The file has a header but no data rows."));

                    return report;
                }

                DatasetProfile.Finish(columns, accumulators, report);
                report.Success = report.Errors.Count == 0;

                return report;
            }
            catch (CsvFormatException ex)
            {
                report.Errors.Add(new DatasetParseError(ex.Code, ex.Message, null, null, ex.Line));

                return report;
            }
            catch (DatasetSizeLimitException)
            {
                report.Errors.Add(new DatasetParseError("TooLarge",
                    $"The file exceeds the permitted size of {limits.MaxBytes} bytes."));

                return report;
            }
            catch (DecoderFallbackException)
            {
                report.Errors.Add(new DatasetParseError("NotUtf8", "The file is not valid UTF-8 text."));

                return report;
            }
            finally
            {
                report.BytesRead = counting.BytesRead;
            }
        }

        public static string SuggestName(string header, int index, ISet<string> taken)
        {
            return DatasetColumnNames.SuggestName(header, index, taken);
        }

        private static char DetectDelimiter(string header)
        {
            var inQuotes = false;
            var commas = 0;
            var semicolons = 0;

            foreach (var c in header)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (!inQuotes && c == ',')
                {
                    commas++;
                }
                else if (!inQuotes && c == ';')
                {
                    semicolons++;
                }
            }

            return semicolons > commas ? ';' : ',';
        }

        private static List<DatasetColumnDefinition> BuildColumns(List<string> fields, DatasetLimits limits,
            DatasetParseReport report)
        {
            return DatasetColumnNames.Build(fields, limits, report);
        }

        private static DatasetCell Classify(string raw, DatasetLimits limits, DatasetColumnAccumulator accumulator)
        {
            var text = raw.Trim();

            if (missingTokens.Contains(text))
            {
                return accumulator.AddMissing();
            }

            if (double.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint |
                                      NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var value)
                && double.IsFinite(value))
            {
                return accumulator.AddNumber(value);
            }

            return accumulator.AddText(text, limits);
        }

        private static async Task<int> ReadFullyAsync(Stream stream, byte[] buffer, CancellationToken token)
        {
            var total = 0;

            while (total < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(total), token).ConfigureAwait(false);

                if (read == 0)
                {
                    break;
                }

                total += read;
            }

            return total;
        }

        private sealed class CharSource(StreamReader reader)
        {
            private readonly char[] buffer = new char[8192];
            private int length;
            private string pending = string.Empty;
            private int pendingPosition;
            private int position;

            public void Push(string text)
            {
                pending = text;
                pendingPosition = 0;
            }

            public async Task<string> ReadFirstLineAsync(int maxLength, CancellationToken token)
            {
                var builder = new StringBuilder();
                var inQuotes = false;

                while (true)
                {
                    var c = await ReadAsync(token).ConfigureAwait(false);

                    if (c < 0)
                    {
                        break;
                    }

                    if (c == '"')
                    {
                        inQuotes = !inQuotes;
                    }

                    builder.Append((char)c);

                    if (!inQuotes && (c == '\n' || c == '\r'))
                    {
                        break;
                    }

                    if (builder.Length > maxLength)
                    {
                        throw new CsvFormatException("HeaderTooLong", "The header row is too long.", 1);
                    }
                }

                return builder.ToString();
            }

            public async ValueTask<int> ReadAsync(CancellationToken token)
            {
                if (pendingPosition < pending.Length)
                {
                    return pending[pendingPosition++];
                }

                if (position >= length && !await FillAsync(token).ConfigureAwait(false))
                {
                    return -1;
                }

                return buffer[position++];
            }

            public async ValueTask<int> PeekAsync(CancellationToken token)
            {
                if (pendingPosition < pending.Length)
                {
                    return pending[pendingPosition];
                }

                if (position >= length && !await FillAsync(token).ConfigureAwait(false))
                {
                    return -1;
                }

                return buffer[position];
            }

            private async ValueTask<bool> FillAsync(CancellationToken token)
            {
                length = await reader.ReadAsync(buffer.AsMemory(), token).ConfigureAwait(false);
                position = 0;

                return length > 0;
            }
        }

        private sealed class Tokenizer(CharSource source, char delimiter, int maxCellLength)
        {
            private readonly StringBuilder cell = new();
            private long line = 1;
            public bool LastRecordWasBlank { get; private set; }
            public long LastRecordLine { get; private set; }

            public async Task<bool> ReadRecordAsync(List<string> fields, CancellationToken token)
            {
                fields.Clear();
                cell.Clear();
                LastRecordWasBlank = false;
                LastRecordLine = line;
                var inQuotes = false;
                var afterQuote = false;
                var quotedField = false;

                while (true)
                {
                    var c = await source.ReadAsync(token).ConfigureAwait(false);

                    if (c < 0)
                    {
                        if (inQuotes)
                        {
                            throw new CsvFormatException("UnterminatedQuote",
                                $"A quoted value starting on line {LastRecordLine} is never closed.", LastRecordLine);
                        }

                        if (fields.Count == 0 && cell.Length == 0 && !quotedField)
                        {
                            return false;
                        }

                        EndField(fields);

                        return true;
                    }

                    if ((c < 0x20 && c != '\t' && c != '\r' && c != '\n') || c == 0x7F)
                    {
                        throw new CsvFormatException("BinaryContent",
                            $"The file contains a control character on line {line}; it is not a text CSV file.", line);
                    }

                    if (inQuotes)
                    {
                        if (c == '"')
                        {
                            if (await source.PeekAsync(token).ConfigureAwait(false) == '"')
                            {
                                await source.ReadAsync(token).ConfigureAwait(false);
                                Append('"');
                            }
                            else
                            {
                                inQuotes = false;
                                afterQuote = true;
                            }
                        }
                        else
                        {
                            if (c == '\n')
                            {
                                line++;
                            }

                            Append((char)c);
                        }

                        continue;
                    }

                    if (c == delimiter)
                    {
                        EndField(fields);
                        afterQuote = false;
                        quotedField = false;
                        continue;
                    }

                    if (c == '\r' || c == '\n')
                    {
                        if (c == '\r' && await source.PeekAsync(token).ConfigureAwait(false) == '\n')
                        {
                            await source.ReadAsync(token).ConfigureAwait(false);
                        }

                        line++;

                        if (fields.Count == 0 && cell.Length == 0 && !quotedField)
                        {
                            LastRecordWasBlank = true;

                            return true;
                        }

                        EndField(fields);

                        return true;
                    }

                    if (afterQuote)
                    {
                        throw new CsvFormatException("StrayCharacter",
                            $"Unexpected character after a closing quote on line {line}.", line);
                    }

                    if (c == '"')
                    {
                        if (cell.Length > 0 || quotedField)
                        {
                            throw new CsvFormatException("StrayQuote",
                                $"A quote inside an unquoted value on line {line}; quote the whole value.", line);
                        }

                        inQuotes = true;
                        quotedField = true;
                        continue;
                    }

                    Append((char)c);
                }
            }

            private void Append(char c)
            {
                if (cell.Length >= maxCellLength)
                {
                    throw new CsvFormatException("CellTooLong",
                        $"A value on line {line} exceeds the permitted {maxCellLength} characters.", line);
                }

                cell.Append(c);
            }

            private void EndField(List<string> fields)
            {
                fields.Add(cell.ToString());
                cell.Clear();
            }
        }
    }
}