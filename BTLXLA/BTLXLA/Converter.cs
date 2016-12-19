using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Services.Maps;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using Windows.Storage.FileProperties;
using System.Diagnostics;

namespace BTLXLA
{
    public class Converter
    {
        public static Geometry FromStringToGeometry(string vectorData)
        {
            Geometry geo;
            string geoCode = "<Geometry xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">";
            geoCode += vectorData + "</Geometry>";
            geo = (Geometry)XamlReader.Load(geoCode);
            return geo;
        }


        public static DateTime ConvertFromUnixTimestamp(int timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            origin = origin.Add(new TimeSpan(7, 0, 0));
            return origin.AddSeconds(timestamp);
        }

        public static int ConvertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return (int)Math.Floor(diff.TotalSeconds);
        }


        public static string MapAddressToAdressString(MapAddress mapAddress)
        {
            //string s = String.Format("{0}, {1}, {2}, {3}", mp.Address.Street, mp.Address.District, mp.Address.Town, mp.Address.Country);
            MapAddress address = mapAddress;
            string result = "";
            if (!address.Street.Equals(""))
                result += " " + address.Street + ",";
            if (!address.District.Equals(""))
                result += " " + address.District + ",";
            if (!address.Town.Equals(""))
                result += " " + address.Town + ",";
            if (!address.Continent.Equals(""))
                result += " " + address.Continent;
            if (result[result.Length - 1].Equals(','))
                result = result.Substring(0, result.Length - 1);
            return result;
        }

        public static Color ConvertHexStringToColor(string strColor)
        {
            Color color = new Color();
            byte r = Convert.ToByte(strColor[1].ToString() + strColor[2].ToString(), 16);
            byte g = Convert.ToByte(strColor[3].ToString() + strColor[4].ToString(), 16);
            byte b = Convert.ToByte(strColor[5].ToString() + strColor[6].ToString(), 16);
            color = Color.FromArgb(255, r, g, b);
            return color;
        }


        public static async Task<BitmapImage> Base64StringToBitmap(string source)
        {
            var ims = new InMemoryRandomAccessStream();
            var bytes = Convert.FromBase64String(source);
            var dataWriter = new DataWriter(ims);
            dataWriter.WriteBytes(bytes);
            await dataWriter.StoreAsync();
            ims.Seek(0);
            var img = new BitmapImage();
            img.SetSource(ims);
            return img;
        }

        public static async Task<string> ConvertStorageFileToBase64String(StorageFile imageFile)
        {
            var stream = await imageFile.OpenReadAsync();

            using (var dataReader = new DataReader(stream))
            {
                var bytes = new byte[stream.Size];
                await dataReader.LoadAsync((uint)stream.Size);
                dataReader.ReadBytes(bytes);

                return Convert.ToBase64String(bytes);
            }
        }

        public static async Task<byte[]> GetBytesAsync(StorageFile file)
        {
            byte[] fileBytes = null;
            if (file == null) return null;
            using (var stream = await file.OpenReadAsync())
            {
                fileBytes = new byte[stream.Size];
                using (var reader = new DataReader(stream))
                {
                    await reader.LoadAsync((uint)stream.Size);
                    reader.ReadBytes(fileBytes);
                }
            }
            return fileBytes;
        }

        public static async Task<byte[]> StorageFileToBinary(StorageFile storageFile)
        {
            var fileStream = await storageFile.OpenStreamForReadAsync();
            byte[] buffer = new byte[fileStream.Length];
            fileStream.Read(buffer, 0, (int)fileStream.Length);
            fileStream.Dispose();
            return buffer;
        }

