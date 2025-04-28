using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace SimplifiedGame
{
    public enum EnemyType
    {
        Basic,
        Fast,
        Tank,
        Shooter
    }

    class Enemy
    {
        // Enemy properties
        private Vector2 position;
        private Vector2 direction;
        private float speed;
        private float health;
        private float maxHealth;
        private Color color;
        private Texture2D texture;
        private EnemyType type;
        private bool isActive = true;
        private float scale = 1.0f;
        
        // Target to move towards (usually a player or the campfire)
        private Vector2 targetPosition;
        
        // Damage this enemy deals on collision
        private float damageAmount;
        
        // Time between attacks
        private float attackCooldown;
        private float attackTimer;
        
        // Drawing origin
        private Vector2 origin;
        
        // Rotation for facing towards target
        private float rotation;
        
        // Properties
        public Vector2 Position => position;
        public Vector2 Direction => direction;
        public float Health => health;
        public float MaxHealth => maxHealth;
        public bool IsActive => isActive;
        public float Radius => texture.Width * scale / 2;
        public EnemyType Type => type;
        public float DamageAmount => damageAmount;
        
        public Enemy(Vector2 position, EnemyType type, Texture2D texture)
        {
            this.position = position;
            this.type = type;
            this.texture = texture;
            this.origin = new Vector2(texture.Width / 2, texture.Height / 2);
            
            // Set properties based on enemy type
            switch (type)
            {
                case EnemyType.Basic:
                    speed = 75f;
                    health = maxHealth = 3f;
                    color = Color.DarkRed;
                    damageAmount = 1f;
                    scale = 0.7f;
                    break;
                    
                case EnemyType.Fast:
                    speed = 150f;
                    health = maxHealth = 2f;
                    color = Color.Pink;
                    damageAmount = 1f;
                    scale = 0.6f;
                    break;
                    
                case EnemyType.Tank:
                    speed = 50f;
                    health = maxHealth = 8f;
                    color = Color.DarkGray;
                    damageAmount = 2f;
                    scale = 1.0f;
                    break;
                    
                case EnemyType.Shooter:
                    speed = 60f;
                    health = maxHealth = 4f;
                    color = Color.Purple;
                    damageAmount = 1f;
                    attackCooldown = 2.0f;
                    scale = 0.8f;
                    break;
            }
            
            // Initialize attack timer with random offset to prevent all enemies attacking at once
            attackTimer = new Random().Next(0, (int)(attackCooldown * 100)) / 100f;
        }

        public void SetPosition(Vector2 newPosition)
        {
            this.position = newPosition;
        }

        public void Update(GameTime gameTime, Vector2 target)
        {
            if (!isActive) return;
            
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            targetPosition = target;
            
            // Calculate direction to target
            direction = targetPosition - position;
            
            // If we're close enough to our target, don't move any further
            if (direction.Length() > 5)
            {
                direction.Normalize();
                
                // Apply movement
                position += direction * speed * deltaTime;
            }
            
            // Update rotation to face target
            rotation = (float)Math.Atan2(direction.Y, direction.X);
            
            // Update attack timer
            if (attackTimer > 0)
            {
                attackTimer -= deltaTime;
            }
        }
        
        // Method to check if enemy can attack (used for shooter type)
        public bool CanAttack()
        {
            if (attackTimer <= 0)
            {
                attackTimer = attackCooldown;
                return true;
            }
            return false;
        }
        
        public void TakeDamage(float damage)
        {
            health -= damage;
            if (health <= 0)
            {
                isActive = false;
            }
        }
        
        public void Draw(SpriteBatch spriteBatch)
        {
            if (!isActive) return;
            
            // Draw the enemy with rotation to face target
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
            
            // Draw health bar
            float healthPercentage = health / maxHealth;
            int healthBarWidth = (int)(texture.Width * scale);
            int healthBarHeight = 5;
            
            // Background (red)
            spriteBatch.Draw(
                texture, // Reusing the texture as a pixel
                new Rectangle(
                    (int)(position.X - healthBarWidth / 2),
                    (int)(position.Y - texture.Height * scale / 2 - 10),
                    healthBarWidth,
                    healthBarHeight
                ),
                new Rectangle(0, 0, 1, 1), // Using just one pixel
                Color.Red
            );
            
            // Foreground (green)
            spriteBatch.Draw(
                texture, // Reusing the texture as a pixel
                new Rectangle(
                    (int)(position.X - healthBarWidth / 2),
                    (int)(position.Y - texture.Height * scale / 2 - 10),
                    (int)(healthBarWidth * healthPercentage),
                    healthBarHeight
                ),
                new Rectangle(0, 0, 1, 1), // Using just one pixel
                Color.Green
            );
        }
    }
}