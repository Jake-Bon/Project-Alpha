using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Pathfinding : MonoBehaviour
{
    NavMeshAgent agent;
    public Behaviour behaviour;
    public List<Transform> waypoints;

    [Header("Child Parameters")]
    [SerializeField] private float followDistance = 0.0f;
    [SerializeField] private float refreshRate = 0.0f;
    [SerializeField] private Transform spawnpoint;

    private GameObject player;
    private float lastRefresh = 0.0f;


    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if(behaviour==Behaviour.Follow){
            player = GameObject.Find("Player");
            waypoints.Add(player.transform);
        }
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
            handlePatrol();
        }

        if(behaviour==Behaviour.Move){
            handleMove();
        }

        if(behaviour==Behaviour.Follow){
            handleFollow();
        }
    }

    void handlePatrol(){
        if (!agent.hasPath) {
            agent.destination = waypoints[Random.Range(0, waypoints.Count)].transform.position;
        }
    }

    void handleMove(){
        if (!agent.hasPath) {
            agent.destination = waypoints[Random.Range(0, waypoints.Count)].transform.position;
        }
    }

    void handleFollow(){//Child
        //if(a-b.sqrMagnitude<=dist) do not move
        
        if((player.transform.position-gameObject.transform.position).sqrMagnitude<=followDistance){
            Pause();
        }else{
            if(Time.time-lastRefresh>=refreshRate){
                agent.destination = player.transform.position;
                lastRefresh = Time.time;
            }
            Resume();
        }
    }

    public void Pause() {
        agent.isStopped = true;
    }

    public void Resume() {
        agent.isStopped = false;
    }

    public void resetChild(){
        agent.destination = player.transform.position;
        transform.position = spawnpoint.position;
    }

    public enum Behaviour {Patrol, Move, Follow, Stationary};
}