        public static async Task<StorageFile> WriteableBitmapToStorageFile(WriteableBitmap WB, FileFormat fileFormat)
        {
            string FileName = "MyFile.";
            Guid BitmapEncoderGuid = BitmapEncoder.JpegEncoderId;
            switch (fileFormat)
            {
                case FileFormat.Jpeg:
                    FileName += "jpeg";
                    BitmapEncoderGuid = BitmapEncoder.JpegEncoderId;
                    break;
                case FileFormat.Png:
                    FileName += "png";
                    BitmapEncoderGuid = BitmapEncoder.PngEncoderId;
                    break;
                case FileFormat.Bmp:
                    FileName += "bmp";
                    BitmapEncoderGuid = BitmapEncoder.BmpEncoderId;
                    break;
                case FileFormat.Tiff:
                    FileName += "tiff";
                    BitmapEncoderGuid = BitmapEncoder.TiffEncoderId;
                    break;
                case FileFormat.Gif:
                    FileName += "gif";
                    BitmapEncoderGuid = BitmapEncoder.GifEncoderId;
                    break;
            }
            var file = await Windows.Storage.ApplicationData.Current.TemporaryFolder
        .CreateFileAsync(FileName, CreationCollisionOption.GenerateUniqueName);
            using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoderGuid, stream);
                Stream pixelStream = WB.PixelBuffer.AsStream();
                //byte[] pixels = new byte[pixelStream.Length];
                byte[] pixels = new byte[pixelStream.Length];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = 255;
                await pixelStream.ReadAsync(pixels, 0, pixels.Length);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied,
                          (uint)WB.PixelWidth,
                          (uint)WB.PixelHeight,
                          96.0,
                          96.0,
                          pixels);
                await encoder.FlushAsync();
            }
            return file;
        }
        public enum FileFormat
        {
            Jpeg,
            Png,
            Bmp,
            Tiff,
            Gif
        }

        public static async Task<WriteableBitmap> StorageFileToWriteableBitmap(StorageFile file)
        {
            WriteableBitmap wb = null;
            ImageProperties properties = await file.Properties.GetImagePropertiesAsync();
            wb = new WriteableBitmap((int)properties.Width, (int)properties.Height);
            wb.SetSource((await file.OpenReadAsync()));
            return wb;
        }

        public static double[,] ByteArrayToMatrix(byte[] bytes, int width, int skip)
        {
            int height = (bytes.Length / skip) / width;
            double[,] matrix = new double[height, width];
            for (int i = 0; i < bytes.Length; i += skip)
            {
                int x = (i / skip) % width;
                int y = (i / skip) / width;
                matrix[y, x] = (double)bytes[i];
            }
            return matrix;
        }
        public static byte[] MatrixToByteArray(double[,] matrix)
        {
            int height = matrix.GetLength(0);
            int width = matrix.GetLength(1);
            byte[] bytes = new byte[height * width * 4];
            for (int i = 0; i < height; i++)
                for (int j = 0; j < width; j++)
                {
                    int curren_pos = (i * width) * 4 + j * 4;
                    bytes[curren_pos + 0] = (byte)matrix[i, j];
                    bytes[curren_pos + 1] = (byte)matrix[i, j];
                    bytes[curren_pos + 2] = (byte)matrix[i, j];
                    bytes[curren_pos + 3] = 255;
                }
            return bytes;
        }
        

        private async Task<WriteableBitmap> ResizeWritableBitmap(WriteableBitmap baseWriteBitmap, uint width, uint height)
        {
            // Get the pixel buffer of the writable bitmap in bytes
            Stream stream = baseWriteBitmap.PixelBuffer.AsStream();
            byte[] pixels = new byte[(uint)stream.Length];
            await stream.ReadAsync(pixels, 0, pixels.Length);
            //Encoding the data of the PixelBuffer we have from the writable bitmap
            var inMemoryRandomStream = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, inMemoryRandomStream);
            encoder.SetPixelData(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Ignore, width, height, 96, 96, pixels);
            await encoder.FlushAsync();
            // At this point we have an encoded image in inMemoryRandomStream
            // We apply the transform and decode
            var transform = new BitmapTransform
            {
                ScaledWidth = width,
                ScaledHeight = height
            };
            inMemoryRandomStream.Seek(0);
            var decoder = await BitmapDecoder.CreateAsync(inMemoryRandomStream);
            var pixelData = await decoder.GetPixelDataAsync(
                            BitmapPixelFormat.Rgba8,
                            BitmapAlphaMode.Straight,
                            transform,
                            ExifOrientationMode.IgnoreExifOrientation,
                            ColorManagementMode.DoNotColorManage);
            //An array containing the decoded image data
            var sourceDecodedPixels = pixelData.DetachPixelData();
            // Approach 1 : Encoding the image buffer again:
            //Encoding data
            var inMemoryRandomStream2 = new InMemoryRandomAccessStream();
            var encoder2 = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, inMemoryRandomStream2);
            encoder2.SetPixelData(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Ignore, width, height, 96, 96, sourceDecodedPixels);
            await encoder2.FlushAsync();
            inMemoryRandomStream2.Seek(0);
            // finally the resized writablebitmap
            var bitmap = new WriteableBitmap((int)width, (int)height);
            await bitmap.SetSourceAsync(inMemoryRandomStream2);
            return bitmap;
        }

    }
}
