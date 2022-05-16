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
                float magnitude = (transform.position-npcList[i].transform.position).sqrMagnitude;
                if(magnitude<=personalSpace){
                    if (transform.InverseTransformPoint(npcList[i].transform.position).x > 0)
                    {
                        npcList[i].SendMessage("DoStrafe",new StrafeInfo(true,transform,magnitude));
                    }else{
                        npcList[i].SendMessage("DoStrafe",new StrafeInfo(false,transform,magnitude));
                    }
                }
            }
        }else{
            for(int i = 0; i<npcList.Length;i++){
                Transform npcTransform = npcList[ptr].transform;
                float magnitude = (npcTransform.position-npcList[i].transform.position).sqrMagnitude;
                if(ptr!=i&&magnitude<=personalSpace){
                    if (npcTransform.InverseTransformPoint(npcList[i].transform.position).x > 0)
                    {
                        npcList[i].SendMessage("DoStrafe",new StrafeInfo(true,npcTransform,3.5f));
                    }else{
                        npcList[i].SendMessage("DoStrafe",new StrafeInfo(false,npcTransform,3.5f));
                    }
                }
            }
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
    public float magnitude;
    public StrafeInfo(bool b, Transform t, float m){
        isRight = b;
        source = t;
        magnitude = m;
    }
}
