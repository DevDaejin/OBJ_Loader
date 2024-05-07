using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Concurrent
{
    public class AssetLoader : MonoBehaviour
    {
        [SerializeField] public LoaderModule LoaderModule { get; set; }
        private StringBuilder pathSb = new StringBuilder();
        private const string Exetension = ".obj";
        private LoadingUI loadingUI;
        private System.Diagnostics.Stopwatch sw;
        //쓰레드 관련
        private List<LoaderModule> loaderModules = new List<LoaderModule>();
        private int modelSize = 20;
        private int threadSize = 20;
        private SemaphoreSlim semaphore;
        private ConcurrentQueue<Action> mainThreadCallback = new ConcurrentQueue<Action>();
        private Action callback;

        private void Start()
        {   
            loadingUI = FindObjectOfType<LoadingUI>(true);

            semaphore = new SemaphoreSlim(threadSize);
            for (int i = 0; i < modelSize; i++)
            {
                loaderModules.Add(new LoaderModule());
            }
        }

        public void GetAsset()
        {
            SelectObjFile();
        }

        private void SelectObjFile()
        {
            List<string> selectedAssetNames = GetObjFiles("/Resources/Models");
            Load(selectedAssetNames);
        }

        private List<string> GetObjFiles(string directory)
        {
            List<string> fileNames = new List<string>();

            pathSb.Clear();
            pathSb.Append(Application.dataPath).Append(directory);

            DirectoryInfo dirInfo = null;
            if (Directory.Exists(pathSb.ToString()))
            {
                dirInfo = new DirectoryInfo(pathSb.ToString());
            }

            if (dirInfo != null)
            {
                FileInfo[] fileInfoArr = dirInfo.GetFiles();

                //오름차순
                fileInfoArr = fileInfoArr.OrderBy((info) => info.Length).ToArray();

                for (int i = 0; i < fileInfoArr.Length; i++)
                {
                    if (fileInfoArr[i].Extension.Equals(Exetension))
                    {//obj 파일만
                        fileNames.Add(fileInfoArr[i].FullName);
                    }
                }
            }

            return fileNames;
        }

        public async void Load(List<string> assetName)
        {
            loadingUI.gameObject.SetActive(true);

            // 이전 오브젝트 초기화
            ObjFileGameObjectPool.Instance.ReturnObjectAll();

            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            // 병렬 처리
            List<Task> tasks = new List<Task>();

            for (int i = 0; i < assetName.Count && i < threadSize; i++)
            {
                int index = i; // closure 이슈

                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        // 데이터 파싱
                        await loaderModules[index].LoadAssetAsync(assetName[index]);

                        mainThreadCallback.Enqueue(() =>
                        {//Unity api 처리용 콜백, 오브젝트 생성
                            loaderModules[index].BuildObj().transform.SetParent(transform);
                        });
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
            loadingUI.gameObject.SetActive(false);
        }

        private void Update()
        {
            while(mainThreadCallback.Count > 0)
            {
                mainThreadCallback.TryDequeue(out callback);
                callback?.Invoke();
            }
        }
    }
}