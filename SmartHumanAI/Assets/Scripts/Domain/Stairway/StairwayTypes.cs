namespace Domain.Stairway
{
    internal class StraightStairs : AStairway
    {
        public StraightStairs(int pStairwayId)
        {
            StairtwayId = pStairwayId;
        }

        public override StarwayType GetStairwayType()
        {
            return StarwayType.Straight;
        }
    }

    internal class HalfLandingStairway : AStairway
    {
        public HalfLandingStairway(int pStairwayId)
        {
            StairtwayId = pStairwayId;
        }

        public override StarwayType GetStairwayType()
        {
            return StarwayType.HalfLanding;
        }      
    }

    internal class EscalatorStairway : AStairway
    {
        public EscalatorStairway(int pStairwayId)
        {
            StairtwayId = pStairwayId;
        }

        public override StarwayType GetStairwayType()
        {
            return StarwayType.Escalator;
        }
    }
}
