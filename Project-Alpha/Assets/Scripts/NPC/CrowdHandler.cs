using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrowdHandler : MonoBehaviour
{   
    GameObject[] intermediateNpcList;
    List<GameObject> npcList;
    List<bool> npcAlignment;

    GameObject[] intermediateEnemyList;
    List<Transform> enemyList;

    ThirdPersonController player;
    int ptr;
    bool npcsInit = false;
    bool setupDone = false;

    [SerializeField] private int npcUpdatesPerFrame = 10;
    [SerializeField] private float personalSpace = 3.0f;
    [SerializeField] private bool allowInterEnemyStrafe;
    private int updatesPerFrame;

    List<float> capsuleDiameterNPCs;
    List<float> capsuleDiameterEnemies;
    
    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.Find("Player").GetComponent<ThirdPersonController>();
        intermediateNpcList = GameObject.FindGameObjectsWithTag("Neutral");
        intermediateEnemyList = GameObject.FindGameObjectsWithTag("Enemy");

        enemyList = new List<Transform>();
        foreach(GameObject enemy in intermediateEnemyList){
            enemyList.Add(enemy.GetComponent<Transform>());
        }
    }

    IEnumerator ResetCrowdHandler(){ //Call on cam changes if we disable npcs out of view?
        yield return new WaitForSeconds(.1f);
        npcList = new List<GameObject>();
        npcAlignment = new List<bool>();
        capsuleDiameterNPCs = new List<float>();
        capsuleDiameterEnemies = new List<float>();
       
        int i = 0;
        foreach(GameObject npc in intermediateNpcList){
            if(npc.GetComponent<Renderer>().isVisible){
                capsuleDiameterNPCs.Add(npc.GetComponent<CapsuleCollider>().radius*2);
                if(npc.GetComponent<Enemy>()!=null)
                    npcAlignment.Add(true);
                else
                    npcAlignment.Add(false);
                i++;
                npcList.Add(npc);
            }
        }

        foreach(GameObject enemy in intermediateEnemyList){
            capsuleDiameterEnemies.Add(enemy.GetComponent<CapsuleCollider>().radius*2);
        }

        updatesPerFrame = npcUpdatesPerFrame;

        if(npcUpdatesPerFrame>npcList.Count){
            updatesPerFrame = npcList.Count;
        }
        ptr = 0;
        setupDone = true;
        yield return null;
    }

    // Update is called once per frame
    void Update()
    {
        if(!npcsInit){
            npcsInit = true;
            StartCoroutine(ResetCrowdHandler());
        }else if(setupDone){
            doPlayerStrafing();
            for(int i = 0; i<enemyList.Count;i++){
                doEnemyStrafing(i);
            }
            for(int i = 0; i<updatesPerFrame;i++){
                doNPCStrafing();
            }
        }
    }

    void doPlayerStrafing(){
        for(int i = 0; i<npcList.Count;i++){
            float magnitude = (transform.position-npcList[i].transform.position).sqrMagnitude;
            if(!(npcAlignment[i])&&magnitude<=personalSpace+capsuleDiameterNPCs[i]){
                if (transform.InverseTransformPoint(npcList[i].transform.position).x > 0)
                {
                    npcList[i].SendMessage("DoStrafe",new StrafeInfo(true,transform,magnitude,(player.GetSpeed())));
                }else{
                    npcList[i].SendMessage("DoStrafe",new StrafeInfo(false,transform,magnitude,(player.GetSpeed())));
                }
            }
        }
    }

    void doEnemyStrafing(int num){
        for(int i = 0; i<npcList.Count;i++){
            float magnitude = (enemyList[num].position-npcList[i].transform.position).sqrMagnitude;
            bool ignore = (enemyList[num].GetComponent<Pathfinding>()==null||enemyList[num].GetComponent<Pathfinding>().behaviour==Pathfinding.Behaviour.Stationary)&&npcList[i].GetComponent<Pathfinding>()!=null;
            if(!ignore&&magnitude<=personalSpace+capsuleDiameterNPCs[i]){
                if (enemyList[num].InverseTransformPoint(npcList[i].transform.position).x > 0)
                {
                    npcList[i].SendMessage("DoStrafe",new StrafeInfo(true,enemyList[num],3.5f,2.5f));
                }else{
                    npcList[i].SendMessage("DoStrafe",new StrafeInfo(false,enemyList[num],3.5f,2.5f));
                }
            }
        }

        if(!allowInterEnemyStrafe)
            return;
        for(int j = 0;j<enemyList.Count;j++){
            if(j==num)
                continue;
            float magnitude = (enemyList[num].position-enemyList[j].transform.position).sqrMagnitude;
            bool ignore = (enemyList[num].GetComponent<Pathfinding>()==null||enemyList[num].GetComponent<Pathfinding>().behaviour==Pathfinding.Behaviour.Stationary)&&enemyList[j].GetComponent<Pathfinding>()!=null;
            if(!ignore&&magnitude<=personalSpace+capsuleDiameterEnemies[j]){
                if (enemyList[num].InverseTransformPoint(enemyList[j].transform.position).x > 0)
                {
                    enemyList[j].SendMessage("DoStrafe",new StrafeInfo(true,enemyList[num],3.5f,2.5f));
                }else{
                    enemyList[j].SendMessage("DoStrafe",new StrafeInfo(false,enemyList[num],3.5f,2.5f));
                }
            }
        }
    }

    void doNPCStrafing(){
        for(int i = 0; i<npcList.Count;i++){
            Transform npcTransform = npcList[ptr].transform;
            float magnitude = (npcTransform.position-npcList[i].transform.position).sqrMagnitude;
            bool ignore = (npcList[ptr].GetComponent<Pathfinding>()==null||npcList[ptr].GetComponent<Pathfinding>().behaviour==Pathfinding.Behaviour.Stationary)&&npcList[i].GetComponent<Pathfinding>()!=null&&(npcList[i].GetComponent<Pathfinding>().behaviour==Pathfinding.Behaviour.Patrol||npcList[i].GetComponent<Pathfinding>().behaviour==Pathfinding.Behaviour.Move);
            if(ptr!=i&&!ignore&&magnitude<=personalSpace+capsuleDiameterNPCs[i]){
                if (npcTransform.InverseTransformPoint(npcList[i].transform.position).x > 0)
                {
                    npcList[i].SendMessage("DoStrafe",new StrafeInfo(true,npcTransform,3.5f,2.5f));
                }else{
                    npcList[i].SendMessage("DoStrafe",new StrafeInfo(false,npcTransform,3.5f,2.5f));
                }
            }
        }
        IncrRoundRobin();
    }

    void IncrRoundRobin(){
        ptr++;
        if(ptr==npcList.Count)
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
