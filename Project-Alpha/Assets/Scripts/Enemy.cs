using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    bool isAlert = false;

    Transform player;
    
    Pathfinding pathfinding;
    FOVDetector fov;
    Material relaxedMaterial;
    Material alertMaterial;
    Renderer[] renderers; 

    // Start is called before the first frame update
    void Start()
    {
        fov = gameObject.GetComponent<FOVDetector>();
        pathfinding = gameObject.GetComponent<Pathfinding>();
        relaxedMaterial = Resources.Load<Material>("Materials/Red");
        alertMaterial = Resources.Load<Material>("Materials/Bright Red");
        player = GameObject.Find("Player").transform;
        renderers = gameObject.GetComponentsInChildren<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (fov.inSight) {
            if (!isAlert) ToggleAlert();
        } else {
            if (isAlert) ToggleAlert();
        }
    }

    void CastRay() {
        Vector3 look = player.position - gameObject.transform.position;
        look.Normalize();

        Debug.DrawRay(gameObject.transform.position, look * 15, Color.red, 0.1f, false);

        int layerMask = (1 << LayerMask.NameToLayer("NPC"));
        layerMask = ~layerMask;

        RaycastHit hit;
        if (Physics.Raycast(gameObject.transform.position, look, out hit, 15.0f, layerMask)) {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Player")) {
                if (!isAlert) ToggleAlert();
            } else {
                if (isAlert) ToggleAlert();
            }
        } else if (isAlert) ToggleAlert();
    }

    void ToggleAlert() {
        if (isAlert) {
            foreach (Renderer r in renderers) {
                r.material = relaxedMaterial;
            }
            isAlert = false;
            //pathfinding.Resume();
        } else {
            foreach (Renderer r in renderers) {
                r.material = alertMaterial;
            }
            isAlert = true;
            //pathfinding.Pause();
        }
    }
}
