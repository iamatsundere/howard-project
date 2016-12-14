using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Media.Imaging;

namespace BTLXLA
{
    public class ImageClass
    {
        public static int[,] maskSobelDx = new int[3, 3] { { 1, 0, -1 }, { 2, 0, -2 }, { 1, 0, -1 } };
        public static int[,] maskSobelDy = new int[3, 3] { { 1, 2, 1 }, { 0, 0, 0 }, { -1, -2, -1 } };

        public static int[,] maskPrewittDx = new int[3, 3] { { 1, 0, -1 }, { 1, 0, -1 }, { 1, 0, -1 } };
        public static int[,] maskPrewittDy = new int[3, 3] { { -1, -1, -1 }, { 0, 0, 0 }, { 1, 1, 1 } };

        public static WriteableBitmap ConvertDoubleArrToBitmap(double[,] arr)
        {
            WriteableBitmap bmpOutput = new WriteableBitmap(arr.GetLength(1), arr.GetLength(0));
            int intHeight = arr.GetLength(0);
            int intWidth = arr.GetLength(1);
            byte[,] bytData = new byte[intHeight, intWidth];

            for (int y = 0; y < intHeight; y++)
            {
                for (int x = 0; x < intWidth; x++)
                {
                    if (arr[y, x] < 0)
                    {
                        bmpOutput.SetPixel(x, y, 0);
                    }
                    else if (arr[y, x] > 255)
                    {
                        bmpOutput.SetPixel(x, y, 255);
                    }
                    else
                        bmpOutput.SetPixel(x, y, (int)arr[y, x]);
                }
            }
            return bmpOutput;
        }

        public static double[,] ConvertBitmapToDoubleArr(WriteableBitmap bmp)
        {
            int intHeight = bmp.PixelHeight;
            int intWidth = bmp.PixelWidth;
            double[,] bytData = new double[intHeight, intWidth];

            for (int y = 0; y < intHeight; y++)
            {
                for (int x = 0; x < intWidth; x++)
                {
                    Windows.UI.Color c = bmp.GetPixel(x, y);
                    double gray = 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;
                    bytData[y, x] = gray;
                }
            }
            return bytData;
        }


        public static double[,] ConvolutionFilter(WriteableBitmap bmpInput, int[,] maskByte, int bias)
        {
            double[,] arrRes = new double[bmpInput.PixelHeight, bmpInput.PixelWidth];
            int intHeight = bmpInput.PixelHeight;
            int intWidth = bmpInput.PixelWidth;
            byte[,] bytData = new byte[intHeight, intWidth];
            bytData = ImageClass.GetMatrixData(bmpInput);

            for (int y = 0; y < intHeight; y++)
            {
                for (int x = 0; x < intWidth; x++)
                {
                    int sum = 0;
                    for (int y_mask = 0; y_mask < maskByte.GetLength(1); y_mask++)
                    {
                        for (int x_mask = 0; x_mask < maskByte.GetLength(0); x_mask++)
                            if ((x - (x_mask - 1)) < 0 || (x - (x_mask - 1)) >= intWidth
                                || (y - (y_mask - 1)) < 0 || (y - (y_mask - 1) >= intHeight))
                            {
                                sum += 0;
                            }
                            else
                            {
                                sum = sum + (int)(bytData[y - (y_mask - 1), x - (x_mask - 1)] * maskByte[y_mask, x_mask]);
                            }
                    }
                    arrRes[y, x] = sum / (double)(bias);
                }
            }
            return arrRes;
        }

        public static double[,] ConvolutionFilter(double[,] bmpInput, int[,] maskByte, int bias)
        {
            double[,] arrRes = new double[bmpInput.GetLength(0), bmpInput.GetLength(1)];
            int intHeight = bmpInput.GetLength(0);
            int intWidth = bmpInput.GetLength(1);
            //byte[,] bytData = new byte[intHeight, intWidth];
            //bytData = ImageClass.GetMatrixData(bmpInput);

            for (int y = 1; y < intHeight - 1; y++)
            {
                for (int x = 1; x < intWidth - 1; x++)
                {
                    int sum = 0;
                    for (int y_mask = 0; y_mask < maskByte.GetLength(1); y_mask++)
                    {
                        for (int x_mask = 0; x_mask < maskByte.GetLength(0); x_mask++)
                            if ((x - (x_mask - 1)) < 0 || (x - (x_mask - 1)) >= intWidth
                                || (y - (y_mask - 1)) < 0 || (y - (y_mask - 1) >= intHeight))
                            {
                                sum += 0;
                            }
                            else
                            {
                                sum = sum + (int)(bmpInput[y - (y_mask - 1), x - (x_mask - 1)] * maskByte[y_mask, x_mask]);
                            }
                    }
                    arrRes[y, x] = sum / (double)(bias);
                }
            }
            return arrRes;
        }

        public static double[,] Crop(double[,] arrIn, int x, int y, int w, int h)
        {
            if (y + h >= arrIn.GetLength(0))
                h = arrIn.GetLength(0) - y - 1;
            if (x + w >= arrIn.GetLength(1))
                w = arrIn.GetLength(1) - x - 1;

            double[,] arrRes = new double[h, w];
            for (int i = x; i < x + w; i++)
            {
                for (int j = y; j < y + h; j++)
                {
                    arrRes[j - y, i - x] = arrIn[y, x];
                }
            }
            return arrRes;
        }

