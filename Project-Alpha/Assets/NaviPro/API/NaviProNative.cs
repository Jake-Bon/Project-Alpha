using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;



namespace NaviPro
{
	// The GoalStatus provides info on what the Agent is doing regarding its Goals
	// Many of the following status will only be shown for one update cycle
	public enum GoalStatus
	{
		// No Goal is set
		NoGoalSet = 0,
		// The Agent is moving towards a Goal
		MovingTowardsGoal = 1,
		// The Agent is close to a Goal and has begun slowing down
		NearingGoalSlowingDown = 2,
		// The Agent is close to a Goal and has begun moving towards a next Goal
		NearingGoalMovingToNext = 3,
		// The last goal has been reached
		GoalReached = 4,
		// The Agent is moving towards a Goal
		MovingTowardsPointNearGoal = 5,
		// The Agent is close to a Goal and has begun slowing down
		NearingPointNearGoalSlowingDown = 6,
		// The last goal has been reached
		PointNearGoalReached = 7
	};

	// The PathStatus provides info on what the Agent is doing regarding its pathing
	// Many of the following status will only be shown for one update cycle
	public enum PathStatus
	{
		// The Agent is not following a path
		NotFollowingPath = 0,
		// The Agent is following a path
		FollowingPath = 1,
		// The Agent was following a path, but it was recently obstructed
		// The path has been updated
		PathObstructedAndUpdated = 2,
		// The Agent was following a path, but could no longer see its nearest point
		// The path has been updated
		PathLostAndUpdated = 3,
		// The Agent was following a path, but found a faster alternative
		// The path has been updated
		FasterPathFoundAndUpdated = 4,
		// The Agent was following a path, but found a path leading closer to its goal
		// The path has been updated
		ImprovedPathFoundAndUpdated = 5,
		// The Agent was following a path, but was moved to a new position
		// The path has been updated
		MovedAgentPathUpdated = 6
	};

	// This is a compact description of an Agent as part of AgentControlData
	public struct NearbyAgentData
	{
		// The current position in Sector space
		public Vector2 position;
		// The current speed in Sector space
		public Vector2 speed;
		// The physical radius
		public float radius;
	};

	// This structure contains the information provided when manually controlling an Agent
	public struct AgentControlData
	{
		// The agent being controlled
		public NaviProAgent agent;
		// The transform of the Sector the Agent is in
		// This is useful for transforming from Sector space to Worls space and vice versa
		public Transform sectorTransform;
		// The current position of the Agent in Sector space
		// Note that this does not contain any height data
		public Vector2 position;
		// The position of the nearest obstacle point in Sector space
		// Obstacles are edges of the walkable surface, such as walls or NaviProObstacles
		public Vector2 nearestObstacle;
		// The distance from the center of the nearest corridor to the nearest obstacle point
		// This measure is useful for determining the size of the environment
		public float centerObstacleDistance;
		// The nearest point on the path the agent is currently following
		// If the agent is not following a path, this will be equal to Vector2.zero
		public Vector2 nearestPathPoint;
		// The tangent or direction at the nearest point on the path the agent is currently following
		// If the agent is not following a path, this will be equal to Vector2.zero
		public Vector2 nearestPathTangent;
		// The suggested pathing direction, based on the current path, the next goal and the nearest obstacle
		// If the agent is not following a path, this will be equal to Vector2.zero
		// Note that this does not include any Agent avoidance
		public Vector2 suggestedPathingDirection;
		// The nearby agents within sight of the controlled Agent
		public List<NearbyAgentData> nearbyAgents;
	};
}


public class NaviProNative
{
	internal const string ErrorMessage = "An unexpected error has occured in NaviPro. Behaviour may not be as expected. Please contact support with details about what has happened.";

