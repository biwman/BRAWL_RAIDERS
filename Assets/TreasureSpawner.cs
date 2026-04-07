using UnityEngine;

public class TreasureSpawner : MonoBehaviour
{
    public GameObject treasurePrefab; // 👈 TO DODAJ
    public int treasureCount = 10;

    public float mapSizeX = 25f;
    public float mapSizeY = 25f;

    void Start()
    {
        int spawned = 0;
        int attempts = 0;

        while (spawned < treasureCount && attempts < 100)
        {
            attempts++;

            float margin = 2f;

            float x = Random.Range(-mapSizeX / 2 + margin, mapSizeX / 2 - margin);
            float y = Random.Range(-mapSizeY / 2 + margin, mapSizeY / 2 - margin);

            Vector2 pos = new Vector2(x, y);

            Collider2D hit = Physics2D.OverlapCircle(pos, 1f);

            if (hit == null)
            {
                Instantiate(treasurePrefab, pos, Quaternion.identity);
                spawned++;
            }
        }
    }
}