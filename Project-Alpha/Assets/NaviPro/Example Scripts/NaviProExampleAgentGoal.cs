using UnityEngine;


[AddComponentMenu("NaviPro/NaviPro Example Agent Goal")]
public class NaviProExampleAgentGoal : MonoBehaviour
{
	public Transform Goal;

	void Start()
	{
		if (Goal && GetComponent<NaviProAgent>())
		{
			GetComponent<NaviProAgent>().SetGoal(Goal.position);
		}
	}
}
