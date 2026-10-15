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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Jube.Migrations.Rag.Models;
using MessagePack;
using Npgsql;
using NpgsqlTypes;

namespace Jube.Migrations.Rag
{
    public static class RagPayloadLoader
    {
        private const string StagingTable = "RagSyncStaging";

        private const string StagingColumns =
            "\"Hash\",\"Delete\",\"ParentHash\",\"CategoryId\",\"KindId\",\"SourceFile\",\"StartLine\",\"EndLine\"," +
            "\"QualifiedName\",\"Content\",\"Embedding\",\"CreatedDate\"";

        private static readonly MessagePackSerializerOptions payloadFormat =
            MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block);

        public static void Apply(DynamicEnvironment.DynamicEnvironment dynamicEnvironment, string resourceToken)
        {
            var assembly = typeof(RagPayloadLoader).Assembly;
            var payload = PayloadResources(assembly, resourceToken);

            using var connection = new NpgsqlConnection(dynamicEnvironment.AppSettings("ConnectionString"));
            connection.Open();

            using var transaction = connection.BeginTransaction();

            CreateStagingTable(connection, transaction);
            var staged = Stage(connection, assembly, payload);
            var merged = Merge(connection, transaction);
            var dropped = DropMissing(connection, transaction);

            transaction.Commit();

            Console.WriteLine($@"AskJoobyRagChunk sync {resourceToken.Trim('.', '-')}: staged {staged} row(s) from " +
                              $@"{payload.Count} payload file(s); merged {merged}, dropped {dropped}.");
        }

        private static List<string> PayloadResources(Assembly assembly, string resourceToken)
        {
            var names = assembly.GetManifestResourceNames()
                .Where(n => n.Contains(resourceToken, StringComparison.Ordinal) &&
                            n.EndsWith(".msgpack", StringComparison.Ordinal))
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToList();

            if (names.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No embedded RAG payload matches \"{resourceToken}\" in {assembly.GetName().Name}. The " +
                    "rag-<timestamp>-NNNN.msgpack file(s) sit next to their migration in the run folder it was " +
                    "copied in with, and have to be embedded into the assembly — the migrations project needs this " +
                    "line, once: <ItemGroup><EmbeddedResource Include=\"**\\Rag\\**\\*.msgpack\" /></ItemGroup>");
            }

