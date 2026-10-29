using UnityEngine;

public class CameraIkut : MonoBehaviour
{
    private static Transform target;

    [SerializeField] private float tinggiOffset = 2f;
    [SerializeField] private float halus = 6f;

    public static void Daftar(Transform t)
    {
        target = t;
    }

    public static void Hapus(Transform t)
    {
        if (target == t)
            target = null;
    }

    private float posisiZ;

    private void Awake()
    {
        posisiZ = transform.position.z;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 tujuan = new Vector3(
            target.position.x,
            target.position.y + tinggiOffset,
            posisiZ
        );

        transform.position = Vector3.Lerp(
            transform.position,
            tujuan,
            halus * Time.deltaTime
        );
        transform.position = new Vector3(transform.position.x, transform.position.y, posisiZ);
        transform.rotation = Quaternion.identity;
    }
}