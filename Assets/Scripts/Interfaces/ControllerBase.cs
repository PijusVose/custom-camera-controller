using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ControllerBase : MonoBehaviour
{
    // TODO: add GameController variable, subscribe to Awake and Start methods.
    protected GameController gameController;
    protected bool isInitialized;
    
    public virtual void Init(GameController gameController)
    {
        this.gameController = gameController;

        gameController.OnAwake += AwakeController;
        gameController.OnStart += StartController;

        isInitialized = true;
    }

    private void OnDisable()
    {
        gameController.OnAwake -= AwakeController;
        gameController.OnStart -= StartController;
    }

    protected virtual void StartController()
    {
        
    }

    protected virtual void AwakeController()
    {
        
    }
}
