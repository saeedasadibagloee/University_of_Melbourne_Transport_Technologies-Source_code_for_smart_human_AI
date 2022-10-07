using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.Handlers
{
    internal class SocialClassData
    {
        public SocialClassData(int pGateID = 0, int pCycle = 0)
        {
            GateID = pGateID;
            LatestCycle = pCycle;
        }

        public int GateID { get; set; } 
        public int LatestCycle { get; set; }       
    }

    interface ISocialInfluence
    {
        bool NewEntriesExist();
        void SaveSocialDataEntry(SocialClassData sData);
        List<SocialClassData> Entries();
        void Clear();
    }

    internal class SocialDataHandler : ISocialInfluence
    {
        private object _locker = new object();

        private List<SocialClassData> _recentUpdates =
            new List<SocialClassData>();

        public bool NewEntriesExist()
        {
            return _recentUpdates.Count != 0;
        }
        public void SaveSocialDataEntry(SocialClassData sData)
        {
            lock (_locker)
            {
                _recentUpdates.Add(sData);
            }
        }
        public List<SocialClassData> Entries()
        {
            var updates = new List<SocialClassData>();
            lock (_locker)
            {
                updates.AddRange(_recentUpdates);
                _recentUpdates.Clear();
            }
            return updates;
        }

        public void Clear()
        {
            lock (_locker)
            {
                _recentUpdates.Clear();
            }
        }
    }
}
