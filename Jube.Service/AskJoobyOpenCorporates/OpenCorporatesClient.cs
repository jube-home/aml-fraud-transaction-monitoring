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

using System.Diagnostics;
using System.Text.Json;
using Jube.AskJooby.Observability.Interfaces;
using Jube.AskJooby.Tools.OpenCorporates.Interfaces;
using Jube.AskJooby.Tools.OpenCorporates.Models;

namespace Jube.Service.AskJoobyOpenCorporates
{
    public sealed class OpenCorporatesClient(HttpClient httpClient, string endpoint, string apiKey)
        : IOpenCorporatesClient
    {
        public Task<OpenCorporatesCompanySearchResponse> SearchCompanyAsync(string query, int maxResults,
            CancellationToken token = default)
        {
            return SearchCompanyAsync(query, maxResults, null, null, token);
        }

        public async Task<OpenCorporatesCompanySearchResponse> SearchCompanyAsync(string query, int maxResults,
            IRunRecorder? recorder, string? runId, CancellationToken token = default)
        {
            var clock = Stopwatch.StartNew();

            var url = $"{endpoint}/companies/search?q={Uri.EscapeDataString(query)}&per_page={maxResults}" +
                      $"&api_token={Uri.EscapeDataString(apiKey)}";

            var (_, rawJson) = await SendAsync(url, query, token).ConfigureAwait(false);

            using var document = JsonDocument.Parse(rawJson);
            var result = ReadCompanySearch(document.RootElement, rawJson);

            await RecordAsync(recorder, runId, "SearchCompany", query, rawJson, clock.ElapsedMilliseconds, token)
                .ConfigureAwait(false);

            return result;
        }

        public Task<OpenCorporatesCompanyResponse> GetCompanyAsync(string jurisdictionCode, string companyNumber,
            CancellationToken token = default)
        {
            return GetCompanyAsync(jurisdictionCode, companyNumber, null, null, token);
        }

        public async Task<OpenCorporatesCompanyResponse> GetCompanyAsync(string jurisdictionCode,
            string companyNumber, IRunRecorder? recorder, string? runId, CancellationToken token = default)
        {
            var clock = Stopwatch.StartNew();
            var query = $"{jurisdictionCode}/{companyNumber}";

            var url = $"{endpoint}/companies/{Uri.EscapeDataString(jurisdictionCode)}/" +
                      $"{Uri.EscapeDataString(companyNumber)}?api_token={Uri.EscapeDataString(apiKey)}";

            var (status, rawJson) = await SendAsync(url, query, token, allowNotFound: true).ConfigureAwait(false);

            var result = status == System.Net.HttpStatusCode.NotFound
                ? new OpenCorporatesCompanyResponse(null, rawJson)
                : ReadCompanyResponse(JsonDocument.Parse(rawJson).RootElement, rawJson);

            await RecordAsync(recorder, runId, "GetCompanyDetails", query, rawJson, clock.ElapsedMilliseconds,
                token).ConfigureAwait(false);

            return result;
        }

        public Task<OpenCorporatesBeneficialOwnerSearchResponse> SearchBeneficialOwnersAsync(string companyName,
            string? ownerName, CancellationToken token = default)
        {
            return SearchBeneficialOwnersAsync(companyName, ownerName, null, null, token);
        }

        public async Task<OpenCorporatesBeneficialOwnerSearchResponse> SearchBeneficialOwnersAsync(
            string companyName, string? ownerName, IRunRecorder? recorder, string? runId,
            CancellationToken token = default)
        {
            var clock = Stopwatch.StartNew();
            var query = string.IsNullOrWhiteSpace(ownerName) ? companyName : $"{companyName} / {ownerName}";

            var url = $"{endpoint}/statements/control_statements/search" +
                      $"?controlled_entity_name={Uri.EscapeDataString(companyName)}" +
                      "&is_ultimate_beneficial_owner=true" +
                      (string.IsNullOrWhiteSpace(ownerName)
                          ? ""
                          : $"&controlling_entities_name={Uri.EscapeDataString(ownerName)}") +
                      $"&api_token={Uri.EscapeDataString(apiKey)}";

            var (_, rawJson) = await SendAsync(url, query, token).ConfigureAwait(false);

            using var document = JsonDocument.Parse(rawJson);
            var result = ReadBeneficialOwnerSearch(document.RootElement, rawJson);

            await RecordAsync(recorder, runId, "SearchBeneficialOwners", query, rawJson, clock.ElapsedMilliseconds,
                token).ConfigureAwait(false);

            return result;
        }

        private async Task<(System.Net.HttpStatusCode Status, string RawJson)> SendAsync(string url, string query,
            CancellationToken token, bool allowNotFound = false)
        {
            using var response = await httpClient.GetAsync(url, token).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode &&
                !(allowNotFound && response.StatusCode == System.Net.HttpStatusCode.NotFound))
            {
                var body = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                throw new HttpRequestException(
                    $"OpenCorporates answered {(int)response.StatusCode} for '{query}': {Shorten(body)}");
            }

            var rawJson = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

            return (response.StatusCode, rawJson);
        }

