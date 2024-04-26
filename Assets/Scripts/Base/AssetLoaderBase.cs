using UnityEngine;

public abstract class AssetLoaderBase : MonoBehaviour
{
    [SerializeField] public LoaderModule LoaderModule { get; set; }

    private void Start()
    {
        if(!LoaderModule)
        {
            LoaderModule = 
                FindObjectOfType<LoaderModule>() ?? 
                gameObject.AddComponent<LoaderModule>();
        }
    }

    public abstract void GetAsset(LoadType type);
}
