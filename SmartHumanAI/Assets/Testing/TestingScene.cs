using System;
using System.Collections.Generic;
using Domain;
using Helper;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Tests
{
    public class TestingScene : MonoBehaviour
    {
        public GameObject DefaultGameObject;
        public int numAgents = 20;
        public List<Transform> gates = new List<Transform>();
        private List<Transform> agents = new List<Transform>();

        public LineFactory lineFactory;

        // Update is called once per frame
        void Update () {
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                Debug.Log("Organising queues.");

                foreach (var agent in agents)
                {
                    Destroy(agent.gameObject);
                }

                agents.Clear();
                
                for (int i = 0; i < numAgents; i++)
                {
                    Vector3 randomLocation = new Vector3(Random.Range(-4.72f, 4.72f), 0f, Random.Range(-2.22f, 2.22f)) + new Vector3(5f, 0f, 3f);
                    int count = 0;

                    while (Collides(randomLocation))
                    {
                        randomLocation = new Vector3(Random.Range(-4.72f, 4.72f), 0f, Random.Range(-2.22f, 2.22f)) + new Vector3(5f, 0f, 3f);

                        if (count++ > 1000)
                        {
                            Debug.LogError("Couldn't find a spot");
                            return;
                        }
                    }

                    var newAgent = Instantiate(DefaultGameObject, randomLocation, Quaternion.identity);

                    agents.Add(newAgent.transform);
                }

                
            } else if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                lineFactory.transform.position = Vector3.zero;
                lineFactory.transform.rotation = Quaternion.Euler(Vector3.zero);
                var activeLines = lineFactory.GetActive();
                foreach (var line in activeLines)
                    line.gameObject.SetActive(false);

                List<CpmPair> gatesCPM = new List<CpmPair>();
                List<CpmPair> agentsCPM = new List<CpmPair>();

                foreach (var gate in gates)
                    gatesCPM.Add(new CpmPair(gate.position.x, gate.position.z));

                foreach (var agent in agents)
                    agentsCPM.Add(new CpmPair(agent.position.x, agent.position.z));

                List<List<int>> queues = QueueFormer.Execute(gatesCPM, agentsCPM);

                for (int i = 0; i < queues.Count; i++)
                    DrawQueue(queues[i], i);

                lineFactory.transform.position = new Vector3(0, 0.1f, 0);
                lineFactory.transform.rotation = Quaternion.Euler(new Vector3(90f, 0, 0));
            }
        }

        private void DrawQueue(List<int> queue, int gateID)
        {
            for (int i = 0; i < queue.Count; i++)
            {
                if (i == 0)
                {
                    lineFactory.GetLine(new Vector2(gates[gateID].position.x, gates[gateID].position.z),
                        new Vector2(agents[queue[i]].position.x, agents[queue[i]].position.z), 0.02f, Color.blue);
                }
                else
                {
                    lineFactory.GetLine(new Vector2(agents[queue[i - 1]].position.x, agents[queue[i - 1]].position.z),
                        new Vector2(agents[queue[i]].position.x, agents[queue[i]].position.z), 0.02f, Color.blue);
                }
            }
        }

        private bool Collides(Vector3 randomLocation)
        {
            foreach (var agent in agents)
            {
                if (Vector3.Distance(agent.position, randomLocation) < 0.5f)
                    return true;
            }
            return false;
        }
    }
}
