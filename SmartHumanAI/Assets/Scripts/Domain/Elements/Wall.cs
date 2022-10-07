namespace Domain.Elements
{
    internal class Wall : RoomElement
    {
        public Wall(float startX, float startY, float endX, float endY)
            : base(startX, startY, endX, endY)
        {
            ElementType = ElementType.Wall;
        }

        public override int Type() { return (int)ElementType; }
    }
}
