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

using Jube.Data.Poco;

namespace Jube.Monitoring
{
    public sealed class HaProxyServerStatusSampler(HttpClient httpClient, string statsUrl)
    {
        public async Task<List<HaProxyServerStatus>> SampleAsync()
        {
            var results = new List<HaProxyServerStatus>();

            string csv;
            try
            {
                csv = await httpClient.GetStringAsync(statsUrl).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return results;
            }

            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
            {
                return results;
            }

            var header = lines[0].TrimStart('#', ' ').Split(',');
            var columnIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < header.Length; i++)
            {
                columnIndex[header[i].Trim()] = i;
            }

            if (!columnIndex.TryGetValue("type", out var typeIndex))
            {
                return results;
            }

            foreach (var line in lines.Skip(1))
            {
                var fields = line.Split(',');
                if (typeIndex >= fields.Length || fields[typeIndex] != "2")
                {
                    continue;
                }

                results.Add(new HaProxyServerStatus
                {
                    PxName = Field(fields, columnIndex, "pxname"),
                    SvName = Field(fields, columnIndex, "svname"),
                    Status = Field(fields, columnIndex, "status"),
                    Addr = Field(fields, columnIndex, "addr"),
                    CheckStatus = Field(fields, columnIndex, "check_status"),
                    CheckCode = IntField(fields, columnIndex, "check_code"),
                    ChkFail = IntField(fields, columnIndex, "chkfail"),
                    ChkDown = IntField(fields, columnIndex, "chkdown"),
                    LastChg = IntField(fields, columnIndex, "lastchg"),
                    Scur = IntField(fields, columnIndex, "scur"),
                    Qcur = IntField(fields, columnIndex, "qcur"),
                    Weight = IntField(fields, columnIndex, "weight"),
                    Act = IntField(fields, columnIndex, "act"),
                    Bck = IntField(fields, columnIndex, "bck"),
                    Hrsp2Xx = LongField(fields, columnIndex, "hrsp_2xx"),
                    Hrsp5Xx = LongField(fields, columnIndex, "hrsp_5xx"),
                    Mode = Field(fields, columnIndex, "mode")
                });
            }

            return results;
        }

        private static string? Field(string[] fields, Dictionary<string, int> columnIndex, string name)
        {
            if (!columnIndex.TryGetValue(name, out var index) || index >= fields.Length)
            {
                return null;
            }

            var value = fields[index].Trim();
            return value.Length == 0 ? null : value;
        }

        private static int? IntField(string[] fields, Dictionary<string, int> columnIndex, string name)
        {
            var value = Field(fields, columnIndex, name);
            return value != null && int.TryParse(value, out var parsed) ? parsed : null;
        }

        private static long? LongField(string[] fields, Dictionary<string, int> columnIndex, string name)
        {
            var value = Field(fields, columnIndex, name);
            return value != null && long.TryParse(value, out var parsed) ? parsed : null;
        }
    }
}