using System;
using DataFormats;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Core;
using InputOutput;

namespace Info
{
    public class AgentDistInfo : MonoBehaviour
    {
        public int ID = -1;
        public int NumberOfAgents = 100;

        public List<float> NumberOfAgentsIncremental = new List<float>();
        public int MaxAgents = 0;
        public List<DesignatedGatesData> DGatesData;
        public List<int> Times;

        public Def.AgentPlacement AgentPlacement = Def.AgentPlacement.Circle;
        public Def.DistributionType AgentType = Def.DistributionType.Static;

        public List<Group> GroupNumbers = new List<Group>();
        public List<Group> GroupDynamicNumbers = new List<Group>();

        public List<TimePopulation> PopulationTimetable;
        public Def.TimetableType TimetableType = Def.TimetableType.Discrete;

        public Color color = Color.white;
        public Vector3 SquarePoint1 = Vector3.zero;
        public Vector3 SquarePoint2 = Vector3.zero;
        public Vector3 SquarePoint3 = Vector3.zero;
        public Vector3 SquarePoint4 = Vector3.zero;
        public float angle;
        public float distance1;
        public float distance2;

        internal void Initiate()
        {
            ID = ObjectInfo.Instance.ArtifactId++;
        }

        internal void ApplyAgentDist(AgentDistInfo agp)
        {
         
        }

        private float Area()
        {
            float radius = transform.localScale.x / 2f;
            return Mathf.PI * radius * radius;
        }

        public void CheckDensity()
        {
            int maxAgentsByDensity = int.MaxValue;

            switch (AgentPlacement)
            {
                case Def.AgentPlacement.Circle:
                    maxAgentsByDensity = Mathf.CeilToInt(Consts.MaxAgentsDensity * Area());
                    break;
                case Def.AgentPlacement.Room:
                    Ray ray = new Ray(transform.position, Vector3.down);
                    RaycastHit hit;
                    Physics.Raycast(ray, out hit);
                    if (hit.transform != null)
                    {
                        maxAgentsByDensity = Mathf.CeilToInt(Consts.MaxAgentsDensity *
                            Create.Instance.GetRoomArea(int.Parse(hit.transform.name.Split('_')[1])));
                    }
                    break;
                case Def.AgentPlacement.Rectangle:
                    maxAgentsByDensity = Mathf.CeilToInt(Consts.MaxAgentsDensity * distance1 * distance2);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Debug.Log("maxAgentsByDensity: " + maxAgentsByDensity);

            if (NumberOfAgents > maxAgentsByDensity)
            {
                NumberOfAgents = maxAgentsByDensity;
                UIController.Instance.ShowGeneralDialog("Density of people exceeds " + Consts.MaxAgentsDensity + " p/" + Str.SqrMeter + Environment.NewLine +
                    "Number of agents in distribution set to " + maxAgentsByDensity, "Too many agents!");
            }
        }

        internal List<Vertex> GetRectangleVertices()
        {
            var vertices = new List<Vertex>();


            return vertices;
        }

        internal void ReCalculateCorners()
        {
            angle = (transform.rotation.eulerAngles.y - 90) / 180 * Mathf.PI;

            Vector v1 = new Vector(transform.position.x + distance1 / 2f, transform.position.z + distance2 / 2f);
            Vector v2 = new Vector(transform.position.x - distance1 / 2f, transform.position.z + distance2 / 2f);
            Vector v3 = new Vector(transform.position.x - distance1 / 2f, transform.position.z - distance2 / 2f);
            Vector v4 = new Vector(transform.position.x + distance1 / 2f, transform.position.z - distance2 / 2f);
            SquarePoint1 = new Vector3(v1.X, SquarePoint1.y, v1.Y);
            SquarePoint2 = new Vector3(v2.X, SquarePoint1.y, v2.Y);
            SquarePoint3 = new Vector3(v3.X, SquarePoint1.y, v3.Y);
            SquarePoint4 = new Vector3(v4.X, SquarePoint1.y, v4.Y);
        }

        internal void ApplySquarePartOne(Vector3 position1, Vector3 position2)
        {
            SquarePoint1 = position1;
            SquarePoint2 = position2;

            //Calculate the length of the adjacent and opposite
            float diffX = SquarePoint2.x - SquarePoint1.x;
            float diffY = SquarePoint2.z - SquarePoint1.z;

            //Calculates the Tan to get the radians (TAN(alpha) = opposite / adjacent)
            angle = Mathf.Atan(diffY / diffX);

            distance1 = Utils.DistanceBetween(SquarePoint2.x, SquarePoint2.z, SquarePoint1.x, SquarePoint1.z);
        }

        internal void UpdateSquare(Vector3 SquarePoint3Rough)
        {
            distance2 = Utils.DistanceBetween(SquarePoint3Rough.x, SquarePoint3Rough.z, SquarePoint2.x, SquarePoint2.z);

            float multi = 1f;

            if (SquarePoint3Rough.x > SquarePoint2.x)
                multi = -1f;

            if (SquarePoint2.x < SquarePoint1.x)
                multi *= -1;

            if (distance2 <= 0)
                return;

            float x = Mathf.Sin(angle) * distance2;
            float y = Mathf.Cos(angle) * distance2;

            SquarePoint3 = new Vector3(SquarePoint2.x + multi * x, SquarePoint2.y, SquarePoint2.z - multi * y);
            SquarePoint4 = new Vector3(SquarePoint1.x + multi * x, SquarePoint1.y, SquarePoint1.z - multi * y);

            Vector3 midPoint = new Vector3((SquarePoint2.x + SquarePoint4.x) / 2f, (SquarePoint2.y + SquarePoint4.y) / 2f, (SquarePoint2.z + SquarePoint4.z) / 2f) + Consts.AgentOffset * (Statics.LevelHeight / 2.5f);

            transform.position = midPoint;
            //transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, angle * 180 / Mathf.PI);
            transform.localScale = new Vector3(distance2, transform.localScale.y, transform.localScale.z);
        }

        public void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(SquarePoint1 + Consts.AgentOffset * (Statics.LevelHeight / 2.5f), 0.5f);
            Gizmos.DrawWireSphere(SquarePoint2 + Consts.AgentOffset * (Statics.LevelHeight / 2.5f), 0.5f);
            Gizmos.DrawWireSphere(SquarePoint3 + Consts.AgentOffset * (Statics.LevelHeight / 2.5f), 0.5f);
            Gizmos.DrawWireSphere(SquarePoint4 + Consts.AgentOffset * (Statics.LevelHeight / 2.5f), 0.5f);
        }

