using Domain;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataFormats
{
    public class AgentUpdatePackage : EventArgs
    {
        public short classID { get; set; }

        public int agent_id { get; set; }

        public float radius { get; set; }

        public float x { get; set; }
        public float y { get; set; }

        public int location { get; set; }

        /// <summary>
        /// Level ID. If not on a level (such as on a staircase), this is -1.
        /// </summary>
        public ushort levelId { get; set; }

        public ushort levelIdreal { get; set; }

        public float density { get; set; }

        public float gate_x { get; set; }
        public float gate_y { get; set; }

        public bool isActive { get; set; }

        public bool couldPathUpdate { get; set; }
        public List<Vector3> pathOriginal = new List<Vector3>();
        public List<Vector3> pathModified = new List<Vector3>();

        public Color color = Color.white;

        public AgentType type = AgentType.Individual;


        /// <summary>
        /// As cycles, from zero.
        /// </summary>
        public int generationCycle { get; set; }
        public float evacuationTime { get; set; }
        public bool HasJustUpdatedPath { get; set; }
        public bool HasJustUpdatedDecision { get; set; }


        public AgentUpdatePackage()
        {

        }

        public AgentUpdatePackage(int id)
        {
            agent_id = id;
        }

        public override string ToString()
        {
            string str = "";

            str += "ID: " + agent_id;
            str += " Pos: (" + x.ToString("00.000000") + ", " + y.ToString("00.000000") + ")";
            str += " LevelID: " + levelId;
            str += " Active: " + isActive.ToString();

            return str;
        }
    }

    [Serializable]
    public class AgentUpdatePackageSmall
    {
        //public short classID;
        public int id;
        public uint r;
        public uint x;
        public uint y;
        public int loc;

        /// <summary>
        /// Level ID. If not on a level (such as on a staircase), this is -1.
        /// </summary>
        public ushort lId;
        public ushort lIdr;

        //public float density { get; set; }

        public uint gx;
        public uint gy;

        public bool a; //isActive

        //public bool couldPathUpdate { get; set; }
        //public List<Vector3> pathOriginal = new List<Vector3>();
        //public List<Vector3> pathModified = new List<Vector3>();

        public Color3 c = new Color3(1, 1, 1);

        public AgentType t = AgentType.Individual;

        /// <summary>
        /// As cycles, from zero.
        /// </summary>
        public int gC; //generation cycle
        public uint eT;//evacuationTime
        public bool hjup; //HasJustUpdatedPath
        public bool hjud; //HasJustUpdatedDecision


        public AgentUpdatePackageSmall()
        {

        }

        public override string ToString()
        {
            string str = "";

            str += "ID: " + id;
            str += " Pos: (" + x.ToString("00.000000") + ", " + y.ToString("00.000000") + ")";
            str += " LevelID: " + lId;
            str += " Active: " + a.ToString();

            return str;
        }
    }

    public class SimulationStatusPackage : EventArgs
    {
        public int state = Consts.MinusOne;

        public SimulationStatusPackage(int state)
        {
            this.state = state;
        }
    }

    public class InactiveAgentsCountPackage : EventArgs
    {
        public int nInactiveAgents = Consts.MinusOne;

        public InactiveAgentsCountPackage(int pInactive)
        {
            nInactiveAgents = pInactive;
        }
    }
}