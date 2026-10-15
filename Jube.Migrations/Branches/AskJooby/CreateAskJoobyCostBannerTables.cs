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

using System.Globalization;
using FluentMigrator;

namespace Jube.Migrations.Branches.AskJooby
{
    [Migration(20263107750000)]
    public class CreateAskJoobyCostBannerTables : Migration
    {
        public override void Up()
        {
            CreateAskJoobyEmbeddingCall();
            CreateAskJoobyRerankCall();
            CreateAskJoobyEndpointFee();
            CreateAskJoobyRerankFee();
            CreateAskJoobyTurnCost();
            SeedPricing();
        }

        private void SeedPricing()
        {
            void ChatBand(string days, string start, string end, double uncached, double cached, double output)
            {
                Execute.Sql(
                    "INSERT INTO \"AskJoobyEndpointFee\" " +
                    "(\"EndpointKindId\",\"Monday\",\"Tuesday\",\"Wednesday\",\"Thursday\",\"Friday\"," +
                    "\"Saturday\",\"Sunday\",\"StartTimeOfDay\",\"EndTimeOfDay\"," +
                    "\"RateInputPerMillionTokens\",\"RateCachedInputPerMillionTokens\"," +
                    "\"RateOutputPerMillionTokens\",\"CreatedDate\",\"CreatedUser\") VALUES " +
                    $"(1, {days}, '{start}', '{end}', {uncached.ToString(CultureInfo.InvariantCulture)}, " +
                    $"{cached.ToString(CultureInfo.InvariantCulture)}, " +
                    $"{output.ToString(CultureInfo.InvariantCulture)}, now(), 'Administrator');");
            }

            const string weekdays = "true,true,true,true,true,false,false";
            const string weekend = "false,false,false,false,false,true,true";

            ChatBand(weekdays, "00:00:00", "00:59:59", 0.15, 0.003, 0.6);
            ChatBand(weekdays, "01:00:00", "03:59:59", 0.3, 0.006, 1.2);
            ChatBand(weekdays, "04:00:00", "05:59:59", 0.15, 0.003, 0.6);
            ChatBand(weekdays, "06:00:00", "09:59:59", 0.3, 0.006, 1.2);
            ChatBand(weekdays, "10:00:00", "23:59:59", 0.15, 0.003, 0.6);
            ChatBand(weekend, "00:00:00", "23:59:59", 0.15, 0.003, 0.6);

            Execute.Sql(
                "INSERT INTO \"AskJoobyRerankFee\" " +
                "(\"ModelTypeId\",\"RatePerDocumentCompared\",\"RatePerMillionTokensCompared\"," +
                "\"CreatedDate\",\"CreatedUser\") VALUES " +
                "(4, NULL, 0.01, now(), 'Administrator');");
        }

        public override void Down()
        {
        }

        private void CreateAskJoobyEmbeddingCall()
        {
            Create.Table("AskJoobyEmbeddingCall")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("AskJoobyRunId").AsInt32().Nullable()
                .WithColumn("ModelTypeId").AsByte().Nullable()
                .WithColumn("InputTokens").AsInt32().Nullable()
                .WithColumn("DurationMs").AsInt64().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable();

            Create.Index().OnTable("AskJoobyEmbeddingCall").OnColumn("AskJoobyRunId").Ascending();

            Create.ForeignKey().FromTable("AskJoobyEmbeddingCall").ForeignColumn("AskJoobyRunId")
                .ToTable("AskJoobyRun").PrimaryColumn("Id");

            Execute.Sql(
                "COMMENT ON COLUMN \"AskJoobyEmbeddingCall\".\"ModelTypeId\" IS " +
                "'See Jube.Data.Repository.Models.AskJoobyModelType. 0=Unknown, 1=DeepSeekV41Flash, " +
                "2=DeepSeekV4Pro, 3=BgeM3, 4=BgeRerankerV2M3. NULL behaves as 0.';");
        }

        private void CreateAskJoobyRerankCall()
        {
            Create.Table("AskJoobyRerankCall")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("AskJoobyRunId").AsInt32().Nullable()
                .WithColumn("ModelTypeId").AsByte().Nullable()
                .WithColumn("DocumentsCompared").AsInt32().Nullable()
                .WithColumn("TokensCompared").AsInt32().Nullable()
                .WithColumn("DurationMs").AsInt64().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable();

            Create.Index().OnTable("AskJoobyRerankCall").OnColumn("AskJoobyRunId").Ascending();

            Create.ForeignKey().FromTable("AskJoobyRerankCall").ForeignColumn("AskJoobyRunId")
                .ToTable("AskJoobyRun").PrimaryColumn("Id");

            Execute.Sql(
                "COMMENT ON COLUMN \"AskJoobyRerankCall\".\"ModelTypeId\" IS " +
                "'See Jube.Data.Repository.Models.AskJoobyModelType. 0=Unknown, 1=DeepSeekV41Flash, " +
                "2=DeepSeekV4Pro, 3=BgeM3, 4=BgeRerankerV2M3. NULL behaves as 0.';");

            Execute.Sql(
                "COMMENT ON COLUMN \"AskJoobyRerankCall\".\"TokensCompared\" IS " +
                "'Set only when the reranker''s own response reports a token usage figure (see " +
                "BgeReranker''s usage parsing) -- a provider billed per document, such as a " +
                "self-hosted cross encoder with no usage field, leaves this NULL and DocumentsCompared " +
                "is the figure to price against instead. See AskJoobyRerankFee.';");
        }

