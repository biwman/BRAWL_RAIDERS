using Photon.Pun;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class GameVisualTheme : MonoBehaviour
{
    const float PlayerTargetSize = 1.04f;
    const float AstronautTargetSize = 0.56f;
    const float TreasureTargetSize = 1.5f;
    const float TreasureColliderSizeMultiplier = 0.9f;
    const float PlayerBodyColliderWidthFactor = 0.46f;
    const float PlayerBodyColliderHeightFactor = 0.62f;
    const float PlayerPickupRadiusFactor = 0.8f;
    const float ObstacleTargetSize = 3.0f;
    const float ExtractionTargetSize = 4.3f;
    const float BackgroundTileWorldSize = 8f;
    const float RefreshInterval = 0.75f;

    static GameVisualTheme instance;

    Sprite[] shipSprites;
    Sprite[] wreckSprites;
    Sprite enemyBotSprite;
    Sprite astronautSprite;
    Sprite treasureSprite;
    Sprite goldTreasureSprite;
    Sprite rareTreasureSprite;
    Sprite[] obstacleSprites;
    Sprite extractionSprite;
    Sprite backgroundSprite;
    float nextRefreshTime;
#if UNITY_EDITOR
    double nextEditorRefreshTime;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        EnsureInstance();
    }

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    static void BootstrapInEditor()
    {
        EditorApplication.delayCall += EnsureEditorInstance;
    }
#endif

    static void EnsureInstance()
    {
        if (instance != null)
            return;

        GameObject root = GameObject.Find("GameVisualTheme");
        if (root == null)
        {
            root = new GameObject("GameVisualTheme");
#if UNITY_EDITOR
            if (!Application.isPlaying)
                root.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor;
#endif
        }

        instance = root.GetComponent<GameVisualTheme>();
        if (instance == null)
            instance = root.AddComponent<GameVisualTheme>();

        if (Application.isPlaying)
            DontDestroyOnLoad(root);
    }

#if UNITY_EDITOR
    static void EnsureEditorInstance()
    {
        if (Application.isPlaying)
            return;

        EnsureInstance();
        if (instance != null)
            instance.ApplyThemeInEditor();
    }
#endif

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        LoadAssets();
    }

    void OnEnable()
    {
        LoadAssets();
        nextRefreshTime = 0f;
        ApplyTheme();
#if UNITY_EDITOR
        if (!Application.isPlaying)
            EditorApplication.hierarchyChanged += OnEditorHierarchyChanged;
#endif
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
#if UNITY_EDITOR
        if (!Application.isPlaying)
            EditorApplication.hierarchyChanged -= OnEditorHierarchyChanged;
#endif
        if (instance == this)
            instance = null;
    }

    void Start()
    {
        ApplyTheme();
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (EditorApplication.timeSinceStartup < nextEditorRefreshTime)
                return;

            nextEditorRefreshTime = EditorApplication.timeSinceStartup + RefreshInterval;
            ApplyThemeInEditor();
            return;
        }
#endif

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

#if UNITY_EDITOR
    void OnEditorHierarchyChanged()
    {
        if (Application.isPlaying)
            return;

        ApplyThemeInEditor();
    }

    void ApplyThemeInEditor()
    {
        LoadAssets();
        ApplyTheme();
    }
