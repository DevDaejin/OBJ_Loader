using System.Collections.Concurrent;
using UnityEngine;

public class ObjFileGameObjectPool : MonoBehaviour
{
    public static ObjFileGameObjectPool Instance { get => instance; }
    private static ObjFileGameObjectPool instance = null;

    public int poolInitSize = 30;
    private GameObject meshGameObject;
    
    //동시 처리 시 오류 방지
    private ConcurrentQueue<GameObject> objectPool = new ConcurrentQueue<GameObject>();
    private ConcurrentDictionary<int, GameObject> activeObjectPool = new ConcurrentDictionary<int, GameObject>();

    private const string MeshGameObjectPath = "Prefabs/MeshGameObject";
    private const string defaultName = "None";

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        meshGameObject ??=
                Resources.Load<GameObject>(MeshGameObjectPath) ??
                throw new System.Exception("MeshGameObject is null");

        transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        // 풀 초기화
        for (int i = 0; i < poolInitSize; i++)
        {
            GameObject go = CreateNewOne();
            objectPool.Enqueue(go);
        }
    }

    // Dequeue
    public GameObject GetObject()
    {
        if(objectPool.Count > 0)
        {
            GameObject go;
            if (objectPool.TryDequeue(out go))
            {
                go.SetActive(true);
                activeObjectPool.TryAdd(go.GetInstanceID(), go);
                return go;
            }
        }

        GameObject additional = CreateNewOne();
        activeObjectPool.TryAdd(additional.GetInstanceID(), additional);
        return additional;
    }

    // Create
    private GameObject CreateNewOne()
    {
        GameObject o = Instantiate(meshGameObject, transform);
        InitObject(o);
        return o;
    }

    private void InitObject(GameObject go)
    {
        go.transform.SetParent(transform);
        go.name = defaultName;
        go.SetActive(false);
    }

    // Enqueue
    public void ReturnObject(GameObject go)
    {
        GameObject returnGo;
        if(activeObjectPool.TryRemove(go.GetInstanceID(), out returnGo))
        {
            InitObject(returnGo);
            objectPool.Enqueue(returnGo);
        }
    }


    public void ReturnObjectAll()
    {
        var activeObjects = activeObjectPool.ToArray();

        foreach (var activeObject in activeObjects)
        {
            ReturnObject(activeObject.Value);
        }
    }
}
