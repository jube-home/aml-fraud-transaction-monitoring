using Jube.AskJooby.Observability.Interfaces;

namespace Jube.AskJooby.Observability
{
    public sealed class CompositeTokenBudget(ITokenBudget turn, ITokenBudget agent) : ITokenBudget
    {
        public long Limit => turn.Limit;
        public long Spent => turn.Spent;
        public bool IsExhausted => turn.IsExhausted || agent.IsExhausted;
        public bool IsOverrun => turn.IsOverrun || agent.IsOverrun;

        public void Record(long inputTokens, long outputTokens, long cachedInputTokens = 0)
        {
            turn.Record(inputTokens, outputTokens, cachedInputTokens);
            agent.Record(inputTokens, outputTokens, cachedInputTokens);
        }
    }
}