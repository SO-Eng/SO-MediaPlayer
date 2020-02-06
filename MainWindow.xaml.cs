using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Windows.Threading;

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

        // Timer (Ticker) um aktuelle Zeit wiederzuegeben an Label
        DispatcherTimer timer = new DispatcherTimer();


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
                mediaPlayer.Source = new Uri(openDialog.FileName);
                ImagePlay();
                mediaPlayer.Stop();
                playing = false;
                buttonPlayPause.IsEnabled = true;
                buttonBackwards.IsEnabled = true;
                buttonForwards.IsEnabled = true;
                imagePlay.Opacity = 1;
                imageBackward.Opacity = 1;
                imageForward.Opacity = 1;
                labelFileName.Content = openDialog.FileName;
                // Timer (Ticker) starten
                timer.Tick += TimerTick;
                timer.Start();
            }
        }


        // Wenn MediaFile geladen, Gesamtzeit anzeigen
        private void MediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                labelMaxTime.Content = mediaPlayer.NaturalDuration.TimeSpan.ToString(@"hh\:mm\:ss");
                if (mediaPlayer.HasVideo)
                {
                    imageAudio.Source = null;
                    return;
                }
                if (mediaPlayer.HasAudio)
                {
                    // Photo by Marcela Laskoski(https://unsplash.com/@marcelalaskoski?utm_source=unsplash&utm_medium=referral&utm_content=creditCopyText) on Unsplash(https://unsplash.com)
                    imageAudio.Source = new BitmapImage(new Uri("icons/audio.jpg", UriKind.Relative));
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
            if (mediaPlayer != null)
            {
                if (mediaPlayer.NaturalDuration.HasTimeSpan)
                {
                    labelCurrentTime.Content = mediaPlayer.Position.ToString(@"hh\:mm\:ss");
                }
            }
            else
            {
                labelCurrentTime.Content = "00:00:00";
            }
        }


        // Mediendatei abspielen || pausieren
        private void ButtonPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (!playing)
            {
                mediaPlayer.Play();
                ImagePause();
                playing = true;
            }
            else
            {
                mediaPlayer.Pause();
                ImagePlay();
                playing = false;
            }
            // Button Stoppen aktivieren
            buttonStop.IsEnabled = true;
            imageStop.Opacity = 1;
        }

        // Mediendatei stoppen
        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            ImagePlay();
            mediaPlayer.Stop();
            imageStop.Opacity = 0.5;
            buttonStop.IsEnabled = false;
            playing = false;
        }


        // Bilder tauschen (Play || Pause)
        void ImagePlay()
        {
            imagePlay.Source = new BitmapImage(new Uri("icons/play.png", UriKind.Relative));
            buttonPlayPause.ToolTip = "Wiedergeben";
        }

        void ImagePause()
        {
            imagePlay.Source = new BitmapImage(new Uri("icons/pause.png", UriKind.Relative));
            buttonPlayPause.ToolTip = "Pausieren";
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
                mediaPlayer.Volume = mediaPlayer.Volume + 0.1;
            }
            else
            {
                // Nach hinten reduziert
                mediaPlayer.Volume = mediaPlayer.Volume - 0.1;
            }
        }


        // 10 Sekunden vorwaerts springen
        private void ButtonForwards_Click(object sender, RoutedEventArgs e)
        {
            if (!playing)
            {
                return;
            }
            if ((mediaPlayer.NaturalDuration.TimeSpan - mediaPlayer.Position) <= TimeSpan.FromSeconds(10))
            {
                mediaPlayer.Position = mediaPlayer.NaturalDuration.TimeSpan;
                ButtonStop_Click(sender, e);
            }
            else
            {
                mediaPlayer.Position += TimeSpan.FromSeconds(10);
            }
        }

        // 10 sekunden zurueck springen
        private void ButtonBackwards_Click(object sender, RoutedEventArgs e)
        {
            if (!playing)
            {
                return;
            }
            if (mediaPlayer.NaturalDuration.TimeSpan <= TimeSpan.FromSeconds(10))
            {
                mediaPlayer.Position = TimeSpan.Zero;
            }
            else
            {
                mediaPlayer.Position -= TimeSpan.FromSeconds(10);
            }
        }


        // ProgressBar auf MousLeftButtonDown reagieren lassen und an geklickte stelle den Value setzen.
        private void ProgressVolume_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            double mousePosition = e.GetPosition(progressVolume).X;
            progressVolume.Value = SetProgressBarValue(mousePosition);
        }

        private double SetProgressBarValue(double mP)
        {
            double ratio = mP / progressVolume.ActualWidth;
            double progressBarValue = ratio * progressVolume.Maximum;
            return progressBarValue;
        }
    }
}
