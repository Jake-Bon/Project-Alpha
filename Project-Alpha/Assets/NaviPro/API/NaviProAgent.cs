using UnityEngine;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif


[AddComponentMenu("NaviPro/NaviPro Agent")]
[DisallowMultipleComponent]
[DefaultExecutionOrder(-3)]
public class NaviProAgent : MonoBehaviour
{
	[Tooltip("The physical radius of the agent for determining collision, measured in meters.")]
	[Min(0.0f)]
	public float Radius = 0.35f;
	[Tooltip("The range within which an Agent prefers to have no other agents, measured from the side of the agent in meters.")]
	[Min(0.0f)]
	public float PersonalSpaceDistance = 0.4f;
	[Tooltip("The agent's preferred distance to obstacles, if sufficient space is available, measured from the side of the agent in meters.")]
	[Min(0.0f)]
	public float SafeDistance = 2.0f;
	[Tooltip("The range within which the Agent can see and react to other Agents, measured from the center of the agent in meters.")]
	[Min(0.0f)]
	public float VisibilityRadius = 8.0f;
	[Tooltip("Maximum forward speed, in meters per second")]
	[Min(0.0f)]
	public float MovementSpeed = 1.5f;
	[Tooltip("The maximum forward acceleration, in meters per second squared")]
	[Min(0.0f)]
	public float Acceleration = 6.0f;
	[Tooltip("The maximum turning speed, used to steer in the direction of preferred movement, in radians per second")]
	[Min(0.0f)]
	public float TurningSpeed = 3.0f;
	[Tooltip("The maximum turning acceleration, in radians per second squared")]
	[Min(0.0f)]
	public float TurningAcceleration = 15.0f;
	[Tooltip("The fraction of the forward movement speed and acceleration to use for sideways movement, between 0 and 1")]
	[Range(0.0f, 1.0f)]
	public float SideWaysMovementFactor = 0.2f;
	[Tooltip("The fraction of the forward movement speed and acceleration to use for backwards movement, between 0 and 1")]
	[Range(0.0f, 1.0f)]
	public float BackwardsMovementFactor = 0.1f;
	[Tooltip("Whether to adjust the local up direction of this Agent to the slope. This only works on custom Walkable Meshes.")]
	public bool AlignOrientationToSlope = false;

	public NaviPro.GoalStatus GoalStatus { get; private set; }
	public NaviPro.PathStatus PathStatus { get; private set; }

	public delegate void AgentEvent(NaviProAgent agent);

	// Goal Events based on a change in GoalStatus
	// Nearing goal, slowing down: the Agent has begun slowing down to stop at the goal
	public event AgentEvent EventNearingGoalSlowingDown;
	// Nearing goal, moving to next: the Agent has begun moving to the next assigned goal
	public event AgentEvent EventNearingGoalMovingToNext;
	// Goal reached: the Agent has stopped at its goal
	public event AgentEvent EventGoalReached;
	// Nearing point near goal, slowing down: the Agent has begun slowing down to stop at a point close to the goal
	// The goal could not be reached
	public event AgentEvent EventNearingPointNearGoalSlowingDown;
	// Point near goal reached: the Agent has stopped at a point close to the goal
	// The goal could not be reached
	public event AgentEvent EventPointNearGoalReached;

	// Path Events based on a change in PathStatus
	// Path obstructed and updated: the followed path was obstructed by a change in the Sector and the Agent has planned a new path
	public event AgentEvent EventPathObstructedAndUpdated;
	// Path lost and updated: the Agent deviated too far from the followed path and has planned a new path
	public event AgentEvent EventPathLostAndUpdated;
	// Faster path found and updated: a new and faster route has opened up and the Agent has planned a new path
	public event AgentEvent EventFasterPathFoundAndUpdated;
	// Improved path found and updated: a new route leading closer to the goal has opened up and the Agent has planned a new path
	public event AgentEvent EventImprovedPathFoundAndUpdated;
	// Moved agent, path updated: the agent was moved to a new position and has planned a new path
	public event AgentEvent EventMovedAgentPathUpdated;

	// Other Events
	// Agent disabled: the Agent could not be placed on the Sector and has been disabled
	public event AgentEvent EventAgentDisabled;

	internal AgentTransformData transformData;

	NaviProSector sector;


