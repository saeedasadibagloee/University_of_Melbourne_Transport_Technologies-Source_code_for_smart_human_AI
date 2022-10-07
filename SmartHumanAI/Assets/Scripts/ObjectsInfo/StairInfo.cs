using System;
using System.Collections.Generic;
using DataFormats;
using Domain;
using UnityEngine;

namespace Info
{
    public class StairInfo : MonoBehaviour
    {
        public int StairId = -1;
        public Def.StairType stairType = (int)Def.StairType.Unknown;
        public Def.StairDirection stairDirection = Def.StairDirection.Bidirectional;
        public int level = -1;
        public int rotation = 0;
        public float speed = -1;
        public int spanFloors = Consts.StairHeight;

        private float length = Consts.StairLength;
        private float width = Consts.StairWidth;
        private float widthLanding = Consts.StairLandingWidth;

        public Transform upperGate;
        public Transform lowerGate;
        public List<Transform> wallList = new List<Transform>();
        public List<Transform> seperatorList = new List<Transform>();

        public float xMax, yMax = float.MinValue;
        public float xMin, yMin = float.MaxValue;
        public float m1, m2 = 0;
        public float c1, c2 = 0;

        public Animator escalatorSteps = null;

        public float Length
        {
            get { return length; }
        }

        public float Width
        {
            get { return width; }
        }

        public float WidthLanding
        {
            get { return widthLanding; }
        }

        public void UpdateSize(float length, float width, float widthLanding)
        {
            this.length = length;
            this.width = width;
            this.widthLanding = widthLanding;
            UpdateSize();
        }

