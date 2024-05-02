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

            // 콜백
            LoaderModule.OnLoadCompleted += OnLoadCompleted;
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
            // 이전 오브젝트 초기화
            ObjFileGameObjectPool.Instance.ReturnObjectAll();

            foreach(Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            // 데이터 파싱 & 생성
            LoaderModule.LoadAsset(assetName);
        }

        private void OnLoadCompleted(GameObject loadedAsset)
        {
            loadedAsset.transform.SetParent(transform);
        }
    }
}