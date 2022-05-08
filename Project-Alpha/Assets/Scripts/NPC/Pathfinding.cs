using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Pathfinding : MonoBehaviour
{
    NavMeshAgent agent;
    public Behaviour behaviour;
    public List<Transform> waypoints;

    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (waypoints.Count < 1) {
            GameObject[] waypointGameObjects = GameObject.FindGameObjectsWithTag("Waypoint");
            foreach (GameObject obj in waypointGameObjects) {
                waypoints.Add(obj.transform);
            }
        }
        agent.destination = waypoints[0].transform.position;
        if (behaviour == Behaviour.Stationary) {
            Pause();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!agent.hasPath) {
            agent.destination = waypoints[Random.Range(0, waypoints.Count)].transform.position;
        }
    }

    public void Pause() {
        agent.isStopped = true;
    }

    public void Resume() {
        agent.isStopped = false;
    }

    public enum Behaviour {Patrol, Move, Follow, Stationary};
}
