using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

public abstract class LoaderModuleBase : MonoBehaviour
{
    public abstract void LoadAsset(string assetName);
    public Action<GameObject> OnLoadCompleted;

    public MeshGameObjectPool ObjectPool { private set; get; } // 게임 오브젝트 풀

    private List<ObjMeshBuilder> builders = new List<ObjMeshBuilder>(); // 생성
    private MeshData meshData; // 생성 기반 데이터

    private List<Vector3> tempVertices = new List<Vector3>();
    private List<Vector3> tempNormals = new List<Vector3>();
    private List<Vector2> tempUVs = new List<Vector2>();

    private Dictionary<(int, int, int), int> remapDict = new Dictionary<(int, int, int), int>();

    private const char LineSplitChar = '\n';
    private const char BlankSplitChar = ' ';
    private const char FaceSplitChar = '/';

    private const string ObjectToken = "o";
    private const string VerticesToken = "v";
    private const string UVsToken = "vt";
    private const string NormalsToken = "vn";
    private const string FaceToken = "f";


    private void Start()
    {
        ObjectPool ??=
            gameObject.AddComponent<MeshGameObjectPool>() ??
            throw new Exception("Mesh game object pool is null");
    }

    // 현재 작업은 Blender 기준
    // TODO : Max, Maya도 고려 해보자
    protected GameObject[] CreateObjMesh(string path)
    {
        using (StreamReader sw = new StreamReader(path))
        {
            string[] rawData = sw.ReadToEnd().Split(LineSplitChar);

            tempVertices.Clear();
            tempNormals.Clear();
            tempUVs.Clear();
            remapDict.Clear();

            foreach (string line in rawData)
            {
                string[] splitLine = line.Split(BlankSplitChar);

                switch (splitLine[0])//Token
                {
                    case ObjectToken:
                        meshData = new MeshData();
                        meshData.MeshGameObjectName = splitLine[1];
                        builders.Add(new ObjMeshBuilder(meshData));
                        break;

                    case VerticesToken:
                        tempVertices.Add(ToVector(splitLine.Skip(1).ToArray()));
                        break;

                    case UVsToken:
                        tempUVs.Add(ToVector(splitLine.Skip(1).ToArray(), true));
                        break;

                    case NormalsToken:
                        tempNormals.Add(ToVector(splitLine.Skip(1).ToArray()));
                        break;

                    case FaceToken:
                        UpdateFaceData(splitLine.Skip(1).ToArray());
                        break;
                }
            }
        }

        List<GameObject> gos = new List<GameObject>();

        for (int i = 0; i < builders.Count; i++)
        {
            GameObject go = ObjectPool.GetObject();
            gos.Add(go);
            builders[i].Build(ref go);
        }
        builders.Clear();
        return gos.ToArray();
    }

    private void UpdateFaceData(string[] data)
    {
        int[] vertexIndecies = new int[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            int vertexIndex = GetFaceDataElement(data[i], FaceType.Vertex);
            int uvIndex = GetFaceDataElement(data[i], FaceType.UV);
            int normalIndex = GetFaceDataElement(data[i], FaceType.Normal);

            (int, int, int) key = (vertexIndex, uvIndex, normalIndex);

            if (!remapDict.ContainsKey(key))
            {
                if (tempVertices.Count > 0)
                    meshData.Vertices.Add(tempVertices[vertexIndex]);

                if(tempUVs.Count > 0)
                    meshData.UVs.Add(tempUVs[uvIndex]);

                if (tempNormals.Count > 0)
                    meshData.Normals.Add(tempNormals[normalIndex]);

                vertexIndecies[i] = meshData.Vertices.Count - 1;
                remapDict[key] = vertexIndecies[i];
            }
            else
            {
                vertexIndecies[i] = remapDict[key];
            }
        }

        Triangulate(vertexIndecies);
    }

    private void Triangulate(int[] vertexIndices)
    {
        if (vertexIndices.Length == 3)
        {// 삼각
            meshData.Triangles.Add(vertexIndices[0]);
            meshData.Triangles.Add(vertexIndices[1]);
            meshData.Triangles.Add(vertexIndices[2]);
        }
        else if (vertexIndices.Length == 4)
        {// 사각을 삼각 2개로
            meshData.Triangles.Add(vertexIndices[0]);
            meshData.Triangles.Add(vertexIndices[1]);
            meshData.Triangles.Add(vertexIndices[2]);

            meshData.Triangles.Add(vertexIndices[2]);
            meshData.Triangles.Add(vertexIndices[3]);
            meshData.Triangles.Add(vertexIndices[0]);
        }
        else if (vertexIndices.Length > 4)
        {// 원점 기준 순회
            for (int j = vertexIndices.Length - 1; j >= 2; j--)
            {
                meshData.Triangles.Add(vertexIndices[0]);
                meshData.Triangles.Add(vertexIndices[j - 1]);
                meshData.Triangles.Add(vertexIndices[j]);
            }
        }
        else
        {
            throw new Exception("Obj face is not valid");
        }
    }

    private int GetFaceDataElement(string data, FaceType type)
    {
        string[] elements = data.Split(FaceSplitChar);

        if (elements.Length <= (int)type || string.IsNullOrEmpty(elements[(int)type]))
        {
            return 0;
        }

        return int.Parse(elements[(int)type], CultureInfo.InvariantCulture) - 1;
    }

    private Vector3 ToVector(string[] str, bool isVector2 = false)
    {
        Vector3 value = Vector3.zero;

        value.x = string.IsNullOrEmpty(str[0]) ? 0 : float.Parse(str[0], CultureInfo.InvariantCulture);
        value.y = string.IsNullOrEmpty(str[1]) ? 0 : float.Parse(str[1], CultureInfo.InvariantCulture);
        value.z = isVector2 ? 0 : string.IsNullOrEmpty(str[2]) ? 0 : float.Parse(str[2], CultureInfo.InvariantCulture);

        return value;
    }
}