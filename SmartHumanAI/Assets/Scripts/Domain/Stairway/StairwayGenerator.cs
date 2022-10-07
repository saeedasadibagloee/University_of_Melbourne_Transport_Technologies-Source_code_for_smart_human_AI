namespace Domain.Stairway
{
    internal class StairwayGenerator
    {
        public static AStairway GetStairway(int stairwayType, int stairwayId)
        {
            switch (stairwayType)
            {
                case (int)StarwayType.Straight:
                    return new StraightStairs(stairwayId);

                case (int)StarwayType.HalfLanding:
                    return new HalfLandingStairway(stairwayId);

                case (int)StarwayType.Escalator:
                    return new EscalatorStairway(stairwayId);

                default:
                    return null;
            }
        }
    }
}
