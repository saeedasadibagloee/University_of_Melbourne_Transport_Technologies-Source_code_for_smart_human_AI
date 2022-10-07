using System;
using System.Collections.Generic;
using Core;
using DataFormats;
using UnityEngine;

namespace Assets.Scripts.DataFormats
{
    [Serializable]
    public class TrainPackage
    {
        public List<Train> trains { get; set; }

        public TrainPackage()
        {
            trains = new List<Train>();
        }
    }

    [Serializable]
    public class Train
    {
        public List<TrainData> trainDataTimetable { get; set; }
        public List<Vertex> waitingAreaVertices { get; set; }
        public int destinationGateID = -1;

        public int numberCarriages = -1;
        public float doorWidth = 1.638f;
        public List<int> numDoorsList = new List<int>();
        public List<float> carriageLengthsList = new List<float>();
        public List<int> wallGatesIds = new List<int>();
        public List<int> agentDistIds = new List<int>();

        public List<float> boardDistributionList = new List<float>();
        public List<List<int>> gateIDsByCarriage = new List<List<int>>();

        public Vector3 StartVector3;
        public Vector3 MiddleVector3;
        public Vector3 EndVector3;
        public int ID = -1;
        public int rotation = 0;
        public int type = (int)Def.TrainType.Train;

        public Train()
        {
            trainDataTimetable = new List<TrainData>();
            waitingAreaVertices = new List<Vertex>();
            destinationGateID = -1;
        }
    }
}
