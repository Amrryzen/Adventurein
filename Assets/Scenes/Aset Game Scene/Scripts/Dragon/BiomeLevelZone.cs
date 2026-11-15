using Photon.Pun;
using UnityEngine;

/// <summary>
/// Trigger di perbatasan bioma. Saat pemain lokal masuk, level bioma diminta berganti.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BiomeLevelZone : MonoBehaviour
{
    [SerializeField, Range(1, 3)] private int levelTujuan = 2;
    [Tooltip("Cegah level turun jika pemain kembali ke bioma sebelumnya.")]
    [SerializeField] private bool hanyaNaik = true;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController p = other.GetComponentInParent<PlayerController>();
        if (p == null || !p.photonView.IsMine) return;

        BiomeProgression biome = BiomeProgression.Instance;
        if (biome == null) return;
        if (hanyaNaik && levelTujuan <= biome.LevelSaatIni) return;

        biome.MintaGantiLevel(levelTujuan);
    }
}