	/**
	 * Specifies a new goal position in local Sector space.
	 * All previous goals will be cleared.
	 */
	public void SetGoal(Vector3 goal)
	{
		goalsChanged = true;
		goalsCleared = true;
		newGoals.Clear();
		newGoals.Add(getSectorTransformWorldToLocal().MultiplyPoint3x4(goal));
	}
	/**
	 * Adds a new goal position in local Sector space.
	 * The new goal will be followed when the previous goals are reached.
	 */
	public void AddGoal(Vector3 goal)
	{
		goalsChanged = true;
		newGoals.Add(getSectorTransformWorldToLocal().MultiplyPoint3x4(goal));
	}
	/**
	 * Clears all goals.
	 * The agent will stop following a goal.
	 */
	public void ClearGoals()
	{
		goalsChanged = true;
		goalsCleared = true;
		newGoals.Clear();
	}
	/**
	 * Once a path to the current goal has been found, this will return whether the path reaches the goal.
	 * If this returns false, it means the Agent is on its way to or has arrived at a point near the goal instead.
	 */
	public bool CanReachCurrentGoal()
	{
		// This first return statement is not very useful, but makes more sense than returning true for this case
		if (GoalStatus == NaviPro.GoalStatus.NoGoalSet)
			return false;
		if (GoalStatus == NaviPro.GoalStatus.MovingTowardsPointNearGoal || GoalStatus == NaviPro.GoalStatus.NearingPointNearGoalSlowingDown || GoalStatus == NaviPro.GoalStatus.PointNearGoalReached)
			return false;
		return true;
	}


	/**
	 * Move the agent to a new position with the given rotation.
	 * Note that the rotation will be adjusted if needed, so that the Agent remains upright.
	 */
	public void MoveTo(Vector3 position, Quaternion rotation)
	{
		moved = true;
		newPosition = getSectorTransformWorldToLocal().MultiplyPoint3x4(position);
		newRotation = getSectorTransformWorldToLocal().rotation * rotation;
	}


	public Vector3 GetLocalSpeed()
	{
		return Vector3.forward * localSpeed.x + Vector3.left * localSpeed.y;
	}

	public Vector3 GetLocalPositionInSector(Vector3 worldPosition)
	{
		if (created)
			return getSector().transform.worldToLocalMatrix.MultiplyPoint3x4(worldPosition);
		else
			return worldPosition;
	}


	/**
	 * Set a custom function to determine the desired movement speed of the Agent.
	 * The callback function is supplied with AgentControlData, containing info on the current path, the nearest obstacle and nearby Agents.
	 * The value returned from the callback should be the desired movement speed vector in world space.
	 */
	public void SetCustomControlCallback(Func<NaviPro.AgentControlData, Vector3> callback)
	{
		customControlCallback = callback;
		if (agent != null)
			NaviProCore.System.SetAgentCustomControl(agent, true);
	}

	public void ClearCustomControlCallback()
	{
		customControlCallback = null;
		NaviProCore.System.SetAgentCustomControl(agent, false);
	}


	[HideInInspector]
	internal NaviProNative.Agent agent;
	internal NaviProNative.Agent.AgentProperties properties;

	internal LinkedListNode<NaviProAgent> coreref = null;
	bool created = false;
	bool ignoreDisable = false;

	bool goalsChanged = false;
	bool goalsCleared = false;
	List<Vector3> newGoals = new List<Vector3>();

	internal Vector2 localSpeed;

	internal Vector3 newPosition;
	internal Quaternion newRotation;
	internal bool moved = false;

	bool needsSectorCheck = false;

	internal Func<NaviPro.AgentControlData, Vector3> customControlCallback;


	void OnEnable()
	{
		if (created)
			NaviProCore.Instance.removeAgents.Remove(this);
		else
			NaviProCore.Instance.insertAgents.Add(this);
	}
	void OnDisable()
	{
		if (ignoreDisable)
		{
			ignoreDisable = false;
			return;
		}

		if (NaviProCore.IsDestroyed())
			return;
		if (created)
			NaviProCore.Instance.removeAgents.Add(this);
		else
			NaviProCore.Instance.insertAgents.Remove(this);
	}


	void OnTransformParentChanged()
	{
		needsSectorCheck = true;
	}



	Matrix4x4 getSectorTransformWorldToLocal()
	{
		if (created)
			return getSector().transform.worldToLocalMatrix;
		else
		{
			var parentSector = GetComponentInParent<NaviProSector>();
			if (parentSector)
				return parentSector.transform.worldToLocalMatrix;
			else
				return Matrix4x4.identity;
		}
	}



