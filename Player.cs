using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace SimplifiedGame
{
    // Gun types for upgrades
    public enum GunType
    {
        Pistol,    // Basic starting gun
        Shotgun,   // Multiple projectiles
        Rifle,     // Fast, accurate
        Cannon     // Slow, high damage
    }

    class Player
    {
        // Player properties
        private Vector2 position;
        private Vector2 direction; // Current facing direction
        private Vector2 aimDirection;
        private float speed;
        private Color color;
        private int playerIndex;
        private Texture2D texture;
        private float rotation = 0f;

        // Health
        private float health = 10f;
        private float maxHealth = 10f;

        // Screen boundaries
        private int screenWidth;
        private int screenHeight;

        // Origin for drawing
        private Vector2 origin;

        // Player scale (smaller than original)
        private float scale = 0.7f;

        // Gun properties
        private GunType gunType = GunType.Pistol;
        private float fireRate = 0.5f; // Time between shots in seconds
        private float fireTimer = 0f;
        private float projectileDamage = 1f;
        private float projectileSpeed = 400f;
        private int projectilesPerShot = 1;
        private float spreadAngle = 0.1f; // Used for shotgun

        // Modifiers for upgrades
        public float DamageModifier { get; private set; } = 1.0f;
        public float SpeedModifier { get; private set; } = 1.0f;
        public float FireRateModifier { get; private set; } = 1.0f;
        public float HealthModifier { get; private set; } = 1.0f;

        // Public properties
        public Color Color => color;
        public Vector2 Position => position;
        public float Health => health;
        public float MaxHealth => maxHealth;
        public float Radius => texture.Width * scale / 2;
        public bool IsAlive => health > 0;
        public GunType CurrentGun => gunType;

        public Player(Vector2 position, Color color, int playerIndex, int screenWidth, int screenHeight, Texture2D texture)
        {
            this.position = position;
            this.color = color;
            this.playerIndex = playerIndex;
            this.speed = 200;
            this.screenWidth = screenWidth;
            this.screenHeight = screenHeight;
            this.texture = texture;

            // Set initial direction (facing right)
            this.direction = new Vector2(1, 0);
            this.aimDirection = new Vector2(1, 0);
            this.rotation = 0f; // Starting rotation (facing right)

            // Set origin to center of texture
            this.origin = new Vector2(texture.Width / 2, texture.Height / 2);
        }

        // Method to reset the player's position
        public void ResetPosition(Vector2 newPosition)
        {
            position = newPosition;
        }

        public void TakeDamage(float damage)
        {
            health -= damage;
            if (health < 0) health = 0;
        }

        public void Heal(float amount)
        {
            health += amount;
            if (health > maxHealth) health = maxHealth;
        }

        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Get movement using the new relative movement system
            MovementData movementData = InputManager.GetRelativeMovement(playerIndex);

            // Apply rotation from controls
            rotation += movementData.RotationAmount;

            // Update direction vector based on current rotation
            direction = new Vector2(
                (float)Math.Cos(rotation),
                (float)Math.Sin(rotation)
            );

            // Apply forward/backward movement along current direction
            Vector2 movement = Vector2.Zero;
            if (Math.Abs(movementData.ForwardAmount) > 0.1f) // Small deadzone
            {
                // Scale by speed, direction and input amount
                movement = direction * movementData.ForwardAmount * speed * SpeedModifier * deltaTime;
                position += movement;
            }

            // Keep player within full screen bounds with small padding
            float padding = 20; // Small padding so player doesn't disappear off-screen
            position.X = MathHelper.Clamp(position.X, padding, screenWidth - padding);
            position.Y = MathHelper.Clamp(position.Y, padding, screenHeight - padding);

            // Get aim direction from right stick or arrow keys
            Vector2 aimInput = InputManager.GetAimDirection(playerIndex);

            // If there's explicit aim input, use it for aiming only (not for movement direction)
            if (aimInput.Length() > 0.2f) // Deadzone
            {
                aimDirection = aimInput;
                aimDirection.Normalize();
            }
            else
            {
                // Otherwise use current facing direction for aiming
                aimDirection = direction;
            }

            // Update fire timer
            if (fireTimer > 0)
            {
                fireTimer -= deltaTime;
            }
        }

        // Try to fire - returns true if a shot was fired
        public bool TryFire(out List<Projectile> newProjectiles, Texture2D projectileTexture)
        {
            newProjectiles = new List<Projectile>();

            // Check if we can fire
            if (fireTimer <= 0)
            {
                // Reset fire timer based on gun type and modifiers
                fireTimer = fireRate / FireRateModifier;

                // Create projectiles based on gun type
                switch (gunType)
                {
                    case GunType.Pistol:
                        // Single shot
                        newProjectiles.Add(CreateProjectile(projectileTexture));
                        break;

                    case GunType.Shotgun:
                        // Multiple spread shots
                        for (int i = 0; i < 5; i++)
                        {
                            float angle = rotation - spreadAngle + (spreadAngle * 2 * i / 4);
                            Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                            newProjectiles.Add(CreateProjectile(projectileTexture, dir));
                        }
                        break;

                    case GunType.Rifle:
                        // Fast, accurate single shot
                        newProjectiles.Add(CreateProjectile(projectileTexture));
                        break;

                    case GunType.Cannon:
                        // High damage single shot
                        newProjectiles.Add(CreateProjectile(projectileTexture));
                        break;
                }

                return true;
            }

            return false;
        }

        private Projectile CreateProjectile(Texture2D texture, Vector2? customDirection = null)
        {
            // Calculate spawn position (slightly in front of the player)
            Vector2 spawnOffset = customDirection.HasValue ? customDirection.Value : aimDirection;
            spawnOffset.Normalize();
            spawnOffset *= (texture.Width * scale / 2 + 10); // Offset by player radius + small gap

            // Create projectile with appropriate properties based on gun type
            float damage = projectileDamage * DamageModifier;
            float speed = projectileSpeed;

            switch (gunType)
            {
                case GunType.Shotgun:
                    damage *= 0.6f; // Less damage per pellet
                    speed *= 0.9f;  // Slightly slower
                    break;
                case GunType.Rifle:
                    damage *= 0.8f;
                    speed *= 1.5f;  // Much faster
                    break;
                case GunType.Cannon:
                    damage *= 2.5f; // Much higher damage
                    speed *= 0.7f;  // Slower
                    break;
            }

            // Use custom direction if provided, otherwise use player's aim direction
            Vector2 direction = customDirection ?? aimDirection;

            return new Projectile(
                position + spawnOffset,
                direction,
                damage,
                speed,
                color,
                texture
            );
        }

        // Upgrade the player's gun
        public void UpgradeGun(GunType newGunType)
        {
            gunType = newGunType;

            // Update gun properties based on type
            switch (gunType)
            {
                case GunType.Pistol:
                    fireRate = 0.5f;
                    projectileDamage = 1f;
                    projectileSpeed = 400f;
                    break;

                case GunType.Shotgun:
                    fireRate = 0.8f; // Slower fire rate
                    projectileDamage = 0.6f; // Less damage per pellet
                    projectileSpeed = 350f; // Slightly slower
                    break;

                case GunType.Rifle:
                    fireRate = 0.2f; // Much faster fire rate
                    projectileDamage = 0.8f; // Slightly less damage
                    projectileSpeed = 600f; // Much faster
                    break;

                case GunType.Cannon:
                    fireRate = 1.0f; // Slow fire rate
                    projectileDamage = 3f; // High damage
                    projectileSpeed = 300f; // Slower
                    break;
            }
        }

        // Upgrade player stats
        public void UpgradeStat(string statType, float amount)
        {
            switch (statType)
            {
                case "damage":
                    DamageModifier += amount;
                    break;
                case "speed":
                    SpeedModifier += amount;
                    break;
                case "fireRate":
                    FireRateModifier += amount;
                    break;
                case "health":
                    HealthModifier += amount;
                    maxHealth = 10f * HealthModifier;
                    health += 2f; // Small heal when upgrading health
                    if (health > maxHealth) health = maxHealth;
                    break;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Draw with default color
            Draw(spriteBatch, color);
        }

        public void Draw(SpriteBatch spriteBatch, Color drawColor)
        {
            // Draw gun first (so player appears on top of it)
            DrawGun(spriteBatch, drawColor);

            // Draw the player circle with specified color and rotation
            spriteBatch.Draw(
                texture,
                position,
                null,
                drawColor,
                rotation,
                origin,
                scale,
                SpriteEffects.None,
                0
            );

            // Draw health bar
            DrawHealthBar(spriteBatch);
        }

        // Draw the gun as a black rectangle
        private void DrawGun(SpriteBatch spriteBatch, Color playerColor)
        {
            // Calculate gun dimensions and position
            float gunLength = 25;  // Increased gun length for visibility
            float gunWidth = 10;   // Increased gun width for visibility

            // Calculate gun position - start at player center, move in aim direction
            Vector2 gunBase = position + direction * (texture.Width * scale * 0.3f);

            // Create a solid black rectangle for the gun body
            Texture2D pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });

            // Draw the gun as a rotated rectangle
            spriteBatch.Draw(
                pixel,  // Use a pixel texture to ensure solid color
                gunBase,  // Position at the gun base
                null,  // Use the entire texture
                Color.Black,  // Black color for gun
                rotation,  // Rotate to match direction
                new Vector2(0, 0.5f),  // Origin at middle-left of rectangle
                new Vector2(gunLength, gunWidth),  // Scale to create gun dimensions
                SpriteEffects.None,
                0
            );

            // For gun type visual indicator
            Color gunTypeColor = Color.White;
            switch (gunType)
            {
                case GunType.Pistol:
                    gunTypeColor = Color.LightGray;
                    break;
                case GunType.Shotgun:
                    gunTypeColor = Color.Orange;
                    break;
                case GunType.Rifle:
                    gunTypeColor = Color.CornflowerBlue;
                    break;
                case GunType.Cannon:
                    gunTypeColor = Color.Red;
                    break;
            }

            // Add a colored tip to indicate gun type
            Vector2 gunTip = position + direction * (texture.Width * scale * 0.3f + gunLength);
            float tipSize = 6; // Increased size for visibility
            spriteBatch.Draw(
                pixel,
                gunTip,
                null,
                gunTypeColor,
                rotation,
                new Vector2(0, 0.5f),
                new Vector2(tipSize, gunWidth + 4), // Slightly larger than gun width
                SpriteEffects.None,
                0
            );
        }

        // Draw a health bar above the player
        private void DrawHealthBar(SpriteBatch spriteBatch)
        {
            if (!IsAlive) return;

            // Calculate health percentage
            float healthPercentage = health / maxHealth;

            // Health bar dimensions - made wider and taller for visibility
            int healthBarWidth = (int)(texture.Width * scale);
            int healthBarHeight = 8; // Increased height for visibility

            // Position above player - moved up slightly for better visibility
            Vector2 barPosition = new Vector2(
                position.X - healthBarWidth / 2,
                position.Y - texture.Height * scale / 2 - 20 // Increased offset
            );

            // Create a solid pixel texture for the health bar
            Texture2D pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });

            // Draw background (dark red)
            spriteBatch.Draw(
                pixel,
                new Rectangle(
                    (int)barPosition.X,
                    (int)barPosition.Y,
                    healthBarWidth,
                    healthBarHeight
                ),
                Color.DarkRed  // Dark red background
            );

            // Draw foreground (green) - representing current health
            if (healthPercentage > 0)
            {
                spriteBatch.Draw(
                    pixel,
                    new Rectangle(
                        (int)barPosition.X,
                        (int)barPosition.Y,
                        (int)(healthBarWidth * healthPercentage),
                        healthBarHeight
                    ),
                    Color.Green  // Green for health
                );
            }

            // Add a black border around health bar
            // Top border
            spriteBatch.Draw(
                pixel,
                new Rectangle(
                    (int)barPosition.X - 1,
                    (int)barPosition.Y - 1,
                    healthBarWidth + 2,
                    1
                ),
                Color.Black
            );

            // Bottom border
            spriteBatch.Draw(
                pixel,
                new Rectangle(
                    (int)barPosition.X - 1,
                    (int)barPosition.Y + healthBarHeight,
                    healthBarWidth + 2,
                    1
                ),
                Color.Black
            );

            // Left border
            spriteBatch.Draw(
                pixel,
                new Rectangle(
                    (int)barPosition.X - 1,
                    (int)barPosition.Y,
                    1,
                    healthBarHeight
                ),
                Color.Black
            );

            // Right border
            spriteBatch.Draw(
                pixel,
                new Rectangle(
                    (int)barPosition.X + healthBarWidth,
                    (int)barPosition.Y,
                    1,
                    healthBarHeight
                ),
                Color.Black
            );
        }
    }
}