        public void UpdateSize()
        {
            // Scale items in stairs to fit parameters.
            Debug.Log("SpanFloors " + spanFloors);
            switch (stairType)
            {
                case Def.StairType.HalfLanding:
                    if (wallList.Count < 4)
                    {
                        Debug.LogError("Need at least 4 walls for a half landing staircase.");
                        return;
                    }

                    wallList[0].transform.localScale = new Vector3(wallList[0].transform.localScale.x,
                        Statics.LevelHeight, wallList[0].transform.localScale.z);
                    wallList[1].transform.localScale = new Vector3(wallList[1].transform.localScale.x,
                        Statics.LevelHeight, wallList[1].transform.localScale.z);
                    wallList[2].transform.localScale = new Vector3(wallList[2].transform.localScale.x,
                        Statics.LevelHeight, wallList[2].transform.localScale.z);
                    wallList[3].transform.localScale = new Vector3(wallList[3].transform.localScale.x,
                        Statics.LevelHeight, wallList[3].transform.localScale.z);
                    lowerGate.transform.localScale = new Vector3(lowerGate.transform.localScale.x,
                        Statics.LevelHeight / 10f, lowerGate.transform.localScale.z);
                    upperGate.transform.localScale = new Vector3(upperGate.transform.localScale.x,
                        Statics.LevelHeight / 10f, upperGate.transform.localScale.z);

                    wallList[0].GetComponent<WallInfo>().UpdateLength(Width * 2);
                    wallList[0].transform.localPosition = new Vector3(Width, 0, 0);
                    wallList[1].GetComponent<WallInfo>().UpdateLength(Length + WidthLanding);
                    wallList[1].transform.localPosition = new Vector3(0, 0, (Length + WidthLanding) / 2f);
                    wallList[2].GetComponent<WallInfo>().UpdateLength(Length + WidthLanding);
                    wallList[2].transform.localPosition = new Vector3(Width * 2, 0, (Length + WidthLanding) / 2f);
                    wallList[3].GetComponent<WallInfo>().UpdateLength(Width);
                    wallList[3].transform.localPosition = new Vector3(Width / 2, 0, Length + WidthLanding);
                    lowerGate.GetComponent<GateInfo>().UpdateLength(Width);
                    lowerGate.localPosition = new Vector3(Width * 1.5f, Statics.LevelHeight / 2.5f, Length + WidthLanding);
                    upperGate.GetComponent<GateInfo>().UpdateLength(Width);
                    upperGate.localPosition = new Vector3(Width / 2, Statics.LevelHeight / 2.5f + Statics.LevelHeight, Length + WidthLanding);

                    float rampLength = Mathf.Sqrt((Statics.LevelHeight / 2f) * (Statics.LevelHeight / 2f) + Length * Length);
                    float rampAngle = Mathf.Atan((Statics.LevelHeight / 2f) / Length) * 180 / Mathf.PI;

                    foreach (Transform child in transform)
                    {
                        switch (child.name)
                        {
                            case "Stairs1_Prefab":
                                child.localScale = new Vector3(child.localScale.x, Width - 0.07f, rampLength);
                                child.localPosition = new Vector3(Width / 2, 0.573f * (Statics.LevelHeight / 2.5f), (Length / 2) + WidthLanding);
                                child.localRotation = Quaternion.Euler(-rampAngle, 0, -90);
                                if (rampAngle < Consts.RampAngle)
                                {
                                    child.GetComponent<MeshRenderer>().material = Create.Instance.MaterialsOpaque[(int)Def.Mat.Floor];
                                    child.GetComponent<MeshRenderer>().material.mainTextureScale = new Vector2(rampLength * 0.5f, 0.5f);
                                }
                                break;
                            case "Stairs2_Prefab":
                                child.localScale = new Vector3(child.localScale.x, Width - 0.07f, rampLength);
                                child.localPosition = new Vector3(Width * 1.5f, -0.637f * (Statics.LevelHeight / 2.5f), (Length / 2) + WidthLanding);
                                child.localRotation = Quaternion.Euler(rampAngle, 0, -90);
                                if (rampAngle < Consts.RampAngle)
                                {
                                    child.GetComponent<MeshRenderer>().material = Create.Instance.MaterialsOpaque[(int)Def.Mat.Floor];
                                    child.GetComponent<MeshRenderer>().material.mainTextureScale = new Vector2(rampLength * 0.5f, 0.5f);
                                }
                                break;
                            case "Landing_Prefab":
                                child.localScale = new Vector3(child.localScale.x, WidthLanding, Width * 2);
                                child.localPosition = new Vector3(Width, child.localPosition.y, WidthLanding / 2);
                                break;
                            case "Seperator_Prefab":
                                child.GetComponent<WallInfo>().UpdateLength(Length);
                                child.localPosition = new Vector3(Width, 0, (Length / 2) + WidthLanding);
                                child.localScale = new Vector3(child.localScale.x, Statics.LevelHeight, child.localScale.z);
                                break;
                        }
                    }

                    break;
                case Def.StairType.Straight:
                    if (wallList.Count < 3)
                    {
                        Debug.LogError("Need at least 3 walls for a straight staircase.");
                        return;
                    }

                    wallList[0].transform.localScale = new Vector3(wallList[0].transform.localScale.x,
                        Statics.LevelHeight * spanFloors, wallList[0].transform.localScale.z);
                    wallList[1].transform.localScale = new Vector3(wallList[1].transform.localScale.x,
                        Statics.LevelHeight * spanFloors, wallList[1].transform.localScale.z);
                    wallList[2].transform.localScale = new Vector3(wallList[2].transform.localScale.x,
                        Statics.LevelHeight * spanFloors, wallList[2].transform.localScale.z);
                    lowerGate.transform.localScale = new Vector3(lowerGate.transform.localScale.x,
                        Statics.LevelHeight / 10f, lowerGate.transform.localScale.z);
                    upperGate.transform.localScale = new Vector3(upperGate.transform.localScale.x,
                        Statics.LevelHeight / 10f, upperGate.transform.localScale.z);

                    wallList[0].GetComponent<WallInfo>().UpdateLength(Length);
                    wallList[0].transform.localPosition = new Vector3(0, (spanFloors - 1) * (Statics.LevelHeight / 2), Length / 2);
                    wallList[1].GetComponent<WallInfo>().UpdateLength(Length);
                    wallList[1].transform.localPosition = new Vector3(Width, (spanFloors - 1) * (Statics.LevelHeight / 2), Length / 2);
                    wallList[2].GetComponent<WallInfo>().UpdateLength(Width);
                    wallList[2].transform.localPosition = new Vector3(Width / 2, (spanFloors - 1) * (Statics.LevelHeight / 2), Length);
                    lowerGate.GetComponent<GateInfo>().UpdateLength(Width);
                    lowerGate.localPosition = new Vector3(Width / 2, Statics.LevelHeight / 2.5f, 0);
                    upperGate.GetComponent<GateInfo>().UpdateLength(Width);
                    upperGate.localPosition = new Vector3(Width / 2, Statics.LevelHeight / 2.5f + Statics.LevelHeight * spanFloors, Length);

                    Transform ramp = transform.GetChild(0);
                    foreach (Transform child in transform)
                        if (child.name == "Stairs1_Prefab")
                            ramp = child;

                    float rampAngle2 = Mathf.Atan(Statics.LevelHeight * spanFloors / Length) * 180 / Mathf.PI;
                    float rampLength2 = Mathf.Sqrt(Statics.LevelHeight * spanFloors * Statics.LevelHeight * spanFloors + Length * Length);

                    ramp.localScale = new Vector3(ramp.localScale.x, Width - 0.07f, rampLength2);
                    ramp.localPosition = new Vector3(Width / 2, ramp.localPosition.y + (spanFloors - 1) * (Statics.LevelHeight / 2), (Length - 0.458f) / 2);
                    ramp.localRotation = Quaternion.Euler(-rampAngle2, 0, -90);

                    // Recommended angle defining either a staircase or a ramp is 19 degrees.
                    if (rampAngle2 < Consts.RampAngle)
                    {
                        ramp.GetComponent<MeshRenderer>().material = Create.Instance.MaterialsOpaque[(int)Def.Mat.Floor];
                        ramp.GetComponent<MeshRenderer>().material.mainTextureScale = new Vector2(rampLength2 * 0.5f, 0.5f);
                    }

                    break;

                case Def.StairType.Escalator:
                    lowerGate.localPosition = new Vector3(lowerGate.localPosition.x, Statics.LevelHeight / 2.5f, lowerGate.localPosition.z);
                    upperGate.localPosition = new Vector3(upperGate.localPosition.x, Statics.LevelHeight / 2.5f + Statics.LevelHeight, upperGate.localPosition.z);
                    lowerGate.transform.localScale = new Vector3(lowerGate.transform.localScale.x,
                        Statics.LevelHeight / 10f, lowerGate.transform.localScale.z);
                    upperGate.transform.localScale = new Vector3(upperGate.transform.localScale.x,
                        Statics.LevelHeight / 10f, upperGate.transform.localScale.z);

                    escalatorSteps.transform.parent.localScale = new Vector3(escalatorSteps.transform.parent.localScale.x, 0.9526836f * (Statics.LevelHeight / 2.5f), escalatorSteps.transform.parent.localScale.z);
                    escalatorSteps.transform.parent.localPosition = new Vector3(escalatorSteps.transform.parent.localPosition.x, -2.34f * (Statics.LevelHeight / 2.5f), escalatorSteps.transform.parent.localPosition.z);
                    break;
            }
        }

