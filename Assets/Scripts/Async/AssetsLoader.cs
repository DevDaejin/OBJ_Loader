using UnityEditor;
using UnityEngine;

namespace Async
{
    public class AssetLoader : MonoBehaviour
    {
        [SerializeField] public LoaderModule LoaderModule { get; set; }

        private LoadingUI loadingUI;

        private void Start()
        {
            if (!LoaderModule)
            {
                LoaderModule =
                    FindObjectOfType<LoaderModule>() ??
                    gameObject.AddComponent<LoaderModule>();
            }

            loadingUI = FindObjectOfType<LoadingUI>(true);
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
            loadingUI.gameObject.SetActive(true);

            GameObject loadedAssets = await LoaderModule.LoadAssetAsync(assetName);
            loadedAssets.transform.SetParent(transform);

            loadingUI.gameObject.SetActive(false);
        }
    }
}