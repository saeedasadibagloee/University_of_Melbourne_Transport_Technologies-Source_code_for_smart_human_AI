using System.Collections.Generic;
using Assets.Scripts.Domain.Distribution;
using Core;
using DataFormats;

namespace Domain.Distribution
{
    internal class RectangularDistribution : BaseDistribution
    {
        protected List<Vertex> _vertices = new List<Vertex>();

        public RectangularDistribution(List<Vertex> roomVertices)
        {
            _vertices.AddRange(roomVertices);
        }

        public new bool HasPopulation()
        {
            if (_dynamicData == null && _population < 1 && _groupIDs.Count < 1)
                return false;
            
            return true;
        }

        public override DistributionType Type
        {
            get { return _dynamicData == null ? DistributionType.Uniform : DistributionType.Dynamic; }
        }

        public override CpmPair GenerateLocation()
        {
            return Utils.GenerateLocation(_vertices, _walls);
        }
    }
}
