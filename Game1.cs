using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SimplifiedGame
{
    public enum GameState
    {
        Menu,
        Playing,
        Upgrading,
        GameOver
    }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // Custom bitmap font renderer
        private BitmapFontRenderer fontRenderer;

        // Current game state
        private GameState currentState = GameState.Menu;

        // Simple circle texture
        private Texture2D circleTexture;

        // Campfire texture
        private Texture2D campfireTexture;

        // Gray overlay texture for inactive players
        private Texture2D grayOverlayTexture;

        // Start button texture
        private Texture2D startButtonTexture;

        // Projectile texture
        private Texture2D projectileTexture;

        // List of players
        private List<Player> players = new List<Player>();

        // List of enemies
        private List<Enemy> enemies = new List<Enemy>();

        // List of projectiles
        private List<Projectile> projectiles = new List<Projectile>();

        // Track which players have joined
        private bool[] joinedPlayers = new bool[4] { false, false, false, false };

        // Button cooldown to prevent multiple presses
        private float buttonCooldown = 0f;
        private const float COOLDOWN_TIME = 0.3f;

        // Screen dimensions
        private int screenWidth, screenHeight;

        // Start button position and size
        private Rectangle startButtonRect;

        // Wave manager
        private WaveManager waveManager;

        // Campfire position (center of screen)
        private Vector2 campfirePosition;

        // Campfire health
        private float campfireHealth = 100f;
        private float campfireMaxHealth = 100f;
        private bool campfireVulnerable = false; // Only vulnerable during gameplay

        // Upgrade selection
        private int selectedUpgradeOption = 0;

        // Upgrade options - now all upgrades are team upgrades
        private List<string> upgradeOptions = new List<string>
        {
            "All Players: Pistols",
            "All Players: Shotguns",
            "All Players: Rifles",
            "All Players: Cannons",
            "Team: Damage+",
            "Team: Speed+",
            "Team: Fire Rate+",
            "Team: Health+",
            "Team: Heal Campfire"
        };

        // Available upgrades - will be filtered based on what players already have
        private List<string> availableUpgrades = new List<string>();

        // Game over timer
        private float gameOverTimer = 5.0f;

        // Tiled map
        private TileMap tileMap;

        // Camera for the game view
        private Camera2D camera;

        // Fullscreen support
        private bool isFullscreen = true; // Start in fullscreen by default
        private int windowedWidth = 1366;
        private int windowedHeight = 768;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // Configure initial graphics settings
            SetGraphicsMode(isFullscreen);

            // Initialize camera
            camera = new Camera2D(GraphicsDevice.Viewport);

            base.Initialize();
        }

        private void SetGraphicsMode(bool fullscreen)
        {
            if (fullscreen)
            {
                // Get the current display mode for fullscreen
                var displayMode = GraphicsDevice.Adapter.CurrentDisplayMode;
                _graphics.IsFullScreen = true;
                _graphics.PreferredBackBufferWidth = displayMode.Width;
                _graphics.PreferredBackBufferHeight = displayMode.Height;
            }
            else
            {
                // Use windowed mode
                _graphics.IsFullScreen = false;
                _graphics.PreferredBackBufferWidth = windowedWidth;
                _graphics.PreferredBackBufferHeight = windowedHeight;
            }

            _graphics.ApplyChanges();

            // Store screen dimensions
            screenWidth = _graphics.PreferredBackBufferWidth;
            screenHeight = _graphics.PreferredBackBufferHeight;

            Console.WriteLine($"[Game] Display mode: {(fullscreen ? "Fullscreen" : "Windowed")} {screenWidth}x{screenHeight}");

            // Update zoom if map is loaded
            if (tileMap != null)
            {
                SetOptimalZoom();
            }
        }

        private void UpdateStartButtonPosition()
        {
            // Set start button position (center of screen for menu)
            // Scale button size based on screen resolution
            int buttonWidth = Math.Min(300, screenWidth / 4);
            int buttonHeight = Math.Min(80, screenHeight / 12);

            startButtonRect = new Rectangle(
                screenWidth / 2 - buttonWidth / 2,
                screenHeight / 2 - buttonHeight / 2,
                buttonWidth,
                buttonHeight
            );
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Create our bitmap font renderer
            fontRenderer = new BitmapFontRenderer(GraphicsDevice);

            // Load the Tiled map
            tileMap = new TileMap();

            // Add debug information
            Console.WriteLine("Loading map: Testkarta");
            string mapPath = Path.Combine(Content.RootDirectory, "Testkarta.tmx");
            Console.WriteLine($"Map path: {mapPath}");
            Console.WriteLine($"File exists: {File.Exists(mapPath)}");

            try
            {
                tileMap.LoadContent(Content, "Testkarta");
                Console.WriteLine($"Map loaded successfully! Size: {tileMap.WidthInPixels}x{tileMap.HeightInPixels} pixels");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading map: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw; // Re-throw to see the error
            }

            // Set optimal camera zoom based on screen size and map size
            SetOptimalZoom();

            // Set campfire position to center of the map
            campfirePosition = new Vector2(tileMap.WidthInPixels / 2, tileMap.HeightInPixels / 2);

            // Create a simple circle texture
            circleTexture = CreateCircleTexture(40);

            // Create a projectile texture (smaller circle)
            projectileTexture = CreateCircleTexture(10);

            // Create a simple campfire texture
            campfireTexture = CreateCampfireTexture(60);

            // Create a gray overlay texture
            grayOverlayTexture = new Texture2D(GraphicsDevice, 1, 1);
            grayOverlayTexture.SetData(new[] { new Color((byte)128, (byte)128, (byte)128, (byte)128) });

            // Create start button texture
            startButtonTexture = new Texture2D(GraphicsDevice, 1, 1);
            startButtonTexture.SetData(new[] { Color.White });

            // Set start button position
            UpdateStartButtonPosition();

            // Create players with different colors in their respective corners
            Color[] playerColors = { Color.Red, Color.Green, Color.Blue, Color.Yellow };

            // Player starting positions at the corners of the map with padding
            int padding = 100;
            Vector2[] playerCorners = new Vector2[4] {
                new Vector2(padding, tileMap.HeightInPixels - padding),  // Red - bottom left
                new Vector2(tileMap.WidthInPixels - padding, tileMap.HeightInPixels - padding),  // Green - bottom right
                new Vector2(tileMap.WidthInPixels - padding, padding),  // Blue - top right
                new Vector2(padding, padding)  // Yellow - top left
            };

            for (int i = 0; i < 4; i++)
            {
                // Create players in their corners
                players.Add(new Player(playerCorners[i], playerColors[i], i, tileMap.WidthInPixels, tileMap.HeightInPixels, circleTexture));
            }

            // Create wave manager
            waveManager = new WaveManager(tileMap.WidthInPixels, tileMap.HeightInPixels, circleTexture);

            // Setup spawn points around the edges of the map
            waveManager.SetupSpawnPoints(tileMap);
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // Toggle fullscreen with Alt+Enter or F11
            if (InputManager.IsKeyPressed(Keys.F11) ||
                (InputManager.IsKeyPressed(Keys.Enter) && (InputManager.IsKeyDown(Keys.LeftAlt) || InputManager.IsKeyDown(Keys.RightAlt))))
            {
                isFullscreen = !isFullscreen;
                SetGraphicsMode(isFullscreen);

                // Update the camera viewport after resolution change
                camera = new Camera2D(GraphicsDevice.Viewport);

                // Update UI positions after resolution change
                UpdateStartButtonPosition();

                // Recalculate optimal zoom for new resolution
                if (tileMap != null)
                {
                    SetOptimalZoom();
                    // Reset camera position to center
                    camera.Position = campfirePosition;
                }
            }

            // Zoom mode controls (F keys)
            if (InputManager.IsKeyPressed(Keys.F2))
            {
                currentZoomMode = ZoomMode.FitMap;
                SetOptimalZoom();
                Console.WriteLine("[Camera] Switched to FitMap mode - Entire map visible");
            }
            else if (InputManager.IsKeyPressed(Keys.F3))
            {
                currentZoomMode = ZoomMode.FillScreen;
                SetOptimalZoom();
                Console.WriteLine("[Camera] Switched to FillScreen mode - No black bars");
            }
            else if (InputManager.IsKeyPressed(Keys.F4))
            {
                currentZoomMode = ZoomMode.FixedZoom;
                SetOptimalZoom();
                Console.WriteLine($"[Camera] Switched to FixedZoom mode - Zoom level: {fixedZoomLevel}");
            }

            // Manual zoom controls (+ and - keys)
            if (currentZoomMode == ZoomMode.FixedZoom)
            {
                if (InputManager.IsKeyPressed(Keys.OemPlus) || InputManager.IsKeyPressed(Keys.Add))
                {
                    fixedZoomLevel = Math.Min(fixedZoomLevel + 0.5f, 10.0f);
                    SetOptimalZoom();
                    Console.WriteLine($"[Camera] Zoom increased to {fixedZoomLevel}");
                }
                else if (InputManager.IsKeyPressed(Keys.OemMinus) || InputManager.IsKeyPressed(Keys.Subtract))
                {
                    fixedZoomLevel = Math.Max(fixedZoomLevel - 0.5f, 0.5f);
                    SetOptimalZoom();
                    Console.WriteLine($"[Camera] Zoom decreased to {fixedZoomLevel}");
                }
            }

            // Update input manager
            InputManager.Update();

            // Update the tile map
            tileMap.Update(gameTime);

            // Update button cooldown
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            buttonCooldown -= deltaTime;

            switch (currentState)
            {
                case GameState.Menu:
                    UpdateMenu(gameTime);
                    break;

                case GameState.Playing:
                    UpdatePlaying(gameTime);

                    // Update camera to follow active players or campfire
                    UpdateCamera();
                    break;

                case GameState.Upgrading:
                    UpdateUpgrading(gameTime);
                    break;

                case GameState.GameOver:
                    UpdateGameOver(gameTime);
                    break;
            }

            base.Update(gameTime);
        }

        private void UpdateCamera()
        {
            // Default to campfire
            Vector2 targetPosition = campfirePosition;

            // If we have active players, center on their average position
            List<Vector2> activePlayers = new List<Vector2>();
            foreach (var player in players)
            {
                int playerIndex = players.IndexOf(player);
                if (joinedPlayers[playerIndex] && player.IsAlive)
                {
                    activePlayers.Add(player.Position);
                }
            }

            if (activePlayers.Count > 0)
            {
                Vector2 sum = Vector2.Zero;
                foreach (var pos in activePlayers)
                {
                    sum += pos;
                }
                targetPosition = sum / activePlayers.Count;
            }

            // Update camera position with slight smoothing
            camera.Position = Vector2.Lerp(camera.Position, targetPosition, 0.1f);

            // Make sure camera doesn't show beyond map bounds
            camera.LimitToBounds(0, 0, tileMap.WidthInPixels, tileMap.HeightInPixels);
        }

        // Zoom behavior options
        public enum ZoomMode
        {
            FitMap,        // Entire map visible, may have black bars
            FillScreen,    // No black bars, map may be cut off
            FixedZoom      // Use a fixed zoom level
        }

        private ZoomMode currentZoomMode = ZoomMode.FillScreen;
        private float fixedZoomLevel = 3.0f; // Used when ZoomMode is FixedZoom

        private void SetOptimalZoom()
        {
            float scaleX = screenWidth / (float)tileMap.WidthInPixels;
            float scaleY = screenHeight / (float)tileMap.HeightInPixels;

            switch (currentZoomMode)
            {
                case ZoomMode.FitMap:
                    // Show entire map, may have black bars
                    camera.Zoom = Math.Min(scaleX, scaleY) * 0.95f;
                    break;

                case ZoomMode.FillScreen:
                    // Fill screen completely, map may be cut off
                    camera.Zoom = Math.Max(scaleX, scaleY);
                    break;

                case ZoomMode.FixedZoom:
                    // Use a fixed zoom level
                    camera.Zoom = fixedZoomLevel;
                    break;
            }

            // Set minimum zoom so the game doesn't get too small
            camera.Zoom = Math.Max(camera.Zoom, 1.0f);

            Console.WriteLine($"[Camera] {currentZoomMode} - Set zoom to {camera.Zoom:F2} (Screen: {screenWidth}x{screenHeight}, Map: {tileMap.WidthInPixels}x{tileMap.HeightInPixels})");
        }

        private void UpdateMenu(GameTime gameTime)
        {
            // Reset campfire health when in menu
            campfireHealth = campfireMaxHealth;
            campfireVulnerable = false;

            // Check for player joins using A button for each player
            for (int i = 0; i < 4; i++)
            {
                if (InputManager.IsButtonPressed(i, Buttons.A) && buttonCooldown <= 0)
                {
                    buttonCooldown = COOLDOWN_TIME;
                    joinedPlayers[i] = !joinedPlayers[i]; // Toggle joined status
                    System.Diagnostics.Debug.WriteLine($"Player {i} joined: {joinedPlayers[i]}");
                }
            }

            // Start game with menu button (Space, Enter, etc. as configured in InputManager)
            if (InputManager.ShouldStartGame() && buttonCooldown <= 0)
            {
                buttonCooldown = COOLDOWN_TIME;
                StartGame();
            }

            // Debug override - F1 key forces game start
            if (Keyboard.GetState().IsKeyDown(Keys.F1) && buttonCooldown <= 0)
            {
                buttonCooldown = COOLDOWN_TIME;
                StartGame();
            }
        }

        private void UpdatePlaying(GameTime gameTime)
        {
            // Make campfire vulnerable during gameplay
            campfireVulnerable = true;

            // Check for new players joining during gameplay
            for (int i = 0; i < 4; i++)
            {
                if (InputManager.IsButtonPressed(i, Buttons.A) && buttonCooldown <= 0 && !joinedPlayers[i])
                {
                    buttonCooldown = COOLDOWN_TIME;
                    joinedPlayers[i] = true; // Allow players to join during gameplay
                    System.Diagnostics.Debug.WriteLine($"Player {i} joined during gameplay");
                }
            }

            // Update only joined players and handle shooting
            for (int i = 0; i < players.Count; i++)
            {
                if (joinedPlayers[i] && players[i].IsAlive)
                {
                    // Store old position for collision detection
                    Vector2 oldPosition = players[i].Position;

                    // Update player
                    players[i].Update(gameTime);

                    // Check collision with the map
                    if (tileMap.CheckCollision(players[i].Position, players[i].Radius))
                    {
                        // If player collides with the map, revert to old position
                        players[i].ResetPosition(oldPosition);
                    }

                    // Handle shooting
                    if (InputManager.IsFirePressed(i))
                    {
                        List<Projectile> newProjectiles;
                        if (players[i].TryFire(out newProjectiles, projectileTexture))
                        {
                            projectiles.AddRange(newProjectiles);
                        }
                    }
                }
            }

            // Update projectiles
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                Vector2 oldPosition = projectiles[i].Position;
                projectiles[i].Update(gameTime);

                // Check collision with the map
                if (tileMap.CheckCollision(projectiles[i].Position, projectiles[i].Radius))
                {
                    // If projectile hits the map, deactivate it
                    projectiles[i].Deactivate();
                }

                // Remove inactive projectiles
                if (!projectiles[i].IsActive)
                {
                    projectiles.RemoveAt(i);
                }
            }

            // Update wave manager
            waveManager.Update(gameTime, enemies, campfirePosition);

            // Update enemies
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (enemies[i].IsActive)
                {
                    // Store old position for collision detection
                    Vector2 oldPosition = enemies[i].Position;

                    // Find closest player
                    Vector2 targetPosition = FindClosestPlayerPosition(enemies[i].Position);

                    // Update enemy with target
                    enemies[i].Update(gameTime, targetPosition);

                    // Check collision with the map
                    if (tileMap.CheckCollision(enemies[i].Position, enemies[i].Radius))
                    {
                        // If enemy collides with the map, revert to old position
                        // and try to move along the wall
                        Vector2 moveDir = enemies[i].Direction;

                        // Try moving only horizontally
                        Vector2 horizontalMove = oldPosition + new Vector2(moveDir.X, 0) * 5;
                        if (!tileMap.CheckCollision(horizontalMove, enemies[i].Radius))
                        {
                            enemies[i].SetPosition(horizontalMove);
                        }
                        // Try moving only vertically
                        else
                        {
                            Vector2 verticalMove = oldPosition + new Vector2(0, moveDir.Y) * 5;
                            if (!tileMap.CheckCollision(verticalMove, enemies[i].Radius))
                            {
                                enemies[i].SetPosition(verticalMove);
                            }
                            else
                            {
                                // If both failed, just revert to old position
                                enemies[i].SetPosition(oldPosition);
                            }
                        }
                    }

                    // Check for collision with players
                    CheckEnemyPlayerCollisions(enemies[i]);

                    // Check for collision with campfire
                    CheckEnemyCampfireCollision(enemies[i]);

                    // For shooter enemies, handle shooting
                    if (enemies[i].Type == EnemyType.Shooter && enemies[i].CanAttack())
                    {
                        // Find direction to closest player
                        Vector2 targetPlayer = FindClosestPlayerPosition(enemies[i].Position);
                        Vector2 direction = targetPlayer - enemies[i].Position;
                        if (direction != Vector2.Zero)
                        {
                            direction.Normalize();
                        }

                        // Create enemy projectile
                        Projectile enemyProjectile = new Projectile(
                            enemies[i].Position,
                            direction,
                            enemies[i].DamageAmount,
                            200f, // Speed
                            Color.Purple,
                            projectileTexture
                        );

                        projectiles.Add(enemyProjectile);
                    }
                }
                else
                {
                    // Remove inactive enemies
                    enemies.RemoveAt(i);
                }
            }

            // Check for projectile collisions
            CheckProjectileCollisions();

            // Check if wave is complete and no wave is active/preparing
            if (!waveManager.WaveActive && !waveManager.WavePreparing)
            {
                // Start upgrading phase
                StartUpgradingPhase();

                // Heal the campfire a bit between waves
                campfireHealth = Math.Min(campfireMaxHealth, campfireHealth + 10);
            }

            // Check if all players are dead
            if (AllPlayersAreDead() || campfireHealth <= 0)
            {
                currentState = GameState.GameOver;
                gameOverTimer = 5.0f;
            }
        }

        private void CheckEnemyCampfireCollision(Enemy enemy)
        {
            if (!campfireVulnerable) return;

            float distanceSquared = Vector2.DistanceSquared(
                enemy.Position,
                campfirePosition
            );

            float radiusSum = enemy.Radius + (campfireTexture.Width / 2);

            if (distanceSquared < radiusSum * radiusSum)
            {
                // Enemy collided with campfire
                campfireHealth -= enemy.DamageAmount * 0.2f;
                if (campfireHealth < 0) campfireHealth = 0;

                // Push enemy away slightly
                Vector2 pushDir = enemy.Position - campfirePosition;
                if (pushDir != Vector2.Zero)
                {
                    pushDir.Normalize();
                    enemy.TakeDamage(1.0f); // Damage enemy slightly when colliding with campfire
                    enemy.SetPosition(enemy.Position + pushDir * 5); // Push enemy away
                }
            }
        }

        private void UpdateUpgrading(GameTime gameTime)
        {
            // During upgrading, campfire is not vulnerable
            campfireVulnerable = false;

            // If there are no available upgrades, just start the next wave
            if (availableUpgrades.Count == 0)
            {
                currentState = GameState.Playing;
                waveManager.StartNextWave();
                return;
            }

            // Handle upgrade selection
            if (buttonCooldown <= 0)
            {
                // Navigate up/down through options
                if (Keyboard.GetState().IsKeyDown(Keys.Up) || InputManager.IsButtonDown(0, Buttons.DPadUp))
                {
                    buttonCooldown = COOLDOWN_TIME;
                    selectedUpgradeOption = (selectedUpgradeOption - 1 + availableUpgrades.Count) % availableUpgrades.Count;
                }
                else if (Keyboard.GetState().IsKeyDown(Keys.Down) || InputManager.IsButtonDown(0, Buttons.DPadDown))
                {
                    buttonCooldown = COOLDOWN_TIME;
                    selectedUpgradeOption = (selectedUpgradeOption + 1) % availableUpgrades.Count;
                }

                // Select upgrade - any player can confirm the team upgrade
                bool selectPressed = false;
                for (int i = 0; i < players.Count; i++)
                {
                    if (joinedPlayers[i] && players[i].IsAlive &&
                        (InputManager.IsButtonPressed(i, Buttons.A) || Keyboard.GetState().IsKeyDown(Keys.Space)))
                    {
                        selectPressed = true;
                        break;
                    }
                }

                if (selectPressed && buttonCooldown <= 0)
                {
                    buttonCooldown = COOLDOWN_TIME;
                    ApplyTeamUpgrade(selectedUpgradeOption);

                    // Move directly to next wave after upgrade
                    currentState = GameState.Playing;
                    waveManager.StartNextWave();
                }
            }
        }

        // Method to update available upgrades based on current player states
        private void UpdateAvailableUpgrades()
        {
            availableUpgrades.Clear();

            // Check which weapons are already owned by all active players
            bool allHavePistols = true;
            bool allHaveShotguns = true;
            bool allHaveRifles = true;
            bool allHaveCannons = true;

            int activePlayerCount = 0;

            // Check which weapons every active player already has
            for (int i = 0; i < players.Count; i++)
            {
                if (joinedPlayers[i] && players[i].IsAlive)
                {
                    activePlayerCount++;
                    GunType currentGun = players[i].CurrentGun;

                    if (currentGun != GunType.Pistol) allHavePistols = false;
                    if (currentGun != GunType.Shotgun) allHaveShotguns = false;
                    if (currentGun != GunType.Rifle) allHaveRifles = false;
                    if (currentGun != GunType.Cannon) allHaveCannons = false;
                }
            }

            // Only add weapon upgrades if not all players have them already
            if (!allHavePistols)
                availableUpgrades.Add("All Players: Pistols");
            if (!allHaveShotguns)
                availableUpgrades.Add("All Players: Shotguns");
            if (!allHaveRifles)
                availableUpgrades.Add("All Players: Rifles");
            if (!allHaveCannons)
                availableUpgrades.Add("All Players: Cannons");

            // Always add stat upgrades
            availableUpgrades.Add("Team: Damage+");
            availableUpgrades.Add("Team: Speed+");
            availableUpgrades.Add("Team: Fire Rate+");
            availableUpgrades.Add("Team: Health+");

            // Only add Heal Campfire if campfire health is less than max
            if (campfireHealth < campfireMaxHealth)
                availableUpgrades.Add("Team: Heal Campfire");
        }

        private void UpdateGameOver(GameTime gameTime)
        {
            gameOverTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (gameOverTimer <= 0 || Keyboard.GetState().IsKeyDown(Keys.Space))
            {
                // Reset game
                ResetGame();
                currentState = GameState.Menu;
            }
        }

        private void ApplyTeamUpgrade(int upgradeIndex)
        {
            // Apply the selected upgrade from the available upgrades list
            string upgradeName = availableUpgrades[upgradeIndex];

            // Apply upgrade to all active players
            foreach (var player in players)
            {
                if (upgradeName == "All Players: Pistols")
                {
                    player.UpgradeGun(GunType.Pistol);
                }
                else if (upgradeName == "All Players: Shotguns")
                {
                    player.UpgradeGun(GunType.Shotgun);
                }
                else if (upgradeName == "All Players: Rifles")
                {
                    player.UpgradeGun(GunType.Rifle);
                }
                else if (upgradeName == "All Players: Cannons")
                {
                    player.UpgradeGun(GunType.Cannon);
                }
                else if (upgradeName == "Team: Damage+")
                {
                    player.UpgradeStat("damage", 0.2f);
                }
                else if (upgradeName == "Team: Speed+")
                {
                    player.UpgradeStat("speed", 0.2f);
                }
                else if (upgradeName == "Team: Fire Rate+")
                {
                    player.UpgradeStat("fireRate", 0.2f);
                }
                else if (upgradeName == "Team: Health+")
                {
                    player.UpgradeStat("health", 0.2f);
                }
            }

            // Handle campfire specific upgrade
            if (upgradeName == "Team: Heal Campfire")
            {
                campfireHealth = Math.Min(campfireMaxHealth, campfireHealth + 30);
            }
        }

        private void CheckProjectileCollisions()
        {
            // Check for projectile-enemy collisions
            for (int p = projectiles.Count - 1; p >= 0; p--)
            {
                Projectile projectile = projectiles[p];
                if (!projectile.IsActive) continue;

                // Check for projectile-campfire collisions (enemy projectiles can damage campfire)
                if (!IsPlayerProjectile(projectile) && campfireVulnerable)
                {
                    float distanceSquared = Vector2.DistanceSquared(
                        projectile.Position,
                        campfirePosition
                    );

                    float radiusSum = projectile.Radius + (campfireTexture.Width / 2);

                    if (distanceSquared < radiusSum * radiusSum)
                    {
                        // Hit campfire
                        campfireHealth -= projectile.Damage * 0.5f;
                        if (campfireHealth < 0) campfireHealth = 0;
                        projectile.Deactivate();
                        continue;
                    }
                }

                // Player projectiles (colored as player colors) hit enemies
                if (IsPlayerProjectile(projectile))
                {
                    for (int e = 0; e < enemies.Count; e++)
                    {
                        if (!enemies[e].IsActive) continue;

                        float distanceSquared = Vector2.DistanceSquared(
                            projectile.Position,
                            enemies[e].Position
                        );

                        float radiusSum = projectile.Radius + enemies[e].Radius;

                        if (distanceSquared < radiusSum * radiusSum)
                        {
                            // Hit an enemy
                            enemies[e].TakeDamage(projectile.Damage);
                            projectile.Deactivate();
                            break;
                        }
                    }
                }
                // Enemy projectiles (colored purple) hit players
                else
                {
                    for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
                    {
                        if (!joinedPlayers[playerIndex] || !players[playerIndex].IsAlive) continue;

                        float distanceSquared = Vector2.DistanceSquared(
                            projectile.Position,
                            players[playerIndex].Position
                        );

                        float radiusSum = projectile.Radius + players[playerIndex].Radius;

                        if (distanceSquared < radiusSum * radiusSum)
                        {
                            // Hit a player
                            players[playerIndex].TakeDamage(projectile.Damage);
                            projectile.Deactivate();
                            break;
                        }
                    }
                }
            }
        }

        private bool IsPlayerProjectile(Projectile projectile)
        {
            // Check if projectile color is purple (enemy projectile)
            if (projectile.Color.R == Color.Purple.R &&
                projectile.Color.G == Color.Purple.G &&
                projectile.Color.B == Color.Purple.B)
            {
                return false;
            }
            return true;
        }

        private void CheckEnemyPlayerCollisions(Enemy enemy)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (!joinedPlayers[i] || !players[i].IsAlive) continue;

                float distanceSquared = Vector2.DistanceSquared(
                    enemy.Position,
                    players[i].Position
                );

                float radiusSum = enemy.Radius + players[i].Radius;

                if (distanceSquared < radiusSum * radiusSum)
                {
                    // Enemy collided with player
                    players[i].TakeDamage(enemy.DamageAmount * 0.5f);

                    // Push player away slightly
                    Vector2 pushDir = players[i].Position - enemy.Position;
                    if (pushDir != Vector2.Zero)
                    {
                        pushDir.Normalize();
                        players[i].ResetPosition(players[i].Position + pushDir * 5);
                    }
                }
            }
        }

        private Vector2 FindClosestPlayerPosition(Vector2 fromPosition)
        {
            Vector2 closestPosition = campfirePosition; // Default to campfire
            float closestDistance = Vector2.DistanceSquared(fromPosition, campfirePosition);

            for (int i = 0; i < players.Count; i++)
            {
                if (!joinedPlayers[i] || !players[i].IsAlive) continue;

                float distance = Vector2.DistanceSquared(fromPosition, players[i].Position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPosition = players[i].Position;
                }
            }

            return closestPosition;
        }

        private bool AllPlayersAreDead()
        {
            bool anyPlayerAlive = false;

            for (int i = 0; i < players.Count; i++)
            {
                if (joinedPlayers[i] && players[i].IsAlive)
                {
                    anyPlayerAlive = true;
                    break;
                }
            }

            return !anyPlayerAlive;
        }

        private void StartUpgradingPhase()
        {
            currentState = GameState.Upgrading;
            selectedUpgradeOption = 0;

            // Filter the available upgrades when entering the upgrade phase
            UpdateAvailableUpgrades();
        }

        private void StartGame()
        {
            currentState = GameState.Playing;
            System.Diagnostics.Debug.WriteLine("Game started!");

            // Reset campfire health
            campfireHealth = campfireMaxHealth;

            // If no players have joined, make the first player active
            if (!AnyPlayersJoined())
            {
                joinedPlayers[0] = true;
            }

            // Reset positions to be around the campfire for all joined players
            for (int i = 0; i < players.Count; i++)
            {
                if (joinedPlayers[i])
                {
                    // Position players in a circle around the campfire
                    float angle = i * MathHelper.TwoPi / 4;
                    Vector2 offset = new Vector2(
                        (float)Math.Cos(angle) * 150,
                        (float)Math.Sin(angle) * 150
                    );
                    players[i].ResetPosition(campfirePosition + offset);
                }
            }

            // Set initial camera position
            camera.Position = campfirePosition;

            // Start first wave
            waveManager.StartNextWave();
        }

        private void ResetGame()
        {
            // Clear enemies and projectiles
            enemies.Clear();
            projectiles.Clear();

            // Reset campfire health
            campfireHealth = campfireMaxHealth;
            campfireVulnerable = false;

            // Reset player states
            for (int i = 0; i < players.Count; i++)
            {
                // Reset position to corners
                int padding = 100;
                Vector2[] playerCorners = new Vector2[4] {
                    new Vector2(padding, tileMap.HeightInPixels - padding),  // Red - bottom left
                    new Vector2(tileMap.WidthInPixels - padding, tileMap.HeightInPixels - padding),  // Green - bottom right
                    new Vector2(tileMap.WidthInPixels - padding, padding),  // Blue - top right
                    new Vector2(padding, padding)  // Yellow - top left
                };

                players[i] = new Player(playerCorners[i], players[i].Color, i, tileMap.WidthInPixels, tileMap.HeightInPixels, circleTexture);
            }

            // Reset joined players
            joinedPlayers = new bool[4] { false, false, false, false };

            // Create new wave manager
            waveManager = new WaveManager(tileMap.WidthInPixels, tileMap.HeightInPixels, circleTexture);
            waveManager.SetupSpawnPoints(tileMap);
        }

        // Helper method to check if any players have joined
        private bool AnyPlayersJoined()
        {
            for (int i = 0; i < joinedPlayers.Length; i++)
            {
                if (joinedPlayers[i])
                    return true;
            }
            return false;
        }

        protected override void Draw(GameTime gameTime)
        {
            // Use a dark forest green background instead of black
            GraphicsDevice.Clear(new Color(20, 40, 20));

            // For gameplay and upgrading, use camera transform
            if (currentState == GameState.Playing)
            {
                // Draw game world with camera transform
                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, camera.GetViewMatrix());

                // Draw the tile map
                tileMap.Draw(_spriteBatch);

                // Draw the campfire in the middle
                _spriteBatch.Draw(
                    campfireTexture,
                    campfirePosition,
                    null,
                    Color.White,
                    0,
                    new Vector2(campfireTexture.Width / 2, campfireTexture.Height / 2),
                    1.0f,
                    SpriteEffects.None,
                    0
                );

                // Draw all enemies
                foreach (var enemy in enemies)
                {
                    enemy.Draw(_spriteBatch);
                }

                // Draw all projectiles
                foreach (var projectile in projectiles)
                {
                    projectile.Draw(_spriteBatch);
                }

                // Draw players that have joined
                for (int i = 0; i < players.Count; i++)
                {
                    if (joinedPlayers[i] && players[i].IsAlive)
                    {
                        players[i].Draw(_spriteBatch);

                        // Draw player number (P1, P2, etc.)
                        fontRenderer.DrawTextCentered(
                            _spriteBatch,
                            $"P{i + 1}",
                            new Vector2(players[i].Position.X, players[i].Position.Y + 50),
                            players[i].Color,
                            1.0f
                        );
                    }
                }

                _spriteBatch.End();

                // Draw UI elements without camera transform
                _spriteBatch.Begin();

                // Draw campfire health bar if in playing state
                if (currentState == GameState.Playing)
                {
                    // Calculate health percentage
                    float healthPercentage = campfireHealth / campfireMaxHealth;
                    int healthBarWidth = 120;
                    int healthBarHeight = 10;

                    // Draw health bar above campfire
                    // Background (red)
                    _spriteBatch.Draw(
                        startButtonTexture, // Reuse texture as a pixel
                        new Rectangle(
                            (int)(screenWidth / 2 - healthBarWidth / 2),
                            20,
                            healthBarWidth,
                            healthBarHeight
                        ),
                        Color.Red
                    );

                    // Foreground (green) - shows current health
                    _spriteBatch.Draw(
                        startButtonTexture, // Reuse texture as a pixel
                        new Rectangle(
                            (int)(screenWidth / 2 - healthBarWidth / 2),
                            20,
                            (int)(healthBarWidth * healthPercentage),
                            healthBarHeight
                        ),
                        Color.Green
                    );

                    // Draw "Campfire" text
                    fontRenderer.DrawTextCentered(
                        _spriteBatch,
                        "CAMPFIRE",
                        new Vector2(screenWidth / 2, 10),
                        Color.White,
                        1.0f
                    );

                    // Draw health value
                    fontRenderer.DrawTextCentered(
                        _spriteBatch,
                        $"HP: {(int)campfireHealth}/{(int)campfireMaxHealth}",
                        new Vector2(screenWidth / 2, 40),
                        Color.White,
                        1.0f
                    );
                }

                // Draw wave information
                waveManager.Draw(_spriteBatch, fontRenderer);

                // Display controls information
                fontRenderer.DrawTextCentered(
                    _spriteBatch,
                    "MOVE: LEFT STICK/WASD - SHOOT: TRIGGER/SPACE",
                    new Vector2(screenWidth / 2, screenHeight - 70),
                    Color.White,
                    1.0f
                );

                // Add hint about auto-aiming
                fontRenderer.DrawTextCentered(
                    _spriteBatch,
                    "AUTO-AIM: SHOOTS IN MOVEMENT DIRECTION",
                    new Vector2(screenWidth / 2, screenHeight - 40),
                    Color.White,
                    1.0f
                );

                // Show current zoom mode and controls
                string zoomInfo = $"ZOOM: {currentZoomMode} ({camera.Zoom:F1}x) - F2: FIT MAP | F3: FILL SCREEN | F4: FIXED ZOOM";
                if (currentZoomMode == ZoomMode.FixedZoom)
                {
                    zoomInfo += " | +/-: ADJUST";
                }
                fontRenderer.DrawTextCentered(
                    _spriteBatch,
                    zoomInfo,
                    new Vector2(screenWidth / 2, screenHeight - 10),
                    Color.Gray,
                    0.8f
                );

                _spriteBatch.End();
            }
            else
            {
                // For menu, upgrading, and game over, use regular drawing
                _spriteBatch.Begin();

                switch (currentState)
                {
                    case GameState.Menu:
                        DrawMenu();
                        break;

                    case GameState.Upgrading:
                        DrawUpgrading();
                        break;

                    case GameState.GameOver:
                        DrawGameOver();
                        break;
                }

                _spriteBatch.End();
            }

            base.Draw(gameTime);
        }

        private void DrawMenu()
        {
            // Draw menu title
            fontRenderer.DrawTextCentered(
                _spriteBatch,
                "CAMPFIRE SURVIVAL",
                new Vector2(screenWidth / 2, 80),
                Color.White,
                2.0f
            );

            // Draw instructions
            fontRenderer.DrawTextCentered(
                _spriteBatch,
                "PRESS A TO JOIN/LEAVE - GREEN PLAYER'S R2 OR SPACE TO START",
                new Vector2(screenWidth / 2, screenHeight - 80),
                Color.White,
                1.0f
            );

            // Draw fullscreen toggle instruction
            fontRenderer.DrawTextCentered(
                _spriteBatch,
                "F11 OR ALT+ENTER: TOGGLE FULLSCREEN",
                new Vector2(screenWidth / 2, screenHeight - 40),
                Color.Gray,
                0.8f
            );

            // Draw campfire in the middle
            _spriteBatch.Draw(
                campfireTexture,
                campfirePosition,
                null,
                Color.White,
                0,
                new Vector2(campfireTexture.Width / 2, campfireTexture.Height / 2),
                1.0f,
                SpriteEffects.None,
                0
            );

            // In menu state, draw all players (joined or grayed out)
            for (int i = 0; i < players.Count; i++)
            {
                if (joinedPlayers[i])
                {
                    // Draw joined players normally
                    players[i].Draw(_spriteBatch);

                    // Draw "READY" text above joined players
                    fontRenderer.DrawTextCentered(
                        _spriteBatch,
                        "READY",
                        new Vector2(players[i].Position.X, players[i].Position.Y - 60),
                        players[i].Color,
                        1.0f
                    );
                }
                else
                {
                    // Draw non-joined players as grayed out
                    players[i].Draw(_spriteBatch, new Color(
                        players[i].Color.R,
                        players[i].Color.G,
                        players[i].Color.B,
                        (byte)100));

                    // Draw "PRESS A" text above inactive players
                    fontRenderer.DrawTextCentered(
                        _spriteBatch,
                        "PRESS A",
                        new Vector2(players[i].Position.X, players[i].Position.Y - 60),
                        new Color(players[i].Color.R, players[i].Color.G, players[i].Color.B, (byte)100),
                        1.0f
                    );
                }

                // Draw player number (P1, P2, etc.)
                string playerText = $"P{i + 1}";
                Color playerTextColor = joinedPlayers[i] ?
                    players[i].Color :
                    new Color(players[i].Color.R, players[i].Color.G, players[i].Color.B, (byte)100);

                fontRenderer.DrawTextCentered(
                    _spriteBatch,
                    playerText,
                    new Vector2(players[i].Position.X, players[i].Position.Y + 50),
                    playerTextColor,
                    1.0f
                );
            }

            // Draw start button
            _spriteBatch.Draw(startButtonTexture, startButtonRect, Color.DarkGreen);

            // Draw "START GAME" text on the button
            fontRenderer.DrawTextCentered(
                _spriteBatch,
                "START GAME",
                new Vector2(startButtonRect.X + startButtonRect.Width / 2, startButtonRect.Y + startButtonRect.Height / 2),
                Color.White,
                1.2f
            );
        }

        private void DrawUpgrading()
        {
            // Draw a semi-transparent overlay to hide the game area
            _spriteBatch.Draw(
                startButtonTexture,
                new Rectangle(0, 0, screenWidth, screenHeight),
                new Color(0, 0, 0, 150)
            );

            // Draw upgrade title
            fontRenderer.DrawTextCentered(
                _spriteBatch,
                "TEAM UPGRADE",
                new Vector2(screenWidth / 2, 80),
                Color.White,
                2.0f
            );

            // Draw wave completed message
            fontRenderer.DrawTextCentered(
                _spriteBatch,
                $"WAVE {waveManager.CurrentWave} COMPLETED!",
                new Vector2(screenWidth / 2, 130),
                Color.Yellow,
                1.5f
            );

            // Draw info about team upgrades
            fontRenderer.DrawTextCentered(
                _spriteBatch,
                "CHOOSE ONE UPGRADE FOR YOUR TEAM",
                new Vector2(screenWidth / 2, 170),
                Color.White,
                1.2f
            );

            // Draw current stats summary for active players
            if (AnyPlayersJoined())
            {
                // Find an active player to get stats from
                Player activePlayer = null;
                for (int i = 0; i < players.Count; i++)
                {
                    if (joinedPlayers[i] && players[i].IsAlive)
                    {
                        activePlayer = players[i];
                        break;
                    }
                }

                if (activePlayer != null)
                {
                    // Display current stats
                    fontRenderer.DrawTextCentered(
                        _spriteBatch,
                        $"CURRENT STATS: DMG x{activePlayer.DamageModifier:F1} | SPD x{activePlayer.SpeedModifier:F1} | RATE x{activePlayer.FireRateModifier:F1} | HP x{activePlayer.HealthModifier:F1}",
                        new Vector2(screenWidth / 2, 200),
                        Color.Cyan,
                        1.0f
                    );
                }
            }

            // Draw upgrade options
            int startY = 240; // Moved down to make room for stats
            int spacing = 35;

            // Check if we have any available upgrades
            if (availableUpgrades.Count == 0)
            {
                fontRenderer.DrawTextCentered(
                    _spriteBatch,
                    "NO UPGRADES AVAILABLE - CONTINUING TO NEXT WAVE",
                    new Vector2(screenWidth / 2, startY),
                    Color.Yellow,
                    1.2f
                );
            }
            else
            {
                for (int i = 0; i < availableUpgrades.Count; i++)
                {
                    string option = availableUpgrades[i];
                    Color textColor = (i == selectedUpgradeOption) ? Color.Yellow : Color.White;
                    string prefix = (i == selectedUpgradeOption) ? "> " : "  ";

                    // For stat upgrades, show the new value that would result
                    if (AnyPlayersJoined())
                    {
                        // Find an active player to reference
                        Player activePlayer = null;
                        for (int p = 0; p < players.Count; p++)
                        {
                            if (joinedPlayers[p] && players[p].IsAlive)
                            {
                                activePlayer = players[p];
                                break;
                            }
                        }

                        if (activePlayer != null)
                        {
                            if (option == "Team: Damage+")
                            {
                                float newValue = activePlayer.DamageModifier + 0.2f;
                                option = $"Team: Damage+ (x{activePlayer.DamageModifier:F1} → x{newValue:F1})";
                            }
                            else if (option == "Team: Speed+")
                            {
                                float newValue = activePlayer.SpeedModifier + 0.2f;
                                option = $"Team: Speed+ (x{activePlayer.SpeedModifier:F1} → x{newValue:F1})";
                            }
                            else if (option == "Team: Fire Rate+")
                            {
                                float newValue = activePlayer.FireRateModifier + 0.2f;
                                option = $"Team: Fire Rate+ (x{activePlayer.FireRateModifier:F1} → x{newValue:F1})";
                            }
                            else if (option == "Team: Health+")
                            {
                                float newValue = activePlayer.HealthModifier + 0.2f;
                                option = $"Team: Health+ (x{activePlayer.HealthModifier:F1} → x{newValue:F1})";
                            }
                            else if (option == "Team: Heal Campfire")
                            {
                                int newHealth = (int)Math.Min(campfireMaxHealth, campfireHealth + 30);
                                option = $"Team: Heal Campfire ({(int)campfireHealth} → {newHealth})";
                            }
                        }
                    }

                    fontRenderer.DrawTextCentered(
                        _spriteBatch,
                        prefix + option,
                        new Vector2(screenWidth / 2, startY + i * spacing),
                        textColor,
                        1.2f
                    );
                }
            }

            // Draw controls
            fontRenderer.DrawTextCentered(
                _spriteBatch,
                "UP/DOWN: SELECT - A/SPACE: CONFIRM",
                new Vector2(screenWidth / 2, screenHeight - 40),
                Color.White,
                1.0f
            );
        }

        private void DrawGameOver()
        {
            // Draw game over text
            fontRenderer.DrawTextCentered(
                _spriteBatch,
                "GAME OVER",
                new Vector2(screenWidth / 2, screenHeight / 2 - 150),
                Color.Red,
                3.0f
            );

            // Draw wave reached
            fontRenderer.DrawTextCentered(
                _spriteBatch,
                $"YOU SURVIVED {waveManager.CurrentWave} WAVES",
                new Vector2(screenWidth / 2, screenHeight / 2 - 50),
                Color.White,
                1.5f
            );

            // Display different message depending on what caused game over
            if (campfireHealth <= 0)
            {
                fontRenderer.DrawTextCentered(
                    _spriteBatch,
                    "THE CAMPFIRE WAS DESTROYED",
                    new Vector2(screenWidth / 2, screenHeight / 2),
                    Color.OrangeRed,
                    1.5f
                );
            }
            else
            {
                fontRenderer.DrawTextCentered(
                    _spriteBatch,
                    "ALL PLAYERS WERE DEFEATED",
                    new Vector2(screenWidth / 2, screenHeight / 2),
                    Color.OrangeRed,
                    1.5f
                );
            }

            // Draw restart message
            fontRenderer.DrawTextCentered(
                _spriteBatch,
                $"RESTARTING IN {(int)Math.Ceiling(gameOverTimer)}... (PRESS SPACE)",
                new Vector2(screenWidth / 2, screenHeight / 2 + 50),
                Color.White,
                1.2f
            );
        }

        // Create a simple circle texture
        private Texture2D CreateCircleTexture(int radius)
        {
            int diameter = radius * 2;
            Texture2D texture = new Texture2D(GraphicsDevice, diameter, diameter);
            Color[] colorData = new Color[diameter * diameter];

            float radiusSquared = radius * radius;

            for (int x = 0; x < diameter; x++)
            {
                for (int y = 0; y < diameter; y++)
                {
                    int index = x * diameter + y;
                    Vector2 pos = new Vector2(x - radius, y - radius);
                    if (pos.LengthSquared() <= radiusSquared)
                    {
                        colorData[index] = Color.White;
                    }
                    else
                    {
                        colorData[index] = Color.Transparent;
                    }
                }
            }

            texture.SetData(colorData);
            return texture;
        }

        // Create a simple campfire texture
        private Texture2D CreateCampfireTexture(int size)
        {
            Texture2D texture = new Texture2D(GraphicsDevice, size, size);
            Color[] colorData = new Color[size * size];

            // Create a more visible campfire (orange/red circle with inner yellow)
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    int index = x * size + y;

                    // Calculate distance from center
                    Vector2 pos = new Vector2(x - size / 2, y - size / 2);
                    float distance = pos.Length();

                    if (distance < size / 2)
                    {
                        // Inner part is yellow, outer part is orange/red
                        if (distance < size / 4)
                        {
                            colorData[index] = Color.Yellow;
                        }
                        else
                        {
                            // Gradient from orange to red
                            float t = (distance - size / 4) / (size / 4);
                            colorData[index] = Color.Lerp(Color.Orange, Color.Red, t);
                        }
                    }
                    else
                    {
                        colorData[index] = Color.Transparent;
                    }
                }
            }

            texture.SetData(colorData);
            return texture;
        }
    }

    // Camera class for handling view transformations
    public class Camera2D
    {
        private Matrix transformMatrix;
        private Vector2 position;
        private float zoom;
        private Viewport viewport;

        public Vector2 Position
        {
            get { return position; }
            set { position = value; UpdateMatrix(); }
        }

        public float Zoom
        {
            get { return zoom; }
            set { zoom = value; UpdateMatrix(); }
        }

        public Camera2D(Viewport viewport)
        {
            this.viewport = viewport;
            position = new Vector2(viewport.Width / 2, viewport.Height / 2);
            zoom = 1.0f;
            UpdateMatrix();
        }

        private void UpdateMatrix()
        {
            // Create a translation matrix to center camera on position
            Matrix translation = Matrix.CreateTranslation(
                -position.X + (viewport.Width / 2) / zoom,
                -position.Y + (viewport.Height / 2) / zoom,
                0);

            // Create a scale matrix for zoom
            Matrix scale = Matrix.CreateScale(zoom);

            // Combine transformations
            transformMatrix = translation * scale;
        }

        public Matrix GetViewMatrix()
        {
            return transformMatrix;
        }

        public void LimitToBounds(int minX, int minY, int maxX, int maxY)
        {
            // Calculate the visible area in world coordinates
            float halfScreenWidth = viewport.Width / (2 * zoom);
            float halfScreenHeight = viewport.Height / (2 * zoom);

            // Calculate how much extra space we have if the zoomed map is larger than the screen
            float zoomedMapWidth = maxX * zoom;
            float zoomedMapHeight = maxY * zoom;

            if (zoomedMapWidth > viewport.Width && zoomedMapHeight > viewport.Height)
            {
                // If the map is larger than the screen in both dimensions, use normal limiting
                position.X = MathHelper.Clamp(position.X, minX + halfScreenWidth, maxX - halfScreenWidth);
                position.Y = MathHelper.Clamp(position.Y, minY + halfScreenHeight, maxY - halfScreenHeight);
            }
            else if (zoomedMapWidth > viewport.Width)
            {
                // Map is wider than screen but not taller - center vertically, limit horizontally
                position.X = MathHelper.Clamp(position.X, minX + halfScreenWidth, maxX - halfScreenWidth);
                position.Y = maxY / 2f; // Center on map
            }
            else if (zoomedMapHeight > viewport.Height)
            {
                // Map is taller than screen but not wider - center horizontally, limit vertically
                position.X = maxX / 2f; // Center on map
                position.Y = MathHelper.Clamp(position.Y, minY + halfScreenHeight, maxY - halfScreenHeight);
            }
            else
            {
                // Map fits entirely on screen - center it
                position.X = maxX / 2f;
                position.Y = maxY / 2f;
            }

            // Update the transformation matrix with the new position
            UpdateMatrix();
        }
    }
}