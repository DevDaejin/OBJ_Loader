using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public class MeshData
{
    public string MeshGameObjectName { private set; get; }
    public List<Vector3> Vertices { private set; get; } = new List<Vector3>();
    public List<int> Triangles { private set; get; } = new List<int>();

    private const char SplitCharacter = ' ';
    private const char FaceSplitCharacter = '/';

    private const string VerticesPrefix = "v";
    private const string FacePrefix = "f";

    public MeshData UpdateMeshData(string[] rawMeshData)
    {
        for (int i = 0; i < rawMeshData.Length; i++)
        {
            string[] splitedLine = rawMeshData[i].Split(SplitCharacter);

            if(i == 0)
            {
                MeshGameObjectName = splitedLine[0];
            }
            switch (splitedLine[0])
            {
                case VerticesPrefix:
                    UpdateVertices(splitedLine.Skip(1).ToArray());
                    break;

                case FacePrefix:
                    UpdateTriangles(splitedLine.Skip(1).ToArray());
                    break;
            }
        }

        return this;
    }

    private void UpdateVertices(string[] data)
    {
        Vertices.Add(new Vector3(
                float.Parse(data[0], CultureInfo.InvariantCulture),
                float.Parse(data[1], CultureInfo.InvariantCulture),
                float.Parse(data[2], CultureInfo.InvariantCulture)));
    }
    
    private void UpdateTriangles(string[] data)
    {// 삼각 순환법(Fan Triangulation)
     // 첫 번째 버텍스를 기준으로 삼각형을 형성

        // 첫 번째 버텍스 인덱스 저장
        int firstIndex = ConvertIndex(data[0], FaceType.Vertex);

        // n각형을 삼각형으로 분할
        for (int i = 1; i < data.Length - 1; i++)
        {
            Triangles.Add(firstIndex);
            Triangles.Add(ConvertIndex(data[i], FaceType.Vertex));
            Triangles.Add(ConvertIndex(data[i + 1], FaceType.Vertex));
        }
    }

    private int ConvertIndex(string data, FaceType type)
    {
        // Index가 0부터 시작하는게 아니라 1부터 시작하므로 수정해줘야 함
        string[] parts = data.Split(FaceSplitCharacter);
        return int.Parse(parts[(int)type]) - 1;
    }

    public void SetTriangleOffset(int offsetValue)
    {
        for (int i = 0; i < Triangles.Count; i++)
        {
            Triangles[i] -= offsetValue;
        }
    }
}