        private void CreateAskJoobyEndpointFee()
        {
            Create.Table("AskJoobyEndpointFee")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EndpointKindId").AsByte().NotNullable()
                .WithColumn("Monday").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("Tuesday").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("Wednesday").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("Thursday").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("Friday").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("Saturday").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("Sunday").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("StartTimeOfDay").AsTime().NotNullable()
                .WithColumn("EndTimeOfDay").AsTime().NotNullable()
                .WithColumn("RateInputPerMillionTokens").AsDouble().NotNullable()
                .WithColumn("RateCachedInputPerMillionTokens").AsDouble().Nullable()
                .WithColumn("RateOutputPerMillionTokens").AsDouble().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("CreatedUser").AsString(255).Nullable();

            Create.Index().OnTable("AskJoobyEndpointFee").OnColumn("EndpointKindId").Ascending();

            Execute.Sql(
                "COMMENT ON TABLE \"AskJoobyEndpointFee\" IS " +
                "'Per-kind token pricing for everything that bills per token -- chat completions and " +
                "embeddings alike. EndpointKind is the real key, one to many: " +
                "AskJooby deliberately runs one opinionated model per kind rather than letting a user or " +
                "the model itself choose one, so pricing never needed to match the provider''s literal " +
                "model-name string -- it only ever needed to know which kind of call this was and when it " +
                "happened. Several rows may share an EndpointKind, each a day/time band (Monday..Sunday " +
                "plus StartTimeOfDay/EndTimeOfDay, both inclusive, a band wrapping past midnight when " +
                "StartTimeOfDay > EndTimeOfDay), which is how an off-peak pricing matrix is expressed -- a " +
                "single row covering the whole day uses StartTimeOfDay=00:00:00, EndTimeOfDay=23:59:59, " +
                "the latest instant a Postgres time column can hold. Read live by " +
                "AskJoobyCostRegistryRefreshService on an interval rather than cached for the life of the " +
                "process, so a rate change here takes effect without a restart. " +
                "RateCachedInputPerMillionTokens prices the subset of a call''s InputTokens the provider " +
                "served from its prompt cache (see AskJoobyModelCall.CachedInputTokens); the remainder of " +
                "InputTokens, never the whole figure, is priced at RateInputPerMillionTokens. " +
                "RateOutputPerMillionTokens is null for an embedding row, which has no output side.';");

            Execute.Sql(
                "COMMENT ON COLUMN \"AskJoobyEndpointFee\".\"EndpointKindId\" IS " +
                "'See Jube.Data.Repository.Models.AskJoobyEndpointKind. 1=Chat, 2=Embedding.';");
        }

        private void CreateAskJoobyRerankFee()
        {
            Create.Table("AskJoobyRerankFee")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("ModelTypeId").AsByte().NotNullable()
                .WithColumn("RatePerDocumentCompared").AsDouble().Nullable()
                .WithColumn("RatePerMillionTokensCompared").AsDouble().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("CreatedUser").AsString(255).Nullable();

            Create.Index().OnTable("AskJoobyRerankFee").OnColumn("ModelTypeId").Unique();

            Execute.Sql(
                "COMMENT ON TABLE \"AskJoobyRerankFee\" IS " +
                "'Kept apart from AskJoobyEndpointFee because a reranker''s billing unit varies by " +
                "provider -- a hosted BGE Reranker v2 m3 may bill per token, while a " +
                "self-hosted cross encoder with no usage reporting can only be priced per document " +
                "compared. Both rate columns are nullable and a row sets whichever one its provider " +
                "actually bills on; pricing (see AskJoobyConversationService.PriceRerankCalls) prefers " +
                "RatePerMillionTokensCompared against AskJoobyRerankCall.TokensCompared when both are " +
                "present, falling back to RatePerDocumentCompared against DocumentsCompared otherwise. " +
                "Read live by AskJoobyCostRegistryRefreshService the same way AskJoobyEndpointFee is.';");

            Execute.Sql(
                "COMMENT ON COLUMN \"AskJoobyRerankFee\".\"ModelTypeId\" IS " +
                "'See Jube.Data.Repository.Models.AskJoobyModelType. 1=DeepSeekV41Flash, 2=DeepSeekV4Pro, " +
                "3=BgeM3, 4=BgeRerankerV2M3.';");
        }

        private void CreateAskJoobyTurnCost()
        {
            Create.Table("AskJoobyTurnCost")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("AskJoobyRunId").AsInt32().Nullable()
                .WithColumn("InputTokens").AsInt32().Nullable()
                .WithColumn("CachedInputTokens").AsInt32().Nullable()
                .WithColumn("OutputTokens").AsInt32().Nullable()
                .WithColumn("EmbeddingInputTokens").AsInt32().Nullable()
                .WithColumn("RerankDocumentsCompared").AsInt32().Nullable()
                .WithColumn("JoobrikCount").AsInt32().Nullable()
                .WithColumn("DistinctJoobrikCount").AsInt32().Nullable()
                .WithColumn("ChatCost").AsDouble().Nullable()
                .WithColumn("EmbeddingCost").AsDouble().Nullable()
                .WithColumn("RerankCost").AsDouble().Nullable()
                .WithColumn("TotalCost").AsDouble().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable();

            Create.Index().OnTable("AskJoobyTurnCost").OnColumn("AskJoobyRunId").Unique();

            Create.ForeignKey().FromTable("AskJoobyTurnCost").ForeignColumn("AskJoobyRunId")
                .ToTable("AskJoobyRun").PrimaryColumn("Id");

            Execute.Sql(
                "COMMENT ON TABLE \"AskJoobyTurnCost\" IS " +
                "'One durable row per completed top-level turn (AskJoobyRunId is that turn''s own root " +
                "run, unique here), written once at the same point the live Stats banner is computed, so " +
                "cost can be reported on across turns later without re-walking the run tree. Every count " +
                "and cost figure here is the same figure the banner showed at the time -- a null cost " +
                "means a rate was missing for something this turn used, not that it was free.';");
        }
    }
}