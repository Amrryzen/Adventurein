using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pabrik efek visual dummy tanpa aset: material, primitif tanpa collider, dan partikel.
/// Dipakai DragonJuice, AnginBerkala, dan BurungPelatuk agar tidak menduplikasi kode.
/// Shader dicoba berurutan (Sprites/Default dulu karena mendukung alpha di semua render pipeline).
/// </summary>
public static class FabrikEfek
{
    private static readonly Dictionary<Color, Material> cache = new();
    private static Shader shaderCache;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatik()
    {
        cache.Clear();
        shaderCache = null;
    }

    private static Shader CariShader()
    {
        if (shaderCache != null) return shaderCache;

        string[] kandidat = { "Sprites/Default", "Universal Render Pipeline/Unlit", "Unlit/Color", "Standard" };
        foreach (string nama in kandidat)
        {
            Shader shader = Shader.Find(nama);
            if (shader != null)
            {
                shaderCache = shader;
                break;
            }
        }

        return shaderCache;
    }

    public static Material AmbilMaterial(Color warna)
    {
        if (cache.TryGetValue(warna, out Material material) && material != null) return material;

        Shader shader = CariShader();
        if (shader == null) return null;

        material = new Material(shader) { color = warna };
        cache[warna] = material;
        return material;
    }

    /// <summary>Primitif berwarna tanpa collider dan tanpa bayangan. Material-nya salinan (aman diubah alpha-nya).</summary>
    public static GameObject BuatPrimitif(PrimitiveType tipe, string nama, Color warna, Transform induk)
    {
        GameObject objek = GameObject.CreatePrimitive(tipe);
        objek.name = nama;
        if (induk != null) objek.transform.SetParent(induk, false);

        Collider kol = objek.GetComponent<Collider>();
        if (kol != null) Object.Destroy(kol);

        Renderer renderer = objek.GetComponent<Renderer>();
        Material dasar = AmbilMaterial(warna);
        if (renderer != null && dasar != null)
        {
            renderer.sharedMaterial = new Material(dasar);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        return objek;
    }

    /// <summary>
    /// Sistem partikel serbaguna (world space, tanpa emisi otomatis).
    /// Burst: Pancar(ps, posisi, jumlah). Kontinu: AturLaju(ps, partikelPerDetik).
    /// </summary>
    public static ParticleSystem BuatPartikel(string nama, Transform induk, Color warna, float ukuran,
                                              float umur, float kecepatanMin, float kecepatanMaks, float gravitasi)
    {
        GameObject objek = new GameObject(nama);
        if (induk != null) objek.transform.SetParent(induk, false);

        ParticleSystem ps = objek.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = umur;
        main.startSpeed = new ParticleSystem.MinMaxCurve(kecepatanMin, kecepatanMaks);
        main.startSize = new ParticleSystem.MinMaxCurve(ukuran * 0.6f, ukuran);
        main.startColor = warna;
        main.gravityModifier = gravitasi;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 160;

        ParticleSystem.EmissionModule emisi = ps.emission;
        emisi.enabled = true;
        emisi.rateOverTime = 0f;

        ParticleSystem.ShapeModule bentuk = ps.shape;
        bentuk.shapeType = ParticleSystemShapeType.Sphere;
        bentuk.radius = 0.15f;

        ParticleSystem.SizeOverLifetimeModule mengecil = ps.sizeOverLifetime;
        mengecil.enabled = true;
        mengecil.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        ParticleSystemRenderer renderer = objek.GetComponent<ParticleSystemRenderer>();
        Material material = AmbilMaterial(Color.white);
        if (renderer != null && material != null) renderer.sharedMaterial = material;

        ps.Play();
        return ps;
    }

    public static void Pancar(ParticleSystem ps, Vector3 posisi, int jumlah)
    {
        if (ps == null) return;
        ps.transform.position = posisi;
        ps.Emit(jumlah);
    }

    public static void AturLaju(ParticleSystem ps, float partikelPerDetik)
    {
        if (ps == null) return;
        ParticleSystem.EmissionModule emisi = ps.emission;
        emisi.rateOverTime = partikelPerDetik;
    }
}
