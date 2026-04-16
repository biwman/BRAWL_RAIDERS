using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public class ObstacleSpawner : MonoBehaviour
{
    const string GameStartedKey = "gameStarted";
    const string ObstacleDensityKey = "obstacleDensity";
    const string MapSeedKey = "mapSeed";
    const string ObstacleLayoutKey = "obstacleLayout";
    const string ExtractionLayoutKey = "extractionLayout";
    const float MinDistanceFromExtraction = 3.5f;

    int mapSeed;
    public GameObject obstaclePrefab;
    public int obstacleCount = 10;

    public float mapSizeX = 25f;
    public float mapSizeY = 25f;

    public float margin = 2f;
    public float checkRadius = 1.5f;
    public float minObstacleDistance = 3.25f;

    bool layoutApplied = false;
    int ResolvedObstacleCount => Mathf.Max(1, Mathf.RoundToInt(obstacleCount * GetDensityMultiplier() * RoomSettings.GetMapAreaMultiplier()));

    void Start()
    {
        Vector2 mapSize = RoomSettings.GetMapDimensions();
        mapSizeX = mapSize.x;
        mapSizeY = mapSize.y;

        Debug.Log("ObstacleSpawner Start");
        StartCoroutine(InitializeWhenRoundStarts());
    }

    IEnumerator InitializeWhenRoundStarts()
    {
        while (!PhotonNetwork.InRoom)
            yield return null;

        while (!IsRoundStarted())
            yield return null;

        while (!HasExtractionLayout())
            yield return null;

        if (layoutApplied)
            yield break;

        if (PhotonNetwork.IsMasterClient)
        {
            mapSeed = Random.Range(0, 100000);
            string layout = BuildObstacleLayout(mapSeed);

            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props[MapSeedKey] = mapSeed;
            props[ObstacleLayoutKey] = layout;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            ApplyLayoutIfNeeded(layout);
        }
        else
        {
            yield return StartCoroutine(WaitForFreshLayout());
        }
    }

    IEnumerator WaitForFreshLayout()
    {
        while (!layoutApplied)
        {
            if (PhotonNetwork.CurrentRoom != null &&
                PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(ObstacleLayoutKey, out object layoutValue) &&
                layoutValue is string layout &&
                !string.IsNullOrWhiteSpace(layout))
            {
                if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(MapSeedKey, out object seedValue) && seedValue is int seed)
                {
                    mapSeed = seed;
                }

                ApplyLayoutIfNeeded(layout);
            }

            if (!layoutApplied)
                yield return null;
        }
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
        Vector2 mapSize = RoomSettings.GetMapDimensions();
        mapSizeX = mapSize.x;
        mapSizeY = mapSize.y;

        Random.State previousState = Random.state;
        Random.InitState(seed);

        List<Vector2> positions = new List<Vector2>();
        List<Vector2> extractionPositions = ParseExtractionPositions();
        int spawned = 0;
        int attempts = 0;
        int targetCount = ResolvedObstacleCount;

        while (spawned < targetCount && attempts < 300)
        {
            attempts++;

            float x = Random.Range(-mapSizeX / 2 + margin, mapSizeX / 2 - margin);
            float y = Random.Range(-mapSizeY / 2 + margin, mapSizeY / 2 - margin);
            Vector2 pos = new Vector2(x, y);
            Collider2D hit = Physics2D.OverlapCircle(pos, checkRadius);

            if (hit == null && IsFarEnoughFromOtherObstacles(pos, positions) && IsFarEnoughFromExtractionZones(pos, extractionPositions))
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
        int obstacleIndex = 0;

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

            GameObject obstacle = Instantiate(obstaclePrefab, new Vector2(x, y), Quaternion.identity);
            ConfigureMovingObstacle(obstacle, obstacleIndex);
            obstacleIndex++;
        }
    }

    void ConfigureMovingObstacle(GameObject obstacle, int obstacleIndex)
    {
        if (obstacle == null)
            return;

        MovingSpaceObject movingObject = obstacle.GetComponent<MovingSpaceObject>();
        if (movingObject == null)
        {
            movingObject = obstacle.AddComponent<MovingSpaceObject>();
        }

        movingObject.Configure("obstacle_" + obstacleIndex, MovingSpaceObject.SpaceObjectType.Obstacle);
    }

    bool IsFarEnoughFromOtherObstacles(Vector2 candidate, List<Vector2> positions)
    {
        for (int i = 0; i < positions.Count; i++)
        {
            if (Vector2.Distance(candidate, positions[i]) < minObstacleDistance)
                return false;
        }

        return true;
    }

    bool IsFarEnoughFromExtractionZones(Vector2 candidate, List<Vector2> extractionPositions)
    {
        for (int i = 0; i < extractionPositions.Count; i++)
        {
            if (Vector2.Distance(candidate, extractionPositions[i]) < MinDistanceFromExtraction)
                return false;
        }

        return true;
    }

    List<Vector2> ParseExtractionPositions()
    {
        List<Vector2> positions = new List<Vector2>();

        if (PhotonNetwork.CurrentRoom == null ||
            !PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(ExtractionLayoutKey, out object value) ||
            value is not string layout ||
            string.IsNullOrWhiteSpace(layout))
        {
            return positions;
        }

        string[] entries = layout.Split(';');
        foreach (string entry in entries)
        {
            string[] parts = entry.Split(',');
            if (parts.Length != 2)
                continue;

            if (float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
            {
                positions.Add(new Vector2(x, y));
            }
        }

        return positions;
    }

    float GetDensityMultiplier()
    {
        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(ObstacleDensityKey, out object value) &&
            value is string density)
        {
            switch (density)
            {
                case "low": return 0.5f;
                case "high": return 2f;
                default: return 1f;
            }
        }

        return 1f;
    }

    bool IsRoundStarted()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return false;

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(GameStartedKey, out object value) && value is bool started)
        {
            return started;
        }

        return false;
    }

    bool HasExtractionLayout()
    {
        return PhotonNetwork.CurrentRoom != null &&
               PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(ExtractionLayoutKey, out object value) &&
               value is string layout &&
               !string.IsNullOrWhiteSpace(layout);
    }
}
