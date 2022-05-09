using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HidingSpotTrigger : MonoBehaviour
{
    TMP_Text hideUI;
    HidingSpot spot;
    bool inTrigger;
    bool inHidingSpot;
    // Start is called before the first frame update
    void Start()
    {
        hideUI = GameObject.Find("HideUI").GetComponent<TMP_Text>();
        hideUI.enabled = false;
        spot = GetComponentInParent<HidingSpot>();
    }

    // Update is called once per frame
    void Update()
    {
        if (inTrigger) {
            if (Input.GetKeyDown(KeyCode.E)) {
                spot.Hide();
                hideUI.text = "Press E to leave";
                inTrigger = false;
                inHidingSpot = true;
            }
        } else if (inHidingSpot) {
            if (Input.GetKeyDown(KeyCode.E)) {
                spot.Unhide();
                inHidingSpot = false;
                hideUI.text = "Press E to hide";
            }
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (inHidingSpot) return;
        inTrigger = true;
        if (other.gameObject.layer == LayerMask.NameToLayer("Player")) {
            hideUI.enabled = true;
            hideUI.text = "Press E to hide";
        }
    }

    private void OnTriggerExit(Collider other) {
        if (inHidingSpot) return;
        inTrigger = true;
        if (other.gameObject.layer == LayerMask.NameToLayer("Player")) {
            hideUI.enabled = false;
        }
    }
}
