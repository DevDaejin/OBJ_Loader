using System;
using System.Threading.Tasks;
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
            loadingUI = FindObjectOfType<LoadingUI>(true);

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

        public async void Load(string assetName)
        {
            loadingUI.gameObject.SetActive(true);

            // 이전 오브젝트 초기화
            ObjFileGameObjectPool.Instance.ReturnObjectAll();

            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            // 데이터 파싱
            await Task.Run(() => LoaderModule.LoadAssetAsync(assetName));

            // 오브젝트 생성
            LoaderModule.BuildObj().transform.SetParent(transform);

            loadingUI.gameObject.SetActive(false);
        }
    }
}