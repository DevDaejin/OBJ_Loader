using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class ObjMeshBuilder
{
    private MeshData meshData;

    //uint16 임계치
    private const int Limited = 65536;

    public ObjMeshBuilder(MeshData meshData)
    {
        this.meshData = meshData;
    }

    public void Build(ref GameObject go)
    {
        go.name = meshData.MeshGameObjectName;

        int vertexOffset = meshData.Triangles.Min();

        for (int i = 0; i < meshData.Triangles.Count; i++)
        {
            meshData.Triangles[i] -= vertexOffset;
        }

        go.GetComponent<MeshFilter>().sharedMesh = CreateMesh();
    }

    private Mesh CreateMesh()
    {
        //index가 특정 수 이상 넘어가면 모델이 정상적으로 출력 되지 않음.
        IndexFormat format = 
            meshData.Vertices.Count> Limited 
            ? IndexFormat.UInt32 : IndexFormat.UInt16;

        Mesh mesh = new Mesh()
        {
            indexFormat = format,
            vertices = meshData.Vertices.ToArray(),
            triangles = meshData.Triangles.ToArray(),
            uv = meshData.UVs.ToArray(),
            normals = meshData.Normals.ToArray()
        };

        if(meshData.Normals.Count == 0)
            mesh.RecalculateNormals();

        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        return mesh;
    }
}
