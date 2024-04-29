using System.Collections.Generic;
using UnityEngine;

public class MeshData
{
    public string MeshGameObjectName = string.Empty;
    public List<Material> Materials = new List<Material>();

    public List<Vector3> Vertices = new List<Vector3>();
    public List<Vector2> UVs = new List<Vector2>();
    public List<Vector3> Normals = new List<Vector3>();

    public List<int> Triangles = new List<int>();
}