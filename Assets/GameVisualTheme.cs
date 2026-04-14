using Photon.Pun;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameVisualTheme : MonoBehaviour
{
    const float PlayerTargetSize = 1.04f;
    const float TreasureTargetSize = 1.5f;
    const float ObstacleTargetSize = 3.0f;
    const float ExtractionTargetSize = 4.3f;
    const float RefreshInterval = 0.75f;

    static GameVisualTheme instance;

    Sprite[] shipSprites;
    Sprite treasureSprite;
    Sprite obstacleSprite;
    Sprite extractionSprite;
    Sprite backgroundSprite;
    float nextRefreshTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null)
            return;

        GameObject root = new GameObject("GameVisualTheme");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<GameVisualTheme>();
    }

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        LoadAssets();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        ApplyTheme();
    }

    void Update()
    {
        if (Time.unscaledTime < nextRefreshTime)
            return;

        nextRefreshTime = Time.unscaledTime + RefreshInterval;
        ApplyTheme();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        LoadAssets();
        nextRefreshTime = 0f;
        ApplyTheme();
    }

    void LoadAssets()
    {
        shipSprites = new[]
        {
            LoadSpriteFromProjectOrResources("ship1.png", "Visuals/Ships/ship1_resource"),
            LoadSpriteFromProjectOrResources("ship2.png", "Visuals/Ships/ship2_resource"),
            LoadSpriteFromProjectOrResources("ship3.png", "Visuals/Ships/ship3_resource")
        };

        treasureSprite = LoadSpriteFromProjectOrResources("asteroida_treasure.png", "Visuals/Treasures/asteroid_treasure_resource");
        obstacleSprite = LoadSpriteFromProjectOrResources("asteroida_obstacle.png", "Visuals/Obstacles/asteroid_obstacle_resource");
        extractionSprite = LoadSpriteFromProjectOrResources("baza1.png", "Visuals/Bases/base1_resource");
        backgroundSprite = LoadSpriteFromProjectOrResources("tło5.png", "Visuals/Backgrounds/background5_resource");
    }

    void ApplyTheme()
    {
        ApplyGroundBackground();
        ApplyPlayerSprites();
        ApplyTreasureSprites();
        ApplyObstacleSprites();
        ApplyExtractionZoneSprites();
    }

    void ApplyGroundBackground()
    {
        if (backgroundSprite == null)
            return;

        GameObject ground = GameObject.Find("Ground");
        if (ground == null)
            return;

        SpriteRenderer renderer = ground.GetComponent<SpriteRenderer>();
        if (renderer == null)
            return;

        renderer.sprite = backgroundSprite;
        renderer.color = Color.white;
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = new Vector2(25f, 25f);
    }

    void ApplyPlayerSprites()
    {
        if (shipSprites == null || shipSprites.Length == 0)
            return;

        PlayerHealth[] players = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        foreach (PlayerHealth player in players)
        {
            if (player == null)
                continue;

            PhotonView view = player.GetComponent<PhotonView>();
            SpriteRenderer renderer = player.GetComponent<SpriteRenderer>();

            if (view == null || renderer == null || view.Owner == null)
                continue;

            int shipIndex = Mathf.Abs(view.Owner.ActorNumber - 1) % shipSprites.Length;
            Sprite sprite = shipSprites[shipIndex];
            if (sprite == null)
                continue;

            BoxCollider2D bodyCollider = player.GetComponent<BoxCollider2D>();
            CircleCollider2D pickupCollider = player.GetComponent<CircleCollider2D>();
            Vector2 bodyWorldSize = GetWorldBoxSize(bodyCollider);
            float pickupWorldRadius = GetWorldCircleRadius(pickupCollider);

            if (renderer.sprite != sprite)
            {
                renderer.sprite = sprite;
                renderer.color = Color.white;
            }
            FitSpriteToTargetSize(renderer, PlayerTargetSize);
            SetWorldBoxSize(bodyCollider, bodyWorldSize);
            SetWorldCircleRadius(pickupCollider, pickupWorldRadius);
        }
    }

    void ApplyTreasureSprites()
    {
        if (treasureSprite == null)
            return;

        Treasure[] treasures = FindObjectsByType<Treasure>(FindObjectsSortMode.None);
        foreach (Treasure treasure in treasures)
        {
            if (treasure == null)
                continue;

            SpriteRenderer renderer = treasure.GetComponent<SpriteRenderer>();
            if (renderer == null)
                continue;

            BoxCollider2D triggerCollider = treasure.GetComponent<BoxCollider2D>();
            Vector2 triggerWorldSize = GetWorldBoxSize(triggerCollider);

            if (renderer.sprite != treasureSprite)
            {
                renderer.sprite = treasureSprite;
            }
            FitSpriteToTargetSize(renderer, TreasureTargetSize);
            SetWorldBoxSize(triggerCollider, triggerWorldSize);
        }
    }

    void ApplyObstacleSprites()
    {
        if (obstacleSprite == null)
            return;

        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            GameObject target = renderer.gameObject;
            if (!target.name.StartsWith("Obstacle"))
                continue;

            if (target.GetComponent<PlayerHealth>() != null || target.GetComponent<Treasure>() != null)
                continue;

            PolygonCollider2D polygonCollider = target.GetComponent<PolygonCollider2D>();
            BoxCollider2D boxCollider = target.GetComponent<BoxCollider2D>();

            if (renderer.sprite != obstacleSprite)
            {
                renderer.sprite = obstacleSprite;
                renderer.color = Color.white;
            }
            float obstacleSize = ObstacleTargetSize * GetStableObstacleSizeMultiplier(target.transform.position);
            FitSpriteToTargetSize(renderer, obstacleSize);

            if (polygonCollider == null)
            {
                polygonCollider = target.AddComponent<PolygonCollider2D>();
            }

            polygonCollider.isTrigger = false;
            polygonCollider.autoTiling = false;

            if (boxCollider != null)
            {
                boxCollider.enabled = false;
            }
        }
    }

    void ApplyExtractionZoneSprites()
    {
        if (extractionSprite == null)
            return;

        ExtractionZone[] zones = FindObjectsByType<ExtractionZone>(FindObjectsSortMode.None);
        foreach (ExtractionZone zone in zones)
        {
            if (zone == null)
                continue;

            SpriteRenderer renderer = zone.GetComponent<SpriteRenderer>();
            if (renderer == null)
                continue;

            CircleCollider2D triggerCollider = zone.GetComponent<CircleCollider2D>();
            float triggerWorldRadius = GetWorldCircleRadius(triggerCollider);

            if (renderer.sprite != extractionSprite)
            {
                renderer.sprite = extractionSprite;
            }
            FitSpriteToTargetSize(renderer, ExtractionTargetSize);
            SetWorldCircleRadius(triggerCollider, triggerWorldRadius);
        }
    }

    void FitSpriteToTargetSize(SpriteRenderer renderer, float targetMaxWorldSize)
    {
        if (renderer == null || renderer.sprite == null)
            return;

        float maxDimension = Mathf.Max(renderer.sprite.bounds.size.x, renderer.sprite.bounds.size.y);
        if (maxDimension <= 0f)
            return;

        float scale = targetMaxWorldSize / maxDimension;
        renderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    float GetStableObstacleSizeMultiplier(Vector3 position)
    {
        float sampleX = Mathf.Round(position.x * 100f) * 0.0137f + 17.3f;
        float sampleY = Mathf.Round(position.y * 100f) * 0.0191f + 29.7f;
        float noise = Mathf.PerlinNoise(sampleX, sampleY);
        return Mathf.Lerp(0.5f, 1.5f, noise);
    }

    Vector2 GetWorldBoxSize(BoxCollider2D collider2D)
    {
        if (collider2D == null)
            return Vector2.zero;

        Vector3 scale = collider2D.transform.lossyScale;
        return new Vector2(
            Mathf.Abs(collider2D.size.x * scale.x),
            Mathf.Abs(collider2D.size.y * scale.y));
    }

    void SetWorldBoxSize(BoxCollider2D collider2D, Vector2 worldSize)
    {
        if (collider2D == null || worldSize == Vector2.zero)
            return;

        Vector3 scale = collider2D.transform.lossyScale;
        float safeX = Mathf.Abs(scale.x) > 0.0001f ? Mathf.Abs(scale.x) : 1f;
        float safeY = Mathf.Abs(scale.y) > 0.0001f ? Mathf.Abs(scale.y) : 1f;

        collider2D.size = new Vector2(worldSize.x / safeX, worldSize.y / safeY);
    }

    float GetWorldCircleRadius(CircleCollider2D collider2D)
    {
        if (collider2D == null)
            return 0f;

        Vector3 scale = collider2D.transform.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        return collider2D.radius * maxScale;
    }

    void SetWorldCircleRadius(CircleCollider2D collider2D, float worldRadius)
    {
        if (collider2D == null || worldRadius <= 0f)
            return;

        Vector3 scale = collider2D.transform.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        if (maxScale <= 0.0001f)
            maxScale = 1f;

        collider2D.radius = worldRadius / maxScale;
    }

    Sprite LoadSpriteFromProjectOrResources(string projectFileName, string resourcesPath)
    {
        string filePath = Path.Combine(Application.dataPath, projectFileName);
        if (File.Exists(filePath))
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(bytes, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            float pixelsPerUnit = Mathf.Max(texture.width, texture.height);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
        }

        return Resources.Load<Sprite>(resourcesPath);
    }
}
