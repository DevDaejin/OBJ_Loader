using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sync 
{
    public class AssetLoader : MonoBehaviour
    {
        [SerializeField] public LoaderModule LoaderModule { get; set; }

        private void Start()
        {
            LoaderModule = new LoaderModule();
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
            ObjFileGameObjectPool.Instance.ReturnObjectAll();

            foreach(Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            LoaderModule.OnLoadCompleted += OnLoadCompleted;
            LoaderModule.LoadAsset(assetName);

        }

        private void OnLoadCompleted(GameObject loadedAsset)
        {
            loadedAsset.transform.SetParent(transform);
        }
    }
}