using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace VisionWinML
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RegisterName : Page
    {

        public RegisterName()
        {
            this.InitializeComponent();
        }

        private async void AppBarButtonAddPerson_Click(object sender, RoutedEventArgs e)
        {
                if (txtPerson.Text.Trim() != "" && txtPerson.Text != "...")
                {

                    string response = await PersonsCmds.CreatePerson(globals.gPersonGroupSelected.personGroupId,// txtPerson.Text.ToLower().Replace(' ', '_'),
                                                                    txtPerson.Text, null);

                    ////////////////////////////////////////////////////////////////
                    //Junghwan 181023
                    //person 이름 신규로 입력 시 바로 해당 face 페이지로 넘어감 
                    List<Persons> persons = await PersonsCmds.ListPersonInGroup(globals.gPersonGroupSelected.personGroupId);
                    globals.gPersonsList = persons;
                    int count = globals.gPersonsList.Count();
                    int number = 0;
                    for (int i = 0; i < count; i++)
                    {
                        if (globals.gPersonsList[i].name == txtPerson.Text)
                        {
                            number = i;
                            break;
                        }
                    }
                    Persons person = globals.gPersonsList[number];
                    globals.gPersonSelected = person;
                    globals.gFaceSelected = null;
                    Frame.Navigate(typeof(RegisterFace));
                    ////////////////////////////////////////////////////////////////
                }
                else
                {
                }
        }

        private void Home_Button_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage));
        }
    }
}
