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
}