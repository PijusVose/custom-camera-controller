using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class GameController : Singleton<GameController>
{
    // Private fields
    
    private List<ControllerBase> controllers;

    // Events
    
    public event Action OnAwake;
    public event Action OnStart; 

    // GameController
    
    // TODO: I am just basically inventing dependency injection from scratch. Need to call Start and Awake functions from GameController for each controller.
    // Also got tip from coworker to initialize all controller and start them from here in order.

    private void Awake()
    {
        InitControllers();
        
        OnAwake?.Invoke();
    }

    private void Start()
    {
        OnStart?.Invoke();
    }

    private void GetControllers()
    {
        controllers = FindObjectsOfType<ControllerBase>().ToList();
    }

    public T GetController<T>() where T : ControllerBase
    {
        return controllers.OfType<T>().FirstOrDefault();
    }

    private void InitControllers()
    {
        GetControllers();
        
        foreach (var plugin in controllers)         
        {
            plugin.Init(this);
        }
    }
}
