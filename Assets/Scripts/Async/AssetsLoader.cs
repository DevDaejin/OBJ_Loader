using UnityEditor;
using UnityEngine;

namespace Async
{
    public class AssetLoader : MonoBehaviour
    {
        [SerializeField] public LoaderModule LoaderModule { get; set; }

        private void Start()
        {
            if (!LoaderModule)
            {
                LoaderModule =
                    FindObjectOfType<LoaderModule>() ??
                    gameObject.AddComponent<LoaderModule>();
            }
        }

        public void GetAsset()
        {
            SelectObjFile();
        }

        private void SelectObjFile()
        {
            string selectedAssetName = EditorUtility.OpenFilePanel("Select obj model", "", "obj");

            if (!string.IsNullOrEmpty(selectedAssetName))
            {
                Load(selectedAssetName);
            }
        }

        public async void Load(string assetName)
        {
            GameObject loadedAssets = await LoaderModule.LoadAssetAsync(assetName);
            loadedAssets.transform.SetParent(transform);
        }
    }
}