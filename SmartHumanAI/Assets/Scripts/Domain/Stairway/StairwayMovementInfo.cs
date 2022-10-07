using System.Collections.Generic;

namespace Domain.Stairway
{
    internal class GateInfo
    {
        public GateInfo(int levelId)
        {
            this.LevelId = levelId;
            X = .0f;
            Y = .0f;
        }

        public void SetGateInfo(int gateId, float x, float y)
        {
            this.GateId = gateId;
            this.X = x;
            this.Y = y;
        }

        public int LevelId;
        public int GateId;
        public float X;
        public float Y;
    }

    internal class StairEntries
    {
        public StairEntries(AStairway stair, GateInfo entryPoint, GateInfo exitPoint, bool IntentedToEvacuate = true)
        {
            this.Stair = stair;
            this.EntryPoint = entryPoint;
            this.ExitPoint = exitPoint;
            this.IntentedToEvacuate = IntentedToEvacuate;
        }

    }

    internal class StairwayMovement
    {
        public StairwayMovement()
        {
            StairEntries = new Dictionary<int, StairEntries>();
        }

        public Dictionary<int, StairEntries> StairEntries;
    }
}
