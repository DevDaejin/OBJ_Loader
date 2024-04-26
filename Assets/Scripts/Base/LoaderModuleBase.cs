using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public abstract class LoaderModuleBase : MonoBehaviour
{
    public abstract void LoadAsset(string assetName);
    public Action<GameObject> OnLoadCompleted;

    public Mesh CreateObjMesh(string path)
    {
        return null;
    }
}