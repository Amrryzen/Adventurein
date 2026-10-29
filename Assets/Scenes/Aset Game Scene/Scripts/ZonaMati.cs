using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ZonaMati : MonoBehaviour
{
    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController p = other.GetComponentInParent<PlayerController>();
        if (p != null) p.Mati();
    }
}