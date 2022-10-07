namespace Domain.Elements
{
    internal class Barricade : RoomElement
    {
        public Barricade(float startX, float startY, float endX, float endY)
            : base(startX, startY, endX, endY) { }     

        public override int Type() { return (int)ElementType.Barricade; }
    }
}
