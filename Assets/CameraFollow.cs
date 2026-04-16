using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float mapSizeX = 25f;
    public float mapSizeY = 25f;

    float camHalfWidth;
    float camHalfHeight;

    void Start()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            camHalfHeight = cam.orthographicSize;
            camHalfWidth = camHalfHeight * Screen.width / Screen.height;
        }
    }

    void LateUpdate()
    {
        Vector2 mapSize = RoomSettings.GetMapDimensions();
        mapSizeX = mapSize.x;
        mapSizeY = mapSize.y;

        if (target == null)
            return;

        float minX = -mapSizeX / 2f + camHalfWidth;
        float maxX = mapSizeX / 2f - camHalfWidth;
        float minY = -mapSizeY / 2f + camHalfHeight;
        float maxY = mapSizeY / 2f - camHalfHeight;

        float clampedX = Mathf.Clamp(target.position.x, minX, maxX);
        float clampedY = Mathf.Clamp(target.position.y, minY, maxY);
        Vector3 targetPos = new Vector3(clampedX, clampedY, -10f);

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);
    }
}
