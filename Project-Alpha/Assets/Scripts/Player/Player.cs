using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public bool isDead;

    public bool stopInput;

    public bool inCrowd = false;

    public bool isHiding = false;

    public bool noChild = false;

    float baseVisibility = 100.0f;

    public float GetVisibility() {
        float visibility = baseVisibility;
        if (isHiding) {
            visibility -= 90.0f;
        }
        else if (inCrowd) {
            visibility -= 60.0f;
        }

        if(noChild){
            visibility -= 10.0f;
        }

        return visibility/100.0f;
    }
    
}

