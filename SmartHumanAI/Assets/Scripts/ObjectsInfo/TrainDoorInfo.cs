using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataFormats;
using Domain;
using Info;
using UnityEngine;

public class TrainDoorInfo : MonoBehaviour
{
    public int trainID = -1;
	public List<Vector3> waitingPositions = new List<Vector3>();

    public void UpdateInfo(int trainID)
    {
        this.trainID = trainID;

        GetComponent<GateInfo>().IsTrainDoor = true;
    }

    public TrainDoorData GetTrainData()
    {
        TrainDoorData trainData = new TrainDoorData();

        foreach (var waitingPos in GetComponent<TrainDoorInfo>().waitingPositions)
            trainData.waitingPositions.Add(Vector3s.Convert(waitingPos));
        trainData.trainID = trainID;

        return trainData;
    }

    public void CopyFrom(TrainDoorInfo getComponent)
    {
        trainID = getComponent.trainID;
        waitingPositions.Clear();
        waitingPositions = getComponent.waitingPositions.ToArray().ToList();
    }

    public void OnDrawGizmos()
    {
        foreach (var point in waitingPositions)
            Gizmos.DrawSphere(point, 0.1f);
    }
}
