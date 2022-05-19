using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrowdHandler : MonoBehaviour
{   
    GameObject[] intermediateNpcList;
    List<GameObject> npcList;
    List<bool> npcAlignment;
    ThirdPersonController player;
    int ptr;
    bool npcsInit = false;

    [SerializeField] private int npcUpdatesPerFrame = 10;
    [SerializeField] private float personalSpace = 3.0f;
    private int updatesPerFrame;

    List<float> capsuleDiameter;
    
    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.Find("Player").GetComponent<ThirdPersonController>();
        intermediateNpcList = GameObject.FindGameObjectsWithTag("Neutral");
    }

    IEnumerator ResetCrowdHandler(){ //Call on cam changes if we disable npcs out of view?
        yield return new WaitForSeconds(.1f);
        npcList = new List<GameObject>();
        npcAlignment = new List<bool>();
        capsuleDiameter = new List<float>();
       
        int i = 0;
        foreach(GameObject npc in intermediateNpcList){
            if(npc.GetComponent<Renderer>().isVisible){
                capsuleDiameter.Add(npc.GetComponent<CapsuleCollider>().radius*2);
                if(npc.GetComponent<Enemy>()!=null)
                    npcAlignment.Add(true);
                else
                    npcAlignment.Add(false);
                i++;
                npcList.Add(npc);
            }
        }

        updatesPerFrame = npcUpdatesPerFrame;

        if(npcUpdatesPerFrame>npcList.Count){
            updatesPerFrame = npcList.Count;
        }
        ptr = 0;
        yield return null;
    }

    // Update is called once per frame
    void Update()
    {
        if(!npcsInit){
            Debug.Log("e");
            npcsInit = true;
            StartCoroutine(ResetCrowdHandler());
        }
        for(int i = 0; i<updatesPerFrame;i++){
            doStrafing();
        }
    }

    void doStrafing(){
        Debug.Log(ptr + " " + npcList.Count);
        if(ptr==npcList.Count){
            for(int i = 0; i<npcList.Count;i++){
                float magnitude = (transform.position-npcList[i].transform.position).sqrMagnitude;
                if(!(npcAlignment[i])&&magnitude<=personalSpace+capsuleDiameter[i]){
                    if (transform.InverseTransformPoint(npcList[i].transform.position).x > 0)
                    {
                        npcList[i].SendMessage("DoStrafe",new StrafeInfo(true,transform,magnitude,(player.GetSpeed())));
                    }else{
                        npcList[i].SendMessage("DoStrafe",new StrafeInfo(false,transform,magnitude,(player.GetSpeed())));
                    }
                }
            }
        }else{
            for(int i = 0; i<npcList.Count;i++){
                Transform npcTransform = npcList[ptr].transform;
                float magnitude = (npcTransform.position-npcList[i].transform.position).sqrMagnitude;
                bool ignore = (npcList[ptr].GetComponent<Pathfinding>()==null||npcList[ptr].GetComponent<Pathfinding>().behaviour==Pathfinding.Behaviour.Stationary)&&npcList[i].GetComponent<Pathfinding>()!=null&&(npcList[i].GetComponent<Pathfinding>().behaviour==Pathfinding.Behaviour.Patrol||npcList[i].GetComponent<Pathfinding>().behaviour==Pathfinding.Behaviour.Move);
                if(ptr!=i&&!ignore&&magnitude<=personalSpace+capsuleDiameter[i]){
                    if (npcTransform.InverseTransformPoint(npcList[i].transform.position).x > 0)
                    {
                        npcList[i].SendMessage("DoStrafe",new StrafeInfo(true,npcTransform,3.5f,2.5f));
                    }else{
                        npcList[i].SendMessage("DoStrafe",new StrafeInfo(false,npcTransform,3.5f,2.5f));
                    }
                }
            }
        }
        IncrRoundRobin();
    }

    void IncrRoundRobin(){
        ptr++;
        if(ptr==npcList.Count+1)
            ptr=0;
    }

    public float GetPersonalSpace(){
        return personalSpace;
    }
}

public class StrafeInfo{
    public bool isRight;
    public Transform source;
    public float magnitude;
    public float speed;
    public StrafeInfo(bool b, Transform t, float m, float s){
        isRight = b;
        source = t;
        magnitude = m;
        speed = s;
    }
}
