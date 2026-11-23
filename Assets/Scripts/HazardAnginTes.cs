using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HazardAnginTes : MonoBehaviour
{
    [SerializeField] private float gayaAngin = 8f;
    [SerializeField] private Vector3 arahAngin = Vector3.right;
    [SerializeField] private bool patukanAktif = true;
    [SerializeField] private float jedaPatukan = 2f;
    [SerializeField] private float kekuatanPatukan = 12f;

    private readonly HashSet<PlayerController> pemainDalamZona = new HashSet<PlayerController>();
    private float patukanBerikutnya;

    private void OnTriggerEnter(Collider other)
    {
        PlayerController pemain = other.GetComponentInParent<PlayerController>();
        if (pemain != null)
        {
            pemainDalamZona.Add(pemain);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController pemain = other.GetComponentInParent<PlayerController>();
        if (pemain != null)
        {
            pemainDalamZona.Remove(pemain);
        }
    }

    private void FixedUpdate()
    {
        foreach (PlayerController pemain in pemainDalamZona)
        {
            if (pemain == null || !pemain.photonView.IsMine)
            {
                continue;
            }

            DragonSkillNetwork jaringan = pemain.GetComponent<DragonSkillNetwork>();
            if (jaringan != null && jaringan.IsAktif(DragonSkillType.SteelSkin))
            {
                continue;
            }

            Rigidbody badan = pemain.GetComponent<Rigidbody>();
            if (badan != null)
            {
                badan.AddForce(arahAngin.normalized * gayaAngin, ForceMode.Acceleration);
            }
        }

        if (patukanAktif && Time.time >= patukanBerikutnya)
        {
            patukanBerikutnya = Time.time + jedaPatukan;
            foreach (PlayerController pemain in pemainDalamZona)
            {
                if (pemain == null || !pemain.photonView.IsMine)
                    continue;

                DragonSkillNetwork jaringan = pemain.GetComponent<DragonSkillNetwork>();
                if (jaringan != null && jaringan.IsAktif(DragonSkillType.SteelSkin))
                    continue;

                Rigidbody badan = pemain.GetComponent<Rigidbody>();
                if (badan != null)
                    badan.AddForce(arahAngin.normalized * kekuatanPatukan, ForceMode.Impulse);
            }
        }
    }
}
