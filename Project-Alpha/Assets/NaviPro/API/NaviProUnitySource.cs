using System.Collections.Generic;
using UnityEngine;


public abstract class NavMeshObject
{
	public Matrix4x4 Transform { get; protected set; }
	public bool Walkable { get; protected set; }
}

public class NavMeshObjectMesh : NavMeshObject
{
	public Mesh Mesh { get; private set; }

	public NavMeshObjectMesh(Mesh mesh, Matrix4x4 transform, bool walkable)
	{
		Mesh = mesh;
		Transform = transform;
		Walkable = walkable;
	}
}

public class NavMeshObjectBox : NavMeshObject
{
	public Vector3 Size { get; private set; }

	public NavMeshObjectBox(Vector3 size, Matrix4x4 transform, bool walkable)
	{
		Size = size;
		Transform = transform;
		Walkable = walkable;
	}
}

public class NavMeshObjectTerrain : NavMeshObject
{
	public Terrain Terrain { get; private set; }

	public NavMeshObjectTerrain(Terrain terrain, Matrix4x4 transform, bool walkable)
	{
		Terrain = terrain;
		Transform = transform;
		Walkable = walkable;
	}
}


public abstract class NaviProUnitySource : MonoBehaviour
{
	abstract public List<NavMeshObject> GetSourceObjects();
}
