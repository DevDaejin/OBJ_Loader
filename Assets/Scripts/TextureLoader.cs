using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class TextureLoader : MonoBehaviour
{
    public Texture2D GetTexture(string filePath)
    {
        // 파일 경로 검증
        if (!File.Exists(filePath))
        {
            Debug.LogError("File not found: " + filePath);
            return null;
        }

        // 파일로부터 바이트 데이터 읽기
        byte[] fileData = File.ReadAllBytes(filePath);
        Texture2D texture = new Texture2D(2, 2); // 임의의 크기로 초기화

        // 바이트 데이터로부터 텍스처 생성
        if (texture.LoadImage(fileData)) // LoadImage는 이미지 데이터를 자동으로 해석하여 텍스처 크기를 조정
        {
            return texture;
        }
        else
        {
            Debug.LogError("Failed to load texture from data: " + filePath);
            return null;
        }
    }

    public async Task<Texture> GetTextureAsync(string filePath)
    {
        // 파일 경로 검증
        if (!File.Exists(filePath))
        {
            Debug.LogError("File not found: " + filePath);
            return null;
        }

        byte[] fileData;

        using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
        {
            fileData = new byte[fileStream.Length];
            await fileStream.ReadAsync(fileData, 0, (int)fileStream.Length);
        }

        Texture2D texture = new Texture2D(2, 2); // 임의의 크기로 초기화

        // 바이트 데이터로부터 텍스처 생성
        if (texture.LoadImage(fileData)) // LoadImage는 이미지 데이터를 자동으로 해석하여 텍스처 크기를 조정
        {
            return texture;
        }
        else
        {
            Debug.LogError("Failed to load texture from data: " + filePath);
            return null;
        }
    }
}
