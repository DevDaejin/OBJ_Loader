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

        Mesh mesh = new Mesh();
        Material[] mats = default;
        CreateData(ref mesh, ref mats);
        go.GetComponent<MeshFilter>().mesh = mesh;
        if (mats != null && mats.Length > 0)
            go.GetComponent<MeshRenderer>().materials = mats;
    }

    private void CreateData(ref Mesh mesh, ref Material[] mats)
    {
        Debug.Log(meshData.Vertices.Count());
        Debug.Log(meshData.Triangles.Count());

        if (mesh == null)
            mesh = new Mesh();

        //index가 특정 수 이상 넘어가면 모델이 정상적으로 출력 되지 않음.
        IndexFormat format =
            meshData.Vertices.Count > Limited
            ? IndexFormat.UInt32 : IndexFormat.UInt16;

        mesh.indexFormat = format;
        mesh.SetVertices(meshData.Vertices);
        mesh.SetUVs(0, meshData.UVs);
        mesh.SetNormals(meshData.Normals);
        mesh.subMeshCount = meshData.Materials.Count == 0 ? 1 : meshData.Materials.Count;
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            mesh.SetTriangles(meshData.Triangles, i);
        }

        if (meshData.Normals.Count == 0)
            mesh.RecalculateNormals();

        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        if (mats == null) mats = new Material[meshData.Materials.Count];
        mats = meshData.Materials.ToArray();
    }
}