        public static double[,] NoiseFilter(double[,] bmpInput, string type)
        {
            int intHeight = bmpInput.GetLength(0);
            int intWidth = bmpInput.GetLength(1);
            double[,] bitmapOutput = new double[intHeight, intWidth];
            //lock bits to memory, disable bit A just execute the RGB image
            //turn image to an array of bits (1 dimension) start with its first address in memory
            for (int i = 0; i <= intHeight - 1; i++)
            {
                for (int j = 0; j <= intWidth - 1; j++)
                {
                    if ((i > 0) && (i < intHeight - 1) && (j > 0) && (j < intWidth - 1))
                    {
                        double[] arr = new double[9] {bmpInput[i-1,j-1], bmpInput[i-1, j], bmpInput[i-1, j+1],
                            bmpInput[i,j-1],bmpInput[i,j],bmpInput[i,j+1],
                            bmpInput[i+1,j-1],bmpInput[i+1,j],bmpInput[i+1,j+1]};
                        bitmapOutput[i, j] = FindByte(arr, type);
                    }
                }
            }
            return bitmapOutput;
        }


        const string fMax = "MAX";
        const string fMed = "MED";
        const string fMin = "MIN";
        public static double FindByte(double[] arr, string type)
        {
            double[] arrI = new double[9];
            for (int i = 0; i < 9; i++)
                arrI[i] = arr[i];
            Array.Sort(arrI);
            if (type.CompareTo(fMax) == 0)
                return (byte)arrI[8];
            if (type.CompareTo(fMed) == 0)
                return (byte)arrI[5];
            if (type.CompareTo(fMin) == 0)
                return (byte)arrI[0];
            return 0;
        }

        public static void MakeGrayscale2Bitmap(WriteableBitmap original)
        {
            for (int i = 0; i < original.PixelHeight; i++)
                for (int j = 0; j < original.PixelWidth; j++)
                {
                    byte x = (byte)((original.GetPixel(j, i).R + original.GetPixel(j, i).G + original.GetPixel(j, i).B) / 3);
                    original.SetPixel(j, i, Windows.UI.Color.FromArgb(255, x, x, x));
                }
        }

        public static double[,] MakeGrayscale2Double(WriteableBitmap original)
        {
            double[,] arr = new double[original.PixelHeight, original.PixelHeight];
            int intHeight = original.PixelHeight;
            int intWidth = original.PixelHeight;

            for (int y = 0; y < intHeight; y++)
            {
                for (int x = 0; x < intWidth; x++)
                {
                    Windows.UI.Color c = original.GetPixel(x, y);
                    double gray = 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;
                    arr[y, x] = gray;
                }
            }
            return arr;
        }

        //public static WriteableBitmap MakeGrayscale3(WriteableBitmap original)
        //{
        //    try
        //    {
        //        //create a blank bitmap the same size as original
        //        WriteableBitmap newBitmap = new WriteableBitmap(original.PixelWidth, original.PixelHeight);

        //        //get a graphics object from the new image
        //        Graphics g = Graphics.FromImage(newBitmap);

        //        //create the grayscale ColorMatrix
        //        ColorMatrix colorMatrix = new ColorMatrix(
        //           new float[][]
        //           {
        // new float[] {.3f, .3f, .3f, 0, 0},
        // new float[] {.59f, .59f, .59f, 0, 0},
        // new float[] {.11f, .11f, .11f, 0, 0},
        // new float[] {0, 0, 0, 1, 0},
        // new float[] {0, 0, 0, 0, 1}
        //           });

        //        //create some image attributes
        //        ImageAttributes attributes = new ImageAttributes();

        //        //set the color matrix attribute
        //        attributes.SetColorMatrix(colorMatrix);

        //        //draw the original image on the new image
        //        //using the grayscale color matrix
        //        g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
        //           0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);

        //        //dispose the Graphics object
        //        g.Dispose();
        //        return newBitmap;
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}

