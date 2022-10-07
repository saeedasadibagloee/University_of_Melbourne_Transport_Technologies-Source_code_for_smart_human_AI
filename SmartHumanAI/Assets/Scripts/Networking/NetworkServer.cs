using Assets.Scripts.Networking;
using Core;
using DataFormats;
using Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using Telepathy;
using TMPro;
using UnityEngine;

public enum MsgType { CONNECT = 1, DISCONNECT, STATUS, RESET, MODEL, USER, AGENTS, HEATMAP, PARAMETERS, DISPLAY, TIME, WALL};

public class NetworkServer : MonoBehaviour
{
    private const int port = 54321;
    public TextMeshProUGUI StatusText;
    public UnityEngine.UI.Text ButtonText;
    public bool isConnected = false;

    Server server = new Server();
    private bool isListening = false;

    private static NetworkServer _instance = null;
    private string networkText = "";
    private bool updateNetworkText = false;
    private const bool debug = false;

    XmlSerializer serializer = new XmlSerializer(typeof(Model));

    public static NetworkServer Instance
    {
        get
        {
            return _instance ?? (_instance = FindObjectOfType<NetworkServer>());
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        StatusText.text = "";
    }

    void Update()
    {
        if (updateNetworkText)
        {
            updateNetworkText = false;
            StatusText.text = networkText;
        }

        if (server.Active)
        {
            foreach (var client in server.GetClients())
            {
                int q = client.sendQueue.Count;
                if (q > 0) Debug.Log("Server send requests waiting in queue: " + q);
            }

            // show all new messages
            Message msg;
            while (server.GetNextMessage(out msg))
            {
                switch (msg.eventType)
                {
                    case Telepathy.EventType.Connected:
                        isConnected = true;
                        networkText = "Connected to HoloLens 2.";
                        updateNetworkText = true;
                        SendNetworkItem(MsgType.CONNECT, "PeakHour Crowd " + Application.version);
                        Params.Current.UiUpdateCycle = 100; // Receive less data to send to HoloLens
                        break;
          
                }
            }
        }
    }

    void Awake()
    {
        // update even if window isn't focused, otherwise we don't receive.
        Application.runInBackground = true;

        // use Debug.Log functions for Telepathy so we can see it in the console
        Telepathy.Logger.Log = Debug.Log;
        Telepathy.Logger.LogWarning = Debug.LogWarning;
        Telepathy.Logger.LogError = Debug.LogError;
    }

    internal void ToggleStatus()
    {
        isListening = !isListening;

        if (ButtonText != null)
            ButtonText.text = isListening ? "Stop HoloLens Connection" : "Connect to HoloLens";

     }

    public void StartListening()
    {
        server.Start(54321);

        IPAddress[] ipv4Addresses = Array.FindAll(
            Dns.GetHostEntry(Dns.GetHostName()).AddressList,
            a => a.AddressFamily == AddressFamily.InterNetwork);

        string ipaddresses = "";

        foreach (var item in ipv4Addresses)
        {
            if (ipv4Addresses.Length > 1 && item.Equals("127.0.0.1"))
                continue;
            ipaddresses += item + ", ";
        }

        ipaddresses = ipaddresses.Remove(ipaddresses.Length - 2);

        UpdateUIStatus("Listening on " + ipaddresses);
    }

    private void StopListening()
    {
        server.Stop();
        UpdateUIStatus("");
    }

    private void UpdateUIStatus(string v)
    {
        if (!string.IsNullOrWhiteSpace(v))
            Debug.Log(v);
        networkText = v;
        updateNetworkText = true;
    }

    internal void SendNetworkItem(MsgType msgType, string message)
    {
        string s = (int)msgType + "~" + message;
        server.SendAll(Encoding.UTF8.GetBytes(s));
        if (debug) Debug.Log("Sent " + msgType.ToString() + ": " + message);
    }

    internal void SendDisplayUpdate(bool displayAll, bool displayGrid, int displayLevel)
    {
        if (!isConnected)
            return;
        string msg = (displayAll ? 1 : 0) + "," + (displayGrid ? 1 : 0) + "," + displayLevel;
        SendNetworkItem(MsgType.DISPLAY, msg);
    }

    internal void SendModel(Model currentModel)
    {
        if (!isConnected)
            return;

        Debug.Log("Send Model");
        StringWriter sWriter = new StringWriter();
        serializer.Serialize(XmlWriter.Create(sWriter, new XmlWriterSettings { Indent = false, OmitXmlDeclaration = true }), currentModel);
        SendNetworkItem(MsgType.MODEL, sWriter.ToString());
    }

    internal void UpdateSimStatus(int status)
    {
        SendNetworkItem(MsgType.STATUS, status.ToString());
    }

    internal void AgentUpdate(AgentUpdatePackage agPkg)
    {
        var agPkgS = new AgentUpdatePackageSmall // X.000 of precision
        {
            id = agPkg.agent_id,
            r = (uint)Mathf.RoundToInt(agPkg.radius * 1000),
            x = (uint)Mathf.RoundToInt(agPkg.x * 1000),
            y = (uint)Mathf.RoundToInt(agPkg.y * 1000),
            loc = agPkg.location,
            lId = agPkg.levelId,
            lIdr = agPkg.levelIdreal,
            gx = (uint)Mathf.RoundToInt(agPkg.gate_x * 1000),
            gy = (uint)Mathf.RoundToInt(agPkg.gate_y * 1000),
            a = agPkg.isActive,
            c = Color3.Convert(agPkg.color),
            t = agPkg.type,
            gC = agPkg.generationCycle,
            eT = (uint)Mathf.RoundToInt(agPkg.evacuationTime * 1000),
            hjup = agPkg.HasJustUpdatedPath,
            hjud = agPkg.HasJustUpdatedDecision
        };

        string s = JsonUtility.ToJson(agPkgS);
        SendNetworkItem(MsgType.AGENTS, s);
    }

    internal void SendHeatmap(HeatmapData heatmapData)
    {
        if (!isConnected)
            return;

        var heatmapString = JsonUtility.ToJson(new HeatmapDataH(heatmapData));
        //Debug.Log("Send Heatmap: " + heatmapString);
        SendNetworkItem(MsgType.HEATMAP, heatmapString);
    }
}