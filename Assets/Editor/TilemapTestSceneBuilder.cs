#if UNITY_EDITOR
using System;
using TarodevController;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class TilemapTestSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/TilemapTestScene.unity";
    private const string TileFolderPath = "Assets/Tiles";
    private const string SolidTilePath = TileFolderPath + "/PrototypeFootstoolTile.asset";
    private const string AccentTilePath = TileFolderPath + "/PrototypeFootstoolAltTile.asset";
    private const string PlayerStatsPath = "Assets/Scenes/Scripts/PlayerStats.asset";
    private const string PhysicsMaterialPath = "Assets/Physics/New Physics Material 2D.physicsMaterial2D";
    [MenuItem("Tools/G4/Build Tilemap Test Scene")]
    public static void Build()
    {
        EnsureFolder("Assets", "Editor");
        EnsureFolder("Assets", "Tiles");

        Tile solidTile = EnsureTile(
            SolidTilePath,
            LoadSprite("Assets/Sprites/Footstool_1.png", "Footstool_1_0"),
            new Color(0.88f, 0.78f, 0.52f, 1f)
        );

        Tile accentTile = EnsureTile(
            AccentTilePath,
            LoadSprite("Assets/Sprites/Footstool_2.png", "Footstool_2_0"),
            new Color(0.72f, 0.84f, 0.68f, 1f)
        );

        bool batchMode = Application.isBatchMode;
        Scene previousActiveScene = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, batchMode ? NewSceneMode.Single : NewSceneMode.Additive);
        scene.name = "TilemapTestScene";
        SceneManager.SetActiveScene(scene);

        try
        {
            CreateGlobalLight();
            GameObject player = CreatePlayer(new Vector2(0f, -6.3f));
            CreateCamera(player.transform);
            CreateSolidTilemap(solidTile, accentTile);
            CreatePrefabObstacles();

            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        finally
        {
            if (!batchMode && previousActiveScene.IsValid() && previousActiveScene.isLoaded)
            {
                SceneManager.SetActiveScene(previousActiveScene);
            }

            if (!batchMode && scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        AssetDatabase.SaveAssets();

        Debug.Log($"Built tilemap test scene at {ScenePath}");
    }

    private static void CreateSolidTilemap(TileBase solidTile, TileBase accentTile)
    {
        GameObject gridObject = new("Tilemap Prototype Grid");
        gridObject.transform.position = new Vector3(0f, -7.36f, 0f);

        Grid grid = gridObject.AddComponent<Grid>();
        grid.cellLayout = GridLayout.CellLayout.Rectangle;
        grid.cellSize = Vector3.one;

        GameObject terrainObject = new("Solid Terrain Tilemap");
        terrainObject.transform.SetParent(gridObject.transform, false);

        Tilemap tilemap = terrainObject.AddComponent<Tilemap>();
        TilemapRenderer renderer = terrainObject.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = 0;

        terrainObject.AddComponent<TilemapCollider2D>();

        PaintRect(tilemap, solidTile, accentTile, -8, 15, -1, 0);
        PaintRect(tilemap, solidTile, accentTile, 20, 37, -1, 0);
        PaintRect(tilemap, solidTile, accentTile, 42, 59, -1, 0);
        PaintRect(tilemap, solidTile, accentTile, 65, 82, -1, 0);

        PaintRect(tilemap, solidTile, accentTile, 7, 12, 3, 3);
        PaintRect(tilemap, solidTile, accentTile, 17, 23, 5, 5);
        PaintRect(tilemap, solidTile, accentTile, 31, 36, 3, 3);
        PaintRect(tilemap, solidTile, accentTile, 49, 53, 6, 6);
        PaintRect(tilemap, solidTile, accentTile, 68, 74, 3, 3);

        PaintRect(tilemap, solidTile, accentTile, 46, 46, 1, 8);
        PaintRect(tilemap, solidTile, accentTile, 55, 55, 1, 8);
        PaintRect(tilemap, solidTile, accentTile, 55, 62, 8, 8);

        tilemap.CompressBounds();
    }

    private static GameObject CreatePlayer(Vector2 position)
    {
        GameObject player = new("Player");
        player.transform.position = new Vector3(position.x, position.y, 0f);
        player.layer = LayerMask.NameToLayer("Player");
        player.tag = "Player";

        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        CapsuleCollider2D capsule = player.AddComponent<CapsuleCollider2D>();
        capsule.direction = CapsuleDirection2D.Vertical;
        capsule.offset = new Vector2(-0.038600042f, 0.3008738f);
        capsule.size = new Vector2(1.0094012f, 0.7225979f);

        PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(PhysicsMaterialPath);
        if (material != null)
        {
            capsule.sharedMaterial = material;
        }

        PlayerController controller = player.AddComponent<PlayerController>();
        PlayerDeath death = player.AddComponent<PlayerDeath>();

        ScriptableStats stats = AssetDatabase.LoadAssetAtPath<ScriptableStats>(PlayerStatsPath);
        SetObjectReference(controller, "_stats", stats);

        GameObject visual = new("PlayerVisual");
        visual.layer = player.layer;
        visual.transform.SetParent(player.transform, false);
        visual.transform.localPosition = new Vector3(-0.22f, 0.35f, 0f);
        visual.transform.localScale = new Vector3(0.25f, 0.25f, 0.5f);

        SpriteRenderer spriteRenderer = visual.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 2;

        PlayerAnimator animator = visual.AddComponent<PlayerAnimator>();
        ConfigurePlayerAnimator(animator, spriteRenderer);

        SerializedObject deathObject = new(death);
        deathObject.FindProperty("playerController").objectReferenceValue = controller;
        deathObject.FindProperty("playerAnimator").objectReferenceValue = animator;
        deathObject.FindProperty("useFallDeath").boolValue = true;
        deathObject.FindProperty("fallDeathY").floatValue = -17f;
        deathObject.FindProperty("initialRespawnPoint").vector2Value = position;
        deathObject.ApplyModifiedPropertiesWithoutUndo();

        return player;
    }

    private static void ConfigurePlayerAnimator(PlayerAnimator animator, SpriteRenderer spriteRenderer)
    {
        Sprite[] runSprites = LoadSprites(
            "Assets/Sprites/Cat_Running_remix.png",
            "cat_remix_0",
            "cat_remix_1",
            "cat_remix_2",
            "cat_remix_3",
            "cat_remix_4"
        );

        Sprite[] jumpSprites = LoadSprites(
            "Assets/Sprites/Cat_Jump_remix.png",
            "Cat_Jump_0",
            "Cat_Jump_1",
            "Cat_Jump_2",
            "Cat_Jump_3",
            "Cat_Jump_4",
            "Cat_Jump_5"
        );

        Sprite damagedSprite = LoadSprite("Assets/Sprites/Cat_Damaged.png", "Cat_Damaged_0");

        SerializedObject animatorObject = new(animator);
        animatorObject.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
        animatorObject.FindProperty("idleSprite").objectReferenceValue = runSprites.Length > 4 ? runSprites[4] : null;
        SetArray(animatorObject.FindProperty("runSprites"), runSprites);
        SetArray(animatorObject.FindProperty("jumpSprites"), jumpSprites);
        animatorObject.FindProperty("damagedSprite").objectReferenceValue = damagedSprite;
        animatorObject.FindProperty("runFramesPerSecond").floatValue = 10f;
        animatorObject.FindProperty("jumpFramesPerSecond").floatValue = 15f;
        animatorObject.ApplyModifiedPropertiesWithoutUndo();

        spriteRenderer.sprite = runSprites.Length > 4 ? runSprites[4] : null;
    }

    private static void CreateCamera(Transform player)
    {
        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.46226418f, 0.40812457f, 0.377225f, 0f);
        camera.orthographic = true;
        camera.orthographicSize = 10f;

        cameraObject.AddComponent<AudioListener>();

        CameraMove cameraMove = cameraObject.AddComponent<CameraMove>();
        SerializedObject cameraMoveObject = new(cameraMove);
        cameraMoveObject.FindProperty("player").objectReferenceValue = player;
        cameraMoveObject.FindProperty("smoothSpeed").floatValue = 10f;
        cameraMoveObject.FindProperty("minCameraY").floatValue = 0f;
        cameraMoveObject.FindProperty("cameraZ").floatValue = -10f;
        cameraMoveObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateGlobalLight()
    {
        GameObject lightObject = new("Global Light 2D");
        Light2D light = lightObject.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;
    }

    private static void CreatePrefabObstacles()
    {
        GameObject root = new("Dynamic Prefab Obstacles");

        SpawnPrefab("Double Jump Unlock", "Assets/Prefabs/DoubleJumpUnlockPickup.prefab", new Vector2(10f, -5.75f), root.transform);
        SpawnPrefab("Broken Wire Timing Gate", "Assets/Prefabs/BrokenWireHazard.prefab", new Vector2(18f, -5.85f), root.transform);
        SpawnPrefab("Spike Hazard Sample", "Assets/Prefabs/SpikeHazard.prefab", new Vector2(24f, -5.95f), root.transform);
        SpawnPrefab("Falling Sign Trap Sample", "Assets/Prefabs/FallingSignTrap.prefab", new Vector2(28f, -6.35f), root.transform);
        SpawnPrefab("Checkpoint Sample", "Assets/Prefabs/SavePoint.prefab", new Vector2(32f, -5.75f), root.transform);
        SpawnPrefab("Wall Jump Unlock", "Assets/Prefabs/WallJumpUnlockPickup.prefab", new Vector2(39f, -5.75f), root.transform);
        SpawnPrefab("Ending Trigger Sample", "Assets/Prefabs/EndingSceneTrigger.prefab", new Vector2(78f, -5.75f), root.transform);
    }

    private static void SpawnPrefab(string name, string path, Vector2 position, Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning($"Missing prefab: {path}");
            return;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null) return;

        instance.name = name;
        instance.transform.position = new Vector3(position.x, position.y, 0f);
        instance.transform.SetParent(parent, true);
    }

    private static Tile EnsureTile(string path, Sprite sprite, Color color)
    {
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }

        tile.sprite = sprite;
        tile.color = color;
        tile.colliderType = Tile.ColliderType.Grid;

        if (sprite != null)
        {
            Vector3 boundsSize = sprite.bounds.size;
            float scaleX = boundsSize.x > 0f ? 1f / boundsSize.x : 1f;
            float scaleY = boundsSize.y > 0f ? 1f / boundsSize.y : 1f;
            tile.transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scaleX, scaleY, 1f));
        }

        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static void PaintRect(Tilemap tilemap, TileBase solidTile, TileBase accentTile, int minX, int maxX, int minY, int maxY)
    {
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                TileBase tile = ((x + y) & 1) == 0 ? solidTile : accentTile;
                tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }
    }

    private static Sprite LoadSprite(string path, string spriteName)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (UnityEngine.Object asset in assets)
        {
            if (asset is Sprite sprite && sprite.name == spriteName)
            {
                return sprite;
            }
        }

        throw new InvalidOperationException($"Could not find sprite {spriteName} at {path}");
    }

    private static Sprite[] LoadSprites(string path, params string[] spriteNames)
    {
        Sprite[] sprites = new Sprite[spriteNames.Length];
        for (int i = 0; i < spriteNames.Length; i++)
        {
            sprites[i] = LoadSprite(path, spriteNames[i]);
        }

        return sprites;
    }

    private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        SerializedObject serializedObject = new(target);
        serializedObject.FindProperty(propertyName).objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray(SerializedProperty property, Sprite[] sprites)
    {
        property.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }
    }


    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
#endif





