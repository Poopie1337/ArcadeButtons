using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace SimplifiedGame
{
    class Projectile
    {
        // Projectile properties
        private Vector2 position;
        private Vector2 direction;
        private float speed;
        private float damage;
        private bool isActive = true;
        private Color color;
        private Texture2D texture;
        private float timeToLive;
        private float scale = 0.4f; // Make projectiles small

        // Drawing origin
        private Vector2 origin;

        // Rotation
        private float rotation;

        // Public properties
        public Vector2 Position => position;
        public bool IsActive => isActive;
        public float Damage => damage;
        public float Radius => texture.Width * scale / 2;
        public Color Color => color;

        public Projectile(Vector2 position, Vector2 direction, float damage, float speed, Color color, Texture2D texture)
        {
            this.position = position;
            this.direction = direction;
            this.damage = damage;
            this.speed = speed;
            this.color = color;
            this.texture = texture;
            this.timeToLive = 2.0f; // Projectiles disappear after 2 seconds

            // Calculate rotation based on direction
            this.rotation = (float)Math.Atan2(direction.Y, direction.X);

            // Set origin to center of texture
            this.origin = new Vector2(texture.Width / 2, texture.Height / 2);
        }

        public void Update(GameTime gameTime)
        {
            if (!isActive) return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Move the projectile
            position += direction * speed * deltaTime;

            // Reduce time to live
            timeToLive -= deltaTime;
            if (timeToLive <= 0)
            {
                isActive = false;
            }
        }

        public void Deactivate()
        {
            isActive = false;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!isActive) return;

            // Draw the projectile
            spriteBatch.Draw(
                texture,
                position,
                null,
                color,
                rotation,
                origin,
                scale,
                SpriteEffects.None,
                0
            );
        }
    }
}