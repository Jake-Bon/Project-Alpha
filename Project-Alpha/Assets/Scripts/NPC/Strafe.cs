using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Strafe : MonoBehaviour
{
    [Header("Optional Field")]
    [SerializeField] private float resetDistance = -1.0f;
    ThirdPersonController player;
    bool isStrafe = false;
    public void Start(){
        player = GameObject.Find("Player").GetComponent<ThirdPersonController>();
        if(resetDistance==-1.0f)
            resetDistance = GameObject.Find("Player").GetComponent<CrowdHandler>().GetResetDist();
    }

    public void DoStrafe(StrafeInfo info){
        if(isStrafe){
            return;
        }
        isStrafe = true;
        Debug.Log("Attempting Strafe... Direction: " + info.isRight);
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        transform.forward = info.source.right;
        if(!info.isRight){
            transform.Rotate(0, 180.0f, 0, Space.Self);
        }
        agent.velocity = transform.forward.normalized*(player.GetSpeed()/3)*((resetDistance-info.magnitude));
        //StartCoroutine(EndStrafe(agent, new Vector3(transform.position.x,transform.position.y,transform.position.z)));
        isStrafe=false;

        if((transform.position-info.source.position).sqrMagnitude>=resetDistance)
            agent.velocity = Vector3.zero;
    }

    IEnumerator EndStrafe(UnityEngine.AI.NavMeshAgent agent,Vector3 initPos){
        while ((transform.position-initPos).sqrMagnitude<4.0f) {
            yield return null;
        }
        
    }
}
