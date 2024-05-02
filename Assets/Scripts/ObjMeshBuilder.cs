using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class ObjMeshBuilder
{
    public MeshData meshData;

    // uint16 임계치
    private const int Limited = 65536;

    public ObjMeshBuilder(MeshData meshData)
    {
        this.meshData = meshData;
    }

    public void Build(ref GameObject go)
    {
        if (meshData.Vertices.Count == 0) return;

        go.name = meshData.MeshGameObjectName;

        int vertexOffset = meshData.Triangles.Min();

        for (int i = 0; i < meshData.Triangles.Count; i++)
        {
            meshData.Triangles[i] -= vertexOffset;
        }

        go.GetComponent<MeshFilter>().mesh = CreateMesh();
        go.GetComponent<MeshRenderer>().materials = GetMaterials();
    }

    private Material[] GetMaterials()
    {
        Material[] mats = new Material[meshData.MatDatas.Count];

        if (mats.Length == 0)
        {
            mats = new Material[1];
            mats[0] = MaterialManager.Instance.DefaultMat;
        }
        else
        {
            for (int i = 0; i < meshData.MatDatas.Count; i++)
            {
                mats[i] = MaterialManager.Instance.GetMaterial(meshData.MatDatas[i]);
            }
        }
        return mats;
    }

    private Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();

        // index가 특정 수 이상 넘어가면 모델이 정상적으로 출력 되지 않음.
        IndexFormat format =
            meshData.Vertices.Count > Limited
            ? IndexFormat.UInt32 : IndexFormat.UInt16;

        mesh.indexFormat = format;
        mesh.SetVertices(meshData.Vertices);
        mesh.SetUVs(0, meshData.UVs);
        mesh.SetNormals(meshData.Normals);
        mesh.subMeshCount = meshData.MatDatas.Count;

        if (mesh.subMeshCount == 0) mesh.subMeshCount = 1;

        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            mesh.SetTriangles(meshData.Triangles, i);
        }

        if (meshData.Normals.Count == 0)
            mesh.RecalculateNormals();

        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        return mesh;
    }
}
