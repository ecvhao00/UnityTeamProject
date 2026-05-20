#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class SquareSlicedTileAssetBuilder
{
    private const int SliceSize = 128;
    private const string SourceFolder = "Assets/Sprites/SquareSliced";
    private const string TileRootFolder = "Assets/Tiles/SquareSliced";
    private const string RequestFilePath = "Assets/Editor/SquareSlicedTileAssetBuilder.request";

    [InitializeOnLoadMethod]
    private static void BuildWhenRequested()
    {
        if (!File.Exists(GetRequestFileSystemPath())) return;

        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(GetRequestFileSystemPath())) return;

            try
            {
                DeleteBuildRequest();
                Build();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        };
    }

    [MenuItem("Tools/G4/Build Square Sliced Tiles")]
    public static void Build()
    {
        EnsureFolder("Assets", "Tiles");
        EnsureFolder("Assets/Tiles", "SquareSliced");

        string[] texturePaths = AssetDatabase.FindAssets("t:Texture2D", new[] { SourceFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith("_SquareTiles.png", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        int spriteCount = 0;
        int tileCount = 0;

        foreach (string texturePath in texturePaths)
        {
            spriteCount += SliceTexture(texturePath);
            tileCount += CreateTiles(texturePath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Built square sliced tiles from {texturePaths.Length} sheets: {spriteCount} sprites, {tileCount} tile assets.");
    }

    private static int SliceTexture(string texturePath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null) return 0;

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null) return 0;

        int columns = Mathf.Max(1, texture.width / SliceSize);
        int rows = Mathf.Max(1, texture.height / SliceSize);
        string baseName = Path.GetFileNameWithoutExtension(texturePath);
        List<SpriteMetaData> sprites = new();

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                Rect rect = new(
                    column * SliceSize,
                    texture.height - ((row + 1) * SliceSize),
                    SliceSize,
                    SliceSize
                );

                sprites.Add(new SpriteMetaData
                {
                    name = $"{baseName}_{row:D2}_{column:D2}",
                    rect = rect,
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                });
            }
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = SliceSize;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.spritesheet = sprites.ToArray();
        importer.SaveAndReimport();

        return sprites.Count;
    }

    private static int CreateTiles(string texturePath)
    {
        string baseName = Path.GetFileNameWithoutExtension(texturePath);
        string targetFolder = $"{TileRootFolder}/{baseName}";
        EnsureAssetFolder(targetFolder);

        bool solidByDefault = baseName.StartsWith("Footstool", StringComparison.OrdinalIgnoreCase);
        Tile.ColliderType colliderType = solidByDefault ? Tile.ColliderType.Sprite : Tile.ColliderType.None;

        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        int count = 0;
        foreach (Sprite sprite in sprites)
        {
            string tilePath = $"{targetFolder}/{sprite.name}.asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }

            tile.sprite = sprite;
            tile.colliderType = colliderType;
            EditorUtility.SetDirty(tile);
            count++;
        }

        return count;
    }

    private static string GetRequestFileSystemPath()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", RequestFilePath));
    }

    private static void DeleteBuildRequest()
    {
        string requestPath = GetRequestFileSystemPath();
        if (File.Exists(requestPath))
        {
            File.Delete(requestPath);
        }

        string metaPath = requestPath + ".meta";
        if (File.Exists(metaPath))
        {
            File.Delete(metaPath);
        }
    }

    private static void EnsureAssetFolder(string assetFolder)
    {
        string[] parts = assetFolder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
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


