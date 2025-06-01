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

        // Wizard textures for each type
        private Texture2D[] wizardTextures = new Texture2D[4];

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
        private bool campfireVulnerable = false;

        // Upgrade selection
        private int selectedUpgradeOption = 0;

        // Wizard-focused upgrade options
        private List<string> upgradeOptions = new List<string>
        {
            "Team: Spell Power+",
            "Team: Move Speed+",
            "Team: Casting Speed+",
            "Team: Mana+",
            "Team: Health+",
            "Team: Heal Campfire"
        };

        // Available upgrades - will be filtered based on what players need
        private List<string> availableUpgrades = new List<string>();

        // Game over timer
        private float gameOverTimer = 5.0f;

        // Tiled map
        private TileMap tileMap;

        // Camera for the game view
        private Camera2D camera;

        // Fullscreen support
        private bool isFullscreen = true;
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
            SetGraphicsMode(isFullscreen);
            camera = new Camera2D(GraphicsDevice.Viewport);
            base.Initialize();
        }

        private void SetGraphicsMode(bool fullscreen)
        {
            if (fullscreen)
            {
                var displayMode = GraphicsDevice.Adapter.CurrentDisplayMode;
                _graphics.IsFullScreen = true;
                _graphics.PreferredBackBufferWidth = displayMode.Width;
                _graphics.PreferredBackBufferHeight = displayMode.Height;
            }
            else
            {
                _graphics.IsFullScreen = false;
                _graphics.PreferredBackBufferWidth = windowedWidth;
                _graphics.PreferredBackBufferHeight = windowedHeight;
            }

            _graphics.ApplyChanges();
            screenWidth = _graphics.PreferredBackBufferWidth;
            screenHeight = _graphics.PreferredBackBufferHeight;

            Console.WriteLine($"[Game] Display mode: {(fullscreen ? "Fullscreen" : "Windowed")} {screenWidth}x{screenHeight}");

            if (tileMap != null)
            {
                SetOptimalZoom();
            }
        }

        private void UpdateStartButtonPosition()
        {
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
            fontRenderer = new BitmapFontRenderer(GraphicsDevice);
            tileMap = new TileMap();

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
                throw;
            }

            SetOptimalZoom();
            campfirePosition = new Vector2(tileMap.WidthInPixels / 2, tileMap.HeightInPixels / 2);

            // Create textures
            circleTexture = CreateCircleTexture(40);
            projectileTexture = CreateCircleTexture(10);
            campfireTexture = CreateCampfireTexture(60);
            grayOverlayTexture = new Texture2D(GraphicsDevice, 1, 1);
            grayOverlayTexture.SetData(new[] { new Color((byte)128, (byte)128, (byte)128, (byte)128) });
            startButtonTexture = new Texture2D(GraphicsDevice, 1, 1);
            startButtonTexture.SetData(new[] { Color.White });

            UpdateStartButtonPosition();

            // Load wizard textures
            wizardTextures[0] = Content.Load<Texture2D>("wizard_red");
            wizardTextures[1] = Content.Load<Texture2D>("wizard_green");
            wizardTextures[2] = Content.Load<Texture2D>("wizard_blue");
            wizardTextures[3] = Content.Load<Texture2D>("wizard_purple");

            // Create players with different colors and wizard textures
            Color[] playerColors = { Color.Red, Color.Green, Color.Blue, Color.Yellow };
            int padding = 100;
            Vector2[] playerCorners = new Vector2[4] {
                new Vector2(padding, tileMap.HeightInPixels - padding),  // Red - bottom left
                new Vector2(tileMap.WidthInPixels - padding, tileMap.HeightInPixels - padding),  // Green - bottom right
                new Vector2(tileMap.WidthInPixels - padding, padding),  // Blue - top right
                new Vector2(padding, padding)  // Yellow - top left
            };

            for (int i = 0; i < 4; i++)
            {
                players.Add(new Player(playerCorners[i], playerColors[i], i, tileMap.WidthInPixels, tileMap.HeightInPixels, wizardTextures[i]));
            }

            waveManager = new WaveManager(tileMap.WidthInPixels, tileMap.HeightInPixels, circleTexture);
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
                camera = new Camera2D(GraphicsDevice.Viewport);
                UpdateStartButtonPosition();

                if (tileMap != null)
                {
                    SetOptimalZoom();
                    camera.Position = campfirePosition;
                }
            }

            // Zoom mode controls
            if (InputManager.IsKeyPressed(Keys.F2))
            {
                currentZoomMode = ZoomMode.FitMap;
                SetOptimalZoom();
                Console.WriteLine("[Camera] Switched to FitMap mode");
            }
            else if (InputManager.IsKeyPressed(Keys.F3))
            {
                currentZoomMode = ZoomMode.FillScreen;
                SetOptimalZoom();
                Console.WriteLine("[Camera] Switched to FillScreen mode");
            }
            else if (InputManager.IsKeyPressed(Keys.F4))
            {
                currentZoomMode = ZoomMode.FixedZoom;
                SetOptimalZoom();
                Console.WriteLine($"[Camera] Switched to FixedZoom mode - Zoom level: {fixedZoomLevel}");
            }

            // Manual zoom controls
            if (currentZoomMode == ZoomMode.FixedZoom)
            {
                if (InputManager.IsKeyPressed(Keys.OemPlus) || InputManager.IsKeyPressed(Keys.Add))
                {
                    fixedZoomLevel = Math.Min(fixedZoomLevel + 0.5f, 10.0f);
                    SetOptimalZoom();
                }
                else if (InputManager.IsKeyPressed(Keys.OemMinus) || InputManager.IsKeyPressed(Keys.Subtract))
                {
                    fixedZoomLevel = Math.Max(fixedZoomLevel - 0.5f, 0.5f);
                    SetOptimalZoom();
                }
            }

            InputManager.Update();
            tileMap.Update(gameTime);

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            buttonCooldown -= deltaTime;

            switch (currentState)
            {
                case GameState.Menu:
                    UpdateMenu(gameTime);
                    break;
                case GameState.Playing:
                    UpdatePlaying(gameTime);
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
            Vector2 targetPosition = campfirePosition;

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

            camera.Position = Vector2.Lerp(camera.Position, targetPosition, 0.1f);
            camera.LimitToBounds(0, 0, tileMap.WidthInPixels, tileMap.HeightInPixels);
        }

        // Zoom behavior options
        public enum ZoomMode
        {
            FitMap,
            FillScreen,
            FixedZoom
        }

        private ZoomMode currentZoomMode = ZoomMode.FillScreen;
        private float fixedZoomLevel = 3.0f;

        private void SetOptimalZoom()
        {
            float scaleX = screenWidth / (float)tileMap.WidthInPixels;
            float scaleY = screenHeight / (float)tileMap.HeightInPixels;

            switch (currentZoomMode)
            {
                case ZoomMode.FitMap:
                    camera.Zoom = Math.Min(scaleX, scaleY) * 0.95f;
                    break;
                case ZoomMode.FillScreen:
                    camera.Zoom = Math.Max(scaleX, scaleY);
                    break;
                case ZoomMode.FixedZoom:
                    camera.Zoom = fixedZoomLevel;
                    break;
            }

            camera.Zoom = Math.Max(camera.Zoom, 1.0f);
            Console.WriteLine($"[Camera] {currentZoomMode} - Set zoom to {camera.Zoom:F2}");
        }

        private void UpdateMenu(GameTime gameTime)
        {
            campfireHealth = campfireMaxHealth;
            campfireVulnerable = false;

            for (int i = 0; i < 4; i++)
            {
                if (InputManager.IsButtonPressed(i, Buttons.A) && buttonCooldown <= 0)
                {
                    buttonCooldown = COOLDOWN_TIME;
                    joinedPlayers[i] = !joinedPlayers[i];
                    System.Diagnostics.Debug.WriteLine($"Player {i} joined: {joinedPlayers[i]}");
                }
            }

            if (InputManager.ShouldStartGame() && buttonCooldown <= 0)
            {
                buttonCooldown = COOLDOWN_TIME;
                StartGame();
            }

            if (Keyboard.GetState().IsKeyDown(Keys.F1) && buttonCooldown <= 0)
            {
                buttonCooldown = COOLDOWN_TIME;
                StartGame();
            }
        }

        private void UpdatePlaying(GameTime gameTime)
        {
            campfireVulnerable = true;

            for (int i = 0; i < 4; i++)
            {
                if (InputManager.IsButtonPressed(i, Buttons.A) && buttonCooldown <= 0 && !joinedPlayers[i])
                {
                    buttonCooldown = COOLDOWN_TIME;
                    joinedPlayers[i] = true;
                    System.Diagnostics.Debug.WriteLine($"Player {i} joined during gameplay");
                }
            }

            // Update only joined players and handle spell casting
            for (int i = 0; i < players.Count; i++)
            {
                if (joinedPlayers[i] && players[i].IsAlive)
                {
                    Vector2 oldPosition = players[i].Position;
                    players[i].Update(gameTime);

                    if (tileMap.CheckCollision(players[i].Position, players[i].Radius))
                    {
                        players[i].ResetPosition(oldPosition);
                    }

                    // Handle spell casting - FIXED: Added 'enemies' parameter
                    if (InputManager.IsFirePressed(i))
                    {
                        List<Projectile> newProjectiles;
                        if (players[i].TryCastSpell(out newProjectiles, projectileTexture, enemies))
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

                if (tileMap.CheckCollision(projectiles[i].Position, projectiles[i].Radius))
                {
                    projectiles[i].Deactivate();
                }

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
                    Vector2 oldPosition = enemies[i].Position;
                    Vector2 targetPosition = FindClosestPlayerPosition(enemies[i].Position);
                    enemies[i].Update(gameTime, targetPosition);

                    if (tileMap.CheckCollision(enemies[i].Position, enemies[i].Radius))
                    {
                        Vector2 moveDir = enemies[i].Direction;
                        Vector2 horizontalMove = oldPosition + new Vector2(moveDir.X, 0) * 5;

                        if (!tileMap.CheckCollision(horizontalMove, enemies[i].Radius))
                        {
                            enemies[i].SetPosition(horizontalMove);
                        }
                        else
                        {
                            Vector2 verticalMove = oldPosition + new Vector2(0, moveDir.Y) * 5;
                            if (!tileMap.CheckCollision(verticalMove, enemies[i].Radius))
                            {
                                enemies[i].SetPosition(verticalMove);
                            }
                            else
                            {
                                enemies[i].SetPosition(oldPosition);
                            }
                        }
                    }

                    CheckEnemyPlayerCollisions(enemies[i]);
                    CheckEnemyCampfireCollision(enemies[i]);

                    if (enemies[i].Type == EnemyType.Shooter && enemies[i].CanAttack())
                    {
                        Vector2 targetPlayer = FindClosestPlayerPosition(enemies[i].Position);
                        Vector2 direction = targetPlayer - enemies[i].Position;
                        if (direction != Vector2.Zero)
                        {
                            direction.Normalize();
                        }

                        Projectile enemyProjectile = new Projectile(
                            enemies[i].Position,
                            direction,
                            enemies[i].DamageAmount,
                            200f,
                            Color.Purple,
                            projectileTexture
                        );

                        projectiles.Add(enemyProjectile);
                    }
                }
                else
                {
                    enemies.RemoveAt(i);
                }
            }

            CheckProjectileCollisions();

            if (!waveManager.WaveActive && !waveManager.WavePreparing)
            {
                StartUpgradingPhase();
                campfireHealth = Math.Min(campfireMaxHealth, campfireHealth + 10);
            }

            if (AllPlayersAreDead() || campfireHealth <= 0)
            {
                currentState = GameState.GameOver;
                gameOverTimer = 5.0f;
            }
        }

        private void CheckEnemyCampfireCollision(Enemy enemy)
        {
            if (!campfireVulnerable) return;

            float distanceSquared = Vector2.DistanceSquared(enemy.Position, campfirePosition);
            float radiusSum = enemy.Radius + (campfireTexture.Width / 2);

            if (distanceSquared < radiusSum * radiusSum)
            {
                campfireHealth -= enemy.DamageAmount * 0.2f;
                if (campfireHealth < 0) campfireHealth = 0;

                Vector2 pushDir = enemy.Position - campfirePosition;
                if (pushDir != Vector2.Zero)
                {
                    pushDir.Normalize();
                    enemy.TakeDamage(1.0f);
                    enemy.SetPosition(enemy.Position + pushDir * 5);
                }
            }
        }

        private void UpdateUpgrading(GameTime gameTime)
        {
            campfireVulnerable = false;

            if (availableUpgrades.Count == 0)
            {
                currentState = GameState.Playing;
                waveManager.StartNextWave();
                return;
            }

            if (buttonCooldown <= 0)
            {
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
                    currentState = GameState.Playing;
                    waveManager.StartNextWave();
                }
            }
        }

        private void UpdateAvailableUpgrades()
        {
            availableUpgrades.Clear();
            availableUpgrades.Add("Team: Spell Power+");
            availableUpgrades.Add("Team: Move Speed+");
            availableUpgrades.Add("Team: Casting Speed+");
            availableUpgrades.Add("Team: Mana+");
            availableUpgrades.Add("Team: Health+");

            if (campfireHealth < campfireMaxHealth)
                availableUpgrades.Add("Team: Heal Campfire");
        }

        private void UpdateGameOver(GameTime gameTime)
        {
            gameOverTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (gameOverTimer <= 0 || Keyboard.GetState().IsKeyDown(Keys.Space))
            {
                ResetGame();
                currentState = GameState.Menu;
            }
        }

        private void ApplyTeamUpgrade(int upgradeIndex)
        {
            string upgradeName = availableUpgrades[upgradeIndex];

            foreach (var player in players)
            {
                if (upgradeName == "Team: Spell Power+")
                {
                    player.UpgradeStat("spellPower", 0.3f);
                }
                else if (upgradeName == "Team: Move Speed+")
                {
                    player.UpgradeStat("speed", 0.3f);
                }
                else if (upgradeName == "Team: Casting Speed+")
                {
                    player.UpgradeStat("castSpeed", 0.3f);
                }
                else if (upgradeName == "Team: Mana+")
                {
                    player.UpgradeStat("mana", 0.3f);
                }
                else if (upgradeName == "Team: Health+")
                {
                    player.UpgradeStat("health", 0.3f);
                }
            }

            if (upgradeName == "Team: Heal Campfire")
            {
                campfireHealth = Math.Min(campfireMaxHealth, campfireHealth + 40);
            }
        }

        private void CheckProjectileCollisions()
        {
            for (int p = projectiles.Count - 1; p >= 0; p--)
            {
                Projectile projectile = projectiles[p];
                if (!projectile.IsActive) continue;

                // Check for projectile-campfire collisions
                if (!IsPlayerProjectile(projectile) && campfireVulnerable)
                {
                    float distanceSquared = Vector2.DistanceSquared(projectile.Position, campfirePosition);
                    float radiusSum = projectile.Radius + (campfireTexture.Width / 2);

                    if (distanceSquared < radiusSum * radiusSum)
                    {
                        campfireHealth -= projectile.Damage * 0.5f;
                        if (campfireHealth < 0) campfireHealth = 0;
                        projectile.Deactivate();
                        continue;
                    }
                }

                // Player projectiles hit enemies
                if (IsPlayerProjectile(projectile))
                {
                    for (int e = 0; e < enemies.Count; e++)
                    {
                        if (!enemies[e].IsActive) continue;

                        float distanceSquared = Vector2.DistanceSquared(projectile.Position, enemies[e].Position);
                        float radiusSum = projectile.Radius + enemies[e].Radius;

                        if (distanceSquared < radiusSum * radiusSum)
                        {
                            enemies[e].TakeDamage(projectile.Damage);
                            projectile.Deactivate();
                            break;
                        }
                    }
                }
                // Enemy projectiles hit players
                else
                {
                    for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
                    {
                        if (!joinedPlayers[playerIndex] || !players[playerIndex].IsAlive) continue;

                        float distanceSquared = Vector2.DistanceSquared(projectile.Position, players[playerIndex].Position);
                        float radiusSum = projectile.Radius + players[playerIndex].Radius;

                        if (distanceSquared < radiusSum * radiusSum)
                        {
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

                float distanceSquared = Vector2.DistanceSquared(enemy.Position, players[i].Position);
                float radiusSum = enemy.Radius + players[i].Radius;

                if (distanceSquared < radiusSum * radiusSum)
                {
                    players[i].TakeDamage(enemy.DamageAmount * 0.5f);

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
            Vector2 closestPosition = campfirePosition;
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
            UpdateAvailableUpgrades();
        }

        private void StartGame()
        {
            currentState = GameState.Playing;
            System.Diagnostics.Debug.WriteLine("Game started!");

            campfireHealth = campfireMaxHealth;

            if (!AnyPlayersJoined())
            {
                joinedPlayers[0] = true;
            }

            for (int i = 0; i < players.Count; i++)
            {
                if (joinedPlayers[i])
                {
                    float angle = i * MathHelper.TwoPi / 4;
                    Vector2 offset = new Vector2(
                        (float)Math.Cos(angle) * 150,
                        (float)Math.Sin(angle) * 150
                    );
                    players[i].ResetPosition(campfirePosition + offset);
                }
            }

            camera.Position = campfirePosition;
            waveManager.StartNextWave();
        }

        private void ResetGame()
        {
            enemies.Clear();
            projectiles.Clear();
            campfireHealth = campfireMaxHealth;
            campfireVulnerable = false;

            for (int i = 0; i < players.Count; i++)
            {
                int padding = 100;
                Vector2[] playerCorners = new Vector2[4] {
                    new Vector2(padding, tileMap.HeightInPixels - padding),
                    new Vector2(tileMap.WidthInPixels - padding, tileMap.HeightInPixels - padding),
                    new Vector2(tileMap.WidthInPixels - padding, padding),
                    new Vector2(padding, padding)
                };

                players[i] = new Player(playerCorners[i], players[i].Color, i, tileMap.WidthInPixels, tileMap.HeightInPixels, wizardTextures[i]);
            }

            joinedPlayers = new bool[4] { false, false, false, false };
            waveManager = new WaveManager(tileMap.WidthInPixels, tileMap.HeightInPixels, circleTexture);
            waveManager.SetupSpawnPoints(tileMap);
        }

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
            GraphicsDevice.Clear(new Color(20, 40, 20));

            if (currentState == GameState.Playing)
            {
                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, camera.GetViewMatrix());

                tileMap.Draw(_spriteBatch);

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

                foreach (var enemy in enemies)
                {
                    enemy.Draw(_spriteBatch);
                }

                foreach (var projectile in projectiles)
                {
                    projectile.Draw(_spriteBatch);
                }

                for (int i = 0; i < players.Count; i++)
                {
                    if (joinedPlayers[i] && players[i].IsAlive)
                    {
                        players[i].Draw(_spriteBatch);

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

                _spriteBatch.Begin();

                if (currentState == GameState.Playing)
                {
                    float healthPercentage = campfireHealth / campfireMaxHealth;
                    int healthBarWidth = 120;
                    int healthBarHeight = 10;

                    _spriteBatch.Draw(
                        startButtonTexture,
                        new Rectangle(
                            (int)(screenWidth / 2 - healthBarWidth / 2),
                            20,
                            healthBarWidth,
                            healthBarHeight
                        ),
                        Color.Red
                    );

                    _spriteBatch.Draw(
                        startButtonTexture,
                        new Rectangle(
                            (int)(screenWidth / 2 - healthBarWidth / 2),
                            20,
                            (int)(healthBarWidth * healthPercentage),
                            healthBarHeight
                        ),
                        Color.Green
                    );

                    fontRenderer.DrawTextCentered(
                        _spriteBatch,
                        "CAMPFIRE",
                        new Vector2(screenWidth / 2, 10),
                        Color.White,
                        1.0f
                    );

                    fontRenderer.DrawTextCentered(
                        _spriteBatch,
                        $"HP: {(int)campfireHealth}/{(int)campfireMaxHealth}",
                        new Vector2(screenWidth / 2, 40),
                        Color.White,
                        1.0f
                    );
                }

                waveManager.Draw(_spriteBatch, fontRenderer);

                fontRenderer.DrawTextCentered(
                    _spriteBatch,
                    "MOVE: LEFT STICK/WASD - CAST SPELLS: TRIGGER/SPACE",
                    new Vector2(screenWidth / 2, screenHeight - 70),
                    Color.White,
                    1.0f
                );

                fontRenderer.DrawTextCentered(
                    _spriteBatch,
                    "AUTO-AIM: CASTS SPELLS IN MOVEMENT DIRECTION",
                    new Vector2(screenWidth / 2, screenHeight - 40),
                    Color.White,
                    1.0f
                );

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
            fontRenderer.DrawTextCentered(
                _spriteBatch,
                "WIZARD TOWER DEFENSE",
                new Vector2(screenWidth / 2, 80),
                Color.White,
                2.0f
            );

            fontRenderer.DrawTextCentered(
                _spriteBatch,
                "PRESS A TO JOIN/LEAVE - GREEN WIZARD'S R2 OR SPACE TO START",
                new Vector2(screenWidth / 2, screenHeight - 80),
                Color.White,
                1.0f
            );

            fontRenderer.DrawTextCentered(
                _spriteBatch,
                "F11 OR ALT+ENTER: TOGGLE FULLSCREEN",
                new Vector2(screenWidth / 2, screenHeight - 40),
                Color.Gray,
                0.8f
            );

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

            // Display wizard types
            string[] wizardNames = { "FIRE WIZARD", "NATURE WIZARD", "ICE WIZARD", "LIGHTNING WIZARD" };

            for (int i = 0; i < players.Count; i++)
            {
                if (joinedPlayers[i])
                {
                    players[i].Draw(_spriteBatch);

                    fontRenderer.DrawTextCentered(
                        _spriteBatch,
                        "READY",
                        new Vector2(players[i].Position.X, players[i].Position.Y - 80),
                        players[i].Color,
                        1.0f
                    );

                    fontRenderer.DrawTextCentered(
                        _spriteBatch,
                        wizardNames[i],
                        new Vector2(players[i].Position.X, players[i].Position.Y - 60),
                        players[i].Color,
                        0.8f
                    );
                }
                else
                {
                    players[i].Draw(_spriteBatch, new Color(
                        players[i].Color.R,
                        players[i].Color.G,
                        players[i].Color.B,
                        (byte)100));

                    fontRenderer.DrawTextCentered(
                        _spriteBatch,
                        "PRESS A",
                        new Vector2(players[i].Position.X, players[i].Position.Y - 80),
                        new Color(players[i].Color.R, players[i].Color.G, players[i].Color.B, (byte)100),
                        1.0f
                    );

                    fontRenderer.DrawTextCentered(
                        _spriteBatch,
                        wizardNames[i],
                        new Vector2(players[i].Position.X, players[i].Position.Y - 60),
                        new Color(players[i].Color.R, players[i].Color.G, players[i].Color.B, (byte)100),
                        0.8f
                    );
                }

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

            _spriteBatch.Draw(startButtonTexture, startButtonRect, Color.DarkGreen);

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
            _spriteBatch.Draw(
                startButtonTexture,
                new Rectangle(0, 0, screenWidth, screenHeight),
                new Color(0, 0, 0, 150)
            );

            fontRenderer.DrawTextCentered(
                _spriteBatch,
                "WIZARD UPGRADE",
                new Vector2(screenWidth / 2, 80),
                Color.White,
                2.0f
            );

            fontRenderer.DrawTextCentered(
                _spriteBatch,
                $"WAVE {waveManager.CurrentWave} COMPLETED!",
                new Vector2(screenWidth / 2, 130),
                Color.Yellow,
                1.5f
            );

            fontRenderer.DrawTextCentered(
                _spriteBatch,
                "CHOOSE ONE UPGRADE FOR ALL WIZARDS",
                new Vector2(screenWidth / 2, 170),
                Color.White,
                1.2f
            );

            if (AnyPlayersJoined())
            {
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
                    string statsText = $"CURRENT STATS: POWER x{activePlayer.SpellPowerModifier:F1} | SPEED x{activePlayer.SpeedModifier:F1} | " +
                                     $"CAST x{activePlayer.CastSpeedModifier:F1} | MANA x{activePlayer.ManaModifier:F1} | HP x{activePlayer.HealthModifier:F1}";
                    fontRenderer.DrawTextCentered(
                        _spriteBatch,
                        statsText,
                        new Vector2(screenWidth / 2, 200),
                        Color.Cyan,
                        0.9f
                    );
                }
            }

            int startY = 240;
            int spacing = 35;

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

                    if (AnyPlayersJoined())
                    {
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
                            if (option == "Team: Spell Power+")
                            {
                                float newValue = activePlayer.SpellPowerModifier + 0.3f;
                                option = $"Team: Spell Power+ (x{activePlayer.SpellPowerModifier:F1} → x{newValue:F1})";
                            }
                            else if (option == "Team: Move Speed+")
                            {
                                float newValue = activePlayer.SpeedModifier + 0.3f;
                                option = $"Team: Move Speed+ (x{activePlayer.SpeedModifier:F1} → x{newValue:F1})";
                            }
                            else if (option == "Team: Casting Speed+")
                            {
                                float newValue = activePlayer.CastSpeedModifier + 0.3f;
                                option = $"Team: Casting Speed+ (x{activePlayer.CastSpeedModifier:F1} → x{newValue:F1})";
                            }
                            else if (option == "Team: Mana+")
                            {
                                float newValue = activePlayer.ManaModifier + 0.3f;
                                option = $"Team: Mana+ (x{activePlayer.ManaModifier:F1} → x{newValue:F1})";
                            }
                            else if (option == "Team: Health+")
                            {
                                float newValue = activePlayer.HealthModifier + 0.3f;
                                option = $"Team: Health+ (x{activePlayer.HealthModifier:F1} → x{newValue:F1})";
                            }
                            else if (option == "Team: Heal Campfire")
                            {
                                int newHealth = (int)Math.Min(campfireMaxHealth, campfireHealth + 40);
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
            fontRenderer.DrawTextCentered(
                _spriteBatch,
                "GAME OVER",
                new Vector2(screenWidth / 2, screenHeight / 2 - 150),
                Color.Red,
                3.0f
            );

            fontRenderer.DrawTextCentered(
                _spriteBatch,
                $"YOUR WIZARDS SURVIVED {waveManager.CurrentWave} WAVES",
                new Vector2(screenWidth / 2, screenHeight / 2 - 50),
                Color.White,
                1.5f
            );

            if (campfireHealth <= 0)
            {
                fontRenderer.DrawTextCentered(
                    _spriteBatch,
                    "THE MAGICAL CAMPFIRE WAS DESTROYED",
                    new Vector2(screenWidth / 2, screenHeight / 2),
                    Color.OrangeRed,
                    1.5f
                );
            }
            else
            {
                fontRenderer.DrawTextCentered(
                    _spriteBatch,
                    "ALL WIZARDS WERE DEFEATED",
                    new Vector2(screenWidth / 2, screenHeight / 2),
                    Color.OrangeRed,
                    1.5f
                );
            }

            fontRenderer.DrawTextCentered(
                _spriteBatch,
                $"RESTARTING IN {(int)Math.Ceiling(gameOverTimer)}... (PRESS SPACE)",
                new Vector2(screenWidth / 2, screenHeight / 2 + 50),
                Color.White,
                1.2f
            );
        }

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

        private Texture2D CreateCampfireTexture(int size)
        {
            Texture2D texture = new Texture2D(GraphicsDevice, size, size);
            Color[] colorData = new Color[size * size];

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    int index = x * size + y;

                    Vector2 pos = new Vector2(x - size / 2, y - size / 2);
                    float distance = pos.Length();

                    if (distance < size / 2)
                    {
                        if (distance < size / 4)
                        {
                            colorData[index] = Color.Yellow;
                        }
                        else
                        {
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
            Matrix translation = Matrix.CreateTranslation(
                -position.X + (viewport.Width / 2) / zoom,
                -position.Y + (viewport.Height / 2) / zoom,
                0);

            Matrix scale = Matrix.CreateScale(zoom);
            transformMatrix = translation * scale;
        }

        public Matrix GetViewMatrix()
        {
            return transformMatrix;
        }

        public void LimitToBounds(int minX, int minY, int maxX, int maxY)
        {
            float halfScreenWidth = viewport.Width / (2 * zoom);
            float halfScreenHeight = viewport.Height / (2 * zoom);

            float zoomedMapWidth = maxX * zoom;
            float zoomedMapHeight = maxY * zoom;

            if (zoomedMapWidth > viewport.Width && zoomedMapHeight > viewport.Height)
            {
                position.X = MathHelper.Clamp(position.X, minX + halfScreenWidth, maxX - halfScreenWidth);
                position.Y = MathHelper.Clamp(position.Y, minY + halfScreenHeight, maxY - halfScreenHeight);
            }
            else if (zoomedMapWidth > viewport.Width)
            {
                position.X = MathHelper.Clamp(position.X, minX + halfScreenWidth, maxX - halfScreenWidth);
                position.Y = maxY / 2f;
            }
            else if (zoomedMapHeight > viewport.Height)
            {
                position.X = maxX / 2f;
                position.Y = MathHelper.Clamp(position.Y, minY + halfScreenHeight, maxY - halfScreenHeight);
            }
            else
            {
                position.X = maxX / 2f;
                position.Y = maxY / 2f;
            }

            UpdateMatrix();
        }
    }
}