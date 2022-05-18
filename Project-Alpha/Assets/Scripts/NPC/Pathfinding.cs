using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Pathfinding : MonoBehaviour
{
    NavMeshAgent agent;
    public Behaviour behaviour;
    public List<Transform> waypoints;

    public float moveSpeed = 3.0f;
    public float patrolSpeed = 5.0f;
    public float pursuitSpeed = 5.0f;


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
        if(behaviour==Behaviour.Patrol){
            HandlePatrol();
        }

        if(behaviour==Behaviour.Move){
            HandleMove();
        }

        if (behaviour == Behaviour.Pursuit) {
            HandlePursuit();
        }
    }

    void HandlePatrol(){
        agent.speed = patrolSpeed;
        if (!agent.hasPath) {
            agent.destination = waypoints[Random.Range(0, waypoints.Count)].transform.position;
        }
    }

    void HandleMove(){
        agent.speed = moveSpeed;
        if (!agent.hasPath) {
            agent.destination = waypoints[Random.Range(0, waypoints.Count)].transform.position;
        }
    }

    void HandlePursuit() {
        // Move to player's location while in sight
    }

    void HandleInvestigate() {
        // Move to player's last known location then look around
    }

    public void Pause() {
        agent.isStopped = true;
    }

    public void Resume() {
        agent.isStopped = false;
    }

    public enum Behaviour {Patrol, Move, Stationary, Pursuit, Investigate};
}