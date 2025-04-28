using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System.Reflection;


namespace ArcadeButtons
{
    internal static class DrawArcade
    {
        static Texture2D[] gfx_button = new Texture2D[4];
        static Texture2D gfx_joyCenter;
        static Texture2D gfx_joyMove;
        static SpriteFont info;

        static Color[] colors = new Color[4];
        static Vector2[] positions = new Vector2[25];
        static string[] signs = { "A", "B", "X", "Y" };

        public static void LoadContent(ContentManager content)
        {
            gfx_button[0] = content.Load<Texture2D>("knapp");       // släckt knapp
            gfx_button[1] = content.Load<Texture2D>("knapp2");      // tänd knapp
            gfx_button[2] = content.Load<Texture2D>("knapp3");      // släckt sidoknapp
            gfx_button[3] = content.Load<Texture2D>("knapp4");      // tänd sidoknapp
            gfx_joyCenter = content.Load<Texture2D>("joystick_center");
            gfx_joyMove = content.Load<Texture2D>("joystick");
            info = content.Load<SpriteFont>("info");

            colors[0] = new Color(255, 50, 50);
            colors[1] = new Color(0, 250, 0);
            colors[2] = new Color(20, 20, 255);
            colors[3] = new Color(255, 255, 0);

            // Vi måste tyvärr hårdkoda alla positioner för knapparna. Ordningen är enligt spelarID röd, grön, blå, gul.
            // *** RÖD ***
            positions[0] = new Vector2(275, 460);   // Joystick
            positions[1] = new Vector2(284, 508);   // A
            positions[2] = new Vector2(303, 527);   // B
            positions[3] = new Vector2(308, 552);   // X
            positions[4] = new Vector2(306, 576);   // Y
            positions[5] = new Vector2(306, 667);   // L2 (pinball)

            // *** Grön ***
            positions[6] = new Vector2(1064, 271);   // Joystick
            positions[7] = new Vector2(1058, 228);   // A
            positions[8] = new Vector2(1038, 210);   // B
            positions[9] = new Vector2(1033, 185);   // X
            positions[10] = new Vector2(1035, 159);   // Y
            positions[11] = new Vector2(1028, 74);   // L2 (pinball)

            // *** Blå ***
            positions[12] = new Vector2(1033, 577);   // Joystick
            positions[13] = new Vector2(1049, 532);   // A
            positions[14] = new Vector2(1042, 509);   // B
            positions[15] = new Vector2(1050, 484);   // X
            positions[16] = new Vector2(1065, 461);   // Y
            positions[17] = new Vector2(1029, 667);   // L2 (pinball)

            // *** GUL ***
            positions[18] = new Vector2(303, 155);   // Joystick
            positions[19] = new Vector2(289, 205);   // A
            positions[20] = new Vector2(298, 229);   // B
            positions[21] = new Vector2(289, 253);   // X
            positions[22] = new Vector2(274, 275);   // Y
            positions[23] = new Vector2(306, 75);    // L2 (pinball)

            // *** MENYKNAPP (BLÅ) Styrs av grön spelare ***
            positions[24] = new Vector2(693, 147);  // Grön R2
        }

        public static void DrawButtons(SpriteBatch spriteBatch, int playerIndex, string buttons)
        {
            // Följande kod ritar upp knapparna, inget som du behöver bry dig om. :)

            for (int i = 1; i < 5; i++)
            {
                int light = 0;
                if (buttons.Contains(signs[i - 1]))   // i hoppar index 0 som är joysticken
                    light = 1;
                spriteBatch.Draw(gfx_button[light], positions[i + playerIndex * 6], null, colors[playerIndex], 0, Vector2.Zero, 1, SpriteEffects.None, 0);
            }

            if (playerIndex == 0 || playerIndex == 2)
            {
                if (buttons.Contains("L2"))
                    spriteBatch.Draw(gfx_button[3], positions[5 + playerIndex * 6], null, colors[playerIndex], 0, Vector2.Zero, 1, SpriteEffects.FlipVertically, 0);
                else
                    spriteBatch.Draw(gfx_button[2], positions[5 + playerIndex * 6], null, colors[playerIndex], 0, Vector2.Zero, 1, SpriteEffects.FlipVertically, 0);
            }
            else
            {
                if (buttons.Contains("L2"))
                    spriteBatch.Draw(gfx_button[3], positions[5 + playerIndex * 6], null, colors[playerIndex], 0, Vector2.Zero, 1, SpriteEffects.None, 0);
                else
                    spriteBatch.Draw(gfx_button[2], positions[5 + playerIndex * 6], null, colors[playerIndex], 0, Vector2.Zero, 1, SpriteEffects.None, 0);
            }
            if (playerIndex == 1 && buttons.Contains("R2"))      // Knapp Meny (blå)
                spriteBatch.Draw(gfx_button[1], positions[24], null, colors[2], 0, Vector2.Zero, 1, SpriteEffects.None, 0);
            else if (playerIndex == 1)
                spriteBatch.Draw(gfx_button[0], positions[24], null, colors[2], 0, Vector2.Zero, 1, SpriteEffects.None, 0);

            // Ritar knapptexten vid sidan.
            Vector2 textPos = new Vector2(100, positions[playerIndex * 6].Y + 50);
            if (playerIndex == 1 || playerIndex == 2)
                textPos = new Vector2(1200, positions[playerIndex * 6].Y - 50);

            spriteBatch.DrawString(info, buttons, textPos, Color.Black);
        }

        public static void DrawStick(SpriteBatch spriteBatch, int playerIndex, Vector2 direction)
        {
            // Följande kod ritar upp joysticken, inget som du behöver bry dig om. :)
            float angle = 0;

            if ((direction.X > 0 && direction.Y == 0))
                angle = 0;
            else if (direction.X > 0 && direction.Y > 0)
                angle = 0.79f;
            else if (direction.X == 0 && direction.Y > 0)
                angle = 1.57f;
            else if (direction.X < 0 && direction.Y > 0)
                angle = 2.356f;
            else if (direction.X < 0 && direction.Y == 0)
                angle = 3.14f;
            else if (direction.X < 0 && direction.Y < 0)
                angle = 3.93f;
            else if (direction.X == 0 && direction.Y < 0)
                angle = 4.71f;
            else if (direction.X > 0 && direction.Y < 0)
                angle = 5.5f;

            if (direction.X == 0 && direction.Y == 0)
                spriteBatch.Draw(gfx_joyCenter, positions[playerIndex * 6], null, colors[playerIndex], 0, Vector2.Zero, 1, SpriteEffects.None, 0);
            else
                spriteBatch.Draw(gfx_joyMove, positions[playerIndex * 6] + new Vector2(18), null, colors[playerIndex], angle, new Vector2(18), 1, SpriteEffects.None, 0);
        }
    }
}