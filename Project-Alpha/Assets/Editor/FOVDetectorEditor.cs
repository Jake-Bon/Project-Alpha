using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FOVDetector))]
public class FOVDetectorEditor : Editor
{
    private void OnSceneGUI() {
        FOVDetector fov = (FOVDetector)target;
        Handles.color = Color.white;
        Handles.DrawWireArc(fov.transform.position, Vector3.up, Vector3.forward, 360, fov.GetAdjustedRadius());

        Vector3 viewAngle01 = DirectionFromAngle(fov.transform.eulerAngles.y, -fov.fieldOfView / 2.0f);
        Vector3 viewAngle02 = DirectionFromAngle(fov.transform.eulerAngles.y, fov.fieldOfView / 2.0f);
        Handles.color = Color.yellow;
        Handles.DrawLine(fov.transform.position, fov.transform.position + viewAngle01 * fov.GetAdjustedRadius());
        Handles.DrawLine(fov.transform.position, fov.transform.position + viewAngle02 * fov.GetAdjustedRadius());

        if (fov.inSight) {
            Handles.color = Color.green;
            Handles.DrawLine(fov.transform.position, fov.target.transform.position);
        }
    }

    private Vector3 DirectionFromAngle(float eulerY, float degrees) {
        degrees += eulerY;
        return new Vector3(Mathf.Sin(degrees * Mathf.Deg2Rad), 0, Mathf.Cos(degrees * Mathf.Deg2Rad));
    }
}
