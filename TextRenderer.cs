using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace SimplifiedGame
{
    public class BitmapFontRenderer
    {
        private GraphicsDevice _graphicsDevice;
        private Texture2D _fontTexture;
        private Dictionary<char, Rectangle> _characterMap;
        private int _charWidth = 5;
        private int _charHeight = 8;
        private int _spacing = 1;

        public BitmapFontRenderer(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;
            CreateFontTexture();
        }

        private void CreateFontTexture()
        {
            // Create a texture large enough to hold all our characters
            // We'll create a simple 5x8 pixel font
            _fontTexture = new Texture2D(_graphicsDevice, 256, 128);
            Color[] fontData = new Color[256 * 128];

            // Initialize with transparent
            for (int i = 0; i < fontData.Length; i++)
            {
                fontData[i] = Color.Transparent;
            }

            // Create character map
            _characterMap = new Dictionary<char, Rectangle>();

            // Define ASCII characters (32-126)
            // Each character is in a 5x8 grid cell

            // Define pixel data for lowercase letters
            DefineLetters(fontData);

            // Define pixel data for uppercase letters
            DefineUppercaseLetters(fontData);

            // Define pixel data for numbers
            DefineNumbers(fontData);

            // Define pixel data for punctuation
            DefinePunctuation(fontData);

            // Set the texture data
            _fontTexture.SetData(fontData);
        }

        private void DefineLetters(Color[] fontData)
        {
            // Define lowercase a
            DrawCharacter(fontData, 'a', new bool[,] {
                { false, true, true, true, false },
                { false, false, false, true, false },
                { false, true, true, true, false },
                { true, false, false, true, false },
                { false, true, true, true, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false }
            });

            // Define lowercase b
            DrawCharacter(fontData, 'b', new bool[,] {
                { true, false, false, false, false },
                { true, false, false, false, false },
                { true, true, true, false, false },
                { true, false, false, true, false },
                { true, false, false, true, false },
                { true, false, false, true, false },
                { true, true, true, false, false },
                { false, false, false, false, false }
            });

            // Define lowercase c
            DrawCharacter(fontData, 'c', new bool[,] {
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, true, true, true, false },
                { true, false, false, false, false },
                { true, false, false, false, false },
                { true, false, false, false, false },
                { false, true, true, true, false },
                { false, false, false, false, false }
            });

            // Define lowercase d
            DrawCharacter(fontData, 'd', new bool[,] {
                { false, false, false, true, false },
                { false, false, false, true, false },
                { false, true, true, true, false },
                { true, false, false, true, false },
                { true, false, false, true, false },
                { true, false, false, true, false },
                { false, true, true, true, false },
                { false, false, false, false, false }
            });

            // Define lowercase e
            DrawCharacter(fontData, 'e', new bool[,] {
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, true, true, false, false },
                { true, false, false, true, false },
                { true, true, true, true, false },
                { true, false, false, false, false },
                { false, true, true, true, false },
                { false, false, false, false, false }
            });

            // Define lowercase f
            DrawCharacter(fontData, 'f', new bool[,] {
                { false, false, true, true, false },
                { false, true, false, false, false },
                { true, true, true, false, false },
                { false, true, false, false, false },
                { false, true, false, false, false },
                { false, true, false, false, false },
                { false, true, false, false, false },
                { false, false, false, false, false }
            });

            // Define lowercase g
            DrawCharacter(fontData, 'g', new bool[,] {
                { false, false, false, false, false },
                { false, true, true, true, false },
                { true, false, false, true, false },
                { true, false, false, true, false },
                { false, true, true, true, false },
                { false, false, false, true, false },
                { false, true, true, false, false },
                { false, false, false, false, false }
            });

            // Define lowercase h
            DrawCharacter(fontData, 'h', new bool[,] {
                { true, false, false, false, false },
                { true, false, false, false, false },
                { true, true, true, false, false },
                { true, false, false, true, false },
                { true, false, false, true, false },
                { true, false, false, true, false },
                { true, false, false, true, false },
                { false, false, false, false, false }
            });

            // Define lowercase i
            DrawCharacter(fontData, 'i', new bool[,] {
                { false, false, true, false, false },
                { false, false, false, false, false },
                { false, true, true, false, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, true, true, true, false },
                { false, false, false, false, false }
            });

            // Define lowercase j
            DrawCharacter(fontData, 'j', new bool[,] {
                { false, false, false, true, false },
                { false, false, false, false, false },
                { false, false, true, true, false },
                { false, false, false, true, false },
                { false, false, false, true, false },
                { true, false, false, true, false },
                { false, true, true, false, false },
                { false, false, false, false, false }
            });

            // Define lowercase k
            DrawCharacter(fontData, 'k', new bool[,] {
                { true, false, false, false, false },
                { true, false, false, false, false },
                { true, false, false, true, false },
                { true, false, true, false, false },
                { true, true, false, false, false },
                { true, false, true, false, false },
                { true, false, false, true, false },
                { false, false, false, false, false }
            });

            // Define lowercase l
            DrawCharacter(fontData, 'l', new bool[,] {
                { false, true, true, false, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, true, true, true, false },
                { false, false, false, false, false }
            });

            // Define lowercase m
            DrawCharacter(fontData, 'm', new bool[,] {
                { false, false, false, false, false },
                { false, false, false, false, false },
                { true, false, false, true, false },
                { true, true, true, true, true },
                { true, false, true, false, true },
                { true, false, true, false, true },
                { true, false, true, false, true },
                { false, false, false, false, false }
            });

            // Define lowercase n
            DrawCharacter(fontData, 'n', new bool[,] {
                { false, false, false, false, false },
                { false, false, false, false, false },
                { true, true, true, false, false },
                { true, false, false, true, false },
                { true, false, false, true, false },
                { true, false, false, true, false },
                { true, false, false, true, false },
                { false, false, false, false, false }
            });

            // Define lowercase o
            DrawCharacter(fontData, 'o', new bool[,] {
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, true, true, false, false },
                { true, false, false, true, false },
                { true, false, false, true, false },
                { true, false, false, true, false },
                { false, true, true, false, false },
                { false, false, false, false, false }
            });

            // Define lowercase p
            DrawCharacter(fontData, 'p', new bool[,] {
                { false, false, false, false, false },
                { false, false, false, false, false },
                { true, true, true, false, false },
                { true, false, false, true, false },
                { true, true, true, false, false },
                { true, false, false, false, false },
                { true, false, false, false, false },
                { false, false, false, false, false }
            });

            // Define lowercase q
            DrawCharacter(fontData, 'q', new bool[,] {
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, true, true, true, false },
                { true, false, false, true, false },
                { false, true, true, true, false },
                { false, false, false, true, false },
                { false, false, false, true, false },
                { false, false, false, false, false }
            });

            // Define lowercase r
            DrawCharacter(fontData, 'r', new bool[,] {
                { false, false, false, false, false },
                { false, false, false, false, false },
                { true, false, true, true, false },
                { true, true, false, false, false },
                { true, false, false, false, false },
                { true, false, false, false, false },
                { true, false, false, false, false },
                { false, false, false, false, false }
            });

            // Define lowercase s
            DrawCharacter(fontData, 's', new bool[,] {
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, true, true, true, false },
                { true, false, false, false, false },
                { false, true, true, false, false },
                { false, false, false, true, false },
                { true, true, true, false, false },
                { false, false, false, false, false }
            });

            // Define lowercase t
            DrawCharacter(fontData, 't', new bool[,] {
                { false, false, true, false, false },
                { false, false, true, false, false },
                { true, true, true, true, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, false, false, true, false },
                { false, false, false, false, false }
            });

            // Define lowercase u
            DrawCharacter(fontData, 'u', new bool[,] {
                { false, false, false, false, false },
                { false, false, false, false, false },
                { true, false, false, true, false },
                { true, false, false, true, false },
                { true, false, false, true, false },
                { true, false, false, true, false },
                { false, true, true, false, true },
                { false, false, false, false, false }
            });

            // Define lowercase v
            DrawCharacter(fontData, 'v', new bool[,] {
                { false, false, false, false, false },
                { false, false, false, false, false },
                { true, false, false, true, false },
                { true, false, false, true, false },
                { true, false, false, true, false },
                { false, true, false, true, false },
                { false, false, true, false, false },
                { false, false, false, false, false }
            });

            // Define lowercase w
            DrawCharacter(fontData, 'w', new bool[,] {
                { false, false, false, false, false },
                { false, false, false, false, false },
                { true, false, false, false, true },
                { true, false, true, false, true },
                { true, false, true, false, true },
                { true, false, true, false, true },
                { false, true, false, true, false },
                { false, false, false, false, false }
            });

            // Define lowercase x
            DrawCharacter(fontData, 'x', new bool[,] {
                { false, false, false, false, false },
                { false, false, false, false, false },
                { true, false, false, true, false },
                { false, true, false, true, false },
                { false, false, true, false, false },
                { false, true, false, true, false },
                { true, false, false, true, false },
                { false, false, false, false, false }
            });

            // Define lowercase y
            DrawCharacter(fontData, 'y', new bool[,] {
                { false, false, false, false, false },
                { false, false, false, false, false },
                { true, false, false, true, false },
                { true, false, false, true, false },
                { false, true, true, true, false },
                { false, false, false, true, false },
                { false, true, true, false, false },
                { false, false, false, false, false }
            });

            // Define lowercase z
            DrawCharacter(fontData, 'z', new bool[,] {
                { false, false, false, false, false },
                { false, false, false, false, false },
                { true, true, true, true, false },
                { false, false, false, true, false },
                { false, false, true, false, false },
                { false, true, false, false, false },
                { true, true, true, true, false },
                { false, false, false, false, false }
            });
        }

        private void DefineUppercaseLetters(Color[] fontData)
        {
            // Define uppercase A
            DrawCharacter(fontData, 'A', new bool[,] {
                { false, false, true, false, false },
                { false, true, false, true, false },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { true, true, true, true, true },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { false, false, false, false, false }
            });

            // Define uppercase B
            DrawCharacter(fontData, 'B', new bool[,] {
                { true, true, true, true, false },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { true, true, true, true, false },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { true, true, true, true, false },
                { false, false, false, false, false }
            });

            // Define uppercase C
            DrawCharacter(fontData, 'C', new bool[,] {
                { false, true, true, true, false },
                { true, false, false, false, true },
                { true, false, false, false, false },
                { true, false, false, false, false },
                { true, false, false, false, false },
                { true, false, false, false, true },
                { false, true, true, true, false },
                { false, false, false, false, false }
            });

            // Define uppercase D
            DrawCharacter(fontData, 'D', new bool[,] {
                { true, true, true, false, false },
                { true, false, false, true, false },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { true, false, false, true, false },
                { true, true, true, false, false },
                { false, false, false, false, false }
            });

            // Define uppercase E
            DrawCharacter(fontData, 'E', new bool[,] {
                { true, true, true, true, true },
                { true, false, false, false, false },
                { true, false, false, false, false },
                { true, true, true, true, false },
                { true, false, false, false, false },
                { true, false, false, false, false },
                { true, true, true, true, true },
                { false, false, false, false, false }
            });

            // Define uppercase F
            DrawCharacter(fontData, 'F', new bool[,] {
                { true, true, true, true, true },
                { true, false, false, false, false },
                { true, false, false, false, false },
                { true, true, true, true, false },
                { true, false, false, false, false },
                { true, false, false, false, false },
                { true, false, false, false, false },
                { false, false, false, false, false }
            });

            // Define uppercase G
            DrawCharacter(fontData, 'G', new bool[,] {
                { false, true, true, true, false },
                { true, false, false, false, true },
                { true, false, false, false, false },
                { true, false, true, true, true },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { false, true, true, true, false },
                { false, false, false, false, false }
            });

            // Define uppercase H
            DrawCharacter(fontData, 'H', new bool[,] {
                { true, false, false, false, true },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { true, true, true, true, true },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { false, false, false, false, false }
            });

            // Define uppercase I
            DrawCharacter(fontData, 'I', new bool[,] {
                { false, true, true, true, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, true, true, true, false },
                { false, false, false, false, false }
            });

            // Define uppercase J
            DrawCharacter(fontData, 'J', new bool[,] {
                { false, false, false, true, false },
                { false, false, false, true, false },
                { false, false, false, true, false },
                { false, false, false, true, false },
                { false, false, false, true, false },
                { true, false, false, true, false },
                { false, true, true, false, false },
                { false, false, false, false, false }
            });

            // Define uppercase K
            DrawCharacter(fontData, 'K', new bool[,] {
                { true, false, false, true, false },
                { true, false, true, false, false },
                { true, true, false, false, false },
                { true, true, false, false, false },
                { true, false, true, false, false },
                { true, false, false, true, false },
                { true, false, false, false, true },
                { false, false, false, false, false }
            });

            // Define uppercase L
            DrawCharacter(fontData, 'L', new bool[,] {
                { true, false, false, false, false },
                { true, false, false, false, false },
                { true, false, false, false, false },
                { true, false, false, false, false },
                { true, false, false, false, false },
                { true, false, false, false, false },
                { true, true, true, true, true },
                { false, false, false, false, false }
            });

            // Define uppercase M
            DrawCharacter(fontData, 'M', new bool[,] {
                { true, false, false, false, true },
                { true, true, false, true, true },
                { true, false, true, false, true },
                { true, false, true, false, true },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { false, false, false, false, false }
            });

            // Define uppercase N
            DrawCharacter(fontData, 'N', new bool[,] {
                { true, false, false, false, true },
                { true, true, false, false, true },
                { true, false, true, false, true },
                { true, false, true, false, true },
                { true, false, false, true, true },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { false, false, false, false, false }
            });

            // Define uppercase O
            DrawCharacter(fontData, 'O', new bool[,] {
                { false, true, true, true, false },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { false, true, true, true, false },
                { false, false, false, false, false }
            });

            // Define uppercase P
            DrawCharacter(fontData, 'P', new bool[,] {
                { true, true, true, true, false },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { true, true, true, true, false },
                { true, false, false, false, false },
                { true, false, false, false, false },
                { true, false, false, false, false },
                { false, false, false, false, false }
            });

            // Define uppercase Q
            DrawCharacter(fontData, 'Q', new bool[,] {
                { false, true, true, true, false },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { true, false, true, false, true },
                { true, false, false, true, false },
                { false, true, true, false, true },
                { false, false, false, false, false }
            });

            // Define uppercase R
            DrawCharacter(fontData, 'R', new bool[,] {
                { true, true, true, true, false },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { true, true, true, true, false },
                { true, false, true, false, false },
                { true, false, false, true, false },
                { true, false, false, false, true },
                { false, false, false, false, false }
            });

            // Define uppercase S
            DrawCharacter(fontData, 'S', new bool[,] {
                { false, true, true, true, false },
                { true, false, false, false, true },
                { true, false, false, false, false },
                { false, true, true, true, false },
                { false, false, false, false, true },
                { true, false, false, false, true },
                { false, true, true, true, false },
                { false, false, false, false, false }
            });

            // Define uppercase T
            DrawCharacter(fontData, 'T', new bool[,] {
                { true, true, true, true, true },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, false, false, false, false }
            });

            // Define uppercase U
            DrawCharacter(fontData, 'U', new bool[,] {
                { true, false, false, false, true },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { false, true, true, true, false },
                { false, false, false, false, false }
            });

            // Define uppercase V
            DrawCharacter(fontData, 'V', new bool[,] {
                { true, false, false, false, true },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { false, true, false, true, false },
                { false, true, false, true, false },
                { false, false, true, false, false },
                { false, false, false, false, false }
            });

            // Define uppercase W
            DrawCharacter(fontData, 'W', new bool[,] {
                { true, false, false, false, true },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { true, false, true, false, true },
                { true, false, true, false, true },
                { true, true, false, true, true },
                { true, false, false, false, true },
                { false, false, false, false, false }
            });

            // Define uppercase X
            DrawCharacter(fontData, 'X', new bool[,] {
                { true, false, false, false, true },
                { false, true, false, true, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, true, false, true, false },
                { true, false, false, false, true },
                { false, false, false, false, false }
            });

            // Define uppercase Y
            DrawCharacter(fontData, 'Y', new bool[,] {
                { true, false, false, false, true },
                { false, true, false, true, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, false, false, false, false }
            });

            // Define uppercase Z
            DrawCharacter(fontData, 'Z', new bool[,] {
                { true, true, true, true, true },
                { false, false, false, true, false },
                { false, false, true, false, false },
                { false, true, false, false, false },
                { true, false, false, false, false },
                { true, false, false, false, false },
                { true, true, true, true, true },
                { false, false, false, false, false }
            });
        }

        private void DefineNumbers(Color[] fontData)
        {
            // Define number 0
            DrawCharacter(fontData, '0', new bool[,] {
                { false, true, true, true, false },
                { true, false, false, false, true },
                { true, false, false, true, true },
                { true, false, true, false, true },
                { true, true, false, false, true },
                { true, false, false, false, true },
                { false, true, true, true, false },
                { false, false, false, false, false }
            });

            // Define number 1
            DrawCharacter(fontData, '1', new bool[,] {
                { false, false, true, false, false },
                { false, true, true, false, false },
                { true, false, true, false, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { true, true, true, true, true },
                { false, false, false, false, false }
            });

            // Define number 2
            DrawCharacter(fontData, '2', new bool[,] {
                { false, true, true, true, false },
                { true, false, false, false, true },
                { false, false, false, false, true },
                { false, false, false, true, false },
                { false, false, true, false, false },
                { false, true, false, false, false },
                { true, true, true, true, true },
                { false, false, false, false, false }
            });

            // Define number 3
            DrawCharacter(fontData, '3', new bool[,] {
                { false, true, true, true, false },
                { true, false, false, false, true },
                { false, false, false, false, true },
                { false, false, true, true, false },
                { false, false, false, false, true },
                { true, false, false, false, true },
                { false, true, true, true, false },
                { false, false, false, false, false }
            });

            // Define number 4
            DrawCharacter(fontData, '4', new bool[,] {
                { false, false, false, true, false },
                { false, false, true, true, false },
                { false, true, false, true, false },
                { true, false, false, true, false },
                { true, true, true, true, true },
                { false, false, false, true, false },
                { false, false, false, true, false },
                { false, false, false, false, false }
            });

            // Define number 5
            DrawCharacter(fontData, '5', new bool[,] {
                { true, true, true, true, true },
                { true, false, false, false, false },
                { true, true, true, true, false },
                { false, false, false, false, true },
                { false, false, false, false, true },
                { true, false, false, false, true },
                { false, true, true, true, false },
                { false, false, false, false, false }
            });

            // Define number 6
            DrawCharacter(fontData, '6', new bool[,] {
                { false, false, true, true, false },
                { false, true, false, false, false },
                { true, false, false, false, false },
                { true, true, true, true, false },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { false, true, true, true, false },
                { false, false, false, false, false }
            });

            // Define number 7
            DrawCharacter(fontData, '7', new bool[,] {
                { true, true, true, true, true },
                { false, false, false, false, true },
                { false, false, false, true, false },
                { false, false, true, false, false },
                { false, true, false, false, false },
                { false, true, false, false, false },
                { false, true, false, false, false },
                { false, false, false, false, false }
            });

            // Define number 8
            DrawCharacter(fontData, '8', new bool[,] {
                { false, true, true, true, false },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { false, true, true, true, false },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { false, true, true, true, false },
                { false, false, false, false, false }
            });

            // Define number 9
            DrawCharacter(fontData, '9', new bool[,] {
                { false, true, true, true, false },
                { true, false, false, false, true },
                { true, false, false, false, true },
                { false, true, true, true, true },
                { false, false, false, false, true },
                { false, false, false, true, false },
                { false, true, true, false, false },
                { false, false, false, false, false }
            });
        }

        private void DefinePunctuation(Color[] fontData)
        {
            // Define space
            _characterMap[' '] = new Rectangle(0, 0, _charWidth, _charHeight);

            // Define period
            DrawCharacter(fontData, '.', new bool[,] {
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, true, false, false, false },
                { false, false, false, false, false }
            });

            // Define comma
            DrawCharacter(fontData, ',', new bool[,] {
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, true, false, false, false }
            });

            // Define exclamation mark
            DrawCharacter(fontData, '!', new bool[,] {
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, false, false, false, false },
                { false, false, true, false, false },
                { false, false, false, false, false }
            });

            // Define colon
            DrawCharacter(fontData, ':', new bool[,] {
                { false, false, false, false, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, false, false, false, false }
            });

            // Define slash
            DrawCharacter(fontData, '/', new bool[,] {
                { false, false, false, false, true },
                { false, false, false, true, false },
                { false, false, false, true, false },
                { false, false, true, false, false },
                { false, false, true, false, false },
                { false, true, false, false, false },
                { true, false, false, false, false },
                { false, false, false, false, false }
            });

            // Define hyphen
            DrawCharacter(fontData, '-', new bool[,] {
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, true, true, true, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false }
            });
        }

        private void DrawCharacter(Color[] fontData, char c, bool[,] pixels)
        {
            int charIndex = c;
            int x = (charIndex % 16) * (_charWidth + 1);
            int y = (charIndex / 16) * (_charHeight + 1);

            // Store the character's position in the map
            _characterMap[c] = new Rectangle(x, y, _charWidth, _charHeight);

            // Draw the character's pixels
            for (int py = 0; py < _charHeight; py++)
            {
                for (int px = 0; px < _charWidth; px++)
                {
                    int index = (y + py) * 256 + (x + px);
                    if (index < fontData.Length && px < pixels.GetLength(1) && py < pixels.GetLength(0))
                    {
                        fontData[index] = pixels[py, px] ? Color.White : Color.Transparent;
                    }
                }
            }
        }

        public void DrawText(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float scale = 1.0f)
        {
            Vector2 currentPos = position;
            foreach (char c in text.ToUpper()) // Convert to uppercase for consistent rendering
            {
                if (_characterMap.ContainsKey(c))
                {
                    Rectangle sourceRect = _characterMap[c];
                    Vector2 drawPos = currentPos;

                    // Scale the character
                    Rectangle destRect = new Rectangle(
                        (int)drawPos.X,
                        (int)drawPos.Y,
                        (int)(_charWidth * scale),
                        (int)(_charHeight * scale)
                    );

                    spriteBatch.Draw(_fontTexture, destRect, sourceRect, color);

                    // Move to the next character position
                    currentPos.X += (_charWidth + _spacing) * scale;
                }
                else if (c == ' ')
                {
                    // Handle spaces
                    currentPos.X += 3 * scale;
                }
            }
        }

        public Vector2 MeasureString(string text, float scale = 1.0f)
        {
            return new Vector2(
                text.Length * (_charWidth + _spacing) * scale,
                _charHeight * scale
            );
        }

        public void DrawTextCentered(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float scale = 1.0f)
        {
            Vector2 size = MeasureString(text, scale);
            Vector2 pos = new Vector2(
                position.X - size.X / 2,
                position.Y - size.Y / 2
            );
            DrawText(spriteBatch, text, pos, color, scale);
        }
    }
}