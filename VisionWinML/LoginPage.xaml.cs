using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using VisionWinML.Helpers;

using Windows.UI.Popups;
using Newtonsoft.Json;
using Windows.UI.Xaml.Media;
using Microsoft.WindowsAzure.Storage.Blob;
using Windows.Storage.Streams;
using System.Net.Http;
using System.Linq;
using System.Text;
// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace VisionWinML
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginPage : Page
    {
        private WebcamHelper webcam;
        private bool doorbellJustPressed = false;
        public LoginPage()
        {
            this.InitializeComponent();
            WebcamFeed.Visibility = Visibility.Visible;
        }
        private async void DoorbellButton_Click(object sender, RoutedEventArgs e)
        {
            if (!doorbellJustPressed)
            {
                doorbellJustPressed = true;
                await DoorbellPressed();
            }

        }
        private async Task DoorbellPressed()
        {
            StorageFile file = null;
            if (webcam.IsInitialized())
            {
                // Stores current frame from webcam feed in a temporary folder
                file = await webcam.CapturePhoto();
                FaceQuery(file);
            }
            else
            {
                if (!webcam.IsInitialized())
                {
                    // The webcam has not been fully initialized for whatever reason:
                    Debug.WriteLine("Unable to analyze visitor at door as the camera failed to initlialize properly.");
                }
            }
            doorbellJustPressed = false;
            //FaceQuery(file);
        }
        private async void FaceQuery(StorageFile file)
        {
            CloudBlockBlob blob = null;
            string blobFileName = null;
            if (null != file)
            {
                progressRingMainPage.IsActive = true;
                BitmapImage bitmapImage = new BitmapImage();
                IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read);
                bitmapImage.SetSource(fileStream);

                blobFileName = System.Guid.NewGuid() + "." + file.Name.Split('.').Last<string>();

                await HttpHandler.tempContainer.CreateIfNotExistsAsync();
                BlobContainerPermissions permissions = new BlobContainerPermissions();
                permissions.PublicAccess = BlobContainerPublicAccessType.Blob;
                await HttpHandler.tempContainer.SetPermissionsAsync(permissions);
                blob = HttpHandler.tempContainer.GetBlockBlobReference(blobFileName);
                await blob.DeleteIfExistsAsync();
                await blob.UploadFromFileAsync(file);

                string uri = "https://westus.api.cognitive.microsoft.com/face/v1.0/detect?returnFaceId=true";
                string jsonString = "{\"url\":\"" + HttpHandler.storagePath + "visitors/" + blobFileName + "\"}";
                HttpContent content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await HttpHandler.client.PostAsync(uri, content);
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();

                    if (null == globals.gPersonGroupList)
                        globals.gPersonGroupList = await PersonGroupCmds.ListPersonGroups();

                    List<string> names = await VisitorCmds.CheckVisitorFace(responseBody, globals.gPersonGroupList);
                    if (0 == names.Count)
                    {
                        MessageDialog msg = new MessageDialog("Unregistered user. To register, please return to the Home page and press the Register button.", "Confirmation");
                        await msg.ShowAsync();
                    }
                    else
                        UnlockDoor(string.Join(", ", names.ToArray()));
                }
                else
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    globals.ShowJsonErrorPopup(responseBody);
                }

                await blob.DeleteAsync();
                progressRingMainPage.IsActive = false;
            }
        }
        private async void WebcamFeed_Loaded(object sender, RoutedEventArgs e)
        {
            if (webcam == null || !webcam.IsInitialized())
            {
                // Initialize Webcam Helper
                webcam = new WebcamHelper();
                await webcam.InitializeCameraAsync();

                // Set source of WebcamFeed on MainPage.xaml
                WebcamFeed.Source = webcam.mediaCapture;

                // Check to make sure MediaCapture isn't null before attempting to start preview. Will be null if no camera is attached.
                if (WebcamFeed.Source != null)
                {
                    // Start the live feed
                    await webcam.StartCameraPreview();
                }
            }
            else if (webcam.IsInitialized())
            {
                WebcamFeed.Source = webcam.mediaCapture;

                // Check to make sure MediaCapture isn't null before attempting to start preview. Will be null if no camera is attached.
                if (WebcamFeed.Source != null)
                {
                    await webcam.StartCameraPreview();
                }
            }

        }
        private async void UnlockDoor(string visitorName)
        {
            // Greet visitor
            MessageDialog msg = new MessageDialog("Welcome " + visitorName + ".", "Confirmation");
            await msg.ShowAsync();
            Frame.Navigate(typeof(VisionPos));
            //if (gpioAvailable)
            //{
            //    // Unlock door for specified ammount of time
            //    gpioHelper.UnlockDoor();
            //}
        }
        private void Home_Button_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage));
        }
    }
}
