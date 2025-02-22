﻿using UnityEditor;
using UnityEngine;


namespace AsepriteImporter
{
    public enum MirrorOption
    {
        None,
        X,
        Y
    }

    public enum TransparencyMode
    {
        Disabled,
        Alpha,
        Mask
    }

    [System.Serializable]
    public class AseFileTextureSettings
    {
        [SerializeField] public TextureImporterType textureType = TextureImporterType.Sprite;
        [SerializeField] public int pixelsPerUnit = 32;
        [SerializeField] public MirrorOption mirror = MirrorOption.None;
        [SerializeField] public TransparencyMode transparencyMode = TransparencyMode.Disabled;
        [SerializeField] public Color transparentColor = Color.magenta;
        [SerializeField] public SpriteMeshType meshType = SpriteMeshType.Tight;
        [Range(0, 32)] [SerializeField] public uint extrudeEdges = 1;
        [SerializeField] public Vector2 spritePivot = new Vector2(0.5f, 0.5f);
        [SerializeField] public bool generatePhysics = true;
        [SerializeField] public bool generateAnimations = true;

        [SerializeField] public TextureWrapMode wrapMode = TextureWrapMode.Clamp;
        [SerializeField] public FilterMode filterMode = FilterMode.Point;

        [SerializeField] public Vector2Int tileSize = new Vector2Int(32, 32);
        [SerializeField] public Vector2Int tilePadding = new Vector2Int(0, 0);
        [SerializeField] public Vector2Int tileOffset = new Vector2Int(0, 0);
    }
}
