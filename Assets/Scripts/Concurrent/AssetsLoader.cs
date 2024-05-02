using System;
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
        private List<LoaderModule> loaderModules = new List<LoaderModule>();

        private StringBuilder pathSb = new StringBuilder();
        private const string Exetension = ".obj";

        private LoadingUI loadingUI;

        private int modelSize = 20;
        private int threadSize = 20;
        private SemaphoreSlim semaphore;

        private Queue<Action> mainThreadAction = new Queue<Action>();
        private Action deqeued;

        private void Start()
        {
            loadingUI = FindObjectOfType<LoadingUI>(true);

            for (int i = 0; i < modelSize; i++)
            {
                loaderModules.Add(new LoaderModule());
            }

            semaphore = new SemaphoreSlim(threadSize);
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

            if (Directory.Exists(pathSb.ToString()))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(pathSb.ToString());
                FileInfo[] fileInfoArr = dirInfo.GetFiles();

                fileInfoArr = fileInfoArr.OrderBy((info) => info.Length).ToArray();

                for (int i = 0; i < fileInfoArr.Length; i++)
                {
                    if (fileInfoArr[i].Extension.Equals(Exetension))
                    {
                        fileNames.Add(fileInfoArr[i].FullName);
                    }
                }
            }

            return fileNames;
        }

        public async void Load(List<string> assetName)
        {
            loadingUI.gameObject.SetActive(true);
            ObjFileGameObjectPool.Instance.ReturnObjectAll();

            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            List<Task> tasks = new List<Task>();

            for (int i = 0; i < assetName.Count && i < threadSize; i++)
            {
                int index = i; // 클로저 캡처 문제 해결
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync(); // 세마포어 활용하여 동시 실행 제한
                    try
                    {
                        await loaderModules[index].LoadAssetAsync(assetName[index]);
                        lock (mainThreadAction)
                        {
                            mainThreadAction.Enqueue(() =>
                            {
                                loaderModules[index].BuildObj().transform.SetParent(transform);
                            });
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);

            Debug.Log("XXX");

            loadingUI.gameObject.SetActive(false);
        }

        private void Update()
        {
            while(mainThreadAction.Count > 0)
            {
                lock(mainThreadAction)
                {
                    deqeued = mainThreadAction.Dequeue();
                }

                deqeued?.Invoke();
            }
        }
    }
}

//await Task.WhenAll(assetName.Select(async asset =>
//{
//    await semaphore.WaitAsync();
//    try
//    {
//        await loaderModules[assetName.IndexOf(asset) % loaderModules.Count].LoadAssetAsync(asset);
//    }
//    finally
//    {
//        semaphore.Release();
//        loaderModules[assetName.IndexOf(asset)]
//            .BuildObj()
//            .transform
//            .SetParent(transform);
//    }
//}));