        public static void NegativeT(WriteableBitmap bmpInput)
        {
            //    // Lock the bitmap's bits.  
            //    Rectangle rtg = new Rectangle(0, 0, bmpInput.PixelWidth, bmpInput.PixelHeight);
            //    BitmapData bmpData = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, bmpInput.PixelFormat);

            //    // Get the address of the first line.
            //    IntPtr ptr = bmpData.Scan0;

            //    // Declare an array to hold the bytes of the bitmap.
            //    // This code is specific to a bitmap with 24 bits per pixels.

            //    int bytes = bmpInput.PixelWidth * bmpInput.PixelHeight * 3;
            //    byte[] rgbValues = new byte[bytes];

            //    // Copy the RGB values into the array.
            //    System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            //    for (int counter = 0; counter < rgbValues.Length; counter += 3)

            //    {
            //        //counter = (x * bmpInput.PixelWidth + y) * 3;

            //        rgbValues[counter + 0] = (byte)(255 - rgbValues[counter + 0]);
            //        rgbValues[counter + 1] = (byte)(255 - rgbValues[counter + 1]);
            //        rgbValues[counter + 2] = (byte)(255 - rgbValues[counter + 2]);
            //    }

            //    //MessageBox.Show(counter.ToString());

            //    // Copy the RGB values back to the bitmap
            //    System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            //    // Unlock the bits.
            //    bmpInput.UnlockBits(bmpData);
        }
        public static void NegativeT2(WriteableBitmap bmpInput)
        {


            //// Lock the bitmap's bits.  
            //Rectangle rtg = new Rectangle(0, 0, bmpInput.PixelWidth, bmpInput.PixelHeight);
            //BitmapData bmpData = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, bmpInput.PixelFormat);

            //// Get the address of the first line.
            //IntPtr ptr = bmpData.Scan0;

            //// Declare an array to hold the bytes of the bitmap.
            //// This code is specific to a bitmap with 24 bits per pixels.
            //int bytes = bmpInput.PixelWidth * bmpInput.PixelHeight * 3;
            //byte[] rgbValues = new byte[bytes];
            //int x = 0;
            //int y = 0;
            //int counter = 0;

            //// Copy the RGB values into the array.
            //System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            //for (x = 0; x < bmpInput.PixelHeight; x++)
            //{
            //    for (y = 0; y < bmpInput.PixelWidth; y++)
            //    {
            //        counter = (x * bmpInput.PixelWidth + y) * 3;

            //        rgbValues[counter + 0] = (byte)(255 - rgbValues[counter + 0]);
            //        rgbValues[counter + 1] = (byte)(255 - rgbValues[counter + 1]);
            //        rgbValues[counter + 2] = (byte)(255 - rgbValues[counter + 2]);
            //    }
            //}

            //// Copy the RGB values back to the bitmap
            //System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            //// Unlock the bits.
            //bmpInput.UnlockBits(bmpData);
        }
        public static void LogT(WriteableBitmap bmpInput)
        {
            //// Lock the bitmap's bits.  
            //Rectangle rtg = new Rectangle(0, 0, bmpInput.PixelWidth, bmpInput.PixelHeight);
            //BitmapData bmpData = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, bmpInput.PixelFormat);

            //// Get the address of the first line.
            //IntPtr ptr = bmpData.Scan0;
            //// Declare an array to hold the bytes of the bitmap.
            //// This code is specific to a bitmap with 24 bits per pixels.
            //int bytes = bmpInput.PixelWidth * bmpInput.PixelHeight * 3;
            //byte[] rgbValues = new byte[bytes];
            //// Copy the RGB values into the array.
            //System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);
            //double GrayLevel;
            //double c = 2.0;
            //for (int counter = 0; counter < rgbValues.Length; counter += 3)
            //{
            //    GrayLevel = (byte)((rgbValues[counter] + rgbValues[counter + 1] + rgbValues[counter + 2]) / 3);
            //    GrayLevel = (byte)(c * 255 * Math.Log((double)((double)GrayLevel / 255 + 1), 2));
            //    rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = (byte)GrayLevel;
            //}
            //// Copy the RGB values back to the bitmap
            //System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);
            //// Unlock the bits.
            //bmpInput.UnlockBits(bmpData);
        }
        public static void PowerT(WriteableBitmap bmpInput, double mu)
        {
            //// Lock the bitmap's bits.  
            //Rectangle rtg = new Rectangle(0, 0, bmpInput.PixelWidth, bmpInput.PixelHeight);
            //BitmapData bmpData = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, bmpInput.PixelFormat);
            //// Get the address of the first line.
            //IntPtr ptr = bmpData.Scan0;
            //// Declare an array to hold the bytes of the bitmap.
            //// This code is specific to a bitmap with 24 bits per pixels.
            //int bytes = bmpInput.PixelWidth * bmpInput.PixelHeight * 3;
            //byte[] rgbValues = new byte[bytes];
            //// Copy the RGB values into the array.
            //System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);
            //double x;
            //byte GrayLevel;
            //for (int counter = 0; counter < rgbValues.Length; counter += 3)
            //{
            //    x = Convert.ToDouble(rgbValues[counter]) / 255;
            //    GrayLevel = Convert.ToByte(255 * Math.Pow(x, mu));
            //    rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = GrayLevel;
            //}
            //// Copy the RGB values back to the bitmap
            //System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);
            //// Unlock the bits.
            //bmpInput.UnlockBits(bmpData);
        }
        public static void PiecewiseLinearT(WriteableBitmap bmpInput)
        {
            //byte r1;
            //byte r2;

            //// Lock the bitmap's bits.  
            //Rectangle rtg = new Rectangle(0, 0, bmpInput.PixelWidth, bmpInput.PixelHeight);
            //BitmapData bmpData = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, bmpInput.PixelFormat);

            //// Get the address of the first line.
            //IntPtr ptr = bmpData.Scan0;

            //// Declare an array to hold the bytes of the bitmap.
            //// This code is specific to a bitmap with 24 bits per pixels.
            //int bytes = bmpInput.PixelWidth * bmpInput.PixelHeight * 3;
            //byte[] rgbValues = new byte[bytes];

            //// Copy the RGB values into the array.
            //System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            //r1 = 255;
            //r2 = 0;

            //for (int counter = 0; counter < rgbValues.Length; counter += 3)
            //{
            //    if (r2 < rgbValues[counter]) r2 = rgbValues[counter];
            //    if (r1 > rgbValues[counter]) r1 = rgbValues[counter];
            //}

            //double GrayLevel;

            //for (int counter = 0; counter < rgbValues.Length; counter += 3)
            //{
            //    if (rgbValues[counter] <= r1)
            //        rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = 0;
            //    else
            //        if (rgbValues[counter] >= r2)
            //        rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = 255;
            //    else
            //    {
            //        // GrayLevel = 255 * ((rgbValues[counter] - r1) / (r2 - r1)); -> Lỗi nghiêm trọng
            //        GrayLevel = (255 * (rgbValues[counter] - r1)) / (r2 - r1);
            //        rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = (byte)GrayLevel;
            //    }
            //}

            //// Copy the RGB values back to the bitmap
            //System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            //// Unlock the bits.
            //bmpInput.UnlockBits(bmpData);
        }
        public static void BitsPlaneExt(WriteableBitmap bmpInput, byte np)
        {
            //// Lock the bitmap's bits.  
            //Rectangle rtg = new Rectangle(0, 0, bmpInput.PixelWidth, bmpInput.PixelHeight);
            //BitmapData bmpData = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, bmpInput.PixelFormat);

            //// Get the address of the first line.
            //IntPtr ptr = bmpData.Scan0;

            //// Declare an array to hold the bytes of the bitmap.
            //// This code is specific to a bitmap with 24 bits per pixels.
            //int bytes = bmpInput.PixelWidth * bmpInput.PixelHeight * 3;
            //byte[] rgbValues = new byte[bytes];

            //// Copy the RGB values into the array.
            //System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            //int GrayLevel;
            //byte matna = 0;
            //switch (np)
            //{
            //    case 7: { matna = 128; } break;
            //    case 6: { matna = 64; } break;
            //    case 5: { matna = 32; } break;
            //    case 4: { matna = 16; } break;
            //    case 3: { matna = 8; } break;
            //    case 2: { matna = 4; } break;
            //    case 1: { matna = 2; } break;
            //    case 0: { matna = 1; } break;
            //}
            //for (int counter = 0; counter < rgbValues.Length; counter += 3)
            //{
            //    GrayLevel = rgbValues[counter] & matna;
            //    if (GrayLevel > 0)
            //    {
            //        rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = 255;
            //    }
            //    else
            //    {
            //        rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = 0;
            //    }
            //}

            //// Copy the RGB values back to the bitmap
            //System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            //// Unlock the bits.
            //bmpInput.UnlockBits(bmpData);
        }


