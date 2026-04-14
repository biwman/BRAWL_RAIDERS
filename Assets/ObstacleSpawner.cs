using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public class ObstacleSpawner : MonoBehaviour
{
    int mapSeed;
    public GameObject obstaclePrefab;
    public int obstacleCount = 10;

    public float mapSizeX = 25f;
    public float mapSizeY = 25f;

    public float margin = 2f;
    public float checkRadius = 1.5f;
    public float minObstacleDistance = 3.25f;

    const string MapSeedKey = "mapSeed";
    const string ObstacleLayoutKey = "obstacleLayout";

    bool layoutApplied = false;

    void Start()
    {
        Debug.Log("ObstacleSpawner Start");

        if (!PhotonNetwork.InRoom)
        {
            Debug.Log("Czekam az dolacze do pokoju...");
            Invoke(nameof(Start), 0.2f);
            return;
        }

        Debug.Log("IsMaster: " + PhotonNetwork.IsMasterClient);

        if (PhotonNetwork.IsMasterClient)
        {
            mapSeed = Random.Range(0, 100000);
            Debug.Log("Seed set: " + mapSeed);
            string layout = BuildObstacleLayout(mapSeed);

            Hashtable props = new Hashtable();
            props[MapSeedKey] = mapSeed;
            props[ObstacleLayoutKey] = layout;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            ApplyLayoutIfNeeded(layout);
        }
        else
        {
            Invoke(nameof(WaitForLayout), 0.5f);
        }
    }

    void WaitForLayout()
    {
        if (layoutApplied)
            return;

        if (!PhotonNetwork.InRoom)
        {
            Debug.Log("Jeszcze nie w pokoju...");
            Invoke(nameof(WaitForLayout), 0.2f);
            return;
        }

        Debug.Log("Waiting for layout...");

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(ObstacleLayoutKey))
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(MapSeedKey, out object seedValue) && seedValue is int seed)
            {
                mapSeed = seed;
                Debug.Log("Got seed: " + mapSeed);
            }

            TryApplyLayoutFromRoom();
        }
        else
        {
            Invoke(nameof(WaitForLayout), 0.2f);
        }
    }

    void TryApplyLayoutFromRoom()
    {
        if (layoutApplied || PhotonNetwork.CurrentRoom == null)
            return;

        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(ObstacleLayoutKey, out object layoutValue))
            return;

        if (layoutValue is not string layout || string.IsNullOrWhiteSpace(layout))
            return;

        ApplyLayoutIfNeeded(layout);
    }

    void ApplyLayoutIfNeeded(string layout)
    {
        if (layoutApplied || string.IsNullOrWhiteSpace(layout))
            return;

        ApplyObstacleLayout(layout);
        layoutApplied = true;
    }

    string BuildObstacleLayout(int seed)
    {
        Random.State previousState = Random.state;
        Random.InitState(seed);

        List<Vector2> positions = new List<Vector2>();
        int spawned = 0;
        int attempts = 0;

        while (spawned < obstacleCount && attempts < 100)
        {
            attempts++;

            float x = Random.Range(-mapSizeX / 2 + margin, mapSizeX / 2 - margin);
            float y = Random.Range(-mapSizeY / 2 + margin, mapSizeY / 2 - margin);

            Vector2 pos = new Vector2(x, y);
            Collider2D hit = Physics2D.OverlapCircle(pos, checkRadius);

            if (hit == null && IsFarEnoughFromOtherObstacles(pos, positions))
            {
                positions.Add(pos);
                spawned++;
            }
        }

        Random.state = previousState;

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < positions.Count; i++)
        {
            if (i > 0)
                builder.Append(';');

            builder.Append(positions[i].x.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(positions[i].y.ToString(CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    void ApplyObstacleLayout(string layout)
    {
        string[] entries = layout.Split(';');

        foreach (string entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry))
                continue;

            string[] parts = entry.Split(',');
            if (parts.Length != 2)
                continue;

            if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x))
                continue;

            if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                continue;

            Instantiate(obstaclePrefab, new Vector2(x, y), Quaternion.identity);
        }
    }

    bool IsFarEnoughFromOtherObstacles(Vector2 candidate, List<Vector2> positions)
    {
        for (int i = 0; i < positions.Count; i++)
        {
            if (Vector2.Distance(candidate, positions[i]) < minObstacleDistance)
            {
                return false;
            }
        }

        return true;
    }
}
