using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCEnabling : MonoBehaviour
{
    void OnBecameInvisible()
    {
        Debug.Log("I'm not visible anymore");
    }

    void OnBecameVisible()
    {
        Debug.Log("Hey! I'm visible!");
    }
}
