using Info;
using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.ObjectsInfo;

public class TicketGateInfo : BaseObject
{
    public int ObjectId = -1;
    public int level = -1;
    public int rotation = 0;
    public bool isBidirectional = false;

    public TextMesh GateDirTextMesh;
    public float waitTime = 1.5f;
    public Transform gate;
    public Transform waitingPosition;
    public Transform targetPosition;
    public Transform waitingPosition2;
    public Transform targetPosition2;
    public List<Transform> barricades = new List<Transform>();

    public override void UpdateInfo()
    {
        level = Create.Instance.SelectedLevel;
        ObjectId = gate.GetComponent<GateInfo>().Id;
        setBiDir();
    }

    internal void UpdateSize(float ticketGateWidth)
    {
        gate.GetComponent<GateInfo>().UpdateLength(ticketGateWidth);
        gate.localPosition = new Vector3(-0.5f + ticketGateWidth / 2f, gate.localPosition.y, gate.localPosition.z);

        if (barricades.Count < 4)
            Debug.LogError("Not enough barricades attached!");
        else
        {
            barricades[1].localPosition = new Vector3(ticketGateWidth - 0.5f, barricades[1].localPosition.y, barricades[1].localPosition.z);
            barricades[2].localPosition = new Vector3(-0.5f + ticketGateWidth / 2f, barricades[2].localPosition.y, barricades[2].localPosition.z);
            barricades[3].localPosition = new Vector3(-0.5f + ticketGateWidth / 2f, barricades[3].localPosition.y, barricades[3].localPosition.z);

            barricades[0].GetComponent<WallInfo>().UpdateLength(1.4f);
            barricades[1].GetComponent<WallInfo>().UpdateLength(1.4f);
            barricades[2].GetComponent<WallInfo>().UpdateLength(ticketGateWidth);
            barricades[3].GetComponent<WallInfo>().UpdateLength(ticketGateWidth);

            barricades[0].GetComponent<WallInfo>().IsBarricade = true;
            barricades[1].GetComponent<WallInfo>().IsBarricade = true;
            barricades[2].GetComponent<WallInfo>().IsBarricade = true;
            barricades[3].GetComponent<WallInfo>().IsBarricade = true;
        }

        foreach (Transform child in transform)
        {
            switch (child.name)
            {
                case "Gate2":
                    child.localPosition = new Vector3(ticketGateWidth - 0.5f, child.localPosition.y, child.localPosition.z);
                    break;
                case "WaitingPosition":
                case "GateDirection":
                case "TargetPosition":
                    child.localPosition = new Vector3(-0.5f + ticketGateWidth / 2f, child.localPosition.y, child.localPosition.z);
                    break;
            }
        }
    }

    public void toggleBidir()
    {
        isBidirectional = !isBidirectional;
        setBiDir();
    }

    public void setBiDir()
    {
        GateDirTextMesh.text = isBidirectional ? "↕" : "↓";
    }

    public override Vector3 PositionOffset()
    {
        return Vector3.zero;
    }

    public override void EndClick()
    {
        gate.SetParent(Create.Instance.CurrentLevelTransform());
        gate.GetComponent<GateInfo>().UpdateInfo();

        foreach (var barricade in barricades)
        {
            barricade.SetParent(Create.Instance.CurrentLevelTransform());
            barricade.GetComponent<WallInfo>().UpdateInfo();
        }

        rotation = (int)transform.localEulerAngles.y;
        UpdateInfo();

        // Check if theres a wall connected and destroy the wall-end pole.
        foreach (var wall in FindObjectsOfType<WallInfo>())
        {
            foreach (var vertex in gate.GetComponent<GateInfo>().Vertices)
            {
                if (vertex.id == wall.Vertices[0].id)
                    Destroy(wall.transform.GetChild(0).GetComponent<MeshRenderer>());
                if (vertex.id == wall.Vertices[1].id)
                    Destroy(wall.transform.GetChild(1).GetComponent<MeshRenderer>());
            }
        }
    }

    public override void StartClick()
    {
        throw new NotImplementedException();
    }
}
