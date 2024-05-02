using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public partial class LoaderModule : MonoBehaviour
{
    public async Task<GameObject> LoadAssetAsync(string assetName, bool isDeletePrevious = true)
    {
        GameObject root = new GameObject(Path.GetFileNameWithoutExtension(assetName));
        
        // 기존 생성 모델 초기화
        if(isDeletePrevious)
            ObjFileGameObjectPool.Instance.ReturnObjectAll();

        // 생성 시간 측정 시작
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

        //데이터 파싱
        await ParseDatasAsync(assetName);
        //데이터 생성
        GameObject[] gos = BuildAll();

        // 부모 할당
        for (int i = 0; i < gos.Length; i++)
        {
            gos[i].transform.SetParent(root.transform);
        }
        // 생성 시간 측정 종료
        sw.Stop();
        Debug.Log($"{root.name}(Async) - {sw.ElapsedMilliseconds * 0.001f} sec");
        //기존 오브젝트 제거
        DeleteAllChild(root.transform);

        return root;
    }

    // 현재 작업은 Blender 기준
    // TODO : Max, Maya도 고려 해보자
    private async Task ParseDatasAsync(string path)
    {
        this.path = path;
        int bufferSize = 1024 * 64;
        char[] buffer = new char[bufferSize];

        using (StreamReader sr = new StreamReader(path))
        {
            ClearAll();

            StringBuilder sb = new StringBuilder();
            int readChars;

            while((readChars = await sr.ReadAsync(buffer, 0, bufferSize)) > 0)
            {
                sb.Append(buffer, 0, readChars);

                string content = sb.ToString();
                int contentEndline = content.LastIndexOf(Environment.NewLine);

                if(contentEndline != -1)
                {
                    string processed = content.Substring(0, contentEndline);
                    sb = new StringBuilder(content.Substring(contentEndline + Environment.NewLine.Length));

                    string[] lineData = processed.Split(Environment.NewLine);

                    for (int i = 0; i < lineData.Length; i++)
                    {
                        if (string.IsNullOrEmpty(lineData[i])) continue;

                        DataParsing(lineData[i]);
                    }
                }
            }

            if (sb.Length > 0)
            {
                string[] lineData = sb.ToString().Split(Environment.NewLine);

                for (int i = 0; i < lineData.Length; i++)
                {
                    if (string.IsNullOrEmpty(lineData[i])) continue;

                    DataParsing(lineData[i]);
                }
            }
        }
    }
}