	internal bool create()
	{
		if (created)
			return false;
		if (!gameObject.GetComponentInParent<NaviProSector>() && !NaviProCore.HasBaseSector())
		{
			ignoreDisable = true;
			enabled = false;
			return false;
		}

		properties.radius = Radius;
		properties.personalSpaceDistance = PersonalSpaceDistance;
		properties.safeDistance = SafeDistance;
		properties.visibilityRadius = VisibilityRadius;
		properties.movementSpeed = MovementSpeed;
		properties.acceleration = Acceleration;
		properties.turnSpeed = TurningSpeed;
		properties.turnAcceleration = TurningAcceleration;
		properties.sidewaysMovementFactor = SideWaysMovementFactor;
		properties.backwardsMovementFactor = BackwardsMovementFactor;

		agent = NaviProCore.System.CreateAgent(properties);

		sector = gameObject.GetComponentInParent<NaviProSector>();
		Vector3 agentPosition = getSector().transform.worldToLocalMatrix.MultiplyPoint3x4(transform.position);
		float agentOrientation = ForwardToOrientation(getSector().transform.worldToLocalMatrix.MultiplyVector(transform.forward));
		NaviProCore.System.MoveAgentTo(agent, getSector().sector, agentPosition, agentOrientation);

		if (!NaviProCore.System.IsAgentEnabled(agent))
		{
			created = true;
			forceDisable();
			return false;
		}

		if (needsNavMeshPlacement())
			agentPosition = getSector().adjustHeight(agentPosition, Radius * 10.0f);
		transform.position = getSector().transform.localToWorldMatrix.MultiplyPoint3x4(agentPosition);

		if (customControlCallback != null)
			NaviProCore.System.SetAgentCustomControl(agent, true);

		transformData = new AgentTransformData();
		transformData.Set(agentPosition, OrientationToRotation(agentOrientation), Vector2.zero);

		created = true;
		return true;
	}

	internal void forceDisable()
	{
		destroy();
		ignoreDisable = true;
		enabled = false;
		if (EventAgentDisabled != null)
			EventAgentDisabled(this);
	}

	internal void destroy()
	{
		if (!created)
			return;

		NaviProCore.System.DestroyAgent(agent);
		agent = null;

		created = false;
	}

	internal bool checkMoved()
	{
		bool moveAgent = false;
		if (needsSectorCheck)
		{
			var newSector = gameObject.GetComponentInParent<NaviProSector>();
			if (newSector != sector)
			{
				sector = newSector;
				moveAgent = true;
			}
			needsSectorCheck = false;
		}
		if (moved)
		{
			moveAgent = true;
			transformData.newPosition = transformData.previousPosition = getSector().transform.localToWorldMatrix.MultiplyPoint3x4(newPosition);
			transformData.newRotation = transformData.previousRotation = getSector().transform.localToWorldMatrix.rotation * OrientationToRotation(ForwardToOrientation(newRotation * Vector3.forward));
			moved = false;
		}
		if (moveAgent)
		{
			float localOrienation = ForwardToOrientation(newRotation * Vector3.forward);
			NaviProCore.System.MoveAgentTo(agent, getSector().sector, newPosition, localOrienation);

			if (!NaviProCore.System.IsAgentEnabled(agent))
				return false;
		}

		return true;
	}

	internal void checkProperties()
	{
		if (properties.radius != Radius || properties.personalSpaceDistance != PersonalSpaceDistance || properties.safeDistance != SafeDistance || properties.visibilityRadius != VisibilityRadius
			 || properties.movementSpeed != MovementSpeed || properties.acceleration != Acceleration
		     || properties.turnSpeed != TurningSpeed || properties.turnAcceleration != TurningAcceleration
		     || properties.sidewaysMovementFactor != SideWaysMovementFactor || properties.backwardsMovementFactor != BackwardsMovementFactor)
		{
			properties.radius = Radius;
			properties.safeDistance = SafeDistance;
			properties.movementSpeed = MovementSpeed;
			properties.acceleration = Acceleration;
			properties.turnSpeed = TurningSpeed;
			properties.turnAcceleration = TurningAcceleration;
			properties.sidewaysMovementFactor = SideWaysMovementFactor;
			properties.backwardsMovementFactor = BackwardsMovementFactor;

			NaviProCore.System.SetAgentProperties(agent, properties);
		}
	}

	internal void updateGoal()
	{
		if (goalsChanged)
		{
			if (goalsCleared)
			{
				if (newGoals.Count > 0)
					NaviProCore.System.SetAgentGlobalGoal(agent, newGoals[0]);
				else
					NaviProCore.System.ClearAgentGlobalGoal(agent);
				goalsCleared = false;
			}
			for (int i = 1; i < newGoals.Count; ++i)
				NaviProCore.System.AddAgentGlobalGoal(agent, newGoals[i]);
			goalsChanged = false;
			newGoals.Clear();
		}
	}

	internal NaviProSector getSector()
	{
		if (sector)
			return sector;
		else
			return NaviProCore.GetBaseSector();
	}

