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

        if (behaviour == Behaviour.Stationary) {
            return;
        }

        if (waypoints.Count < 1) {
            GameObject[] waypointGameObjects = GameObject.FindGameObjectsWithTag("Waypoint");
            foreach (GameObject obj in waypointGameObjects) {
                waypoints.Add(obj.transform);
            }
        }
        agent.destination = waypoints[0].transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if(behaviour==Behaviour.Patrol){
            HandlePatrol();
        }

        if(behaviour==Behaviour.Move){
            HandleMove();
        }
    }

    void HandlePatrol(){
        if (!agent.hasPath||agent.remainingDistance<=.6f) {
            agent.destination = waypoints[Random.Range(0, waypoints.Count)].transform.position;
        }
    }

    void HandleMove(){
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

    public enum Behaviour {Patrol, Move, Stationary};
}