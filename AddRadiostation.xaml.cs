using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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

        #endregion


        #region Methods
        public AddRadiostation()
        {
            InitializeComponent();
            LabelDescrContent();

            TextBoxStationName.Focus();
        }

        private void LabelDescrContent()
        {
            labelDescrContent = "Fügen Sie Ihren Lieblingssender zur Liste hinzu.\nWenn Sie die Liste speichern, wird der Sender Ihnen auch in Zukuft zur verfügung stehen!";
            TextBlockDescr.Text = labelDescrContent;
        }

        #endregion

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
                    MessageBox.Show("Diese WebStation scheint leider nicht erreichbar zu sein...", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    exception = true;
                    return;
                }
                MediaPlayerListen.Play();
                ButtonPrelisten.Content = "Stop";
                isPlaying = true;
            }
            else
            {
                MediaPlayerListen.Source = null;
                MediaPlayerListen.Stop();
                ButtonPrelisten.Content = "Vorhören";
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
    }
}
