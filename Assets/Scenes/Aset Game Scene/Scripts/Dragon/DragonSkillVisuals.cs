using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visual feedback (animator, partikel, aura). Berjalan di SEMUA client:
/// mendengarkan event DragonSkillNetwork, jadi owner dan remote melihat hal yang sama.
/// Tidak berisi logika gameplay.
/// </summary>
public class DragonSkillVisuals : MonoBehaviour
{
    [SerializeField] private DragonSkillNetwork jaringan;

    [Header("Animator (opsional)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string paramPush = "IsPushing";     // bool
    [SerializeField] private string paramSteel = "IsSteelSkin";  // bool
    [SerializeField] private string triggerPukul = "Smash";      // trigger

    [Header("Efek (opsional)")]
    [SerializeField] private ParticleSystem vfxPukul;
    [SerializeField] private GameObject auraSteel;
    [SerializeField] private bool buatVfxRinganOtomatis = true;

    private readonly HashSet<int> paramAda = new();
    private int hashPush, hashSteel, hashPukul;
    private ParticleSystem vfxPukulDefault;

    private void Awake()
    {
        if (jaringan == null) jaringan = GetComponent<DragonSkillNetwork>();

        hashPush = Animator.StringToHash(paramPush);
        hashSteel = Animator.StringToHash(paramSteel);
        hashPukul = Animator.StringToHash(triggerPukul);

        // Cache parameter yang benar-benar ada agar tidak spam warning.
        if (animator != null)
            foreach (AnimatorControllerParameter p in animator.parameters)
                paramAda.Add(p.nameHash);

        if (auraSteel != null) auraSteel.SetActive(false);

        if (vfxPukul == null && buatVfxRinganOtomatis)
            vfxPukulDefault = BuatVfxPukulRingan();
    }

    private void OnEnable()
    {
        if (jaringan == null) return;
        jaringan.SkillAktifBerubah += SaatSkillAktifBerubah;
        jaringan.PukulanDipicu += SaatPukulan;
    }

    private void OnDisable()
    {
        if (jaringan == null) return;
        jaringan.SkillAktifBerubah -= SaatSkillAktifBerubah;
        jaringan.PukulanDipicu -= SaatPukulan;
    }

    private void SaatSkillAktifBerubah(DragonSkillType tipe, bool aktif)
    {
        switch (tipe)
        {
            case DragonSkillType.Push:
                SetBool(hashPush, aktif);
                break;

            case DragonSkillType.SteelSkin:
                SetBool(hashSteel, aktif);
                if (auraSteel != null) auraSteel.SetActive(aktif);
                break;
        }
    }

    private void SaatPukulan(Vector3 titik)
    {
        if (animator != null && paramAda.Contains(hashPukul))
            animator.SetTrigger(hashPukul);

        ParticleSystem vfx = vfxPukul != null ? vfxPukul : vfxPukulDefault;
        if (vfx != null)
        {
            vfx.transform.position = titik;
            vfx.Play();
        }
    }

    private ParticleSystem BuatVfxPukulRingan()
    {
        GameObject efek = new GameObject("VFX Pukul Ringan");
        efek.transform.SetParent(transform);
        efek.transform.localPosition = Vector3.zero;

        ParticleSystem particle = efek.AddComponent<ParticleSystem>();
        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule utama = particle.main;
        utama.duration = 0.25f;
        utama.loop = false;
        utama.startLifetime = 0.25f;
        utama.startSpeed = 4f;
        utama.startSize = 0.18f;
        utama.startColor = new Color(1f, 0.45f, 0.08f, 1f);
        utama.playOnAwake = false;

        ParticleSystem.EmissionModule emission = particle.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

        ParticleSystem.ShapeModule bentuk = particle.shape;
        bentuk.shapeType = ParticleSystemShapeType.Sphere;
        bentuk.radius = 0.12f;

        ParticleSystem.SizeOverLifetimeModule mengecil = particle.sizeOverLifetime;
        mengecil.enabled = true;
        mengecil.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        ParticleSystemRenderer renderer = efek.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        return particle;
    }

    private void SetBool(int hash, bool nilai)
    {
        if (animator != null && paramAda.Contains(hash))
            animator.SetBool(hash, nilai);
    }
}
