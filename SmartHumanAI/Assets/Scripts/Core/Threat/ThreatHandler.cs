using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Core.Threat
{
    /// <summary>
    /// Threat handler interface
    /// </summary>
    interface IThreatHandler
    {
        void AddNewThreat(GeneralThreatData pThreat);
        void RemoveThreat(GeneralThreatData pThreat);

        bool ThreatCollectionIsChanged();
        bool AnotherChoiceShouldBeMade();

        bool IsObjectBlocked(IThreatHandlingStrategy st, int objectID, int levelID);
        bool LevelHasBlockedObjects(int levelID, IThreatHandlingStrategy st = null);

        List<ILevelThreat> BlockedObjects(IThreatHandlingStrategy st, int levelID);

        void SetDefaultState();
        void ClearThreatCollection();
    }

    internal class ThreatHandler : IThreatHandler
    {
        private List<GeneralThreatData> _localThreats = new List<GeneralThreatData>();

        private Dictionary<int, LevelThreats> _levelThreats = 
            new Dictionary<int, LevelThreats>();

        private object _threatLocker = new object();
        private long _threatCollectionIsChanged = 0;
        private long _makeAnotherChoice = 0;

        public ThreatHandler() { }

        public void AddNewThreat(GeneralThreatData pThreat)
        {
            Interlocked.Exchange(ref this._threatCollectionIsChanged, 1);

            lock (_threatLocker)
            {
                _localThreats.Add(pThreat);
            }

            Interlocked.Exchange(ref this._makeAnotherChoice, 1); 
        }

        public void RemoveThreat(GeneralThreatData pThreat)
        {
            Interlocked.Exchange(ref this._threatCollectionIsChanged, 1);

            lock (_threatLocker)
            {               
                _localThreats.RemoveAll(lThreat => lThreat.ThreatId == pThreat.ThreatId);
            }

            Interlocked.Exchange(ref this._makeAnotherChoice, 1);                 
        }

        public bool ThreatCollectionIsChanged()
        {   
            if (Interlocked.Read(ref this._threatCollectionIsChanged) != 1)
            {
                if (Interlocked.Read(ref this._makeAnotherChoice) == 1)
                    Interlocked.Exchange(ref this._makeAnotherChoice, 0);               
            }
            else
            {
                UpdateThreats();
                return true;
            }

            return false;
        }

        public bool AnotherChoiceShouldBeMade()
        { 
           return Interlocked.Read(ref this._makeAnotherChoice) == 1;
        }

        private void UpdateThreats()
        {
            _levelThreats.Clear();

            lock (_threatLocker)
            {
                foreach (var threat in _localThreats)
                {
                    if (_levelThreats.ContainsKey(threat.ThreatLevelID))
                        _levelThreats[threat.ThreatLevelID].AddNewThreatDetails(threat);
                    else
                        _levelThreats.Add(threat.ThreatLevelID, new LevelThreats(threat));
                }
            }                     
        }

        public void SetDefaultState()
        {
            if (Interlocked.Read(ref this._threatCollectionIsChanged) == 1)
                Interlocked.Exchange(ref this._threatCollectionIsChanged, 0);
        }

        public bool IsObjectBlocked(IThreatHandlingStrategy st, int objectID, int levelID)
        {
            if (_levelThreats.ContainsKey(levelID) && st != null)          
                return st.IsObjectBlocked(objectID, _levelThreats[levelID]._threats);

            return false;
        }

        public bool LevelHasBlockedObjects(int levelID, IThreatHandlingStrategy st = null)
        {
            if (st != null && _levelThreats.ContainsKey(levelID))
                return st.LevelHasBlockedObjects(_levelThreats[levelID]._threats);

            else if (_levelThreats.ContainsKey(levelID))
                return _levelThreats[levelID]._threats.Count != 0;

            return false;
        }

        public List<ILevelThreat> BlockedObjects(IThreatHandlingStrategy st, int levelID)
        {
            if (_levelThreats.ContainsKey(levelID) & st != null)
                return st.BlockedObjects(_levelThreats[levelID]._threats);

            return null;
        }
       
        public void ClearThreatCollection()
        {
            _levelThreats.Clear();
            _localThreats.Clear();
        }
    }
}
