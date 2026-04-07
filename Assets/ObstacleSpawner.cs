using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject obstaclePrefab;
    public int obstacleCount = 10;

    public float mapSizeX = 25f;
    public float mapSizeY = 25f;

    public float margin = 2f;        // odstęp od ścian
    public float checkRadius = 1.5f; // sprawdzanie czy miejsce wolne

    void Start()
    {
        SpawnObstacles();
    }

    void SpawnObstacles()
    {
        int spawned = 0;
        int attempts = 0;

        while (spawned < obstacleCount && attempts < 100)
        {
            attempts++;

            float x = Random.Range(-mapSizeX / 2 + margin, mapSizeX / 2 - margin);
            float y = Random.Range(-mapSizeY / 2 + margin, mapSizeY / 2 - margin);

            Vector2 pos = new Vector2(x, y);

            // sprawdzamy czy coś już tam jest (ściana, obstacle, player itd.)
            Collider2D hit = Physics2D.OverlapCircle(pos, checkRadius);

            if (hit == null)
            {
                Instantiate(obstaclePrefab, pos, Quaternion.identity);
                spawned++;
            }
        }
    }
}
