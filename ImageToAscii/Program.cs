using System;
using System.Drawing;
using SchnappiSchnap.Imaging;

class Program
{
    static void Main()
    {
        Bitmap greyscale = new Bitmap(@"greyscale_input.png");
        Bitmap cat = new Bitmap(@"cat_input.bmp");
        Font font = new Font("Lucida Console", 12, FontStyle.Regular);

        ImageToAscii greyAsciiImage = new ImageToAscii(greyscale);
        Image greyOutput = greyAsciiImage.GenerateImage(font, Color.White, Color.Black);
        greyOutput.Save(@"greyscale_output.PNG");

        ImageToAscii catAsciiImage = new ImageToAscii(cat);
        Image catOutput = catAsciiImage.GenerateImage(font, Color.White, Color.Black);
        catOutput.Save(@"cat_output.PNG");
    }
}