	internal class Sector
	{
		public IntPtr _ref;
	}
	internal class Obstacle
	{
		public IntPtr _ref;
	}
	internal class Platform
	{
		public IntPtr _ref;
	}
	internal class Agent
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct AgentProperties
		{
			public float radius;
			public float personalSpaceDistance;
			public float safeDistance;
			public float visibilityRadius;
			public float movementSpeed;
			public float acceleration;
			public float turnSpeed;
			public float turnAcceleration;
			public float sidewaysMovementFactor;
			public float backwardsMovementFactor;
		};

		public IntPtr _ref;
	}


	internal NaviProNative()
	{
		system = Native.Create();
	}

	~NaviProNative()
	{
		Native.Destroy(system);
	}

	internal Sector CreateSectorFromInputMesh(float[] vertices, int[] indices, bool mergeVertices, float mergeSize, bool fixTopology)
	{
		IntPtr verticesRaw = Marshal.AllocHGlobal(vertices.Length * sizeof(float));
		IntPtr indicesRaw = Marshal.AllocHGlobal(indices.Length * sizeof(int));
		Marshal.Copy(vertices, 0, verticesRaw, vertices.Length);
		Marshal.Copy(indices, 0, indicesRaw, indices.Length);

		IntPtr sector = Native.CreateSectorFromInputMesh(system, verticesRaw, vertices.Length / 3, indicesRaw, indices.Length / 3, mergeVertices, mergeSize, fixTopology);

		Marshal.FreeHGlobal(verticesRaw);
		Marshal.FreeHGlobal(indicesRaw);

		if (sector == IntPtr.Zero)
		{
			UnityEngine.Debug.LogError(ErrorMessage);
			return null;
		}

		return new Sector
		{
			_ref = sector
		};
	}

	internal void RemoveSector(Sector sector)
	{
		Native.DestroySector(system, sector._ref);
	}

	internal void SetSectorMaximumSnappingHeight(Sector sector, float snappingHeight)
	{
		Native.SetSectorMaximumSnappingHeight(sector._ref, snappingHeight);
	}

	internal Obstacle CreateObstacle(Sector sector, Vector3[] points, Vector3 pivot, Vector3 up, float height)
	{
		float[] coordinates = new float[points.Length * 3];
		for (int i = 0; i < points.Length; ++i)
		{
			coordinates[i * 3] = points[i].x;
			coordinates[i * 3 + 1] = points[i].z;
			coordinates[i * 3 + 2] = points[i].y;
		}

		IntPtr coordinatesRaw = Marshal.AllocHGlobal(coordinates.Length * sizeof(float));
		Marshal.Copy(coordinates, 0, coordinatesRaw, coordinates.Length);

		IntPtr obstacle = Native.CreateObstacle(system, sector._ref, coordinatesRaw, points.Length, new Float3(pivot), new Float3(up), height);
		if (obstacle == IntPtr.Zero)
		{
			UnityEngine.Debug.LogError(ErrorMessage);
			return null;
		}

		Marshal.FreeHGlobal(coordinatesRaw);

		return new Obstacle
		{
			_ref = obstacle
		};
	}

	internal void RemoveObstacle(Sector sector, Obstacle obstacle)
	{
		Native.DestroyObstacleOrPlatform(system, sector._ref, obstacle._ref);
	}

	internal Platform CreatePlatform(Sector sector, Vector3[] points, Vector3 pivot, Vector3 up, float height)
	{
		float[] coordinates = new float[points.Length * 3];
		for (int i = 0; i < points.Length; ++i)
		{
			coordinates[i * 3] = points[i].x;
			coordinates[i * 3 + 1] = points[i].z;
			coordinates[i * 3 + 2] = points[i].y;
		}

		IntPtr coordinatesRaw = Marshal.AllocHGlobal(coordinates.Length * sizeof(float));
		Marshal.Copy(coordinates, 0, coordinatesRaw, coordinates.Length);

		IntPtr platform = Native.CreatePlatform(system, sector._ref, coordinatesRaw, points.Length, new Float3(pivot), new Float3(up), height);
		if (platform == IntPtr.Zero)
		{
			UnityEngine.Debug.LogError(ErrorMessage);
			return null;
		}

		Marshal.FreeHGlobal(coordinatesRaw);

		return new Platform
		{
			_ref = platform
		};
	}

	internal void RemovePlatform(Sector sector, Platform platform)
	{
		if (!Native.DestroyObstacleOrPlatform(system, sector._ref, platform._ref))
			UnityEngine.Debug.LogError(ErrorMessage);
	}


	internal Agent CreateAgent(Agent.AgentProperties properties)
	{
		IntPtr agent = Native.CreateAgent(system, properties);

		return new Agent
		{
			_ref = agent
		};
	}

	internal void SetAgentProperties(Agent agent, Agent.AgentProperties properties)
	{
		Native.SetAgentProperties(agent._ref, properties);
	}

	internal void MoveAgentTo(Agent agent, Sector sector, Vector3 position, float orientation)
	{
		if (!Native.MoveAgentTo(agent._ref, sector._ref, new Float3(position), orientation))
			UnityEngine.Debug.LogError(ErrorMessage);
	}

	internal bool IsAgentEnabled(Agent agent)
	{
		return Native.IsAgentEnabled(agent._ref);
	}

	internal void SetAgentGlobalGoal(Agent agent, Vector3 goalPosition)
	{
		if (!Native.SetAgentGlobalGoal(agent._ref, new Float3(goalPosition)))
			UnityEngine.Debug.LogError(ErrorMessage);
	}
	internal void AddAgentGlobalGoal(Agent agent, Vector3 goalPosition)
	{
		if (!Native.AddAgentGlobalGoal(agent._ref, new Float3(goalPosition)))
			UnityEngine.Debug.LogError(ErrorMessage);
	}
	internal void ClearAgentGlobalGoal(Agent agent)
	{
		if (!Native.ClearAgentGlobalGoal(agent._ref))
			UnityEngine.Debug.LogError(ErrorMessage);
	}

	internal Vector3 GetAgentPosition(IntPtr agentRef)
	{
		return (Vector3)Native.GetAgentPosition(agentRef);
	}
	internal float GetAgentOrientation(IntPtr agentRef)
	{
		return Native.GetAgentOrientation(agentRef);
	}
	internal Vector2 GetAgentLocalSpeed(IntPtr agentRef)
	{
		return (Vector2)Native.GetAgentLocalSpeed(agentRef);
	}

	internal NaviPro.GoalStatus GetAgentGoalStatus(IntPtr agentRef)
	{
		return (NaviPro.GoalStatus)Native.GetAgentGoalStatus(agentRef);
	}
	internal NaviPro.PathStatus GetAgentPathStatus(IntPtr agentRef)
	{
		return (NaviPro.PathStatus)Native.GetAgentPathStatus(agentRef);
	}

	internal bool IsAgentOnPlatform(IntPtr agentRef)
	{
		return Native.IsAgentOnPlatform(agentRef);
	}

	internal void DestroyAgent(Agent agent)
	{
		Native.DestroyAgent(system, agent._ref);
	}


	internal Vector3 GetUpVectorAtPosition(Sector sector, Vector3 position)
	{
		return (Vector3)Native.GetUpVectorAtPosition(sector._ref, new Float3(position));
	}
	internal Vector3 GetUpVectorForAgent(IntPtr agentRef, Vector3 position)
	{
		return (Vector3)Native.GetUpVectorForAgent(agentRef, new Float3(position));
	}


	//internal bool PrepareForSimulation(float deltaTime)
	//{
	//	return Native.PrepareForSimulation(system, deltaTime);
	//}
	internal bool AgentPrepareForSimulation(IntPtr agentRef)
	{
		return Native.AgentPrepareForSimulation(agentRef);
	}
	internal void AgentSimulateMovement(IntPtr agentRef, float deltaTime)
	{
		if (!Native.AgentSimulateMovement(agentRef, deltaTime))
			UnityEngine.Debug.LogError(ErrorMessage);
	}
	internal void AgentApplySimulatedMovement(IntPtr agentRef, float deltaTime)
	{
		if (!Native.AgentApplySimulatedMovement(agentRef, deltaTime))
			UnityEngine.Debug.LogError(ErrorMessage);
	}
	internal void AgentUpdateSpatialIndexing(IntPtr agentRef)
	{
		if (!Native.AgentUpdateSpatialIndexing(agentRef))
			UnityEngine.Debug.LogError(ErrorMessage);
	}


	internal List<Vector3> GetECMMedialAxis(Sector sector)
	{
		int numEdges = Native.GetECMMedialAxisNumEdges(system, sector._ref);
		if (numEdges == 0)
			return new List<Vector3>();

		IntPtr coordinatesRaw = Marshal.AllocHGlobal(numEdges * 6 * sizeof(float));

		Native.GetECMMedialAxisEdges(system, sector._ref, coordinatesRaw);

		float[] coordinates = new float[numEdges * 6];
		Marshal.Copy(coordinatesRaw, coordinates, 0, numEdges * 6);

		Marshal.FreeHGlobal(coordinatesRaw);

		List<Vector3> vertices = new List<Vector3>(numEdges * 2);
		for (int i = 0; i < numEdges * 2; ++i)
		{
			vertices.Add(new Vector3(coordinates[i * 3 + 0], coordinates[i * 3 + 2], coordinates[i * 3 + 1]));
		}

		return vertices;
	}

	internal List<Vector3> GetBoundary(Sector sector)
	{
		int numEdges = Native.GetBoundaryNumEdges(system, sector._ref);
		if (numEdges == 0)
			return new List<Vector3>();

		IntPtr coordinatesRaw = Marshal.AllocHGlobal(numEdges * 6 * sizeof(float));

		Native.GetBoundaryEdges(system, sector._ref, coordinatesRaw);

		float[] coordinates = new float[numEdges * 6];
		Marshal.Copy(coordinatesRaw, coordinates, 0, numEdges * 6);

		Marshal.FreeHGlobal(coordinatesRaw);

		List<Vector3> vertices = new List<Vector3>(numEdges * 2);
		for (int i = 0; i < numEdges * 2; ++i)
		{
			vertices.Add(new Vector3(coordinates[i * 3 + 0], coordinates[i * 3 + 2], coordinates[i * 3 + 1]));
		}

		return vertices;
	}

	internal List<Vector2> GetPath(Sector sector, Vector3 start, Vector3 end, float agentRadius, float safeDistance)
	{
		IntPtr path = Native.ComputeNewPath(sector._ref, new Float3(start), new Float3(end), agentRadius, safeDistance);
		if (path == IntPtr.Zero)
			return new List<Vector2>();

		int numPoints = Native.GetComputedPathNumPoints(path);
		if (numPoints == 0)
			return new List<Vector2>();

		IntPtr coordinatesRaw = Marshal.AllocHGlobal(numPoints * 2 * sizeof(float));

		Native.GetComputedPathPoints(path, coordinatesRaw);
		float[] coordinates = new float[numPoints * 2];
		Marshal.Copy(coordinatesRaw, coordinates, 0, numPoints * 2);
		Marshal.FreeHGlobal(coordinatesRaw);
		Native.DestroyComputedPath(path);

		List<Vector2> points = new List<Vector2>(numPoints);
		for (int i = 0; i < numPoints; ++i)
		{
			points.Add(new Vector2(coordinates[i * 2 + 0], coordinates[i * 2 + 1]));
		}

		return points;
	}


	internal bool IsPointOnSector(Sector sector, Vector3 point)
	{
		return Native.IsPointOnSector(sector._ref, new Float3(point));
	}


	internal bool FindNearestAgentPositionWithinRadius(Sector sector, Vector3 point, float radius, out Vector3 agentPosition)
	{
		var agent = Native.FindNearestAgentWithinRadius(sector._ref, new Float3(point), radius);
		if (agent == IntPtr.Zero)
		{
			agentPosition = Vector3.zero;
			return false;
		}
		agentPosition = GetAgentPosition(agent);
		return true;
	}


	internal void SetAgentCustomControl(Agent agent, bool customControl)
	{
		Native.SetAgentCustomControl(agent._ref, customControl);
	}

	internal void SetAgentDesiredSpeed(Agent agent, Vector2 desiredSpeed)
	{
		Native.SetAgentDesiredSpeed(agent._ref, new Float2(desiredSpeed));
	}

	internal NaviPro.AgentControlData GetAgentControlData(Agent agent)
	{
		NaviPro.AgentControlData data = new NaviPro.AgentControlData();

		NativeAgentControlData nativeData = Native.GetAgentControlData(agent._ref);
		data.position = (Vector2)nativeData.position;
		data.nearestObstacle = (Vector2)nativeData.nearestObstacle;
		data.centerObstacleDistance = nativeData.centerObstacleDistance;
		data.nearestPathPoint = (Vector2)nativeData.nearestPathPoint;
		data.nearestPathTangent = (Vector2)nativeData.nearestPathTangent;
		data.suggestedPathingDirection = (Vector2)nativeData.suggestedPathingDirection;

		IntPtr resultAgentSet = Native.FindNearbyAgentsFromAgent(agent._ref);
		int numNearbyAgents = Native.GetNumNearbyAgents(resultAgentSet);
		IntPtr agentDataRaw = Marshal.AllocHGlobal(numNearbyAgents * 5 * sizeof(float));
		Native.GetNearbyAgentData(resultAgentSet, agentDataRaw);
		float[] agentDataFloat = new float[numNearbyAgents * 5];
		Marshal.Copy(agentDataRaw, agentDataFloat, 0, numNearbyAgents * 5);
		Marshal.FreeHGlobal(agentDataRaw);
		Native.DestroyNearbyAgentResult(resultAgentSet);

		data.nearbyAgents = new List<NaviPro.NearbyAgentData>();
		for (int i = 0; i < numNearbyAgents; ++i)
		{
			NaviPro.NearbyAgentData nearbyAgent;
			nearbyAgent.position = new Vector2(agentDataFloat[i * 5 + 0], agentDataFloat[i * 5 + 1]);
			nearbyAgent.speed = new Vector2(agentDataFloat[i * 5 + 2], agentDataFloat[i * 5 + 3]);
			nearbyAgent.radius = agentDataFloat[i * 5 + 4];
			data.nearbyAgents.Add(nearbyAgent);
		}

		return data;
	}

	IntPtr system;


	[StructLayout(LayoutKind.Sequential)]
	struct Float2
	{
		public float x;
		public float y;
		public Float2(Vector2 v)
		{
			x = v.x;
			y = v.y;
		}
		public static explicit operator Vector2(Float2 v)
		{
			return new Vector2(v.x, v.y);
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	struct Float3
	{
		public float x;
		public float y;
		public float z;
		public Float3(Vector3 v)
		{
			x = v.x;
			y = v.z;
			z = v.y;
		}
		public static explicit operator Vector3(Float3 v)
		{
			return new Vector3(v.x, v.z, v.y);
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	struct NativeAgentControlData
	{
		public Float2 position;
		public Float2 nearestObstacle;
		public float centerObstacleDistance;
		public Float2 nearestPathPoint;
		public Float2 nearestPathTangent;
		public Float2 suggestedPathingDirection;
	};

	[StructLayout(LayoutKind.Sequential)]
	struct NativeNearbyAgentData
	{
		public Float2 position;
		public Float2 speed;
		public float radius;
	};

	class Native
	{
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr Create();
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern void Destroy(IntPtr system);

		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr CreateSectorFromInputMesh(IntPtr system, IntPtr vertices, int numVertices, IntPtr indices, int numTriangles, bool mergeVertices, float mergeSize, bool fixTopology);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern void DestroySector(IntPtr system, IntPtr sector);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern void SetSectorMaximumSnappingHeight(IntPtr sector, float snappingHeight);

		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr CreateObstacle(IntPtr system, IntPtr sector, IntPtr coordinates, int numPoints, Float3 pivot, Float3 up, float height);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr CreatePlatform(IntPtr system, IntPtr sector, IntPtr coordinates, int numPoints, Float3 pivot, Float3 up, float height);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool DestroyObstacleOrPlatform(IntPtr system, IntPtr sector, IntPtr obstaclePlatform);

		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr CreateAgent(IntPtr system, Agent.AgentProperties properties);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SetAgentProperties(IntPtr agent, Agent.AgentProperties properties);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool MoveAgentTo(IntPtr agent, IntPtr sector, Float3 position, float orientation);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool IsAgentEnabled(IntPtr agent);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool SetAgentGlobalGoal(IntPtr agent, Float3 goalPosition);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool AddAgentGlobalGoal(IntPtr agent, Float3 goalPosition);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool ClearAgentGlobalGoal(IntPtr agent);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern Float3 GetAgentPosition(IntPtr agent);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern float GetAgentOrientation(IntPtr agent);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern Float2 GetAgentLocalSpeed(IntPtr agent);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern int GetAgentGoalStatus(IntPtr agent);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern int GetAgentPathStatus(IntPtr agent);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool IsAgentOnPlatform(IntPtr agent);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern void DestroyAgent(IntPtr system, IntPtr agent);

		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern Float3 GetUpVectorAtPosition(IntPtr sector, Float3 position);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern Float3 GetUpVectorForAgent(IntPtr agent, Float3 position);

		//[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		//public static extern bool PrepareForSimulation(IntPtr system, float deltaTime);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool AgentPrepareForSimulation(IntPtr agent);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool AgentSimulateMovement(IntPtr agent, float deltaTime);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool AgentApplySimulatedMovement(IntPtr agent, float deltaTime);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool AgentUpdateSpatialIndexing(IntPtr agent);

		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern int GetECMMedialAxisNumEdges(IntPtr system, IntPtr sector);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern void GetECMMedialAxisEdges(IntPtr system, IntPtr sector, IntPtr coordinates);

		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern int GetBoundaryNumEdges(IntPtr system, IntPtr sector);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern void GetBoundaryEdges(IntPtr system, IntPtr sector, IntPtr coordinates);

		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr ComputeNewPath(IntPtr sector, Float3 start, Float3 end, float agentRadius, float safeDistance);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern int GetComputedPathNumPoints(IntPtr path);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern void GetComputedPathPoints(IntPtr path, IntPtr coordinates);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern void DestroyComputedPath(IntPtr path);

		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool IsPointOnSector(IntPtr sector, Float3 point);

		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr FindNearbyAgentsFromAgent(IntPtr agent);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern int GetNumNearbyAgents(IntPtr resultAgentSet);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern void GetNearbyAgentData(IntPtr resultAgentSet, IntPtr data);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern void DestroyNearbyAgentResult(IntPtr resultAgentSet);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr FindNearestAgentWithinRadius(IntPtr sector, Float3 point, float radius);

		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern void SetAgentCustomControl(IntPtr agent, bool customControl);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern void SetAgentDesiredSpeed(IntPtr agent, Float2 desiredSpeed);
		[DllImport("NaviPro", CallingConvention = CallingConvention.Cdecl)]
		public static extern NativeAgentControlData GetAgentControlData(IntPtr agent);
	}
}
