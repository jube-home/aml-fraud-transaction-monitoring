using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using log4net;
using Npgsql;

namespace Jube.Data.Query;

public class GetArchiveDistinctEntryKeyValue(ILog log, string connectionString)
{
    public async Task<List<string>> Execute(Guid entityAnalysisModelGuid,
        string key, DateTime dateFrom, DateTime dateTo)
    {
        var connection = new NpgsqlConnection(connectionString);
        var value = new List<string>();
        try
        {
            await connection.OpenAsync();

            var sql = "select distinct \"Json\" -> 'payload' ->> (@key)" +
                      " from \"Archive\" a inner join \"EntityAnalysisModel\" e on a.\"EntityAnalysisModelId\" = e.\"Id\"" +
                      " where e.\"Guid\" = (@entityAnalysisModelGuid)" +
                      " and \"Json\" -> 'payload' ->> (@key) = (@value)" +
                      " and \"CreatedDate\" > (@dateFrom) and \"CreatedDate\" < (@dateTo)";

            var command = new NpgsqlCommand(sql);
            command.Connection = connection;
            command.Parameters.AddWithValue("key", key);
            command.Parameters.AddWithValue("dateFrom", dateFrom);
            command.Parameters.AddWithValue("dateTo", dateTo);
            command.Parameters.AddWithValue("entityAnalysisModelGuid", entityAnalysisModelGuid);
            await command.PrepareAsync();

            var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                if (!reader.IsDBNull(0))
                    value.Add(reader.GetValue(0).ToString());

            await reader.CloseAsync();
            await reader.DisposeAsync();
            await command.DisposeAsync();
        }
        catch (Exception ex)
        {
            log.Error($"Archive SQL: Has created an exception as {ex}.");
        }
        finally
        {
            await connection.CloseAsync();
            await connection.DisposeAsync();
        }

        return value;
    }

    public async Task<List<string>> Execute(Guid entityAnalysisModelGuid,
        string key)
    {
        var connection = new NpgsqlConnection(connectionString);
        var value = new List<string>();
        try
        {
            await connection.OpenAsync();

            var sql = "select distinct \"Json\" -> 'payload' ->> (@key)" +
                      " from \"Archive\" a inner join \"EntityAnalysisModel\" e on a.\"EntityAnalysisModelId\" = e.\"Id\"" +
                      " where e.\"Guid\" = (@entityAnalysisModelGuid)";

            var command = new NpgsqlCommand(sql);
            command.Connection = connection;
            command.Parameters.AddWithValue("entityAnalysisModelGuid", entityAnalysisModelGuid);
            command.Parameters.AddWithValue("key", key);
            await command.PrepareAsync();

            var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                if (!reader.IsDBNull(0))
                    value.Add(reader.GetValue(0).ToString());

            await reader.CloseAsync();
            await reader.DisposeAsync();
            await command.DisposeAsync();
        }
        catch (Exception ex)
        {
            log.Error($"Archive SQL: Has created an exception as {ex}.");
        }
        finally
        {
            await connection.CloseAsync();
            await connection.DisposeAsync();
        }

        return value;
    }
}