using UnityEngine;


[AddComponentMenu("NaviPro/NaviPro Example Custom Control")]
public class NaviProExampleCustomControl : MonoBehaviour
{
	void Start()
	{
		if (GetComponent<NaviProAgent>())
		{
			GetComponent<NaviProAgent>().SetCustomControlCallback(exampleCallback);
		}
	}

	Vector3 exampleCallback(NaviPro.AgentControlData data)
	{
		// Determine a move vector based on WASD input (PC only)
		Vector2 moveVector = Vector3.zero;
		if (Input.GetKey(KeyCode.W))
			moveVector += Vector2.up;
		if (Input.GetKey(KeyCode.S))
			moveVector -= Vector2.up;
		if (Input.GetKey(KeyCode.A))
			moveVector -= Vector2.right;
		if (Input.GetKey(KeyCode.D))
			moveVector += Vector2.right;

		if (moveVector.magnitude > 0.0)
		{
			// If there is movement input, map the up input to the Agent's forward direction and right input to the Agent's right direction
			Vector3 agentDirection = data.agent.transform.forward * moveVector.y + data.agent.transform.right * moveVector.x;
			// Multiply the direction by the agent's movement speed
			return agentDirection * data.agent.MovementSpeed;
		}
		return Vector3.zero;
	}
}