        public static void HistogramEqu(WriteableBitmap bmpInput)
        {
            //// Lock the bitmap's bits.
            //    Rectangle rtg = new Rectangle(0, 0, bmpInput.PixelWidth, bmpInput.PixelHeight);
            //    BitmapData bmpData = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, bmpInput.PixelFormat);
            //    // Get the address of the first line.
            //    IntPtr ptr = bmpData.Scan0;
            //    // Declare an array to hold the bytes of the bitmap.
            //    // This code is specific to a bitmap with 24 bits per pixels.
            //    int bytes = bmpInput.PixelWidth * bmpInput.PixelHeight * 3;
            //    byte[] rgbValues = new byte[bytes];
            //    long[] His = new long[256];
            //    // Copy the RGB values into the array.
            //    System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            //    for (int i = 0; i < 256; i++) His[i] = 0; // Đặt các histogram về không

            //    for (int counter = 0; counter < rgbValues.Length; counter += 3)
            //        His[rgbValues[counter]] = His[rgbValues[counter]] + 1; // Tăng số biến đếm của mỗi histogram
            //    for (int i = 1; i < 256; i++) His[i] = His[i] + His[i - 1];
            //    long tongHis = His[255];
            //    for (int i = 0; i < 256; i++) His[i] = (255 * His[i]) / (tongHis);
            //    for (int counter = 0; counter < rgbValues.Length; counter += 3)
            //    {
            //        rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = (byte)His[rgbValues[counter]];
            //    }
            //    // Copy the RGB values back to the bitmap
            //    System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);
            //    bmpInput.UnlockBits(bmpData);
        }

