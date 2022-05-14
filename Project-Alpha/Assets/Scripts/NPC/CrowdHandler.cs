using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrowdHandler : MonoBehaviour
{   
    GameObject[] npcList;
    //Transform player;
    int ptr;

    [SerializeField] private double personalSpace = 3.0f;
    [SerializeField] private double capsuleDiameter = 1.0f;
    
    // Start is called before the first frame update
    void Start()
    {
        //player = GameObject.Find("Player").GetComponent<Transform>();
        npcList = GameObject.FindGameObjectsWithTag("Neutral");
        ptr = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if(ptr==npcList.Length){
            for(int i = 0; i<npcList.Length;i++){
                if((transform.position-npcList[i].transform.position).sqrMagnitude<=personalSpace){
                    if (transform.InverseTransformPoint(npcList[i].transform.position).x > 0)
                    {
                        npcList[i].SendMessage("DoStrafe",new StrafeInfo(true,transform));
                    }else{
                        npcList[i].SendMessage("DoStrafe",new StrafeInfo(false,transform));
                    }
                }
            }
        }else{

        }
        incrRoundRobin();
    }

    void incrRoundRobin(){
        ptr++;
        if(ptr==npcList.Length+1)
            ptr=0;
    }
}

public class StrafeInfo{
    public bool isRight;
    public Transform source;
    public StrafeInfo(bool b, Transform t){
        isRight = b;
        source = t;
    }
}
