using UnityEngine;

public class KeyAnimation : MonoBehaviour
{
    [Header("Rotasi")]
    [SerializeField] private float kecepatanPutar = 90f;
    [SerializeField] private Vector3 sumbuPutar = Vector3.up;

    [Header("Cahaya Berkedip")]
    [SerializeField] private Light cahayaKunci;
    [SerializeField] private float kecepatanKedip = 2f;
    [SerializeField] private float intensitasMin = 0.5f;
    [SerializeField] private float intensitasMax = 2.5f;

    private void Update()
    {
        // Memutar objek
        transform.Rotate(sumbuPutar * kecepatanPutar * Time.deltaTime, Space.World);

        // Membuat cahaya berkedip (pulsing)
        if (cahayaKunci != null)
        {
            float pingPong = Mathf.PingPong(Time.time * kecepatanKedip, 1f);
            cahayaKunci.intensity = Mathf.Lerp(intensitasMin, intensitasMax, pingPong);
        }
    }
}