        public static void HistogramMatching(WriteableBitmap bmpInput)
        {
            //// Lock the bitmap's bits.  
            //Rectangle rtg = new Rectangle(0, 0, bmpInput.PixelWidth, bmpInput.PixelHeight);
            //BitmapData bmpData = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, bmpInput.PixelFormat);

            //// Get the address of the first line.
            //IntPtr ptr = bmpData.Scan0;

            //// Declare an array to hold the bytes of the bitmap.
            //// This code is specific to a bitmap with 24 bits per pixels.
            //int bytes = bmpInput.PixelWidth * bmpInput.PixelHeight * 3;
            //byte[] rgbValues = new byte[bytes];
            //long[] His = new long[256];
            //long[] SfHis = new long[256];

            //// Copy the RGB values into the array.
            //System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            //// Đặt các histogram về không
            //for (int i = 0; i < 256; i++)
            //{
            //    His[i] = 0;
            //    //SfHis[i] = 255-i; //->Hay
            //    SfHis[i] = i;   // Cũng hay
            //    //SfHis[i] = (bmpInput.PixelWidth * bmpInput.PixelHeight) / 255; // Không biến đổi
            //    //SfHis[i] = 100; // Không biến đổi
            //}


            //// Tăng số biến đếm của mỗi histogram
            //for (int counter = 0; counter < rgbValues.Length; counter += 3) His[rgbValues[counter]]++;
            //// công dồn

            //for (int i = 1; i < 256; i++)
            //{
            //    His[i] = His[i] + His[i - 1];
            //    SfHis[i] = SfHis[i] + SfHis[i - 1];
            //}

            //long tongHis = His[255];
            //long tongSfHis = SfHis[255];

            //for (int i = 0; i < 256; i++)
            //{
            //    His[i] = (255 * His[i]) / (tongHis);
            //    SfHis[i] = (255 * SfHis[i]) / (tongSfHis);
            //}

            //long hValue;
            //byte sIndex = 0;

            //for (int counter = 0; counter < rgbValues.Length; counter += 3)
            //{

            //    hValue = His[rgbValues[counter]];

            //    for (int j = 0; j < 256; j++)
            //    {
            //        if (hValue < SfHis[j])
            //        {
            //            if (j - 1 >= 0)
            //                sIndex = (byte)(j - 1); // Nên tính trước giá trị này
            //            else sIndex = 0;
            //            break;
            //        }
            //    }
            //    rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = sIndex;
            //}

            //// Copy the RGB values back to the bitmap
            //System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            //// Unlock the bits.
            //bmpInput.UnlockBits(bmpData);
        }
        public static void HistogramStatistic(WriteableBitmap bmpInput)
        {
            //// Lock the bitmap's bits.  
            //Rectangle rtg = new Rectangle(0, 0, bmpInput.PixelWidth, bmpInput.PixelHeight);
            //BitmapData bmpData = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, bmpInput.PixelFormat);

            //// Get the address of the first line.
            //IntPtr ptr = bmpData.Scan0;

            //// Declare an array to hold the bytes of the bitmap.
            //// This code is specific to a bitmap with 24 bits per pixels.
            //int bytes = bmpInput.PixelWidth * bmpInput.PixelHeight * 3;

            //byte[] rgbValues = new byte[bytes];
            //double[] p = new double[256];
            //int n = bmpInput.PixelWidth * bmpInput.PixelHeight;
            //double Dg = 0;
            //double Mg = 0;

            //// Copy the RGB values into the array.
            //System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            //// Đặt các histogram về không
            //for (int i = 0; i < 256; i++) { p[i] = 0; }

            //// Tính nk =pk
            //for (int counter = 0; counter < rgbValues.Length; counter += 3) p[rgbValues[counter]]++;

            //// Tính pk
            //for (int i = 0; i < 256; i++) { p[i] = p[i] / n; }

            //// Tính Mg
            //Mg = 0;
            //for (int i = 0; i < 256; i++) { Mg = Mg + i * p[i]; } //            MessageBox.Show(Mg.ToString());
            //// Tính Dg
            //Dg = 0;
            //for (int i = 0; i < 256; i++) { Dg = Dg + (i - Mg) * (i - Mg) * p[i]; }//   MessageBox.Show(Dg.ToString());

            //for (int counter = 0; counter < rgbValues.Length; counter += 3)
            //{
            //    rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = 0; // (byte)hValue;
            //}

            //// Copy the RGB values back to the bitmap
            //System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            //// Unlock the bits.
            //bmpInput.UnlockBits(bmpData);
        }
        public static void LogicOperators(WriteableBitmap bmpInput, WriteableBitmap orMark, WriteableBitmap AndMark)
        {
            //// Lock the bitmap's bits.  
            //Rectangle rtg = new Rectangle(0, 0, bmpInput.PixelWidth, bmpInput.PixelHeight);
            //BitmapData bmpData = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, bmpInput.PixelFormat);
            //BitmapData bmpDataOr = orMark.LockBits(rtg, ImageLockMode.ReadOnly, orMark.PixelFormat);
            //BitmapData bmpDataAnd = AndMark.LockBits(rtg, ImageLockMode.ReadOnly, AndMark.PixelFormat);

            //// Get the address of the first line.
            //IntPtr ptr = bmpData.Scan0;
            //IntPtr ptrOr = bmpDataOr.Scan0;
            //IntPtr ptrAnd = bmpDataAnd.Scan0;

            //// Declare an array to hold the bytes of the bitmap.
            //// This code is specific to a bitmap with 24 bits per pixels.
            //int bytes = bmpInput.PixelWidth * bmpInput.PixelHeight * 3;

            //byte[] rgbValues = new byte[bytes];
            //byte[] Or_Values = new byte[bytes];
            //byte[] AndValues = new byte[bytes];

            //// Copy the RGB values into the array.
            //System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);
            //System.Runtime.InteropServices.Marshal.Copy(ptrOr, Or_Values, 0, bytes);
            //System.Runtime.InteropServices.Marshal.Copy(ptrAnd, AndValues, 0, bytes);

            //int hValue = 0;
            //for (int counter = 0; counter < rgbValues.Length; counter += 3)
            //{
            //    hValue = rgbValues[counter] & AndValues[counter];
            //    hValue = hValue | Or_Values[counter];
            //    rgbValues[counter] = (byte)hValue;

            //    //rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = (byte)hValue;

            //    hValue = rgbValues[counter + 1] & AndValues[counter + 1];
            //    hValue = hValue | Or_Values[counter + 1];
            //    rgbValues[counter + 1] = (byte)hValue;

            //    hValue = rgbValues[counter + 2] & AndValues[counter + 2];
            //    hValue = hValue | Or_Values[counter + 2];
            //    rgbValues[counter + 2] = (byte)hValue;
            //}

            //// Copy the RGB values back to the bitmap
            //System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            //// Unlock the bits.
            //bmpInput.UnlockBits(bmpData);
            //orMark.UnlockBits(bmpDataOr);
            //AndMark.UnlockBits(bmpDataAnd);
        }
        public static void Subtraction(WriteableBitmap bmpInput, WriteableBitmap bmpSub)
        {
            //// Lock the bitmap's bits.  
            //Rectangle rtg = new Rectangle(0, 0, bmpInput.PixelWidth, bmpInput.PixelHeight);
            //BitmapData bmpData = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, bmpInput.PixelFormat);
            //BitmapData bmpDataSub = bmpSub.LockBits(rtg, ImageLockMode.ReadOnly, bmpSub.PixelFormat);

            //// Get the address of the first line.
            //IntPtr ptr = bmpData.Scan0;
            //IntPtr ptrSub = bmpDataSub.Scan0;


            //// Declare an array to hold the bytes of the bitmap.
            //// This code is specific to a bitmap with 24 bits per pixels.
            //int bytes = bmpInput.PixelWidth * bmpInput.PixelHeight * 3;

            //byte[] rgbValues = new byte[bytes];
            //byte[] SubValues = new byte[bytes];

            //// Copy the RGB values into the array.
            //System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);
            //System.Runtime.InteropServices.Marshal.Copy(ptrSub, SubValues, 0, bytes);

            //double hValue = 0;
            //for (int counter = 0; counter < rgbValues.Length; counter += 3)
            //{
            //    hValue = (rgbValues[counter] - SubValues[counter] + 255) / 2;
            //    rgbValues[counter] = (byte)hValue;


            //    //rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = (byte)hValue;

            //    hValue = (rgbValues[counter + 1] - SubValues[counter + 1] + 255) / 2;
            //    rgbValues[counter + 1] = (byte)hValue;

            //    hValue = (rgbValues[counter + 2] - SubValues[counter + 2] + 255) / 2;
            //    rgbValues[counter + 2] = (byte)hValue;
            //}

            //// Copy the RGB values back to the bitmap
            //System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            //// Unlock the bits.
            //bmpInput.UnlockBits(bmpData);
            //bmpSub.UnlockBits(bmpDataSub);
        }
        public static void Average(WriteableBitmap bmpInput, WriteableBitmap bmpDoituong, byte k)
        {
            //// Lock the bitmap's bits.  
            //Rectangle rtg = new Rectangle(0, 0, bmpInput.PixelWidth, bmpInput.PixelHeight);
            //BitmapData bmpData = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, bmpInput.PixelFormat);
            //BitmapData bmpDatadt = bmpDoituong.LockBits(rtg, ImageLockMode.ReadWrite, bmpDoituong.PixelFormat);

            //// Get the address of the first line.
            //IntPtr ptr = bmpData.Scan0;
            //IntPtr ptrdt = bmpDatadt.Scan0;


            //// Declare an array to hold the bytes of the bitmap.
            //// This code is specific to a bitmap with 24 bits per pixels.
            //int bytes = bmpInput.PixelWidth * bmpInput.PixelHeight * 3;

            //byte[] rgbValues = new byte[bytes];
            //byte[] dtgValues = new byte[bytes];

            //// Copy the RGB values into the array.
            //System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);
            //System.Runtime.InteropServices.Marshal.Copy(ptrdt, dtgValues, 0, bytes);

            //double hValue = 0;
            //for (int counter = 0; counter < rgbValues.Length; counter += 3)
            //{
            //    hValue = rgbValues[counter] + dtgValues[counter];
            //    for (int i = 0; i < k - 1; i++) hValue += rgbValues[counter];
            //    hValue = hValue / k;
            //    rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = (byte)hValue;
            //}

            //// Copy the RGB values back to the bitmap
            //System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            //// Unlock the bits.
            //bmpInput.UnlockBits(bmpData);

            //bmpDoituong.UnlockBits(bmpData);
        }

