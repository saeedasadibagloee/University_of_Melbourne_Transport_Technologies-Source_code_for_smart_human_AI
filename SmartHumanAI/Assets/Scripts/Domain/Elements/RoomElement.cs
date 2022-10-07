using DataFormats;

namespace Domain.Elements
{
    public enum ElementType { Unknown, Wall, Barrier, Gate, Barricade, CircularObstacle, StairwayElement };
    public enum InnerType   { Unknown, EvacuationGate, ConnectingGate, StairwayGateDown, StairwayGateUp, TrainDoorGate }

    interface IInnerType
    {
        void SetInnerType(InnerType type);
        InnerType GetInnerType();
    }

    internal abstract class RoomElement : IInnerType
    {
        public Vertex VStart = null;
        public Vertex VEnd = null;
        public Vertex VMiddle = null;

        private int   _elementId = -1;
        protected bool  Destination = false;
        protected bool IgnoreWhenLeavingWaitingGate = false;
        protected bool IsLowBarricade = false;
        protected float length = 0f;
        protected float angle = 0f;

        protected ElementType ElementType = ElementType.Unknown;
        protected InnerType   InnerType   = InnerType.Unknown;

        protected RoomElement(float startX = 0, float startY = 0, float endX = 0, float endY = 0)
        {
            VStart = new Vertex(startX, startY);
            VEnd = new Vertex(endX, endY);

            var middleX = (startX + endX) / 2;
            var middleY = (startY + endY) / 2;

            VMiddle = new Vertex(middleX, middleY);
        }

        protected RoomElement(float x, float y, float radius)
        {
            VMiddle = new Vertex(x, y);
            length = radius;
        }

        public void SetInnerType(InnerType type) { InnerType = type; }
        public InnerType GetInnerType() { return InnerType; }

        public void ChangeType(ElementType newType) { ElementType = newType; }
        public abstract int Type();

        public virtual void SetAsDestination() { }
        public virtual bool IsDestination
        {            
            get { return Destination; }
        }

        public void SetElementId(int id)
        {
            _elementId = id;
        }

        public int ElementId
        {
            get { return _elementId; }
        }

        public void SetLength(float length)
        {
            this.length = length;
        }
        public float Length
        {
            get { return length; }
        }

        public void SetAngle(float angle)
        {
            this.angle = angle;
        }

        public float Angle
        {
            get { return angle; }
        }

        public void SetVerticesId(int vStartId, int vEndId)
        {
            VStart.id = vStartId;
            VEnd.id   = vEndId;
        }

        public void SetIWLWG(bool pWallIWlWg)
        {
            IgnoreWhenLeavingWaitingGate = pWallIWlWg;
        }

        public void SetIsLow(bool pWallIsLow)
        {
            IsLowBarricade = pWallIsLow;
        }

        public bool IsIWLWG()
        {
            return IgnoreWhenLeavingWaitingGate;
        }

        public bool IsLow()
        {
            return IsLowBarricade;
        }
    }

    
}
