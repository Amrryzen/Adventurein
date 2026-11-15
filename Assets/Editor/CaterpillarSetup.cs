using System.Collections.Generic;
using System.IO;
using Photon.Pun;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class CaterpillarSetup
{
    private const string JalurPemain = "Assets/Resources/Player.prefab";
    private const string JalurDragon = "Assets/Resources/Dragon.prefab";
    private const string JalurLimau = "Assets/Resources/Limau.prefab";
    private const string JalurSceneTes = "Assets/Scenes/DragonTest.unity";
    private const string FolderMaterial = "Assets/Materials";

    [MenuItem("Tools/Caterpillar/1. Setup Karakter")]
    private static void SetupKarakter()
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
        PastikanVarian(pemainAsal, JalurLimau, materialLimau, false, ringkasan);
        Debug.Log("Setup karakter selesai:\n- " + string.Join("\n- ", ringkasan));
    }

    [MenuItem("Tools/Caterpillar/2. Buat Scene Tes Dragon")]
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
                ringkasan.Add($"Varian sudah ada, diperbarui tanpa membuat ulang: {jalur}");
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

            TerapkanMaterial(objek, material);
            if (tambahSkill)
            {
                TambahkanSkillDragon(objek, ringkasan);
            }
            else
            {
                // TODO: Limau belum memiliki skill; material placeholder saja yang dipasang.
                ringkasan.Add("Limau: skill tidak ditambahkan (belum didesain).");
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
}
