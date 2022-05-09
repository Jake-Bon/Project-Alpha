using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FOVDetector : MonoBehaviour
{
    public bool inSight = false;
    public float baseRadius = 10.0f;
    [Range(0.0f, 360.0f)]
    public float fieldOfView = 70.0f;

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
        StartCoroutine(FOVRoutine(0.2f));
    }

    IEnumerator FOVRoutine(float delay) {
        WaitForSeconds wait = new WaitForSeconds(delay);
        while (true) {
            FieldOfViewCheck();
            yield return wait;
        }
    }

    void FieldOfViewCheck() {
        float radius = GetAdjustedRadius();
        Collider[] rangeCheck = Physics.OverlapSphere(transform.position, radius, 1 << target.layer);

        if (rangeCheck.Length > 0) {
            Transform targetTransform = rangeCheck[0].transform;
            Vector3 directionToTarget = (targetTransform.position - transform.position).normalized;

            if (Vector3.Angle(transform.forward, directionToTarget) < fieldOfView / 2.0f) {
                float distanceToTarget = Vector3.Distance(targetTransform.position, transform.position);

                float visibility = player.GetVisibility();

                if (!Physics.Raycast(transform.position, directionToTarget, distanceToTarget, layerMask)) {
                    inSight = true;
                } else {
                    inSight = false;
                }
            } else {
                inSight = false;
            }
        } else {
            inSight = false;
        }
    }

    public float GetAdjustedRadius() {
        if (player == null) {
            return baseRadius;
        }
        return baseRadius * player.GetVisibility();
    }
}
