using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace DIPHW37
{
    public class ImageClass
    {
        public static int[,] maskSobelDx = new int[3, 3] { { 1, 0, -1 }, { 2, 0, -2 }, { 1, 0, -1 } };
        public static int[,] maskSobelDy = new int[3, 3] { { 1, 2, 1 }, { 0, 0, 0 }, { -1, -2, -1 } };

        public static int[,] maskPrewittDx = new int[3, 3] { { 1, 0, -1 }, { 1, 0, -1 }, { 1, 0, -1 } };
        public static int[,] maskPrewittDy = new int[3, 3] { { -1, -1, -1 }, { 0, 0, 0 }, { 1, 1, 1 } };

        public static Bitmap ConvertDoubleArrToBitmap(double[,] arr)
        {
            Bitmap bmpOutput = new Bitmap(arr.GetLength(1), arr.GetLength(0));
            int intHeight = arr.GetLength(0);
            int intWidth = arr.GetLength(1);
            byte[,] bytData = new byte[intHeight, intWidth];

            Rectangle rtg = new Rectangle(0, 0, bmpOutput.Width, bmpOutput.Height);
            BitmapData datOutput = bmpOutput.LockBits(rtg, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            IntPtr ptrScan0 = datOutput.Scan0;
            int intStride = datOutput.Stride;
            int intSpace = intStride - intWidth * 3;
            unsafe
            {
                byte* ptr = (byte*)ptrScan0;
                for (int y = 0; y < intHeight; y++)
                {
                    for (int x = 0; x < intWidth; x++)
                    {
                        if (arr[y, x] < 0)
                        {
                            ptr[0] = ptr[1] = ptr[2] = (byte)0;
                        }
                        else if (arr[y, x] > 255)
                        {
                            ptr[0] = ptr[1] = ptr[2] = (byte)255;
                        }
                        else
                            ptr[0] = ptr[1] = ptr[2] = (byte)arr[y, x];
                        ptr += 3;
                    }
                    ptr += intSpace;
                }
            }

            bmpOutput.UnlockBits(datOutput);
            return bmpOutput;
        }



        public static double[,] ConvertBitmapToDoubleArr(Bitmap bmp)
        {
            int intHeight = bmp.Height;
            int intWidth = bmp.Width;
            double[,] bytData = new double[intHeight, intWidth];

            Rectangle rtg = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData datOutput = bmp.LockBits(rtg, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            IntPtr ptrScan0 = datOutput.Scan0;
            int intStride = datOutput.Stride;
            int intSpace = intStride - intWidth * 3;
            unsafe
            {
                byte* ptr = (byte*)ptrScan0;
                for (int y = 0; y < intHeight; y++)
                {
                    for (int x = 0; x < intWidth; x++)
                    {
                        bytData[y, x] = (double)(ptr[0] + ptr[18] + ptr[2]) / 3;
                        ptr += 3;
                    }
                    ptr += intSpace;
                }
            }
            bmp.UnlockBits(datOutput);
            return bytData;
        }

        public static void Gray(Bitmap bmpInput)
        {
            // Lock the bitmap's bits.  
            Rectangle rtg = new Rectangle(0, 0, bmpInput.Width, bmpInput.Height);
            BitmapData bmpData = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, bmpInput.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            // This code is specific to a bitmap with 24 bits per pixels.
            int bytes = bmpInput.Width * bmpInput.Height * 3;
            //int bytes = Math.Abs(bmpData.Stride) * bmpInput.Height;
            byte[] rgbValues = new byte[bytes];

            try
            {
                // Copy the RGB values into the array.
                System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            // Gray transform 
            int GrayLevel;
            for (int counter = 0; counter < rgbValues.Length; counter += 3)
            {
                GrayLevel = (rgbValues[counter + 0] + rgbValues[counter + 1] + rgbValues[counter + 2]) / 3;
                rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = (byte)GrayLevel;
            }

            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            // Unlock the bits.
            bmpInput.UnlockBits(bmpData);
        }

        public static double[,] ConvolutionFilter(Bitmap bmpInput, int[,] maskByte, int bias)
        {
            double[,] arrRes = new double[bmpInput.Height, bmpInput.Width];
            int intHeight = bmpInput.Height;
            int intWidth = bmpInput.Width;
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
            double[,] arrRes = new double[bmpInput.GetLength(0), bmpInput.GetLength(0)];
            int intHeight = bmpInput.GetLength(0);
            int intWidth = bmpInput.GetLength(0);
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

        public static Bitmap ConvolutionFilter(Bitmap bmpInput, int[,] maskByte, int bias, double[,] arrRes)
        {
            Bitmap bmpOutput = new Bitmap(bmpInput.Width, bmpInput.Height);
            int intHeight = bmpInput.Height;
            int intWidth = bmpInput.Width;
            byte[,] bytData = new byte[intHeight, intWidth];
            bytData = ImageClass.GetMatrixData(bmpInput);

            Rectangle rtg = new Rectangle(0, 0, bmpOutput.Width, bmpOutput.Height);
            BitmapData datOutput = bmpOutput.LockBits(rtg, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            IntPtr ptrScan0 = datOutput.Scan0;
            int intStride = datOutput.Stride;
            int intSpace = intStride - intWidth * 3;
            unsafe
            {
                byte* ptr = (byte*)ptrScan0;
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
                        arrRes[y, x] = (sum / (double)bias);
                        int flag = (sum / bias);
                        if (flag < 0)
                        {
                            flag = 0;
                        }
                        else if (flag > 255)
                        {
                            flag = 255;
                        }
                        ptr[0] = ptr[1] = ptr[2] = (byte)flag;
                        ptr += 3;
                    }
                    ptr += intSpace;
                }
            }

            bmpOutput.UnlockBits(datOutput);
            return bmpOutput;
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

        public static void MakeGrayscale2Bitmap(Bitmap original)
        {
            for (int i = 0; i < original.Height; i++)
                for (int j = 0; j < original.Width; j++)
                {
                    byte x = (byte)((original.GetPixel(j, i).R + original.GetPixel(j, i).G + original.GetPixel(j, i).B) / 3);
                    original.SetPixel(j, i, Color.FromArgb(255, x, x, x));
                }
        }

        public static double[,] MakeGrayscale2Double(Bitmap original)
        {
            double[,] arr = new double[original.Height, original.Width];
            int intHeight = original.Height;
            int intWidth = original.Width;

            Rectangle rtg = new Rectangle(0, 0, original.Width, original.Height);
            BitmapData datOutput = original.LockBits(rtg, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            IntPtr ptrScan0 = datOutput.Scan0;
            int intStride = datOutput.Stride;
            int intSpace = intStride - intWidth * 3;
            unsafe
            {
                byte* ptr = (byte*)ptrScan0;
                for (int y = 0; y < intHeight; y++)
                {
                    for (int x = 0; x < intWidth; x++)
                    {
                        arr[y, x] = (ptr[0] + ptr[1] + ptr[2]) / 3.0;
                        ptr += 3;
                    }
                    ptr += intSpace;
                }
            }
            original.UnlockBits(datOutput);
            return arr;
        }

        public static Bitmap MakeGrayscale3(Bitmap original)
        {
            try
            {
                //create a blank bitmap the same size as original
                Bitmap newBitmap = new Bitmap(original.Width, original.Height);

                //get a graphics object from the new image
                Graphics g = Graphics.FromImage(newBitmap);

                //create the grayscale ColorMatrix
                ColorMatrix colorMatrix = new ColorMatrix(
                   new float[][]
                   {
         new float[] {.3f, .3f, .3f, 0, 0},
         new float[] {.59f, .59f, .59f, 0, 0},
         new float[] {.11f, .11f, .11f, 0, 0},
         new float[] {0, 0, 0, 1, 0},
         new float[] {0, 0, 0, 0, 1}
                   });

                //create some image attributes
                ImageAttributes attributes = new ImageAttributes();

                //set the color matrix attribute
                attributes.SetColorMatrix(colorMatrix);

                //draw the original image on the new image
                //using the grayscale color matrix
                g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
                   0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);

                //dispose the Graphics object
                g.Dispose();
                return newBitmap;
            }
            catch
            {
                return null;
            }
        }

        public static void NegativeT(Bitmap bmpInput)
        {
            // Lock the bitmap's bits.  
            Rectangle rtg = new Rectangle(0, 0, bmpInput.Width, bmpInput.Height);
            BitmapData bmpData = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, bmpInput.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            // This code is specific to a bitmap with 24 bits per pixels.

            int bytes = bmpInput.Width * bmpInput.Height * 3;
            byte[] rgbValues = new byte[bytes];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            for (int counter = 0; counter < rgbValues.Length; counter += 3)

            {
                //counter = (x * bmpInput.Width + y) * 3;

                rgbValues[counter + 0] = (byte)(255 - rgbValues[counter + 0]);
                rgbValues[counter + 1] = (byte)(255 - rgbValues[counter + 1]);
                rgbValues[counter + 2] = (byte)(255 - rgbValues[counter + 2]);
            }

            //MessageBox.Show(counter.ToString());

            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            // Unlock the bits.
            bmpInput.UnlockBits(bmpData);
        }
        public static void NegativeT2(Bitmap bmpInput)
        {


            // Lock the bitmap's bits.  
            Rectangle rtg = new Rectangle(0, 0, bmpInput.Width, bmpInput.Height);
            BitmapData bmpData = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, bmpInput.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            // This code is specific to a bitmap with 24 bits per pixels.
            int bytes = bmpInput.Width * bmpInput.Height * 3;
            byte[] rgbValues = new byte[bytes];
            int x = 0;
            int y = 0;
            int counter = 0;

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            for (x = 0; x < bmpInput.Height; x++)
            {
                for (y = 0; y < bmpInput.Width; y++)
                {
                    counter = (x * bmpInput.Width + y) * 3;

                    rgbValues[counter + 0] = (byte)(255 - rgbValues[counter + 0]);
                    rgbValues[counter + 1] = (byte)(255 - rgbValues[counter + 1]);
                    rgbValues[counter + 2] = (byte)(255 - rgbValues[counter + 2]);
                }
            }

            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            // Unlock the bits.
            bmpInput.UnlockBits(bmpData);
        }
        public static void LogT(Bitmap bmpInput)
        {
            // Lock the bitmap's bits.  
            Rectangle rtg = new Rectangle(0, 0, bmpInput.Width, bmpInput.Height);
            BitmapData bmpData = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, bmpInput.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;
            // Declare an array to hold the bytes of the bitmap.
            // This code is specific to a bitmap with 24 bits per pixels.
            int bytes = bmpInput.Width * bmpInput.Height * 3;
            byte[] rgbValues = new byte[bytes];
            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);
            double GrayLevel;
            double c = 2.0;
            for (int counter = 0; counter < rgbValues.Length; counter += 3)
            {
                GrayLevel = (byte)((rgbValues[counter] + rgbValues[counter + 1] + rgbValues[counter + 2]) / 3);
                GrayLevel = (byte)(c * 255 * Math.Log((double)((double)GrayLevel / 255 + 1), 2));
                rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = (byte)GrayLevel;
            }
            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);
            // Unlock the bits.
            bmpInput.UnlockBits(bmpData);
        }
        public static void PowerT(Bitmap bmpInput, double mu)
        {
            // Lock the bitmap's bits.  
            Rectangle rtg = new Rectangle(0, 0, bmpInput.Width, bmpInput.Height);
            BitmapData bmpData = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, bmpInput.PixelFormat);
            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;
            // Declare an array to hold the bytes of the bitmap.
            // This code is specific to a bitmap with 24 bits per pixels.
            int bytes = bmpInput.Width * bmpInput.Height * 3;
            byte[] rgbValues = new byte[bytes];
            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);
            double x;
            byte GrayLevel;
            for (int counter = 0; counter < rgbValues.Length; counter += 3)
            {
                x = Convert.ToDouble(rgbValues[counter]) / 255;
                GrayLevel = Convert.ToByte(255 * Math.Pow(x, mu));
                rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = GrayLevel;
            }
            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);
            // Unlock the bits.
            bmpInput.UnlockBits(bmpData);
        }
        public static void PiecewiseLinearT(Bitmap bmpInput)
        {
            byte r1;
            byte r2;

            // Lock the bitmap's bits.  
            Rectangle rtg = new Rectangle(0, 0, bmpInput.Width, bmpInput.Height);
            BitmapData bmpData = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, bmpInput.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            // This code is specific to a bitmap with 24 bits per pixels.
            int bytes = bmpInput.Width * bmpInput.Height * 3;
            byte[] rgbValues = new byte[bytes];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            r1 = 255;
            r2 = 0;

            for (int counter = 0; counter < rgbValues.Length; counter += 3)
            {
                if (r2 < rgbValues[counter]) r2 = rgbValues[counter];
                if (r1 > rgbValues[counter]) r1 = rgbValues[counter];
            }

            double GrayLevel;

            for (int counter = 0; counter < rgbValues.Length; counter += 3)
            {
                if (rgbValues[counter] <= r1)
                    rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = 0;
                else
                    if (rgbValues[counter] >= r2)
                    rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = 255;
                else
                {
                    // GrayLevel = 255 * ((rgbValues[counter] - r1) / (r2 - r1)); -> Lỗi nghiêm trọng
                    GrayLevel = (255 * (rgbValues[counter] - r1)) / (r2 - r1);
                    rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = (byte)GrayLevel;
                }
            }

            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            // Unlock the bits.
            bmpInput.UnlockBits(bmpData);
        }
        public static void BitsPlaneExt(Bitmap bmpInput, byte np)
        {
            // Lock the bitmap's bits.  
            Rectangle rtg = new Rectangle(0, 0, bmpInput.Width, bmpInput.Height);
            BitmapData bmpData = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, bmpInput.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            // This code is specific to a bitmap with 24 bits per pixels.
            int bytes = bmpInput.Width * bmpInput.Height * 3;
            byte[] rgbValues = new byte[bytes];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            int GrayLevel;
            byte matna = 0;
            switch (np)
            {
                case 7: { matna = 128; } break;
                case 6: { matna = 64; } break;
                case 5: { matna = 32; } break;
                case 4: { matna = 16; } break;
                case 3: { matna = 8; } break;
                case 2: { matna = 4; } break;
                case 1: { matna = 2; } break;
                case 0: { matna = 1; } break;
            }
            for (int counter = 0; counter < rgbValues.Length; counter += 3)
            {
                GrayLevel = rgbValues[counter] & matna;
                if (GrayLevel > 0)
                {
                    rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = 255;
                }
                else
                {
                    rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = 0;
                }
            }

            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            // Unlock the bits.
            bmpInput.UnlockBits(bmpData);
        }


        public static void HistogramEqu(Bitmap bmpInput)
        {   // Lock the bitmap's bits.
            Rectangle rtg = new Rectangle(0, 0, bmpInput.Width, bmpInput.Height);
            BitmapData bmpData = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, bmpInput.PixelFormat);
            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;
            // Declare an array to hold the bytes of the bitmap.
            // This code is specific to a bitmap with 24 bits per pixels.
            int bytes = bmpInput.Width * bmpInput.Height * 3;
            byte[] rgbValues = new byte[bytes];
            long[] His = new long[256];
            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            for (int i = 0; i < 256; i++) His[i] = 0; // Đặt các histogram về không

            for (int counter = 0; counter < rgbValues.Length; counter += 3)
                His[rgbValues[counter]] = His[rgbValues[counter]] + 1; // Tăng số biến đếm của mỗi histogram
            for (int i = 1; i < 256; i++) His[i] = His[i] + His[i - 1];
            long tongHis = His[255];
            for (int i = 0; i < 256; i++) His[i] = (255 * His[i]) / (tongHis);
            for (int counter = 0; counter < rgbValues.Length; counter += 3)
            {
                rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = (byte)His[rgbValues[counter]];
            }
            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);
            bmpInput.UnlockBits(bmpData);
        }

        public static void HistogramMatching(Bitmap bmpInput)
        {
            // Lock the bitmap's bits.  
            Rectangle rtg = new Rectangle(0, 0, bmpInput.Width, bmpInput.Height);
            BitmapData bmpData = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, bmpInput.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            // This code is specific to a bitmap with 24 bits per pixels.
            int bytes = bmpInput.Width * bmpInput.Height * 3;
            byte[] rgbValues = new byte[bytes];
            long[] His = new long[256];
            long[] SfHis = new long[256];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            // Đặt các histogram về không
            for (int i = 0; i < 256; i++)
            {
                His[i] = 0;
                //SfHis[i] = 255-i; //->Hay
                SfHis[i] = i;   // Cũng hay
                //SfHis[i] = (bmpInput.Width * bmpInput.Height) / 255; // Không biến đổi
                //SfHis[i] = 100; // Không biến đổi
            }


            // Tăng số biến đếm của mỗi histogram
            for (int counter = 0; counter < rgbValues.Length; counter += 3) His[rgbValues[counter]]++;
            // công dồn

            for (int i = 1; i < 256; i++)
            {
                His[i] = His[i] + His[i - 1];
                SfHis[i] = SfHis[i] + SfHis[i - 1];
            }

            long tongHis = His[255];
            long tongSfHis = SfHis[255];

            for (int i = 0; i < 256; i++)
            {
                His[i] = (255 * His[i]) / (tongHis);
                SfHis[i] = (255 * SfHis[i]) / (tongSfHis);
            }

            long hValue;
            byte sIndex = 0;

            for (int counter = 0; counter < rgbValues.Length; counter += 3)
            {

                hValue = His[rgbValues[counter]];

                for (int j = 0; j < 256; j++)
                {
                    if (hValue < SfHis[j])
                    {
                        if (j - 1 >= 0)
                            sIndex = (byte)(j - 1); // Nên tính trước giá trị này
                        else sIndex = 0;
                        break;
                    }
                }
                rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = sIndex;
            }

            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            // Unlock the bits.
            bmpInput.UnlockBits(bmpData);
        }
        public static void HistogramStatistic(Bitmap bmpInput)
        {
            // Lock the bitmap's bits.  
            Rectangle rtg = new Rectangle(0, 0, bmpInput.Width, bmpInput.Height);
            BitmapData bmpData = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, bmpInput.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            // This code is specific to a bitmap with 24 bits per pixels.
            int bytes = bmpInput.Width * bmpInput.Height * 3;

            byte[] rgbValues = new byte[bytes];
            double[] p = new double[256];
            int n = bmpInput.Width * bmpInput.Height;
            double Dg = 0;
            double Mg = 0;

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            // Đặt các histogram về không
            for (int i = 0; i < 256; i++) { p[i] = 0; }

            // Tính nk =pk
            for (int counter = 0; counter < rgbValues.Length; counter += 3) p[rgbValues[counter]]++;

            // Tính pk
            for (int i = 0; i < 256; i++) { p[i] = p[i] / n; }

            // Tính Mg
            Mg = 0;
            for (int i = 0; i < 256; i++) { Mg = Mg + i * p[i]; } //            MessageBox.Show(Mg.ToString());
            // Tính Dg
            Dg = 0;
            for (int i = 0; i < 256; i++) { Dg = Dg + (i - Mg) * (i - Mg) * p[i]; }//   MessageBox.Show(Dg.ToString());

            for (int counter = 0; counter < rgbValues.Length; counter += 3)
            {
                rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = 0; // (byte)hValue;
            }

            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            // Unlock the bits.
            bmpInput.UnlockBits(bmpData);
        }
        public static void LogicOperators(Bitmap bmpInput, Bitmap orMark, Bitmap AndMark)
        {
            // Lock the bitmap's bits.  
            Rectangle rtg = new Rectangle(0, 0, bmpInput.Width, bmpInput.Height);
            BitmapData bmpData = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, bmpInput.PixelFormat);
            BitmapData bmpDataOr = orMark.LockBits(rtg, ImageLockMode.ReadOnly, orMark.PixelFormat);
            BitmapData bmpDataAnd = AndMark.LockBits(rtg, ImageLockMode.ReadOnly, AndMark.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;
            IntPtr ptrOr = bmpDataOr.Scan0;
            IntPtr ptrAnd = bmpDataAnd.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            // This code is specific to a bitmap with 24 bits per pixels.
            int bytes = bmpInput.Width * bmpInput.Height * 3;

            byte[] rgbValues = new byte[bytes];
            byte[] Or_Values = new byte[bytes];
            byte[] AndValues = new byte[bytes];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);
            System.Runtime.InteropServices.Marshal.Copy(ptrOr, Or_Values, 0, bytes);
            System.Runtime.InteropServices.Marshal.Copy(ptrAnd, AndValues, 0, bytes);

            int hValue = 0;
            for (int counter = 0; counter < rgbValues.Length; counter += 3)
            {
                hValue = rgbValues[counter] & AndValues[counter];
                hValue = hValue | Or_Values[counter];
                rgbValues[counter] = (byte)hValue;

                //rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = (byte)hValue;

                hValue = rgbValues[counter + 1] & AndValues[counter + 1];
                hValue = hValue | Or_Values[counter + 1];
                rgbValues[counter + 1] = (byte)hValue;

                hValue = rgbValues[counter + 2] & AndValues[counter + 2];
                hValue = hValue | Or_Values[counter + 2];
                rgbValues[counter + 2] = (byte)hValue;
            }

            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            // Unlock the bits.
            bmpInput.UnlockBits(bmpData);
            orMark.UnlockBits(bmpDataOr);
            AndMark.UnlockBits(bmpDataAnd);
        }
        public static void Subtraction(Bitmap bmpInput, Bitmap bmpSub)
        {
            // Lock the bitmap's bits.  
            Rectangle rtg = new Rectangle(0, 0, bmpInput.Width, bmpInput.Height);
            BitmapData bmpData = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, bmpInput.PixelFormat);
            BitmapData bmpDataSub = bmpSub.LockBits(rtg, ImageLockMode.ReadOnly, bmpSub.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;
            IntPtr ptrSub = bmpDataSub.Scan0;


            // Declare an array to hold the bytes of the bitmap.
            // This code is specific to a bitmap with 24 bits per pixels.
            int bytes = bmpInput.Width * bmpInput.Height * 3;

            byte[] rgbValues = new byte[bytes];
            byte[] SubValues = new byte[bytes];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);
            System.Runtime.InteropServices.Marshal.Copy(ptrSub, SubValues, 0, bytes);

            double hValue = 0;
            for (int counter = 0; counter < rgbValues.Length; counter += 3)
            {
                hValue = (rgbValues[counter] - SubValues[counter] + 255) / 2;
                rgbValues[counter] = (byte)hValue;


                //rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = (byte)hValue;

                hValue = (rgbValues[counter + 1] - SubValues[counter + 1] + 255) / 2;
                rgbValues[counter + 1] = (byte)hValue;

                hValue = (rgbValues[counter + 2] - SubValues[counter + 2] + 255) / 2;
                rgbValues[counter + 2] = (byte)hValue;
            }

            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            // Unlock the bits.
            bmpInput.UnlockBits(bmpData);
            bmpSub.UnlockBits(bmpDataSub);
        }
        public static void Average(Bitmap bmpInput, Bitmap bmpDoituong, byte k)
        {
            // Lock the bitmap's bits.  
            Rectangle rtg = new Rectangle(0, 0, bmpInput.Width, bmpInput.Height);
            BitmapData bmpData = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, bmpInput.PixelFormat);
            BitmapData bmpDatadt = bmpDoituong.LockBits(rtg, ImageLockMode.ReadWrite, bmpDoituong.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;
            IntPtr ptrdt = bmpDatadt.Scan0;


            // Declare an array to hold the bytes of the bitmap.
            // This code is specific to a bitmap with 24 bits per pixels.
            int bytes = bmpInput.Width * bmpInput.Height * 3;

            byte[] rgbValues = new byte[bytes];
            byte[] dtgValues = new byte[bytes];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);
            System.Runtime.InteropServices.Marshal.Copy(ptrdt, dtgValues, 0, bytes);

            double hValue = 0;
            for (int counter = 0; counter < rgbValues.Length; counter += 3)
            {
                hValue = rgbValues[counter] + dtgValues[counter];
                for (int i = 0; i < k - 1; i++) hValue += rgbValues[counter];
                hValue = hValue / k;
                rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = (byte)hValue;
            }

            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            // Unlock the bits.
            bmpInput.UnlockBits(bmpData);

            bmpDoituong.UnlockBits(bmpData);
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

        public void FourierTrans(Bitmap bmpInput)
        {

            int m_width = bmpInput.Width;
            int m_height = bmpInput.Height;

            // lock source bitmap data
            data = new Complex[m_height, m_width];
            BitmapData srcData = bmpInput.LockBits(new Rectangle(0, 0, m_width, m_height), ImageLockMode.ReadWrite, bmpInput.PixelFormat);

            #region Chuyển thành số phức


            int offset = srcData.Stride - ((bmpInput.PixelFormat == PixelFormat.Format8bppIndexed) ? m_width : m_width * 3);

            // do the job
            unsafe
            {
                byte* src = (byte*)srcData.Scan0.ToPointer();

                if (bmpInput.PixelFormat == PixelFormat.Format8bppIndexed)
                {
                    // for each line
                    for (int y = 0; y < m_height; y++)
                    {
                        // for each pixel
                        for (int x = 0; x < m_width; x++, src++)
                        {
                            data[y, x].Re = (float)*src / 255;
                        }
                        src += offset;
                    }
                }
                else
                {
                    // RGB image

                    // for each line
                    for (int y = 0; y < m_height; y++)
                    {
                        // for each pixel
                        for (int x = 0; x < m_width; x++, src += 3)
                        {
                            data[y, x].Re = (float)(0.2125f * src[2] + 0.7154f * src[1] + 0.0721f * src[0]) / 255;
                        }
                        src += offset;
                    }
                }
            }
            #endregion

            #region Biến đổi fourier thuận
            for (int y = 0; y < m_height; y++)
            {
                for (int x = 0; x < m_width; x++)
                {
                    if (((x + y) & 0x1) != 0)
                    {
                        data[y, x].Re *= -1;
                        data[y, x].Im *= -1;
                    }
                }
            }

            #region Biến đôi f thuận nhanh

            int k = data.GetLength(0);
            int n = data.GetLength(1);

            #region Process rows
            Complex[] row = new Complex[n];

            for (int i = 0; i < k; i++)
            {
                // copy row
                for (int j = 0; j < n; j++) row[j] = data[i, j];
                // transform it

                #region Biến đổi fourier trên 1 dòng
                int nn = row.Length;
                int m = Log2(nn);

                // reorder data first
                ReorderData(row);

                // compute FFT
                int tn = 1, tm;

                for (int kk = 1; kk <= m; kk++)                   //
                {
                    Complex[] rotation = GetComplexRotation(kk, FourierDirection.Forward);

                    tm = tn;
                    tn <<= 1;

                    for (int ii = 0; ii < tm; ii++)
                    {
                        Complex t = rotation[ii];

                        for (int even = ii; even < nn; even += tn)
                        {
                            int odd = even + tm;
                            Complex ce = row[even];
                            Complex co = row[odd];

                            float tr = co.Re * t.Re - co.Im * t.Im;
                            float ti = co.Re * t.Im + co.Im * t.Re;

                            row[even].Re += tr;
                            row[even].Im += ti;

                            row[odd].Re = ce.Re - tr;
                            row[odd].Im = ce.Im - ti;
                        }
                    }
                }
                for (int ii = 0; ii < nn; ii++)
                {
                    row[ii].Re /= (float)nn;
                    row[ii].Im /= (float)nn;
                }
                #endregion

                // copy back
                for (int j = 0; j < n; j++) data[i, j] = row[j];
            }
            #endregion

            #region Process columns
            Complex[] col = new Complex[k];

            for (int j = 0; j < n; j++)
            {
                // copy column
                for (int i = 0; i < k; i++) col[i] = data[i, j];
                // transform it

                #region Biến đổi fourier trên 1 cột
                int nn = col.Length;
                int m = Log2(nn);

                // reorder data first
                ReorderData(col);

                // compute FFT
                int tn = 1, tm;

                for (int kk = 1; kk <= m; kk++)
                {
                    Complex[] rotation = GetComplexRotation(kk, FourierDirection.Forward);

                    tm = tn;
                    tn <<= 1;

                    for (int ii = 0; ii < tm; ii++)
                    {
                        Complex t = rotation[ii];

                        for (int even = ii; even < nn; even += tn)
                        {
                            int odd = even + tm;
                            Complex ce = col[even];
                            Complex co = col[odd];

                            float tr = co.Re * t.Re - co.Im * t.Im;
                            float ti = co.Re * t.Im + co.Im * t.Re;

                            col[even].Re += tr;
                            col[even].Im += ti;

                            col[odd].Re = ce.Re - tr;
                            col[odd].Im = ce.Im - ti;
                        }
                    }
                }
                for (int ii = 0; ii < nn; ii++)
                {
                    col[ii].Re /= (float)nn;
                    col[ii].Im /= (float)nn;
                }
                #endregion

                // copy back
                for (int i = 0; i < k; i++)
                    data[i, j] = col[i];
            }
            #endregion

            #endregion

            #endregion

            #region Hiển thị dạng ảnh của biến đổi Fourier
            float scale = (float)Math.Sqrt(m_width * m_height);
            unsafe
            {
                IntPtr ptrScan0 = srcData.Scan0;
                byte* ptr = (byte*)ptrScan0;
                byte bValue;
                int intStride = srcData.Stride;
                int intSpace = intStride - m_width * 3;

                for (int i = 0; i <= m_height - 1; i++)
                {
                    for (int j = 0; j <= m_width - 1; j++)
                    {
                        if ((i > 0) && (i < m_height - 1) && (j > 0) && (j < m_width - 1))
                        {
                            bValue = (byte)System.Math.Max(0, System.Math.Min(255, data[i, j].Magnitude * scale * 255));
                            ptr[0] = ptr[1] = ptr[2] = bValue;
                        }
                        ptr += 3;
                    }
                    ptr += intSpace;
                }
            }
            #endregion

            bmpInput.UnlockBits(srcData);
        }
        public void FourierTrans(Bitmap bmpInput, FourierDirection fdirection)
        {
            int m_width = bmpInput.Width;
            int m_height = bmpInput.Height;

            // lock source bitmap data
            BitmapData srcData = bmpInput.LockBits(new Rectangle(0, 0, m_width, m_height), ImageLockMode.ReadWrite, bmpInput.PixelFormat);

            #region Chuyển thành số phức
            //Complex[,] data = new Complex[m_height, m_width];

            int offset = srcData.Stride - ((bmpInput.PixelFormat == PixelFormat.Format8bppIndexed) ? m_width : m_width * 3);

            //#region Loc
            //if (fdirection == FourierDirection.Backward)
            //{
            //    for (int i = 0; i <= m_height - 1; i++)
            //    {
            //        for (int j = 0; j <= m_width - 1; j++)
            //        {
            //            if ((i > 0) && (i < m_height - 1) && (j > 0) && (j < m_width - 1))
            //            {
            //                if (Math.Sqrt((i - m_height / 2) * (i - m_height / 2) + (j - m_width / 2) * (j - m_width / 2)) < 30)
            //                {
            //                    data[i, j].Re = 0;
            //                    data[i, j].Im = 0;
            //                }
            //            }
            //        }
            //    }
            //}
            //#endregion

            // do the job
            if (fdirection == FourierDirection.Forward)
            {
                data = new Complex[m_height, m_width];
                unsafe
                {
                    byte* src = (byte*)srcData.Scan0.ToPointer();

                    if (bmpInput.PixelFormat == PixelFormat.Format8bppIndexed)
                    {
                        // for each line
                        for (int y = 0; y < m_height; y++)
                        {
                            // for each pixel
                            for (int x = 0; x < m_width; x++, src++)
                            {
                                data[y, x].Re = (float)*src / 255;
                            }
                            src += offset;
                        }
                    }
                    else
                    {
                        // RGB image

                        // for each line
                        for (int y = 0; y < m_height; y++)
                        {
                            // for each pixel
                            for (int x = 0; x < m_width; x++, src += 3)
                            {
                                data[y, x].Re = (float)(0.2125f * src[2] + 0.7154f * src[1] + 0.0721f * src[0]) / 255;
                            }
                            src += offset;
                        }
                    }
                }
            }
            #endregion

            #region Biến đổi fourier thuận
            if (fdirection == FourierDirection.Forward)
            {
                for (int y = 0; y < m_height; y++)
                {
                    for (int x = 0; x < m_width; x++)
                    {
                        if (((x + y) & 0x1) != 0)
                        {
                            data[y, x].Re *= -1;
                            data[y, x].Im *= -1;
                        }
                    }
                }
            }
            #region Biến đôi f thuận nhanh

            int k = data.GetLength(0);
            int n = data.GetLength(1);

            #region Process rows
            Complex[] row = new Complex[n];

            for (int i = 0; i < k; i++)
            {
                // copy row
                for (int j = 0; j < n; j++) row[j] = data[i, j];
                // transform it

                #region Biến đổi fourier trên 1 dòng
                int nn = row.Length;
                int m = Log2(nn);

                // reorder data first
                ReorderData(row);

                // compute FFT
                int tn = 1, tm;

                for (int kk = 1; kk <= m; kk++)                   //
                {
                    Complex[] rotation = GetComplexRotation(kk, fdirection);

                    tm = tn;
                    tn <<= 1;

                    for (int ii = 0; ii < tm; ii++)
                    {
                        Complex t = rotation[ii];

                        for (int even = ii; even < nn; even += tn)
                        {
                            int odd = even + tm;
                            Complex ce = row[even];
                            Complex co = row[odd];

                            float tr = co.Re * t.Re - co.Im * t.Im;
                            float ti = co.Re * t.Im + co.Im * t.Re;

                            row[even].Re += tr;
                            row[even].Im += ti;

                            row[odd].Re = ce.Re - tr;
                            row[odd].Im = ce.Im - ti;
                        }
                    }
                }
                if (fdirection == FourierDirection.Forward)
                {
                    for (int ii = 0; ii < nn; ii++)
                    {
                        row[ii].Re /= (float)nn;
                        row[ii].Im /= (float)nn;
                    }
                }
                #endregion

                // copy back
                for (int j = 0; j < n; j++) data[i, j] = row[j];
            }
            #endregion

            #region Process columns
            Complex[] col = new Complex[k];

            for (int j = 0; j < n; j++)
            {
                // copy column
                for (int i = 0; i < k; i++) col[i] = data[i, j];
                // transform it

                #region Biến đổi fourier trên 1 cột
                int nn = col.Length;
                int m = Log2(nn);

                // reorder data first
                ReorderData(col);

                // compute FFT
                int tn = 1, tm;

                for (int kk = 1; kk <= m; kk++)
                {
                    Complex[] rotation = GetComplexRotation(kk, fdirection);

                    tm = tn;
                    tn <<= 1;

                    for (int ii = 0; ii < tm; ii++)
                    {
                        Complex t = rotation[ii];

                        for (int even = ii; even < nn; even += tn)
                        {
                            int odd = even + tm;
                            Complex ce = col[even];
                            Complex co = col[odd];

                            float tr = co.Re * t.Re - co.Im * t.Im;
                            float ti = co.Re * t.Im + co.Im * t.Re;

                            col[even].Re += tr;
                            col[even].Im += ti;

                            col[odd].Re = ce.Re - tr;
                            col[odd].Im = ce.Im - ti;
                        }
                    }
                }
                if (fdirection == FourierDirection.Forward)
                {
                    for (int ii = 0; ii < nn; ii++)
                    {
                        col[ii].Re /= (float)nn;
                        col[ii].Im /= (float)nn;
                    }
                }
                #endregion

                // copy back 
                for (int i = 0; i < k; i++) data[i, j] = col[i];
            }
            #endregion

            #endregion

            if (fdirection == FourierDirection.Backward)
            {
                for (int y = 0; y < m_height; y++)
                {
                    for (int x = 0; x < m_width; x++)
                    {
                        if (((x + y) & 0x1) != 0)
                        {
                            data[y, x].Re *= -1;
                            data[y, x].Im *= -1;
                        }
                    }
                }
            }
            #endregion

            #region Hiển thị dạng ảnh của biến đổi Fourier
            float scale = 1;
            if (fdirection == FourierDirection.Forward) scale = (float)Math.Sqrt(m_width * m_height);
            unsafe
            {
                IntPtr ptrScan0 = srcData.Scan0;
                byte* ptr = (byte*)ptrScan0;
                byte bValue;
                int intStride = srcData.Stride;
                int intSpace = intStride - m_width * 3;

                for (int i = 0; i <= m_height - 1; i++)
                {
                    for (int j = 0; j <= m_width - 1; j++)
                    {
                        if ((i > 0) && (i < m_height - 1) && (j > 0) && (j < m_width - 1))
                        {
                            bValue = (byte)System.Math.Max(0, System.Math.Min(255, data[i, j].Magnitude * scale * 255));
                            ptr[0] = ptr[1] = ptr[2] = bValue;
                        }
                        ptr += 3;
                    }
                    ptr += intSpace;
                }
            }
            #endregion
            bmpInput.UnlockBits(srcData);
        }


        public void Gradient(Bitmap bmpInput)
        {
            int intHeight = bmpInput.Height;
            int intWidth = bmpInput.Width;
            byte[,] bytData = new byte[intHeight, intWidth];
            bytData = GetMatrixData(bmpInput);

            Rectangle rtg = new Rectangle(0, 0, bmpInput.Width, bmpInput.Height);
            BitmapData datInput = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            IntPtr ptrScan0 = datInput.Scan0;
            int intStride = datInput.Stride;
            int intSpace = intStride - intWidth * 3;
            unsafe
            {
                byte* ptr = (byte*)ptrScan0;
                int[,] intMask1 = new int[3, 3] { { 0, -1, 0 }, { -1, 4, -1 }, { 0, -1, 0 } };
                int intValue;
                for (int i = 0; i <= intHeight - 1; i++)
                {
                    for (int j = 0; j <= intWidth - 1; j++)
                    {
                        if ((i > 0) && (i < intHeight - 1) && (j > 0) && (j < intWidth - 1))
                        {
                            intValue = (PointCalc(bytData, intMask1, i, j)); //+ PointCalc(bytData, intMask2, i, j));
                            if (intValue > 255) intValue = 255;
                            if (intValue < 0) intValue = 0;
                            ptr[0] = ptr[1] = ptr[2] = (byte)intValue;
                        }
                        ptr += 3;
                    }
                    ptr += intSpace;
                }
            }
            bmpInput.UnlockBits(datInput);
        }
        public void SegmentFixd(Bitmap bmpInput, byte threshold)
        {
            // Lock the bitmap's bits.  
            Rectangle rtg = new Rectangle(0, 0, bmpInput.Width, bmpInput.Height);
            BitmapData bmpData = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, bmpInput.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            // This code is specific to a bitmap with 24 bits per pixels.
            int bytes = bmpInput.Width * bmpInput.Height * 3;
            byte[] rgbValues = new byte[bytes];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            // Gray transform 
            int GrayLevel;
            for (int counter = 0; counter < rgbValues.Length; counter += 3)
            {
                if (rgbValues[counter + 0] > threshold)
                    GrayLevel = 255;
                else
                    GrayLevel = 0;
                rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = (byte)GrayLevel;
            }

            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            // Unlock the bits.
            bmpInput.UnlockBits(bmpData);
        }

        public void SegmentLap(Bitmap bmpInput)
        {
            // Lock the bitmap's bits.  
            Rectangle rtg = new Rectangle(0, 0, bmpInput.Width, bmpInput.Height);
            BitmapData bmpData = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, bmpInput.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            // This code is specific to a bitmap with 24 bits per pixels.
            int bytes = bmpInput.Width * bmpInput.Height * 3;
            byte[] rgbValues = new byte[bytes];
            long[] His = new long[256];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            for (int i = 0; i < 256; i++) His[i] = 0; // Đặt các histogram về không
            for (int counter = 0; counter < rgbValues.Length; counter += 3) His[rgbValues[counter]]++; // Tăng số biến đếm của mỗi histogram
            int T, K1, K0;
            long smf, smb;
            long mf = 0; long mb = 0;

            T = 128; K0 = 0; K1 = 128;
            while (K0 != K1)
            {
                K0 = K1; mf = 0; smf = 0;
                for (int i = 0; i < T; i++)
                {
                    mf += His[i] * i;
                    smf += His[i];
                }
                mf = mf / smf;
                mb = 0;
                smb = 0;
                for (int i = T; i < 256; i++)
                {
                    mb += His[i] * i;
                    smb += His[i];
                }
                mb = mb / smb;
                K1 = Convert.ToInt16((mb + mf) / 2);
                T = K1;
            }
            MessageBox.Show(T.ToString());
            for (int counter = 0; counter < rgbValues.Length; counter += 3)
            {
                if (rgbValues[counter] > T)
                    rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = 255;
                else rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = 0;
            }

            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);
            // Unlock the bits.
            bmpInput.UnlockBits(bmpData);
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

        public static byte[,] GetMatrixData(Bitmap bmpInput)
        {
            Rectangle rtg = new Rectangle(0, 0, bmpInput.Width, bmpInput.Height);
            BitmapData datInput = bmpInput.LockBits(rtg, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            IntPtr ptrScan0 = datInput.Scan0;
            int intHeight = datInput.Height;
            int intWidth = datInput.Width;
            int intSpace = datInput.Stride - intWidth * 3;
            byte[,] bytResult = new byte[intHeight, intWidth];
            unsafe
            {
                byte* ptr = (byte*)ptrScan0;
                double dblGray;
                byte bytGray;
                for (int i = 0; i <= intHeight - 1; i++)
                {
                    for (int j = 0; j <= intWidth - 1; j++)
                    {
                        dblGray = 0.299 * ptr[2] + 0.587 * ptr[1] + 0.144 * ptr[0];
                        if (dblGray > 255) dblGray = 255;
                        if (dblGray < 0) dblGray = 0;
                        bytGray = (byte)dblGray;
                        bytResult[i, j] = bytGray;
                        ptr += 3;
                    }
                    ptr += intSpace;
                }
            }
            bmpInput.UnlockBits(datInput);
            return bytResult;
        }

        public void AverageFilter(Bitmap bmpInput)
        {
            int intHeight = bmpInput.Height; int intWidth = bmpInput.Width;
            byte[,] bytData = new byte[intHeight, intWidth];
            bytData = GetMatrixData(bmpInput);
            Rectangle rtg = new Rectangle(0, 0, bmpInput.Width, bmpInput.Height);
            BitmapData datInput = bmpInput.LockBits(rtg, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            IntPtr ptrScan0 = datInput.Scan0;
            int intStride = datInput.Stride;
            int intSpace = intStride - intWidth * 3;
            unsafe
            {
                byte* ptr = (byte*)ptrScan0;
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
                            ptr[0] = ptr[1] = ptr[2] = (byte)intValue;
                        }
                        ptr += 3;
                    }
                    ptr += intSpace;
                }
            }
            bmpInput.UnlockBits(datInput);
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