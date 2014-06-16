using System;
using System.Drawing;
using System.IO;

namespace RealEstateParser.OCRs
{
    public abstract class SimpleOcr
    {
        public string Recognize(byte[] phoneImage)
        {
            using (var memory = new MemoryStream(phoneImage))
            using (var bmp = (Bitmap)Image.FromStream(memory))
            {
                var result = "";
                var currentRow = 0;
                var fromY = 0;

                for (var y = 0; y < bmp.Height; y++)
                {
                    var rowWeight = CalcRowWeight(y, bmp);
                    if (rowWeight == 0)
                    {
                        if (currentRow == 0)
                            continue;

                        result += ParseRow(fromY, y, bmp);

                        currentRow = 0;
                    }
                    else
                    {
                        if (currentRow == 0)
                            fromY = y;

                        currentRow += rowWeight;

                        if (y == bmp.Height - 1)
                            result += ParseRow(fromY, y, bmp);
                    }
                }

                return result;
            }
        }

        protected virtual string ParseRow(int fromY, int y, Bitmap bmp)
        {
            var currentLetter = 0;
            var result = "";

            for (var x = 0; x < bmp.Width; x++)
            {
                var columnWeight = CalcColumnWeight(x, fromY, y, bmp);
                if (columnWeight == 0)
                {
                    // end of the letter
                    if (currentLetter != 0)
                    {
                        result += ResolveLetter(currentLetter);
                        currentLetter = 0;
                    }
                    else
                    {
                        // start of text whitespace -> just ignore
                    }
                }
                else
                {
                    currentLetter += columnWeight;
                }
            }

            return result;
        }

        protected abstract Map[] Symbols {get;}
        protected class Map
        {
            public char Letter { get; set; }
            public int Weight { get; set; }
        }
        protected virtual string ResolveLetter(int currentLetter)
        {
            var bestLetter = '#';
            var bestWeight = int.MaxValue;

            foreach (var letter in Symbols)
            {
                var delta = Math.Abs(currentLetter - letter.Weight);

                if (delta < bestWeight)
                {
                    bestLetter = letter.Letter;
                    bestWeight = delta;
                }
            }

            return bestLetter.ToString();
        }

        protected virtual int CalcRowWeight(int y, Bitmap bmp)
        {
            var weight = 0;

            for (var x = 0; x < bmp.Width; x++)
            {
                var pixel = bmp.GetPixel(x, y);
                var r = 255 - pixel.R;
                var g = 255 - pixel.G;
                var b = 255 - pixel.B;

                weight += (r + g + b) / 3;
            }

            return weight;
        }
        protected virtual int CalcColumnWeight(int x, int yMin, int yMax, Bitmap bmp)
        {
            var weight = 0;

            for (var y = yMin; y < yMax; y++)
            {
                var pixel = bmp.GetPixel(x, y);
                var r = 255 - pixel.R;
                var g = 255 - pixel.G;
                var b = 255 - pixel.B;

                weight += (r + g + b) / 3;
            }

            return weight;
        }
    }

    public class AvitoOcr : SimpleOcr
    {
        protected override Map[] Symbols
        {
            get
            {
                return new Map[]{
                    new Map{Letter = '0', Weight = 6155},
                    new Map{Letter = '1', Weight = 4128},
                    new Map{Letter = '2', Weight = 5627},
                    new Map{Letter = '3', Weight = 5885},
                    new Map{Letter = '4', Weight = 6113},
                    new Map{Letter = '5', Weight = 6457},
                    new Map{Letter = '6', Weight = 5722},
                    new Map{Letter = '7', Weight = 4106},
                    new Map{Letter = '8', Weight = 7065},
                    new Map{Letter = '9', Weight = 5828},
                    new Map{Letter = '-', Weight = 1275},
                };
            }
        }
    }

    public class HandsOcr : SimpleOcr
    {
        protected override Map[] Symbols
        {
            get
            {
                return new Map[]{
                    new Map{Letter = '0', Weight = 6155},
                    new Map{Letter = '1', Weight = 4128},
                    new Map{Letter = '2', Weight = 5627},
                    new Map{Letter = '3', Weight = 5885},
                    new Map{Letter = '4', Weight = 6113},
                    new Map{Letter = '5', Weight = 6457},
                    new Map{Letter = '6', Weight = 5722},
                    new Map{Letter = '7', Weight = 4106},
                    new Map{Letter = '8', Weight = 7065},
                    new Map{Letter = '9', Weight = 5828},
                    new Map{Letter = '+', Weight = 1275},
                };
            }
        }
        const int MIN_WEIGHT = 170;
        protected override string ParseRow(int fromY, int y, Bitmap bmp)
        {
            var currentLetter = 0;
            var result = "";

            for (var x = 0; x < bmp.Width; x++)
            {
                var columnWeight = CalcColumnWeight(x, fromY, y, bmp);
                if (columnWeight < MIN_WEIGHT)
                {
                    // end of the letter
                    if (currentLetter != 0)
                    {
                        result += ResolveLetter(currentLetter);
                        currentLetter = 0;
                    }
                    else
                    {
                        // start of text whitespace -> just ignore
                    }
                }
                else
                {
                    currentLetter += columnWeight;
                }
            }

            return result;
        }

        protected override string ResolveLetter(int currentLetter)
        {
            if (currentLetter <= MIN_WEIGHT) return String.Empty;

            var bestLetter = "#";
            var bestWeight = int.MaxValue;

            foreach (var letter in Symbols)
            {
                var delta = Math.Abs(currentLetter - letter.Weight);

                if (delta < bestWeight)
                {
                    bestLetter = letter.Letter.ToString();
                    bestWeight = delta;
                }
            }

            return bestLetter;
        }
    }

    
}
