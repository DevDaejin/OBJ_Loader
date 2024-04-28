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

    // 메모리 효율을 위해 풀 사용
    public MeshGameObjectPool ObjectPool { private set; get; }

    //TODO : 삭제
    protected MeshData[] meshDatas;

    private const char LineSplitChar = '\n';
    private const char ElementSplitChar = ' ';
    private const char FaceSplitChar = '/';

    private const string ObjectToken = "o ";
    private const string VerticesToken = "v";
    private const string UVsToken = "vt";
    private const string NoramlsToken = "vn";
    private const string FaceToken = "f";

    private void Start()
    {
        if (ObjectPool == null)
        {
            ObjectPool =
                gameObject.AddComponent<MeshGameObjectPool>() ??
                throw new Exception("Mesh game object pool is null");
        }
    }

    // 현재 작업은 Blender 기준
    // TODO : Max, Maya도 고려 해보자
    protected Mesh[] CreateObjMesh(string path)
    {
        using (StreamReader reader = new StreamReader(path))
        {
            string[] objRawData = reader.ReadToEnd().Split(ObjectToken);

            meshDatas = new MeshData[objRawData.Length - 1];

            int stackedOffset = 0;

            for (int i = 1; i < objRawData.Length; i++)
            {
                string[] lines = objRawData[i].Split(LineSplitChar);

                MeshData meshData = new MeshData();

                int previousIndex = i - 2;

                if (previousIndex > 0 && previousIndex < meshDatas.Length - 1)
                {
                    stackedOffset += meshDatas[previousIndex].Triangles.Max() + 1;
                    meshData.IndexOffset = stackedOffset;
                }

                meshData.MeshGameObjectName = lines[0];

                foreach (string line in lines.Skip(1))
                {
                    string[] lineElement = line.Split(ElementSplitChar);
                    switch (lineElement[0])
                    {
                        case VerticesToken:
                            meshData
                                .Vertices
                                .Add(StringsToVector3(lineElement.Skip(1).ToArray()));
                            break;

                        case NoramlsToken:
                            meshData
                                .Normals
                                .Add(StringsToVector3(lineElement.Skip(1).ToArray()));
                            break;

                        case UVsToken:
                            meshData
                                .UVs
                                .Add(StringsToVector3(lineElement.Skip(1).ToArray(), true));
                            break;

                        case FaceToken:
                            UpdateFaceData(ref meshData, lineElement.Skip(1).ToArray());
                            break;
                    }
                }

                meshDatas[i - 1] = meshData;
            }            
        }

        Mesh[] meshes = new Mesh[meshDatas.Length];
        for (int i = 0; i < meshes.Length; i++)
        {
            meshes[i] = CreateMesh(meshDatas[i]);
        }

        return meshes;
    }

    private void UpdateFaceData(ref MeshData meshData, string[] data)
    {
        if(data.Length == 3)
        {// 삼각
            meshData.Triangles.Add(GetFaceDataElement(data[0], FaceType.Vertex) - meshData.IndexOffset);
            meshData.Triangles.Add(GetFaceDataElement(data[1], FaceType.Vertex) - meshData.IndexOffset);
            meshData.Triangles.Add(GetFaceDataElement(data[2], FaceType.Vertex) - meshData.IndexOffset);
        }
        else if (data.Length == 4)
        {// 사각을 삼각 2개로
            meshData.Triangles.Add(GetFaceDataElement(data[0], FaceType.Vertex) - meshData.IndexOffset);
            meshData.Triangles.Add(GetFaceDataElement(data[1], FaceType.Vertex) - meshData.IndexOffset);
            meshData.Triangles.Add(GetFaceDataElement(data[2], FaceType.Vertex) - meshData.IndexOffset);

            meshData.Triangles.Add(GetFaceDataElement(data[2], FaceType.Vertex) - meshData.IndexOffset);
            meshData.Triangles.Add(GetFaceDataElement(data[3], FaceType.Vertex) - meshData.IndexOffset);
            meshData.Triangles.Add(GetFaceDataElement(data[0], FaceType.Vertex) - meshData.IndexOffset);
        }
        else if (data.Length > 4)
        {// 원점 기준 순회
            for (int i = data.Length - 1; i >= 2; i--)
            {
                meshData.Triangles.Add(GetFaceDataElement(data[0], FaceType.Vertex) - meshData.IndexOffset);
                meshData.Triangles.Add(GetFaceDataElement(data[i - 1], FaceType.Vertex) - meshData.IndexOffset);
                meshData.Triangles.Add(GetFaceDataElement(data[i], FaceType.Vertex) - meshData.IndexOffset);
            }
        }
        else
        {
            throw new Exception("Obj face is not valid");
        }
    }


    private Vector3 StringsToVector3(string[] str, bool isVector2 = false)
    {
        return new Vector3(
            float.Parse(str[0], CultureInfo.InvariantCulture),
            float.Parse(str[1], CultureInfo.InvariantCulture),
            isVector2 ? 0 : float.Parse(str[2], CultureInfo.InvariantCulture));
    }

    private Mesh CreateMesh(MeshData meshData)
    {
        List<Vector2> newUVs = new List<Vector2>();
        List<Vector3> newNormals = new List<Vector3>();

        // 인덱스의 범위를 확인하여 예외를 방지합니다.
        for (int i = 0; i < meshData.Triangles.Count; i++)
        {
            if (meshData.UVsIndecies.Count > i && meshData.UVs.Count > meshData.UVsIndecies[i])
                newUVs.Add(meshData.UVs[meshData.UVsIndecies[i]]);
            if (meshData.NormalIndecies.Count > i && meshData.Normals.Count > meshData.NormalIndecies[i])
                newNormals.Add(meshData.Normals[meshData.NormalIndecies[i]]);
        }

        Mesh mesh = new Mesh()
        {
            vertices = meshData.Vertices.ToArray(),
            triangles = meshData.Triangles.ToArray(),
            //uv = newUVs.ToArray(),
            //normals = newNormals.ToArray()

        };

        using (StreamWriter sw = new StreamWriter(Path.Combine(Application.persistentDataPath, "A.txt")))
        {
            for (int i = 0; i < mesh.triangles.Length; i++)
            {
                sw.WriteLine(mesh.triangles[i]);
            }
        }

        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        return mesh;
    }

    private int GetFaceDataElement(string data, FaceType type)
    {
        // Index가 0부터 시작하는게 아니라 1부터 시작하므로 수정해줘야 함
        string[] parts = data.Split(FaceSplitChar);
        return int.Parse(parts[(int)type]) - 1;
    }
}


