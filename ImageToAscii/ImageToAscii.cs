using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace SchnappiSchnap.Imaging
{
    public class ImageToAscii
    {
        private struct Coordinate
        {
            public int X { get; }
            public int Y { get; }

            public Coordinate(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        private readonly float[] brightnesses;
        private readonly Size imageSize;

        public ImageToAscii(Image inputImage)
        {
            // Create a bitmap of the image.
            Bitmap bitmap = new Bitmap(inputImage);

            // Get the data from the bitmap.
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bmd = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);
            int depth = Image.GetPixelFormatSize(bitmap.PixelFormat);
            if (depth != 8 && depth != 24 && depth != 32)
                throw new ArgumentException("Only 8, 24 and 32 bpp images are supported.");
            int colourComponentsCount = depth / 8;

            // Get the RGB data of each pixel.
            IntPtr ptr = bmd.Scan0;
            int bytes = Math.Abs(bmd.Stride) * bmd.Height;
            byte[] rgb = new byte[bytes];
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgb, 0, bytes);

            // Set the field values.
            this.imageSize = new Size(bmd.Width, bmd.Height);
            this.brightnesses = new float[imageSize.Width * imageSize.Height];
            for(int i = 0; i < bytes; i += colourComponentsCount)
            {
                if (depth == 32)
                {
                    byte b = rgb[i];
                    byte g = rgb[i + 1];
                    byte r = rgb[i + 2];
                    byte a = rgb[i + 3];
                    this.brightnesses[i / 4] = Color.FromArgb(a, r, g, b).GetBrightness();
                }
                else if(depth == 24)
                {
                    byte b = rgb[i];
                    byte g = rgb[i + 1];
                    byte r = rgb[i + 2];
                    this.brightnesses[i / 4] = Color.FromArgb(r, g, b).GetBrightness();
                }
                else if(depth == 8)
                {
                    byte c = rgb[i];
                    this.brightnesses[i / 4] = Color.FromArgb(c, c, c).GetBrightness();
                }
            }
        }

        public string GenerateString(Font font)
        {
            // Define a greyscale of chars.
            char[] greyscale = new char[10] { '@', '%', '#', '*', '+', '=', '-', ':', '.', ' ' };

            // Get the width and height of a character in the specified font.
            SizeF fontSize = GetMonospaceSize(font);
            int fontWidth = (int)Math.Round(fontSize.Width, 0);
            int fontHeight = (int)Math.Round(fontSize.Height, 0);

            // Get the number of characters in each dimension of the output array.
            int characterCountX = this.imageSize.Width / fontWidth;
            int characterCountY = this.imageSize.Height / fontHeight;

            StringBuilder sb = new StringBuilder();

            // Build the string.
            for (int y = 0; y < characterCountY; ++y)
            {
                for (int x = 0; x < characterCountX; ++x)
                {
                    // Get the coordinates of the top-left pixel in a group
                    // the size of the font.
                    Coordinate inputCoord = new Coordinate(x * fontWidth, y * fontHeight);

                    List<Coordinate> pixelGroup = GetPixelsInGroup(inputCoord, fontHeight, fontWidth);

                    float averageBrightness = GetAverageBrightness(pixelGroup);

                    // Get the greyscale index based on the brightness.
                    int i = (int)Math.Floor(averageBrightness * greyscale.Length);
                    if (i >= greyscale.Length)
                        i = greyscale.Length - 1;

                    sb.Append(greyscale[i]);
                }

                sb.Append(Environment.NewLine);
            }

            return sb.ToString();
        }

        public Image GenerateImage(Font font, Color backgroundColor, Color textColour)
        {
            string imageString = GenerateString(font);

            // Measure the string
            SizeF outputSize;
            using (Image img = new Bitmap(1, 1))
            {
                using (Graphics graphics = Graphics.FromImage(img))
                {
                    StringFormat format = new StringFormat(StringFormatFlags.MeasureTrailingSpaces);
                    outputSize = graphics.MeasureString(imageString, font, new PointF(0, 0), format);
                }
            }

            // Draw the string
            Image output = new Bitmap((int)outputSize.Width, (int)outputSize.Height);
            using (Graphics graphics = Graphics.FromImage(output))
            {
                graphics.Clear(backgroundColor);

                using (Brush brush = new SolidBrush(textColour))
                {
                    graphics.DrawString(imageString, font, brush, 0, 0);
                    graphics.Save();
                }
            }

            return output;
        }

        float GetAverageBrightness(IList<Coordinate> coordinates)
        {
            if (coordinates.Count == 0)
                throw new ArgumentException("Coordinate IList is empty", "coordinates");

            float brightnessSum = 0f;
            for (int i = 0; i < coordinates.Count; ++i)
            {
                brightnessSum += brightnesses[coordinates[i].X + (coordinates[i].Y * imageSize.Width)];
            }

            return brightnessSum / coordinates.Count;
        }

        List<Coordinate> GetPixelsInGroup(Coordinate coord, int groupHeight, int groupWidth)
        {
            List<Coordinate> pixels = new List<Coordinate>();
            for (int y = coord.Y; y < (groupHeight + coord.Y); ++y)
            {
                for (int x = coord.X; x < (groupWidth + coord.X); ++x)
                {
                    if (x < imageSize.Width && y < imageSize.Height)
                        pixels.Add(new Coordinate(x, y));
                }
            }

            return pixels;
        }

        private SizeF GetMonospaceSize(Font font)
        {
            Image image = new Bitmap(1, 1);

            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.PageUnit = GraphicsUnit.Pixel;

                SizeF doubleSize = graphics.MeasureString("MM\nMM", font);
                SizeF singleSize = graphics.MeasureString("M", font);

                return doubleSize - singleSize;
            }
        }
    }
}
