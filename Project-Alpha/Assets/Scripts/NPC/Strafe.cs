using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Strafe : MonoBehaviour
{
    [Header("Optional Field")]
    [SerializeField] private float resetDistance = -1.0f;
    bool isStrafe = false;
    Transform rotationHandler;

    public void Start(){
        if(resetDistance==-1.0f)
            resetDistance = GameObject.Find("Player").GetComponent<CrowdHandler>().GetPersonalSpace()+GetComponent<CapsuleCollider>().radius*2;
        rotationHandler = GameObject.Find("RotationHandler").GetComponent<Transform>();
    }

    public void DoStrafe(StrafeInfo info){
        if(isStrafe){
            return;
        }
        isStrafe = true;
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        rotationHandler.forward = info.source.right;
        if(!info.isRight){
            rotationHandler.Rotate(0, 180.0f, 0, Space.Self);
        }
        agent.velocity = Vector3.ClampMagnitude(agent.velocity + rotationHandler.forward.normalized*(info.speed/3)*((resetDistance-info.magnitude)*(resetDistance-info.magnitude)),4.5f);
        Debug.Log("Why not move + " + info.isRight);
        isStrafe=false;

        if((transform.position-info.source.position).sqrMagnitude>=resetDistance)
            agent.velocity = Vector3.zero;
    }
}