        public bool PointInside(float x, float y)
        {
            if (!(x < xMax) || !(x > xMin)) return false;
            return y < yMax && y > yMin;
        }

        public void UpdateBoundaries(int level, int rotation)
        {


            while (rotation >= 360)
                rotation -= 360;

            while (rotation < 0)
                rotation += 360;

            //Debug.Log(rotation);

            this.level = level;
            this.rotation = rotation;
            float width1 = 0;
            float length1 = 0;

            switch (stairType)
            {
                case Def.StairType.HalfLanding:
                    width1 = Width * 2;
                    length1 = Length + WidthLanding;
                    break;
                case Def.StairType.Straight:
                case Def.StairType.Escalator:
                    width1 = Width;
                    length1 = Length;
                    break;
            }

            var c1Start = 0f;

            switch (rotation)
            {
                case 0:
                    xMin = transform.position.x;
                    xMax = xMin + width1;
                    yMin = transform.position.z;
                    yMax = yMin + length1;
                    c1Start = yMin;
                    break;
                case 90:
                    xMin = transform.position.x;
                    xMax = xMin + length1;
                    yMax = transform.position.z;
                    yMin = yMax - width1;
                    c1Start = xMin;
                    break;
                case 180:
                    xMax = transform.position.x;
                    xMin = xMax - width1;
                    yMax = transform.position.z;
                    yMin = yMax - length1;
                    c1Start = yMax;
                    break;
                case 270:
                    xMax = transform.position.x;
                    xMin = xMax - length1;
                    yMin = transform.position.z;
                    yMax = yMin + width1;
                    c1Start = xMax;
                    break;
                default:
                    Debug.LogError("What's the rotation of the stairs??");
                    break;
            }

            var yMinPlusYMax = yMin + yMax;

            switch (stairType)
            {
                case Def.StairType.HalfLanding:
                    float slopeHeight = Statics.FloorHeight + Statics.FloorOffset + Statics.LevelHeight * (level + 0.5f);
                    m1 = (Statics.LevelHeight / 2) / Length;
                    c1 = slopeHeight - m1 * (yMin + widthLanding);
                    m2 = -m1;
                    c2 = slopeHeight - m2 * (yMin + widthLanding);
                    break;
                case Def.StairType.Straight:
                case Def.StairType.Escalator:
                    m1 = Statics.LevelHeight * spanFloors / length1; //Gradient

                    if (rotation == 180 || rotation == 270)
                        m1 *= -1;

                    c1 = Statics.FloorHeight + Statics.FloorOffset +
                         Statics.LevelHeight * level - m1 * c1Start; //Intersection

                    break;
            }
        }

