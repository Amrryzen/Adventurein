#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class DragonSkillDebugFeedback : MonoBehaviour
{
    private DragonSkillNetwork jaringan;
    private DragonSkillController controller;
    private DragonDestroySkill destroySkill;
    private PhotonView view;
    private CapsuleCollider kapsul;

    private GameObject panahPush;
    private GameObject auraSteel;
    private TextMesh labelMelayang;
    private Coroutine hapusLabel;
    private bool hudAktif = true;
    private bool cachePushAktif;
    private bool cacheSteelAktif;
    private float cacheCooldown;
    private int cacheLevel = -1;
    private string cacheSkill = "";
    private string cacheHud;

    private readonly Dictionary<Color, Material> materialPerWarna = new();

    private void Awake()
    {
        jaringan = GetComponent<DragonSkillNetwork>();
        controller = GetComponent<DragonSkillController>();
        destroySkill = GetComponent<DragonDestroySkill>();
        view = GetComponent<PhotonView>();
        kapsul = GetComponent<CapsuleCollider>();

        panahPush = BuatPanahPush();
        auraSteel = BuatAuraSteel();
        labelMelayang = BuatLabel();
        panahPush.SetActive(false);
        auraSteel.SetActive(false);
        labelMelayang.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (jaringan != null)
        {
            jaringan.SkillAktifBerubah += SaatSkillAktifBerubah;
            jaringan.PukulanDipicu += SaatPukulan;
        }

        if (controller != null)
            controller.SkillBerganti += SaatSkillBerganti;
    }

    private void OnDisable()
    {
        if (jaringan != null)
        {
            jaringan.SkillAktifBerubah -= SaatSkillAktifBerubah;
            jaringan.PukulanDipicu -= SaatPukulan;
        }

        if (controller != null)
            controller.SkillBerganti -= SaatSkillBerganti;
    }

    private void Update()
    {
        if (view != null && view.IsMine && Input.GetKeyDown(KeyCode.F1))
            hudAktif = !hudAktif;
    }

    private void LateUpdate()
    {
        if (labelMelayang != null && labelMelayang.gameObject.activeSelf)
        {
            if (Camera.main != null)
                labelMelayang.transform.rotation = Camera.main.transform.rotation;

            labelMelayang.transform.position =
                transform.position + Vector3.up * (kapsul != null ? kapsul.bounds.extents.y + 0.4f : 1.5f);
        }

        if (auraSteel != null && auraSteel.activeSelf)
        {
            float skala = 1f + Mathf.PingPong(Time.time * 2f, 0.08f);
            auraSteel.transform.localScale = Vector3.one * skala;
        }
    }

    private void SaatSkillAktifBerubah(DragonSkillType tipe, bool aktif)
    {
        string nama = tipe switch
        {
            DragonSkillType.Push => "Push",
            DragonSkillType.Destroy => "Destroy",
            DragonSkillType.SteelSkin => "SteelSkin",
            _ => tipe.ToString()
        };

        DragonDebug.Log($"{DragonDebug.Label(view)} | {nama} {(aktif ? "AKTIF" : "NONAKTIF")}");

        if (tipe == DragonSkillType.Push)
        {
            cachePushAktif = aktif;
            panahPush.SetActive(aktif);
            TampilkanLabel(aktif ? "PUSH ON" : "PUSH OFF", aktif ? new Color(0.1f, 0.8f, 1f) : new Color(0.5f, 0.5f, 0.5f), aktif);
        }
        else if (tipe == DragonSkillType.SteelSkin)
        {
            cacheSteelAktif = aktif;
            auraSteel.SetActive(aktif);
            TampilkanLabel(aktif ? "STEEL ON" : "STEEL OFF", new Color(0.72f, 0.72f, 0.72f), aktif);
        }
    }

    private void SaatPukulan(Vector3 titik)
    {
        DragonDebug.Log($"{DragonDebug.Label(view)} | Destroy PUKULAN di {titik}");
        TampilkanLabel("SMASH!", new Color(1f, 0.45f, 0.08f), true);
        StartCoroutine(EfekLedakan(titik));
    }

    private void SaatSkillBerganti(DragonSkillType tipe)
    {
        DragonDebug.Log($"{DragonDebug.Label(view)} | Level -> skill {tipe}", this);
    }

    private void TampilkanLabel(string teks, Color warna, bool aktif)
    {
        if (hapusLabel != null)
            StopCoroutine(hapusLabel);

        labelMelayang.text = teks;
        warna.a = 1f;
        labelMelayang.color = warna;
        labelMelayang.gameObject.SetActive(true);
        hapusLabel = StartCoroutine(HapusLabelSatuDetik(labelMelayang));
    }

    private IEnumerator HapusLabelSatuDetik(TextMesh target)
    {
        yield return new WaitForSeconds(1f);

        if (target != null)
            target.gameObject.SetActive(false);

        hapusLabel = null;
    }

    private IEnumerator EfekLedakan(Vector3 titik)
    {
        GameObject ledakan = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ledakan.name = "VFX Debug Ledakan";
        ledakan.transform.position = titik;
        ledakan.transform.localScale = Vector3.one * 0.2f;

        Collider colliderLedakan = ledakan.GetComponent<Collider>();
        if (colliderLedakan != null)
            Destroy(colliderLedakan);

        Renderer rendererLedakan = ledakan.GetComponent<Renderer>();
        Material materialLedakan = AmbilMaterial(new Color(1f, 0.45f, 0.08f, 1f));
        if (materialLedakan != null)
            rendererLedakan.sharedMaterial = new Material(materialLedakan);
        else
            rendererLedakan.sharedMaterial = null;

        materialLedakan = rendererLedakan.sharedMaterial;

        float waktu = 0f;
        while (waktu < 0.4f)
        {
            waktu += Time.deltaTime;
            float normal = Mathf.Clamp01(waktu / 0.4f);
            ledakan.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 1.2f, normal);
            ledakan.transform.Rotate(Vector3.up, 720f * Time.deltaTime);

            if (materialLedakan != null)
            {
                Color warna = materialLedakan.color;
                warna.a = Mathf.Lerp(1f, 0f, normal);
                materialLedakan.color = warna;
            }
            yield return null;
        }

        Destroy(ledakan);
    }

    private GameObject BuatPanahPush()
    {
        GameObject induk = new GameObject("Debug Push");
        induk.transform.SetParent(transform);
        induk.transform.localPosition = Vector3.up * 1.8f;

        for (int i = -1; i <= 1; i += 2)
        {
            GameObject panah = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panah.name = i < 0 ? "Panah Kiri" : "Panah Kanan";
            panah.transform.SetParent(induk.transform);
            panah.transform.localPosition = new Vector3(i * 0.7f, 0f, 0f);
            panah.transform.localScale = new Vector3(0.5f, 0.12f, 0.12f);
            HapusCollider(panah);
            panah.GetComponent<Renderer>().sharedMaterial =
                AmbilMaterial(new Color(0.1f, 0.8f, 1f, 0.9f));
        }

        return induk;
    }

    private GameObject BuatAuraSteel()
    {
        GameObject aura = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        aura.name = "Debug Steel Skin";
        aura.transform.SetParent(transform);
        aura.transform.localPosition = Vector3.zero;

        float radius = kapsul != null ? Mathf.Max(kapsul.radius * 1.5f, kapsul.height * 0.35f) : 1f;
        aura.transform.localScale = Vector3.one * radius * 2.2f;
        HapusCollider(aura);
        aura.GetComponent<Renderer>().sharedMaterial =
            AmbilMaterial(new Color(0.72f, 0.72f, 0.72f, 0.28f));
        return aura;
    }

    private TextMesh BuatLabel()
    {
        GameObject label = new GameObject("Debug Label");
        label.transform.SetParent(transform);
        TextMesh teks = label.AddComponent<TextMesh>();
        teks.characterSize = 0.08f;
        teks.fontSize = 32;
        teks.anchor = TextAnchor.MiddleCenter;
        teks.alignment = TextAlignment.Center;
        return teks;
    }

    private static void HapusCollider(GameObject objek)
    {
        foreach (Collider collider in objek.GetComponentsInChildren<Collider>())
            Destroy(collider);
    }

    private Material AmbilMaterial(Color warna)
    {
        if (materialPerWarna.TryGetValue(warna, out Material cache))
            return cache;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = shader != null ? new Material(shader) : null;
        if (material != null)
        {
            material.color = warna;
            if (warna.a < 1f)
                material.renderQueue = 3000;
        }

        materialPerWarna[warna] = material;
        return material;
    }

    private void OnGUI()
    {
        if (view == null || !view.IsMine || !hudAktif)
            return;

        int level = controller != null ? controller.SkillSaatIni != DragonSkillType.None ? (int)controller.SkillSaatIni : 0 : 0;
        string namaSkill = controller != null ? controller.SkillSaatIni.ToString() : "-";
        bool push = jaringan != null && jaringan.IsAktif(DragonSkillType.Push);
        bool steel = jaringan != null && jaringan.IsAktif(DragonSkillType.SteelSkin);
        float cooldown = destroySkill != null ? destroySkill.CooldownNormal : 0f;
        int levelBioma = BiomeProgression.Instance != null ? BiomeProgression.Instance.LevelSaatIni : 0;

        if (level != cacheLevel || namaSkill != cacheSkill ||
            push != cachePushAktif || steel != cacheSteelAktif ||
            Mathf.Abs(cooldown - cacheCooldown) > 0.01f)
        {
            cacheLevel = level;
            cacheSkill = namaSkill;
            cachePushAktif = push;
            cacheSteelAktif = steel;
            cacheCooldown = cooldown;
            cacheHud =
                $"Level skill: {cacheLevel}\n" +
                $"Skill: {cacheSkill}\n" +
                $"Push: {(push ? "ON" : "OFF")}\n" +
                $"Steel Skin: {(steel ? "ON" : "OFF")}\n" +
                $"Cooldown Destroy: {cooldown * 100f:0}%\n" +
                $"Level Bioma: {levelBioma}\nF1: sembunyikan";
        }

        GUI.Box(new Rect(10f, 10f, 230f, 120f), cacheHud);
        GUI.Label(new Rect(20f, 20f, 210f, 110f), cacheHud);
    }
}
#endif
