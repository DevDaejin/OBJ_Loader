using UnityEngine;
using UnityEngine.UI;

public class Manager : MonoBehaviour
{
    [SerializeField] private Button syncButton;
    [SerializeField] private Button asyncButton;
    [SerializeField] private Button concurrentAsyncButton;

    private Sync.AssetLoader assetLoaderSync;
    private Async.AssetLoader assetLoaderAsync;
    private Concurrent.AssetLoader assetLoaderConcurrent;

    private void Start()
    {
        assetLoaderSync ??= gameObject.AddComponent<Sync.AssetLoader>();
        assetLoaderAsync ??= gameObject.AddComponent<Async.AssetLoader>();
        assetLoaderConcurrent ??= gameObject.AddComponent<Concurrent.AssetLoader>();

        // 기능 바인딩
        syncButton.onClick.AddListener(LoadOBJSync);
        asyncButton.onClick.AddListener(LoadOBJAsync);
        concurrentAsyncButton.onClick.AddListener(LoadOBJConcurrentAsync);
    }

    void LoadOBJSync()
    {// 동기
        assetLoaderSync.GetAsset();
    }

    void LoadOBJAsync()
    {// 비동기
        assetLoaderAsync.GetAsset();
    }

    void LoadOBJConcurrentAsync()
    {// 동시 비동기
        assetLoaderConcurrent.GetAsset();
    }
}
