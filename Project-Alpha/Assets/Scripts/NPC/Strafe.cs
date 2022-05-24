using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Strafe : MonoBehaviour
{
    [Header("Optional Field")]
    [SerializeField] private float resetDistance = -1.0f;
    Transform rotationHandler;
    Pathfinding path;

    public void Start(){
        if(resetDistance==-1.0f)
            resetDistance = GameObject.Find("Player").GetComponent<CrowdHandler>().GetPersonalSpace()+GetComponent<CapsuleCollider>().radius*2;
        rotationHandler = GameObject.Find("RotationHandler").GetComponent<Transform>();
        path = GetComponent<Pathfinding>();
    }

    public void DoStrafe(StrafeInfo info){
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if((transform.position-info.source.position).sqrMagnitude>=resetDistance){
            agent.velocity = Vector3.zero;
            return;
        }

        path.SetNPCPushing();
        rotationHandler.forward = info.source.right;
        if(!info.isRight){
            rotationHandler.Rotate(0, 180.0f, 0, Space.Self);
        }
        agent.destination = path.GetResetVec();
        if(info.source.name=="Player"){
            Debug.Log(name + " - 1 - " + agent.velocity + " POWER: " + rotationHandler.forward.normalized*(info.speed/3)*((resetDistance-info.magnitude)*(resetDistance-info.magnitude)));
        }
        agent.velocity = Vector3.ClampMagnitude(agent.velocity + rotationHandler.forward.normalized*(info.speed/3)*((resetDistance-info.magnitude)*(resetDistance-info.magnitude)),3.5f);
        if(info.source.name=="Player"){
            Debug.Log(name + " - 2 - " + agent.velocity);
        }
    }

    public float GetResetDistance(){
        return resetDistance;
    }
}
