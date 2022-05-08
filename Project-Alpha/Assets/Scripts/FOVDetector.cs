using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FOVDetector : MonoBehaviour
{
    public bool inSight = false;
    public float radius = 10.0f;
    [Range(0.0f, 360.0f)]
    public float fieldOfView = 70.0f;

    public GameObject target;

    public List<LayerMask> ignoredLayers;

    int layerMask;
    // Start is called before the first frame update
    void Start()
    {
        foreach (LayerMask mask in ignoredLayers) {
            int convertedMask = 1 << mask.value;
            layerMask = layerMask | convertedMask;
        }
        layerMask = ~layerMask;
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
        Collider[] rangeCheck = Physics.OverlapSphere(transform.position, radius);

        foreach (Collider c in rangeCheck) {
            Debug.Log(c.name);
        }
        return;

        Debug.Log(rangeCheck.Length);

        if (rangeCheck.Length > 0) {
            Debug.Log("Detected player in radius");
            Transform targetTransform = rangeCheck[0].transform;
            Vector3 directionToTarget = (targetTransform.position - transform.position).normalized;

            if (Vector3.Angle(transform.forward, directionToTarget) > fieldOfView / 2.0f) {
                float distanceToTarget = Vector3.Distance(targetTransform.position, transform.position);

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
}
