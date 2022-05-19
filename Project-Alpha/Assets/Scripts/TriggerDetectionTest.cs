using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerDetectionTest : MonoBehaviour
{
    [Header("Trigger Parameters")]
    [SerializeField] private ActionType actionType;
    [SerializeField] private int actionValue; 

    private ThirdPersonController player;

    void Start(){
        player = GameObject.Find("Player").GetComponent<ThirdPersonController>();
    }


    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Player")) 
            return;
        //Debug.Log("Enter");
        if(actionType==ActionType.CameraChange){
            HandleCameraChange();
        }
    }
    void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Player")) 
            return;
    }
    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Player")) 
            return;
        //Debug.Log("Exit");
    }

    void HandleCameraChange(){
        player.ChangeCamera(actionValue);
    }

    public enum ActionType {VisibilityChange, CameraChange};
}
