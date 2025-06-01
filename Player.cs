using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimplifiedGame
{
    // Wizard types for the 4 different magical classes
    public enum WizardType
    {
        Fire,       // Red - High damage, area effects, slower casting
        Ice,        // Blue - Fast projectiles, slowing effects, precision
        Nature,     // Green - Healing abilities, balanced stats, defensive spells
        Lightning   // Purple/Yellow - High speed, chain lightning, rapid casting
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
        private WizardType wizardType;

        // Health and mana
        private float health = 10f;
        private float maxHealth = 10f;
        private float mana = 100f;
        private float maxMana = 100f;
        private float manaRegenRate = 20f; // Mana per second

        // Screen boundaries
        private int screenWidth;
        private int screenHeight;

        // Wizard spell properties
        private float castingTime = 0.5f; // Time between spells in seconds
        private float castingTimer = 0f;
        private float spellDamage = 1f;
        private float spellSpeed = 400f;
        private float spellManaCost = 10f;

        // Modifiers for upgrades
        public float SpellPowerModifier { get; private set; } = 1.0f;
        public float SpeedModifier { get; private set; } = 1.0f;
        public float CastSpeedModifier { get; private set; } = 1.0f;
        public float ManaModifier { get; private set; } = 1.0f;
        public float HealthModifier { get; private set; } = 1.0f;

        // Animation and sprite properties
        private Texture2D wizardTexture;
        private Rectangle currentFrame;
        private int frameWidth = 64; // Width of each sprite frame
        private int frameHeight = 64; // Height of each sprite frame
        private float animationTimer = 0f;
        private int currentFrameIndex = 0;
        private bool isMoving = false;
        private bool isCasting = false;
        private float castingAnimationTimer = 0f;
        private float castingAnimationDuration = 0.8f;

        // Direction and flipping
        private bool facingLeft = false;
        private SpriteEffects spriteEffect = SpriteEffects.None;

        // Track if spell has been fired during current cast
        private bool spellFiredThisCast = false;

        // Auto-aim settings
        private float autoAimConeAngle = MathHelper.Pi / 1.5f; // 60 degree cone (Pi/3)
        private float autoAimRange = 300f; // Max distance to auto-aim

        // Public properties
        public Color Color => color;
        public Vector2 Position => position;
        public float Health => health;
        public float MaxHealth => maxHealth;
        public float Mana => mana;
        public float MaxMana => maxMana;
        public float Radius => 18f; // Smaller hitbox - roughly the actual character size
        public bool IsAlive => health > 0;
        public WizardType WizardType => wizardType;

        // Store projectile texture for delayed firing
        private Texture2D projectileTexture;

        // Store enemies list for auto-aim
        private List<Enemy> currentEnemies;

        public Player(Vector2 position, Color color, int playerIndex, int screenWidth, int screenHeight, Texture2D wizardTexture)
        {
            this.position = position;
            this.color = color;
            this.playerIndex = playerIndex;
            this.speed = 100;
            this.screenWidth = screenWidth;
            this.screenHeight = screenHeight;
            this.wizardTexture = wizardTexture;

            // Set wizard type based on player index/color
            switch (playerIndex)
            {
                case 0: wizardType = WizardType.Fire; break;      // Red player
                case 1: wizardType = WizardType.Nature; break;   // Green player
                case 2: wizardType = WizardType.Ice; break;      // Blue player
                case 3: wizardType = WizardType.Lightning; break; // Yellow/Purple player
            }

            // Set initial direction (facing right)
            this.direction = new Vector2(1, 0);
            this.aimDirection = new Vector2(1, 0);
            this.facingLeft = false;

            // Initialize wizard-specific stats
            InitializeWizardStats();

            // Set initial animation frame - start with frame 0
            currentFrameIndex = 0;
            UpdateAnimationFrame();
        }

        private void InitializeWizardStats()
        {
            switch (wizardType)
            {
                case WizardType.Fire:
                    spellDamage = 1.5f;
                    castingTime = 0.7f;
                    spellManaCost = 15f;
                    spellSpeed = 300f;
                    autoAimRange = 350f; // Shorter range but high damage
                    break;

                case WizardType.Ice:
                    spellDamage = 1.0f;
                    castingTime = 0.4f;
                    spellManaCost = 8f;
                    spellSpeed = 500f;
                    autoAimRange = 400f; // Longer range, fast projectiles
                    break;

                case WizardType.Nature:
                    spellDamage = 0.8f;
                    castingTime = 0.5f;
                    spellManaCost = 10f;
                    spellSpeed = 400f;
                    health = 12f;
                    maxHealth = 12f;
                    autoAimRange = 320f; // Balanced range
                    break;

                case WizardType.Lightning:
                    spellDamage = 1.0f;
                    castingTime = 0.3f;
                    spellManaCost = 12f;
                    spellSpeed = 600f;
                    speed = 120f;
                    autoAimRange = 380f; // Good range for chain lightning
                    autoAimConeAngle = MathHelper.PiOver2; // Wider cone for lightning (90 degrees)
                    break;
            }
        }

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

        public void RestoreMana(float amount)
        {
            mana += amount;
            if (mana > maxMana) mana = maxMana;
        }

        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Regenerate mana
            mana += manaRegenRate * deltaTime;
            if (mana > maxMana) mana = maxMana;

            // Get regular movement
            Vector2 movement = InputManager.GetMovement(playerIndex);

            // Apply movement
            isMoving = false;
            if (movement.Length() > 0.1f)
            {
                isMoving = true;
                position += movement * speed * SpeedModifier * deltaTime;

                // Update facing direction based on movement
                if (movement.X < -0.1f)
                {
                    facingLeft = true;
                    spriteEffect = SpriteEffects.FlipHorizontally;
                    direction = new Vector2(-1, 0);
                }
                else if (movement.X > 0.1f)
                {
                    facingLeft = false;
                    spriteEffect = SpriteEffects.None;
                    direction = new Vector2(1, 0);
                }
            }

            // Keep player within bounds
            float padding = 20;
            position.X = MathHelper.Clamp(position.X, padding, screenWidth - padding);
            position.Y = MathHelper.Clamp(position.Y, padding, screenHeight - padding);

            // Get aim direction - this sets the "cone center" for auto-aim
            Vector2 aimInput = InputManager.GetAimDirection(playerIndex);
            if (aimInput.Length() > 0.2f)
            {
                aimDirection = aimInput;
                aimDirection.Normalize();
            }
            else
            {
                // Default aim direction based on facing
                aimDirection = direction;
            }

            // Update timers
            if (castingTimer > 0)
            {
                castingTimer -= deltaTime;
            }

            if (isCasting)
            {
                castingAnimationTimer -= deltaTime;
                if (castingAnimationTimer <= 0)
                {
                    isCasting = false;
                    currentFrameIndex = 0;
                    spellFiredThisCast = false; // Reset for next cast
                }
            }

            // Update animation
            UpdateAnimation(deltaTime);
        }

        private void UpdateAnimation(float deltaTime)
        {
            animationTimer += deltaTime;

            float frameTime = isCasting ? 0.1f : (isMoving ? 0.15f : 0.5f);

            if (animationTimer >= frameTime)
            {
                animationTimer = 0f;
                UpdateAnimationFrame();
            }
        }

        private void UpdateAnimationFrame()
        {
            // Animation layout
            int row = 0;
            int maxFrames = 5;

            if (isCasting)
            {
                row = 6; // Ground attack row
                maxFrames = 9;
                currentFrameIndex++;
                if (currentFrameIndex >= maxFrames)
                {
                    currentFrameIndex = maxFrames - 1;
                }
            }
            else if (isMoving)
            {
                row = 3; // Walk row
                maxFrames = 8;
                currentFrameIndex = (currentFrameIndex + 1) % maxFrames;
            }
            else
            {
                row = 0; // Idle row
                maxFrames = 5;
                currentFrameIndex = (currentFrameIndex + 1) % maxFrames;
            }

            // Calculate frame position (64x64 frames)
            int pixelX = currentFrameIndex * frameWidth;
            int pixelY = row * frameHeight;

            currentFrame = new Rectangle(pixelX, pixelY, frameWidth, frameHeight);
        }

        public bool TryCastSpell(out List<Projectile> newProjectiles, Texture2D projectileTexture, List<Enemy> enemies)
        {
            newProjectiles = new List<Projectile>();

            float adjustedManaCost = spellManaCost / ManaModifier;
            if (castingTimer <= 0 && mana >= adjustedManaCost && !isCasting)
            {
                isCasting = true;
                currentFrameIndex = 0; // Reset to start of casting animation
                castingAnimationTimer = castingAnimationDuration;
                castingTimer = castingTime / CastSpeedModifier;
                mana -= adjustedManaCost;
                spellFiredThisCast = false; // Reset firing flag

                // Store texture and enemies for delayed firing
                this.projectileTexture = projectileTexture;
                this.currentEnemies = enemies;

                return false; // Don't return projectiles immediately
            }

            // Check if casting animation is at the right frame to fire
            if (isCasting && !spellFiredThisCast && currentFrameIndex >= 7)
            {
                CreateSpellWithAutoAim(newProjectiles, this.projectileTexture, this.currentEnemies);
                spellFiredThisCast = true;
                return true;
            }

            return false;
        }

        private Vector2 FindAutoAimTarget(List<Enemy> enemies)
        {
            if (enemies == null || enemies.Count == 0)
                return aimDirection; // Default to aim direction if no enemies

            Enemy bestTarget = null;
            float bestScore = float.MinValue;

            foreach (var enemy in enemies)
            {
                if (!enemy.IsActive) continue;

                Vector2 toEnemy = enemy.Position - position;
                float distance = toEnemy.Length();

                // Skip if too far
                if (distance > autoAimRange) continue;

                // Normalize for angle calculation
                toEnemy.Normalize();

                // Calculate angle from aim direction
                float dotProduct = Vector2.Dot(aimDirection, toEnemy);
                float angle = (float)Math.Acos(MathHelper.Clamp(dotProduct, -1f, 1f));

                // Skip if outside cone
                if (angle > autoAimConeAngle / 2) continue;

                // Score based on distance and angle (closer and more centered = better)
                float score = (1f - (distance / autoAimRange)) * (1f - (angle / (autoAimConeAngle / 2)));

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = enemy;
                }
            }

            if (bestTarget != null)
            {
                Vector2 targetDirection = bestTarget.Position - position;
                targetDirection.Normalize();
                return targetDirection;
            }

            return aimDirection; // No valid target found
        }

        private void CreateSpellWithAutoAim(List<Projectile> projectiles, Texture2D projectileTexture, List<Enemy> enemies)
        {
            if (projectileTexture == null) return;

            Vector2 autoAimDirection = FindAutoAimTarget(enemies);

            // Projectiles spawn directly from character center
            Vector2 spawnPosition = position;

            float damage = spellDamage * SpellPowerModifier;
            float speed = spellSpeed;

            switch (wizardType)
            {
                case WizardType.Fire:
                    projectiles.Add(new Projectile(
                        spawnPosition,
                        autoAimDirection,
                        damage,
                        speed,
                        Color.OrangeRed,
                        projectileTexture
                    ));
                    break;

                case WizardType.Ice:
                    projectiles.Add(new Projectile(
                        spawnPosition,
                        autoAimDirection,
                        damage,
                        speed,
                        Color.LightBlue,
                        projectileTexture
                    ));
                    break;

                case WizardType.Nature:
                    projectiles.Add(new Projectile(
                        spawnPosition,
                        autoAimDirection,
                        damage,
                        speed,
                        Color.LimeGreen,
                        projectileTexture
                    ));
                    break;

                case WizardType.Lightning:
                    // Lightning shoots multiple projectiles
                    float baseAngle = (float)Math.Atan2(autoAimDirection.Y, autoAimDirection.X);
                    for (int i = -1; i <= 1; i++)
                    {
                        float angle = baseAngle + (i * 0.3f);
                        Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                        projectiles.Add(new Projectile(
                            spawnPosition,
                            direction,
                            damage * 0.8f,
                            speed,
                            Color.Yellow,
                            projectileTexture
                        ));
                    }
                    break;
            }
        }

        public void UpgradeStat(string statType, float amount)
        {
            switch (statType)
            {
                case "spellPower":
                    SpellPowerModifier += amount;
                    break;
                case "speed":
                    SpeedModifier += amount;
                    break;
                case "castSpeed":
                    CastSpeedModifier += amount;
                    break;
                case "mana":
                    ManaModifier += amount;
                    maxMana = 100f * ManaModifier;
                    mana += 20f;
                    if (mana > maxMana) mana = maxMana;
                    break;
                case "health":
                    HealthModifier += amount;
                    maxHealth = 10f * HealthModifier;
                    health += 3f;
                    if (health > maxHealth) health = maxHealth;
                    break;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            Draw(spriteBatch, Color.White);
        }

        public void Draw(SpriteBatch spriteBatch, Color tintColor)
        {
            // Draw the wizard sprite with current animation frame
            spriteBatch.Draw(
                wizardTexture,
                position,
                currentFrame,
                tintColor,
                0f, // No rotation for the character
                new Vector2(frameWidth / 2, frameHeight / 2),
                1.0f, // Normal scale since sprites are already 64x64
                spriteEffect, // Use sprite effect for flipping
                0
            );

            // No more staff indicator - pure magic auto-aim!

            // Draw health bar
            DrawHealthBar(spriteBatch);

            // Draw mana bar
            DrawManaBar(spriteBatch);

            // Debug: Draw auto-aim cone (uncomment to visualize)
            /*
            if (InputManager.IsKeyDown(Keys.F12))
            {
                DrawAutoAimCone(spriteBatch);
            }
            */
        }

        private void DrawAutoAimCone(SpriteBatch spriteBatch)
        {
            // Debug visualization of auto-aim cone
            Texture2D pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });

            float aimAngle = (float)Math.Atan2(aimDirection.Y, aimDirection.X);
            int lineCount = 20;

            for (int i = 0; i <= lineCount; i++)
            {
                float t = (float)i / lineCount;
                float angle = aimAngle - autoAimConeAngle / 2 + t * autoAimConeAngle;
                Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                Vector2 endPoint = position + direction * autoAimRange;

                // Draw line (simplified - just draw points along the cone edge)
                for (int j = 0; j < autoAimRange; j += 10)
                {
                    Vector2 point = position + direction * j;
                    spriteBatch.Draw(pixel, new Rectangle((int)point.X, (int)point.Y, 2, 2), Color.Yellow * 0.3f);
                }
            }
        }

        private void DrawHealthBar(SpriteBatch spriteBatch)
        {
            if (!IsAlive) return;

            float healthPercentage = health / maxHealth;
            int healthBarWidth = 24;
            int healthBarHeight = 4;

            Vector2 barPosition = new Vector2(
                position.X - healthBarWidth / 2,
                position.Y - 25
            );

            Texture2D pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });

            // Background
            spriteBatch.Draw(
                pixel,
                new Rectangle((int)barPosition.X, (int)barPosition.Y, healthBarWidth, healthBarHeight),
                Color.DarkRed
            );

            // Foreground
            if (healthPercentage > 0)
            {
                spriteBatch.Draw(
                    pixel,
                    new Rectangle((int)barPosition.X, (int)barPosition.Y, (int)(healthBarWidth * healthPercentage), healthBarHeight),
                    Color.Green
                );
            }
        }

        private void DrawManaBar(SpriteBatch spriteBatch)
        {
            if (!IsAlive) return;

            float manaPercentage = mana / maxMana;
            int manaBarWidth = 24;
            int manaBarHeight = 3;

            Vector2 barPosition = new Vector2(
                position.X - manaBarWidth / 2,
                position.Y - 19
            );

            Texture2D pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });

            // Background
            spriteBatch.Draw(
                pixel,
                new Rectangle((int)barPosition.X, (int)barPosition.Y, manaBarWidth, manaBarHeight),
                Color.DarkBlue
            );

            // Foreground
            if (manaPercentage > 0)
            {
                spriteBatch.Draw(
                    pixel,
                    new Rectangle((int)barPosition.X, (int)barPosition.Y, (int)(manaBarWidth * manaPercentage), manaBarHeight),
                    Color.Blue
                );
            }
        }
    }
}