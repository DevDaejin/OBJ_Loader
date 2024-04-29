using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public abstract class LoaderModuleBase : MonoBehaviour
{
    public abstract void LoadAsset(string assetName);
    public Action<GameObject> OnLoadCompleted;

    public MeshGameObjectPool ObjectPool { private set; get; } // 게임 오브젝트 풀

    private List<ObjMeshBuilder> builders = new List<ObjMeshBuilder>(); // 생성
    private MeshData meshData; // 생성 기반 데이터
    private List<Material> materials = new List<Material>();
    private Material currentMaterial;

    private List<Vector3> tempVertices = new List<Vector3>();
    private List<Vector3> tempNormals = new List<Vector3>();
    private List<Vector2> tempUVs = new List<Vector2>();

    private Dictionary<(int, int, int), int> remapDict = new Dictionary<(int, int, int), int>();

    private Shader shader;

    //공통
    private const char LineSplitChar = '\n';
    private const char BlankSplitChar = ' ';
    private const char FaceSplitChar = '/';

    //obj
    private const string ObjectToken = "o";
    private const string MatLibraryToken = "mtllib";
    private const string VerticesToken = "v";
    private const string UVsToken = "vt";
    private const string NormalsToken = "vn";
    private const string FaceToken = "f";

    //mtl
    private const string MaterialToken = "newmtl";
    private const string Kd = "Kd";
    private const string Ks = "Ks";
    private const string Ns = "Ns";
    private const string Map_Kd = "Map_Kd";
    private const string Map_Bump = "Map_Bump";
    private const string d = "d";
    private const string Ke = "Ke";
    private const string NormalMap = "_NORMALMAP";
    private const string BumpMap = "_BumpMap";
    private const string MainTex = "_MainTex";
    private const string Glossiness = "_Glossiness";
    private const string Metallic = "_Metallic";

    /*        
    * Kd = Diffuse Color, Albedo
    * Ks = Specular Color, Metallic
    * Ns = Smoothness, Glossiness
    * Map_Kd = Diffuse Texture, Albedo 텍스처
    * Map_Bump = Bump Mapping, Normal Map 텍스처
    * d = 투명도
    * Ke = 에미션 컬러
    * Ni는 지원 안함.
    */


    private void Start()
    {
        ObjectPool ??=
            gameObject.AddComponent<MeshGameObjectPool>() ??
            throw new Exception("Mesh game object pool is null");

        shader = Shader.Find("Standard");
    }

    // 현재 작업은 Blender 기준
    // TODO : Max, Maya도 고려 해보자
    protected GameObject[] CreateObjMesh(string path)
    {
        using (StreamReader sr = new StreamReader(path))
        {
            string[] rawData = sr.ReadToEnd().Split(LineSplitChar);

            tempVertices.Clear();
            tempNormals.Clear();
            tempUVs.Clear();
            remapDict.Clear();

            for (int i = 0; i < rawData.Length; i++)
            {
                string[] splitLine = rawData[i].Split(BlankSplitChar);

                switch (splitLine[0])//Token
                {
                    case MatLibraryToken:
                        StringBuilder mtlPath = new StringBuilder(Path.GetDirectoryName(path));
                        mtlPath.Append("/").Append(splitLine[1]);
                        if (File.Exists(mtlPath.ToString()))
                        {
                            UpdateMaterials(mtlPath.ToString());
                        }
                        break;

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

        GameObject[] gos = new GameObject[builders.Count];

        for (int i = 0; i < builders.Count; i++)
        {
            GameObject go = ObjectPool.GetObject();
            gos[i] = go;
            builders[i].Build(ref go);
        }
        builders.Clear();

        return gos;
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

    private int GetFaceDataElement(string data, FaceType type)
    {
        string[] elements = data.Split(FaceSplitChar);

        if (string.IsNullOrEmpty(elements[(int)type]))
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
            StringBuilder builder = null;
            string dirPath = Path.GetDirectoryName(path);
            string[] lines = sr.ReadToEnd().Split(LineSplitChar);

            for (int i = 0; i < lines.Length; i++)
            {
                string[] words = lines[i].Split(BlankSplitChar);
                switch (words[0])
                {
                    case MaterialToken:
                        currentMaterial = new Material(shader);
                        materials.Add(currentMaterial);
                        break;

                    case Kd: //Kd = Diffuse Color, Albedo
                        currentMaterial.color = new Color(
                            float.Parse(words[1], CultureInfo.InvariantCulture),
                            float.Parse(words[2], CultureInfo.InvariantCulture),
                            float.Parse(words[3], CultureInfo.InvariantCulture),
                            1);
                        break;
                    case Ks: //Ks = Specular Color, Metallic
                        currentMaterial.SetFloat(Metallic, float.Parse(words[1], CultureInfo.InvariantCulture));
                        break;
                    case Ns: //Ns = Smoothness, Glossiness
                        currentMaterial.SetFloat(Glossiness, float.Parse(words[1], CultureInfo.InvariantCulture));
                        break;
                    case Map_Kd: //Map_Kd = Diffuse Texture, Albedo 텍스처
                        builder = new StringBuilder(path).Append("/").Append(words[1]);
                        currentMaterial.SetTexture(MainTex, new TextureLoader().GetTexture(builder.ToString()));
                        break;
                    case Map_Bump: //Map_Bump = Bump Mapping, Normal Map 텍스처
                        builder = new StringBuilder(path).Append("/").Append(words[1]);
                        currentMaterial.SetTexture(BumpMap, new TextureLoader().GetTexture(builder.ToString()));
                        currentMaterial.EnableKeyword(NormalMap);
                        break;
                    case d: // Alpha
                        float alpha = float.Parse(words[1], CultureInfo.InvariantCulture);
                        Color color = currentMaterial.color;
                        if (alpha != 1)
                        {
                            color.a *= alpha;
                            currentMaterial.color = color;
                            currentMaterial.SetFloat("_Mode", 3);
                        }
                        break;
                    case Ke: // Emission
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
    }
}