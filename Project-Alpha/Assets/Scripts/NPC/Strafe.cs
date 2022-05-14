using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Strafe : MonoBehaviour
{
    public void DoStrafe(StrafeInfo info){
        Debug.Log("Attempting Strafe... Direction: " + info.isRight);
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if(info.isRight){
            agent.velocity = info.source.right.normalized * 10.0f;
            transform.forward = info.source.right;
        }else{
            //agent.velocity = 20.0;
            info.source.Rotate(0, 180.0f, 0, Space.Self);
            agent.velocity = info.source.right.normalized * 10.0f;
            transform.forward = info.source.right;
        }
        StartCoroutine(EndStrafe(agent, new Vector3(transform.position.x,transform.position.y,transform.position.z)));
    }

    IEnumerator EndStrafe(UnityEngine.AI.NavMeshAgent agent,Vector3 initPos){
        while ((transform.position-initPos).sqrMagnitude<4.0f) {
            yield return null;
        }
        agent.velocity = Vector3.zero;
    }
}
