using Core.Signals;
using System.Threading;

namespace Core
{
    /// <summary>
    /// Worker threads params to be set in Core
    /// and used inside the thread
    /// </summary>
    internal class ThreadParams
    {
        public int Id = 0;
        public long _IsOn = 1;

        private int _beginRange = -1;
        private int _endRange = -1;

        private readonly UpdateSignals _updateSignals = null;
        private readonly object _objectData = null;       

        public int width = 0;
        public int height = 0;

        public ModelType MType { get; set; }
        public int BeginRange
        {
            get { return _beginRange; }
            set { _beginRange = value;  }
        }

        public int EndRange
        {
            get { return _endRange; }
            set { _endRange = value; }
        }

        public long IsOn
        {
            get { return _IsOn; }
            set { _IsOn = value; }
        }

        public ThreadParams(int id, ModelType mType, UpdateSignals pSignals)
        {
            Id = id;
            MType = mType;
            _updateSignals = pSignals;
        }

        public ThreadParams(int id, ModelType mType, UpdateSignals pSignals, object pObjectData)
        {
            Id = id;
            MType = mType;
            _updateSignals = pSignals;
            _objectData = pObjectData;
        }

        public UpdateSignals USignals { get { return _updateSignals; } }

        public object ObjectData { get { return _objectData; } }   

    }
}
