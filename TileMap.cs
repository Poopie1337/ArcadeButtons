using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;

namespace SimplifiedGame
{
    /// <summary>
    /// Loads, renders and provides collision for a TMX map exported from Tiled.
    /// Supports CSV, Base‑64, Base‑64 + gzip and Base‑64 + zlib layer encodings.
    /// </summary>
    public class TileMap
    {
        // ───────────────────────────────────────── fields ─────────────────────────────────────────
        private int width, height;           // tile counts
        private int tileWidth, tileHeight;   // pixels

        private readonly List<TileLayer> layers = new();
        private readonly Dictionary<int, Animation> animations = new();

        private Texture2D tilesetTexture;
        private int tilesetFirstGid = 1;    // set while loading

        private float animationClock = 0f;

        // ───────────────────────────────────────── public API ─────────────────────────────────────
        public int Width => width;
        public int Height => height;
        public int TileWidth => tileWidth;
        public int TileHeight => tileHeight;
        public int WidthInPixels => width * tileWidth;
        public int HeightInPixels => height * tileHeight;

        /// <summary>
        /// Reads a TMX file (without extension) from the Content directory and loads any tileset images via the MonoGame pipeline.
        /// </summary>
        public void LoadContent(ContentManager content, string mapName)
        {
            // ───────────── open TMX ─────────────
            string tmxPath = Path.Combine(content.RootDirectory, mapName + ".tmx");
            Console.WriteLine($"[TileMap] Loading TMX: {tmxPath}");

            if (!File.Exists(tmxPath))
            {
                throw new FileNotFoundException($"TMX file not found: {tmxPath}");
            }

            XElement mapElement = XDocument.Load(tmxPath).Element("map") ?? throw new Exception("invalid TMX");

            width = int.Parse(mapElement.Attribute("width").Value);
            height = int.Parse(mapElement.Attribute("height").Value);
            tileWidth = int.Parse(mapElement.Attribute("tilewidth").Value);
            tileHeight = int.Parse(mapElement.Attribute("tileheight").Value);

            Console.WriteLine($"[TileMap] Map dimensions: {width}x{height} tiles, {tileWidth}x{tileHeight} pixels per tile");

            // ───────────── tileset (assumes single tileset) ─────────────
            XElement tilesetEl = mapElement.Element("tileset");
            if (tilesetEl == null)
            {
                throw new Exception("No tileset found in TMX file");
            }

            tilesetFirstGid = int.Parse(tilesetEl.Attribute("firstgid")?.Value ?? "1");

            // Check if tileset has embedded image
            XElement imageEl = tilesetEl.Element("image");
            string imageSource = null;
            string textureKey = null;

            if (imageEl != null)
            {
                // Embedded tileset
                imageSource = imageEl.Attribute("source")?.Value;
                if (imageSource == null)
                {
                    throw new Exception("Tileset image element found but no source attribute");
                }
                textureKey = Path.GetFileNameWithoutExtension(imageSource);
                Console.WriteLine($"[TileMap] Found embedded tileset with image: {imageSource}");
            }
            else
            {
                // Check if it's an external tileset reference
                string externalSource = tilesetEl.Attribute("source")?.Value;
                if (externalSource != null)
                {
                    // External tileset - we need to load the .tsx file
                    string tsxPath = Path.Combine(content.RootDirectory, externalSource);
                    Console.WriteLine($"[TileMap] Loading external tileset: {tsxPath}");

                    if (!File.Exists(tsxPath))
                    {
                        throw new FileNotFoundException($"External tileset file not found: {tsxPath}");
                    }

                    XDocument tsxDoc = XDocument.Load(tsxPath);
                    XElement externalImageEl = tsxDoc.Root.Element("image");

                    if (externalImageEl?.Attribute("source") != null)
                    {
                        imageSource = externalImageEl.Attribute("source").Value;
                        textureKey = Path.GetFileNameWithoutExtension(imageSource);
                        Console.WriteLine($"[TileMap] External tileset references image: {imageSource}");

                        // Load animations from external tileset
                        foreach (var tileEl in tsxDoc.Root.Elements("tile"))
                        {
                            int localId = int.Parse(tileEl.Attribute("id").Value);
                            XElement animEl = tileEl.Element("animation");
                            if (animEl is null) continue;

                            List<AnimationFrame> frames = new();
                            foreach (var frameEl in animEl.Elements("frame"))
                            {
                                frames.Add(new AnimationFrame(int.Parse(frameEl.Attribute("tileid").Value),
                                                              int.Parse(frameEl.Attribute("duration").Value) / 1000f));
                            }
                            animations[localId] = new Animation(frames);
                        }
                    }
                    else
                    {
                        throw new Exception($"Could not find image source in external tileset {externalSource}");
                    }
                }
                else
                {
                    // Create a default texture if no tileset image is found
                    Console.WriteLine("[TileMap] Warning: No tileset image found, creating placeholder");
                    textureKey = "placeholder";
                }
            }

            try
            {
                if (textureKey == "placeholder" || textureKey == null)
                {
                    // Create a default placeholder texture
                    var gd = content.ServiceProvider.GetService(typeof(GraphicsDevice)) as GraphicsDevice;
                    tilesetTexture = new Texture2D(gd, 32, 32);
                    Color[] colors = new Color[32 * 32];
                    for (int i = 0; i < colors.Length; i++)
                        colors[i] = (i % 64 < 32) ? Color.White : Color.Gray; // Checkerboard pattern
                    tilesetTexture.SetData(colors);
                    Console.WriteLine("[TileMap] Using checkerboard placeholder texture");
                }
                else
                {
                    Console.WriteLine($"[TileMap] Loading texture: {textureKey}");
                    tilesetTexture = content.Load<Texture2D>(textureKey);
                    Console.WriteLine($"[TileMap] Texture loaded successfully. Size: {tilesetTexture.Width}x{tilesetTexture.Height}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TileMap] Could not load tileset texture '{textureKey}': {ex.Message}. Using placeholder.");
                var gd = content.ServiceProvider.GetService(typeof(GraphicsDevice)) as GraphicsDevice;
                tilesetTexture = new Texture2D(gd, 32, 32);
                Color[] mag = new Color[32 * 32];
                for (int i = 0; i < mag.Length; i++) mag[i] = Color.Magenta;
                tilesetTexture.SetData(mag);
            }

            // ───────────── optional tile animations (for embedded tilesets) ─────────────
            if (imageEl != null) // Only load animations if it's an embedded tileset
            {
                foreach (var tileEl in tilesetEl.Elements("tile"))
                {
                    int localId = int.Parse(tileEl.Attribute("id").Value);
                    XElement animEl = tileEl.Element("animation");
                    if (animEl is null) continue;

                    List<AnimationFrame> frames = new();
                    foreach (var frameEl in animEl.Elements("frame"))
                    {
                        frames.Add(new AnimationFrame(int.Parse(frameEl.Attribute("tileid").Value),
                                                      int.Parse(frameEl.Attribute("duration").Value) / 1000f));
                    }
                    animations[localId] = new Animation(frames);
                }
            }

            // ───────────── tile layers ─────────────
            foreach (var layerEl in mapElement.Elements("layer"))
            {
                string layerName = layerEl.Attribute("name").Value;
                Console.WriteLine($"[TileMap] Loading layer: {layerName}");

                var layer = new TileLayer(layerName,
                                          width, height,
                                          int.Parse(layerEl.Attribute("visible")?.Value ?? "1") == 1);

                XElement dataEl = layerEl.Element("data");
                string encoding = dataEl.Attribute("encoding")?.Value;
                string compression = dataEl.Attribute("compression")?.Value;

                Console.WriteLine($"[TileMap] Layer encoding: {encoding ?? "none"}, compression: {compression ?? "none"}");

                // ---------- CSV ----------
                if (encoding == "csv")
                {
                    string[] tokens = dataEl.Value.Split(
                        new[] { ',', '\n', '\r', '\t', ' ' },
                        StringSplitOptions.RemoveEmptyEntries);

                    int idx = 0;
                    for (int y = 0; y < height; y++)
                        for (int x = 0; x < width; x++)
                        {
                            int gid = (idx < tokens.Length && int.TryParse(tokens[idx++], out int g)) ? g : 0;
                            layer.SetTile(x, y, gid);
                        }
                }
                // ---------- Base-64 (+ optional gzip / zlib) ----------
                else if (encoding == "base64")
                {
                    byte[] raw = Convert.FromBase64String(dataEl.Value.Trim());

                    Stream s = new MemoryStream(raw);
                    if (compression == "gzip")
                        s = new GZipStream(s, CompressionMode.Decompress);
                    else if (compression == "zlib")
                        s = new DeflateStream(s, CompressionMode.Decompress);

                    using var br = new BinaryReader(s);
                    for (int y = 0; y < height; y++)
                        for (int x = 0; x < width; x++)
                            layer.SetTile(x, y, br.ReadInt32());
                }
                // ---------- Plain XML <tile gid="…"/> ----------
                else if (encoding == null)
                {
                    int index = 0;
                    foreach (var tileEl in dataEl.Elements("tile"))
                    {
                        int gid = int.Parse(tileEl.Attribute("gid").Value);
                        int x = index % width;
                        int y = index / width;
                        layer.SetTile(x, y, gid);
                        index++;
                    }
                }
                else
                    throw new NotSupportedException($"Layer encoding '{encoding}' not supported.");

                layers.Add(layer);
            }

            Console.WriteLine($"[TileMap] Loaded '{mapName}' ({width}x{height} tiles, {layers.Count} layers)");
        }

        // ───────────────────────────────────────── runtime update / draw ─────────────────────────
        public void Update(GameTime gameTime)
        {
            animationClock += (float)gameTime.ElapsedGameTime.TotalSeconds;
            foreach (var anim in animations.Values) anim.Update(animationClock);
        }

        public void Draw(SpriteBatch sb)
        {
            foreach (var layer in layers)
            {
                if (!layer.Visible) continue;
                DrawLayer(sb, layer);
            }
        }

        private void DrawLayer(SpriteBatch sb, TileLayer layer)
        {
            int columns = tilesetTexture.Width / tileWidth;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int gid = layer.GetTile(x, y);
                    if (gid == 0) continue;   // empty

                    int localId = gid - tilesetFirstGid;     // translate to 0‑based index within tileset
                    if (localId < 0) continue;               // safety

                    if (animations.TryGetValue(localId, out var anim))
                        localId = anim.CurrentFrame;

                    int srcX = (localId % columns) * tileWidth;
                    int srcY = (localId / columns) * tileHeight;
                    sb.Draw(tilesetTexture,
                            new Vector2(x * tileWidth, y * tileHeight),
                            new Rectangle(srcX, srcY, tileWidth, tileHeight),
                            Color.White);
                }
            }
        }

