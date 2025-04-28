using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace ArcadeButtons
{
    /// <summary>
    /// Class to handle both gamepad and keyboard inputs with keyboard fallback for testing
    /// </summary>
    public static class InputManager
    {
        private static KeyboardState currentKeyboardState;
        private static KeyboardState previousKeyboardState;
        private static GamePadState[] currentGamePadStates = new GamePadState[4];
        private static GamePadState[] previousGamePadStates = new GamePadState[4];

        // Keyboard mapping for different players (for testing)
        private static readonly Keys[][] movementKeysMap = new Keys[4][]
        {
            // Player 0 (Red): WASD
            new Keys[] { Keys.A, Keys.D, Keys.W, Keys.S },
            // Player 1 (Green): Arrow keys
            new Keys[] { Keys.Left, Keys.Right, Keys.Up, Keys.Down },
            // Player 2 (Blue): Numpad
            new Keys[] { Keys.NumPad4, Keys.NumPad6, Keys.NumPad8, Keys.NumPad2 },
            // Player 3 (Yellow): UIOP cluster
            new Keys[] { Keys.J, Keys.L, Keys.I, Keys.K }
        };

        // Button mapping for different players (for testing)
        private static readonly Keys[][] buttonKeysMap = new Keys[4][]
        {
            // Player 0 (Red): QERFZX for A,B,X,Y,L2,R2
            new Keys[] { Keys.Q, Keys.E, Keys.R, Keys.F, Keys.Z, Keys.X },
            // Player 1 (Green): IJKLNM for A,B,X,Y,L2,R2
            new Keys[] { Keys.U, Keys.O, Keys.Y, Keys.H, Keys.N, Keys.M },
            // Player 2 (Blue): NumPad keys and nearby
            new Keys[] { Keys.NumPad7, Keys.NumPad9, Keys.NumPad1, Keys.NumPad3, Keys.NumPad0, Keys.Decimal },
            // Player 3 (Yellow): 7890[] for A,B,X,Y,L2,R2
            new Keys[] { Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6 }
        };

        public static void Update()
        {
            // Update previous states
            previousKeyboardState = currentKeyboardState;
            for (int i = 0; i < 4; i++)
            {
                previousGamePadStates[i] = currentGamePadStates[i];
            }

            // Get current states
            currentKeyboardState = Keyboard.GetState();
            for (int i = 0; i < 4; i++)
            {
                currentGamePadStates[i] = GamePad.GetState((PlayerIndex)i);
            }
        }

        public static bool IsGamePadConnected(int playerIndex)
        {
            return currentGamePadStates[playerIndex].IsConnected;
        }

        public static Vector2 GetMovement(int playerIndex)
        {
            Vector2 movement = Vector2.Zero;

            // First check gamepad if connected
            if (currentGamePadStates[playerIndex].IsConnected)
            {
                movement = currentGamePadStates[playerIndex].ThumbSticks.Left;
                // Y gives +1 for up, we invert it to match screen coordinates
                movement.Y = -movement.Y;
            }
            else
            {
                // Fallback to keyboard
                Keys[] movementKeys = movementKeysMap[playerIndex];

                // Left
                if (currentKeyboardState.IsKeyDown(movementKeys[0]))
                    movement.X -= 1;
                
                // Right
                if (currentKeyboardState.IsKeyDown(movementKeys[1]))
                    movement.X += 1;
                
                // Up
                if (currentKeyboardState.IsKeyDown(movementKeys[2]))
                    movement.Y -= 1;
                
                // Down
                if (currentKeyboardState.IsKeyDown(movementKeys[3]))
                    movement.Y += 1;
            }

            // Normalize if movement length is > 0
            if (movement.Length() > 0)
                movement.Normalize();

            return movement;
        }

        public static bool IsButtonDown(int playerIndex, Buttons button)
        {
            // First check gamepad if connected
            if (currentGamePadStates[playerIndex].IsConnected)
            {
                return currentGamePadStates[playerIndex].IsButtonDown(button);
            }
            else
            {
                // Fallback to keyboard
                Keys mappedKey = GetMappedKey(playerIndex, button);
                return currentKeyboardState.IsKeyDown(mappedKey);
            }
        }

        public static bool IsButtonPressed(int playerIndex, Buttons button)
        {
            // Check if button was just pressed (down now but not before)
            if (IsButtonDown(playerIndex, button) && !WasButtonDown(playerIndex, button))
                return true;
            
            return false;
        }

        private static bool WasButtonDown(int playerIndex, Buttons button)
        {
            // First check gamepad if it was connected
            if (previousGamePadStates[playerIndex].IsConnected)
            {
                return previousGamePadStates[playerIndex].IsButtonDown(button);
            }
            else
            {
                // Fallback to keyboard
                Keys mappedKey = GetMappedKey(playerIndex, button);
                return previousKeyboardState.IsKeyDown(mappedKey);
            }
        }

        private static Keys GetMappedKey(int playerIndex, Buttons button)
        {
            Keys[] playerButtons = buttonKeysMap[playerIndex];
            
            switch (button)
            {
                case Buttons.A:
                    return playerButtons[0];
                case Buttons.B:
                    return playerButtons[1];
                case Buttons.X:
                    return playerButtons[2];
                case Buttons.Y:
                    return playerButtons[3];
                case Buttons.LeftShoulder:
                    return playerButtons[4]; // L2
                case Buttons.RightShoulder:
                    return playerButtons[5]; // R2
                default:
                    return Keys.None;
            }
        }

        public static string GetPressedButtonsText(int playerIndex)
        {
            string result = "";
            
            if (IsButtonDown(playerIndex, Buttons.A))
                result += "A";
            if (IsButtonDown(playerIndex, Buttons.B))
                result += "B";
            if (IsButtonDown(playerIndex, Buttons.X))
                result += "X";
            if (IsButtonDown(playerIndex, Buttons.Y))
                result += "Y";
            if (IsButtonDown(playerIndex, Buttons.LeftShoulder))
                result += "L2";
            if (IsButtonDown(playerIndex, Buttons.RightShoulder))
                result += "R2";
                
            return result;
        }
    }
}
