namespace Domain.Forces
{
    internal class ForceData
    {
        public ForceData()
        {
            X = 0;
            Y = 0;            
        }

        public float X { get; set; }
        public float Y { get; set; }

        public bool IsApplied { get; set; }
        public bool Collision { get; set; }

        public int XDirection { get; set; }
        public int YDirection { get; set; }
    }
}
