using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float mapSizeX = 25f;
    public float mapSizeY = 25f;

    private float camHalfWidth;
    private float camHalfHeight;

    void Start()
    {
        camHalfHeight = Camera.main.orthographicSize;
        camHalfWidth = camHalfHeight * Screen.width / Screen.height;
    }

    void LateUpdate()
    {
        // 🔥 zabezpieczenie przed errorem
        if (target == null) return;

        float minX = -mapSizeX / 2 + camHalfWidth;
        float maxX = mapSizeX / 2 - camHalfWidth;

        float minY = -mapSizeY / 2 + camHalfHeight;
        float maxY = mapSizeY / 2 - camHalfHeight;

        float clampedX = Mathf.Clamp(target.position.x, minX, maxX);
        float clampedY = Mathf.Clamp(target.position.y, minY, maxY);

        Vector3 targetPos = new Vector3(clampedX, clampedY, -10);

        // 🎥 płynne podążanie (opcjonalne)
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);
    }
}