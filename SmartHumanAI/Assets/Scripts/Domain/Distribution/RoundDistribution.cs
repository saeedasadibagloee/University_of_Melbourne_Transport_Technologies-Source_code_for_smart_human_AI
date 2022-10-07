using System;
using System.Collections.Generic;
using Assets.Scripts.Domain.Distribution;
using DataFormats;
using Domain.Elements;
using UnityEngine;

namespace Domain.Distribution
{
    internal class RoundDistribution : BaseDistribution
    {
        private float _distributionX;
        private float _distributionY;
        private float _radius;

        public RoundDistribution(float x, float y, float radius)
        {
            _distributionX = x;
            _distributionY = y;
            _radius = radius;
        }

        public override DistributionType Type
        {
            get { return _dynamicData == null ? DistributionType.Circle : DistributionType.Dynamic; }          
        }

        public override CpmPair GenerateLocation()
        {
            CpmPair newLocation;

            var angle = (float)_rand.NextDouble() * 2 * Math.PI;
            var distance = Math.Sqrt((float)_rand.NextDouble()) * (_radius / 2); //Why does radius /2 work??

            newLocation.X = (float)(_distributionX + Math.Cos(angle) * distance);
            newLocation.Y = (float)(_distributionY + Math.Sin(angle) * distance);

            return newLocation;
        }
    }
}
