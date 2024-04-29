using System.IO;
using UnityEngine;

public partial class LoaderModule : LoaderModuleBase
{
    public override void LoadAsset(string assetName)
    {
        GameObject root = new GameObject(Path.GetFileNameWithoutExtension(assetName));

        //생성 시간 측정 시작
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

        //모델 생성
        GameObject[] gos = CreateObjMesh(assetName);

        //부모 할당
        for (int i = 0; i < gos.Length; i++)
        {
            gos[i].transform.SetParent(root.transform);
        }

        //생성 시간 측정 종료
        sw.Stop();
        Debug.Log($"{sw.ElapsedMilliseconds * 0.001f} sec");

        //콜백
        OnLoadCompleted.Invoke(root);
    }
}
