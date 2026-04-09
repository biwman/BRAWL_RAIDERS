using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    int mapSeed;
    public GameObject obstaclePrefab;
    public int obstacleCount = 10;

    public float mapSizeX = 25f;
    public float mapSizeY = 25f;

    public float margin = 2f;
    public float checkRadius = 1.5f;

    void Start()
    {
        Debug.Log("ObstacleSpawner Start");

        if (!PhotonNetwork.InRoom)
        {
            Debug.Log("Czekam aż dołączę do pokoju...");
            Invoke(nameof(Start), 0.2f);
            return;
        }

        Debug.Log("IsMaster: " + PhotonNetwork.IsMasterClient);

        if (PhotonNetwork.IsMasterClient)
        {
            mapSeed = Random.Range(0, 100000);
            Debug.Log("Seed set: " + mapSeed);

            Hashtable props = new Hashtable();
            props["mapSeed"] = mapSeed;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            GenerateMap();
        }
        else
        {
            Invoke(nameof(WaitForSeed), 0.5f);
        }
    }

    void WaitForSeed()
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.Log("Jeszcze nie w pokoju...");
            Invoke(nameof(WaitForSeed), 0.2f);
            return;
        }

        Debug.Log("Waiting for seed...");

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("mapSeed"))
        {
            mapSeed = (int)PhotonNetwork.CurrentRoom.CustomProperties["mapSeed"];
            Debug.Log("Got seed: " + mapSeed);

            GenerateMap();
        }
        else
        {
            Invoke(nameof(WaitForSeed), 0.2f);
        }
    }

    void GenerateMap()
    {
        Random.InitState(mapSeed);
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

            Collider2D hit = Physics2D.OverlapCircle(pos, checkRadius);

            if (hit == null)
            {
                Instantiate(obstaclePrefab, pos, Quaternion.identity);
                spawned++; // 🔥 tego brakowało
            }
        }
    }
}