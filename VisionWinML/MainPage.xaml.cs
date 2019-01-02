using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace VisionWinML
{
    public static class globals
    {
        public static PersonGroups gPersonGroupSelected { get; set; }
        public static Persons gPersonSelected { get; set; }
        public static FaceData gFaceSelected { get; set; }
        public static List<PersonGroups> gPersonGroupList { get; set; }
        ////////////////////////////////////////////////////////////////
        //Jungwhan 181023
        public static List<Persons> gPersonsList { get; set; }
        ////////////////////////////////////////////////////////////////
        public static async void ShowJsonErrorPopup(string responseBody)
        {
            if (null != responseBody)
            {
                ResponseObject errorObject = JsonConvert.DeserializeObject<ResponseObject>(responseBody);
                MessageDialog dialog = new MessageDialog(errorObject.error.message,
                                                                 (null != errorObject.error.code) ?
                                                                        errorObject.error.code.ToString() :
                                                                        errorObject.error.statusCode.ToString());
                await dialog.ShowAsync();
            }
            else
            {
                MessageDialog dialog = new MessageDialog("Unknown error in operation");
                await dialog.ShowAsync();
            }
        }
    }
    public class Error
    {
        public string code { get; set; }
        public int statusCode { get; set; }
        public string message { get; set; }
    }

    public class ResponseObject
    {
        public Error error { get; set; }
    }
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();            
            HttpHandler.init();
        }
        private void LOGIN_Button_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(LoginPage));
        }
        private void GUEST_Button_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(VisionPos));
        }

        private async void REGISTER_Button_Click(object sender, RoutedEventArgs e)
        {
            List<PersonGroups> personGroups = await PersonGroupCmds.ListPersonGroups();
            globals.gPersonGroupList = personGroups;
            PersonGroups personGroup = globals.gPersonGroupList[1];
            globals.gPersonGroupSelected = personGroup;
            globals.gPersonSelected = null;
            this.Frame.Navigate(typeof(RegisterName));
        }
    }
}
