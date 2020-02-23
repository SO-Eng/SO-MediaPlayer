using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace SO_Mediaplayer
{
    /// <summary>
    /// Interaktionslogik für AddRadiostation.xaml
    /// </summary>
    public partial class AddRadiostation : Window
    {
        #region Fields

        private string labelDescrContent = string.Empty;
        // Vorhoerfunktion
        private bool isPlaying = false;
        private bool exception = false;

        public string StationName { get; set; }
        public string StationUrl { get; set; }
        public string BitRate { get; set; }
        public bool Favorite { get; set; }

        private readonly SetLanguages Sl;

        #endregion


        #region Methods
        public AddRadiostation(string lang)
        {
            InitializeComponent();
            Sl = new SetLanguages(lang);

            LabelDescrContent();

            TextBoxStationName.Focus();
        }

        private void LabelDescrContent()
        {
            labelDescrContent = Sl.AddStationInfo;
            TextBlockDescr.Text = labelDescrContent;
        }

        private void ButtonPrelisten_Click(object sender, RoutedEventArgs e)
        {
            exception = false;
            if (!isPlaying)
            {
                try
                {
                    MediaPlayerListen.Source = new Uri(TextBoxUrl.Text);
                }
                catch (Exception)
                {
                    MessageBox.Show(Sl.ErrorPlay, Sl.MsgBoxInfo, MessageBoxButton.OK, MessageBoxImage.Information);
                    exception = true;
                    return;
                }
                MediaPlayerListen.Play();
                ButtonPrelisten.Content = Sl.Stop;
                isPlaying = true;
            }
            else
            {
                MediaPlayerListen.Source = null;
                MediaPlayerListen.Stop();
                ButtonPrelisten.Content = Sl.Prelisten;
                isPlaying = false;
                if (!exception && TextBoxStationName.Text != string.Empty && TextBoxUrl.Text != string.Empty)
                {
                    ButtonAdd.IsEnabled = true;
                }
            }

        }

        // Button Hinzufuegen Methode
        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            StationName = TextBoxStationName.Text;
            StationUrl = TextBoxUrl.Text;

            if (TextBoxBitrate.Text != string.Empty)
            {
                BitRate = TextBoxBitrate.Text;
            }
            else
            {
                BitRate = "0";
            }

            if (CheckBoxFav.IsChecked == true)
            {
                Favorite = true;
            }
            else
            {
                Favorite = false;
            }

            DialogResult = true;
        }

        // Nur Zahlen in Textbox fuer Bitrate zulassen
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        #endregion
    }
}