            return names;
        }

        private static void CreateStagingTable(NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            ExecuteSql(connection, transaction, $"""
                                                 CREATE TEMP TABLE "{StagingTable}" (
                                                     "Hash" text NOT NULL,
                                                     "Delete" boolean NULL,
                                                     "ParentHash" text NULL,
                                                     "CategoryId" smallint NULL,
                                                     "KindId" smallint NULL,
                                                     "SourceFile" text NULL,
                                                     "StartLine" integer NULL,
                                                     "EndLine" integer NULL,
                                                     "QualifiedName" text NULL,
                                                     "Content" text NULL,
                                                     "Embedding" text NULL,
                                                     "CreatedDate" timestamp NULL
                                                 ) ON COMMIT DROP;
                                                 """);
        }

        private static long Stage(NpgsqlConnection connection, Assembly assembly, List<string> payload)
        {
            var staged = 0L;

            using var writer = connection.BeginBinaryImport(
                $"COPY \"{StagingTable}\" ({StagingColumns}) FROM STDIN (FORMAT BINARY)");

            foreach (var name in payload)
            {
                using var resource = assembly.GetManifestResourceStream(name)
                                     ?? throw new InvalidOperationException(
                                         $"Embedded payload {name} could not be opened.");

                using var buffer = new MemoryStream();
                resource.CopyTo(buffer);

                var reader = new MessagePackReader(buffer.ToArray());

                while (!reader.End)
                {
                    var row = MessagePackSerializer.Deserialize<RagPayloadRow>(ref reader, payloadFormat)
                              ?? throw new InvalidOperationException(
                                  $"Payload {name} row {staged + 1} deserialised to nothing.");

                    writer.StartRow();
                    writer.Write(row.Hash, NpgsqlDbType.Text);
                    WriteBoolean(writer, row.Delete);
                    WriteText(writer, row.ParentHash);
                    WriteSmallint(writer, row.CategoryId);
                    WriteSmallint(writer, row.KindId);
                    WriteText(writer, row.SourceFile);
                    WriteInteger(writer, row.StartLine);
                    WriteInteger(writer, row.EndLine);
                    WriteText(writer, row.QualifiedName);
                    WriteText(writer, row.Content);
                    WriteText(writer, VectorText(row.Embedding));
                    WriteTimestamp(writer, row.CreatedDate);
                    staged++;
                }
            }

            writer.Complete();

            return staged;
        }

        private static int Merge(NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            return ExecuteSql(connection, transaction, $"""
                                                        INSERT INTO "AskJoobyRagChunk"
                                                            ("Hash","ParentHash","CategoryId","KindId","SourceFile","StartLine","EndLine","QualifiedName","Content","Embedding","CreatedDate")
                                                        SELECT s."Hash", s."ParentHash", s."CategoryId", s."KindId", s."SourceFile", s."StartLine",
                                                               s."EndLine", s."QualifiedName", s."Content", s."Embedding"::vector, s."CreatedDate"
                                                        FROM "{StagingTable}" s
                                                        WHERE s."Delete" IS NOT TRUE
                                                        ON CONFLICT ("Hash") DO UPDATE SET
                                                            "ParentHash" = excluded."ParentHash",
                                                            "CategoryId" = excluded."CategoryId",
                                                            "KindId" = excluded."KindId",
                                                            "SourceFile" = excluded."SourceFile",
                                                            "StartLine" = excluded."StartLine",
                                                            "EndLine" = excluded."EndLine",
                                                            "QualifiedName" = excluded."QualifiedName",
                                                            "Content" = excluded."Content",
                                                            "Embedding" = COALESCE(excluded."Embedding", "AskJoobyRagChunk"."Embedding");
                                                        """);
        }

        private static int DropMissing(NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            return ExecuteSql(connection, transaction, $"""
                                                        DELETE FROM "AskJoobyRagChunk" t
                                                        USING "{StagingTable}" s
                                                        WHERE s."Delete" IS TRUE AND t."Hash" = s."Hash";
                                                        """);
        }

        private static int ExecuteSql(NpgsqlConnection connection, NpgsqlTransaction transaction, string sql)
        {
            using var command = new NpgsqlCommand(sql, connection, transaction);
            command.CommandTimeout = 0;

            return command.ExecuteNonQuery();
        }

        private static string VectorText(float[] embedding)
        {
            if (embedding is null)
            {
                return null;
            }

            var text = new StringBuilder(embedding.Length * 12 + 2);
            text.Append('[');

            for (var i = 0; i < embedding.Length; i++)
            {
                if (i > 0)
                {
                    text.Append(',');
                }

                text.Append(embedding[i].ToString("R", CultureInfo.InvariantCulture));
            }

            text.Append(']');

            return text.ToString();
        }

        private static void WriteText(NpgsqlBinaryImporter writer, string value)
        {
            if (value is null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.Write(value, NpgsqlDbType.Text);
            }
        }

        private static void WriteBoolean(NpgsqlBinaryImporter writer, bool? value)
        {
            if (value is { } present)
            {
                writer.Write(present, NpgsqlDbType.Boolean);
            }
            else
            {
                writer.WriteNull();
            }
        }

        private static void WriteInteger(NpgsqlBinaryImporter writer, int? value)
        {
            if (value is { } present)
            {
                writer.Write(present, NpgsqlDbType.Integer);
            }
            else
            {
                writer.WriteNull();
            }
        }

        /// <summary>Written as the short Npgsql actually needs for a smallint column, not the
        /// byte CategoryId/KindId are typed as elsewhere — Npgsql's own byte mapping targets
        /// Postgres's single-byte "char" type, not smallint, so writing a bare byte here would
        /// target the wrong wire type against a column FluentMigrator's AsByte() created as
        /// smallint (Postgres has no 1-byte integer type).</summary>
        private static void WriteSmallint(NpgsqlBinaryImporter writer, byte? value)
        {
            if (value is { } present)
            {
                writer.Write((short)present, NpgsqlDbType.Smallint);
            }
            else
            {
                writer.WriteNull();
            }
        }

        private static void WriteTimestamp(NpgsqlBinaryImporter writer, DateTime? value)
        {
            if (value is { } present)
            {
                writer.Write(present, NpgsqlDbType.Timestamp);
            }
            else
            {
                writer.WriteNull();
            }
        }
    }
}