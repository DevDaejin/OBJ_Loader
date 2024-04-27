using UnityEngine;
using UnityEngine.UI;

public class Manager : MonoBehaviour
{
    [SerializeField] private Button syncButton;
    [SerializeField] private Button asyncButton;
    [SerializeField] private Button concurrentAsyncButton;

    private AssetLoader assetLoader;

    private void Start()
    {
        if (!assetLoader) assetLoader = gameObject.AddComponent<AssetLoader>();
        // 기능 바인딩
        syncButton.onClick.AddListener(LoadOBJSync);
        asyncButton.onClick.AddListener(LoadOBJAsync);
        concurrentAsyncButton.onClick.AddListener(LoadOBJConcurrentAsync);
    }

    void LoadOBJSync()
    {// 동기
        assetLoader.GetAsset(LoadType.Sync);
    }

    void LoadOBJAsync()
    {// 비동기
        assetLoader.GetAsset(LoadType.Async);
    }

    void LoadOBJConcurrentAsync()
    {// 동시 비동기
        assetLoader.GetAsset(LoadType.ConcurrentAsync);
    }
}
