using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace SimplifiedGame
{
    class WaveManager
    {
        // Wave properties
        private int currentWave = 0;
        private int enemiesRemaining = 0;
        private float spawnTimer = 0;
        private float spawnRate = 2.0f; // Time between enemy spawns
        private Random random = new Random();

        // Wave status
        private bool waveActive = false;
        private bool wavePreparing = false;
        private float waveStartTimer = 5.0f; // 5 second countdown before wave starts

        // Spawn points (edges of screen with padding)
        private List<Vector2> spawnPoints = new List<Vector2>();
        private int screenWidth, screenHeight;
        private const int SPAWN_PADDING = 50;

        // Enemy texture
        private Texture2D enemyTexture;

        // Properties
        public int CurrentWave => currentWave;
        public int EnemiesRemaining => enemiesRemaining;
        public bool WaveActive => waveActive;
        public bool WavePreparing => wavePreparing;
        public float WaveStartTimer => waveStartTimer;

        public WaveManager(int screenWidth, int screenHeight, Texture2D enemyTexture)
        {
            this.screenWidth = screenWidth;
            this.screenHeight = screenHeight;
            this.enemyTexture = enemyTexture;

            // Generate spawn points along the edges of the screen
            GenerateDefaultSpawnPoints();
        }

        // Method to set spawn points from the Tiled map
        public void SetSpawnPoints(List<Vector2> mapSpawnPoints)
        {
            if (mapSpawnPoints != null && mapSpawnPoints.Count > 0)
            {
                // Replace default spawn points with ones from the map
                spawnPoints = mapSpawnPoints;
                Console.WriteLine($"Set {spawnPoints.Count} spawn points from map");
            }
        }

        private void GenerateDefaultSpawnPoints()
        {
            // Create spawn points along the edges with padding

            // Top edge
            for (int x = SPAWN_PADDING; x < screenWidth - SPAWN_PADDING; x += 100)
            {
                spawnPoints.Add(new Vector2(x, SPAWN_PADDING));
            }

            // Bottom edge
            for (int x = SPAWN_PADDING; x < screenWidth - SPAWN_PADDING; x += 100)
            {
                spawnPoints.Add(new Vector2(x, screenHeight - SPAWN_PADDING));
            }

            // Left edge
            for (int y = SPAWN_PADDING; y < screenHeight - SPAWN_PADDING; y += 100)
            {
                spawnPoints.Add(new Vector2(SPAWN_PADDING, y));
            }

            // Right edge
            for (int y = SPAWN_PADDING; y < screenHeight - SPAWN_PADDING; y += 100)
            {
                spawnPoints.Add(new Vector2(screenWidth - SPAWN_PADDING, y));
            }
        }

        public void StartNextWave()
        {
            currentWave++;
            wavePreparing = true;
            waveStartTimer = 5.0f;

            // Adjust difficulty based on wave number
            spawnRate = Math.Max(0.5f, 2.0f - (currentWave * 0.1f));

            // Calculate number of enemies for this wave
            enemiesRemaining = 5 + (currentWave * 3);

            Console.WriteLine($"Preparing Wave {currentWave} with {enemiesRemaining} enemies. Spawn rate: {spawnRate}");
        }

        public void Update(GameTime gameTime, List<Enemy> enemies, Vector2 campfirePosition)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (wavePreparing)
            {
                // Count down to wave start
                waveStartTimer -= deltaTime;
                if (waveStartTimer <= 0)
                {
                    wavePreparing = false;
                    waveActive = true;
                    spawnTimer = 0; // Spawn first enemy immediately
                    Console.WriteLine($"Wave {currentWave} started!");
                }
            }
            else if (waveActive)
            {
                // Check if all enemies are defeated
                if (enemiesRemaining <= 0 && !HasActiveEnemies(enemies))
                {
                    waveActive = false;
                    Console.WriteLine($"Wave {currentWave} completed!");
                    return;
                }

                // Spawn enemies if there are still enemies to spawn
                if (enemiesRemaining > 0)
                {
                    spawnTimer -= deltaTime;
                    if (spawnTimer <= 0)
                    {
                        SpawnEnemy(enemies, campfirePosition);
                        spawnTimer = spawnRate;
                    }
                }
            }
        }

        private bool HasActiveEnemies(List<Enemy> enemies)
        {
            foreach (var enemy in enemies)
            {
                if (enemy.IsActive)
                {
                    return true;
                }
            }
            return false;
        }

        private void SpawnEnemy(List<Enemy> enemies, Vector2 targetPosition)
        {
            if (enemiesRemaining <= 0) return;

            // Get a random spawn point
            Vector2 spawnPosition = spawnPoints[random.Next(spawnPoints.Count)];

            // Make sure spawned enemy is some minimum distance from the target
            // This prevents enemies from spawning right on top of players or campfire
            const float MIN_SPAWN_DISTANCE = 300f;
            int maxAttempts = 10;
            int attempts = 0;

            while (Vector2.Distance(spawnPosition, targetPosition) < MIN_SPAWN_DISTANCE && attempts < maxAttempts)
            {
                spawnPosition = spawnPoints[random.Next(spawnPoints.Count)];
                attempts++;
            }

            // Determine enemy type based on wave and randomness
            EnemyType type;
            float chance = (float)random.NextDouble();

            if (currentWave < 3)
            {
                // Early waves: only Basic enemies
                type = EnemyType.Basic;
            }
            else if (currentWave < 5)
            {
                // Waves 3-4: Basic and Fast enemies
                type = chance < 0.7f ? EnemyType.Basic : EnemyType.Fast;
            }
            else if (currentWave < 8)
            {
                // Waves 5-7: Add Tank enemies
                if (chance < 0.6f)
                    type = EnemyType.Basic;
                else if (chance < 0.85f)
                    type = EnemyType.Fast;
                else
                    type = EnemyType.Tank;
            }
            else
            {
                // Wave 8+: All enemy types
                if (chance < 0.5f)
                    type = EnemyType.Basic;
                else if (chance < 0.75f)
                    type = EnemyType.Fast;
                else if (chance < 0.9f)
                    type = EnemyType.Tank;
                else
                    type = EnemyType.Shooter;
            }

            // Create and add the enemy
            Enemy newEnemy = new Enemy(spawnPosition, type, enemyTexture);
            enemies.Add(newEnemy);

            // Decrease remaining enemies count
            enemiesRemaining--;
        }

        // Add this method to your WaveManager class
        public void SetupSpawnPoints(TileMap tileMap)
        {
            // Clear existing spawn points
            spawnPoints.Clear();

            // Create spawn points along the edges
            int paddingFromEdge = SPAWN_PADDING;
            int spacing = 100;  // Space between spawn points

            // Top edge
            for (int x = paddingFromEdge; x < tileMap.WidthInPixels - paddingFromEdge; x += spacing)
            {
                // Check if this spawn point would be on an obstacle
                Vector2 spawnPos = new Vector2(x, paddingFromEdge);
                if (!tileMap.CheckCollision(spawnPos, 20)) // 20 is an approximate enemy radius
                {
                    spawnPoints.Add(spawnPos);
                }
            }

            // Bottom edge
            for (int x = paddingFromEdge; x < tileMap.WidthInPixels - paddingFromEdge; x += spacing)
            {
                Vector2 spawnPos = new Vector2(x, tileMap.HeightInPixels - paddingFromEdge);
                if (!tileMap.CheckCollision(spawnPos, 20))
                {
                    spawnPoints.Add(spawnPos);
                }
            }

            // Left edge
            for (int y = paddingFromEdge; y < tileMap.HeightInPixels - paddingFromEdge; y += spacing)
            {
                Vector2 spawnPos = new Vector2(paddingFromEdge, y);
                if (!tileMap.CheckCollision(spawnPos, 20))
                {
                    spawnPoints.Add(spawnPos);
                }
            }

            // Right edge
            for (int y = paddingFromEdge; y < tileMap.HeightInPixels - paddingFromEdge; y += spacing)
            {
                Vector2 spawnPos = new Vector2(tileMap.WidthInPixels - paddingFromEdge, y);
                if (!tileMap.CheckCollision(spawnPos, 20))
                {
                    spawnPoints.Add(spawnPos);
                }
            }

            // Make sure we have at least some spawn points
            if (spawnPoints.Count < 10)
            {
                // Fall back to the default spawn points generation if we don't have enough valid spots
                GenerateDefaultSpawnPoints();
            }

            Console.WriteLine($"Set up {spawnPoints.Count} enemy spawn points");
        }

        public void Draw(SpriteBatch spriteBatch, BitmapFontRenderer fontRenderer)
        {
            if (wavePreparing)
            {
                // Draw wave countdown
                fontRenderer.DrawTextCentered(
                    spriteBatch,
                    $"WAVE {currentWave} STARTING IN {Math.Ceiling(waveStartTimer)}",
                    new Vector2(spriteBatch.GraphicsDevice.Viewport.Width / 2, 100),
                    Color.Yellow,
                    1.5f
                );

                // Draw enemy count
                fontRenderer.DrawTextCentered(
                    spriteBatch,
                    $"ENEMIES: {enemiesRemaining}",
                    new Vector2(spriteBatch.GraphicsDevice.Viewport.Width / 2, 140),
                    Color.White,
                    1.0f
                );
            }
            else if (waveActive)
            {
                // Draw wave information
                fontRenderer.DrawTextCentered(
                    spriteBatch,
                    $"WAVE {currentWave}",
                    new Vector2(spriteBatch.GraphicsDevice.Viewport.Width / 2, 50),
                    Color.White,
                    1.0f
                );

                // Draw enemies remaining
                fontRenderer.DrawTextCentered(
                    spriteBatch,
                    $"ENEMIES REMAINING: {enemiesRemaining}",
                    new Vector2(spriteBatch.GraphicsDevice.Viewport.Width / 2, 80),
                    Color.White,
                    1.0f
                );
            }
        }
    }
}