        public Vector3 PlaceOnStairway(Vector3 input)
        {
            float y = input.y;

            switch (stairType)
            {
                case Def.StairType.HalfLanding:
                    CpmPair rotatedPoint = RotatePoint(new CpmPair(input.x, input.z),
                        new CpmPair((xMax + xMin) / 2f, (yMax + yMin) / 2f), rotation);

                    if (rotatedPoint.Y < yMin + widthLanding)
                    {
                        // Place on landing
                        y = Statics.FloorHeight + Statics.FloorOffset + Statics.LevelHeight * (level + 0.5f);
                    }
                    else if (rotatedPoint.X > (xMax + xMin) / 2f)
                    {
                        // Place on right slope
                        y = m2 * rotatedPoint.Y + c2;
                    }
                    else
                    {
                        // Place on left slope
                        y = m1 * rotatedPoint.Y + c1;
                    }
                    break;
                case Def.StairType.Straight:
                case Def.StairType.Escalator:
                    switch (rotation)
                    {
                        case 0:
                        case 180:
                            y = m1 * input.z + c1;
                            break;
                        case 90:
                        case 270:
                            y = m1 * input.x + c1;
                            break;
                        default:
                            Debug.LogError("What's the rotation of the stairs??");
                            break;
                    }
                    break;
            }
            return new Vector3(input.x, y, input.z);
        }

        public void SetUpperGate(Transform t)
        {
            upperGate = t;
        }

        public void SetLowerGate(Transform t)
        {
            lowerGate = t;
        }

        internal Vector3 Centre()
        {
            return new Vector3(xMin + (xMax - xMin) / 2f, 0f, yMin + (yMax - yMin) / 2f);
        }

        public void FlipDirection()
        {
            if (stairDirection == Def.StairDirection.Bidirectional || stairDirection == Def.StairDirection.UpOnly)
                stairDirection = Def.StairDirection.DownOnly;
            else if (stairDirection == Def.StairDirection.DownOnly) stairDirection = Def.StairDirection.UpOnly;
            ApplyDirection();
        }

        public void ApplyDirection()
        {
            if (stairType != Def.StairType.Escalator)
                return;

            float speed = 0;

            if (stairDirection == Def.StairDirection.UpOnly)
                speed = -1;
            else if (stairDirection == Def.StairDirection.DownOnly)
                speed = 1;

            escalatorSteps.SetFloat("Direction", speed);
        }

        /// <summary>
        /// Rotates one point around another
        /// </summary>
        /// <param name="pointToRotate">The point to rotate.</param>
        /// <param name="centerPoint">The center point of rotation.</param>
        /// <param name="angleInDegrees">The rotation angle in degrees.</param>
        /// <returns>Rotated point</returns>
        static CpmPair RotatePoint(CpmPair pointToRotate, CpmPair centerPoint, double angleInDegrees)
        {
            double angleInRadians = angleInDegrees * (Math.PI / 180);
            double cosTheta = Math.Cos(angleInRadians);
            double sinTheta = Math.Sin(angleInRadians);
            return new CpmPair
            {
                X = (float)
                    (cosTheta * (pointToRotate.X - centerPoint.X) -
                     sinTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.X),
                Y = (float)
                    (sinTheta * (pointToRotate.X - centerPoint.X) +
                     cosTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.Y)
            };
        }
    }
}