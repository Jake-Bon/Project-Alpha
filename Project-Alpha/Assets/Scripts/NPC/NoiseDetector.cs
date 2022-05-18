using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseDetector : MonoBehaviour
{
    public bool inEarshot = false;
    public float baseRadius = 10.0f;

    public GameObject target;

    public List<LayerMask> ignoredLayers;

    int layerMask;
    Player player;
    // Start is called before the first frame update
    void Start()
    {
        player = target.GetComponent<Player>();
        foreach (LayerMask mask in ignoredLayers) {
            int convertedMask = 1 << mask.value;
            layerMask = layerMask | convertedMask;
        }
        StartCoroutine(NoiseRoutine(0.2f));
    }

    IEnumerator NoiseRoutine(float delay) {
        WaitForSeconds wait = new WaitForSeconds(delay);
        while (true) {
            EarshotCheck();
            yield return wait;
        }
    }

    void EarshotCheck() {
        float radius = GetAdjustedRadius();
        Collider[] rangeCheck = Physics.OverlapSphere(transform.position, radius, 1 << target.layer);

        if (rangeCheck.Length > 0) {
            inEarshot = true;
        } else {
            inEarshot = false;
        }
    }

    public float GetAdjustedRadius() {
        if (player == null) {
            return baseRadius;
        }
        return baseRadius * player.GetNoise();
    }
}
