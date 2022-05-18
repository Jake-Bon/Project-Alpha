using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    bool isAlert = false;

    Transform player;
    
    Pathfinding pathfinding;
    FOVDetector fov;
    NoiseDetector earshot;
    Material relaxedMaterial;
    Material alertMaterial;
    Renderer[] renderers; 

    // Start is called before the first frame update
    void Start()
    {
        fov = gameObject.GetComponent<FOVDetector>();
        earshot = gameObject.GetComponent<NoiseDetector>();
        pathfinding = gameObject.GetComponent<Pathfinding>();
        relaxedMaterial = Resources.Load<Material>("Materials/Red");
        alertMaterial = Resources.Load<Material>("Materials/Bright Red");
        player = GameObject.Find("Player").transform;
        renderers = gameObject.GetComponentsInChildren<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (fov.inSight || earshot.inEarshot) {
            if (!isAlert) ToggleAlert();
        } else {
            if (isAlert) ToggleAlert();
        }
    }

    void ToggleAlert() {
        if (isAlert) {
            foreach (Renderer r in renderers) {
                r.material = relaxedMaterial;
            }
            isAlert = false;
            pathfinding.Resume();
        } else {
            foreach (Renderer r in renderers) {
                r.material = alertMaterial;
            }
            isAlert = true;
            pathfinding.Pause();
        }
    }
}
