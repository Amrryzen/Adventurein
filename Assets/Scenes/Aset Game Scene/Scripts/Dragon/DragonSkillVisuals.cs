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

    private readonly HashSet<int> paramAda = new();
    private int hashPush, hashSteel, hashPukul;

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

        if (vfxPukul != null)
        {
            vfxPukul.transform.position = titik;
            vfxPukul.Play();
        }
    }

    private void SetBool(int hash, bool nilai)
    {
        if (animator != null && paramAda.Contains(hash))
            animator.SetBool(hash, nilai);
    }
}
