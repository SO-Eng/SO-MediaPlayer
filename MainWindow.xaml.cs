using System;
using System.Net;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;
using System.IO;
using System.Text;

/// <summary>
/// Icon graphic is free comercial use of license and has to be announced:
/// "Icon made by UI Super Basic perfect from www.flaticon.com"
/// https://www.flaticon.com/free-icon/headphone_1053254?term=headphones&page=2&position=51
/// </summary>
namespace Mediaplayer_ILS
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        // Fields
        bool playing = false;
        private string sPath = string.Empty;

        // Timer (Ticker) um aktuelle Zeit wiederzuegeben an Label
        readonly DispatcherTimer timer = new DispatcherTimer();


        public MainWindow()
        {
            InitializeComponent();

            // Timerintervall setzen
            timer.Interval = TimeSpan.FromMilliseconds(250);
        }

        private void ButtonOpen_Click(object sender, RoutedEventArgs e)
        {
            // den Dialog erzeugen
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.FileName = string.Empty;
            if (openDialog.ShowDialog() == true)
            {
                MediaPlayer.Source = new Uri(openDialog.FileName);
                ImagePlay();
                MediaPlayer.Stop();
                playing = false;
                PlayRoutine();
                LabelFileName.Content = openDialog.FileName;
            }
        }

        private void PlayRoutine()
        {
            ButtonPlayPause.IsEnabled = true;
            ButtonBackwards.IsEnabled = true;
            ButtonForwards.IsEnabled = true;
            ImagePlayPic.Opacity = 0.85;
            ImageBackwardPic.Opacity = 0.85;
            ImageForwardPic.Opacity = 0.85;
            // Timer (Ticker) starten
            timer.Tick += TimerTick;
            timer.Start();
        }

        // Open Folder Dialog
        private void ButtonOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            WinForms.FolderBrowserDialog folderDialog = new WinForms.FolderBrowserDialog();
            folderDialog.ShowNewFolderButton = false;
            folderDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            WinForms.DialogResult result = folderDialog.ShowDialog();

            if (result == WinForms.DialogResult.OK)
            {
                sPath = folderDialog.SelectedPath;
                
                DirectoryInfo folder = new DirectoryInfo(sPath);

                if (folder.Exists)
                {
                    ListSelection.Items.Clear();

                    foreach (var fileInfo in folder.GetFiles())
                    {
                        ListSelection.Items.Add(fileInfo);
                    }
                }
            }
        }


        // Wenn MediaFile geladen, Gesamtzeit anzeigen
        private void MediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                LabelMaxTime.Content = MediaPlayer.NaturalDuration.TimeSpan.ToString(@"hh\:mm\:ss");
                if (MediaPlayer.HasVideo)
                {
                    ImageAudio.Source = null;
                    playing = false;
                    ButtonPlayPause_Click(sender, e);
                    return;
                }
                if (MediaPlayer.HasAudio)
                {
                    // Photo by Marcela Laskoski(https://unsplash.com/@marcelalaskoski?utm_source=unsplash&utm_medium=referral&utm_content=creditCopyText) on Unsplash(https://unsplash.com)
                    ImageAudio.Source = new BitmapImage(new Uri("musicBackground/audio.jpg", UriKind.Relative));
                    playing = false;
                    ButtonPlayPause_Click(sender, e);
                    MediaPlayer.Play();
                }
            }
            catch
            {
                MessageBox.Show("Try only load Audio or Video files", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }


        // Aktuelle Spieldauer an Label uebergeben mit Hilfe des Tick Timers
        private void TimerTick(object sender, EventArgs e)
        {
            if (MediaPlayer != null)
            {
                if (MediaPlayer.NaturalDuration.HasTimeSpan)
                {
                    LabelCurrentTime.Content = MediaPlayer.Position.ToString(@"hh\:mm\:ss");
                }
            }
            else
            {
                LabelCurrentTime.Content = "00:00:00";
            }
        }


        // Mediendatei abspielen || pausieren
        private void ButtonPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (!playing)
            {
                MediaPlayer.Play();
                ImagePause();
                playing = true;
            }
            else
            {
                MediaPlayer.Pause();
                ImagePlay();
                playing = false;
            }
            // Button Stoppen aktivieren
            ButtonStop.IsEnabled = true;
            ImageStopPic.Opacity = 0.85;
        }

        // Mediendatei stoppen
        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            ImagePlay();
            MediaPlayer.Stop();
            ImageStopPic.Opacity = 0.5;
            ButtonStop.IsEnabled = false;
            playing = false;
        }


        // Bilder tauschen (Play || Pause)
        void ImagePlay()
        {
            ImagePlayPic.Source = new BitmapImage(new Uri("icons/play.png", UriKind.Relative));
            ButtonPlayPause.ToolTip = "Wiedergeben";
        }

        void ImagePause()
        {
            ImagePlayPic.Source = new BitmapImage(new Uri("icons/pause.png", UriKind.Relative));
            ButtonPlayPause.ToolTip = "Pausieren";
        }


        // Mediafile per Spacetaste und Pfeiltasten (abspielen && pausieren) || 10sek springen(vor || zurueck)
        private void WindowMediaPLayer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                ButtonPlayPause_Click(sender, e);
            }
            else if (e.Key == Key.Right)
            {
                ButtonForwards_Click(sender, e);
            }
            else if (e.Key == Key.Left)
            {
                ButtonBackwards_Click(sender, e);
            }
        }


        // Lautstaerke per Mausrad aendern
        private void WindowMediaPLayer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // In welche Richtung wurde das Mausrad gedreht?
            if (e.Delta > 0)
            {
                // Nach vorne erhöhen wir die Lautstärke
                ProgressVolume.Value += 0.05;
                SoundBoxVolume();
            }
            else
            {
                // Nach hinten reduziert
                ProgressVolume.Value -= 0.05;
                SoundBoxVolume();
            }
        }

        private void SoundBoxVolume()
        {
            if (MediaPlayer.Volume <= 0)
            {
                SoundBoxPic.Source = new BitmapImage(new Uri("soundVol/audio-volume-muted.png", UriKind.Relative));
            }
            else if (MediaPlayer.Volume > 0 && MediaPlayer.Volume <= 0.33)
            {
                SoundBoxPic.Source = new BitmapImage(new Uri("soundVol/audio-volume-low.png", UriKind.Relative));
            }
            else if (MediaPlayer.Volume > 0.33 && MediaPlayer.Volume <= 0.66)
            {
                SoundBoxPic.Source = new BitmapImage(new Uri("soundVol/audio-volume-medium.png", UriKind.Relative));
            }
            else
            {
                SoundBoxPic.Source = new BitmapImage(new Uri("soundVol/audio-volume-high.png", UriKind.Relative));
            }
        }

        // 10 Sekunden vorwaerts springen
        private void ButtonForwards_Click(object sender, RoutedEventArgs e)
        {
            if (!playing)
            {
                return;
            }
            if ((MediaPlayer.NaturalDuration.TimeSpan - MediaPlayer.Position) <= TimeSpan.FromSeconds(10))
            {
                MediaPlayer.Position = MediaPlayer.NaturalDuration.TimeSpan;
                ButtonStop_Click(sender, e);
            }
            else
            {
                MediaPlayer.Position += TimeSpan.FromSeconds(10);
            }
        }

        // 10 sekunden zurueck springen
        private void ButtonBackwards_Click(object sender, RoutedEventArgs e)
        {
            if (!playing)
            {
                return;
            }
            if (MediaPlayer.NaturalDuration.TimeSpan <= TimeSpan.FromSeconds(10))
            {
                MediaPlayer.Position = TimeSpan.Zero;
            }
            else
            {
                MediaPlayer.Position -= TimeSpan.FromSeconds(10);
            }
        }


        // ProgressBar auf MousLeftButtonDown reagieren lassen und an geklickte stelle den Value setzen.
        private void ProgressVolume_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            double mousePosition = e.GetPosition(ProgressVolume).X;
            ProgressVolume.Value = SetProgressBarValue(mousePosition);
        }

        private double SetProgressBarValue(double mP)
        {
            double ratio = mP / ProgressVolume.ActualWidth;
            double progressBarValue = ratio * ProgressVolume.Maximum;
            return progressBarValue;
        }

        // Check ob Internetconnection vorhanden
        private bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead("http://google.com/generate_204"))
                    return true;
            }
            catch
            {
                return false;
            }
        }

        private void WindowMediaPLayer_Loaded(object sender, RoutedEventArgs e)
        {
            if (CheckForInternetConnection() == true)
            {
                InternetConPic.Source = new BitmapImage(new Uri("internetCon/network-connect-3.png", UriKind.Relative));
            }
        }

        private void ListSelection_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ListSelection.SelectedItem == null)
            {
                return;
            }
            else
            {
                string selection = ListSelection.SelectedItem.ToString();
                StringBuilder sB = new StringBuilder(sPath);
                sB.Append(@"\");
                sB.Append(selection);
                MediaPlayer.Source = new Uri(sB.ToString());
                PlayRoutine();
                playing = false;
                ButtonPlayPause_Click(sender, e);
                ListSelection.SelectedItem = null;
            }
        }

    }
}
