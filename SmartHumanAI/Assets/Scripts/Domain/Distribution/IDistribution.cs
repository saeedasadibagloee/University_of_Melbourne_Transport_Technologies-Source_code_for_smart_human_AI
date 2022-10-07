using System.Collections.Generic;
using DataFormats;
using Domain.Elements;
using UnityEngine;

namespace Domain.Distribution
{
    enum DistributionType { None, Uniform, Circle, Rectangular, Dynamic }

    internal interface IPopulation
    {
        void SetPopulation(int population);       
        int Population();

        void SetGroupID(int groupID);
        List<int> GroupIDs();
    }

    internal interface IDynamicDistribution
    {
        void SetDynamicDistributionData(DynamicData dData);
        DynamicData GetDynamicDistributionData();
    }

    internal interface IDesignatedGates
    {
        void SetDesignatedGatesData(List<DesignatedGatesData> dData);
        List<DesignatedGatesData> GetDesignatedGatesData();
    }

    internal interface IDistribution : IPopulation, IDynamicDistribution, IDesignatedGates
    {
        DistributionType Type { get; }
        CpmPair GenerateLocation();
        void SetWalls(List<RoomElement> walls);
        Color GetColor();
        void SetColor(Color color);
        bool HasPopulation();
        int AmountToGenerate(int _nCycle);
    }
}
