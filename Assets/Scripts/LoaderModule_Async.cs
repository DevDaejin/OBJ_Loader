using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public partial class LoaderModule
{
    private int bufferRatio = 1;
    private StringBuilder readingSb = new StringBuilder();
    private System.Diagnostics.Stopwatch sw;
    private string objName;
    public async Task LoadAssetAsync(string assetName, int bufferRatio = 64)
    {
        this.bufferRatio = bufferRatio;
        objName = Path.GetFileNameWithoutExtension(assetName);
        //Debug.Log($"{objName} - Thread : {System.Threading.Thread.CurrentThread.ManagedThreadId}");
        // 생성 시간 측정 시작
        sw = System.Diagnostics.Stopwatch.StartNew();

        //데이터 파싱
        await ParseDatasAsync(assetName);
    }

    public GameObject BuildObj()
    {
        GameObject root = new GameObject(objName);

        //데이터 생성
        GameObject[] gos = BuildAll();

        // 부모 할당
        for (int i = 0; i < gos.Length; i++)
        {
            gos[i].transform.SetParent(root.transform);
        }

        // 생성 시간 측정 종료
        sw.Stop();
        Debug.Log($"{root.name}(Async) - {sw.ElapsedMilliseconds * 0.001f} sec");

        return root;
    }


    // 현재 작업은 Blender 기준
    // TODO : Max, Maya도 고려 해보자
    private async Task ParseDatasAsync(string path)
    {
        this.path = path;
        int bufferSize = 1024 * bufferRatio;
        char[] buffer = new char[bufferSize];

        using (StreamReader sr = new StreamReader(path))
        {
            ClearAll();
            readingSb.Clear();

            int readChars;

            while ((readChars = await sr.ReadAsync(buffer, 0, bufferSize)) > 0)
            {
                readingSb.Append(buffer, 0, readChars);

                string content = readingSb.ToString();
                int contentEndline = content.LastIndexOf(Environment.NewLine);

                if (contentEndline != -1)
                {
                    string processed = content.Substring(0, contentEndline);
                    readingSb = readingSb.Remove(0, contentEndline + Environment.NewLine.Length);

                    ParseData(processed);
                }
            }

            if (readingSb.Length > 0)
            {
                ParseData(readingSb.ToString());
                readingSb.Clear();
            }
        }
    }
    
    private void ParseData(string data)
    {
        string[] lineData = data.Split(Environment.NewLine);

        for (int i = 0; i < lineData.Length; i++)
        {
            if (string.IsNullOrEmpty(lineData[i])) continue;

            ParseLine(lineData[i]);
        }
    }
}