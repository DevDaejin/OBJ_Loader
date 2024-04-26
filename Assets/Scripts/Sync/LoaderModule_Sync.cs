using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LoaderModule : LoaderModuleBase
{
    public override void LoadAsset(string assetName)
    {
        var go = GameObject.Find("Cube");
        go.GetComponent<MeshFilter>().mesh = CreateObjMesh(assetName);

        OnLoadCompleted.Invoke(go);
    }
}
