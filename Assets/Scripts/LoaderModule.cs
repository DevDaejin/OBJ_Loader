using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public partial class LoaderModule
{
    // 콜백
    public Action<GameObject> OnLoadCompleted;

    // 생성 기반 데이터
    private MeshData meshData;
    MatData matData;
    private Dictionary<string, MatData> matDatas = new Dictionary<string, MatData>();

    // 생성
    private List<ObjMeshBuilder> builders = new List<ObjMeshBuilder>();

    // Remap 전 parsing data
    private List<Vector3> tempVertices = new List<Vector3>();
    private List<Vector3> tempNormals = new List<Vector3>();
    private List<Vector2> tempUVs = new List<Vector2>();

    // Remap 컨테이너
    private Dictionary<(int, int, int), int> remapDict = new Dictionary<(int, int, int), int>();

    //메모리 최적화를 위해 ..
    private StringBuilder pathSb = new StringBuilder();
    private string path;
    private string objName;

    public void LoadAsset(string assetName)
    {
        objName = Path.GetFileNameWithoutExtension(assetName);

        GameObject root = new GameObject(objName);

        // 생성 시간 측정 시작
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

        // 데이터 파싱
        ParseDatas(assetName);

        // 데이터 생성
        GameObject[] gos = BuildAll();

        // 부모 할당
        for (int i = 0; i < gos.Length; i++)
        {
            gos[i].transform.SetParent(root.transform);
        }
        // 생성 시간 측정 종료
        sw.Stop();
        Debug.Log($"{root.name}(sync) - {sw.ElapsedMilliseconds * 0.001f} sec");

        // 콜백
        OnLoadCompleted.Invoke(root);
    }
   
    private void ParseDatas(string path)
    {
        this.path = path;

        using (StreamReader sr = new StreamReader(path))
        {
            ClearAll();

            string lineData;

            while ((lineData = sr.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(lineData)) continue;

                ParseLine(lineData);
            }
        }
    }

    private void ClearAll()
    {
        tempVertices.Clear();
        tempNormals.Clear();
        tempUVs.Clear();
        remapDict.Clear();

        builders.Clear();
    }

    private void ParseLine(string line)
    {
        string[] words = line.Split(ConstValue.BlankSplitChar, StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0) return;

        switch (words[0])//Token
        {
            case ConstValue.MatLibraryToken:
                {
                    pathSb.Clear();
                    pathSb.Append(Path.GetDirectoryName(path)).Append("/").Append(words[1]);

                    if (File.Exists(pathSb.ToString()))
                    {
                        UpdateMaterials(pathSb.ToString());
                    }
                }
                break;

            case ConstValue.ObjectToken:
            case ConstValue.GroupToken:
                {
                    if(words.Length > 1) CreateNewMeshData(words[1]);
                }
                break;

            case ConstValue.VerticesToken:
                {
                    tempVertices.Add(ToVector(words.Skip(1).ToArray()));
                }
                break;
            case ConstValue.UVsToken:
                {
                    tempUVs.Add(ToVector(words.Skip(1).ToArray(), true));
                }
                break;
            case ConstValue.NormalsToken:
                {
                    tempNormals.Add(ToVector(words.Skip(1).ToArray()));
                }
                break;
            case ConstValue.FaceToken:
                {
                    UpdateFaceData(words.Skip(1).ToArray());
                }
                break;
            case ConstValue.UseMaterialToken:
                {
                    if (matDatas.Keys.Contains(words[1]))
                    {
                        meshData.MatDatas.Add(words[1]);
                    }
                }
                break;
        }
    }

    private void CreateNewMeshData(string name)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            return; 

        meshData = new MeshData();
        meshData.MeshGameObjectName = name;
        builders.Add(new ObjMeshBuilder(meshData));
    }

    private void UpdateFaceData(string[] data)
    {
        int[] vertexIndecies = new int[data.Length];

        for (int i = 0; i < data.Length; i++)
        {

            // Vtx
            int vertexIndex = GetFaceDataElement(data[i], FaceType.Vertex);
            if (vertexIndex < 0) vertexIndex += tempVertices.Count + 1;

            // UVS
            int uvIndex = GetFaceDataElement(data[i], FaceType.UV);
            if (uvIndex < 0) uvIndex += tempUVs.Count + 1;
            
            // Nor
            int normalIndex = GetFaceDataElement(data[i], FaceType.Normal);
            if (normalIndex < 0) normalIndex += tempNormals.Count + 1;

            // Remap
            (int, int, int) key = (vertexIndex, uvIndex, normalIndex);

            if (!remapDict.ContainsKey(key))
            {
                if (tempVertices.Count > 0)
                {
                    meshData.Vertices.Add(tempVertices[vertexIndex]);
                }

                if (tempUVs.Count > 0)
                {
                    meshData.UVs.Add(tempUVs[uvIndex]);
                }

                if (tempNormals.Count > 0)
                {
                    meshData.Normals.Add(tempNormals[normalIndex]);
                }

                vertexIndecies[i] = meshData.Vertices.Count - 1;
                remapDict[key] = vertexIndecies[i];
            }
            else
            {
                vertexIndecies[i] = remapDict[key];
            }
        }

        // 삼각 분할
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
        {// 팬 트라이앵글레이션
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

    private GameObject[] BuildAll()
    {
        MaterialManager.Instance.BuildMaterial(matDatas.Values.ToArray());

        GameObject[] gos = new GameObject[builders.Count];

        for (int i = 0; i < builders.Count; i++)
        {
            GameObject go = ObjFileGameObjectPool.Instance.GetObject();
            gos[i] = go;
            builders[i].Build(ref go);
        }

        return gos;
    }

    private int GetFaceDataElement(string data, FaceType type)
    {
        string[] elements = data.Split(ConstValue.FaceSplitChar);

        if (elements.Length <= (int)type ||
            string.IsNullOrEmpty(elements[(int)type]) ||
            string.IsNullOrWhiteSpace(elements[(int)type]))
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

    private void UpdateMaterials(string path)
    {
        using (StreamReader sr = new StreamReader(path))
        {
            string dirPath = Path.GetDirectoryName(path);

            string lineData;
            while ((lineData = sr.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(lineData)) continue;

                MTLDataparsing(lineData);
            }
        }
    }

    private void MTLDataparsing(string line)
    {
        string[] words = line.Split(ConstValue.BlankSplitChar, StringSplitOptions.RemoveEmptyEntries);

        //Token
        switch (words[0])
        {
            case ConstValue.NewMaterialToken:
                {
                    matData = new MatData();
                    matData.MatName = words[1];
                    if (!matDatas.ContainsKey(words[1]))
                    {
                        matDatas.Add(words[1], matData);
                    }
                }
                break;

            case ConstValue.Kd: // Kd = Diffuse Color, Albedo
                {
                    matData.Albedo = new Color(
                        float.Parse(words[1], CultureInfo.InvariantCulture),
                        float.Parse(words[2], CultureInfo.InvariantCulture),
                        float.Parse(words[3], CultureInfo.InvariantCulture),
                        1);
                }
                break;

            case ConstValue.Ks: // Ks = Specular Color, Metallic
                {
                    matData.Metallic = float.Parse(words[1], CultureInfo.InvariantCulture);
                }
                break;

            case ConstValue.Ns: // Ns = Smoothness, Glossiness
                {
                    matData.Glossiness = float.Parse(words[1], CultureInfo.InvariantCulture) * 0.001f;
                }
                break;

            case ConstValue.Map_Ns: // Map_Ns = Glossiness, Smoothness 텍스처
                {
                    if (words.Length > 1)
                    {
                        pathSb.Clear();
                        pathSb.Append(Path.GetDirectoryName(path)).Append("/").Append(words[words.Length - 1]);
                        matData.TextureMapPaht = pathSb.ToString();
                    }
                }
                break;

            case ConstValue.Map_Kd: // Map_Kd = Diffuse Texture, Albedo 텍스처
                {
                    if (words.Length > 1)
                    {
                        pathSb.Clear();
                        pathSb.Append(Path.GetDirectoryName(path)).Append("/").Append(words[words.Length - 1]);
                        matData.TextureMapPaht = pathSb.ToString();
                    }
                }
                break;

            case ConstValue.Map_Bump: // Map_Bump = Bump Mapping, Normal Map 텍스처
                {
                    if (words.Length > 1)
                    {
                        pathSb.Clear();
                        pathSb.Append(Path.GetDirectoryName(path)).Append("/").Append(words[words.Length - 1]);
                        matData.NormalMapPath = pathSb.ToString();
                    }
                }
                break;

            case ConstValue.d: // Alpha
                {
                    matData.Alpha = float.Parse(words[1], CultureInfo.InvariantCulture);
                }
                break;

            case ConstValue.Ke: // Emission
                {
                    matData.Emission = new Color(
                        float.Parse(words[1], CultureInfo.InvariantCulture),
                        float.Parse(words[2], CultureInfo.InvariantCulture),
                        float.Parse(words[3], CultureInfo.InvariantCulture),
                        1);
                }
                break;
        }
    }
}