using Core.PositionUpdater.ReactionTimeHandler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.Handlers
{
    internal interface IReactionTimeDataHandler
    {
        void AddReactionData(AgentRectionTimeData data);

        List<AgentRectionTimeData> Data();

        void Clear();
    }

    internal class ReactionTimeDataHandler : IReactionTimeDataHandler
    {
        private object _locker = new object();
        private List<AgentRectionTimeData> _data =
            new List<AgentRectionTimeData>();

        public ReactionTimeDataHandler() { }

        public void AddReactionData(AgentRectionTimeData data)
        {
            lock (_locker) { _data.Add(data); }
        }

        public List<AgentRectionTimeData> Data()
        {
            return _data;
        }

        public void Clear()
        {
            lock (_locker) { _data.Clear(); }
        }
    }
}
