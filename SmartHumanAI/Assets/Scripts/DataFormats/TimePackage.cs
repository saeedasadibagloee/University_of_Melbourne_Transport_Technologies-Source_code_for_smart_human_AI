using System;

namespace DataFormats
{
    [Serializable]
    class TimePackage : EventArgs
    {
        public int cycleNum = 0;

        public TimePackage(int cycle)
        {
            cycleNum = cycle;
        }
    }
}