//protected Mesh[] CreateObjMesh(string path)
//{
//    using (StreamReader reader = new StreamReader(path))
//    {
//        // OBJ 모든 내용
//        string rawData = reader.ReadToEnd();

//        // OBJ에 따라 여러 메쉬를 가진 경우가 있음.
//        // o 단위 분리
//        string[] objectData = rawData.Split(objectPrefix);

//        /*MeshData[]*/ datas = new MeshData[objectData.Length];
//        for (int index = 0; index < objectData.Length; index++)
//        {
//            // 나눈 데이터를 파일로 확인
//            string p = Path.Combine(Application.persistentDataPath, index.ToString() + ".obj");
//            File.WriteAllText(p, objectData[index]);
//            System.Diagnostics.Process.Start(Application.persistentDataPath);

//            // Mesh data 세팅
//            datas[index] = new MeshData().UpdateMeshData(objectData[index].Split(LineCharacter));
//        }

//        // 첫 인덱스 데이터엔 o 데이터가 없음
//        datas = datas.Skip(1).ToArray();


//        // o 데이터 간 offset
//        int vOffset = 0;

//        Mesh[] meshes = new Mesh[datas.Length];

//        for (int index = 0; index < datas.Length; index++)
//        {
//            if (index + 1 < datas.Length)
//            {//다음 트라이앵글 index 조정을 위해
//                vOffset += datas[index].Triangles.Max() + 1;
//                datas[index + 1].SetTriangleOffset(vOffset);
//            }

//            meshes[index] = new Mesh();
//            meshes[index].Clear();
//            meshes[index].SetVertices(datas[index].Vertices);
//            meshes[index].triangles = datas[index].Triangles.ToArray();
//            meshes[index].RecalculateNormals();
//        }

//        return meshes.ToArray();
//    }
//}


//public MeshData UpdateMeshData(string[] rawMeshData)
//{
//    for (int i = 0; i < rawMeshData.Length; i++)
//    {
//        string[] splitedLine = rawMeshData[i].Split(SplitCharacter);

//        if(i == 0)
//        {
//            MeshGameObjectName = splitedLine[0];
//        }
//        switch (splitedLine[0])
//        {
//            case VerticesPrefix:
//                UpdateVertices(splitedLine.Skip(1).ToArray());
//                break;

//            case FacePrefix:
//                UpdateTriangles(splitedLine.Skip(1).ToArray());
//                break;
//        }
//    }

//    return this;
//}

//private void UpdateVertices(string[] data)
//{
//    Vertices.Add(new Vector3(
//            float.Parse(data[0], CultureInfo.InvariantCulture),
//            float.Parse(data[1], CultureInfo.InvariantCulture),
//            float.Parse(data[2], CultureInfo.InvariantCulture)));
//}

//private void UpdateTriangles(string[] data)
//{// 삼각 순환법(Fan Triangulation)
// // 첫 번째 버텍스를 기준으로 삼각형을 형성

//    // 첫 번째 버텍스 인덱스 저장
//    int firstIndex = ConvertIndex(data[0], FaceType.Vertex);

//    // n각형을 삼각형으로 분할
//    for (int i = 1; i < data.Length - 1; i++)
//    {
//        Triangles.Add(firstIndex);
//        Triangles.Add(ConvertIndex(data[i], FaceType.Vertex));
//        Triangles.Add(ConvertIndex(data[i + 1], FaceType.Vertex));
//    }
//}

//private int ConvertIndex(string data, FaceType type)
//{
//    // Index가 0부터 시작하는게 아니라 1부터 시작하므로 수정해줘야 함
//    string[] parts = data.Split(FaceSplitCharacter);
//    return int.Parse(parts[(int)type]) - 1;
//}

//public void SetTriangleOffset(int offsetValue)
//{
//    for (int i = 0; i < Triangles.Count; i++)
//    {
//        Triangles[i] -= offsetValue;
//    }
//}