using UnityEngine;
using Photon.Pun;
using System.Collections;

public class TreasureSpawner : MonoBehaviourPun
{
    public int treasureCount = 10;

    public float mapSizeX = 25f;
    public float mapSizeY = 25f;

    void Start()
    {
        // 🔥 zamiast odpalać od razu → czekamy na Photon
        Debug.Log("🔥 TreasureSpawner Start działa");
        StartCoroutine(SpawnWhenReady());
    }

    IEnumerator SpawnWhenReady()
    {
        // 🔥 czekamy aż Photon będzie gotowy
        while (!PhotonNetwork.IsConnectedAndReady)
        {
            yield return null;
        }

        Debug.Log("✅ Photon gotowy!");

        // 🔥 tylko MasterClient spawnuje
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log("❌ Nie jestem MasterClient - nie spawnuję");
            yield break;
        }

        Debug.Log("🔥 Jestem MasterClient - spawnuję skarby");

        SpawnTreasures();
    }

    void SpawnTreasures()
    {
        int spawned = 0;
        int attempts = 0;

        while (spawned < treasureCount && attempts < 100)
        {
            attempts++;

            float margin = 2f;

            float x = Random.Range(-mapSizeX / 2 + margin, mapSizeX / 2 - margin);
            float y = Random.Range(-mapSizeY / 2 + margin, mapSizeY / 2 - margin);

            Vector2 pos2D = new Vector2(x, y);

            Collider2D hit = Physics2D.OverlapCircle(pos2D, 1f);

            if (hit == null)
            {
                Vector3 pos = new Vector3(x, y, 0);

                // 🔥 KLUCZOWE: Photon spawn
                GameObject t = PhotonNetwork.Instantiate("TreasureNetwork", pos, Quaternion.identity);

                PhotonView pv = t.GetComponent<PhotonView>();

                if (pv == null)
                {
                    Debug.LogError("❌ Treasure NIE ma PhotonView!");
                }
                else
                {
                    Debug.Log("✅ Spawn OK: " + t.name);
                    Debug.Log("➡ ViewID: " + pv.ViewID);
                }

                spawned++;
            }
        }

        Debug.Log("🎯 Spawned treasures: " + spawned);
    }
}