using Aseprite.Chunks;
using Aseprite.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Aseprite
{

    // See file specs here: https://github.com/aseprite/aseprite/blob/master/docs/ase-file-specs.md

    public class AseFile
    {
        public Header Header { get; private set; }
        public List<Frame> Frames { get; private set; }

        private Dictionary<Type, Chunk> chunkCache = new Dictionary<Type, Chunk>();
        private Color32[] tempColorBuffer;

        public AseFile(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);
            byte[] header = reader.ReadBytes(128);

            Header = new Header(header);
            Frames = new List<Frame>();

            tempColorBuffer = new Color32[Header.Width * Header.Height];

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                Frames.Add(new Frame(this, reader));
            }
        }


        public List<T> GetChunks<T>() where T : Chunk
        {
            List<T> chunks = new List<T>();

            for (int i = 0; i < this.Frames.Count; i++)
            {
                List<T> cs = this.Frames[i].GetChunks<T>();

                chunks.AddRange(cs);
            }

            return chunks;
        }

        public T GetChunk<T>() where T : Chunk
        {
            if (!chunkCache.ContainsKey(typeof(T)))
            {
                for (int i = 0; i < this.Frames.Count; i++)
                {
                    List<T> cs = this.Frames[i].GetChunks<T>();

                    if (cs.Count > 0)
                    {
                        chunkCache.Add(typeof(T), cs[0]);
                        break;
                    }
                }
            }

            return (T)chunkCache[typeof(T)];
        }

        public Texture2D[] GetFrames()
        {
            List<Texture2D> frames = new List<Texture2D>();

            for (int i = 0; i < Frames.Count; i++)
            {
                frames.Add(GetFrame(i));
            }

            return frames.ToArray();
        }


        public Texture2D[] GetLayersAsFrames()
        {
            List<Texture2D> frames = new List<Texture2D>();
            List<LayerChunk> layers = GetChunks<LayerChunk>();

            for (int i = 0; i < layers.Count; i++)
            {
                List<Texture2D> layerFrames = GetLayerTexture(i, layers[i]);

                if (layerFrames.Count > 0)
                    frames.AddRange(layerFrames);
            }

            return frames.ToArray();
        }

        private LayerChunk GetParentLayer(LayerChunk layer)
        {
            if (layer.LayerChildLevel == 0)
                return null;

            int childLevel = layer.LayerChildLevel;

            List<LayerChunk> layers = GetChunks<LayerChunk>();
            int index = layers.IndexOf(layer);

            if (index < 0)
                return null;

            for (int i = index -1; i > 0; i--)
            {
                if (layers[i].LayerChildLevel == layer.LayerChildLevel - 1)
                    return layers[i];
            }

            return null;
        }

        public List<Texture2D> GetLayerTexture(int layerIndex, LayerChunk layer)
        {

            List<LayerChunk> layers = GetChunks<LayerChunk>();
            List<Texture2D> textures = new List<Texture2D>();

            for (int frameIndex = 0; frameIndex < Frames.Count; frameIndex++)
            {
                Frame frame = Frames[frameIndex];
                List<CelChunk> cels = frame.GetChunks<CelChunk>();

                for (int i = 0; i < cels.Count; i++)
                {
                    if (cels[i].LayerIndex != layerIndex)
                        continue;

                    LayerBlendMode blendMode = layer.BlendMode;
                    float opacity = Mathf.Min(layer.Opacity / 255f, cels[i].Opacity / 255f);

                    bool visibility = layer.Visible;

                    LayerChunk parent = GetParentLayer(layer);
                    while (parent != null)
                    {
                        visibility &= parent.Visible;
                        if (visibility == false)
                            break;

                        parent = GetParentLayer(parent);
                    }

                    if (visibility == false || layer.LayerType == LayerType.Group)
                        continue;

                    textures.Add(GetTextureFromCel(cels[i]));
                }
            }

            return textures;
        }

        public Texture2D GetFrame(int index)
        {
            Frame frame = Frames[index];

            Color32[] textureColors;
            Texture2D texture = Texture2DUtil.CreateTransparentTexture(Header.Width, Header.Height, out textureColors);
            
            List<LayerChunk> layers = GetChunks<LayerChunk>();
            List<CelChunk> cels = frame.GetChunks<CelChunk>();

            cels.Sort((ca, cb) => ca.LayerIndex.CompareTo(cb.LayerIndex));

            for (int i = 0; i < cels.Count; i++)
            {
                LayerChunk layer = layers[cels[i].LayerIndex];
                if (layer.LayerName.StartsWith("@")) //ignore metadata layer
                    continue;

                LayerBlendMode blendMode = layer.BlendMode;
                float opacity = Mathf.Min(layer.Opacity / 255f, cels[i].Opacity / 255f);

                bool visibility = layer.Visible;


                LayerChunk parent = GetParentLayer(layer);
                while (parent != null)
                {
                    visibility &= parent.Visible;
                    if (visibility == false)
                        break;

                    parent = GetParentLayer(parent);
                }

                if (visibility == false || layer.LayerType == LayerType.Group)
                    continue;

                Color32[] celColors = GetTextureDataFromCel(cels[i]);
                Texture2DBlender.BlendDelegate blendFunc = null;
                
                switch (blendMode)
                {
                    case LayerBlendMode.Normal: blendFunc = Texture2DBlender.Blend_Normal; break;
                    case LayerBlendMode.Multiply: blendFunc = Texture2DBlender.Blend_Multiply; break;
                    case LayerBlendMode.Screen: blendFunc = Texture2DBlender.Blend_Screen; break;
                    case LayerBlendMode.Overlay: blendFunc = Texture2DBlender.Blend_Overlay; break;
                    case LayerBlendMode.Darken: blendFunc = Texture2DBlender.Blend_Darken; break;
                    case LayerBlendMode.Lighten: blendFunc = Texture2DBlender.Blend_Lighten;; break;
                    case LayerBlendMode.ColorDodge: blendFunc = Texture2DBlender.Blend_ColorDodge; break;
                    case LayerBlendMode.ColorBurn: blendFunc = Texture2DBlender.Blend_ColorBurn; break;
                    case LayerBlendMode.HardLight: blendFunc = Texture2DBlender.Blend_HardLight; break;
                    case LayerBlendMode.SoftLight: blendFunc = Texture2DBlender.Blend_SoftLight; break;
                    case LayerBlendMode.Difference: blendFunc = Texture2DBlender.Blend_Difference; break;
                    case LayerBlendMode.Exclusion: blendFunc = Texture2DBlender.Blend_Exclusion; break;
                    case LayerBlendMode.Hue: blendFunc = Texture2DBlender.Blend_Hue; break;
                    case LayerBlendMode.Saturation: blendFunc = Texture2DBlender.Blend_Saturation; break;
                    case LayerBlendMode.Color: blendFunc = Texture2DBlender.Blend_Color; break;
                    case LayerBlendMode.Luminosity: blendFunc = Texture2DBlender.Blend_Luminosity; break;
                    case LayerBlendMode.Addition: blendFunc = Texture2DBlender.Blend_Addition; break;
                    case LayerBlendMode.Subtract: blendFunc = Texture2DBlender.Blend_Subtract; break;
                    case LayerBlendMode.Divide: blendFunc = Texture2DBlender.Blend_Divide; break;
                }

                if (blendFunc != null) {
                    Texture2DBlender.Blend(textureColors, celColors, blendFunc, opacity);
                }
            }

            texture.SetPixels32(textureColors);
            texture.Apply();
            return texture;
        }

        public Texture2D GetTextureFromCel(CelChunk cel)
        {
            int canvasWidth = Header.Width;
            int canvasHeight = Header.Height;
            
            Color32[] colors;
            Texture2D texture = Texture2DUtil.CreateTransparentTexture(canvasWidth, canvasHeight, out colors);

            int pixelIndex = 0;
            int celXEnd = cel.Width + cel.X;
            int celYEnd = cel.Height + cel.Y;


            for (int y = cel.Y; y < celYEnd; y++)
            {
                if (y < 0 || y >= canvasHeight)
                {
                    pixelIndex += cel.Width;
                    continue;
                }

                for (int x = cel.X; x < celXEnd; x++)
                {
                    if (x >= 0 && x < canvasWidth)
                    {
                        int index = (canvasHeight - 1 - y) * canvasWidth + x;
                        colors[index] = cel.RawPixelData[pixelIndex];
                    }

                    ++pixelIndex;
                }
            }

            texture.SetPixels32(0, 0, canvasWidth, canvasHeight, colors);
            texture.Apply();

            return texture;
        }

        private Color32[] GetTextureDataFromCel(CelChunk cel)
        {
            int canvasWidth = Header.Width;
            int canvasHeight = Header.Height;
            
            Color32[] colors = tempColorBuffer;
            Array.Clear(colors, 0, colors.Length);
            
            int pixelIndex = 0;
            int celXEnd = cel.Width + cel.X;
            int celYEnd = cel.Height + cel.Y;


            for (int y = cel.Y; y < celYEnd; y++)
            {
                if (y < 0 || y >= canvasHeight)
                {
                    pixelIndex += cel.Width;
                    continue;
                }

                for (int x = cel.X; x < celXEnd; x++)
                {
                    if (x >= 0 && x < canvasWidth)
                    {
                        int index = (canvasHeight - 1 - y) * canvasWidth + x;
                        colors[index] = cel.RawPixelData[pixelIndex];
                    }

                    ++pixelIndex;
                }
            }

            return colors;
        }

        public FrameTag[] GetAnimations()
        {
            List<FrameTagsChunk> tagChunks = this.GetChunks<FrameTagsChunk>();

            List<FrameTag> animations = new List<FrameTag>();

            foreach (FrameTagsChunk tagChunk in tagChunks)
            {
                foreach (FrameTag tag in tagChunk.Tags)
                {
                    animations.Add(tag);
                }
            }

            return animations.ToArray();
        }

        public MetaData[] GetMetaData(Vector2 spritePivot, int pixelsPerUnit)
        {
            Dictionary<int, MetaData> metadatas = new Dictionary<int, MetaData>();

            for (int index = 0; index < Frames.Count; index++)
            {
                List<LayerChunk> layers = GetChunks<LayerChunk>();
                List<CelChunk> cels = Frames[index].GetChunks<CelChunk>();

                cels.Sort((ca, cb) => ca.LayerIndex.CompareTo(cb.LayerIndex));

                for (int i = 0; i < cels.Count; i++)
                {
                    int layerIndex = cels[i].LayerIndex;
                    LayerChunk layer = layers[layerIndex];
                    if (!layer.LayerName.StartsWith(MetaData.MetaDataChar)) //read only metadata layer
                        continue;

                    if (!metadatas.ContainsKey(layerIndex))
                        metadatas[layerIndex] = new MetaData(layer.LayerName);
                    var metadata = metadatas[layerIndex];

                    CelChunk cel = cels[i];
                    Vector2 center = Vector2.zero;
                    int pixelCount = 0;

                    for (int y = 0; y < cel.Height; ++y)
                    {
                        for (int x = 0; x < cel.Width; ++x)
                        {
                            int texX = cel.X + x;
                            int texY = -(cel.Y + y) + Header.Height - 1;
                            var col = cel.RawPixelData[x + y * cel.Width];
                            if (col.a > 0.1f)
                            {
                                center += new Vector2(texX, texY);
                                pixelCount++;
                            }
                        }
                    }

                    if (pixelCount > 0)
                    {
                        center /= pixelCount;
                        var pivot = Vector2.Scale(spritePivot, new Vector2(Header.Width, Header.Height));
                        var posWorld = (center - pivot) / pixelsPerUnit + Vector2.one * 0.5f / pixelsPerUnit; //center pos in middle of pixels

                        metadata.Transforms.Add(index, posWorld);
                    }
                }
            }
            return metadatas.Values.ToArray();
        }

        public Texture2D GetTextureAtlas()
        {
            Texture2D[] frames = this.GetFrames();

            Texture2D atlas = Texture2DUtil.CreateTransparentTexture(Header.Width * frames.Length, Header.Height);
            List<Rect> spriteRects = new List<Rect>();

            int col = 0;
            int row = 0;

            foreach (Texture2D frame in frames)
            {
                Rect spriteRect = new Rect(col * Header.Width, atlas.height - ((row + 1) * Header.Height), Header.Width, Header.Height);
                atlas.SetPixels((int)spriteRect.x, (int)spriteRect.y, (int)spriteRect.width, (int)spriteRect.height, frame.GetPixels());
                atlas.Apply();

                spriteRects.Add(spriteRect);

                col++;
            }

            return atlas;
        }
    }

}
