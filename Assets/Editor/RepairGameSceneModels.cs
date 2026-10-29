using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class RepairGameSceneModels
{
    private const string ScenePath = "Assets/Scenes/Game Scene.unity";
    private static bool finished;

    static RepairGameSceneModels()
    {
        EditorApplication.update += RepairActiveScene;
    }

    private static void RepairActiveScene()
    {
        if (finished || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            return;

        GameObject gatePrefab = LoadModel("Gate.fbx");
        GameObject spikePrefab = LoadModel("Paku.fbx");
        GameObject bouncerPrefab = LoadModel("Bouncer.fbx");
        GameObject brokenPlatformPrefab = LoadModel("PlatformRetak.fbx");

        if (gatePrefab == null || spikePrefab == null ||
            bouncerPrefab == null || brokenPlatformPrefab == null)
        {
            finished = true;
            EditorApplication.update -= RepairActiveScene;
            return;
        }

        bool sceneWasDirty = scene.isDirty;
        bool changed = false;
        changed |= EnsureModel("Door", "Model Gate", gatePrefab, true, out GameObject gate);
        changed |= EnsureModel("Spike", "Model Paku", spikePrefab, true, out _);
        changed |= EnsureModel("Bouncer", "Model Bouncer", bouncerPrefab, true, out _);
        changed |= EnsureModel("PlatformRetak", "Model Pecahan", brokenPlatformPrefab, false, out GameObject brokenPlatform);

        changed |= AssignReference<DoorController>("Door", "modelPintu", gate.transform);
        changed |= AssignReference<PlatformRetak>("PlatformRetak", "modelPecahan", brokenPlatform);

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!sceneWasDirty && !EditorSceneManager.SaveScene(scene))
                Debug.LogError($"Gagal menyimpan perubahan model ke scene '{ScenePath}'.");
            else if (sceneWasDirty)
                Debug.LogWarning(
                    $"Model sudah dipasang di '{ScenePath}', tetapi scene sebelumnya memiliki perubahan belum disimpan. Tekan Ctrl+S untuk menyimpan."
                );
            else
                Debug.Log("Model Gate, Paku, Bouncer, dan PlatformRetak sudah dipasang ke Game Scene.");
        }

        finished = true;
        EditorApplication.update -= RepairActiveScene;
    }

    private static GameObject LoadModel(string fileName)
    {
        string path = $"Assets/Scenes/Aset Game Scene/Model/{fileName}";
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (model == null)
            Debug.LogError($"Model FBX tidak bisa dimuat dari '{path}'.");
        return model;
    }

    private static bool EnsureModel(
        string objectName,
        string childName,
        GameObject prefab,
        bool hideParentRenderers,
        out GameObject instance)
    {
        GameObject parent = GameObject.Find(objectName);
        if (parent == null)
        {
            Debug.LogError($"GameObject '{objectName}' tidak ditemukan di Game Scene.");
            instance = null;
            return false;
        }

        bool changed = false;
        Transform child = parent.transform.Find(childName);
        if (child == null)
        {
            instance = PrefabUtility.InstantiatePrefab(prefab, parent.transform) as GameObject;
            if (instance == null)
            {
                Debug.LogError($"Gagal membuat instance model '{prefab.name}' di '{objectName}'.");
                return false;
            }

            Undo.RegisterCreatedObjectUndo(instance, $"Add {childName}");
            instance.name = childName;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            changed = true;
        }
        else
        {
            instance = child.gameObject;
        }

        if (hideParentRenderers)
        {
            foreach (Renderer renderer in parent.GetComponents<Renderer>())
            {
                if (!renderer.enabled)
                    continue;

                Undo.RecordObject(renderer, "Hide placeholder model");
                renderer.enabled = false;
                changed = true;
            }
        }

        if (objectName == "PlatformRetak" && instance.activeSelf)
        {
            Undo.RecordObject(instance, "Hide broken platform pieces");
            instance.SetActive(false);
            changed = true;
        }

        return changed;
    }

    private static bool AssignReference<T>(string objectName, string propertyName, Object value)
        where T : Component
    {
        GameObject target = GameObject.Find(objectName);
        T component = target != null ? target.GetComponent<T>() : null;
        if (component == null)
        {
            Debug.LogError($"Component '{typeof(T).Name}' tidak ditemukan di '{objectName}'.");
            return false;
        }

        SerializedObject serialized = new SerializedObject(component);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"Field '{propertyName}' tidak ditemukan pada '{typeof(T).Name}'.");
            return false;
        }

        if (property.objectReferenceValue == value)
            return false;

        property.objectReferenceValue = value;
        serialized.ApplyModifiedProperties();
        return true;
    }
}