        #region Predefine for Fourier Transform
        private const int maxBits = 14;
        private static int[][] reversedBits = new int[maxBits][];
        private static Complex[,][] complexRotation = new Complex[maxBits, 2][];
        public enum FourierDirection
        {
            Forward = 1,
            Backward = -1
        };
        public static int Log2(int x)
        {
            if (x <= 65536)
            {
                if (x <= 256)
                {
                    if (x <= 16)
                    {
                        if (x <= 4)
                        {
                            if (x <= 2)
                            {
                                if (x <= 1)
                                    return 0;
                                return 1;
                            }
                            return 2;
                        }
                        if (x <= 8)
                            return 3;
                        return 4;
                    }
                    if (x <= 64)
                    {
                        if (x <= 32)
                            return 5;
                        return 6;
                    }
                    if (x <= 128)
                        return 7;
                    return 8;
                }
                if (x <= 4096)
                {
                    if (x <= 1024)
                    {
                        if (x <= 512)
                            return 9;
                        return 10;
                    }
                    if (x <= 2048)
                        return 11;
                    return 12;
                }
                if (x <= 16384)
                {
                    if (x <= 8192)
                        return 13;
                    return 14;
                }
                if (x <= 32768)
                    return 15;
                return 16;
            }

            if (x <= 16777216)
            {
                if (x <= 1048576)
                {
                    if (x <= 262144)
                    {
                        if (x <= 131072)
                            return 17;
                        return 18;
                    }
                    if (x <= 524288)
                        return 19;
                    return 20;
                }
                if (x <= 4194304)
                {
                    if (x <= 2097152)
                        return 21;
                    return 22;
                }
                if (x <= 8388608)
                    return 23;
                return 24;
            }
            if (x <= 268435456)
            {
                if (x <= 67108864)
                {
                    if (x <= 33554432)
                        return 25;
                    return 26;
                }
                if (x <= 134217728)
                    return 27;
                return 28;
            }
            if (x <= 1073741824)
            {
                if (x <= 536870912)
                    return 29;
                return 30;
            }
            return 31;
        }
        public static int Pow2(int exp)
        {
            return ((exp >= 0) && (exp <= 30)) ? (1 << exp) : 0;
        }
        public static void ReorderData(Complex[] data)
        {
            int len = data.Length;

            int[] rBits = GetReversedBits(Log2(len));

            for (int i = 0; i < len; i++)
            {
                int s = rBits[i];

                if (s > i)
                {
                    Complex t = data[i];
                    data[i] = data[s];
                    data[s] = t;
                }
            }
        }
        public static int[] GetReversedBits(int numberOfBits)
        {
            // check if the array is already calculated
            if (reversedBits[numberOfBits - 1] == null)
            {
                int n = Pow2(numberOfBits);
                int[] rBits = new int[n];

                // calculate the array
                for (int i = 0; i < n; i++)
                {
                    int oldBits = i;
                    int newBits = 0;

                    for (int j = 0; j < numberOfBits; j++)
                    {
                        newBits = (newBits << 1) | (oldBits & 1);
                        oldBits = (oldBits >> 1);
                    }
                    rBits[i] = newBits;
                }
                reversedBits[numberOfBits - 1] = rBits;
            }
            return reversedBits[numberOfBits - 1];
        }

