using System.Collections.Generic;
using System.IO;
using Photon.Pun;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

 [InitializeOnLoad]
public static class CaterpillarSetup
{
    private const string JalurPemain = "Assets/Resources/Player.prefab";
    private const string JalurDragon = "Assets/Resources/Dragon.prefab";
    private const string JalurLimau = "Assets/Resources/Limau.prefab";
    private const string JalurSceneTes = "Assets/Scenes/DragonTest.unity";
    private const string JalurSceneStage = "Assets/Scenes/DragonTest_Stage.unity";
    private const string JalurKey = "Assets/Resources/Key.prefab";
    private const string FolderMaterial = "Assets/Materials";

    static CaterpillarSetup()
    {
        EditorApplication.delayCall += BersihkanTesDanBangunStageSekali;
        EditorApplication.delayCall += PasangSkillPrefabSekali;
        EditorApplication.delayCall += PasangGameplayBaruSekali;
    }

    private static void PasangGameplayBaruSekali()
    {
        const string kunciSelesai = "Caterpillar.GameplayBaru.v1";
        if (EditorPrefs.GetBool(kunciSelesai, false))
            return;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        SetupKarakter();
        PasangGameplayDiSceneAktif();
        EditorPrefs.SetBool(kunciSelesai, true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Gameplay baru dipasang: Juice/HUD Dragon dan hazard di scene aktif.");
    }

    private static void PasangSkillPrefabSekali()
    {
        const string kunciSelesai = "Caterpillar.LimauSkills.v2";
        if (EditorPrefs.GetBool(kunciSelesai, false))
            return;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        SetupKarakter();
        EditorPrefs.SetBool(kunciSelesai, true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void BersihkanTesDanBangunStageSekali()
    {
        const string kunciSelesai = "Caterpillar.VerticalStage.v1";
        if (EditorPrefs.GetBool(kunciSelesai, false))
            return;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        string[] sceneDummy = {
            "Assets/Scenes/DragonTest.unity",
            "Assets/Scenes/DragonTest_Level1.unity",
            "Assets/Scenes/DragonTest_Level2.unity",
            "Assets/Scenes/DragonTest_Level3.unity",
            "Assets/Scenes/DragonTest_Stage.unity"
        };

        foreach (string jalurScene in sceneDummy)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(jalurScene) != null)
                AssetDatabase.DeleteAsset(jalurScene);
        }

        BersihkanSceneDummyBuildSettings();
        SetupKarakter();
        BuatStageTes();
        PasangDebugFeedback();

        EditorPrefs.SetBool(kunciSelesai, true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Cleanup selesai: satu stage vertical tersisa, scene dummy dan menu Caterpillar Tools dihapus.");
    }

    public static void BuildWindowsPlayer()
    {
        const string outputDirectory = "Build";
        const string outputPath = "Build/Adventurein.exe";

        if (!Directory.Exists(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        List<string> scenePaths = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
                scenePaths.Add(scene.path);
        }

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenePaths.ToArray(),
            locationPathName = outputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new System.Exception(
                $"Windows build gagal: {report.summary.result} - {report.summary.totalErrors} error.");
        }

        Debug.Log($"Windows build berhasil: {outputPath}");
    }

    public static void SetupKarakter()
    {
        List<string> ringkasan = new List<string>();
        if (!PastikanFolder("Assets/Resources") || !PastikanFolder(FolderMaterial))
        {
            Debug.LogError("Folder yang diperlukan untuk setup karakter tidak dapat disiapkan.");
            return;
        }

        if (!PastikanTag())
        {
            return;
        }

        GameObject pemainAsal = AssetDatabase.LoadAssetAtPath<GameObject>(JalurPemain);
        if (pemainAsal == null)
        {
            Debug.LogError($"Prefab dasar tidak ditemukan: {JalurPemain}");
            return;
        }

        Material materialDragon = PastikanMaterial(
            "Dragon_Placeholder",
            new Color(0.85f, 0.08f, 0.06f, 1f),
            ringkasan);
        Material materialLimau = PastikanMaterial(
            "Limau_Placeholder",
            new Color(0.65f, 0.85f, 0.1f, 1f),
            ringkasan);
        if (materialDragon == null || materialLimau == null)
        {
            Debug.LogError("Material placeholder tidak dapat dibuat atau dimuat.");
            return;
        }

        PastikanVarian(pemainAsal, JalurDragon, materialDragon, true, ringkasan);
        PastikanVarian(pemainAsal, JalurLimau, materialLimau, true, ringkasan);
        Debug.Log("Setup karakter selesai:\n- " + string.Join("\n- ", ringkasan));
    }

    public static void BuatSceneTes()
    {
        for (int level = 1; level <= 3; level++)
            BuatSceneTesLevel(level);

        TambahkanKetigaSceneTesKeBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void BuatStageTes()
    {
        if (!PastikanTag())
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cahaya = new GameObject("Directional Light");
        Light lampu = cahaya.AddComponent<Light>();
        lampu.type = LightType.Directional;
        lampu.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        BuatKubus("Lantai", new Vector3(0f, -0.5f, 0f), new Vector3(16f, 1f, 4f));
        BuatKubus("Platform Level 1", new Vector3(0f, 2f, 0f), new Vector3(10f, 0.5f, 3f));
        BuatKubus("Platform Level 2", new Vector3(0f, 4f, 0f), new Vector3(10f, 0.5f, 3f));
        BuatKubus("Platform Level 3", new Vector3(0f, 6f, 0f), new Vector3(10f, 0.5f, 3f));

        Transform spawn1 = BuatTitikSpawn("Spawn_1", new Vector3(-1f, 0.1f, 0f));
        Transform spawn2 = BuatTitikSpawn("Spawn_2", new Vector3(1f, 0.1f, 0f));

        GameObject spawner = new GameObject("Spawner");
        SpawnPemain komponenSpawner = spawner.AddComponent<SpawnPemain>();
        komponenSpawner.titikSpawn = new[] { spawn1, spawn2 };
        komponenSpawner.namaPrefab = "Limau";
        spawner.SetActive(false);

        GameObject tesBootstrap = new GameObject("TesBootstrap");
        TesOfflineBootstrap bootstrap = tesBootstrap.AddComponent<TesOfflineBootstrap>();
        SerializedObject propertiBootstrap = new SerializedObject(bootstrap);
        propertiBootstrap.FindProperty("karakterTes").stringValue = "Limau";
        propertiBootstrap.FindProperty("spawner").objectReferenceValue = spawner;
        propertiBootstrap.ApplyModifiedPropertiesWithoutUndo();
        tesBootstrap.AddComponent<MenuPauseSederhana>();
        tesBootstrap.AddComponent<GameAudio>();

        GameObject cameraObjek = new GameObject("Main Camera");
        cameraObjek.tag = "MainCamera";
        cameraObjek.transform.position = new Vector3(0f, 3f, -12f);
        Camera kamera = cameraObjek.AddComponent<Camera>();
        kamera.orthographic = false;
        cameraObjek.AddComponent<AudioListener>();
        cameraObjek.AddComponent<CameraIkut>();

        GameObject bioma = new GameObject("BiomeProgression");
        PhotonView photonBioma = bioma.AddComponent<PhotonView>();
        BiomeProgression progresi = bioma.AddComponent<BiomeProgression>();
        SerializedObject propertiBioma = new SerializedObject(progresi);
        propertiBioma.FindProperty("levelAwal").intValue = 1;
        propertiBioma.ApplyModifiedPropertiesWithoutUndo();
        photonBioma.sceneViewId = 1;

        BuatKotakDorongDi(new Vector3(-2f, 2.8f, 0f));
        BuatBatuDi(new Vector3(2f, 4.8f, 0f));
        BuatAnginBerkalaDi(new Vector3(0f, 3f, 0f));
        BuatKumbangDi(new Vector3(-3f, 2.8f, 0f));
        BuatBurungPelatukDi(new Vector3(0f, 7f, 0f));
        BuatObjektifKey();
        BuatGerbangLevel(new Vector3(0f, 6.8f, 0f), "");

        EditorSceneManager.SaveScene(scene, JalurSceneStage);
        TetapkanPhotonViewSceneId();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        TambahkanSceneKeBuildSettings(JalurSceneStage);
        Debug.Log($"Stage tes gabungan dibuat: {JalurSceneStage}");
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(JalurSceneStage));
    }

    private static void BuatObjektifKey()
    {
        GameObject prefabKey = AssetDatabase.LoadAssetAtPath<GameObject>(JalurKey);
        if (prefabKey == null)
        {
            Debug.LogError($"Prefab kunci tidak ditemukan: {JalurKey}");
            return;
        }

        GameObject key = PrefabUtility.InstantiatePrefab(prefabKey) as GameObject;
        if (key == null)
            return;

        key.name = "Objektif Key";
        key.transform.position = new Vector3(0f, 4.6f, 0f);

        BoxCollider box = key.GetComponent<BoxCollider>();
        if (box == null)
            box = key.AddComponent<BoxCollider>();
        box.isTrigger = true;

        SphereCollider areaAmbil = key.GetComponent<SphereCollider>();
        if (areaAmbil == null)
            areaAmbil = key.AddComponent<SphereCollider>();
        areaAmbil.isTrigger = true;
        areaAmbil.radius = 0.04f;
    }

    private static void PerbaikiObjektifKeyStage()
    {
        GameObject key = GameObject.Find("Objektif Key");
        if (key == null)
        {
            BuatObjektifKey();
            Debug.Log("Objektif Key dibuat ulang pada stage.");
            return;
        }

        key.transform.position = new Vector3(0f, 4.6f, 0f);
        BoxCollider box = key.GetComponent<BoxCollider>();
        if (box == null)
            box = key.AddComponent<BoxCollider>();
        box.isTrigger = true;

        SphereCollider areaAmbil = key.GetComponent<SphereCollider>();
        if (areaAmbil == null)
            areaAmbil = key.AddComponent<SphereCollider>();
        areaAmbil.isTrigger = true;
        areaAmbil.radius = 0.04f;

        if (key.GetComponent<KeyPickup>() == null)
            key.AddComponent<KeyPickup>();

        PhotonView view = key.GetComponent<PhotonView>();
        if (view == null)
            view = key.AddComponent<PhotonView>();

        Debug.Log("Objektif Key diperbaiki: dekat kunci dan tekan F.");
    }

    private static void BuatGerbangLevel(Vector3 posisi, string namaSceneLevel)
    {
        GameObject gerbang = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gerbang.name = "Gerbang Bioma";
        gerbang.transform.position = posisi;
        gerbang.transform.localScale = new Vector3(1f, 4f, 3f);

        Renderer rendererGerbang = gerbang.GetComponent<Renderer>();
        rendererGerbang.enabled = false;
        BoxCollider triggerGerbang = gerbang.GetComponent<BoxCollider>();
        triggerGerbang.isTrigger = true;

        GameObject model = GameObject.CreatePrimitive(PrimitiveType.Cube);
        model.name = "Model Gerbang";
        model.transform.SetParent(gerbang.transform);
        model.transform.localPosition = Vector3.zero;
        model.transform.localScale = Vector3.one;
        model.GetComponent<Renderer>().enabled = true;

        DoorController door = gerbang.AddComponent<DoorController>();
        door.enabled = true;

        SerializedObject properti = new SerializedObject(door);
        properti.FindProperty("modelPintu").objectReferenceValue = model.transform;
        properti.FindProperty("tinggiBuka").floatValue = 5f;
        properti.FindProperty("kecepatanBuka").floatValue = 2f;
        properti.FindProperty("levelSesudahBuka").intValue = 2;
        properti.FindProperty("namaSceneSesudahBuka").stringValue = namaSceneLevel;
        properti.FindProperty("jedaPindahScene").floatValue = 1f;
        properti.ApplyModifiedPropertiesWithoutUndo();

        gerbang.AddComponent<PhotonView>();
    }

    public static void SetupSemua()
    {
        SetupKarakter();
        BuatSceneTes();
        BuatStageTes();
        PasangDebugFeedback();
    }

    public static void ImplementasiGameplayBaruBatch()
    {
        SetupKarakter();
        BuatStageTes();
        PasangGameplayDiSceneAktif();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void PasangGameplayDiSceneAktif()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid())
            return;

        if (Object.FindObjectOfType<AnginBerkala>() == null)
        {
            BuatAnginBerkalaDi(new Vector3(0f, 3f, 0f));
        }

        if (Object.FindObjectOfType<KumbangContoh>() == null)
        {
            BuatKumbangDi(new Vector3(-3f, 2.8f, 0f));
        }

        if (Object.FindObjectOfType<BurungPelatuk>() == null)
        {
            BuatBurungPelatukDi(new Vector3(0f, 7f, 0f));
        }

        TetapkanPhotonViewSceneId();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static void PasangDebugFeedback()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(JalurDragon);
        if (prefab == null)
        {
            Debug.LogError("Prefab Dragon belum ada. Jalankan Tools/Caterpillar/1. Setup Karakter dulu.");
            return;
        }

        GameObject isi = PrefabUtility.LoadPrefabContents(JalurDragon);
        try
        {
            DragonSkillDebugFeedback feedback = isi.GetComponent<DragonSkillDebugFeedback>();
            if (feedback == null)
            {
                isi.AddComponent<DragonSkillDebugFeedback>();
                PrefabUtility.SaveAsPrefabAsset(isi, JalurDragon);
                Debug.Log("DragonSkillDebugFeedback ditambahkan ke Dragon.prefab.");
            }
            else
            {
                Debug.Log("DragonSkillDebugFeedback sudah ada di Dragon.prefab (dilewati).");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(isi);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void HapusDebugFeedback()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(JalurDragon);
        if (prefab == null)
        {
            Debug.LogError("Prefab Dragon belum ada.");
            return;
        }

        GameObject isi = PrefabUtility.LoadPrefabContents(JalurDragon);
        try
        {
            DragonSkillDebugFeedback[] feedback = isi.GetComponents<DragonSkillDebugFeedback>();
            if (feedback.Length == 0)
            {
                Debug.Log("DragonSkillDebugFeedback tidak ada di Dragon.prefab (dilewati).");
                return;
            }

            for (int i = feedback.Length - 1; i >= 0; i--)
                Object.DestroyImmediate(feedback[i]);

            PrefabUtility.SaveAsPrefabAsset(isi, JalurDragon);
            Debug.Log("DragonSkillDebugFeedback dihapus dari Dragon.prefab.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(isi);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void BuatSceneTesLevel(int level)
    {
        string jalurScene = $"Assets/Scenes/DragonTest_Level{level}.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(jalurScene) != null)
        {
            Debug.Log($"Scene tes level {level} sudah ada (dilewati): {jalurScene}");
            return;
        }

        if (!PastikanTag())
            return;

        if (AssetDatabase.LoadAssetAtPath<GameObject>(JalurDragon) == null)
            Debug.LogWarning("Prefab Dragon belum ada. Jalankan Setup Karakter sebelum memulai scene tes.");

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cahaya = new GameObject("Directional Light");
        Light lampu = cahaya.AddComponent<Light>();
        lampu.type = LightType.Directional;
        lampu.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        GameObject lantai = BuatKubus("Lantai", new Vector3(0f, -0.5f, 0f), new Vector3(40f, 1f, 4f));
        lantai.tag = "Ground";
        BuatKubus("Platform 1", new Vector3(-2f, 1f, 0f), new Vector3(4f, 0.5f, 3f));
        BuatKubus("Platform 2", new Vector3(6f, 1.5f, 0f), new Vector3(4f, 0.5f, 3f));

        Transform spawn1 = BuatTitikSpawn("Spawn_1", new Vector3(-16f, 0.1f, 0f));
        Transform spawn2 = BuatTitikSpawn("Spawn_2", new Vector3(-14f, 0.1f, 0f));

        GameObject spawner = new GameObject("Spawner");
        SpawnPemain komponenSpawner = spawner.AddComponent<SpawnPemain>();
        komponenSpawner.titikSpawn = new[] { spawn1, spawn2 };
        komponenSpawner.namaPrefab = "Limau";
        spawner.SetActive(false);

        GameObject tesBootstrap = new GameObject("TesBootstrap");
        TesOfflineBootstrap bootstrap = tesBootstrap.AddComponent<TesOfflineBootstrap>();
        SerializedObject propertiBootstrap = new SerializedObject(bootstrap);
        propertiBootstrap.FindProperty("karakterTes").stringValue = "Limau";
        propertiBootstrap.FindProperty("spawner").objectReferenceValue = spawner;
        propertiBootstrap.ApplyModifiedPropertiesWithoutUndo();

        GameObject cameraObjek = new GameObject("Main Camera");
        cameraObjek.tag = "MainCamera";
        cameraObjek.transform.position = new Vector3(0f, 2f, -12f);
        Camera kamera = cameraObjek.AddComponent<Camera>();
        kamera.orthographic = false;
        cameraObjek.AddComponent<AudioListener>();
        cameraObjek.AddComponent<CameraIkut>();

        GameObject bioma = new GameObject("BiomeProgression");
        PhotonView photonBioma = bioma.AddComponent<PhotonView>();
        BiomeProgression progresi = bioma.AddComponent<BiomeProgression>();
        SerializedObject propertiBioma = new SerializedObject(progresi);
        propertiBioma.FindProperty("levelAwal").intValue = level;
        propertiBioma.ApplyModifiedPropertiesWithoutUndo();
        photonBioma.sceneViewId = 1;

        if (level == 1)
            BuatKotakDorong();
        else if (level == 2)
            BuatBatu(level);
        else
            BuatZonaAngin();

        EditorSceneManager.SaveScene(scene, jalurScene);
        int idView = 1;
        foreach (PhotonView view in Object.FindObjectsByType<PhotonView>(FindObjectsSortMode.None))
        {
            if (view.sceneViewId == 0)
            {
                while (System.Array.Exists(
                           Object.FindObjectsByType<PhotonView>(FindObjectsSortMode.None),
                           viewLain => viewLain != view && viewLain.sceneViewId == idView))
                {
                    idView++;
                }

                view.sceneViewId = idView++;
                EditorUtility.SetDirty(view);
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        foreach (PhotonView view in Object.FindObjectsByType<PhotonView>(FindObjectsSortMode.None))
        {
            if (view.sceneViewId == 0)
                Debug.LogError($"PhotonView '{view.name}' masih memiliki sceneViewId 0.", view);
        }

        Debug.Log($"Scene tes level {level} dibuat: {jalurScene}");
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(jalurScene));
    }

    private static void BuatKotakDorong()
    {
        GameObject kotak = BuatKubus("KotakDorong", new Vector3(-7f, 0.5f, 0f), Vector3.one);
        SiapkanKotakDorong(kotak);
    }

    private static void BuatKotakDorongDi(Vector3 posisi)
    {
        GameObject kotak = BuatKubus("KotakDorong", posisi, Vector3.one);
        SiapkanKotakDorong(kotak);
    }

    private static void SiapkanKotakDorong(GameObject kotak)
    {
        kotak.tag = "Pushable";
        Rigidbody badan = kotak.AddComponent<Rigidbody>();
        badan.mass = 20f;
        badan.linearDamping = 0.5f;
        badan.constraints = RigidbodyConstraints.FreezePositionZ;

        PhotonView view = kotak.AddComponent<PhotonView>();
        view.OwnershipTransfer = OwnershipOption.Takeover;
        view.Synchronization = ViewSynchronization.UnreliableOnChange;

        PhotonRigidbodyView viewRigidbody = kotak.AddComponent<PhotonRigidbodyView>();
        viewRigidbody.m_SynchronizeVelocity = true;
        viewRigidbody.m_SynchronizeAngularVelocity = true;
        view.ObservedComponents = new List<Component> { viewRigidbody };
        kotak.AddComponent<PushableObject>();
    }

    private static void BuatBatu(int level)
    {
        GameObject batu = BuatKubus("Batu", new Vector3(7f, 0.5f, 0f), Vector3.one);
        SiapkanBatu(batu, level);
    }

    private static void BuatBatuDi(Vector3 posisi)
    {
        GameObject batu = BuatKubus("Batu", posisi, Vector3.one);
        SiapkanBatu(batu, 2);
    }

    private static void BuatAnginBerkalaDi(Vector3 posisi)
    {
        GameObject zona = BuatKubus(
            "Angin Berkala",
            posisi,
            new Vector3(8f, 3f, 3f));
        zona.GetComponent<MeshRenderer>().enabled = false;
        zona.GetComponent<BoxCollider>().isTrigger = true;
        zona.AddComponent<AnginBerkala>();
    }

    private static void BuatKumbangDi(Vector3 posisi)
    {
        GameObject kumbang = BuatKubus(
            "Kumbang Contoh",
            posisi,
            new Vector3(0.8f, 0.8f, 0.8f));
        Rigidbody badan = kumbang.AddComponent<Rigidbody>();
        badan.useGravity = false;
        badan.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        BoxCollider collider = kumbang.GetComponent<BoxCollider>();
        collider.isTrigger = true;
        kumbang.AddComponent<PhotonView>();
        KumbangContoh komponen = kumbang.AddComponent<KumbangContoh>();
        SerializedObject properti = new SerializedObject(komponen);
        properti.FindProperty("jangkauanPatroli").floatValue = 3f;
        properti.FindProperty("kecepatan").floatValue = 1.5f;
        properti.FindProperty("kecepatanKabur").floatValue = 3.5f;
        properti.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuatBurungPelatukDi(Vector3 posisi)
    {
        GameObject burung = new GameObject("Burung Pelatuk");
        burung.transform.position = posisi;
        SphereCollider collider = burung.AddComponent<SphereCollider>();
        collider.radius = 0.25f;
        collider.isTrigger = true;
        burung.AddComponent<PhotonView>();
        BurungPelatuk komponen = burung.AddComponent<BurungPelatuk>();

        SerializedObject properti = new SerializedObject(komponen);
        properti.FindProperty("lebarJalur").floatValue = 20f;
        properti.FindProperty("radiusSerang").floatValue = 0.45f;
        properti.FindProperty("waktuPeringatan").floatValue = 1.6f;
        properti.FindProperty("waktuDash").floatValue = 0.45f;
        properti.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SiapkanBatu(GameObject batu, int level)
    {
        batu.tag = "Breakable";
        PhotonView view = batu.AddComponent<PhotonView>();
        batu.AddComponent<BreakableObject>();
        view.sceneViewId = level == 2 ? 6 : 0;
    }

    private static void BuatZonaAngin()
    {
        GameObject zona = BuatKubus("ZonaAngin", new Vector3(2f, 1.5f, 0f), new Vector3(5f, 3f, 3f));
        zona.GetComponent<MeshRenderer>().enabled = false;
        zona.GetComponent<BoxCollider>().isTrigger = true;
        HazardAnginTes hazard = zona.AddComponent<HazardAnginTes>();
        SerializedObject properti = new SerializedObject(hazard);
        properti.FindProperty("gayaAngin").floatValue = 8f;
        properti.FindProperty("arahAngin").vector3Value = Vector3.right;
        properti.FindProperty("patukanAktif").boolValue = true;
        properti.FindProperty("jedaPatukan").floatValue = 2f;
        properti.FindProperty("kekuatanPatukan").floatValue = 12f;
        properti.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuatSceneTesDragon()
    {
        bool sceneSudahAda = File.Exists(Path.Combine(
            Directory.GetCurrentDirectory(),
            JalurSceneTes.Replace('/', Path.DirectorySeparatorChar)));
        if (sceneSudahAda &&
            !EditorUtility.DisplayDialog(
                "Scene Tes Dragon sudah ada",
                "Apakah scene DragonTest yang ada ingin diganti?",
                "Ganti",
                "Batal"))
        {
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        if (!PastikanTag())
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(JalurPemain) == null)
        {
            Debug.LogError($"Prefab dasar tidak ditemukan: {JalurPemain}");
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(JalurDragon) == null)
        {
            Debug.LogError($"Prefab Dragon belum ada. Jalankan Tools/Caterpillar/1. Setup Karakter terlebih dahulu.");
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuatLingkungan();
        GameObject spawner = BuatSpawner();
        BuatBootstrap(spawner);
        BuatKamera();
        BuatPerkembanganBioma();
        BuatZonaBioma();
        BuatObjekSkill();
        BuatHazard();

        if (!EditorSceneManager.SaveScene(scene, JalurSceneTes))
        {
            Debug.LogError($"Scene tes tidak berhasil disimpan: {JalurSceneTes}");
            return;
        }

        TambahkanKeBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log($"Scene tes Dragon dibuat: {JalurSceneTes}. Spawner dibuat nonaktif sampai room offline siap.");
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(JalurSceneTes));
    }

    private static void BukaDanJalankanSceneTes()
    {
        SceneAsset sceneTes = AssetDatabase.LoadAssetAtPath<SceneAsset>(JalurSceneTes);
        if (sceneTes == null)
        {
            EditorUtility.DisplayDialog(
                "Scene Tes Belum Ada",
                "Jalankan Tools/Caterpillar/2. Buat Scene Tes Dragon terlebih dahulu.",
                "OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.OpenScene(JalurSceneTes);

        if (!EditorApplication.isPlaying)
            EditorApplication.isPlaying = true;
    }

    public static void JalankanSetupKarakterBatch()
    {
        SetupKarakter();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void JalankanSetupSceneTesBatch()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(JalurDragon) == null)
        {
            SetupKarakter();
        }

        if (File.Exists(Path.Combine(
            Directory.GetCurrentDirectory(),
            JalurSceneTes.Replace('/', Path.DirectorySeparatorChar))))
        {
            Debug.Log("DragonTest sudah ada; scene tidak ditimpa oleh batch setup.");
            return;
        }

        BuatSceneTesDragon();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void PastikanVarian(
        GameObject pemainAsal,
        string jalur,
        Material material,
        bool tambahSkill,
        List<string> ringkasan)
    {
        bool sudahAda = File.Exists(Path.Combine(
            Directory.GetCurrentDirectory(),
            jalur.Replace('/', Path.DirectorySeparatorChar)));
        GameObject objek = null;
        bool instancePrefab = false;

        try
        {
            if (sudahAda)
            {
                objek = PrefabUtility.LoadPrefabContents(jalur);
                ringkasan.Add($"Varian sudah ada (dilewati agar tidak ditimpa): {jalur}");
            }
            else
            {
                objek = PrefabUtility.InstantiatePrefab(pemainAsal) as GameObject;
                instancePrefab = true;
                if (objek == null)
                {
                    Debug.LogError($"Gagal membuat instance prefab dari {JalurPemain}.");
                    return;
                }

                ringkasan.Add($"Varian dibuat: {jalur}");
            }

            if (jalur != JalurLimau)
                TerapkanMaterial(objek, material);
            if (tambahSkill)
            {
                if (jalur == JalurDragon)
                {
                    TambahkanSkillDragon(objek, ringkasan);
                    PastikanKeyStateDragon(objek, ringkasan);
                }
                else if (jalur == JalurLimau)
                {
                    TambahkanSkillLimau(objek, ringkasan);
                }
            }
            else
            {
                ringkasan.Add("Skill tidak ditambahkan.");
            }

            PhotonView[] photonViews = objek.GetComponentsInChildren<PhotonView>(true);
            if (photonViews.Length != 1 || photonViews[0].gameObject != objek)
            {
                Debug.LogError(
                    $"Prefab {jalur} harus memiliki tepat satu PhotonView pada root; ditemukan {photonViews.Length}.",
                    objek);
            }
            else
            {
                ringkasan.Add($"{jalur}: satu PhotonView root dipertahankan.");
            }

            PrefabUtility.SaveAsPrefabAsset(objek, jalur);
            EditorUtility.SetDirty(objek);
        }
        finally
        {
            if (instancePrefab && objek != null)
            {
                Object.DestroyImmediate(objek);
            }
            else if (!instancePrefab && objek != null)
            {
                PrefabUtility.UnloadPrefabContents(objek);
            }
        }
    }

    private static void TambahkanSkillDragon(GameObject objek, List<string> ringkasan)
    {
        PastikanKomponen<DragonSkillInput>(objek, ringkasan);
        PastikanKomponen<DragonPhysicsHandler>(objek, ringkasan);
        PastikanKomponen<DragonSkillNetwork>(objek, ringkasan);
        PastikanKomponen<DragonSkillController>(objek, ringkasan);
        PastikanKomponen<DragonPushSkill>(objek, ringkasan);
        PastikanKomponen<DragonDestroySkill>(objek, ringkasan);
        PastikanKomponen<DragonSteelSkinSkill>(objek, ringkasan);
        PastikanKomponen<DragonSkillVisuals>(objek, ringkasan);
        PastikanKomponen<DragonJuice>(objek, ringkasan);
        PastikanKomponen<DragonHud>(objek, ringkasan);
        PastikanKomponen<TargetSutra>(objek, ringkasan);
    }

    private static void TambahkanSkillLimau(GameObject objek, List<string> ringkasan)
    {
        PastikanKomponen<LimauSkillInput>(objek, ringkasan);
        PastikanKomponen<LimauPhysicsHandler>(objek, ringkasan);
        PastikanKomponen<LimauSilkHandler>(objek, ringkasan);
        PastikanKomponen<LimauSkillNetwork>(objek, ringkasan);
        PastikanKomponen<LimauSkillController>(objek, ringkasan);
        PastikanKomponen<LimauInflateSkill>(objek, ringkasan);
        PastikanKomponen<LimauBioluminesensiSkill>(objek, ringkasan);
        PastikanKomponen<LimauSilkSkill>(objek, ringkasan);
        PastikanKomponen<LimauBantal>(objek, ringkasan);
        PastikanKomponen<LimauAura>(objek, ringkasan);
        PastikanKomponen<LimauSkillVisuals>(objek, ringkasan);

        Transform model = null;
        foreach (Transform child in objek.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "ModelPivot")
            {
                model = child;
                break;
            }
        }

        LimauPhysicsHandler physics = objek.GetComponent<LimauPhysicsHandler>();
        if (physics != null)
        {
            SerializedObject physicsSerialized = new SerializedObject(physics);
            SerializedProperty modelProperty = physicsSerialized.FindProperty("model");
            if (modelProperty != null)
            {
                modelProperty.objectReferenceValue = model;
                physicsSerialized.ApplyModifiedPropertiesWithoutUndo();
                ringkasan.Add("LimauPhysicsHandler.model dihubungkan ke ModelPivot.");
            }
        }

        GameObject lineObject = CariAtauBuatAnak(objek, "LimauSilkLine");
        LineRenderer line = lineObject.GetComponent<LineRenderer>();
        if (line == null) line = lineObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = 0.05f;
        line.endWidth = 0.05f;
        line.useWorldSpace = true;
        line.enabled = false;

        GameObject lightObject = CariAtauBuatAnak(objek, "LimauAuraLight");
        Light light = lightObject.GetComponent<Light>();
        if (light == null) light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 6f;
        light.intensity = 0f;
        light.enabled = false;

        LimauSkillVisuals visuals = objek.GetComponent<LimauSkillVisuals>();
        LimauSkillNetwork network = objek.GetComponent<LimauSkillNetwork>();
        LimauSilkHandler silk = objek.GetComponent<LimauSilkHandler>();
        LimauAura aura = objek.GetComponent<LimauAura>();
        if (visuals != null)
        {
            SerializedObject visualsSerialized = new SerializedObject(visuals);
            visualsSerialized.FindProperty("jaringan").objectReferenceValue = network;
            visualsSerialized.FindProperty("sutra").objectReferenceValue = silk;
            visualsSerialized.FindProperty("aura").objectReferenceValue = aura;
            visualsSerialized.FindProperty("garis").objectReferenceValue = line;
            visualsSerialized.FindProperty("cahaya").objectReferenceValue = light;
            visualsSerialized.ApplyModifiedPropertiesWithoutUndo();
            ringkasan.Add("LimauSkillVisuals dihubungkan ke network, sutra, aura, garis, dan cahaya.");
        }
    }

    private static void PastikanKeyStateDragon(GameObject objek, List<string> ringkasan)
    {
        Transform anchor = null;
        foreach (Transform child in objek.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "KeyAnchor")
            {
                anchor = child;
                break;
            }
        }

        if (anchor == null)
        {
            GameObject anchorObjek = new GameObject("KeyAnchor");
            anchorObjek.transform.SetParent(objek.transform);
            anchorObjek.transform.localPosition = new Vector3(0.5f, 1f, 0f);
            anchorObjek.transform.localRotation = Quaternion.identity;
            anchor = anchorObjek.transform;
            ringkasan.Add("KeyAnchor ditambahkan pada Dragon.prefab.");
        }

        PlayerKeyState state = objek.GetComponent<PlayerKeyState>();
        if (state == null)
        {
            state = objek.AddComponent<PlayerKeyState>();
            ringkasan.Add("PlayerKeyState ditambahkan pada Dragon.prefab.");
        }

        SerializedObject serializedState = new SerializedObject(state);
        SerializedProperty keyAnchor = serializedState.FindProperty("keyAnchor");
        if (keyAnchor != null)
        {
            keyAnchor.objectReferenceValue = anchor;
            serializedState.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static GameObject CariAtauBuatAnak(GameObject induk, string nama)
    {
        foreach (Transform child in induk.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == nama) return child.gameObject;
        }

        GameObject childBaru = new GameObject(nama);
        childBaru.transform.SetParent(induk.transform, false);
        return childBaru;
    }

    private static void PastikanKomponen<T>(GameObject objek, List<string> ringkasan)
        where T : Component
    {
        if (objek.GetComponent<T>() != null)
        {
            ringkasan.Add($"{typeof(T).Name} sudah ada (dilewati).");
            return;
        }

        objek.AddComponent<T>();
        ringkasan.Add($"{typeof(T).Name} ditambahkan.");
    }

    private static void TerapkanMaterial(GameObject objek, Material material)
    {
        Renderer[] renderers = objek.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer.GetComponentInParent<TMP_Text>() != null)
            {
                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials.Length == 0)
            {
                materials = new Material[1];
            }

            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = material;
            }

            renderer.sharedMaterials = materials;
        }
    }

    private static Material PastikanMaterial(string nama, Color warna, List<string> ringkasan)
    {
        string jalur = $"{FolderMaterial}/{nama}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(jalur);
        bool baru = material == null;

        if (baru)
        {
            Shader shader = Shader.Find(
                GraphicsSettings.currentRenderPipeline != null
                    ? "Universal Render Pipeline/Lit"
                    : "Standard");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }
            if (shader == null)
            {
                Debug.LogError($"Shader placeholder tidak ditemukan untuk material {nama}.");
                return null;
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, jalur);
            ringkasan.Add($"Material dibuat: {jalur}");
        }
        else
        {
            ringkasan.Add($"Material sudah ada (dilewati pembuatan): {jalur}");
        }

        material.color = warna;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static bool PastikanTag()
    {
        Object[] aset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (aset.Length == 0)
        {
            Debug.LogError("ProjectSettings/TagManager.asset tidak dapat dimuat.");
            return false;
        }

        SerializedObject pengaturan = new SerializedObject(aset[0]);
        SerializedProperty tags = pengaturan.FindProperty("tags");
        string[] tagWajib = { "Pushable", "Breakable" };
        bool berubah = false;

        foreach (string tag in tagWajib)
        {
            bool ditemukan = false;
            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    ditemukan = true;
                    break;
                }
            }

            if (ditemukan)
            {
                continue;
            }

            int indeks = tags.arraySize;
            tags.InsertArrayElementAtIndex(indeks);
            tags.GetArrayElementAtIndex(indeks).stringValue = tag;
            berubah = true;
        }

        if (berubah)
        {
            pengaturan.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log("Tag Pushable dan/atau Breakable ditambahkan ke TagManager.");
        }
        else
        {
            Debug.Log("Tag Pushable dan Breakable sudah ada (dilewati).");
        }

        return true;
    }

    private static bool PastikanFolder(string jalur)
    {
        if (AssetDatabase.IsValidFolder(jalur))
        {
            return true;
        }

        string induk = Path.GetDirectoryName(jalur).Replace('\\', '/');
        string nama = Path.GetFileName(jalur);
        if (!AssetDatabase.IsValidFolder(induk))
        {
            Debug.LogError($"Folder induk tidak ditemukan: {induk}");
            return false;
        }

        AssetDatabase.CreateFolder(induk, nama);
        return AssetDatabase.IsValidFolder(jalur);
    }

    private static void BuatLingkungan()
    {
        BuatKubus("Lantai", new Vector3(0f, -0.5f, 0f), new Vector3(40f, 1f, 4f));
        BuatKubus("Platform 1", new Vector3(-2f, 1f, 0f), new Vector3(4f, 0.5f, 3f));
        BuatKubus("Platform 2", new Vector3(6f, 1.5f, 0f), new Vector3(4f, 0.5f, 3f));
    }

    private static GameObject BuatSpawner()
    {
        GameObject spawner = new GameObject("Spawner");
        Transform titikPertama = BuatTitikSpawn("Titik Spawn 1", new Vector3(-16f, 0.1f, 0f));
        Transform titikKedua = BuatTitikSpawn("Titik Spawn 2", new Vector3(-14f, 0.1f, 0f));
        SpawnPemain spawnPemain = spawner.AddComponent<SpawnPemain>();
        spawnPemain.titikSpawn = new[] { titikPertama, titikKedua };
        spawnPemain.namaPrefab = "Dragon";
        spawner.SetActive(false);
        return spawner;
    }

    private static void BuatBootstrap(GameObject spawner)
    {
        GameObject bootstrapObjek = new GameObject("TesOfflineBootstrap");
        TesOfflineBootstrap bootstrapKomponen = bootstrapObjek.AddComponent<TesOfflineBootstrap>();
        SerializedObject bootstrap = new SerializedObject(bootstrapKomponen);
        bootstrap.FindProperty("spawner").objectReferenceValue = spawner;
        bootstrap.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Transform BuatTitikSpawn(string nama, Vector3 posisi)
    {
        GameObject titik = new GameObject(nama);
        titik.transform.position = posisi;
        return titik.transform;
    }

    private static void BuatKamera()
    {
        GameObject kamera = new GameObject("Main Camera");
        kamera.tag = "MainCamera";
        kamera.transform.position = new Vector3(0f, 2f, -10f);
        kamera.AddComponent<Camera>();
        kamera.AddComponent<CameraIkut>();
    }

    private static void BuatPerkembanganBioma()
    {
        GameObject bioma = new GameObject("BiomeProgression");
        PhotonView photonView = bioma.AddComponent<PhotonView>();
        bioma.AddComponent<BiomeProgression>();
        photonView.sceneViewId = 1;
    }

    private static void BuatZonaBioma()
    {
        BuatZonaLevel("Zona Bioma 1", -10f, 1);
        BuatZonaLevel("Zona Bioma 2", 0f, 2);
        BuatZonaLevel("Zona Bioma 3", 11f, 3);
    }

    private static void BuatZonaLevel(string nama, float posisiX, int level)
    {
        GameObject zona = new GameObject(nama);
        zona.transform.position = new Vector3(posisiX, 1.5f, 0f);
        BoxCollider collider = zona.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(1.5f, 4f, 4f);
        BiomeLevelZone komponen = zona.AddComponent<BiomeLevelZone>();
        SerializedObject properti = new SerializedObject(komponen);
        properti.FindProperty("levelTujuan").intValue = level;
        properti.FindProperty("hanyaNaik").boolValue = true;
        properti.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuatObjekSkill()
    {
        GameObject dorong = BuatKubus(
            "Pushable",
            new Vector3(-7f, 0.5f, 0f),
            Vector3.one);
        dorong.tag = "Pushable";
        Rigidbody badan = dorong.AddComponent<Rigidbody>();
        badan.mass = 20f;
        PhotonView photonView = dorong.AddComponent<PhotonView>();
        photonView.OwnershipTransfer = OwnershipOption.Takeover;
        PhotonRigidbodyView rigidbodyView = dorong.AddComponent<PhotonRigidbodyView>();
        rigidbodyView.m_SynchronizeVelocity = true;
        rigidbodyView.m_SynchronizeAngularVelocity = true;
        photonView.ObservedComponents = new List<Component> { rigidbodyView };
        dorong.AddComponent<PushableObject>();
        photonView.sceneViewId = 5;

        GameObject pecah = BuatKubus(
            "Breakable Batu",
            new Vector3(7f, 0.5f, 0f),
            Vector3.one);
        pecah.tag = "Breakable";
        PhotonView photonBreakable = pecah.AddComponent<PhotonView>();
        pecah.AddComponent<BreakableObject>();
        photonBreakable.sceneViewId = 6;
    }

    private static void BuatHazard()
    {
        GameObject hazard = BuatKubus(
            "Hazard Angin Tes",
            new Vector3(2f, 1.5f, 0f),
            new Vector3(2.5f, 3f, 3f));
        BoxCollider collider = hazard.GetComponent<BoxCollider>();
        collider.isTrigger = true;
        hazard.GetComponent<MeshRenderer>().enabled = false;
        hazard.AddComponent<HazardAnginTes>();
    }

    private static GameObject BuatKubus(string nama, Vector3 posisi, Vector3 skala)
    {
        GameObject objek = GameObject.CreatePrimitive(PrimitiveType.Cube);
        objek.name = nama;
        objek.transform.position = posisi;
        objek.transform.localScale = skala;
        return objek;
    }

    private static void TambahkanKeBuildSettings()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        foreach (EditorBuildSettingsScene scene in scenes)
        {
            if (scene.path == JalurSceneTes)
            {
                Debug.Log("DragonTest sudah ada di Build Settings (dilewati).");
                return;
            }
        }

        List<EditorBuildSettingsScene> daftar = new List<EditorBuildSettingsScene>(scenes)
        {
            new EditorBuildSettingsScene(JalurSceneTes, true)
        };
        EditorBuildSettings.scenes = daftar.ToArray();
        Debug.Log("DragonTest ditambahkan di akhir Build Settings; urutan scene yang ada dipertahankan.");
    }

    private static void TambahkanKetigaSceneTesKeBuildSettings()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        List<EditorBuildSettingsScene> daftar = new List<EditorBuildSettingsScene>(scenes);

        for (int level = 1; level <= 3; level++)
        {
            string jalur = $"Assets/Scenes/DragonTest_Level{level}.unity";
            bool sudahAda = false;
            foreach (EditorBuildSettingsScene scene in daftar)
            {
                if (scene.path == jalur)
                {
                    sudahAda = true;
                    break;
                }
            }

            if (sudahAda)
            {
                Debug.Log($"{jalur} sudah ada di Build Settings (dilewati).");
                continue;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(jalur) != null)
            {
                daftar.Add(new EditorBuildSettingsScene(jalur, true));
                Debug.Log($"{jalur} ditambahkan ke Build Settings.");
            }
        }

        EditorBuildSettings.scenes = daftar.ToArray();
    }

    private static void TetapkanPhotonViewSceneId()
    {
        HashSet<int> idDipakai = new HashSet<int>();
        int idBerikutnya = 1;

        foreach (PhotonView view in Object.FindObjectsByType<PhotonView>(FindObjectsSortMode.None))
        {
            if (view.sceneViewId == 0)
            {
                while (idDipakai.Contains(idBerikutnya))
                    idBerikutnya++;

                view.sceneViewId = idBerikutnya;
                idDipakai.Add(idBerikutnya++);
                EditorUtility.SetDirty(view);
            }
            else
            {
                idDipakai.Add(view.sceneViewId);
            }
        }
    }

    private static void TambahkanSceneKeBuildSettings(string jalur)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(jalur) == null)
            return;

        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        foreach (EditorBuildSettingsScene scene in scenes)
        {
            if (scene.path == jalur)
            {
                Debug.Log($"{jalur} sudah ada di Build Settings (dilewati).");
                return;
            }
        }

        List<EditorBuildSettingsScene> daftar = new List<EditorBuildSettingsScene>(scenes)
        {
            new EditorBuildSettingsScene(jalur, true)
        };
        EditorBuildSettings.scenes = daftar.ToArray();
    }

    private static void BersihkanSceneDummyBuildSettings()
    {
        List<EditorBuildSettingsScene> daftar = new List<EditorBuildSettingsScene>();

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.path == "Assets/Scenes/Lobby Scene.unity" ||
                scene.path == "Assets/Scenes/Game Scene.unity")
            {
                daftar.Add(scene);
            }
        }

        EditorBuildSettings.scenes = daftar.ToArray();
    }
}
