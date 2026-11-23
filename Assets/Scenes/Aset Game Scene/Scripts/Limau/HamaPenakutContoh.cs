using UnityEngine;

/// <summary>
/// CONTOH/TES: hama sederhana yang kabur dari aura Bioluminesensi Limau.
/// Dipakai hanya untuk membuktikan LimauAura bekerja. Pergerakannya lokal di tiap client
/// (tidak disinkronkan). Untuk musuh sungguhan: jalankan AI di satu otoritas (mis. MasterClient)
/// dan sinkronkan lewat PhotonTransformView; cukup panggil LimauAura.TryCariPengusir di AI-nya.
/// </summary>
public class HamaPenakutContoh : MonoBehaviour
{
    [SerializeField] private float kecepatanKabur = 3f;

    private void Update()
    {
        if (!LimauAura.TryCariPengusir(transform.position, out LimauAura aura)) return;

        float arahX = Mathf.Sign(transform.position.x - aura.transform.position.x);
        transform.position += Vector3.right * (arahX * kecepatanKabur * Time.deltaTime);
    }
}