        public static Complex[] GetComplexRotation(int numberOfBits, FourierDirection direction)
        {
            int directionIndex = (direction == FourierDirection.Forward) ? 0 : 1;

            // check if the array is already calculated
            if (complexRotation[numberOfBits - 1, directionIndex] == null)
            {
                int n = 1 << (numberOfBits - 1);
                float uR = 1.0f;
                float uI = 0.0f;
                double angle = System.Math.PI / n * (int)direction;
                float wR = (float)System.Math.Cos(angle);
                float wI = (float)System.Math.Sin(angle);
                float t;
                Complex[] rotation = new Complex[n];

                for (int i = 0; i < n; i++)
                {
                    rotation[i] = new Complex(uR, uI);
                    t = uR * wI + uI * wR;
                    uR = uR * wR - uI * wI;
                    uI = t;
                }

                complexRotation[numberOfBits - 1, directionIndex] = rotation;
            }
            return complexRotation[numberOfBits - 1, directionIndex];
        }

        public struct Complex
        {
            public float Re;		// real component
            public float Im;		// imaginary component
            // Constructors
            public Complex(float re, float im)
            {
                this.Re = re;
                this.Im = im;
            }
            public Complex(Complex c)
            {
                this.Re = c.Re;
                this.Im = c.Im;
            }
            // Get magnitude (absolute) value
            public float Magnitude
            {
                get { return (float)System.Math.Sqrt(Re * Re + Im * Im); }
            }
        }

        Complex[,] data;

        #endregion



