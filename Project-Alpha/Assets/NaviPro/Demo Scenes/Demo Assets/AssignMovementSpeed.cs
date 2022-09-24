using UnityEngine;

public class AssignMovementSpeed : MonoBehaviour
{
    void Update()
    {
		GetComponentInChildren<Animator>().SetFloat("Speed", GetComponent<NaviProAgent>().GetLocalSpeed().z);
    }
}
