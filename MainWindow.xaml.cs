using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Mediaplayer_ILS.Models;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WinForms = System.Windows.Forms;

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
        #region Fields
        // Player spielt
        bool playing = false;
        // fuer HotKey-absicherung
        private bool fileLoaded = false;
        // Pfad zum Ordner oeffnen
        private string sPath = string.Empty;
        // Datei zum importieren (Pfad)
        private string webStationFile = string.Empty;
        // Order oder Datei geoeffnet, wenn false dann WebListe
        private bool folderSelectoin = false;
        // zwischenspeichern (Stop/Play)
        private string tempSelectionWeb;

        private int favsExisting = 0;
        // Maximale Spielzeit der geladenen Dateien ----noch offen
        private string playtime;

        private dynamic selectedItemWeb;
        private dynamic selectionWeb;

        // Spielzeit fuer WebRadio
        DateTime startTime;
        DateTime diff;

        // StandardListe laden oder oeffnen
        private bool checkBox = true;

        // Timer (Ticker) um aktuelle Zeit wiederzuegeben an Label
        readonly DispatcherTimer timer = new DispatcherTimer();
        readonly DispatcherTimer timerWeb = new DispatcherTimer();

        readonly List<WebStations> webStationList = new List<WebStations>();
        readonly List<WebFavs> webFavList = new List<WebFavs>();


        #endregion

        public MainWindow()
        {
            InitializeComponent();

            // Timerintervall setzen
            timer.Interval = TimeSpan.FromMilliseconds(250);
            timerWeb.Interval = TimeSpan.FromSeconds(1);
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
                folderSelectoin = true;
                MediaPlayer.Stop();
                playing = false;
                PlayRoutine();
                LabelFileName.Content = openDialog.FileName;
                ChkBoxSaveOnExit.IsEnabled = false;
                ChkBoxSaveOnExit.IsChecked = false;
            }
        }

        private void ButtonOpenWeb_Click(object sender, RoutedEventArgs e)
        {
            if (webStationList != null)
            {
                webStationList.Clear();
            }
            if (!checkBox)
            {
                OpenFileDialog openWebDialog = new OpenFileDialog();
                openWebDialog.FileName = string.Empty;
                openWebDialog.Multiselect = false;
                //openWebDialog.Filter = "";
                if (openWebDialog.ShowDialog() == true)
                {
                    ListSelectionFolder.Visibility = Visibility.Collapsed;
                    ListSelectionWeb.Visibility = Visibility.Visible;
                    ListSelectionWebFav.Visibility = Visibility.Visible;
                    GridSplitterWebLists.Visibility = Visibility.Visible;
                    StackPanelSearch.Visibility = Visibility.Visible;
                    webStationFile = openWebDialog.FileName;
                    ListSelectionWeb.Items.Clear();
                    folderSelectoin = false;
                    WebStationsStorage();
                }
            }
            else
            {
                ListSelectionWebFav.Items.Clear();
                ListSelectionWeb.Items.Clear();
                ListSelectionFolder.Visibility = Visibility.Collapsed;
                ListSelectionWeb.Visibility = Visibility.Visible;
                ListSelectionWebFav.Visibility = Visibility.Visible;
                GridSplitterWebLists.Visibility = Visibility.Visible;
                StackPanelSearch.Visibility = Visibility.Visible;
                webStationFile = AppDomain.CurrentDomain.BaseDirectory + @"RadioStations\RadioStation-List02.csv";
                ListSelectionWeb.Items.Clear();
                ListSelectionWebFav.Items.Clear();
                folderSelectoin = false;
                WebStationsStorage();
            }

        }

        private void WebStationsStorage()
        {

            var newWebStationses = WebFileProcessor.WebFileProcessor.LoadFromTextFile<WebStations>(webStationFile);
            //var newWebStationsesFav = WebFileProcessor.WebFileProcessor.LoadFromTextFile<WebFavs>(webStationFile);

            foreach (var webStation in newWebStationses)
            {
                ListSelectionWeb.Items.Add(new WebStations { StationFav = webStation.StationFav, StationName = webStation.StationName, BitRate = webStation.BitRate, StationUrl = webStation.StationUrl});
                webStationList.Add( new WebStations { StationName = webStation.StationName, BitRate = webStation.BitRate, StationUrl = webStation.StationUrl, StationFav = webStation.StationFav } );
                if (webStation.StationFav == true)
                {
                    ListSelectionWebFav.Items.Add(new WebStations { StationFav = webStation.StationFav, StationName = webStation.StationName, BitRate = webStation.BitRate, StationUrl = webStation.StationUrl });
                }
            }
            // Datagrid of FavoriteList adjust to height (max 200 in XAML)
            FavListHeight.Height = new GridLength((ListSelectionWebFav.Items.Count * 24) + 45);
            ChkBoxSaveOnExit.IsEnabled = true;
            ChkBoxSaveOnExit.IsChecked = true;
        }

        private void WindowMediaPLayer_Closing(object sender, CancelEventArgs e)
        {
            if (CheckForInternetConnection())
            {
                WebFileProcessor.WebFileProcessor.SaveToTextFile(webStationList, webStationFile);
            }
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
                ListSelectionWeb.Visibility = Visibility.Collapsed;
                ListSelectionWebFav.Visibility = Visibility.Collapsed;
                GridSplitterWebLists.Visibility = Visibility.Collapsed;
                StackPanelSearch.Visibility = Visibility.Collapsed;
                ListSelectionFolder.Visibility = Visibility.Visible;
                // GridView vorbereiten
                //this.ListSelection.View = gridView;

                ChkBoxSaveOnExit.IsEnabled = false;
                ChkBoxSaveOnExit.IsChecked = false;

                // Pfad uebergeben
                sPath = folderDialog.SelectedPath;
                DirectoryInfo folder = new DirectoryInfo(sPath);
                int i = 1;
                if (folder.Exists)
                {
                    ListSelectionFolder.Items.Clear();
                    folderSelectoin = true;
                    foreach (var fileInfo in folder.GetFiles())
                    {
                        playtime = "00:00:00";
                        ListSelectionFolder.Items.Add(new FolderPick { Number = i.ToString(), FileName = fileInfo.ToString(), PlayTime = playtime });
                        i++;
                    }
                }
            }
        }

        // Abspielroutine nach File oder Folder oeffnen
        private void PlayRoutine()
        {
            ButtonPlayPause.IsEnabled = true;
            ButtonBackwards.IsEnabled = true;
            ButtonForwards.IsEnabled = true;
            ImagePlayPic.Opacity = 0.85;
            ImageBackwardPic.Opacity = 0.85;
            ImageForwardPic.Opacity = 0.85;
            fileLoaded = true;
            // Timer (Ticker) starten
            timer.Tick += TimerTick;
            timer.Start();
        }

        private void PlayRoutineWeb()
        {
            ButtonPlayPause.IsEnabled = true;
            ImagePlayPic.Opacity = 0.85;
            fileLoaded = true;
            startTime = DateTime.Now;
            LabelMaxTime.Content = "--:--:--";
            // Timer (Ticker) starten
            timerWeb.Tick += TimerWebTick;
            timerWeb.Start();
        }



        // Wenn MediaFile geladen, Gesamtzeit anzeigen
        private void MediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (!folderSelectoin)
            {
                ImageAudio.Source = new BitmapImage(new Uri("musicBackground/audio.jpg", UriKind.Relative));
                playing = false;
                ButtonPlayPause_Click(sender, e);
                MediaPlayer.Play();
                return;
            }
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

        private void TimerWebTick(object sender, EventArgs e)
        {
            diff = DateTime.Now;
            string seconds = (diff - startTime).ToString(@"hh\:mm\:ss");
            LabelCurrentTime.Content = seconds;
        }


        // Mediendatei abspielen || pausieren
        private void ButtonPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (!playing)
            {
                if (folderSelectoin)
                {
                    MediaPlayer.Play();
                    ImagePause();
                    playing = true;
                }
                else // Webradio
                {
                    MediaPlayer.Source = new Uri(tempSelectionWeb);
                    MediaPlayer.Play();
                    ImagePause();
                    playing = true;
                    timerWeb.Start();
                }
            }
            else
            {
                if (folderSelectoin)
                {
                    MediaPlayer.Pause();
                    ImagePlay();
                    playing = false;
                }
                else
                {
                    MediaPlayer.Pause();
                    ImagePlay();
                    playing = false;
                    timerWeb.Stop();
                }
            }
            // Button Stoppen aktivieren
            ButtonStop.IsEnabled = true;
            ImageStopPic.Opacity = 0.85;
        }

        // Mediendatei stoppen
        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            if (folderSelectoin)
            {
                ImagePlay();
                MediaPlayer.Stop();
                ImageStopPic.Opacity = 0.5;
                ButtonStop.IsEnabled = false;
                playing = false;
            }
            // WebRadio has to deload Source
            else
            {
                ImagePlay();
                MediaPlayer.Stop();
                MediaPlayer.Source = null;
                ImageStopPic.Opacity = 0.5;
                ButtonStop.IsEnabled = false;
                playing = false;
                timerWeb.Stop();
            }
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
            if (!fileLoaded)
            {
                return;
            }
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


        private void ListIdentifier(object sender, SelectionChangedEventArgs e)
        {
            if (ListSelectionWebFav.IsMouseOver)
            {
                ListSelection_SelectionChanged(sender, e, 1);
            }
            else
            {
                ListSelection_SelectionChanged(sender, e, 0);
            }
        }

        private void ListSelection_SelectionChanged(object sender, SelectionChangedEventArgs e, int identifier)
        {
            if (((ListSelectionWeb.Visibility == Visibility.Visible && ListSelectionWeb.SelectedItem == null) && (ListSelectionWebFav.Visibility == Visibility.Visible && ListSelectionWebFav.SelectedItem == null)) || (ListSelectionFolder.Visibility == Visibility.Visible && ListSelectionFolder.SelectedItem == null))
            {
                return;
            }
            else
            {
                if (folderSelectoin)
                {
                    dynamic selectedItemFolder = ListSelectionFolder.SelectedItems[0];
                    var selectionFolder = selectedItemFolder.FileName;

                    //string selection = ListSelectionFolder.SelectedItem.ToString();
                    StringBuilder sB = new StringBuilder(sPath);
                    sB.Append(@"\");
                    sB.Append(selectionFolder);
                    MediaPlayer.Source = new Uri(sB.ToString());
                    PlayRoutine();
                    playing = false;
                    ButtonPlayPause_Click(sender, e);
                    LabelFileName.Content = sB.ToString();
                }
                else if (!folderSelectoin)
                {
                    if (identifier == 1)
                    {
                        selectedItemWeb = ListSelectionWebFav.SelectedItems[0];
                    }
                    else
                    {
                        selectedItemWeb = ListSelectionWeb.SelectedItems[0];
                    }
                    // String bauen
                    selectionWeb = selectedItemWeb.StationUrl;
                    tempSelectionWeb = selectionWeb;
                    try
                    {
                        MediaPlayer.Source = new Uri(selectionWeb);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Diese WebStation scheint leider nicht erreichbar zu sein...", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    PlayRoutineWeb();
                    playing = false;
                    ButtonPlayPause_Click(sender, e);
                    LabelFileName.Content = selectionWeb.ToString();
                }
            }
        }

        // Hotkeys weiterleiten
        private void ListSelection_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            HotkeysForword(sender, e);
        }

        private void GridSplitter_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            HotkeysForword(sender, e);
        }

        private void HotkeysForword(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                case Key.Right:
                //case Key.Up:
                //case Key.Down:
                case Key.Space:
                    e.Handled = true;
                    WindowMediaPLayer_KeyDown(sender, e);
                    break;
                default:
                    break;
            }
        }

        private void CheckBoxList_Click(object sender, RoutedEventArgs e)
        {
            dynamic selectedItemWeb = ListSelectionWeb.SelectedItems[0];
            var selectedName = selectedItemWeb.StationName;
            var selectedFav = selectedItemWeb.StationFav;

            if (selectedFav == false)
            {
                webStationList.First(f => f.StationName == selectedName).StationFav = Convert.ToBoolean("False");
                ListSelectionWebFav.Items.Clear();
                foreach (var station in webStationList)
                {
                    if (station.StationFav == true)
                    {
                        ListSelectionWebFav.Items.Add(new WebStations { StationFav = station.StationFav, StationName = station.StationName, BitRate = station.BitRate, StationUrl = station.StationUrl });
                    }
                }
            }
            else
            {
                webStationList.First(f => f.StationName == selectedName).StationFav = Convert.ToBoolean("True");
                ListSelectionWebFav.Items.Clear();
                foreach (var station in webStationList)
                {
                    if (station.StationFav == true)
                    {
                        ListSelectionWebFav.Items.Add(new WebStations { StationFav = station.StationFav, StationName = station.StationName, BitRate = station.BitRate, StationUrl = station.StationUrl });
                    }
                }
            }
            FavListHeight.Height = new GridLength((ListSelectionWebFav.Items.Count * 24) + 45);

        }

        private void CheckBoxFav_Click(object sender, RoutedEventArgs e)
        {
            dynamic selectedItemWeb = ListSelectionWebFav.SelectedItems[0];
            var selectedName = selectedItemWeb.StationName;
            var selectedFav = selectedItemWeb.StationFav;

            if (selectedFav == false)
            {
                webStationList.First(f => f.StationName == selectedName).StationFav = Convert.ToBoolean("False");
                ListSelectionWeb.Items.Clear();
                foreach (var station in webStationList)
                {
                    ListSelectionWeb.Items.Add(new WebStations { StationName = station.StationName, BitRate = station.BitRate, StationUrl = station.StationUrl, StationFav = station.StationFav });
                }
            }
            else
            {
                webStationList.First(f => f.StationName == selectedName).StationFav = Convert.ToBoolean("True");
                ListSelectionWeb.Items.Clear();
                foreach (var station in webStationList)
                {
                    ListSelectionWeb.Items.Add(new WebStations { StationName = station.StationName, BitRate = station.BitRate, StationUrl = station.StationUrl, StationFav = station.StationFav });
                }
            }
        }
    }




    internal class WebFavs
    {
        public string StationNameFav { get; set; }
        public string BitRateFav { get; set; }
        public string StationUrlFav { get; set; }
        public bool StationFavFav { get; set; }
    }
}
