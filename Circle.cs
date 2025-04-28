using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ArcadeButtons
{
    class Circle
    {
        private Vector2 position;       // cirkelns position.
        private Vector2 cornerPosition; // spelarens hörn.
        private Vector2 movement;       // Hur användaren styr.
        private float speed;
        private Vector2 middle;
        private Texture2D gfx_ring;

        private Color primaryColor;
        private int playerIndex;

        private string pressedButtons;  // Används för att skriva ut i text vilka knappar som klickas.

        // Public property to access position
        public Vector2 Position => position;

        public Circle(Vector2 position, Color color, int player)
        {
            this.position = position;
            speed = 180;
            primaryColor = color;
            playerIndex = player;   // Motsvarar det gamepad-ID som ska kollas.
            pressedButtons = "";

            // Tar reda på spelarens hörn.
            if (player == 0) { cornerPosition = new Vector2(50, 540); }   // röd
            if (player == 1) { cornerPosition = new Vector2(1300, 240); }  // grön
            if (player == 2) { cornerPosition = new Vector2(1300, 720); }  // blå
            if (player == 3) { cornerPosition = new Vector2(50, 50); }      // gul
        }

        public void LoadContent(ContentManager content)
        {
            gfx_ring = content.Load<Texture2D>("circle");
            middle = new Vector2(gfx_ring.Width / 2, gfx_ring.Height / 2);
        }

        // Method to reset position (used when starting the game from menu)
        public void ResetPosition(Vector2 newPosition)
        {
            position = newPosition;
        }

        public void Update(GameTime gameTime)
        {
            // Get movement from InputManager (works with both gamepad and keyboard)
            movement = InputManager.GetMovement(playerIndex);

            if (movement.Length() > 0)      // Om spelaren rör sig..
                movement.Normalize();       // Så sätter vi rörelsen till längden 1. Riktningen behålls. Utan det här så går det lite snabbare diagonalt.

            movement *= speed;              // Movement blir den hastighet vi vill (speed) men behåller riktningen.
            movement *= (float)gameTime.ElapsedGameTime.TotalSeconds;   // Rörelsen anpassas efter tiden som gått.

            Vector2 nextPos = position + movement;                      // Räknar ut vart vi kommer att hamna. 
            if (nextPos.X < 443 || nextPos.X > 925)                    // Kommer vi hamna utanför "skärmen" i x-led?
                movement.X = 0;                                         // Stoppa rörelsen i x-led.

            if (nextPos.Y < 287 || nextPos.Y > 488)                    // Kommer vi hamna utanför "skärmen" i y-led?
                movement.Y = 0;                                         // Stoppa rörelsen i y-led.

            position += movement;

            // Get pressed buttons from InputManager
            pressedButtons = InputManager.GetPressedButtonsText(playerIndex);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Ritar ringen.
            spriteBatch.Draw(gfx_ring, position, null, primaryColor, 0, middle, 0.5f, SpriteEffects.None, 0);

            DrawArcade.DrawStick(spriteBatch, playerIndex, movement);
            // Låter en annan klass rita upp knapparna som trycks ner på arkaden.
            DrawArcade.DrawButtons(spriteBatch, playerIndex, pressedButtons);
        }
    }
}