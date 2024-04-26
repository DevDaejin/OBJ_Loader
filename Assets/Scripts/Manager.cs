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
        //기능 바인딩
        syncButton.onClick.AddListener(LoadOBJSync);
        asyncButton.onClick.AddListener(LoadOBJAsync);
        concurrentAsyncButton.onClick.AddListener(LoadOBJConcurrentAsync);
    }

    void LoadOBJSync()
    {
        assetLoader.GetAsset(LoadType.Sync);
    }

    void LoadOBJAsync()
    {
        assetLoader.GetAsset(LoadType.Async);
    }

    void LoadOBJConcurrentAsync()
    {
        assetLoader.GetAsset(LoadType.ConcurrentAsync);
    }
}
