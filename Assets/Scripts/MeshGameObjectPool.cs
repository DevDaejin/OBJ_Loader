using System.Collections.Concurrent;
using UnityEngine;

public class MeshGameObjectPool : MonoBehaviour
{
    public int poolInitSize = 30;

    private GameObject poolRoot;
    private GameObject meshGameObject;
    //동시 처리 시 오류 방지
    private ConcurrentQueue<GameObject> objectPool = new ConcurrentQueue<GameObject>();

    private const string MeshGameObjectPath = "Prefabs/MeshGameObject";
    private const string PoolRootName = "PoolRoot";

    private void Start()
    {
        if(meshGameObject == null)
        {
            meshGameObject = 
                Resources.Load<GameObject>(MeshGameObjectPath) ?? 
                throw new System.Exception("MeshGameObject is null");
        }

        poolRoot = new GameObject(PoolRootName);
        poolRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        for (int i = 0; i < poolInitSize; i++)
        {
            GameObject go = Instantiate(meshGameObject, poolRoot.transform);
            go.SetActive(false);
            objectPool.Enqueue(go);
        }
    }

    public GameObject GetObject()
    {
        if(objectPool.Count > 0)
        {
            GameObject go;
            objectPool.TryDequeue(out go);
            go.SetActive(true);
            return go;
        }
        else
        {
            GameObject additional = Instantiate(meshGameObject, poolRoot.transform);
            return additional;
        }
    }

    public void ReturnObject(GameObject go)
    {
        go.transform.SetParent(poolRoot.transform);
        go.SetActive(false);
        objectPool.Enqueue(go);
    }
}
