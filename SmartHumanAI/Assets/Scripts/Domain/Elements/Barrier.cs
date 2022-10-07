namespace Domain.Elements
{
    internal class Barrier : RoomElement
    {
        public Barrier(float startX, float startY, float endX, float endY)
            : base(startX, startY, endX, endY)
        {
            ElementType = ElementType.Barrier;
        }

        public override int Type() { return (int)ElementType; }
    }
}
