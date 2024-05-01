using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Concurrent
{
    public class AssetLoader : MonoBehaviour
    {
        [SerializeField] public LoaderModule LoaderModule { get; set; }

        private StringBuilder sb = new StringBuilder();
        private const string Exetension = ".obj";

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
            List<string> selectedAssetNames = GetObjFiles("/Resources/Models");
            Load(selectedAssetNames);
        }

        private List<string> GetObjFiles(string directory)
        {
            List<string> fileNames = new List<string>();

            string path = sb.Append(Application.dataPath).Append(directory).ToString();

            if (Directory.Exists(path))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(path);
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
            List<Task<GameObject>> taskList = new List<Task<GameObject>>();

            for (int i = 0; i < assetName.Count; i++)
            {
                taskList.Add(LoaderModule.LoadAssetAsync(assetName[i]));
            }

            GameObject[] loadedObjs = await Task.WhenAll(taskList);

            for (int i = 0; i < loadedObjs.Length; i++)
            {
                loadedObjs[i].transform.SetParent(transform);
            }
        }
    }
}