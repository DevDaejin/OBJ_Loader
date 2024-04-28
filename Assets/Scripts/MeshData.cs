using System.Collections.Generic;
using UnityEngine;

public class MeshData
{
    public string MeshGameObjectName = string.Empty;
    
    public List<Vector3> Vertices = new List<Vector3>();
    public List<Vector2> UVs = new List<Vector2>();
    public List<Vector3> Normals = new List<Vector3>();
    
    public List<Vector3Int> Faces = new List<Vector3Int>();

    public List<int> UVsIndecies = new List<int>();
    public List<int> NormalIndecies = new List<int>();
    public List<int> Triangles = new List<int>();

    public int IndexOffset;
}