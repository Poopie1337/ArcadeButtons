using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace SimplifiedGame
{
    public static class InputManager
    {
        private static KeyboardState currentKeyboardState;
        private static KeyboardState previousKeyboardState;
        private static GamePadState[] currentGamePadStates = new GamePadState[4];
        private static GamePadState[] previousGamePadStates = new GamePadState[4];

        // Debug mode flag
        private static bool debugModeActive = false;

        // Method to update input states
        public static void Update()
        {
            previousKeyboardState = currentKeyboardState;
            currentKeyboardState = Keyboard.GetState();

            for (int i = 0; i < 4; i++)
            {
                previousGamePadStates[i] = currentGamePadStates[i];
                currentGamePadStates[i] = GamePad.GetState((PlayerIndex)i);
            }

            // Check for F1 key to toggle debug mode
            if (IsKeyPressed(Keys.F1))
            {
                debugModeActive = !debugModeActive;
            }
        }

        // Check if a controller button is pressed (just pressed this frame, not held)
        public static bool IsButtonPressed(int playerIndex, Buttons button)
        {
            bool gamepadPressed = currentGamePadStates[playerIndex].IsButtonDown(button) &&
                                 previousGamePadStates[playerIndex].IsButtonUp(button);

            // Also check keyboard equivalents for this button and player
            bool keyboardPressed = IsKeyboardButtonPressed(playerIndex, button);

            return gamepadPressed || keyboardPressed;
        }

        // Check if a controller button is down (held)
        public static bool IsButtonDown(int playerIndex, Buttons button)
        {
            bool gamepadDown = currentGamePadStates[playerIndex].IsButtonDown(button);

            // Also check keyboard equivalents for this button and player
            bool keyboardDown = IsKeyboardButtonDown(playerIndex, button);

            return gamepadDown || keyboardDown;
        }

        // Check if a keyboard key was pressed this frame
        public static bool IsKeyPressed(Keys key)
        {
            return currentKeyboardState.IsKeyDown(key) &&
                   previousKeyboardState.IsKeyUp(key);
        }

        // Check if a keyboard key is currently down
        public static bool IsKeyDown(Keys key)
        {
            return currentKeyboardState.IsKeyDown(key);
        }

        // Method to map buttons to keyboard keys based on player index
        private static Keys GetKeyForButton(int playerIndex, Buttons button)
        {
            switch (playerIndex)
            {
                case 0: // Red player
                    switch (button)
                    {
                        case Buttons.A: return Keys.Q;
                        case Buttons.B: return Keys.E;
                        case Buttons.X: return Keys.R;
                        case Buttons.Y: return Keys.F;
                        case Buttons.LeftShoulder: return Keys.Z;
                        case Buttons.RightShoulder: return Keys.X;
                        default: return Keys.None;
                    }

                case 1: // Green player
                    switch (button)
                    {
                        case Buttons.A: return Keys.U;
                        case Buttons.B: return Keys.O;
                        case Buttons.X: return Keys.Y;
                        case Buttons.Y: return Keys.H;
                        case Buttons.LeftShoulder: return Keys.N;
                        case Buttons.RightShoulder: return Keys.M;
                        default: return Keys.None;
                    }

                case 2: // Blue player
                    switch (button)
                    {
                        case Buttons.A: return Keys.NumPad7;
                        case Buttons.B: return Keys.NumPad9;
                        case Buttons.X: return Keys.NumPad1;
                        case Buttons.Y: return Keys.NumPad3;
                        case Buttons.LeftShoulder: return Keys.NumPad0;
                        case Buttons.RightShoulder: return Keys.Decimal;
                        default: return Keys.None;
                    }

                case 3: // Yellow player
                    switch (button)
                    {
                        case Buttons.A: return Keys.D1;
                        case Buttons.B: return Keys.D2;
                        case Buttons.X: return Keys.D3;
                        case Buttons.Y: return Keys.D4;
                        case Buttons.LeftShoulder: return Keys.D5;
                        case Buttons.RightShoulder: return Keys.D6;
                        default: return Keys.None;
                    }

                default:
                    return Keys.None;
            }
        }

        // Check if a keyboard button equivalent is pressed
        private static bool IsKeyboardButtonPressed(int playerIndex, Buttons button)
        {
            Keys key = GetKeyForButton(playerIndex, button);
            return key != Keys.None && IsKeyPressed(key);
        }

        // Check if a keyboard button equivalent is down
        private static bool IsKeyboardButtonDown(int playerIndex, Buttons button)
        {
            Keys key = GetKeyForButton(playerIndex, button);
            return key != Keys.None && IsKeyDown(key);
        }

        // Get movement data for player with relative control scheme (forward/back, rotation)
        public static MovementData GetRelativeMovement(int playerIndex)
        {
            float forwardMovement = 0f;
            float rotationAmount = 0f;
            const float ROTATION_SPEED = 1.0f; // Reduced from 3.0f - now 3x slower for smoother rotation

            // First check gamepad if connected
            if (currentGamePadStates[playerIndex].IsConnected)
            {
                Vector2 leftStick = currentGamePadStates[playerIndex].ThumbSticks.Left;

                // Standard Y inversion for MonoGame
                leftStick.Y = -leftStick.Y;

                /* In arcade setup, the joysticks are mounted differently for each player:
                 *           ________________ 
                 *     Gul> |                | <Grön
                 *          |      TV:n      |
                 *          |                |
                 *     Röd> |________________| <Blå
                 */

                // For joystick: Y axis is forward/backward, X axis is rotation
                if (playerIndex == 0 || playerIndex == 3) // Red or Yellow
                {
                    // Swap X and Y for Red/Yellow player
                    forwardMovement = leftStick.X;
                    rotationAmount = leftStick.Y * ROTATION_SPEED; // FIXED: Reversed sign (removed the negative)
                }
                else
                {
                    forwardMovement = leftStick.Y;
                    rotationAmount = leftStick.X * ROTATION_SPEED; // FIXED: Reversed sign (removed the negative)
                }
            }

            // Check keyboard controls based on player
            switch (playerIndex)
            {
                case 0: // Red - WASD
                    if (IsKeyDown(Keys.W)) forwardMovement += 1;
                    if (IsKeyDown(Keys.S)) forwardMovement -= 1;
                    if (IsKeyDown(Keys.A)) rotationAmount -= ROTATION_SPEED * 0.05f; // FIXED: Reversed direction
                    if (IsKeyDown(Keys.D)) rotationAmount += ROTATION_SPEED * 0.05f; // FIXED: Reversed direction
                    break;

                case 1: // Green - Arrow Keys
                    if (IsKeyDown(Keys.Up)) forwardMovement += 1;
                    if (IsKeyDown(Keys.Down)) forwardMovement -= 1;
                    if (IsKeyDown(Keys.Left)) rotationAmount -= ROTATION_SPEED * 0.05f; // FIXED: Reversed direction
                    if (IsKeyDown(Keys.Right)) rotationAmount += ROTATION_SPEED * 0.05f; // FIXED: Reversed direction
                    break;

                case 2: // Blue - Numpad 8,4,2,6
                    if (IsKeyDown(Keys.NumPad8)) forwardMovement += 1;
                    if (IsKeyDown(Keys.NumPad2)) forwardMovement -= 1;
                    if (IsKeyDown(Keys.NumPad4)) rotationAmount -= ROTATION_SPEED * 0.05f; // FIXED: Reversed direction
                    if (IsKeyDown(Keys.NumPad6)) rotationAmount += ROTATION_SPEED * 0.05f; // FIXED: Reversed direction
                    break;

                case 3: // Yellow - IJKL
                    if (IsKeyDown(Keys.I)) forwardMovement += 1;
                    if (IsKeyDown(Keys.K)) forwardMovement -= 1;
                    if (IsKeyDown(Keys.J)) rotationAmount -= ROTATION_SPEED * 0.05f; // FIXED: Reversed direction
                    if (IsKeyDown(Keys.L)) rotationAmount += ROTATION_SPEED * 0.05f; // FIXED: Reversed direction
                    break;
            }

            // Clamp values
            forwardMovement = MathHelper.Clamp(forwardMovement, -1f, 1f);
            rotationAmount = MathHelper.Clamp(rotationAmount, -MathHelper.Pi * 0.05f, MathHelper.Pi * 0.05f);

            return new MovementData(forwardMovement, rotationAmount);
        }

        // Keep the original movement method for compatibility
        public static Vector2 GetMovement(int playerIndex)
        {
            Vector2 movement = Vector2.Zero;

            // First check gamepad if connected
            if (currentGamePadStates[playerIndex].IsConnected)
            {
                movement = currentGamePadStates[playerIndex].ThumbSticks.Left;

                // Standard Y inversion for MonoGame
                movement.Y = -movement.Y;

                /* In arcade setup, the joysticks are mounted differently for each player:
                 *           ________________ 
                 *     Gul> |                | <Grön
                 *          |      TV:n      |
                 *          |                |
                 *     Röd> |________________| <Blå
                 */

                // Handle arcade cabinet orientations
                if (playerIndex == 0 || playerIndex == 3) // Red or Yellow
                {
                    // Swap X and Y and invert X for Red/Yellow player
                    float tempX = movement.X;
                    movement.X = -movement.Y;
                    movement.Y = tempX;
                }
            }

            // Check keyboard controls based on player
            switch (playerIndex)
            {
                case 0: // Red - WASD
                    if (IsKeyDown(Keys.W)) movement.Y -= 1;
                    if (IsKeyDown(Keys.S)) movement.Y += 1;
                    if (IsKeyDown(Keys.A)) movement.X -= 1;
                    if (IsKeyDown(Keys.D)) movement.X += 1;
                    break;

                case 1: // Green - Arrow Keys
                    if (IsKeyDown(Keys.Up)) movement.Y -= 1;
                    if (IsKeyDown(Keys.Down)) movement.Y += 1;
                    if (IsKeyDown(Keys.Left)) movement.X -= 1;
                    if (IsKeyDown(Keys.Right)) movement.X += 1;
                    break;

                case 2: // Blue - Numpad 8,4,2,6
                    if (IsKeyDown(Keys.NumPad8)) movement.Y -= 1;
                    if (IsKeyDown(Keys.NumPad2)) movement.Y += 1;
                    if (IsKeyDown(Keys.NumPad4)) movement.X -= 1;
                    if (IsKeyDown(Keys.NumPad6)) movement.X += 1;
                    break;

                case 3: // Yellow - IJKL
                    if (IsKeyDown(Keys.I)) movement.Y -= 1;
                    if (IsKeyDown(Keys.K)) movement.Y += 1;
                    if (IsKeyDown(Keys.J)) movement.X -= 1;
                    if (IsKeyDown(Keys.L)) movement.X += 1;
                    break;
            }

            // Normalize if necessary
            if (movement.Length() > 1)
                movement.Normalize();

            return movement;
        }

        // Get aim direction for player
        public static Vector2 GetAimDirection(int playerIndex)
        {
            Vector2 aimDir = Vector2.Zero;

            // Try to get aim direction from gamepad right stick first
            if (currentGamePadStates[playerIndex].IsConnected)
            {
                aimDir = currentGamePadStates[playerIndex].ThumbSticks.Right;
                // Invert Y axis (standard for MonoGame)
                aimDir.Y = -aimDir.Y;
            }

            // If no direction from right stick, check buttons (A,B,X,Y)
            if (aimDir.Length() < 0.2f)
            {
                // Add direction based on which buttons are pressed
                if (IsButtonDown(playerIndex, Buttons.A))
                {
                    if (playerIndex == 0 || playerIndex == 3) // Red or Yellow
                        aimDir.Y += 1; // Down for Red/Yellow
                    else
                        aimDir.X += 1; // Right for Green/Blue
                }

                if (IsButtonDown(playerIndex, Buttons.B))
                {
                    if (playerIndex == 0 || playerIndex == 3) // Red or Yellow
                        aimDir.X -= 1; // Left for Red/Yellow
                    else
                        aimDir.Y += 1; // Down for Green/Blue
                }

                if (IsButtonDown(playerIndex, Buttons.X))
                {
                    if (playerIndex == 0 || playerIndex == 3) // Red or Yellow
                        aimDir.Y -= 1; // Up for Red/Yellow
                    else
                        aimDir.X -= 1; // Left for Green/Blue
                }

                if (IsButtonDown(playerIndex, Buttons.Y))
                {
                    if (playerIndex == 0 || playerIndex == 3) // Red or Yellow
                        aimDir.X += 1; // Right for Red/Yellow
                    else
                        aimDir.Y -= 1; // Up for Green/Blue
                }
            }

            // Return normalized vector if length is greater than 0
            if (aimDir.Length() > 0)
            {
                aimDir.Normalize();
            }

            return aimDir;
        }

        // Check if fire button is pressed
        public static bool IsFirePressed(int playerIndex)
        {
            // Fire is triggered by the LeftShoulder (L2) button on controllers
            bool controllerFire = IsButtonDown(playerIndex, Buttons.LeftShoulder);

            // Add specific keyboard keys for shooting based on player index
            bool keyboardFire = false;

            switch (playerIndex)
            {
                case 0: // Red player - Space or Z
                    keyboardFire = IsKeyDown(Keys.Space) || IsKeyDown(Keys.Z);
                    break;
                case 1: // Green player - Enter, RightControl or N
                    keyboardFire = IsKeyDown(Keys.Enter) || IsKeyDown(Keys.RightControl) || IsKeyDown(Keys.N);
                    break;
                case 2: // Blue player - NumPad5, NumPad0
                    keyboardFire = IsKeyDown(Keys.NumPad5) || IsKeyDown(Keys.NumPad0);
                    break;
                case 3: // Yellow player - LeftControl, Tab or 5
                    keyboardFire = IsKeyDown(Keys.LeftControl) || IsKeyDown(Keys.Tab) || IsKeyDown(Keys.D5);
                    break;
            }

            return controllerFire || keyboardFire;
        }

        // Get button text for display (used for debugging arcade buttons)
        public static string GetButtonText(int playerIndex)
        {
            string butt = "";

            if (IsButtonDown(playerIndex, Buttons.A))
                butt += "A";
            if (IsButtonDown(playerIndex, Buttons.B))
                butt += "B";
            if (IsButtonDown(playerIndex, Buttons.X))
                butt += "X";
            if (IsButtonDown(playerIndex, Buttons.Y))
                butt += "Y";
            if (IsButtonDown(playerIndex, Buttons.LeftShoulder))
                butt += "L2";
            if (IsButtonDown(playerIndex, Buttons.RightShoulder))
                butt += "R2";

            return butt;
        }

        // Check if the game should be in debug mode
        public static bool IsDebugModeActive()
        {
            return debugModeActive;
        }

        // Check if the game should start (Green player's R2 or F1)
        public static bool ShouldStartGame()
        {
            return IsButtonPressed(1, Buttons.RightShoulder) || IsKeyPressed(Keys.F1) || IsKeyPressed(Keys.Space);
        }
    }

    // Struct to hold relative movement data
    public struct MovementData
    {
        public float ForwardAmount; // -1 to 1, negative is backward
        public float RotationAmount; // Rotation in radians per frame

        public MovementData(float forward, float rotation)
        {
            ForwardAmount = forward;
            RotationAmount = rotation;
        }
    }
}