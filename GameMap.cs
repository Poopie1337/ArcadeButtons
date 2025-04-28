using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;

namespace SimplifiedGame
{
    /// <summary>
    /// Wraps the TileMap renderer/logic and adds convenient spawn‑point helpers.
    /// </summary>
    public class GameMap
    {
        // The tile map that holds data and handles rendering/collision
        private TileMap tileMap;

        // Map dimensions in pixels
        public int WidthInPixels => tileMap.WidthInPixels;
        public int HeightInPixels => tileMap.HeightInPixels;

        // Object‑type → list of world positions
        private readonly Dictionary<string, List<Vector2>> spawnPoints = new();

        public GameMap()
        {
            tileMap = new TileMap();
        }

        /// <summary>
        /// Loads a TMX map that has already been processed by the MonoGame content pipeline.
        /// </summary>
        /// <param name="mapName">Name of the TMX file *without* extension.</param>
        public void LoadMap(string mapName, ContentManager content)
        {
            tileMap.LoadContent(content, mapName);
            ParseObjectLayers();
        }

        #region Spawn‑point helpers
        private void ParseObjectLayers()
        {
            // Until object‑layer support is added, generate some sensible defaults.
            const int cornerPadding = 100;

            spawnPoints["player"] = new List<Vector2>
            {
                // Clockwise from bottom‑left
                new(cornerPadding, HeightInPixels - cornerPadding),
                new(WidthInPixels - cornerPadding, HeightInPixels - cornerPadding),
                new(WidthInPixels - cornerPadding, cornerPadding),
                new(cornerPadding, cornerPadding)
            };

            // Camp‑fire dead‑center
            spawnPoints["campfire"] = new List<Vector2> { new(WidthInPixels / 2f, HeightInPixels / 2f) };

            // Enemies: every 100 px round the edges, inset a little
            const int edgePad = 50;
            var enemySpawns = new List<Vector2>();
            for (int x = edgePad; x < WidthInPixels - edgePad; x += 100)
            {
                enemySpawns.Add(new(x, edgePad));                          // top
                enemySpawns.Add(new(x, HeightInPixels - edgePad));         // bottom
            }
            for (int y = edgePad; y < HeightInPixels - edgePad; y += 100)
            {
                enemySpawns.Add(new(edgePad, y));                          // left
                enemySpawns.Add(new(WidthInPixels - edgePad, y));          // right
            }
            spawnPoints["enemy"] = enemySpawns;
        }

        public Vector2 GetSpawnPoint(string type, int index = 0)
        {
            return spawnPoints.ContainsKey(type) && index < spawnPoints[type].Count
                ? spawnPoints[type][index]
                : new(WidthInPixels / 2f, HeightInPixels / 2f);
        }

        public List<Vector2> GetAllSpawnPoints(string type) =>
            spawnPoints.TryGetValue(type, out var list) ? list : new();

        /// <summary> Picks a random enemy spawn along the edge of the map. </summary>
        public Vector2 GetRandomEnemySpawnPoint(Random rng)
        {
            var list = GetAllSpawnPoints("enemy");
            if (list.Count > 0) return list[rng.Next(list.Count)];

            // Fallback: random edge position
            return rng.Next(4) switch
            {
                0 => new(rng.Next(WidthInPixels), 0),
                1 => new(WidthInPixels, rng.Next(HeightInPixels)),
                2 => new(rng.Next(WidthInPixels), HeightInPixels),
                _ => new(0, rng.Next(HeightInPixels))
            };
        }
        #endregion

        #region Map update / draw / collision wrapper
        public void Update(GameTime gameTime) => tileMap.Update(gameTime);
        public void Draw(SpriteBatch spriteBatch) => tileMap.Draw(spriteBatch);

        /// <summary>Delegates to TileMap.CheckCollision for convenience.</summary>
        public bool CheckCollision(Vector2 position, float radius) =>
            tileMap.CheckCollision(position, radius);
        #endregion
    }
}
