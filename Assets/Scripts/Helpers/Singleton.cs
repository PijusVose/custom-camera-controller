using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    public static bool IsAwakened { get; set; }
    public static bool IsStarted { get; set; }
    public static bool IsDestroyed { get; set; }

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                if (IsDestroyed) return null;
                
                instance = FindExistingInstance() ?? CreateInstance();
            }

            return instance;
        }
    }

    private static T FindExistingInstance()
    {
        var existingInstances = FindObjectsOfType<T>();

        if (existingInstances == null || existingInstances.Length == 0) return null;

        return existingInstances[0];
    }

    private static T CreateInstance()
    {
        var instanceContainer = new GameObject(typeof(T).Name + " (Singleton)");

        return instanceContainer.AddComponent<T>();
    }
    
    protected virtual void SingletonAwakened() {}
    protected virtual void SingletonStarted() {}
    protected virtual void SingletonDestroyed() {}

    protected virtual void NotifyInstanceRepeated()
    {
        Component.Destroy(this.GetComponent<T>());
    }

    private void Awake()
    {
        var thisInstance = this.GetComponent<T>();

        if (instance == null)
        {
            instance = thisInstance;
            
            // I don't really want for singletons to remain between scenes.
            // DontDestroyOnLoad(instance.gameObject);
        }
        else if (thisInstance != instance)
        {
            Debug.LogWarning($"Found a duplicated instance of a Singleton with type {this.GetType()} in the GameObject {this.gameObject.name}");

            NotifyInstanceRepeated();
            
            return;
        }

        if (!IsAwakened)
        {
            SingletonAwakened();

            IsAwakened = true;
        }
    }

    private void Start()
    {
        if (IsStarted) return;
        
        SingletonStarted();
        IsStarted = true;
    }

    private void OnDestroy()
    {
        if (this != instance) return;

        IsDestroyed = true;
        
        Debug.LogWarning($"Deleted Singleton with type {this.GetType()} of GameObject {this.gameObject.name}");
        SingletonDestroyed();
    }
}
