using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public static class Helper
{
    public static bool IsMouseOverUIElement(GameObject go)
    {
        if (!EventSystem.current.IsPointerOverGameObject()) return false;

        var eventDataCurrentPosition = new PointerEventData(EventSystem.current)
        {
            position = new Vector2(Input.mousePosition.x, Input.mousePosition.y)
        };
        
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

        return results.Count > 0 && results.Exists(x => x.gameObject == go);
    }
}
