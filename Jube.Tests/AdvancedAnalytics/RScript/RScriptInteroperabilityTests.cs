using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.Agents.Basic;
using Jube.Engine.AdvancedAnalytics.RScript;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.RScript
{
    [Trait("Category", "AdvancedAnalytics")]
    public class RScriptInteroperabilityTests
    {
        private const string BaseUrl = "http://r-server.test";

        [Fact]
        public void TheTrainingDataAgentIsRegisteredAsALeafUnderModelRiskSoNarrationAndPlanGovernanceSeeIt()
        {
            const string agentType = "advanced-analytics-training-data";

            AgentDefinitions.All[agentType].AllowedDelegates.Should().BeSubsetOf(["web-researcher"],
                agentType + " runs its own operations and does not route, except to bounce an open-internet "
                          + "question to the shared web research specialist");

            AgentDefinitions.HierarchyDomainOf.Should().ContainKey(agentType,
                agentType + " with no domain entry is invisible to the chat narrator's domain label and "
                          + "bypasses plan-stable governance on SpawnAgent");

            AgentDefinitions.HierarchyDomainOf[agentType].Should().Be("model-risk");
        }

        [Fact]
        public async Task ARejectedScriptReachesTheCallerWithItsStatusAndErrorIntactAsync()
        {
            var handler = new StubHttpMessageHandler((_, _) => Json("""
                                                                    {
                                                                      "status": "rejected",
                                                                      "consoleOutput": "",
                                                                      "error": "Forbidden function 'system' referenced at line 1.",
                                                                      "warnings": [],
                                                                      "durationMs": 0.4,
                                                                      "timedOut": false,
                                                                      "rVersion": "R version 4.5.1",
                                                                      "timestamp": "2026-01-01T00:00:00.000Z"
                                                                    }
                                                                    """));

            var client = new RServerClient(new HttpClient(handler), BaseUrl);

            var result = await client.ExecuteAsync("system('id')", null);

            result.Status.Should().Be("rejected");
            result.Error.Should().Contain("Forbidden function 'system'");
            result.Warnings.Should().BeEmpty();
            result.TimedOut.Should().BeFalse();
        }

        [Fact]
        public void TheCompose_FileRunsJubeRInternallyAndPublishesNoHostPort()
        {
            var compose = File.ReadAllText(Path.Combine(RepositoryRoot(), "docker-compose.yml"));
            var block = ServiceBlock(compose, "jube-r");

            block.Should().Contain("context: ./Jube.R", "the image must be built from the R server's own folder");
            block.Should().NotContain("ports:",
                "Jube.R is unauthenticated by design and must stay on the internal network");

            ServiceBlock(compose, "jube").Should().Contain("RServerUrl=http://jube-r:8080",
                "the Jube app must reach the R server by its compose service name and port");
        }

        private static HttpResponseMessage Json(string body)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "docker-compose.yml")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ??
                   throw new InvalidOperationException("docker-compose.yml not found above the test binaries.");
        }

        private static string ServiceBlock(string compose, string service)
        {
            var lines = compose.Replace("\r\n", "\n").Split('\n');
            var start = Array.FindIndex(lines, line => line == $"  {service}:");

            if (start < 0)
            {
                throw new InvalidOperationException($"No compose service named {service}.");
            }

            var end = start + 1;

            while (end < lines.Length &&
                   (lines[end].Length == 0 || lines[end].StartsWith("   ", StringComparison.Ordinal)))
            {
                end++;
            }

            return string.Join("\n", lines[start..end]);
        }
    }
}