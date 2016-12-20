using BTLXLA.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Input;
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
        private CoreApplicationView view;
        private int stepN = 2;
        private int stepS = 1;


        /// <summary>
        /// 0=front 1=back
        /// </summary>
        //private int CamId = 1;
        private OcrEngine ocrEngine;

        GestureRecognizer gestureRecognizer = new GestureRecognizer();

        public MainPage()
        {
            this.InitializeComponent();

            view = CoreApplication.GetCurrentView();
            this.gestureRecognizer.GestureSettings = Windows.UI.Input.GestureSettings.Tap | Windows.UI.Input.GestureSettings.DoubleTap | Windows.UI.Input.GestureSettings.RightTap | Windows.UI.Input.GestureSettings.Drag;

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
            if (DisplayInformation.AutoRotationPreferences != DisplayOrientations.Portrait)
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
            this.InitCam(Windows.Devices.Enumeration.Panel.Back);
        }


        private async void InitCam(Windows.Devices.Enumeration.Panel panel)
        {
            try
            {
                rectCrop.Visibility = Visibility.Collapsed;

                //StorageFile file = await Converter.WriteableBitmapToStorageFile(new WriteableBitmap(384, 384), Converter.FileFormat.Jpeg);
                //await rectCrop.LoadImage(file);

                Debug.WriteLine(1000);
                if (captureManager != null)
                {
                    Debug.WriteLine(1001);
                    await captureManager.StopPreviewAsync();
                    await captureManager.StopRecordAsync();
                    this.capture.Source = null;
                }

                Debug.WriteLine(1002);
                captureManager = new MediaCapture();
                var cameraDevice = await FindCameraDeviceByPanelAsync(panel);
                var settings = new MediaCaptureInitializationSettings { VideoDeviceId = cameraDevice.Id };
                await captureManager.InitializeAsync(settings);
                capture.Source = captureManager;

                Debug.WriteLine(1003);
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

                Debug.WriteLine(1004);
                var torch = captureManager.VideoDeviceController.TorchControl;
                if (torch.Supported)
                {
                    if (flashMode == 1)
                    {
                        captureManager.VideoDeviceController.FlashControl.Enabled = false;
                    }
                    else if (flashMode == 0)
                    {
                        captureManager.VideoDeviceController.FlashControl.Enabled = true;
                    }
                }

                Debug.WriteLine(1005);
                await captureManager.StartPreviewAsync();

                //IMPROVING CAM RESOLUTION
                //if (GetDisplayAspectRatio() == DisplayAspectRatio.FifteenByNine)
                //{
                //    GetFifteenByNineBounds();
                //}
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
        double[,] matrixImage;

        #region APP MAIN FUNCTIONS
        private async void btnCapture_Tapped(object sender, TappedRoutedEventArgs e)
        {
           //Debug.WriteLine(1);
            await captureManager.VideoDeviceController.FocusControl.FocusAsync();

           //Debug.WriteLine(2);
            //Create JPEG image Encoding format for storing image in JPEG type  
            ImageEncodingProperties imgFormat = ImageEncodingProperties.CreateJpeg();

           //Debug.WriteLine(3);
            //rotate and save the image
            using (var imageStream = new InMemoryRandomAccessStream())
            {

               //Debug.WriteLine(4);
                //generate stream from MediaCapture
                await captureManager.CapturePhotoToStreamAsync(imgFormat, imageStream);

               //Debug.WriteLine(5);
                //create decoder and encoder
                BitmapDecoder dec = await BitmapDecoder.CreateAsync(imageStream);
                BitmapEncoder enc = await BitmapEncoder.CreateForTranscodingAsync(imageStream, dec);

               //Debug.WriteLine(6);
                //roate the image
                enc.BitmapTransform.Rotation = BitmapRotation.Clockwise90Degrees;

               //Debug.WriteLine(7);
                //write changes to the image stream
                await enc.FlushAsync();

               //Debug.WriteLine(8);
                // create storage file in local app storage  
                TimeSpan span = DateTime.Now.TimeOfDay;
                string time = String.Format("{0}{1}{2}", span.Hours, span.Minutes, span.Seconds);
                string fileName = "#XLA_" + DateTime.Today.ToString("yyyyMMdd") + "_" + time + ".jpeg";
                file = await KnownFolders.CameraRoll.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
                //await captureManager.CapturePhotoToStorageFileAsync(imgFormat, file);

                // Get photo as a BitmapImage
                //bmpImage = new BitmapImage(new Uri(file.Path));


               //Debug.WriteLine(9);
                using (var fileStream = await file.OpenStreamForWriteAsync())
                {
                    try
                    {
                       //Debug.WriteLine(10);
                        //because of using statement stream will be closed automatically after copying finished
                        await RandomAccessStream.CopyAsync(imageStream, fileStream.AsOutputStream());

                       //Debug.WriteLine(11);
                        rectCrop.Visibility = Visibility.Visible;
                       //Debug.WriteLine(13);
                        await rectCrop.LoadImage(file);
                       //Debug.WriteLine(12);
                    }
                    catch
                    {

                    }
                }
            }
        }

        private void grdReCap_Tapped(object sender, TappedRoutedEventArgs e)
        {
            imgCapped.Source = null;
            this.InitCam(Windows.Devices.Enumeration.Panel.Back);
        }

        private async void grdScan_Tapped(object sender, TappedRoutedEventArgs e)
        {
            grdLoading.Visibility = Visibility.Visible;

            try
            {

                // Prevent another OCR request, since only image can be processed at the time at same OCR engine instance.
                grdScan.IsTapEnabled = false;
                btnCapture.IsEnabled = false;
                string extractedText = "";

                wb = rectCrop.CroppedImage;

                //PREPROCESSING PROCEDURES

                byte[] arrImg = ImageClass.ConvertBitmapToByteGray(wb);
                matrixImage = Converter.ByteArrayToMatrix(arrImg, wb.PixelWidth, 4);
                
                //SMOOTHING
                matrixImage = ImageClass.GaussSmoothing(matrixImage, stepN * 2 + 1, stepS * 0.5);

                //GAUSSIAN SMOOTHING
                //Debug.WriteLine("stepS " + stepS);
                //Debug.WriteLine("stepN " + stepN);
                //SHARPENNING
                matrixImage = ImageClass.ConvolutionFilter(matrixImage, ImageClass.maskSharp1, 1.0);

                ////This is stupid, too
                //matrixImage = ImageClass.NoiseFilter(matrixImage, "MAX");

                ////HE - I am not sure that method can improve the quality of image
                //matrixImage = ImageClass.HistogranEqualization(matrixImage);

                //THRESHOLDING
                int otsuT = ImageClass.GetOtsuThreshold(matrixImage);
                matrixImage = ImageClass.OtsuProcessed(matrixImage, otsuT);

                //END OF THESE PROCEDURES

                arrImg = Converter.MatrixToByteArray(matrixImage);
                wb = ImageClass.ConvertByteArrayToBitmap(arrImg, wb.PixelWidth);

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

                        return;
                    }

                   //Debug.WriteLine(-1);
                    // This main API call to extract text from image.
                    var ocrResult = await ocrEngine.RecognizeAsync((uint)wb.PixelHeight, (uint)wb.PixelWidth, wb.PixelBuffer.ToArray());

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

                       //Debug.WriteLine(2);
                        // Iterate over recognized lines of text.
                        foreach (var line in ocrResult.Lines)
                        {
                            // Iterate over words in line.
                            foreach (var word in line.Words)
                            {
                               //Debug.WriteLine(word.Text);
                                extractedText += word.Text;
                            }
                            break;
                        }

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
                grdLoading.Visibility = Visibility.Collapsed;
            }
        }

        private void grdLibrary_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FileOpenPicker fileOpenPicker = new FileOpenPicker();
            fileOpenPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            fileOpenPicker.ViewMode = PickerViewMode.Thumbnail;

            fileOpenPicker.FileTypeFilter.Clear();
            fileOpenPicker.FileTypeFilter.Add(".jpg");
            fileOpenPicker.FileTypeFilter.Add(".jpeg");
            fileOpenPicker.FileTypeFilter.Add(".png");

            fileOpenPicker.PickSingleFileAndContinue();
            view.Activated += View_Activated;
        }

        #endregion

        private async void View_Activated(CoreApplicationView sender, IActivatedEventArgs args1)
        {
            FileOpenPickerContinuationEventArgs args = args1 as FileOpenPickerContinuationEventArgs;

            if (args != null)
            {
                if (args.Files.Count == 0) return;

                view.Activated -= View_Activated;
                file = args.Files[0];
                rectCrop.Visibility = Visibility.Visible;
                await rectCrop.LoadImage(file);
            }
        }


        #region CAMERA HELPERS
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
           //Debug.WriteLine(torch.PowerPercent);
            if (torch.Supported)
            {
                if (flashMode == 1)
                {
                    captureManager.VideoDeviceController.FlashControl.Enabled = false;
                    //captureManager.MediaCaptureSettings.
                }
                else if (flashMode == 0)
                {
                    captureManager.VideoDeviceController.FlashControl.Enabled = true;
                }
            }
        }

        /// <summary>
        /// the display resolution format
        /// </summary>
        public enum DisplayAspectRatio
        {
            Unknown = -1,

            FifteenByNine = 0,

            SixteenByNine = 1
        }


        /// <summary>
        /// method to detect DisplayResolutionFormat
        /// </summary>
        /// <returns></returns>
        private DisplayAspectRatio GetDisplayAspectRatio()
        {
            DisplayAspectRatio result = DisplayAspectRatio.Unknown;

            //WP8.1 uses logical pixel dimensions, we need to convert this to raw pixel dimensions
            double logicalPixelWidth = Windows.UI.Xaml.Window.Current.Bounds.Width;
            double logicalPixelHeight = Windows.UI.Xaml.Window.Current.Bounds.Height;

            double rawPerViewPixels = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            double rawPixelHeight = logicalPixelHeight * rawPerViewPixels;
            double rawPixelWidth = logicalPixelWidth * rawPerViewPixels;

            //calculate and return screen format
            double relation = Math.Max(rawPixelWidth, rawPixelHeight) / Math.Min(rawPixelWidth, rawPixelHeight);
            if (Math.Abs(relation - (15.0 / 9.0)) < 0.01)
            {
                result = DisplayAspectRatio.FifteenByNine;
            }
            else if (Math.Abs(relation - (16.0 / 9.0)) < 0.01)
            {
                result = DisplayAspectRatio.SixteenByNine;
            }

            return result;
        }



        /// <summary>
        /// Helper to get the correct Bounds for 15:9 screens and to set finalPhotoAreaBorder values
        /// </summary>
        /// <returns></returns>
        private BitmapBounds GetFifteenByNineBounds()
        {
            BitmapBounds bounds = new BitmapBounds();

            //image size is raw pixels, so we need also here raw pixels
            double logicalPixelWidth = Windows.UI.Xaml.Window.Current.Bounds.Width;
            double logicalPixelHeight = Windows.UI.Xaml.Window.Current.Bounds.Height;

            double rawPerViewPixels = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            double rawPixelHeight = logicalPixelHeight * rawPerViewPixels;
            double rawPixelWidth = logicalPixelWidth * rawPerViewPixels;

            //calculate scale factor of UniformToFill Height (remember, we rotated the preview)
            double scaleFactorVisualHeight = GetMaxResolution().Width / rawPixelHeight;

            //calculate the visual Width 
            //(because UniFormToFill scaled the previewElement Width down to match the previewElement Height)
            double visualWidth = GetMaxResolution().Height / scaleFactorVisualHeight;

            //calculate cropping area for 15:9
            uint scaledBoundsWidth = GetMaxResolution().Height;
            uint scaledBoundsHeight = (scaledBoundsWidth / 9) * 15;

            //we are starting at the top of the image
            bounds.Y = 0;
            //cropping the image width
            bounds.X = 0;
            bounds.Height = scaledBoundsHeight;
            bounds.Width = scaledBoundsWidth;

            //set finalPhotoAreaBorder values that shows the user the area that is captured
            //finalPhotoAreaBorder.Width = (scaledBoundsWidth / scaleFactorVisualHeight) / rawPerViewPixels;
            //finalPhotoAreaBorder.Height = (scaledBoundsHeight / scaleFactorVisualHeight) / rawPerViewPixels;
            //finalPhotoAreaBorder.Margin = new Thickness(
            //                                Math.Floor(((rawPixelWidth - visualWidth) / 2) / rawPerViewPixels),
            //                                0,
            //                                Math.Floor(((rawPixelWidth - visualWidth) / 2) / rawPerViewPixels),
            //                                0);
            //finalPhotoAreaBorder.Visibility = Visibility.Visible;

            return bounds;
        }
        #endregion

        #region CAM RES HELPER
        /// <summary>
        /// get highest possible resolution
        /// </summary>
        /// <returns></returns>
        private VideoEncodingProperties GetMaxResolution()
        {
            VideoEncodingProperties resolutionMax = null;

            //get all photo properties
            var resolutions = captureManager.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.Photo);

            //generate new list to work with
            List<VideoEncodingProperties> vidProps = new List<VideoEncodingProperties>();

            //add only those properties that are 16:9 to our own list
            for (var i = 0; i < resolutions.Count; i++)
            {
                VideoEncodingProperties res = (VideoEncodingProperties)resolutions[i];

                if (MatchScreenFormat(new Size(res.Width, res.Height)) != CameraResolutionFormat.FourByThree)
                {
                    vidProps.Add(res);
                }
            }

            //order the list, and select the highest resolution that fits our limit
            if (vidProps.Count != 0)
            {
                vidProps = vidProps.OrderByDescending(r => r.Width).ToList();

                resolutionMax = vidProps.Where(r => r.Width < 2600).First();
            }

            return resolutionMax;
        }


        /// <summary>
        /// the camera resolution format (aspect ratio)
        /// </summary>
        public enum CameraResolutionFormat
        {
            Unknown = -1,

            FourByThree = 0,

            SixteenByNine = 1
        }

        /// <summary>
        /// Helper to detect the correct camera resolution
        /// </summary>
        /// <param name="resolution"></param>
        /// <returns></returns>
        private CameraResolutionFormat MatchScreenFormat(Size resolution)
        {
            CameraResolutionFormat result = CameraResolutionFormat.Unknown;

            double relation = Math.Max(resolution.Width, resolution.Height) / Math.Min(resolution.Width, resolution.Height);
            if (Math.Abs(relation - (4.0 / 3.0)) < 0.01)
            {
                result = CameraResolutionFormat.FourByThree;
            }
            else if (Math.Abs(relation - (16.0 / 9.0)) < 0.01)
            {
                result = CameraResolutionFormat.SixteenByNine;
            }

            return result;
        }
        #endregion

    }
}
