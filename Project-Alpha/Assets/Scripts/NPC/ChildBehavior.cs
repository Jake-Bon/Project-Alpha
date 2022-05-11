using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChildBehavior : MonoBehaviour
{
    UnityEngine.AI.NavMeshAgent agent;

    private ChildStatus behaviour;
    private GameObject player;
    private Player playerVis;

    [Header("Child Parameters")]
    [SerializeField] private float followDistance = 0.0f;
    [SerializeField] private float refreshRate = 0.0f;
    [SerializeField] private float defaultRoamTime = 10.0f;
    [SerializeField] private Transform spawnpoint;

    private float lastRefresh = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        player = GameObject.Find("Player");
        playerVis = player.GetComponent<Player>();
    }

    // Update is called once per frame
    void Update()
    {
        if(behaviour==ChildStatus.Default)
            HandleDefault();
        else if(behaviour==ChildStatus.Close)
            HandleClose();
        else
            HandleLeftBehind();
    }

    public void HandleDefault(){
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

    public void HandleClose(){

    }

    public void HandleLeftBehind(){

    }

    public void Pause() {
        agent.isStopped = true;
    }

    public void Resume() {
        agent.isStopped = false;
    }

    public void ResetChild(){
        agent.destination = player.transform.position;
        transform.position = spawnpoint.position;
    }

    public enum ChildStatus {Default,Close,LeftBehind};
}
