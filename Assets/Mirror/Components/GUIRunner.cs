using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mirror
{
public class GUIRunner : MonoBehaviour
{
    static List<IOnGUI> onGuiObjects = new List<IOnGUI>();
    static GUIRunner instance;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    void OnGUI()
    {
        for (int i = onGuiObjects.Count - 1; i >= 0; i--)
        {
            try
            {
                IOnGUI onGuiObject = onGuiObjects[i];
                if (onGuiObject.IsAlive())
                    Remove(onGuiObject);
                else if (onGuiObject.IsVisible())
                    onGuiObject.OnGUIAction();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    public static void Add(IOnGUI onGuiObject)
    {
        if (!onGuiObject.IsVisible())
            return;

        if (!onGuiObjects.Contains(onGuiObject))
        {
            onGuiObjects.Add(onGuiObject);

            if (instance == null)
            {
                new GameObject("GUIRunner").AddComponent<GUIRunner>();
            }
        }
    }

    public static void Remove(IOnGUI onGuiObject)
    {
        onGuiObjects.Remove(onGuiObject);
        if (onGuiObjects.Count == 0 && instance != null)
        {
            Destroy(instance.gameObject);
        }
    }
}

public interface IOnGUI
{
    void OnGUIAction();
    bool IsAlive();
    bool IsVisible();
}
}
