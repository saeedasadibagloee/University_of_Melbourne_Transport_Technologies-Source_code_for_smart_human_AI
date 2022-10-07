namespace Domain.Level
{
    public class StochasticDetourData
    {
        public float strength = 0;
        public float x = -1f;
        public float y = -1f;

        public StochasticDetourData() { }

        public StochasticDetourData(float strength, float x, float y)
        {
            this.strength = strength;
            this.x = x;
            this.y = y;
        }
    }
}