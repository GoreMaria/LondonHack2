﻿using Lumia.Imaging;
using Lumia.Imaging.Adjustments;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace NoHands
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        bool frontCam;
        MediaCapture mediaCapture;

        public MainPage()
        {
            this.InitializeComponent();

            this.Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            mediaCapture = new MediaCapture();
            DeviceInformationCollection devices =
        await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            // Use the front camera if found one
            if (devices == null) return;
            DeviceInformation info = devices[0];

            foreach (var devInfo in devices)
            {
                if (devInfo.Name.ToLowerInvariant().Contains("front"))
                {
                    info = devInfo;
                    frontCam = true;
                    continue;
                }
            }

            await mediaCapture.InitializeAsync(
                new MediaCaptureInitializationSettings
                {
                    VideoDeviceId = info.Id
                });

            captureElement.Source = mediaCapture;
            captureElement.FlowDirection = frontCam ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            await mediaCapture.StartPreviewAsync();

            DisplayInformation displayInfo = DisplayInformation.GetForCurrentView();
            displayInfo.OrientationChanged += DisplayInfo_OrientationChanged;

            DisplayInfo_OrientationChanged(displayInfo, null);
        }

        private void DisplayInfo_OrientationChanged(DisplayInformation sender, object args)
        {
            if (mediaCapture != null)
            {
                mediaCapture.SetPreviewRotation(frontCam
                ? VideoRotationLookup(sender.CurrentOrientation, true)
                : VideoRotationLookup(sender.CurrentOrientation, false));
                var rotation = VideoRotationLookup(sender.CurrentOrientation, false);
                mediaCapture.SetRecordRotation(rotation);
            }
        }

        private VideoRotation VideoRotationLookup(DisplayOrientations displayOrientation, bool counterclockwise)
        {
            switch (displayOrientation)
            {
                case DisplayOrientations.Landscape:
                    return VideoRotation.None;

                case DisplayOrientations.Portrait:
                    return (counterclockwise) ? VideoRotation.Clockwise270Degrees : VideoRotation.Clockwise90Degrees;

                case DisplayOrientations.LandscapeFlipped:
                    return VideoRotation.Clockwise180Degrees;

                case DisplayOrientations.PortraitFlipped:
                    return (counterclockwise) ? VideoRotation.Clockwise90Degrees :
                    VideoRotation.Clockwise270Degrees;

                default:
                    return VideoRotation.None;
            }
        }

        private async void OnTap(object sender, TappedRoutedEventArgs e)
        {
            ImageEncodingProperties imageProperties = ImageEncodingProperties.CreateJpeg();
            var fPhotoStream = new InMemoryRandomAccessStream();

            await mediaCapture.CapturePhotoToStreamAsync(imageProperties, fPhotoStream);
            await fPhotoStream.FlushAsync();
            fPhotoStream.Seek(0);

            var _bmp = new BitmapImage();
            _bmp.SetSource(fPhotoStream);
            PreviewImage.Source = _bmp;
        }

        private SwapChainPanelRenderer m_renderer;
        private WriteableBitmap _writeableBitmap;
        private GrayscaleEffect _grayscaleEffect;
        private BrightnessEffect _brightnessEffect;

        /// <summary>
        /// TODO: Apply filter to image
        /// </summary>
        /// <param name="fileStream"></param>
        private async void ApplyEffectAsync(IRandomAccessStream fileStream)
        {
            double scaleFactor = 1.0;
            scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;

            _writeableBitmap = new WriteableBitmap((int)(Window.Current.Bounds.Width * scaleFactor), (int)(Window.Current.Bounds.Height * scaleFactor));
            _grayscaleEffect = new GrayscaleEffect();
            _brightnessEffect = new BrightnessEffect(_grayscaleEffect);
            //            m_renderer = new SwapChainPanelRenderer(_brightnessEffect, SwapChainPanelTarget);

            string errorMessage = null;

            try
            {
                // Rewind the stream to start.
                fileStream.Seek(0);

                // Set the imageSource on the effect and render.
                ((IImageConsumer)_grayscaleEffect).Source = new Lumia.Imaging.RandomAccessStreamImageSource(fileStream);
                await m_renderer.RenderAsync();

            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
            }
        }
    }
}
