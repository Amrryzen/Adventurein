using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// HUB / STATE MACHINE. Meneruskan input ke seluruh skill Limau.
/// Hanya berjalan di client pemilik Limau (photonView.IsMine);
/// pemain remote cukup menerima event dari LimauSkillNetwork.
/// </summary>
[RequireComponent(typeof(LimauSkillInput), typeof(LimauPhysicsHandler),
                  typeof(LimauSilkHandler))]
[RequireComponent(typeof(LimauSkillNetwork))]
public class LimauSkillController : MonoBehaviourPun
{
    private LimauSkillInput masukan;
    private LimauPhysicsHandler fisik;
    private LimauSilkHandler sutra;
    private LimauSkillNetwork jaringan;
    private readonly List<LimauSkillBase> daftarSkill = new();
    private bool sedangMati;

    private void Awake()
    {
        masukan = GetComponent<LimauSkillInput>();
        fisik = GetComponent<LimauPhysicsHandler>();
        sutra = GetComponent<LimauSilkHandler>();
        jaringan = GetComponent<LimauSkillNetwork>();

        foreach (LimauSkillBase skill in GetComponents<LimauSkillBase>())
        {
            skill.Inisialisasi(masukan, fisik, sutra, jaringan);
            daftarSkill.Add(skill);
        }
    }

    private void Start()
    {
        // Pastikan eksekusi input hanya di pemilik karakter.
        if (!photonView.IsMine)
        {
            enabled = false;
            return;
        }

    }

    private void OnDisable()
    {
        foreach (LimauSkillBase skill in daftarSkill)
            skill.OnKeluar();
    }

    private void Update()
    {
        // Mati -> batalkan skill (lepas sutra, kecilkan badan, matikan cahaya) sekali, lalu diam.
        if (fisik.Mati)
        {
            if (!sedangMati)
            {
                sedangMati = true;
                foreach (LimauSkillBase skill in daftarSkill)
                    skill.OnKeluar();
            }
            return;
        }

        if (sedangMati)
        {
            sedangMati = false;
            foreach (LimauSkillBase skill in daftarSkill)
                skill.OnMasuk();
        }

        masukan.Sampel();
        foreach (LimauSkillBase skill in daftarSkill)
            skill.OnUpdateSkill();
    }

    private void FixedUpdate()
    {
        if (fisik.Mati) return;
        foreach (LimauSkillBase skill in daftarSkill)
            skill.OnFixedUpdateSkill();
    }
}
