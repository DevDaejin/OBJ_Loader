using System;
using System.IO;
using System.Linq;
using UnityEngine;
using static UnityEngine.Mesh;

public abstract class LoaderModuleBase : MonoBehaviour
{
    public abstract void LoadAsset(string assetName);
    public Action<GameObject> OnLoadCompleted;

    // 메모리 효율을 위해 풀 사용
    public MeshGameObjectPool ObjectPool { private set; get; }

    private const string objectPrefix = "o ";
    private const char LineCharacter = '\n';

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
            // OBJ 모든 내용
            string rawData = reader.ReadToEnd();

            // OBJ에 따라 여러 메쉬를 가진 경우가 있음.
            // o 단위 분리
            string[] objectData = rawData.Split(objectPrefix);

            MeshData[] datas = new MeshData[objectData.Length];
            for (int index = 0; index < objectData.Length; index++)
            {
                //// 나눈 데이터를 파일로 확인
                //string p = Path.Combine(Application.persistentDataPath, index.ToString() + ".txt");
                //File.WriteAllText(p, objectData[index]);
                //System.Diagnostics.Process.Start(Application.persistentDataPath);

                // Mesh data 세팅
                datas[index] = new MeshData().UpdateMeshData(objectData[index].Split(LineCharacter));
            }

            // 첫 인덱스 데이터엔 o 데이터가 없음
            datas = datas.Skip(1).ToArray();


            // o 데이터 간 offset
            int vOffset = 0;

            Mesh[] meshes = new Mesh[datas.Length];

            for (int index = 0; index < datas.Length; index++)
            {
                if (index + 1 < datas.Length)
                {//다음 트라이앵글 index 조정을 위해
                    vOffset += datas[index].Triangles.Max() + 1;
                    datas[index + 1].SetTriangleOffset(vOffset);
                }

                meshes[index] = new Mesh();
                meshes[index].Clear();
                meshes[index].SetVertices(datas[index].Vertices);
                meshes[index].triangles = datas[index].Triangles.ToArray();
                meshes[index].RecalculateNormals();
            }

            return meshes.ToArray();
        }
    }
}