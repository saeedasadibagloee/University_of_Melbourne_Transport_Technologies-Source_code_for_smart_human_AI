using System;
using System.Collections.Generic;
using Assets.Scripts.DataFormats;
using Core;
using Domain;
using Domain.Level;
using Info;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace DataFormats
{
    [Serializable]
    public class Model
    {
        public string version = "";
        public bool panicMode = true;
        public bool validatedSuccessfully = false;
        public float levelHeight = 2.5f;

        public List<Level> levels = new List<Level>();
        public List<Stair> stairs = new List<Stair>();
        public List<Threat> threats = new List<Threat>();

        public Vector3s cameraPos = Vector3s.zero;
        public Vector3s cameraRot = Vector3s.zero;

        public Variables savedParameters = null;
    }

    /// <summary>
    /// A (s)erializable or (s)maller version of Unity's Vector3.
    /// </summary>
    [Serializable]
    public class Vector3s
    {
        //
        // Summary:
        //     X component of the vector.
        public float x;
        //
        // Summary:
        //     Y component of the vector.
        public float y;
        //
        // Summary:
        //     Z component of the vector.
        public float z;

        public Vector3s()
        {
        }

        public Vector3s(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3 ToV3()
        {
            return new Vector3(this.x, this.y, this.z);
        }

        public static Vector3s Convert(Vector3 v)
        {
            return new Vector3s(v.x, v.y, v.z);
        }

        public static Vector3s zero { get { return new Vector3s(0, 0, 0); } }
    }

    [Serializable]
    public class Color3
    {
        //
        // Summary:
        //     Red component of the color.
        public float r;
        //
        // Summary:
        //     Green component of the color.
        public float g;
        //
        // Summary:
        //     Blue component of the color.
        public float b;

        public Color3() { }
        public Color3(float r, float g, float b)
        { 
            this.r = r;
            this.g = g;
            this.b = b;
        }

        public Color ToColor()
        {
            return new Color(r, g, b, 1);
        }

        public static Color3 Convert(Color c)
        {
            return new Color3(c.r, c.g, c.b);
        }
    }

    [Serializable]
    public class Stair
    {
        public int id = Consts.MinusOne;
        public float x = Consts.MinusOne;
        public float y = Consts.MinusOne;
        public float speed = -1;
        public int spanFloors = 1;
        public float length = Consts.MinusOne;
        public float width = Consts.MinusOne;
        public float widthLanding = Consts.MinusOne;

        public int rotation = 0;

        public int type = (int)Def.StairType.Unknown;
        public int direction = (int)Def.StairDirection.Bidirectional;

        public StairGate upper = new StairGate();
        public StairGate lower = new StairGate();

        public List<Wall> walls = new List<Wall>();
    }

    [Serializable]
    public class StairGate
    {
        public int level = Consts.MinusOne;
        public Gate gate = null;
    }

    [Serializable]
    public class Level
    {
        public int id = Consts.MinusOne;
        public int width = Consts.MinusOne;
        public int height = Consts.MinusOne;

        public WallPackage wall_pkg = new WallPackage();
        public GatePackage gate_pkg = new GatePackage();
        public GatePackage counter_pkg = new GatePackage();
        public DistributionPackage agent_pkg = new DistributionPackage();
        public WaitPointPackage waitPoint_pkg = new WaitPointPackage();
        public CircularObstaclePackage obstacle_pkg = new CircularObstaclePackage();
        public BarricadePackage barricade_pkg = new BarricadePackage();
        public TrainPackage train_pkg = new TrainPackage();

        public Level() { }

        public Level(int id)
        {
            this.id = id;
        }

        public override string ToString()
        {
            string str = "L(";
            str += wall_pkg + " ";
            str += gate_pkg + " ";
            str += agent_pkg + " ";
            str += obstacle_pkg + " ";
            str += barricade_pkg + ")";
            return str;
        }
    }

    [Serializable]
    public class WaitPointPackage
    {
        public List<WaitPoint> waitPoints { get; set; }

        public WaitPointPackage()
        {
            waitPoints = new List<WaitPoint>();
        }
    }

    [Serializable]
    public class WaitPoint
    {
        public WaitPoint()
        {
            id = -1;
            x = 0f;
            y = 0f;
            radius = 0f;
            waitTime = 0f;
            wonderTime = 0f;
            interest = 0f;
        }

        public int id { get; set; }
        public float x { get; set; }
        public float y { get; set; }
        public float radius { get; set; }
        public float waitTime { get; set; }
        public float wonderTime { get; set; }
        public float interest { get; set; }

        internal CpmPair GeneratePointWithin(SimpleLevel currentLevel, int currentRoomId)
        {
            CpmPair cpmPair = new CpmPair();

            int count = 0;

            while (count++ < 10000)
            {
                float distanceFromCentre = Utils.GetNextRnd(0, radius);
                float proposedAngleRadians = Utils.GetNextRnd(0f, 2 * Mathf.PI);

                var point = new Vector(x + distanceFromCentre, y);
                point.RotateAroundPoint(proposedAngleRadians, x, y);

                if (currentLevel.GetRoomID(point.X, point.Y) == currentRoomId)
                {
                    cpmPair = new CpmPair(point.X, point.Y);
                    return cpmPair;
                }
            }

            Debug.Log("Tried 10k times, couldn't find a point :'(");

            return cpmPair;
        }
    }
}