        public void Gradient(WriteableBitmap bmpInput)
        {
            //int intHeight = bmpInput.PixelHeight;
            //int intWidth = bmpInput.PixelWidth;
            //byte[,] bytData = new byte[intHeight, intWidth];
            //bytData = GetMatrixData(bmpInput);

            //Rectangle rtg = new Rectangle(0, 0, bmpInput.PixelWidth, bmpInput.PixelHeight);
            //BitmapData datInput = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            //IntPtr ptrScan0 = datInput.Scan0;
            //int intStride = datInput.Stride;
            //int intSpace = intStride - intWidth * 3;
            //unsafe
            //{
            //    byte* ptr = (byte*)ptrScan0;
            //    int[,] intMask1 = new int[3, 3] { { 0, -1, 0 }, { -1, 4, -1 }, { 0, -1, 0 } };
            //    int intValue;
            //    for (int i = 0; i <= intHeight - 1; i++)
            //    {
            //        for (int j = 0; j <= intWidth - 1; j++)
            //        {
            //            if ((i > 0) && (i < intHeight - 1) && (j > 0) && (j < intWidth - 1))
            //            {
            //                intValue = (PointCalc(bytData, intMask1, i, j)); //+ PointCalc(bytData, intMask2, i, j));
            //                if (intValue > 255) intValue = 255;
            //                if (intValue < 0) intValue = 0;
            //                ptr[0] = ptr[1] = ptr[2] = (byte)intValue;
            //            }
            //            ptr += 3;
            //        }
            //        ptr += intSpace;
            //    }
            //}
            //bmpInput.UnlockBits(datInput);
        }


        static private int PointCalc(byte[,] bytImage, int[,] intMask, int X, int Y)
        {
            return (Math.Abs(intMask[0, 0] * bytImage[X - 1, Y - 1]
                                + intMask[0, 1] * bytImage[X - 1, Y + 0]
                                + intMask[0, 2] * bytImage[X - 1, Y + 1]
                                + intMask[1, 0] * bytImage[X + 0, Y - 1]
                                + intMask[1, 1] * bytImage[X + 0, Y + 0]
                                + intMask[1, 2] * bytImage[X + 0, Y + 1]
                                + intMask[2, 0] * bytImage[X + 1, Y - 1]
                                + intMask[2, 1] * bytImage[X + 1, Y + 0]
                                + intMask[2, 2] * bytImage[X + 1, Y + 1]));
        }

        public static byte[,] GetMatrixData(WriteableBitmap bmpInput)
        {
            int intHeight = bmpInput.PixelHeight;
            int intWidth = bmpInput.PixelWidth;

            byte[,] bytResult = new byte[intHeight, intWidth];
            double dblGray;
            byte bytGray;
            for (int i = 0; i <= intHeight - 1; i++)
            {
                for (int j = 0; j <= intWidth - 1; j++)
                {
                    dblGray = 0.299 * bmpInput.GetPixel(i, j).B + 0.587 * bmpInput.GetPixel(i, j).G + 0.144 * bmpInput.GetPixel(i, j).R;
                    if (dblGray > 255) dblGray = 255;
                    if (dblGray < 0) dblGray = 0;
                    bytGray = (byte)dblGray;
                    bytResult[i, j] = bytGray;
                }
            }
            return bytResult;
        }

        public byte[,] AverageFilter(WriteableBitmap bmpInput)
        {
            int intHeight = bmpInput.PixelHeight; int intWidth = bmpInput.PixelWidth;
            byte[,] bytData = new byte[intHeight, intWidth];
            bytData = GetMatrixData(bmpInput);

            int[,] intMask1 = new int[3, 3] { { 1, 2, 1 }, { 2, 4, 2 }, { 1, 2, 1 } };
            double intValue;
            for (int i = 0; i <= intHeight - 1; i++)
            {
                for (int j = 0; j <= intWidth - 1; j++)
                {
                    if ((i > 0) && (i < intHeight - 1) && (j > 0) && (j < intWidth - 1))
                    {
                        intValue = ((PointCalc(bytData, intMask1, i, j)) / 16);
                        if (intValue > 255) intValue = 255;
                        if (intValue < 0) intValue = 0;
                        bytData[i, j] = (byte)intValue;
                    }
                }
            }
            return bytData;
        }

        #region Basic Global Thresholding Algorithm

        public static double FindT(double[,] arrImage)
        {
            int w = arrImage.GetLength(1);
            int h = arrImage.GetLength(0);
            double sum = 0.0;
            for (int i = 0; i < h; i++)
                for (int j = 0; j < w; j++)
                {
                    sum += arrImage[i, j];
                }
            return (double)sum / (w * h);
        }

        public static int[,] GroupingPixel(double[,] arrImage, double T)
        {
            int w = arrImage.GetLength(1);
            int h = arrImage.GetLength(0);
            int[,] grouped = new int[h, w];
            for (int i = 0; i < h; i++)
                for (int j = 0; j < w; j++)
                {
                    if (arrImage[i, j] > T)
                    {
                        grouped[i, j] = 1;
                    }
                    else
                    {
                        grouped[i, j] = 2;
                    }
                }
            return grouped;
        }

        public static double FindULevel(double[,] arrImage, int[,] groupImage, int level)
        {
            int w = arrImage.GetLength(1);
            int h = arrImage.GetLength(0);
            int count = 0;
            double sum = 0.0;
            for (int i = 0; i < h; i++)
                for (int j = 0; j < w; j++)
                    if (groupImage[i, j] == level)
                    {
                        sum += arrImage[i, j];
                        count++;
                    }
            return (double)sum / count;
        }
        #endregion
    }
}