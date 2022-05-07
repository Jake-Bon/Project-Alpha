using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerDetectionTest : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Enter");
    }
    void OnTriggerStay(Collider other)
    {
        
    }
    void OnTriggerExit(Collider other)
    {
        Debug.Log("Exit");
    }
}
