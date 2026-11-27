using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public static class PauseMenuSceneBootstrap
{
    private const string PrefabResourcePath = "PauseMenuCanvas";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHandler()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.name.StartsWith("Stage", StringComparison.OrdinalIgnoreCase))
            return;

        MenuPauseSederhana[] menus = UnityEngine.Object.FindObjectsByType<MenuPauseSederhana>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (MenuPauseSederhana menu in menus)
        {
            if (menu != null && menu.IsReady)
            {
                EnsureEventSystem();
                return;
            }
        }

        Canvas canvas = FindPauseCanvas();
        if (canvas == null)
        {
            GameObject prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogError(
                    $"Pause Canvas prefab tidak ditemukan di Resources/{PrefabResourcePath}.prefab.");
                return;
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            canvas = instance.GetComponent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Pause Canvas prefab harus memiliki komponen Canvas di root.");
                UnityEngine.Object.Destroy(instance);
                return;
            }
        }

        foreach (MenuPauseSederhana menu in menus)
        {
            if (menu != null)
                UnityEngine.Object.Destroy(menu);
        }

        EnsureEventSystem();
        if (canvas.GetComponent<MenuPauseSederhana>() == null)
            canvas.gameObject.AddComponent<MenuPauseSederhana>();
    }

    private static Canvas FindPauseCanvas()
    {
        Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Canvas canvas in canvases)
        {
            if (canvas.transform.Find("P_Pause") != null)
                return canvas;
        }

        return null;
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>(
            FindObjectsInactive.Include);
        if (eventSystem == null)
            eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();

        if (!eventSystem.gameObject.activeSelf)
            eventSystem.gameObject.SetActive(true);

        InputSystemUIInputModule inputModule =
            eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null)
        {
            inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }
    }
}
