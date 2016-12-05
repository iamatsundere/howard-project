using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using WindowsPreview.Media.Ocr;

namespace BTLXLA
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        MediaCapture captureManager;
        /// <summary>
        /// 0=front 1=back
        /// </summary>
        private int CamId = 1;
        // OCR engine instance used to extract text from images.
        private OcrEngine ocrEngine;

        public MainPage()
        {
            this.InitializeComponent();
            ocrEngine = new OcrEngine(OcrLanguage.English);
            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
            this.InitCam(Windows.Devices.Enumeration.Panel.Back);
        }


        private async void InitCam(Windows.Devices.Enumeration.Panel panel)
        {
            try
            {
                if (captureManager != null)
                {
                    await captureManager.StopPreviewAsync();
                    this.capture.Source = null;
                }
                captureManager = new MediaCapture();
                var cameraDevice = await FindCameraDeviceByPanelAsync(panel);
                var settings = new MediaCaptureInitializationSettings { VideoDeviceId = cameraDevice.Id };
                await captureManager.InitializeAsync(settings);
                capture.Source = captureManager;
                string currentorientation = DisplayInformation.GetForCurrentView().CurrentOrientation.ToString();
                switch (currentorientation)
                {
                    case "Landscape":
                        captureManager.SetPreviewRotation(VideoRotation.None);
                        break;
                    case "Portrait":
                        captureManager.SetPreviewRotation(VideoRotation.Clockwise90Degrees);
                        break;
                    case "LandscapeFlipped":
                        captureManager.SetPreviewRotation(VideoRotation.Clockwise180Degrees);
                        break;
                    case "PortraitFlipped":
                        captureManager.SetPreviewRotation(VideoRotation.Clockwise270Degrees);
                        break;
                    default:
                        captureManager.SetPreviewRotation(VideoRotation.None);
                        break;
                }
                await captureManager.StartPreviewAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private static async Task<DeviceInformation> FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel desiredPanel)
        {
            // Get available devices for capturing pictures
            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            // Get the desired camera by panel
            DeviceInformation desiredDevice = allVideoDevices.FirstOrDefault(x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == desiredPanel);

            // If there is no device mounted on the desired panel, return the first device found
            return desiredDevice ?? allVideoDevices.FirstOrDefault();
        }



        public static WriteableBitmap wb;
        StorageFile file;
        BitmapImage bmpImage;

        private async void btnCapture_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await captureManager.VideoDeviceController.FocusControl.FocusAsync();

            //Create JPEG image Encoding format for storing image in JPEG type  
            ImageEncodingProperties imgFormat = ImageEncodingProperties.CreateJpeg();

            //rotate and save the image
            using (var imageStream = new InMemoryRandomAccessStream())
            {

                //generate stream from MediaCapture
                await captureManager.CapturePhotoToStreamAsync(imgFormat, imageStream);

                //create decoder and encoder
                BitmapDecoder dec = await BitmapDecoder.CreateAsync(imageStream);
                BitmapEncoder enc = await BitmapEncoder.CreateForTranscodingAsync(imageStream, dec);

                //roate the image
                enc.BitmapTransform.Rotation = BitmapRotation.Clockwise90Degrees;

                //write changes to the image stream
                await enc.FlushAsync();

                // create storage file in local app storage  
                TimeSpan span = DateTime.Now.TimeOfDay;
                string time = String.Format("{0}{1}{2}", span.Hours, span.Minutes, span.Seconds);
                string fileName = "123xe_" + DateTime.Today.ToString("yyyyMMdd") + "_" + time + ".jpeg";
                file = await KnownFolders.CameraRoll.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
                //await captureManager.CapturePhotoToStorageFileAsync(imgFormat, file);

                // Get photo as a BitmapImage
                bmpImage = new BitmapImage(new Uri(file.Path));

                using (var fileStream = await file.OpenStreamForWriteAsync())
                {
                    try
                    {
                        Debug.WriteLine(9);
                        //because of using statement stream will be closed automatically after copying finished
                        await RandomAccessStream.CopyAsync(imageStream, fileStream.AsOutputStream());

                        Debug.WriteLine(11);
                        // imagePreview is a <Image> object defined in XAML
                        imgCapped.Source = bmpImage;
                    }
                    catch
                    {

                    }
                }

                //captureManager = new MediaCapture();
                //capture.Source = null;

                //wb = await Converter.StorageFileToWriteableBitmap(file);
                //Frame.Navigate(typeof(CapturedPage), wb);
            }
        }

        private void grdReCap_Tapped(object sender, TappedRoutedEventArgs e)
        {
            imgCapped.Source = null;
            this.InitCam(Windows.Devices.Enumeration.Panel.Back);
        }

        private async void grdScan_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                // Prevent another OCR request, since only image can be processed at the time at same OCR engine instance.
                grdScan.IsTapEnabled = false;
                btnCapture.IsEnabled = false;
                string extractedText = "";

                Debug.WriteLine(bmpImage.PixelWidth + " " + bmpImage.PixelHeight);
                //From stream to WriteableBitmap
                wb = await StorageFileToWriteableBitmap(file);

                int fixedSize = (wb.PixelHeight < wb.PixelWidth) ? wb.PixelWidth : wb.PixelHeight;


                //// Get the size of the image when it is displayed on the phone
                double displayedWidth = imgCapped.ActualWidth;
                double displayedHeight = imgCapped.ActualHeight;

                double fixedDisplay = (displayedHeight < displayedWidth) ? displayedWidth : displayedHeight;

                double ratio = fixedSize / fixedDisplay;
                Debug.WriteLine(ratio);

                double top = grdTop.ActualHeight;
                double left = grdBorder.ActualWidth - grdTop.ActualWidth;

                Debug.WriteLine((int)left + " " + (int)top + " " +
                    (int)(rect.ActualWidth * ratio) + " " + (int)(rect.ActualHeight * ratio));

                wb = wb.Crop((int)(left * ratio), (int)(top * ratio),
                    (int)(rect.ActualWidth * ratio), (int)(rect.ActualHeight * ratio));

                {
                    // Check whether is loaded image supported for processing.
                    // Supported image dimensions are between 40 and 2600 pixels.
                    if (wb.PixelHeight < 40 ||
                        wb.PixelHeight > 2600 ||
                        wb.PixelWidth < 40 ||
                        wb.PixelWidth > 2600)
                    {
                        MessageDialog dialog = new MessageDialog("Image size is not supported." +
                                            Environment.NewLine +
                                            "Loaded image size is " + wb.PixelWidth + "x" + wb.PixelHeight + "." +
                                            Environment.NewLine +
                                            "Supported image dimensions are between 40 and 2600 pixels.");
                        await dialog.ShowAsync();
                        //ImageText.Style = (Style)Application.Current.Resources["RedTextStyle"];

                        return;
                    }

                    Debug.WriteLine(-1);
                    // This main API call to extract text from image.
                    var ocrResult = await ocrEngine.RecognizeAsync((uint)wb.PixelHeight, (uint)wb.PixelWidth, wb.PixelBuffer.ToArray());

                    Debug.WriteLine(0);
                    // OCR result does not contain any lines, no text was recognized. 
                    if (ocrResult.Lines != null)
                    {
                        // Used for text overlay.
                        // Prepare scale transform for words since image is not displayed in original format.
                        var scaleTrasform = new ScaleTransform
                        {
                            CenterX = 0,
                            CenterY = 0,
                            ScaleX = imgCapped.ActualWidth / wb.PixelWidth,
                            ScaleY = imgCapped.ActualHeight / wb.PixelHeight,
                        };

                        if (ocrResult.TextAngle != null)
                        {

                            imgCapped.RenderTransform = new RotateTransform
                            {
                                Angle = (double)ocrResult.TextAngle,
                                CenterX = imgCapped.ActualWidth / 2,
                                CenterY = imgCapped.ActualHeight / 2
                            };
                        }

                        Debug.WriteLine(2);
                        // Iterate over recognized lines of text.
                        foreach (var line in ocrResult.Lines)
                        {
                            Debug.WriteLine(3);
                            // Iterate over words in line.
                            foreach (var word in line.Words)
                            {
                                Debug.WriteLine(4);
                                var originalRect = new Rect(word.Left, word.Top, word.Width, word.Height);
                                var overlayRect = scaleTrasform.TransformBounds(originalRect);

                                var wordTextBlock = new TextBlock()
                                {
                                    Height = overlayRect.Height,
                                    Width = overlayRect.Width,
                                    FontSize = overlayRect.Height * 0.8,
                                    Text = word.Text,

                                };
                                Debug.WriteLine(5);
                                // Define position, background, etc.
                                //var border = new Border()
                                //{
                                //    Margin = new Thickness(overlayRect.Left, overlayRect.Top, 0, 0),
                                //    Height = overlayRect.Height,
                                //    Width = overlayRect.Width,
                                //    Background = new SolidColorBrush(Colors.Orange),
                                //    Opacity = 0.5,
                                //    HorizontalAlignment = HorizontalAlignment.Left,
                                //    VerticalAlignment = VerticalAlignment.Top,
                                //    Child = wordTextBlock,

                                //};

                                //OverlayTextButton.IsEnabled = true;
                                // Put the filled textblock in the results grid.
                                //TextOverlay.Children.Add(border);
                                extractedText += word.Text + " ";
                            }
                        }
                        Debug.WriteLine(7);
                        //txtString.Text = extractedText;

                    }
                    else
                    {
                        extractedText = "";
                    }
                    Frame.Navigate(typeof(CallPage), extractedText);

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                imgCapped.Source = null;
                grdScan.IsTapEnabled = true;
                btnCapture.IsEnabled = true;
                ocrEngine = new OcrEngine(OcrLanguage.English);
            }
        }


        public async Task<WriteableBitmap> StorageFileToWriteableBitmap(StorageFile file)
        {
            WriteableBitmap wb = null;
            ImageProperties properties = await file.Properties.GetImagePropertiesAsync();
            wb = new WriteableBitmap((int)properties.Width, (int)properties.Height);
            wb.SetSource((await file.OpenReadAsync()));
            return wb;
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


        #region CAMERA 
        private int flashMode = 0;
        private string[] flashModesPath = new string[]
        {
            "M7,2V13H10V22L17,10H13L17,2H7Z",
            "M17,10H13L17,2H7V4.18L15.46,12.64M3.27,3L2,4.27L7,9.27V13H10V22L13.58,15.86L17.73,20L19,18.73L3.27,3Z"
            //"M16.85,7.65L18,4L19.15,7.65M19,2H17L13.8,11H15.7L16.4,9H19.6L20.3,11H22.2M3,2V14H6V23L13,11H9L13,2H3Z",
        };
        private void FlashChanged_Tapped(object sender, TappedRoutedEventArgs e)
        {
            flashMode += 1;
            if (flashMode == 2)
                flashMode = 0;
            pathFlash.Data = Converter.FromStringToGeometry(flashModesPath[flashMode]);

            // then to turn on/off camera
            var torch = captureManager.VideoDeviceController.TorchControl;
            Debug.WriteLine(torch.PowerPercent);
            if (torch.Supported)
            {
                if (flashMode == 1)
                {
                    torch.Enabled = false;
                }
                else if (flashMode == 0)
                {
                    torch.Enabled = true;
                }
            }
        }
        #endregion
    }


}