#endif

    void LoadAssets()
    {
        shipSprites = new[]
        {
            LoadSpriteFromProjectOrResources("ship1.png", "Visuals/Ships/ship1_resource"),
            LoadSpriteFromProjectOrResources("ship2.png", "Visuals/Ships/ship2_resource"),
            LoadSpriteFromProjectOrResources("ship3.png", "Visuals/Ships/ship3_resource"),
            LoadSpriteFromProjectOrResources("ship4.png", "ship4_resource")
        };
        wreckSprites = new[]
        {
            null,
            LoadSpriteFromProjectOrResources("wrak2.png", "wrak2_resource"),
            null,
            null
        };
        enemyBotSprite = LoadSpriteFromProjectOrResources("droid1.png", "droid1_resource");
        astronautSprite = LoadSpriteFromProjectOrResources("kosmonauta.png", "kosmonauta_resource");

        treasureSprite = LoadSpriteFromProjectOrResources("asteroida_treasure.png", "Visuals/Treasures/asteroid_treasure_resource");
        goldTreasureSprite = LoadSpriteFromProjectOrResources("asteroida_zloto_clean.png", "asteroida_zloto_clean_resource");
        rareTreasureSprite = LoadSpriteFromProjectOrResources("asteroida_rare_clean.png", "asteroida_rare_clean_resource");
        obstacleSprites = new[]
        {
            LoadObstacleSprite("asteroida_1_clean.png", "asteroida_1.png"),
            LoadObstacleSprite("asteroida_2_clean.png", "asteroida_2.png"),
            LoadObstacleSprite("asteroida_3_clean.png", "asteroida_3.png"),
            LoadObstacleSprite("asteroida_podluzna_1_clean.png", "asteroida_podluzna_1.png"),
            LoadObstacleSprite("asteroida_podluzna_2_clean.png", "asteroida_podluzna_2.png")
        };
        extractionSprite = LoadSpriteFromProjectOrResources("baza1.png", "Visuals/Bases/base1_resource");
        backgroundSprite = Resources.Load<Sprite>("Visuals/Backgrounds/background5_resource");
        if (backgroundSprite == null)
        {
            backgroundSprite = LoadSpriteFromProjectOrResources("tło5.png", "Visuals/Backgrounds/background5_resource");
        }
    }

    void ApplyTheme()
    {
        ApplyGroundBackground();
        ApplyMapBounds();
        ApplyPlayerSprites();
        ApplyTreasureSprites();
        ApplyObstacleSprites();
        ApplyExtractionZoneSprites();
    }

    void ApplyGroundBackground()
    {
        if (backgroundSprite == null)
            return;

        Vector2 mapSize = RoomSettings.GetMapDimensions();

        GameObject ground = GameObject.Find("Ground");
        if (ground == null)
            return;

        SpriteRenderer renderer = ground.GetComponent<SpriteRenderer>();
        if (renderer == null)
            return;

        renderer.sprite = backgroundSprite;
        renderer.color = Color.white;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.tileMode = SpriteTileMode.Continuous;
        renderer.size = mapSize;
    }

    void ApplyMapBounds()
    {
        Vector2 mapSize = RoomSettings.GetMapDimensions();
        UpdateWall("WallTop", new Vector2(0f, mapSize.y / 2f), new Vector2(mapSize.x, 1f), true);
        UpdateWall("WallBottom", new Vector2(0f, -mapSize.y / 2f), new Vector2(mapSize.x, 1f), true);
        UpdateWall("WallLeft", new Vector2(-mapSize.x / 2f, 0f), new Vector2(1f, mapSize.y), false);
        UpdateWall("WallRight", new Vector2(mapSize.x / 2f, 0f), new Vector2(1f, mapSize.y), false);
    }

    void UpdateWall(string wallName, Vector2 position, Vector2 size, bool horizontal)
    {
        GameObject wall = GameObject.Find(wallName);
        if (wall == null)
            return;

        wall.transform.position = new Vector3(position.x, position.y, wall.transform.position.z);

        SpriteRenderer renderer = wall.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
        }

        BoxCollider2D collider = wall.GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            collider.offset = Vector2.zero;
            collider.size = size;
            collider.sharedMaterial = MovingSpaceObject.GetSharedBouncyMaterial();
        }

        wall.transform.localScale = horizontal
            ? new Vector3(1f, wall.transform.localScale.y, 1f)
            : new Vector3(wall.transform.localScale.x, 1f, 1f);
    }

    void ApplyPlayerSprites()
    {
        if (shipSprites == null || shipSprites.Length == 0)
            return;

        PlayerHealth[] players = FindObjectsByType<PlayerHealth>(FindObjectsInactive.Exclude);
        foreach (PlayerHealth player in players)
        {
            if (player == null)
                continue;

            PhotonView view = player.GetComponent<PhotonView>();
            SpriteRenderer renderer = player.GetComponent<SpriteRenderer>();

            if (view == null || renderer == null || view.Owner == null)
                continue;

            bool isEnemyBot = player.IsBotControlled || EnemyBot.IsBotInstantiationData(view.InstantiationData);
            bool isAstronaut = player.GetComponent<AstronautSurvivor>() != null || AstronautSurvivor.IsAstronautInstantiationData(view.InstantiationData);

            Sprite sprite;
            if (player.IsWreck)
            {
                ShipWreck wreck = player.GetComponent<ShipWreck>();
                int wreckSkinIndex = wreck != null ? wreck.SourceShipSkinIndex : RoomSettings.GetPlayerShipSkin(view.Owner, 0);
                sprite = GetMappedWreckSprite(wreckSkinIndex, renderer.sprite);
            }
            else if (isAstronaut && astronautSprite != null)
            {
                sprite = astronautSprite;
            }
            else if (isEnemyBot && enemyBotSprite != null)
            {
                sprite = enemyBotSprite;
            }
            else
            {
                int fallbackIndex = Mathf.Abs(view.Owner.ActorNumber - 1) % shipSprites.Length;
                int shipIndex = RoomSettings.GetPlayerShipSkin(view.Owner, fallbackIndex) % shipSprites.Length;
                sprite = shipSprites[shipIndex];
            }

            if (sprite == null)
                continue;

            BoxCollider2D bodyCollider = player.GetComponent<BoxCollider2D>();
            CircleCollider2D pickupCollider = player.GetComponent<CircleCollider2D>();

            if (renderer.sprite != sprite)
            {
                renderer.sprite = sprite;
            }
            renderer.color = Color.white;
            FitSpriteToTargetSize(renderer, isAstronaut ? AstronautTargetSize : PlayerTargetSize);

            Vector2 spriteWorldSize = GetSpriteWorldSize(renderer);
            SetWorldBoxSize(bodyCollider, new Vector2(
                spriteWorldSize.x * PlayerBodyColliderWidthFactor,
                spriteWorldSize.y * PlayerBodyColliderHeightFactor));
            SetWorldCircleRadius(pickupCollider, Mathf.Max(spriteWorldSize.x, spriteWorldSize.y) * PlayerPickupRadiusFactor);
        }
    }

    void ApplyTreasureSprites()
    {
        if (treasureSprite == null)
            return;

        Treasure[] treasures = FindObjectsByType<Treasure>(FindObjectsInactive.Exclude);
        foreach (Treasure treasure in treasures)
        {
            if (treasure == null)
                continue;

            SpriteRenderer renderer = treasure.GetComponent<SpriteRenderer>();
            if (renderer == null)
                continue;

            BoxCollider2D triggerCollider = treasure.GetComponent<BoxCollider2D>();

            Sprite desiredSprite = GetTreasureSprite(treasure);
            if (desiredSprite == null)
                desiredSprite = treasureSprite;

            if (renderer.sprite != desiredSprite)
            {
                renderer.sprite = desiredSprite;
            }
            FitSpriteToTargetSize(renderer, TreasureTargetSize);

            if (triggerCollider != null)
            {
                Vector2 spriteWorldSize = GetSpriteWorldSize(renderer);
                Vector2 colliderSize = spriteWorldSize * treasure.GetColliderSizeMultiplier();
                triggerCollider.isTrigger = false;
                SetWorldBoxSize(triggerCollider, colliderSize);
            }
        }
    }

    Sprite GetTreasureSprite(Treasure treasure)
    {
        if (treasure == null)
            return treasureSprite;

        switch (treasure.itemId)
        {
            case InventoryItemCatalog.AsteroidGoldId:
                return goldTreasureSprite != null ? goldTreasureSprite : treasureSprite;
            case InventoryItemCatalog.AsteroidRareId:
                return rareTreasureSprite != null ? rareTreasureSprite : treasureSprite;
            default:
                return treasureSprite;
        }
    }

    void ApplyObstacleSprites()
    {
        if (obstacleSprites == null || obstacleSprites.Length == 0)
            return;

        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude);
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

            Sprite obstacleSprite = GetStableObstacleSprite(target);
            if (obstacleSprite == null)
                continue;

            if (renderer.sprite != obstacleSprite)
            {
                renderer.sprite = obstacleSprite;
                renderer.color = Color.white;
            }
            float obstacleSize = ObstacleTargetSize * GetStableObstacleSizeMultiplier(target);
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

        ExtractionZone[] zones = FindObjectsByType<ExtractionZone>(FindObjectsInactive.Exclude);
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

    Vector2 GetSpriteWorldSize(SpriteRenderer renderer)
    {
        if (renderer == null || renderer.sprite == null)
            return Vector2.zero;

        Bounds bounds = renderer.bounds;
        return new Vector2(bounds.size.x, bounds.size.y);
    }

    float GetStableObstacleSizeMultiplier(GameObject target)
    {
        if (target == null)
            return 1f;

        MovingSpaceObject movingObject = target.GetComponent<MovingSpaceObject>();
        string stableKey = movingObject != null && !string.IsNullOrWhiteSpace(movingObject.StableId)
            ? movingObject.StableId
            : target.name;

        int hash = stableKey.GetHashCode();
        float sampleX = Mathf.Abs(hash * 0.00013f) + 17.3f;
        float sampleY = Mathf.Abs(hash * 0.00029f) + 29.7f;
        float noise = Mathf.PerlinNoise(sampleX, sampleY);
        return Mathf.Lerp(0.5f, 1.5f, noise);
    }

    Sprite GetStableObstacleSprite(GameObject target)
    {
        if (target == null || obstacleSprites == null || obstacleSprites.Length == 0)
            return null;

        MovingSpaceObject movingObject = target.GetComponent<MovingSpaceObject>();
        string stableKey = movingObject != null && !string.IsNullOrWhiteSpace(movingObject.StableId)
            ? movingObject.StableId
            : target.name;

        int hash = stableKey.GetHashCode();
        int index = Mathf.Abs(hash) % obstacleSprites.Length;
        return obstacleSprites[index];
    }

    Sprite GetMappedWreckSprite(int shipSkinIndex, Sprite fallback)
    {
        if (wreckSprites != null && shipSkinIndex >= 0 && shipSkinIndex < wreckSprites.Length && wreckSprites[shipSkinIndex] != null)
            return wreckSprites[shipSkinIndex];

        return fallback;
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
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Trilinear;

            float pixelsPerUnit = Mathf.Max(1f, Mathf.Max(texture.width, texture.height) / BackgroundTileWorldSize);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
        }

        return Resources.Load<Sprite>(resourcesPath);
    }

    Sprite LoadObstacleSprite(string cleanedProjectFileName, string fallbackProjectFileName)
    {
        Sprite sprite = LoadSpriteFromProjectOrResources(cleanedProjectFileName, "Visuals/Obstacles/asteroid_obstacle_resource");
        if (sprite != null)
            return sprite;

        return LoadSpriteFromProjectOrResources(fallbackProjectFileName, "Visuals/Obstacles/asteroid_obstacle_resource");
    }
}

