using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ArcadeButtons
{
    public enum GameState
    {
        Menu,
        Playing
    }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D gfx_arcade;
        private Texture2D gfx_grayOverlay;
        private SpriteFont menuFont;

        // Current game state
        private GameState currentState = GameState.Menu;

        // Här är listan med spelare. Varje spelare är en circle.
        private List<Circle> players = new List<Circle>(4);
        private bool[] activePlayers = new bool[4] { false, false, false, false };
        private bool[] joinedPlayers = new bool[4] { false, false, false, false };

        // Timer for button press cooldown
        private float menuButtonCooldown = 0f;
        private const float COOLDOWN_TIME = 0.3f;

        // Debug mode
        private bool debugMode = true;

        // Center position for spawning
        private Vector2 centerPosition = new Vector2(684, 384); // Center of playable area

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true; // Enabling mouse for easier testing
        }

        protected override void Initialize()
        {
            // Ställer in skärmen för arkaden.
            _graphics.PreferredBackBufferWidth = 1366;
            _graphics.PreferredBackBufferHeight = 768;
            _graphics.IsFullScreen = false;

            _graphics.ApplyChanges();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            gfx_arcade = Content.Load<Texture2D>("arkadmaskin");
            menuFont = Content.Load<SpriteFont>("info"); // Using the existing font

            // Create a gray overlay texture
            gfx_grayOverlay = new Texture2D(GraphicsDevice, 1, 1);
            gfx_grayOverlay.SetData(new[] { new Color(128, 128, 128, 128) }); // Semi-transparent gray

            // För att kunna skapa spelarna med en loop så behöver färgerna ligga i en array.
            Color[] color = { Color.Red, Color.Green, Color.Blue, Color.Yellow };

            // Create all potential players but keep them inactive
            for (int i = 0; i < 4; i++)
            {
                players.Add(new Circle(centerPosition, color[i], i));
                players[i].LoadContent(Content);
            }

            DrawArcade.LoadContent(Content);

            // For keyboard/PC testing, activate all players immediately in debug mode
            if (debugMode)
            {
                for (int i = 0; i < 4; i++)
                {
                    activePlayers[i] = true;
                    joinedPlayers[i] = true;

                    // Position players at their specific corners for easier testing
                    float x = (i % 2 == 0) ? 500 : 800;
                    float y = (i < 2) ? 320 : 450;
                    players[i].ResetPosition(new Vector2(x, y));
                }
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // Update our input manager
            InputManager.Update();

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            menuButtonCooldown -= deltaTime;

            if (currentState == GameState.Menu)
            {
                UpdateMenu(gameTime);
            }
            else // Playing state
            {
                // Uppdatera alla aktiva spelare
                for (int i = 0; i < players.Count; i++)
                {
                    if (activePlayers[i])
                    {
                        players[i].Update(gameTime);
                    }
                }
            }

            // In debug mode, F1 toggles menu/play state
            if (debugMode && Keyboard.GetState().IsKeyDown(Keys.F1) && menuButtonCooldown <= 0)
            {
                menuButtonCooldown = COOLDOWN_TIME;
                if (currentState == GameState.Menu)
                    StartGame();
                else
                    currentState = GameState.Menu;
            }

            base.Update(gameTime);
        }

        private void UpdateMenu(GameTime gameTime)
        {
            // Check for player joins
            for (int i = 0; i < 4; i++)
            {
                if (InputManager.IsButtonPressed(i, Buttons.A) && !joinedPlayers[i])
                {
                    joinedPlayers[i] = true;
                    activePlayers[i] = true;
                    System.Diagnostics.Debug.WriteLine($"Player {i} joined");
                }
            }

            // Start game with menu button (R2 on Green player's controller)
            if (InputManager.IsButtonPressed(1, Buttons.RightShoulder) && menuButtonCooldown <= 0)
            {
                menuButtonCooldown = COOLDOWN_TIME;

                // Check if at least one player has joined
                bool anyPlayerJoined = false;
                for (int i = 0; i < 4; i++)
                {
                    if (joinedPlayers[i])
                    {
                        anyPlayerJoined = true;
                        break;
                    }
                }

                if (anyPlayerJoined)
                {
                    StartGame();
                }
            }
        }

        private void StartGame()
        {
            currentState = GameState.Playing;
            System.Diagnostics.Debug.WriteLine("Game started!");

            // Reset player positions to be nicely spread in the center
            for (int i = 0; i < players.Count; i++)
            {
                if (activePlayers[i])
                {
                    // Adjust position based on how many players are active and their index
                    float angle = i * MathHelper.TwoPi / 4; // Evenly space around a circle
                    Vector2 offset = new Vector2(
                        (float)System.Math.Cos(angle) * 50,
                        (float)System.Math.Sin(angle) * 50);

                    players[i].ResetPosition(centerPosition + offset);
                }
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin();

            _spriteBatch.Draw(gfx_arcade, Vector2.Zero, Color.White);

            if (currentState == GameState.Menu)
            {
                DrawMenu();
            }

            // Rita ut alla spelare/cirklar
            for (int i = 0; i < players.Count; i++)
            {
                if (currentState == GameState.Playing)
                {
                    // Only draw active players during gameplay
                    if (activePlayers[i])
                    {
                        players[i].Draw(_spriteBatch);
                    }
                }
                else
                {
                    // In menu state, draw all players but gray out inactive ones
                    players[i].Draw(_spriteBatch);

                    if (!joinedPlayers[i])
                    {
                        // Draw gray overlay on inactive players
                        Rectangle playerRect = new Rectangle(
                            (int)players[i].Position.X - 25,
                            (int)players[i].Position.Y - 25,
                            50, 50); // Approximate size of player circle

                        _spriteBatch.Draw(gfx_grayOverlay, playerRect, Color.White);
                    }
                }
            }

            // Draw debug info
            if (debugMode)
            {
                DrawDebugInfo();
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawMenu()
        {
            string menuText = "ARCADE BUTTONS";
            Vector2 textSize = menuFont.MeasureString(menuText);
            _spriteBatch.DrawString(menuFont, menuText,
                new Vector2(683 - textSize.X / 2, 200), // Centered horizontally
                Color.White);

            string joinText = "Press A to join!";
            textSize = menuFont.MeasureString(joinText);
            _spriteBatch.DrawString(menuFont, joinText,
                new Vector2(683 - textSize.X / 2, 250), // Centered horizontally
                Color.White);

            string startText = "GREEN player: Press R2 to start game";
            textSize = menuFont.MeasureString(startText);
            _spriteBatch.DrawString(menuFont, startText,
                new Vector2(683 - textSize.X / 2, 300), // Centered horizontally
                Color.Yellow);

            if (debugMode)
            {
                string debugText = "DEBUG MODE: Press F1 to toggle menu/play";
                textSize = menuFont.MeasureString(debugText);
                _spriteBatch.DrawString(menuFont, debugText,
                    new Vector2(683 - textSize.X / 2, 350), // Centered horizontally
                    Color.Orange);
            }
        }

        private void DrawDebugInfo()
        {
            // Draw keyboard controls info at bottom of screen
            string controlsText = "";

            if (currentState == GameState.Menu)
            {
                controlsText += "KEYBOARD CONTROLS (DEBUG MODE)\n";
                controlsText += "Player 0 (Red): WASD=move, Q=A button\n";
                controlsText += "Player 1 (Green): Arrows=move, U=A button, M=R2 (menu)\n";
                controlsText += "Player 2 (Blue): Numpad 8456=move, Numpad7=A button\n";
                controlsText += "Player 3 (Yellow): IJKL=move, 1=A button\n";
                controlsText += "Press F1 to start game";
            }
            else
            {
                controlsText += "DEBUG MODE: Press F1 to return to menu";

                // Add player positions
                controlsText += "\nPlayer positions:";
                for (int i = 0; i < players.Count; i++)
                {
                    if (activePlayers[i])
                    {
                        controlsText += $"\nPlayer {i}: ({players[i].Position.X}, {players[i].Position.Y})";
                    }
                }
            }

            _spriteBatch.DrawString(menuFont, controlsText, new Vector2(20, 650), Color.White);
        }
    }
}