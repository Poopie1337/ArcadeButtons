using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace SimplifiedGame
{
    public class TileMap
    {
        // Map properties
        private int width;
        private int height;
        private int tileWidth;
        private int tileHeight;

        // Layers for rendering and collision
        private List<TileLayer> layers = new List<TileLayer>();

        // Tileset texture
        private Texture2D tilesetTexture;

        // Animation timers
        private float animationTimer = 0;
        private Dictionary<int, Animation> animations = new Dictionary<int, Animation>();

        // Public properties
        public int Width => width;
        public int Height => height;
        public int TileWidth => tileWidth;
        public int TileHeight => tileHeight;
        public int WidthInPixels => width * tileWidth;
        public int HeightInPixels => height * tileHeight;

        // Load the map content
        public void LoadContent(ContentManager content, string tmxFilename)
        {
            try
            {
                // Get the full path to the TMX file
                string tmxPath = Path.Combine(content.RootDirectory, tmxFilename + ".tmx");

                // Load the TMX file
                XDocument doc = XDocument.Load(tmxPath);
                XElement mapElement = doc.Element("map");

                // Read map properties
                width = int.Parse(mapElement.Attribute("width").Value);
                height = int.Parse(mapElement.Attribute("height").Value);
                tileWidth = int.Parse(mapElement.Attribute("tilewidth").Value);
                tileHeight = int.Parse(mapElement.Attribute("tileheight").Value);

                // Find tileset element
                XElement tilesetElement = mapElement.Element("tileset");
                string tilesetName = tilesetElement.Attribute("name").Value;

                // Determine tileset image path from the source attribute
                XElement imageElement = tilesetElement.Element("image");
                string source = imageElement.Attribute("source").Value;

                // Extract just the filename without extension to use with Content.Load
                string tilesetPath = Path.GetFileNameWithoutExtension(source);

                // Load tileset texture - assuming it's been processed by the content pipeline
                try
                {
                    tilesetTexture = content.Load<Texture2D>(tilesetPath);
                }
                catch (Exception ex)
                {
                    // If we can't load it (maybe it's not in the content pipeline),
                    // use a placeholder texture
                    Console.WriteLine($"Error loading tileset texture: {ex.Message}");
                    tilesetTexture = new Texture2D(content.ServiceProvider.GetService(typeof(GraphicsDevice)) as GraphicsDevice, 32, 32);
                    Color[] colorData = new Color[32 * 32];
                    for (int i = 0; i < colorData.Length; i++)
                        colorData[i] = Color.Magenta; // Placeholder color
                    tilesetTexture.SetData(colorData);
                }

                // Load animations
                foreach (XElement tileElement in tilesetElement.Elements("tile"))
                {
                    int tileId = int.Parse(tileElement.Attribute("id").Value);
                    XElement animationElement = tileElement.Element("animation");

                    if (animationElement != null)
                    {
                        List<AnimationFrame> frames = new List<AnimationFrame>();

                        foreach (XElement frameElement in animationElement.Elements("frame"))
                        {
                            int frameTileId = int.Parse(frameElement.Attribute("tileid").Value);
                            int duration = int.Parse(frameElement.Attribute("duration").Value);
                            frames.Add(new AnimationFrame(frameTileId, duration / 1000f)); // Convert ms to seconds
                        }

                        animations[tileId] = new Animation(frames);
                    }
                }

                // Load layers
                foreach (XElement layerElement in mapElement.Elements("layer"))
                {
                    string name = layerElement.Attribute("name").Value;
                    bool visible = true;
                    XAttribute visibleAttr = layerElement.Attribute("visible");
                    if (visibleAttr != null)
                    {
                        visible = int.Parse(visibleAttr.Value) == 1;
                    }

                    // Create the layer
                    TileLayer layer = new TileLayer(name, width, height, visible);

                    // Load layer data
                    XElement dataElement = layerElement.Element("data");
                    string encoding = dataElement.Attribute("encoding")?.Value;

                    // Parse the tile data
                    if (encoding == "csv")
                    {
                        string[] values = dataElement.Value.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        int index = 0;

                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                if (index < values.Length)
                                {
                                    int gid = int.Parse(values[index].Trim());
                                    layer.SetTile(x, y, gid);
                                    index++;
                                }
                            }
                        }
                    }
                    else
                    {
                        // For XML encoded data
                        int index = 0;
                        foreach (XElement tileElement in dataElement.Elements("tile"))
                        {
                            int gid = int.Parse(tileElement.Attribute("gid").Value);
                            int x = index % width;
                            int y = index / width;
                            layer.SetTile(x, y, gid);
                            index++;
                        }
                    }

                    layers.Add(layer);
                }

                Console.WriteLine($"Loaded map: {width}x{height} tiles, {tileWidth}x{tileHeight} tile size");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading map: {ex.Message}");

                // Create a default map in case of loading failure
                CreateDefaultMap();
            }
        }

        // Create a simple default map in case loading fails
        private void CreateDefaultMap()
        {
            width = 40;
            height = 24;
            tileWidth = 32;
            tileHeight = 32;

            // Create a basic ground layer
            TileLayer groundLayer = new TileLayer("ground", width, height, true);

            // Fill with a single tile type
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Border tiles
                    if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                    {
                        groundLayer.SetTile(x, y, 2); // Border
                    }
                    else
                    {
                        groundLayer.SetTile(x, y, 1); // Ground
                    }
                }
            }

            layers.Add(groundLayer);

            // Create a obstacle layer
            TileLayer obstacleLayer = new TileLayer("obstacles", width, height, true);

            // Add some random obstacles
            Random random = new Random();
            for (int i = 0; i < 30; i++)
            {
                int x = random.Next(3, width - 3);
                int y = random.Next(3, height - 3);
                obstacleLayer.SetTile(x, y, 3); // Obstacle
            }

            layers.Add(obstacleLayer);
        }

        // Update animations
        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            animationTimer += deltaTime;

            foreach (var animation in animations.Values)
            {
                animation.Update(animationTimer);
            }
        }

        // Draw the map
        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var layer in layers)
            {
                if (layer.Visible)
                {
                    DrawLayer(spriteBatch, layer);
                }
            }
        }

        // Draw a single layer
        private void DrawLayer(SpriteBatch spriteBatch, TileLayer layer)
        {
            // Calculate tileset properties
            int tilesPerRow = tilesetTexture.Width / tileWidth;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int tileId = layer.GetTile(x, y);

                    if (tileId > 0) // 0 means no tile
                    {
                        // Convert from Tiled GID (1-based) to 0-based index for calculations
                        int index = tileId - 1;

                        // Check if this tile has an animation
                        if (animations.ContainsKey(index))
                        {
                            // Use the current animation frame
                            index = animations[index].CurrentFrame;
                        }

                        // Calculate the source rectangle
                        int srcX = (index % tilesPerRow) * tileWidth;
                        int srcY = (index / tilesPerRow) * tileHeight;
                        Rectangle srcRect = new Rectangle(srcX, srcY, tileWidth, tileHeight);

                        // Calculate destination position
                        Vector2 position = new Vector2(x * tileWidth, y * tileHeight);

                        // Draw the tile
                        spriteBatch.Draw(
                            tilesetTexture,
                            position,
                            srcRect,
                            Color.White
                        );
                    }
                }
            }
        }

        // Check collision with the map
        public bool CheckCollision(Vector2 position, float radius)
        {
            // Find the obstacle layer
            TileLayer obstacleLayer = null;
            foreach (var layer in layers)
            {
                if (layer.Name.ToLower().Contains("obstacle"))
                {
                    obstacleLayer = layer;
                    break;
                }
            }

            if (obstacleLayer == null)
                return false;

            // Check a few points around the object for better collision detection
            for (float angle = 0; angle < 360; angle += 45)
            {
                float radians = MathHelper.ToRadians(angle);
                Vector2 checkPoint = new Vector2(
                    position.X + (float)Math.Cos(radians) * radius * 0.8f,
                    position.Y + (float)Math.Sin(radians) * radius * 0.8f
                );

                // Convert to tile coordinates
                int tileX = (int)(checkPoint.X / tileWidth);
                int tileY = (int)(checkPoint.Y / tileHeight);

                // Check if there's an obstacle tile at this position
                if (tileX >= 0 && tileX < width && tileY >= 0 && tileY < height)
                {
                    int tileId = obstacleLayer.GetTile(tileX, tileY);
                    if (tileId > 0) // Any non-zero tile is considered an obstacle
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    // Represents a layer of tiles
    public class TileLayer
    {
        private string name;
        private int width;
        private int height;
        private bool visible;
        private int[,] tiles;

        public string Name => name;
        public int Width => width;
        public int Height => height;
        public bool Visible { get; set; }

        public TileLayer(string name, int width, int height, bool visible)
        {
            this.name = name;
            this.width = width;
            this.height = height;
            this.visible = visible;
            tiles = new int[width, height];
        }

        public void SetTile(int x, int y, int tileId)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                tiles[x, y] = tileId;
            }
        }

        public int GetTile(int x, int y)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                return tiles[x, y];
            }
            return 0;
        }
    }

    // Represents an animation frame
    public class AnimationFrame
    {
        public int TileId { get; private set; }
        public float Duration { get; private set; }

        public AnimationFrame(int tileId, float duration)
        {
            TileId = tileId;
            Duration = duration;
        }
    }

    // Represents a tile animation
    public class Animation
    {
        private List<AnimationFrame> frames;
        private int currentFrameIndex = 0;

        public int CurrentFrame => frames[currentFrameIndex].TileId;

        public Animation(List<AnimationFrame> frames)
        {
            this.frames = frames;
        }

        public void Update(float totalTime)
        {
            // Calculate total duration of all frames
            float totalDuration = 0;
            foreach (var frame in frames)
            {
                totalDuration += frame.Duration;
            }

            // Calculate current position in animation cycle
            float animTime = totalTime % totalDuration;

            // Find current frame
            float timePosition = 0;
            for (int i = 0; i < frames.Count; i++)
            {
                timePosition += frames[i].Duration;
                if (animTime < timePosition)
                {
                    currentFrameIndex = i;
                    break;
                }
            }
        }
    }
}