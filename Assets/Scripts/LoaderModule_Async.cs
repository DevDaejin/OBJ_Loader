using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public partial class LoaderModule : MonoBehaviour
{
    public async Task<GameObject> LoadAssetAsync(string assetName, bool isDeletePrevious = true)
    {
        LoadingUI.SetActive(true);

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

        LoadingUI.SetActive(false);
        return root;
    }

    // 현재 작업은 Blender 기준
    // TODO : Max, Maya도 고려 해보자
    private async Task ParseDatasAsync(string path)
    {
        this.path = path;

        using (StreamReader sr = new StreamReader(path))
        {
            ClearAll();
            
            string rawData = await sr.ReadToEndAsync();
            string[] lines = rawData.Split(ConstValue.LineSplitChar);

            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].TrimEnd(ConstValue.LineCharR);
                DataParsing(lines[i]);
            }
        }
    }
}