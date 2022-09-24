using UnityEngine;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Jobs;	
using System.Collections.Generic;


/**
 * This class manages all data related to the navigation system and runs the update for agents.
 * NaviProCore is never explicitly created by the user. An instance is created when it is first used.
 */
[AddComponentMenu("")]
[DefaultExecutionOrder(-2)]
public class NaviProCore : MonoBehaviour
{
	/**
	 * Find a path from the given start position to the end position in the given Sector. The start and end position are given in world space.
	 * The given agentRadius is used as a minimal distance to all obstacles, while the given safeDistance is used as a desired distance.
	 * The return value is a list of points in world space that make up the path. These are returned as Vector3, but since path finding is in 2D, the local y component is always 0.
	 * If no path can be found, the returned set of points is empty.
	 */
	public static List<Vector3> GetPathInSector(NaviProSector sector, Vector3 start, Vector3 end, float agentRadius, float safeDistance)
	{
		Vector3 localStart = sector.transform.worldToLocalMatrix.MultiplyPoint3x4(start);
		Vector3 localEnd = sector.transform.worldToLocalMatrix.MultiplyPoint3x4(end);

		List<Vector2> points = System.GetPath(sector.sector, localStart, localEnd, agentRadius, safeDistance);

		List<Vector3> worldPoints = new List<Vector3>();
		for (int i = 0; i < points.Count; ++i)
			worldPoints.Add(sector.transform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(points[i].x, 0.0f, points[i].y)));
		return worldPoints;
	}
	public static List<Vector3> GetPathInBaseSector(Vector3 start, Vector3 end, float agentRadius, float safeDistance)
	{
		if (!HasBaseSector())
			return new List<Vector3>();
		return GetPathInSector(GetBaseSector(), start, end, agentRadius, safeDistance);
	}

	// Returns whether the Sector contains the given point.
	// This check tries to map the point to the sector and compares any height difference to the maximum snapping height
	public static bool SectorContainsPoint(NaviProSector sector, Vector3 point)
	{
		return System.IsPointOnSector(sector.sector, point);
	}
	public static bool BaseSectorContainsPoint(Vector3 point)
	{
		if (!HasBaseSector())
			return false;
		return SectorContainsPoint(GetBaseSector(), point);
	}

	// Searches for the nearest agent visible from the given point within the given radius
	// If an agent is found, this function returns true and returns the position of the agent in the agentPosition parameter
	// If no agent is found, this function returns false
	public static bool FindNearestAgentPositionInSector(NaviProSector sector, Vector3 point, float radius, out Vector3 agentPosition)
	{
		return System.FindNearestAgentPositionWithinRadius(sector.sector, point, radius, out agentPosition);
	}
	public static bool FindNearestAgentPositionInBaseSector(Vector3 point, float radius, out Vector3 agentPosition)
	{
		if (!HasBaseSector())
		{
			agentPosition = Vector3.zero;
			return false;
		}
		return FindNearestAgentPositionInSector(GetBaseSector(), point, radius, out agentPosition);
	}


	struct AgentsSimulateMovementJob : IJobParallelFor
	{
		public NativeArray<System.IntPtr> agents;
		public float deltaTime;

		public void Execute(int index)
		{
			System.AgentSimulateMovement(agents[index], deltaTime);
		}
	}

	struct AgentsApplySimulatedMovementJob : IJobParallelFor
	{
		public NativeArray<System.IntPtr> agents;
		public float deltaTime;

		public void Execute(int index)
		{
			System.AgentApplySimulatedMovement(agents[index], deltaTime);
		}
	}

	struct AgentsUpdateSpatialIndexingJob : IJob
	{
		public NativeArray<System.IntPtr> agents;

		public void Execute()
		{
			foreach (var agent in agents)
				System.AgentUpdateSpatialIndexing(agent);
		}
	}

	struct AgentsUpdateTransformDataJob : IJobParallelFor
	{
		public NativeArray<System.IntPtr> agents;
		public NativeArray<Vector3> newPositions;
		public NativeArray<Quaternion> newRotations;
		public NativeArray<Vector2> newLocalSpeeds;
		public NativeArray<int> alignOrientation;

		public void Execute(int index)
		{
			Vector3 newPosition = System.GetAgentPosition(agents[index]);
			Vector3 agentPosition = newPosition;
			if (alignOrientation[index] > 0)
			{
				Vector3 up = System.GetUpVectorForAgent(agents[index], newPosition);
				newRotations[index] = NaviProAgent.OrientationToRotation(up, System.GetAgentOrientation(agents[index]));
			}
			else
			{
				newRotations[index] = NaviProAgent.OrientationToRotation(System.GetAgentOrientation(agents[index]));
			}

			newPositions[index] = agentPosition;
			newLocalSpeeds[index] = System.GetAgentLocalSpeed(agents[index]);
		}
	}


	// An instance of the NaviProNative, responsible for communicating with the native library
	// This instance is created at the same time as the instance of NaviProCore
	internal static readonly NaviProNative System = new NaviProNative();

	// The currently created sectors, agents, platforms and obstacles
	internal List<NaviProSector> sectors = new List<NaviProSector>();
	internal LinkedList<NaviProAgent> agents = new LinkedList<NaviProAgent>();
	internal LinkedList<NaviProPlatform> platforms = new LinkedList<NaviProPlatform>();
	internal LinkedList<NaviProObstacle> obstacles = new LinkedList<NaviProObstacle>();

	// Newly created and recently destroyed sectors
	internal HashSet<NaviProSector> insertSectors = new HashSet<NaviProSector>();
	internal HashSet<NaviProSector> removeSectors = new HashSet<NaviProSector>();

	// Newly created and recently destroyed platforms and obstacles
	internal HashSet<NaviProPlatform> insertPlatforms = new HashSet<NaviProPlatform>();
	internal HashSet<NaviProPlatform> removePlatforms = new HashSet<NaviProPlatform>();
	internal HashSet<NaviProObstacle> insertObstacles = new HashSet<NaviProObstacle>();
	internal HashSet<NaviProObstacle> removeObstacles = new HashSet<NaviProObstacle>();

	// Newly created and recently destroyed agents
	internal HashSet<NaviProAgent> insertAgents = new HashSet<NaviProAgent>();
	internal HashSet<NaviProAgent> removeAgents = new HashSet<NaviProAgent>();


	// A base sector to be used for all objects that are not a child of a NaviProSector
	// This base sector is only actually created when needed
	internal NaviProSector baseSector;


	// Returns the singular instance of NaviProCore
	// If it does not yet exist, it will be created
	static NaviProCore instance = null;
	static bool created = false;
	internal static NaviProCore Instance
	{
		get
		{
			if (IsDestroyed())
				throw new System.Exception("Accessing NaviProCore after it was destroyed");
			if (!instance)
			{
				var coreObject = new GameObject("NaviProCore");
				instance = coreObject.AddComponent<NaviProCore>();
				coreObject.hideFlags = HideFlags.HideInHierarchy;
				DontDestroyOnLoad(coreObject);
				created = true;
			}
			return instance;
		}
	}

	internal static bool IsDestroyed()
	{
		return created && !instance;
	}

	// Returns the base sector
	// If it does not yet exist, it will be created
	// This function should only ever be called from some call starting in the FixedUpdate below
	internal static NaviProSector GetBaseSector()
	{
		if (!Instance.baseSector)
		{
			var baseSectorObject = new GameObject("Base Sector");
			baseSectorObject.hideFlags = HideFlags.HideInHierarchy;
			Instance.baseSector = baseSectorObject.AddComponent<NaviProSector>();
			Instance.baseSector.Settings = NaviProSettings.Get().BaseSectorCreationSettings;
			// Immediately create the required sector
			Instance.baseSector.create(true);
			Instance.sectors.Add(Instance.baseSector);
		}

		return Instance.baseSector;
	}

	// Returns whether a base Sector has been created
	internal static bool HasBaseSector()
	{
		return Instance.baseSector;
	}

	
	private void Start()
	{
		// Check if we need to create a base sector based on the available objects
		checkForBaseSectorCreation();

		createAndDestroyObjects();
	}

	void FixedUpdate()
	{
		if (NaviProSettings.Get().RunInFixedTimeStep)
		{
			createAndDestroyObjects();

			timeStep(Time.deltaTime, !NaviProSettings.Get().InterpolateFixedSteps, NaviProSettings.Get().InterpolateFixedSteps);
		}
	}

	void Update()
	{
		if (!NaviProSettings.Get().RunInFixedTimeStep)
		{
			createAndDestroyObjects();

			timeStep(Time.deltaTime, true, false);
		}
		else if (NaviProSettings.Get().InterpolateFixedSteps)
		{
			NativeArray<Matrix4x4> sectorTransforms = new NativeArray<Matrix4x4>(sectors.Count, Allocator.Temp);
			NativeArray<Quaternion> sectorRotations = new NativeArray<Quaternion>(sectors.Count, Allocator.Temp);
			NativeArray<int> agentSectors = new NativeArray<int>(agentsInTimeStep.Count, Allocator.Temp);

			for (int i = 0; i < sectors.Count; ++i)
			{
				sectorTransforms[i] = sectors[i].transform.localToWorldMatrix;
				sectorRotations[i] = sectors[i].transform.rotation;
			}
			for (int i = 0; i < agentsInTimeStep.Count; ++i)
			{
				agentSectors[i] = sectors.IndexOf(agentsInTimeStep[i].getSector());
			}

			float t = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;

			for (int i = 0; i < agentsInTimeStep.Count; ++i)
			{
				var agent = agentsInTimeStep[i];
				if (agent == null)
					continue;
				var tfdata = agentTransformData[i];
				agent.localSpeed = Vector2.Lerp(tfdata.previousLocalSpeed, tfdata.newLocalSpeed, t);
				agent.transform.position = sectorTransforms[agentSectors[i]].MultiplyPoint3x4(Vector3.Lerp(tfdata.previousPosition, tfdata.newPosition, t));
				agent.transform.rotation = sectorRotations[agentSectors[i]] * (Quaternion.Lerp(tfdata.previousRotation, tfdata.newRotation, t));
			}

			sectorTransforms.Dispose();
			sectorRotations.Dispose();
			agentSectors.Dispose();
		}
	}

	void checkForBaseSectorCreation()
	{
		if (NaviProSector.canCreateBaseSector(NaviProSettings.Get().BaseSectorCreationSettings))
			GetBaseSector();
	}

	void createAndDestroyObjects()
	{
		foreach (var sector in removeSectors)
		{
			sectors.Remove(sector);
			sector.destroy();
			if (sector == Instance.baseSector)
				Instance.baseSector = null;
		}
		foreach (var sector in insertSectors)
		{
			if (sector.create())
				sectors.Add(sector);
		}

		foreach (var platform in removePlatforms)
		{
			platforms.Remove(platform.coreref);
			platform.destroy();
		}
		foreach (var obstacle in removeObstacles)
		{
			obstacles.Remove(obstacle.coreref);
			obstacle.destroy();
		}
		foreach (var platform in platforms)
		{
			platform.checkSector();
		}
		foreach (var obstacle in obstacles)
		{
			obstacle.checkSector();
		}
		foreach (var platform in insertPlatforms)
		{
			if (platform.create())
				platform.coreref = platforms.AddLast(platform);
		}
		foreach (var obstacle in insertObstacles)
		{
			if (obstacle.create())
				obstacle.coreref = obstacles.AddLast(obstacle);
		}

		removeSectors.Clear();
		insertSectors.Clear();
		removePlatforms.Clear();
		insertPlatforms.Clear();
		removeObstacles.Clear();
		insertObstacles.Clear();

		foreach (var agent in removeAgents)
		{
			agents.Remove(agent.coreref);
			agent.destroy();
		}
		foreach (var agent in insertAgents)
		{
			if (agent.create())
				agent.coreref = agents.AddLast(agent);
		}

		removeAgents.Clear();
		insertAgents.Clear();
	}


	void OnDestroy()
	{
		if (agentTransformData.IsCreated)
			agentTransformData.Dispose();
	}



	List<NaviProAgent> agentsInTimeStep = new List<NaviProAgent>();

	NativeArray<AgentTransformData> agentTransformData;

	bool demoTimeoutDisplayed = false;

	void timeStep(float deltaTime, bool updateTransform, bool prepareForTransformInterpolation)
	{
		if (agentTransformData.IsCreated)
			agentTransformData.Dispose();


		for (int i = 0; i < sectors.Count; ++i)
		{
			var sector = sectors[i];
			sector.checkProperties();
		}


		foreach (var agent in agents)
		{
			if (!agent.checkMoved())
			{
				// If the move failed, schedule the agent for disabling
				removeAgents.Add(agent);
				continue;
			}
			if (!System.AgentPrepareForSimulation(agent.agent._ref))
			{
				// If the move failed, schedule the agent for disabling
				removeAgents.Add(agent);
				continue;
			}
			agent.checkProperties();
			agent.updateGoal();

			if (agent.customControlCallback != null)
			{
				var data = System.GetAgentControlData(agent.agent);
				data.agent = agent;
				data.sectorTransform = agent.getSector().transform;
				Vector3 desiredSpeed = data.sectorTransform.worldToLocalMatrix.MultiplyPoint3x4(agent.customControlCallback(data));
				System.SetAgentDesiredSpeed(agent.agent, new Vector2(desiredSpeed.x, desiredSpeed.z));
			}
		}

		// Disable scheduled agents
		foreach (var agent in removeAgents)
		{
			agents.Remove(agent.coreref);
			agent.forceDisable();
		}
		removeAgents.Clear();


		/*
		if (!System.PrepareForSimulation(deltaTime))
		{
			if (!demoTimeoutDisplayed)
			{
				demoTimeoutDisplayed = true;
				Debug.Log("NaviPro has exceeded its demo runtime limit. Please restart the editor to continue the simulation.");
			}
		}
		*/


		agentsInTimeStep = new List<NaviProAgent>(agents);

		if (agentsInTimeStep.Count == 0)
			return;

		/*
		The following section runs the simulation in native code.
		It is a C# jobs adaption of the following three loops.
		foreach (var agent in agents)
			System.AgentSimulateMovement(agent.agent._ref, deltaTime); // multi-threaded
		foreach (var agent in agents)
			System.AgentApplySimulatedMovement(agent.agent._ref, deltaTime); // separate MT loop for correct behaviour
		foreach (var agent in agents)
			System.AgentUpdateSpatialIndexing(agent.agent._ref); // single threaded update of R-tree
		*/

			// Copy agent handles to native array
		var agentRefs = new NativeArray<System.IntPtr>(agentsInTimeStep.Count, Allocator.TempJob);
		var newPositions = new NativeArray<Vector3>(agentsInTimeStep.Count, Allocator.TempJob);
		var newRotations = new NativeArray<Quaternion>(agentsInTimeStep.Count, Allocator.TempJob);
		var newLocalSpeeds = new NativeArray<Vector2>(agentsInTimeStep.Count, Allocator.TempJob);
		var alignOrientations = new NativeArray<int>(agentsInTimeStep.Count, Allocator.TempJob);
		int num = 0;
		for (int i = 0; i < agentsInTimeStep.Count; ++i)
		{
			var agent = agentsInTimeStep[i];
			agentRefs[num] = agent.agent._ref;
			alignOrientations[num] = (agent.AlignOrientationToSlope && !agent.needsNavMeshPlacement()) ? 1 : 0;
			++num;
		}

		int agentsPerBatch = 1;

		AgentsSimulateMovementJob simulateMovementJob = new AgentsSimulateMovementJob();
		simulateMovementJob.agents = agentRefs;
		simulateMovementJob.deltaTime = deltaTime;
		JobHandle simulateMovementHandle = simulateMovementJob.Schedule(agents.Count, agentsPerBatch);

		AgentsApplySimulatedMovementJob applySimulatedMovementJob = new AgentsApplySimulatedMovementJob();
		applySimulatedMovementJob.agents = agentRefs;
		applySimulatedMovementJob.deltaTime = deltaTime;
		JobHandle applySimulatedMovementHandle = applySimulatedMovementJob.Schedule(agents.Count, agentsPerBatch, simulateMovementHandle);

		AgentsUpdateSpatialIndexingJob updateSpatialIndexingJob = new AgentsUpdateSpatialIndexingJob();
		updateSpatialIndexingJob.agents = agentRefs;
		JobHandle updateSpatialIndexingHandle = updateSpatialIndexingJob.Schedule(applySimulatedMovementHandle);

		/*
		After the native code, the new agent transforms are applied to the game objects.
		First, append one more job to get the data from native code.
		*/

		AgentsUpdateTransformDataJob updateTransformDataJob = new AgentsUpdateTransformDataJob();
		updateTransformDataJob.agents = agentRefs;
		updateTransformDataJob.newPositions = newPositions;
		updateTransformDataJob.newRotations = newRotations;
		updateTransformDataJob.newLocalSpeeds = newLocalSpeeds;
		updateTransformDataJob.alignOrientation = alignOrientations;
		JobHandle updateTransformDataHandle = updateTransformDataJob.Schedule(agents.Count, agentsPerBatch, updateSpatialIndexingHandle);

		// Wait for the previous jobs to finish
		updateTransformDataHandle.Complete();

		if (prepareForTransformInterpolation)
			agentTransformData = new NativeArray<AgentTransformData>(agentsInTimeStep.Count, Allocator.Persistent);

		// Update agent positions etc
		for (int i = 0; i < agentsInTimeStep.Count; ++i)
		{
			var agent = agentsInTimeStep[i];

			AgentTransformData transformData = new AgentTransformData();
			transformData.previousPosition = agent.transformData.newPosition;
			transformData.previousRotation = agent.transformData.newRotation;
			transformData.previousLocalSpeed = agent.transformData.newLocalSpeed;
			transformData.newPosition = newPositions[i];
			transformData.newRotation = newRotations[i];
			transformData.newLocalSpeed = newLocalSpeeds[i];

			if (!System.IsAgentEnabled(agent.agent))
			{
				// If the Agent was disabled internally, schedule the agent for disabling here
				removeAgents.Add(agent);

				// The transformdata should still be set to avoid a missing entry
				if (prepareForTransformInterpolation)
					agentTransformData[i] = transformData;

				continue;
			}

			// For agents on a Unity Navmesh created Sector, the height needs to be corrected to match the precise NavMesh geometry
			// When support for Navmesh queries in Jobs is final, this could be moved to a Job
			// Ideally, we would have access to the exact Navmesh geometry so this adjustment can be removed
			if (agent.needsNavMeshPlacement() && !System.IsAgentOnPlatform(agent.agent._ref))
			{
				transformData.newPosition = agent.getSector().adjustHeight(new Vector3(transformData.newPosition.x, transformData.previousPosition.y, transformData.newPosition.z), agent.Radius * 10.0f);
			}

			agent.transformData = transformData;
			
			var newGoalStatus = System.GetAgentGoalStatus(agent.agent._ref);
			if (agent.GoalStatus != newGoalStatus)
				agent.setGoalStatus(newGoalStatus);
			var newPathStatus = System.GetAgentPathStatus(agent.agent._ref);
			if (agent.PathStatus != newPathStatus)
				agent.setPathStatus(newPathStatus);

			if (prepareForTransformInterpolation)
			{
				agentTransformData[i] = transformData;
			}

			if (updateTransform)
			{
				agentsInTimeStep[i].transform.position = agentsInTimeStep[i].getSector().transform.localToWorldMatrix.MultiplyPoint3x4(newPositions[i]);
				agentsInTimeStep[i].transform.rotation = agentsInTimeStep[i].getSector().transform.localToWorldMatrix.rotation * newRotations[i];
			}
		}

		// Disable scheduled agents
		foreach (var agent in removeAgents)
		{
			agents.Remove(agent.coreref);
			agent.forceDisable();
		}
		removeAgents.Clear();

		agentRefs.Dispose();
		newPositions.Dispose();
		newRotations.Dispose();
		newLocalSpeeds.Dispose();
		alignOrientations.Dispose();
	}
}
