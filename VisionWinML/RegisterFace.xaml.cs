using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media.Imaging;
using System.Collections.ObjectModel;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.Media.Capture;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading.Tasks;
using VisionWinML.Helpers;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace VisionWinML
{
    public class ImageChannel
    {
        public string ImagePath { get; set; }
        public string PersistedFaceId { get; set; }
        public FaceData FaceInfo { get; set; }
    }
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RegisterFace : Page
    {
        private WebcamHelper webcam;
        private StorageFile photoFile;
        private double idImageMaxWidth = 0;
        private Image[] userIDImages;
        public RegisterFace()
        {
            this.InitializeComponent();
            AppBarButtonPersonFaceRefresh_Click(null, null);

            WebcamFeed.Visibility = Visibility.Visible;
            //if (GeneralConstants.DisableLiveCameraFeed)
            //{
            //    WebcamFeed.Visibility = Visibility.Collapsed;
            //    DisabledFeedGrid.Visibility = Visibility.Visible;
            //}
            //else
            //{
            //    WebcamFeed.Visibility = Visibility.Visible;
            //    DisabledFeedGrid.Visibility = Visibility.Collapsed;
            //}
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
            //LiveFeedPanel.Visibility = Visibility.Collapsed;
            //DisabledFeedGrid.Visibility = Visibility.Visible;
        }
        private void PhotoGrid_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {

            // Populate photo grid with visitor ID photos:
            AppBarButtonPersonFaceRefresh_Click(null, null);
        }
        private async void AppBarButtonPersonFaceRefresh_Click(object sender, RoutedEventArgs e)
        {
            List<ImageChannel> imageChannel = new List<ImageChannel>();
            textBlockFace.Text = "Face - " + globals.gPersonSelected.name;

            if (null != globals.gPersonSelected && globals.gPersonSelected.name.Equals("...") == false)
            {
                personFaceProgressRing.IsActive = true;
                //appbarDeleteFaceButton.IsEnabled = false;

                //blob
                CloudBlobDirectory userface = HttpHandler.blobContainer.GetDirectoryReference(globals.gPersonGroupSelected.personGroupId + "/" + globals.gPersonSelected.personId);
                BlobContinuationToken token = null;
                idImageMaxWidth = personFaceListView.ActualHeight / 4 - 10;
                var blobcount = 0;
                do
                {
                    BlobResultSegment resultSegment = await userface.ListBlobsSegmentedAsync(token);
                    token = resultSegment.ContinuationToken;

                    foreach (IListBlobItem item in resultSegment.Results)
                    {
                        if (item.GetType() == typeof(CloudBlockBlob))
                        {
                            blobcount++;
                        }
                    }
                } while (token != null);
                userIDImages = new Image[blobcount];
                var num = 0;
                do
                {
                    BlobResultSegment resultSegment = await userface.ListBlobsSegmentedAsync(token);
                    token = resultSegment.ContinuationToken;

                    foreach (IListBlobItem item in resultSegment.Results)
                    {
                        if (item.GetType() == typeof(CloudBlockBlob))
                        {
                            CloudBlockBlob blob = (CloudBlockBlob)item;
                            var sas1 = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy()
                            {
                                Permissions = SharedAccessBlobPermissions.Read,
                                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(1),//Set this date/time according to your requirements
                            });
                            var imageuri = string.Format("{0}{1}", blob.Uri, sas1);
                            string bloburi = new Uri(imageuri).ToString();
                            BitmapImage idImage = new BitmapImage(new Uri(bloburi));
                            Image idImageControl = new Image();
                            idImageControl.Source = idImage;
                            idImageControl.MaxHeight = idImageMaxWidth;
                            userIDImages[num++] = idImageControl;
                        }
                    }
                } while (token != null);
                personFaceListView.ItemsSource = userIDImages;


                imageChannel.Clear();
                foreach (string persistedFaceId in globals.gPersonSelected.persistedFaceIds)
                {

                    FaceData faceInfo = await PersonFaceCmds.GetPersonFace(globals.gPersonGroupSelected.personGroupId,
                                                                         globals.gPersonSelected.personId,
                                                                         persistedFaceId);
                    if (null != faceInfo)
                    {
                        imageChannel.Add(new ImageChannel()
                        {
                            ImagePath = faceInfo.userData,
                            PersistedFaceId = persistedFaceId,
                            FaceInfo = faceInfo
                        });
                    }
                }
                //personFaceListView.ItemsSource = imageChannel;

                personFaceProgressRing.IsActive = false;
            }
            else
            {
                MessageDialog dialog = new MessageDialog("Person to be selected to find associated Faces", "Refresh Error");
                await dialog.ShowAsync();
            }
        }

        //private async void appbarFaceAddFromFileButton_Click(object sender, RoutedEventArgs e)
        //{
        //    personFaceProgressRing.IsActive = true;
        //    //appbarDeleteFaceButton.IsEnabled = false;

        //    LiveFeedPanel.Visibility = Visibility.Collapsed;
        //    //DisabledFeedGrid.Visibility = Visibility.Visible;

        //    FileOpenPicker filePicker = new FileOpenPicker();
        //    filePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
        //    filePicker.ViewMode = PickerViewMode.Thumbnail;

        //    filePicker.FileTypeFilter.Clear();
        //    filePicker.FileTypeFilter.Add(".jpeg"); filePicker.FileTypeFilter.Add(".jpg");
        //    filePicker.FileTypeFilter.Add(".png"); filePicker.FileTypeFilter.Add(".gif");

        //    StorageFile file = await filePicker.PickSingleFileAsync();
        //    photoFile = file;
        //    if (file != null)
        //    {
        //        // Open a stream for the selected file.
        //        // The 'using' block ensures the stream is disposed
        //        // after the image is loaded.
        //        using (Windows.Storage.Streams.IRandomAccessStream fileStream =
        //            await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
        //        {
        //            // Set the image source to the selected bitmap.
        //            BitmapImage bitmapImage = new BitmapImage();
        //            bitmapImage.SetSource(fileStream);
        //            uploadedImage.Source = bitmapImage;
        //        }
        //    }

        //}

        //private async void appbarFaceAddFromCameraButton_Click(object sender, RoutedEventArgs e)
        //{
        //    //appbarDeleteFaceButton.IsEnabled = false;

        //    if (GeneralConstants.DisableLiveCameraFeed)
        //    {
        //        LiveFeedPanel.Visibility = Visibility.Collapsed;
        //        DisabledFeedGrid.Visibility = Visibility.Visible;
        //    }
        //    else
        //    {
        //        LiveFeedPanel.Visibility = Visibility.Visible;
        //        DisabledFeedGrid.Visibility = Visibility.Collapsed;
        //    }
        //}
        private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            //if (LiveFeedPanel.Visibility == Visibility.Visible)
            //{
                StorageFile file = await webcam.CapturePhoto();
                if (null != file)
                {
                    await PersonFaceCmds.updateToBlob(file);
                }
            StorageFile file2 = await webcam.CapturePhoto();
            if (null != file2)
            {
                await PersonFaceCmds.updateToBlob(file2);
            }
            personFaceProgressRing.IsActive = false;
            //}
            //else
            //{
            //    if (null != photoFile)
            //    {
            //        await PersonFaceCmds.updateToBlob(photoFile);
            //    }

            //    personFaceProgressRing.IsActive = false;
            //    //appbarDeleteFaceButton.IsEnabled = true;
            //}
            AppBarButtonPersonFaceRefresh_Click(null, null);
            appBarTrainButton_Click(null, null);
        }
        private async void appBarTrainButton_Click(object sender, RoutedEventArgs e)
        {
            if (null != globals.gPersonGroupSelected && globals.gPersonGroupSelected.name.Equals("...") == false)
            {
                string result = await PersonGroupCmds.TrainPersonGroups(globals.gPersonGroupSelected.personGroupId);
                bool success = true;
                while (success)
                {
                    if (result == "succeeded")
                    {
                        MessageDialog dialog = new MessageDialog("Please go back to the main page and log in.", "Registration Complete.");
                        await dialog.ShowAsync();
                        break;
                    }
                    else
                        result = await PersonGroupCmds.TrainPersonGroups(globals.gPersonGroupSelected.personGroupId);
                    continue;
                }
            }
            else
            {
                MessageDialog dialog = new MessageDialog("Select a PersonGroup to train", "Training Error");
                await dialog.ShowAsync();
            }
        }

        private void Home_Button_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage));
        }
    }
}
