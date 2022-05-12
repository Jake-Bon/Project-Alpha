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
        //Debug.Log("Enter");
        if(actionType==ActionType.CameraChange){
            HandleCameraChange();
        }
    }
    void OnTriggerStay(Collider other)
    {
        
    }
    void OnTriggerExit(Collider other)
    {
        //Debug.Log("Exit");
    }

    void HandleCameraChange(){
        player.ChangeCamera(actionValue);
    }

    public enum ActionType {VisibilityChange, CameraChange};
}
