namespace Domain.Elements
{
    internal class CircularObstacle : RoomElement
    {
        public CircularObstacle(float pX, float pY, float pRadius) : base(pX, pY, pRadius)
        {
            ElementType = ElementType.CircularObstacle;
        }

        public override int Type() { return (int)ElementType; }
    }
}