        // ───────────────────────────────────────── collision helper ─────────────────────────
        public bool CheckCollision(Vector2 pos, float radius)
        {
            // very simple: any tile on a layer whose name contains "obstacle" blocks movement
            TileLayer obstacleLayer = layers.Find(l => l.Name.ToLower().Contains("obstacle"));
            if (obstacleLayer is null) return false;

            for (float a = 0; a < MathF.Tau; a += MathF.PI / 4)
            {
                Vector2 p = pos + radius * 0.8f * new Vector2(MathF.Cos(a), MathF.Sin(a));
                int tx = (int)(p.X / tileWidth);
                int ty = (int)(p.Y / tileHeight);
                if (tx >= 0 && tx < width && ty >= 0 && ty < height && obstacleLayer.GetTile(tx, ty) != 0)
                    return true;
            }
            return false;
        }
    }

    // ───────────────────────────────────────── helper classes ───────────────────────────────────
    public class TileLayer
    {
        private readonly int[,] tiles;
        public string Name { get; }
        public bool Visible { get; set; }

        public TileLayer(string name, int w, int h, bool visible)
        {
            Name = name;
            Visible = visible;
            tiles = new int[w, h];
        }
        public void SetTile(int x, int y, int gid) => tiles[x, y] = gid;
        public int GetTile(int x, int y) => tiles[x, y];
    }

    public class AnimationFrame
    {
        public int TileId { get; }
        public float Length { get; }  // seconds
        public AnimationFrame(int id, float len) { TileId = id; Length = len; }
    }

    public class Animation
    {
        private readonly AnimationFrame[] frames;
        private float totalLength;
        public int CurrentFrame { get; private set; }

        public Animation(List<AnimationFrame> f)
        {
            frames = f.ToArray();
            foreach (var fr in frames) totalLength += fr.Length;
        }
        public void Update(float time)
        {
            float t = time % totalLength;
            float accum = 0f;
            for (int i = 0; i < frames.Length; i++)
            {
                accum += frames[i].Length;
                if (t <= accum) { CurrentFrame = frames[i].TileId; break; }
            }
        }
    }
}