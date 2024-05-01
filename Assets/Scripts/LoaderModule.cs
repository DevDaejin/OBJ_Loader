using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public partial class LoaderModule : MonoBehaviour
{
    // 콜백
    public Action<GameObject> OnLoadCompleted;

    // 생성 기반 데이터 ( 블루프린트 )
    private MeshData meshData;

    // 실 생성
    private List<ObjMeshBuilder> builders = new List<ObjMeshBuilder>();

    // 메테리얼 관련
    private Dictionary<string, Material> materials = new Dictionary<string, Material>();
    private Material standardMaterial;
    private Material currentMaterial;

    // Remap 전 parsing data
    private List<Vector3> tempVertices = new List<Vector3>();
    private List<Vector3> tempNormals = new List<Vector3>();
    private List<Vector2> tempUVs = new List<Vector2>();

    // Remap 컨테이너
    private Dictionary<(int, int, int), int> remapDict = new Dictionary<(int, int, int), int>();

    //메모리 최적화를 위해 ..
    private StringBuilder sb = null;

    private string path;

    private void Start()
    {
        standardMaterial = new Material(Shader.Find("Standard"));
        sb = new StringBuilder(1024);
    }

    public void LoadAsset(string assetName)
    {
        GameObject root = new GameObject(Path.GetFileNameWithoutExtension(assetName));
        // 기존 생성 모델 초기화
        ObjFileGameObjectPool.Instance.ReturnObjectAll();
        // 생성 시간 측정 시작
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        // 모델 생성
        GameObject[] gos = CreateObjMesh(assetName);
        // 부모 할당
        for (int i = 0; i < gos.Length; i++)
        {
            gos[i].transform.SetParent(root.transform);
        }
        // 생성 시간 측정 종료
        sw.Stop();
        Debug.Log($"{sw.ElapsedMilliseconds * 0.001f} sec");
        //기존 오브젝트 제거
        DeleteAllChild(root.transform);
        // 콜백
        OnLoadCompleted.Invoke(root);
    }

    // 현재 작업은 Blender 기준
    // TODO : Max, Maya도 고려 해보자
    private GameObject[] CreateObjMesh(string path)
    {
        this.path = path;

        using (StreamReader sr = new StreamReader(path))
        {
            ClearAll();

            string lineData;

            while ((lineData = sr.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(lineData)) continue;

                DataParsing(lineData);
            }
        }

        return BuildAll();
    }

    private void DeleteAllChild(Transform root)
    {
        foreach (Transform t in transform)
        {
            if (t != root)
            {
                Destroy(t.gameObject);
            }
        }
    }

    private void ClearAll()
    {
        materials.Clear();

        tempVertices.Clear();
        tempNormals.Clear();
        tempUVs.Clear();
        remapDict.Clear();

        builders.Clear();
    }

    private void DataParsing(string line)
    {
        string[] words = line.Split(ConstValue.BlankSplitChar, StringSplitOptions.RemoveEmptyEntries);

        //g와 s는 별도 처리 없음
        switch (words[0])//Token
        {
            case ConstValue.MatLibraryToken:
                sb.Clear();
                sb.Append(Path.GetDirectoryName(path)).Append("/").Append(words[1]);
                if (File.Exists(sb.ToString()))
                {
                    UpdateMaterials(sb.ToString());
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
                    if (materials.Keys.Contains(words[1]))
                    {
                        meshData.Materials.Add(materials[words[1]]);
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
            int vertexIndex = GetFaceDataElement(data[i], FaceType.Vertex);
            if (vertexIndex < 0) vertexIndex += tempVertices.Count + 1;

            int uvIndex = GetFaceDataElement(data[i], FaceType.UV);
            if (uvIndex < 0) uvIndex += tempUVs.Count + 1;
            
            int normalIndex = GetFaceDataElement(data[i], FaceType.Normal);
            if (normalIndex < 0) normalIndex += tempNormals.Count + 1;

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
                    try
                    {
                        meshData.Normals.Add(tempNormals[normalIndex]);
                    }
                    catch
                    {
                        Debug.Log(normalIndex);
                    }
                }

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
        if (string.IsNullOrEmpty(elements[(int)type]) ||
            string.IsNullOrWhiteSpace(elements[(int)type])) 
            return 0;
            
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

        switch (words[0])
        {
            case ConstValue.NewMaterialToken:
                currentMaterial = new Material(standardMaterial);
                materials.Add(words[1], currentMaterial);
                break;

            case ConstValue.Kd: //Kd = Diffuse Color, Albedo
                currentMaterial.color = new Color(
                    float.Parse(words[1], CultureInfo.InvariantCulture),
                    float.Parse(words[2], CultureInfo.InvariantCulture),
                    float.Parse(words[3], CultureInfo.InvariantCulture),
                    1);
                break;

            case ConstValue.Ks: //Ks = Specular Color, Metallic
                currentMaterial.SetFloat(ConstValue.Metallic, float.Parse(words[1], CultureInfo.InvariantCulture));
                break;

            case ConstValue.Ns: //Ns = Smoothness, Glossiness
                currentMaterial.SetFloat(ConstValue.Glossiness, float.Parse(words[1], CultureInfo.InvariantCulture) * 0.001f);
                break;

            case ConstValue.Map_Kd: //Map_Kd = Diffuse Texture, Albedo 텍스처
                sb.Clear();
                sb.Append(Path.GetDirectoryName(path)).Append("/").Append(words[1]);
                currentMaterial.SetTexture(ConstValue.MainTex, new TextureLoader().GetTexture(this.sb.ToString()));
                break;

            case ConstValue.Map_Bump: //Map_Bump = Bump Mapping, Normal Map 텍스처
                sb.Clear();
                sb.Append(Path.GetDirectoryName(path)).Append("/").Append(words[1]);
                currentMaterial.SetTexture(ConstValue.BumpMap, new TextureLoader().GetTexture(this.sb.ToString()));
                currentMaterial.EnableKeyword(ConstValue.NormalMap);
                break;

            case ConstValue.d: // Alpha
                float alpha = float.Parse(words[1], CultureInfo.InvariantCulture);
                Color color = currentMaterial.color;
                if (alpha != 1)
                {
                    color.a *= alpha;
                    currentMaterial.color = color;
                    currentMaterial.SetFloat("_Mode", 3);
                }
                break;

            case ConstValue.Ke: // Emission
                Color emission = new Color(
                    float.Parse(words[1], CultureInfo.InvariantCulture),
                    float.Parse(words[2], CultureInfo.InvariantCulture),
                    float.Parse(words[3], CultureInfo.InvariantCulture),
                    1);

                if (emission != Color.black)
                {
                    currentMaterial.SetColor("_EmissionColor", emission);
                    currentMaterial.EnableKeyword("_EMISSION");
                }
                break;
        }
    }
}