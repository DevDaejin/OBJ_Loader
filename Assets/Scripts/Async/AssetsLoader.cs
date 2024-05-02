using System.Collections.Generic;
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
            LoaderModule = new LoaderModule();
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
            ObjFileGameObjectPool.Instance.ReturnObjectAll();

            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            await LoaderModule.LoadAssetAsync(assetName, 32);
            GameObject loadedAsset = LoaderModule.BuildObj() ;
            loadedAsset.transform.SetParent(transform);
            loadingUI.gameObject.SetActive(false);
        }
    }
}