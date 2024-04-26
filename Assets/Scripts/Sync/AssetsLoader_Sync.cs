using UnityEditor;
using UnityEngine;

public partial class AssetLoader : AssetLoaderBase
{
    public override void GetAsset(LoadType type)
    {
        if (type.Equals(LoadType.Sync))
        {
            SelectObjFile();
        }
    }

    private void SelectObjFile()
    {
        //asset path
        string selectedAssetName = EditorUtility.OpenFilePanel("Select obj model", "", "obj");
        Load(selectedAssetName);
    }

    public void Load(string assetName)
    {
        LoaderModule.OnLoadCompleted += OnLoadCompleted;
        LoaderModule.LoadAsset(assetName);
    }

    private void OnLoadCompleted(GameObject loadedAsset)
    {
        loadedAsset.transform.SetParent(transform);
    }
}
