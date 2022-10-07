using System.Collections.Generic;
using DataFormats;

namespace Domain.Distribution
{
    internal class RoomDistribution : RectangularDistribution
    {        
        public RoomDistribution(List<Vertex> roomVertices) 
            : base(roomVertices) {}

    }
}
