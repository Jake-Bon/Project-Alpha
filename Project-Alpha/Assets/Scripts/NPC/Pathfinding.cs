using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Pathfinding : MonoBehaviour
{
    NavMeshAgent agent;
    public Behaviour behaviour;
    public List<Transform> waypoints;
    
    GameObject[] npcList;
    GameObject closest;

    Vector3 resetVector;
    float resetDistance;
    bool npcPushing = false;


    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.velocity = Vector3.zero;
        resetVector = new Vector3(0,-100,0);
        agent.destination= resetVector;
        closest = gameObject;

        if (behaviour == Behaviour.Stationary||behaviour == Behaviour.Actionless) {
            agent.speed = 1.0f;
            npcList = GameObject.FindGameObjectsWithTag("Neutral");
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
        }else if(behaviour==Behaviour.Move){
            HandleMove();
        }else if(behaviour==Behaviour.Actionless){
            HandleGravity();
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

    void HandleGravity(){
        closest = searchClosest();
        if(closest==null||npcPushing){
            agent.destination = resetVector;
            npcPushing = false;
            return;
        }
        Vector3 dest = transform.position;
        dest.x += (closest.transform.position.x-transform.position.x)/2;
        dest.z += (closest.transform.position.z-transform.position.z)/2;
        agent.destination = dest;

        if(agent.destination!=resetVector){
            if(closest==null)
                return;
            else{
                if((closest.transform.position-transform.position).sqrMagnitude<=(resetDistance*resetDistance)){
                    agent.destination = resetVector;
                    closest = null;
                    Debug.Log(closest.name);
                    agent.velocity = Vector3.zero;
                }
            }
        }
    }

    GameObject searchClosest(){
        float closestNum = Mathf.Infinity;
        float newNum = 0;
        GameObject chosen = null;
        Vector3 pos = transform.position;

        foreach(GameObject npc in npcList){
            if(npc==gameObject)
                continue;
            newNum = (npc.transform.position-pos).sqrMagnitude;
            if(closestNum>newNum){
                closestNum = newNum;
                chosen = npc;
            }
        }
        resetDistance = chosen.GetComponent<Strafe>().GetResetDistance();
        if((chosen.transform.position-transform.position).sqrMagnitude<=(resetDistance*resetDistance))
            return null;
        return chosen;
    }

    public void Pause() {
        agent.isStopped = true;
    }

    public void Resume() {
        agent.isStopped = false;
    }

    public void SetNPCPushing(){
        npcPushing = true;
    }

    public Vector3 GetResetVec(){
        return resetVector;
    }

    public enum Behaviour {Patrol, Move, Stationary, Actionless};
}