        private static Task RecordAsync(IRunRecorder? recorder, string? runId, string operation, string query,
            string rawJson, long durationMs, CancellationToken token)
        {
            return recorder is not null && runId is not null
                ? recorder.RecordOpenCorporatesCallAsync(runId, operation, query, rawJson, durationMs, token)
                : Task.CompletedTask;
        }

        private static OpenCorporatesCompanySearchResponse ReadCompanySearch(JsonElement root, string rawJson)
        {
            var companies = new List<OpenCorporatesCompany>();
            var totalCount = 0;

            if (root.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Object)
            {
                if (results.TryGetProperty("total_count", out var totalCountElement)
                    && totalCountElement.TryGetInt32(out var parsedTotal))
                {
                    totalCount = parsedTotal;
                }

                if (results.TryGetProperty("companies", out var companiesElement)
                    && companiesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var entry in companiesElement.EnumerateArray())
                    {
                        var companyElement = entry.TryGetProperty("company", out var wrapped) ? wrapped : entry;
                        companies.Add(ReadCompany(companyElement));
                    }
                }
            }

            return new OpenCorporatesCompanySearchResponse(companies, totalCount == 0 ? companies.Count : totalCount,
                rawJson);
        }

        private static OpenCorporatesCompanyResponse ReadCompanyResponse(JsonElement root, string rawJson)
        {
            if (root.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Object
                                                                && results.TryGetProperty("company",
                                                                    out var companyElement))
            {
                return new OpenCorporatesCompanyResponse(ReadCompany(companyElement), rawJson);
            }

            return new OpenCorporatesCompanyResponse(null, rawJson);
        }

        private static OpenCorporatesCompany ReadCompany(JsonElement element)
        {
            return new OpenCorporatesCompany(
                ReadString(element, "name") ?? "",
                ReadString(element, "company_number"),
                ReadString(element, "jurisdiction_code"),
                ReadString(element, "company_type"),
                ReadString(element, "current_status"),
                ReadString(element, "incorporation_date"),
                ReadString(element, "dissolution_date"),
                ReadString(element, "registered_address_in_full"),
                ReadStringArray(element, "previous_names", "company_name", "name"),
                ReadOptionalStringArray(element, "ultimate_beneficial_owners", "name"),
                ReadSource(element));
        }

        private static OpenCorporatesSource? ReadSource(JsonElement element)
        {
            if (!element.TryGetProperty("source", out var source) || source.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return new OpenCorporatesSource(
                ReadString(source, "publisher"), ReadString(source, "url"), ReadString(source, "retrieved_at"),
                ReadString(source, "terms"));
        }

        private static OpenCorporatesBeneficialOwnerSearchResponse ReadBeneficialOwnerSearch(JsonElement root,
            string rawJson)
        {
            var statements = new List<OpenCorporatesBeneficialOwnerStatement>();

            if (root.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Object)
            {
                var statementsElement = results.TryGetProperty("control_statements", out var named)
                    ? named
                    : results.TryGetProperty("statements", out var fallback)
                        ? fallback
                        : default;

                if (statementsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var entry in statementsElement.EnumerateArray())
                    {
                        var statementElement = entry.TryGetProperty("control_statement", out var wrapped)
                            ? wrapped
                            : entry.TryGetProperty("statement", out var wrapped2)
                                ? wrapped2
                                : entry;

                        statements.Add(ReadStatement(statementElement));
                    }
                }
            }

            return new OpenCorporatesBeneficialOwnerSearchResponse(statements, rawJson);
        }

        private static OpenCorporatesBeneficialOwnerStatement ReadStatement(JsonElement element)
        {
            var properties = element.TryGetProperty("properties", out var props)
                             && props.ValueKind == JsonValueKind.Object
                ? props
                : element;

            var controllingEntityName = ReadString(properties, "controlling_entities_name")
                                        ?? ReadFirstNestedName(properties, "controlling_entities");

            var controllingEntityType = ReadString(properties, "controlling_entities_entity_type")
                                        ?? ReadFirstNestedField(properties, "controlling_entities", "entity_type");

            var controlledEntityName = ReadString(properties, "controlled_entity_name")
                                       ?? ReadNestedField(properties, "controlled_entity", "name");

            var isUltimateBeneficialOwner = properties.TryGetProperty("is_ultimate_beneficial_owner", out var ubo)
                                            && ubo.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? ubo.GetBoolean()
                : (bool?)null;

            var startDate = ReadString(properties, "started_date") ?? ReadString(properties, "start_date");

            var provenances = new List<OpenCorporatesProvenance>();

            if (element.TryGetProperty("provenances", out var provenancesElement)
                && provenancesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in provenancesElement.EnumerateArray())
                {
                    provenances.Add(new OpenCorporatesProvenance(
                        ReadString(entry, "source_url"),
                        entry.TryGetProperty("confidence", out var confidence) &&
                        confidence.TryGetDouble(out var parsed)
                            ? parsed
                            : null,
                        ReadString(entry, "source_type"), ReadString(entry, "actor_type"),
                        ReadString(entry, "created_at")));
                }
            }

            return new OpenCorporatesBeneficialOwnerStatement(controllingEntityName, controllingEntityType,
                controlledEntityName, isUltimateBeneficialOwner, startDate, provenances);
        }

        private static string? ReadString(JsonElement element, string name)
        {
            return element.ValueKind == JsonValueKind.Object
                   && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }

        private static string? ReadFirstNestedName(JsonElement element, string arrayName)
        {
            return ReadFirstNestedField(element, arrayName, "name");
        }

        private static string? ReadFirstNestedField(JsonElement element, string arrayName, string fieldName)
        {
            if (!element.TryGetProperty(arrayName, out var array) || array.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            foreach (var entry in array.EnumerateArray())
            {
                var value = ReadString(entry, fieldName);

                if (value is not null)
                {
                    return value;
                }
            }

            return null;
        }

        private static string? ReadNestedField(JsonElement element, string objectName, string fieldName)
        {
            return element.TryGetProperty(objectName, out var nested) && nested.ValueKind == JsonValueKind.Object
                ? ReadString(nested, fieldName)
                : null;
        }

        private static IReadOnlyList<string> ReadStringArray(JsonElement element, string arrayName,
            params string[] candidateFields)
        {
            return ReadOptionalStringArray(element, arrayName, candidateFields) ?? [];
        }

        private static IReadOnlyList<string>? ReadOptionalStringArray(JsonElement element, string arrayName,
            params string[] candidateFields)
        {
            if (!element.TryGetProperty(arrayName, out var array) || array.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var values = new List<string>();

            foreach (var entry in array.EnumerateArray())
            {
                if (entry.ValueKind == JsonValueKind.String)
                {
                    values.Add(entry.GetString() ?? "");

                    continue;
                }

                var matched = candidateFields.Select(field => ReadString(entry, field))
                    .FirstOrDefault(value => value is not null);

                if (matched is not null)
                {
                    values.Add(matched);
                }
            }

            return values;
        }

        private static string Shorten(string value)
        {
            var trimmed = value.Trim().Replace('\n', ' ');

            return trimmed.Length <= 300 ? trimmed : trimmed[..300] + "...";
        }
    }
}