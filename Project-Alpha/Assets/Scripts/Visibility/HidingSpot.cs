using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HidingSpot : MonoBehaviour
{
    GameObject player;
    Player playerComp;
    CharacterController controller;
    Vector3 previousPos;
    Collider spotCollider;
    void Start() {
        player = GameObject.Find("Player");  
        playerComp = player.GetComponent<Player>(); 
        controller = player.GetComponent<CharacterController>();
        spotCollider = gameObject.GetComponent<Collider>();   
    }
    // Update is called once per frame
    void Update()
    {
        
    }

    public void Hide() {
        playerComp.isHiding = true;
        controller.enabled = false;
        previousPos = player.transform.position;
        player.transform.position = transform.position;
        spotCollider.isTrigger = true;
    }

    public void Unhide() {
        playerComp.isHiding = false;
        player.transform.position = previousPos;
        spotCollider.isTrigger = false;
        controller.enabled = true;
    }
}
