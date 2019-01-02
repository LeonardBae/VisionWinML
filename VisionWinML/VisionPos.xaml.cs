using System;
using System.Linq;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Media.Capture;
using Windows.ApplicationModel;
using System.Threading.Tasks;
using Windows.System.Display;
using Windows.Graphics.Display;
using Windows.Devices.Enumeration;
using Windows.Media.Capture.Frames;
using System.Threading;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.ApplicationModel.Core;
using System.Collections.Generic;
using System.Collections.ObjectModel;
// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace VisionWinML
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class VisionPos : Page, INotifyPropertyChanged
    {
        ObservableCollection<SnackData> dataList = new ObservableCollection<SnackData>();
        public VisionPos()
        {
            this.InitializeComponent();
            ChoGrid.Visibility = Visibility.Collapsed;
            KongGrid.Visibility = Visibility.Collapsed;
            GoGrid.Visibility = Visibility.Collapsed;
            KanGrid.Visibility = Visibility.Collapsed;
            KanmilGrid.Visibility = Visibility.Collapsed;
            // Initialize the input object
            //this.inputData = new ModelInput();
            // Event handlers
            Application.Current.Suspending += ApplicationSuspending;
            this.Loaded += OnLoaded;
        }
        int totalsum = 0;
        // For capturing video from the camera and displaying a preview
        MediaCapture mediaCapture;
        bool isPreviewing;
        DisplayRequest displayRequest = new DisplayRequest();

        // Classes generated from the ONNX model
        // IMPORTANT: Change to the class names to match the ones defined in the
        //   .cs file generated from your ONNX model
        //ModelInput inputData;
        //Model myVisionModel;
        private ONNXModel model = null;
        private string modelFileName = "selfcheckoutnew.onnx";
        // Frame reader for extracting frames from the video
        MediaFrameReader frameReader;
        int processingFlag;

        // Handle property changes
        public event PropertyChangedEventHandler PropertyChanged;

        string score;
        public string Score
        {
            get => this.score;
            set => this.SetProperty(ref this.score, value);
        }

        /// <summary>
        /// Sets <paramref name="propertyName"/> to a <paramref name="value"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="storage"></param>
        /// <param name="value"></param>
        /// <param name="propertyName"></param>
        void SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            storage = value;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Handle OnLoad event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Load the model
            await this.LoadModelAsync();

            // Start the camera video preview
            await StartPreviewAsync();
        }

        /// <summary>
        /// Load and create the model from the .onnx file
        /// </summary>
        /// <returns></returns>
        private async Task LoadModelAsync()
        {
            // Load the .onnx file
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/{modelFileName}"));
            // Create the model from the file
            // IMPORTANT: Change `Model.CreateModel` to match the class and methods in the
            //   .cs file generated from the ONNX model
            model = await ONNXModel.CreateONNXModel(file);
        }

        /// <summary>
        /// Try to retrieve the information for the camera by panel
        /// </summary>
        /// <param name="desiredPanel"></param>
        /// <returns></returns>
        private static async Task<DeviceInformation> FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel desiredPanel)
        {
            // Get available devices for capturing pictures
            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            // Get the desired camera by panel
            DeviceInformation desiredDevice = allVideoDevices.FirstOrDefault(
                x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == desiredPanel);

            // If there is no device mounted on the desired panel, return the first device found
            return desiredDevice ?? allVideoDevices.FirstOrDefault();
        }

        /// <summary>
        /// Processes media frames as they arrive
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        async void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            if (Interlocked.CompareExchange(ref this.processingFlag, 1, 0) == 0)
            {
                try
                {
                    using (var frame = sender.TryAcquireLatestFrame())
                    using (var videoFrame = frame.VideoMediaFrame?.GetVideoFrame())
                    {
                        if (videoFrame != null)
                        {
                            // If there is a frame, set it as input to the model
                            ONNXModelInput input = new ONNXModelInput();
                            input.data = videoFrame;
                            // Evaluate the input data
                            var evalOutput = await model.EvaluateAsync(input);
                            // Do something with the model output
                            await this.ProcessOutputAsync(evalOutput);
                        }
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref this.processingFlag, 0);
                }
            }
        }

        /// <summary>
        /// Process the output returned by the model
        /// </summary>
        /// <param name="evalOutput"></param>
        /// <returns></returns>
        async Task ProcessOutputAsync(ONNXModelOutput evalOutput)
        {

            //Get the tags and score to string and then display
            string label = evalOutput.classLabel.GetAsVectorView()[0];
            string loss = (evalOutput.loss[0][label] * 100.0f).ToString("#0.00");
            // Format the output string
            string score = $"{label} - {loss}";

            // Display the score
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    this.Score = score;
                }
            );
            string result;
            float a;
            a = System.Convert.ToSingle(loss);// evalOutput.loss.Values.ElementAt(i);
                if (a > (float)90 && label != "Nagative")
                {
                    result = label;

                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    async () =>
                    {
                        await frameReader.StopAsync();
                        bool Result = await ShowConfirmationDialog(result);
                    });
                }
        }
        public async Task<bool> ShowConfirmationDialog(string result)
        {
            // Create the message dialog and set its content.
            var msg = new MessageDialog("Are you sure " + result + " ?", "Confirmation");

            bool Result = false;

            // Add commands and set their callbacks as inline event handlers.
            // The callback methods run in the UI thread.
            msg.Commands.Add(new UICommand("Yes", async command =>
            {
                SetData(result);
                await SetPicAsync(result);
                await frameReader.StartAsync();
                Result = true;
            }));

            msg.Commands.Add(new UICommand("No", async command =>
            {
                await frameReader.StartAsync();
                Result = false;
            }));

            // Set the command that will be invoked by default i.e. when the user presses Enter.
            msg.DefaultCommandIndex = 0;

            // Set the command to be invoked when Esc is pressed.
            msg.CancelCommandIndex = 1;

            // Show the message dialog
            await msg.ShowAsync();

            return Result;
        }
        private async Task SetPicAsync(string name)
        {
            DisabledFeedGrid.Visibility = Visibility.Collapsed;
            if (name == "chocosongi")
            {                
                ChoGrid.Visibility = Visibility.Visible;
                await Task.Delay(TimeSpan.FromSeconds(2));
                ChoGrid.Visibility = Visibility.Collapsed;
            }
            else if (name == "gongryongbaksa")
            {
                KongGrid.Visibility = Visibility.Visible;
                await Task.Delay(TimeSpan.FromSeconds(2));
                KongGrid.Visibility = Visibility.Collapsed;
            }
            else if (name == "goraebab")
            {
                GoGrid.Visibility = Visibility.Visible;
                await Task.Delay(TimeSpan.FromSeconds(2));
                GoGrid.Visibility = Visibility.Collapsed;
            }
            else if (name == "kancho")
            {
                KanGrid.Visibility = Visibility.Visible;
                await Task.Delay(TimeSpan.FromSeconds(2));
                KanGrid.Visibility = Visibility.Collapsed;
            }
            else if (name == "kanchosweet")
            {
                KanmilGrid.Visibility = Visibility.Visible;
                await Task.Delay(TimeSpan.FromSeconds(2));
                KanmilGrid.Visibility = Visibility.Collapsed;
            }
            DisabledFeedGrid.Visibility = Visibility.Visible;
        }
        /// <summary>
        /// Start the video preview
        /// </summary>
        /// <returns></returns>
        /// 
        private async Task StartPreviewAsync()
        {
            try
            {
                // Try to get the rear camera
                var cameraDevice = await FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel.Back);
                var settings = new MediaCaptureInitializationSettings { VideoDeviceId = cameraDevice.Id };
                // Setup video capture from the camera
                mediaCapture = new MediaCapture();

                await mediaCapture.InitializeAsync(settings);
                displayRequest.RequestActive();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;

                // Set up the FrameReader to capture frames from the camera video
                var frameSource = this.mediaCapture.FrameSources.Where(
                    source => source.Value.Info.SourceKind == MediaFrameSourceKind.Color)
                    .First();
                this.frameReader =
                    await this.mediaCapture.CreateFrameReaderAsync(frameSource.Value);
                // Set up handler for frames
                this.frameReader.FrameArrived += OnFrameArrived;
                // Start the FrameReader
                await this.frameReader.StartAsync();
            }
            catch (UnauthorizedAccessException)
            {
                // Display an error if the user denied access to the camera in privacy settings
                ContentDialog unauthorizedMsg = new ContentDialog()
                {
                    Title = "No access",
                    Content = "The app was denied access to the camera",
                    CloseButtonText = "OK"
                };
                await unauthorizedMsg.ShowAsync();
                return;
            }

            try
            {
                // Wire up the video capture to the CaptureElement to display the video preview                
                VideoPreview.Source = mediaCapture;
                await mediaCapture.StartPreviewAsync();
                isPreviewing = true;
            }
            catch (System.IO.FileLoadException)
            {
                mediaCapture.CaptureDeviceExclusiveControlStatusChanged += MediaCaptureCaptureDeviceExclusiveControlStatusChanged;
            }

        }
        /// <summary>
        /// Handler for exclusive control events for the camera
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void MediaCaptureCaptureDeviceExclusiveControlStatusChanged(MediaCapture sender, MediaCaptureDeviceExclusiveControlStatusChangedEventArgs args)
        {
            if (args.Status == MediaCaptureDeviceExclusiveControlStatus.SharedReadOnlyAvailable)
            {
                ContentDialog accessMsg = new ContentDialog()
                {
                    Title = "No access",
                    Content = "Another app has exclusive access",
                    CloseButtonText = "OK"
                };
            }
            else if (args.Status == MediaCaptureDeviceExclusiveControlStatus.ExclusiveControlAvailable && !isPreviewing)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    await StartPreviewAsync();
                });
            }
        }

        /// <summary>
        /// Clean up the camera access/preview
        /// </summary>
        /// <returns></returns>
        private async Task CleanupCameraAsync()
        {
            if (mediaCapture != null)
            {
                if (isPreviewing)
                {
                    await mediaCapture.StopPreviewAsync();
                }

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    VideoPreview.Source = null;
                    if (displayRequest != null)
                    {
                        displayRequest.RequestRelease();
                    }

                    mediaCapture.Dispose();
                    mediaCapture = null;
                });
            }

        }

        /// <summary>
        /// Handler for application suspend
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ApplicationSuspending(object sender, SuspendingEventArgs e)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                var deferral = e.SuspendingOperation.GetDeferral();
                await CleanupCameraAsync();
                deferral.Complete();
            }
        }

        private void Home_Button_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage));
        }
        private async void pay_Click(object sender, RoutedEventArgs e)
        {
            MessageDialog msg = new MessageDialog("Payment Complete.", "Confirmation");
            await msg.ShowAsync();
            dataList.Clear();
            totalsum = 0;
            total.Text = "0";

        }
        private void SetData(string name)
        {
            if (listView.Items.Count == 0)
            {
                if (name == "chocosongi")
                {
                    SnackData item1 = new SnackData() { Name = "초코송이", Tag = "chocosongi", Qty = 1, Value = 800 };
                    dataList.Add(item1);
                    totalsum += 800;
                    listView.ItemsSource = dataList;
                }
                else if (name == "gongryongbaksa")
                {
                    SnackData item2 = new SnackData() { Name = "공룡박사", Tag = "gongryongbaksa", Qty = 1, Value = 1200 };
                    dataList.Add(item2);
                    totalsum += 1200;
                    listView.ItemsSource = dataList;
                }
                else if (name == "goraebab")
                {
                    SnackData item3 = new SnackData() { Name = "고래밥", Tag = "goraebab", Qty = 1, Value = 900 };
                    dataList.Add(item3);
                    totalsum += 900;
                    listView.ItemsSource = dataList;
                }
                else if (name == "kancho")
                {
                    SnackData item4 = new SnackData() { Name = "칸쵸", Tag = "kancho", Qty = 1, Value = 1000 };
                    dataList.Add(item4);
                    totalsum += 1000;
                    listView.ItemsSource = dataList;
                }
                else if (name == "kanchosweet")
                {
                    SnackData item5 = new SnackData() { Name = "칸쵸스윗밀크", Tag = "kanchosweet", Qty = 1, Value = 1100 };
                    dataList.Add(item5);
                    totalsum += 1100;
                    listView.ItemsSource = dataList;
                }
                total.Text = totalsum.ToString();
            }
            else
            {
                int cho = 0;
                int gong = 0;
                int go = 0;
                int kan = 0;
                int kanmil = 0;
                int j = dataList.Count;
                for (int i = 0; i < j; i++)
                {
                    if (dataList[i].Tag == "chocosongi")
                    {
                        cho++;
                    }
                    else if (dataList[i].Tag == "gongryongbaksa")
                    {
                        gong++;
                    }
                    else if (dataList[i].Tag == "goraebab")
                    {
                        go++;
                    }
                    else if (dataList[i].Tag == "kancho")
                    {
                        kan++;
                    }
                    else if (dataList[i].Tag == "kanchosweet")
                    {
                        kanmil++;
                    }
                }

                int k = dataList.Count;
                for (int i = 0; i < k; i++)
                {
                    if (dataList[i].Tag == name)
                    {
                        dataList[i].Qty++;
                        if (name == "chocosongi")
                        {
                            dataList[i].Value += 800;
                            totalsum += 800;
                        }
                        else if (name == "gongryongbaksa")
                        {
                            dataList[i].Value += 1200;
                            totalsum += 1200;
                        }
                        else if (name == "goraebab")
                        {
                            dataList[i].Value += 900;
                            totalsum += 900;
                        }
                        else if (name == "kancho")
                        {
                            dataList[i].Value += 1000;
                            totalsum += 1000;
                        }
                        else if (name == "kanchosweet")
                        {
                            dataList[i].Value += 1100;
                            totalsum += 1100;
                        }
                        listView.ItemsSource = null;
                        total.Text = totalsum.ToString();
                        listView.ItemsSource = dataList;
                        break;
                    }

                }

                if (cho == 0 && name == "chocosongi")
                {
                    SnackData item1 = new SnackData() { Name = "초코송이", Tag = "chocosongi", Qty = 1, Value = 800 };
                    dataList.Add(item1);
                    totalsum += 800;
                    listView.ItemsSource = dataList;
                    //break;
                }
                else if (gong == 0 && name == "gongryongbaksa")
                {
                    SnackData item2 = new SnackData() { Name = "공룡박사", Tag = "gongryongbaksa", Qty = 1, Value = 1200 };
                    dataList.Add(item2);
                    totalsum += 1200;
                    listView.ItemsSource = dataList;
                    //break;
                }
                else if (go == 0 && name == "goraebab")
                {
                    SnackData item3 = new SnackData() { Name = "고래밥", Tag = "goraebab", Qty = 1, Value = 900 };
                    dataList.Add(item3);
                    totalsum += 900;
                    listView.ItemsSource = dataList;
                    //break;
                }
                else if (kan == 0 && name == "kancho")
                {
                    SnackData item4 = new SnackData() { Name = "칸쵸", Tag = "kancho", Qty = 1, Value = 1000 };
                    dataList.Add(item4);
                    totalsum += 1000;
                    listView.ItemsSource = dataList;
                    //break;
                }
                else if (kanmil == 0 && name == "kanchosweet")
                {
                    SnackData item5 = new SnackData() { Name = "칸쵸스윗밀크", Tag = "kanchosweet", Qty = 1, Value = 1100 };
                    dataList.Add(item5);
                    totalsum += 1100;
                    listView.ItemsSource = dataList;
                    //break;
                }
                total.Text = totalsum.ToString();
            }
        }
    }
}
