#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class SquareSlicedTilePaletteBuilder
{
    private const string TileRootFolder = "Assets/Tiles/SquareSliced";
    private const string PaletteFolder = "Assets/TilePalettes/SquareSliced";
    private const string RequestFilePath = "Assets/Editor/SquareSlicedTilePaletteBuilder.request";
    private const int GroupSpacingRows = 1;

    private static readonly string[] AllGroups =
    {
        "Footstool_1_SquareTiles",
        "Footstool_2_SquareTiles",
        "FootStool_3_SquareTiles",
        "Broken_Window_4_SquareTiles",
        "ON_SquareTiles",
        "OFF_SquareTiles",
        "Trashcan_Pla_SquareTiles",
        "Trashcan_Can_SquareTiles",
        "Trashcan_Paper_SquareTiles"
    };

    private static readonly PaletteDefinition[] Palettes =
    {
        new("SquareSliced_All_Organized", AllGroups),
        new("SquareSliced_Platforms", new[]
        {
            "Footstool_1_SquareTiles",
            "Footstool_2_SquareTiles",
            "FootStool_3_SquareTiles"
        }),
        new("SquareSliced_Windows", new[]
        {
            "Broken_Window_4_SquareTiles"
        }),
        new("SquareSliced_Electric", new[]
        {
            "ON_SquareTiles",
            "OFF_SquareTiles"
        }),
        new("SquareSliced_Trashcans", new[]
        {
            "Trashcan_Pla_SquareTiles",
            "Trashcan_Can_SquareTiles",
            "Trashcan_Paper_SquareTiles"
        })
    };

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

    [MenuItem("Tools/G4/Build Square Sliced Tile Palettes")]
    public static void Build()
    {
        EnsureAssetFolder(PaletteFolder);

        int builtPaletteCount = 0;
        int placedTileCount = 0;
        foreach (PaletteDefinition palette in Palettes)
        {
            int placedTiles = BuildPalette(palette);
            if (placedTiles <= 0) continue;

            builtPaletteCount++;
            placedTileCount += placedTiles;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Built {builtPaletteCount} square sliced tile palettes with {placedTileCount} total placed tile entries in {PaletteFolder}.");
    }

    private static int BuildPalette(PaletteDefinition palette)
    {
        string prefabPath = $"{PaletteFolder}/{palette.Name}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            AssetDatabase.DeleteAsset(prefabPath);
        }

        GameObject paletteAsset = GridPaletteUtility.CreateNewPalette(
            PaletteFolder,
            palette.Name,
            GridLayout.CellLayout.Rectangle,
            GridPalette.CellSizing.Manual,
            Vector3.one,
            GridLayout.CellSwizzle.XYZ
        );

        if (paletteAsset == null)
        {
            Debug.LogError($"Failed to create tile palette asset: {prefabPath}");
            return 0;
        }

        prefabPath = AssetDatabase.GetAssetPath(paletteAsset);
        GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Grid grid = contents.GetComponent<Grid>();
            if (grid != null)
            {
                grid.cellSize = Vector3.one;
                grid.cellLayout = GridLayout.CellLayout.Rectangle;
                grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;
            }

            Tilemap tilemap = contents.GetComponentInChildren<Tilemap>();
            if (tilemap == null)
            {
                Debug.LogError($"Palette prefab has no Tilemap: {prefabPath}");
                return 0;
            }

            tilemap.ClearAllTiles();

            int cursorRow = 0;
            int placedTileCount = 0;
            foreach (string groupName in palette.Groups)
            {
                List<TilePlacement> groupTiles = LoadGroupTiles(groupName);
                if (groupTiles.Count == 0) continue;

                int groupHeight = groupTiles.Max(tile => tile.Row) + 1;
                foreach (TilePlacement tile in groupTiles)
                {
                    Vector3Int position = new(tile.Column, -cursorRow - tile.Row, 0);
                    tilemap.SetTile(position, tile.Tile);
                    placedTileCount++;
                }

                cursorRow += groupHeight + GroupSpacingRows;
            }

            tilemap.CompressBounds();
            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            return placedTileCount;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    private static List<TilePlacement> LoadGroupTiles(string groupName)
    {
        string groupFolder = $"{TileRootFolder}/{groupName}";
        if (!AssetDatabase.IsValidFolder(groupFolder))
        {
            Debug.LogWarning($"Missing square sliced tile group folder: {groupFolder}");
            return new List<TilePlacement>();
        }

        return AssetDatabase.FindAssets("t:Tile", new[] { groupFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => ParseGridIndex(path).Row)
            .ThenBy(path => ParseGridIndex(path).Column)
            .Select(path =>
            {
                (int row, int column) = ParseGridIndex(path);
                TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
                return tile == null ? null : new TilePlacement(tile, row, column);
            })
            .Where(tile => tile != null)
            .ToList();
    }

    private static (int Row, int Column) ParseGridIndex(string assetPath)
    {
        string name = Path.GetFileNameWithoutExtension(assetPath);
        Match match = Regex.Match(name, @"_(\d+)_(\d+)$");
        if (!match.Success) return (0, 0);

        return (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
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

    private sealed class PaletteDefinition
    {
        public PaletteDefinition(string name, string[] groups)
        {
            Name = name;
            Groups = groups;
        }

        public string Name { get; }
        public string[] Groups { get; }
    }

    private sealed class TilePlacement
    {
        public TilePlacement(TileBase tile, int row, int column)
        {
            Tile = tile;
            Row = row;
            Column = column;
        }

        public TileBase Tile { get; }
        public int Row { get; }
        public int Column { get; }
    }
}
#endif
