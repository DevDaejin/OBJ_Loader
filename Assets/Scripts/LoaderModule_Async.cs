using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public partial class LoaderModule
{
    private System.Diagnostics.Stopwatch sw;
    private StringBuilder readingSb = new StringBuilder();
    private const int BufferSize = 64;

    public async Task LoadAssetAsync(string assetName)
    {
        objName = Path.GetFileNameWithoutExtension(assetName);

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

    private async Task ParseDatasAsync(string path)
    {
        this.path = path;

        // 버퍼 세팅
        int bufferSize = 1024 * BufferSize;
        char[] buffer = new char[bufferSize];

        using (StreamReader sr = new StreamReader(path))
        {
            ClearAll();
            readingSb.Clear();

            int readChars;

            while ((readChars = await sr.ReadAsync(buffer, 0, bufferSize)) > 0)
            {
                // 버퍼만큼 데이터 추가
                readingSb.Append(buffer, 0, readChars);

                // 버퍼 데이터 텍스트화
                string content = readingSb.ToString();

                int contentEndline = content.LastIndexOf(Environment.NewLine);

                if (contentEndline != -1)
                {
                    // 파싱
                    string processed = content.Substring(0, contentEndline);
                    ParseData(processed);

                    // 처리한 데이터 삭제
                    readingSb = readingSb.Remove(0, contentEndline + Environment.NewLine.Length);
                }
            }

            // 남은 데이터 처리
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