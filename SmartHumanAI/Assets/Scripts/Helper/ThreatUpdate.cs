using DataFormats;

namespace Helper
{
    public class ThreatUpdate
    {
        public ThreatUpdate(int objectId = -1, bool isActive = true)
        {
            ObjectId = objectId;
            IsActive = isActive;
        }
        
        public bool IsActive { get; set; }

        public int ObjectId { get; set; }
    }
}