        public void ApplyDesignatedData(DistributionInformation distributionTableData)
        {
            Times = distributionTableData.Times;
            NumberOfAgentsIncremental = distributionTableData.PeoplePerSecond;
            //DGatesData = CheckAgainstTrains(distributionTableData.DesignatedGatesDatas);
            DGatesData = distributionTableData.DesignatedGatesDatas;
            AgentType = Def.DistributionType.Dynamic;
            MaxAgents = distributionTableData.TotalPopulation;
        }

        private List<DesignatedGatesData> CheckAgainstTrains(List<DesignatedGatesData> designatedGatesDatas)
        {
            List<DesignatedGatesData> dGatesDatas = new List<DesignatedGatesData>();

            foreach (var dGatesData in designatedGatesDatas)
            {
                dGatesDatas.Add(CheckAgainstTrains(dGatesData));
            }

            return dGatesDatas;
        }

        private DesignatedGatesData CheckAgainstTrains(DesignatedGatesData dGatesData)
        {
            DesignatedGatesData dGatesDataChecked = new DesignatedGatesData();

            foreach (var dGate in dGatesData.Distribution)
            {
                foreach (var trainInfo in FindObjectsOfType<TrainInfo>())
                {
                    if (dGate.GateID == trainInfo.ObjectId)
                    {
                        foreach (var trainGate in trainInfo.gateList)
                        {
                            dGatesDataChecked.AddData(trainGate.GetComponent<GateInfo>().Id, dGate.Percentage / trainInfo.gateList.Count);
                        }
                        break;
                    }
                }

                dGatesDataChecked.AddData(dGate.GateID, dGate.Percentage);
            }

            return dGatesDataChecked;
        }

        public float GetFirstIncremental()
        {
            if (NumberOfAgentsIncremental.Count == 0)
                NumberOfAgentsIncremental.Add(2);
            return NumberOfAgentsIncremental[0];
        }

        public void CalcMaxAgents()
        {
            MaxAgents = 0;

            if (PopulationTimetable == null)
                return;

            foreach (var popTime in PopulationTimetable)
                MaxAgents += popTime.numberOfPeople;
        }
    }
}