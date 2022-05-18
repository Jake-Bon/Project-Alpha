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
    float baseNoise = 0.00f;

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

    public float GetNoise() {
        float noise = baseNoise;

        if (Input.GetKey(KeyCode.LeftShift)) {
            noise += 60.0f;
        } else if (Input.GetAxisRaw("Vertical") != 0 || Input.GetAxisRaw("Horizontal") != 0) {
            noise += 20.0f;
        }
        return noise/100.0f;
    }
    
}

