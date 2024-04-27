using System;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;

public class LoaderModule : LoaderModuleBase
{
    public override void LoadAsset(string assetName)
    {
        GameObject go = new GameObject(Path.GetFileName(assetName));

        //생성 시간 측정 시작
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

        //데이터 파싱 및 Mesh화
        Mesh[] meshDatas = CreateObjMesh(assetName);

        //Mesh 적용
        for (int i = 0; i < meshDatas.Length; i++)
        {
            GameObject meshGo = ObjectPool.GetObject();
            meshGo.GetComponent<MeshFilter>().mesh = meshDatas[i];
            meshGo.transform.SetParent(go.transform);
        }

        //생성 시간 측정 종료
        sw.Stop();
        Debug.Log($"{sw.ElapsedMilliseconds * 0.001f} sec");

        //콜백
        OnLoadCompleted.Invoke(go);
    }
}