	// Whether the Agent needs to be placed on the Unity NavMesh after simulation
	internal bool needsNavMeshPlacement()
	{
		return getSector().Settings.creationMethod == NaviProSector.CreationMethod.FromUnityNavMesh;
	}

	internal bool getMoved()
	{
		if (moved)
		{
			moved = false;
			return true;
		}
		return false;
	}

	internal static float ForwardToOrientation(Vector3 forward)
	{
		return Mathf.Atan2(forward.z, forward.x);
	}

	internal static Quaternion OrientationToRotation(float orientation)
	{
		return Quaternion.AngleAxis(90.0f - Mathf.Rad2Deg * orientation, Vector3.up);
	}
	internal static Quaternion OrientationToRotation(Vector3 up, float orientation)
	{
		return Quaternion.FromToRotation(Vector3.up, up) * Quaternion.AngleAxis(90.0f - Mathf.Rad2Deg * orientation, Vector3.up);
	}

	internal void setGoalStatus(NaviPro.GoalStatus goalStatus)
	{
		GoalStatus = goalStatus;
		if (goalStatus == NaviPro.GoalStatus.NearingGoalSlowingDown)
		{
			if (EventNearingGoalSlowingDown != null)
				EventNearingGoalSlowingDown(this);
		}
		else if (goalStatus == NaviPro.GoalStatus.NearingGoalMovingToNext)
		{
			if (EventNearingGoalMovingToNext != null)
				EventNearingGoalMovingToNext(this);
		}
		else if (goalStatus == NaviPro.GoalStatus.GoalReached)
		{
			if (EventGoalReached != null)
				EventGoalReached(this);
		}
		else if (goalStatus == NaviPro.GoalStatus.NearingPointNearGoalSlowingDown)
		{
			if (EventNearingPointNearGoalSlowingDown != null)
				EventNearingPointNearGoalSlowingDown(this);
		}
		else if (goalStatus == NaviPro.GoalStatus.PointNearGoalReached)
		{
			if (EventPointNearGoalReached != null)
				EventPointNearGoalReached(this);
		}
	}
	internal void setPathStatus(NaviPro.PathStatus pathStatus)
	{
		PathStatus = pathStatus;
		if (pathStatus == NaviPro.PathStatus.PathObstructedAndUpdated)
		{
			if (EventPathObstructedAndUpdated != null)
				EventPathObstructedAndUpdated(this);
		}
		else if (pathStatus == NaviPro.PathStatus.PathLostAndUpdated)
		{
			if (EventPathLostAndUpdated != null)
				EventPathLostAndUpdated(this);
		}
		else if (pathStatus == NaviPro.PathStatus.FasterPathFoundAndUpdated)
		{
			if (EventFasterPathFoundAndUpdated != null)
				EventFasterPathFoundAndUpdated(this);
		}
		else if (pathStatus == NaviPro.PathStatus.ImprovedPathFoundAndUpdated)
		{
			if (EventImprovedPathFoundAndUpdated != null)
				EventImprovedPathFoundAndUpdated(this);
		}
		else if (pathStatus == NaviPro.PathStatus.MovedAgentPathUpdated)
		{
			if (EventMovedAgentPathUpdated != null)
				EventMovedAgentPathUpdated(this);
		}
	}
}


internal struct AgentTransformData
{
	public Vector3 previousPosition;
	public Vector3 newPosition;
	public Quaternion previousRotation;
	public Quaternion newRotation;
	public Vector2 previousLocalSpeed;
	public Vector2 newLocalSpeed;


	public void Set(Vector3 position, Quaternion rotation, Vector2 localSpeed)
	{
		previousPosition = newPosition = position;
		previousRotation = newRotation = rotation;
		previousLocalSpeed = newLocalSpeed = localSpeed;
	}
}


#if UNITY_EDITOR
[CustomEditor(typeof(NaviProAgent))]
public class NaviProAgentEditor : Editor
{
	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		EditorGUILayout.Space();
		var property = serializedObject.GetIterator();
		bool first = true;
		while (property.NextVisible(first))
		{
			if (property.displayName != "Script")
				EditorGUILayout.PropertyField(property);
			first = false;
			if (property.propertyType == SerializedPropertyType.Float)
			{
				foreach (var attribute in typeof(NaviProAgent).GetField(property.name).GetCustomAttributes(false))
				{
					MinAttribute minAttribute = attribute as MinAttribute;
					if (minAttribute != null)
					{
						if (property.floatValue < minAttribute.min)
							property.floatValue = minAttribute.min;
					}
				}
			}
		}

		serializedObject.ApplyModifiedProperties();
	}
}
#endif
