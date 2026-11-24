using UnityEngine;

/// <summary>
/// Getaran kamera TANPA mengubah CameraIkut.
/// Cara kerja: offset getaran dicabut di Update (sebelum CameraIkut.LateUpdate membaca posisi),
/// lalu ditambahkan lagi di LateUpdate setelah CameraIkut selesai (DefaultExecutionOrder 100).
/// Pakai: CameraGetar.Getar(kekuatan, durasi). Dipasang otomatis oleh DragonJuice bila belum ada.
/// </summary>
[DefaultExecutionOrder(100)]
public class CameraGetar : MonoBehaviour
{
    private static CameraGetar instance;

    private Vector3 offsetTerakhir;
    private float kekuatan;
    private float durasi;
    private float sisa;

    public static bool Ada => instance != null;

    private void Awake()
    {
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    /// <summary>Getaran baru hanya menimpa getaran berjalan jika lebih kuat dari sisanya.</summary>
    public static void Getar(float kekuatan, float durasi)
    {
        if (instance == null || durasi <= 0f || kekuatan <= 0f) return;

        float kekuatanSisa = instance.sisa > 0f ? instance.kekuatan * (instance.sisa / instance.durasi) : 0f;
        if (kekuatan < kekuatanSisa) return;

        instance.kekuatan = kekuatan;
        instance.durasi = durasi;
        instance.sisa = durasi;
    }

    private void Update()
    {
        if (offsetTerakhir == Vector3.zero) return;

        transform.position -= offsetTerakhir;
        offsetTerakhir = Vector3.zero;
    }

    private void LateUpdate()
    {
        if (sisa <= 0f) return;

        // unscaled: getaran tetap jalan saat hit-stop memperlambat waktu.
        sisa -= Time.unscaledDeltaTime;
        float skala = Mathf.Clamp01(sisa / durasi);
        Vector2 acak = Random.insideUnitCircle * (kekuatan * skala);

        offsetTerakhir = new Vector3(acak.x, acak.y, 0f);
        transform.position += offsetTerakhir;
    }
}
