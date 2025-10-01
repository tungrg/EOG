using UnityEngine;
using System.Collections.Generic;

public class ObjectPool : MonoBehaviour
{
    private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, GameObject> prefabReferences = new Dictionary<string, GameObject>();

    public void InitializePool(GameObject prefab, int initialSize, Transform parent = null)
    {
        if (prefab == null)
        {
            DebugLogger.LogError("Attempted to initialize pool with null prefab.");
            return;
        }

        string poolKey = prefab.name;
        if (!poolDictionary.ContainsKey(poolKey))
        {
            poolDictionary[poolKey] = new Queue<GameObject>();
            prefabReferences[poolKey] = prefab;

            for (int i = 0; i < initialSize; i++)
            {
                GameObject obj = Instantiate(prefab, parent);
                obj.SetActive(false);
                poolDictionary[poolKey].Enqueue(obj);
            }
            //DebugLogger.Log($"[ObjectPool] Initialized pool for {poolKey} with {initialSize} instances.");
        }
    }

    public GameObject GetObject(string poolKey, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!poolDictionary.ContainsKey(poolKey))
        {
            if (prefabReferences.ContainsKey(poolKey))
            {
                GameObject newObj = Instantiate(prefabReferences[poolKey], position, rotation, parent);
                newObj.SetActive(true);
                DebugLogger.Log($"[ObjectPool] Created new object for {poolKey} (pool was empty).");
                return newObj;
            }
            DebugLogger.LogError($"[ObjectPool] No pool or prefab found for key: {poolKey}");
            return null;
        }

        if (poolDictionary[poolKey].Count == 0)
        {
            GameObject newObj = Instantiate(prefabReferences[poolKey], position, rotation, parent);
            newObj.SetActive(true);
            //DebugLogger.Log($"[ObjectPool] Created new object for {poolKey} (pool was empty).");
            return newObj;
        }

        GameObject obj = poolDictionary[poolKey].Dequeue();
        if (obj == null)
        {
            DebugLogger.LogWarning($"[ObjectPool] Dequeued null object from pool {poolKey}. Creating new object.");
            obj = Instantiate(prefabReferences[poolKey], position, rotation, parent);
            obj.SetActive(true);
            return obj;
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.transform.SetParent(parent);
        obj.SetActive(true);
        //DebugLogger.Log($"[ObjectPool] Retrieved object from pool {poolKey}. Remaining: {poolDictionary[poolKey].Count}");
        return obj;
    }

    public void ReturnObject(GameObject obj)
    {
        if (obj == null)
        {
            DebugLogger.LogWarning("[ObjectPool] Attempted to return null object to pool.");
            return;
        }

        if (!obj.activeSelf)
        {
            DebugLogger.LogWarning($"[ObjectPool] Attempted to return already inactive object: {obj.name}. Ignoring.");
            return;
        }

        string poolKey = obj.name.Replace("(Clone)", "");
        if (!poolDictionary.ContainsKey(poolKey))
        {
            DebugLogger.LogWarning($"[ObjectPool] No pool found for {poolKey}. Destroying object.");
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        poolDictionary[poolKey].Enqueue(obj);
        //DebugLogger.Log($"[ObjectPool] Returned object to pool {poolKey}. Pool size: {poolDictionary[poolKey].Count}");
    }

    public void ClearPool(string poolKey)
    {
        if (poolDictionary.ContainsKey(poolKey))
        {
            while (poolDictionary[poolKey].Count > 0)
            {
                GameObject obj = poolDictionary[poolKey].Dequeue();
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            poolDictionary.Remove(poolKey);
            prefabReferences.Remove(poolKey);
            DebugLogger.Log($"[ObjectPool] Cleared pool for {poolKey}.");
        }
    }
}   