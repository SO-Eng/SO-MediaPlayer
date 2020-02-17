using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SO_Mediaplayer.Models;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WinForms = System.Windows.Forms;

/// <summary>
/// Icon graphic is free comercial use of license and has to be announced:
/// "Icon made by UI Super Basic perfect from www.flaticon.com"
/// https://www.flaticon.com/free-icon/headphone_1053254?term=headphones&page=2&position=51
/// </summary>
namespace SO_Mediaplayer
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
        private string sPath = String.Empty;
        // Datei zum importieren (Pfad)
        private string webStationFile = String.Empty;
        // Order oder Datei geoeffnet, wenn false dann WebListe
        private bool folderSelection = true;
        // zwischenspeichern (Stop/Play)
        private string tempSelectionWeb;
        // Maximale Spielzeit der geladenen Dateien ----noch offen
        private string playtime;

        // Progressbar Steuerung
        private bool sliderMoving;

        // Hilfsattribute fuer die Listenauswahl (click)
        private dynamic selectedItemWeb;
        private dynamic selectionWeb;

        // Bool fuer Ansichten
        private bool folderSelected = true;
        private bool favListSelected = true;
        private bool webListSelected = true;
        private bool firstLoad;
        //private double columnListMinWidth;
        private double favListMinHeight;
        private GridLength columnList;
        private GridLength columnSplitter;
        private GridLength rowFavList;

        // Spielzeit fuer WebRadio
        DateTime startTime;
        DateTime diff;

        // StandardListe laden oder oeffnen
        private bool checkBox = true;

        // Timer (Ticker) um aktuelle Zeit wiederzuegeben an Label
        readonly DispatcherTimer timer = new DispatcherTimer();
        readonly DispatcherTimer timerWeb = new DispatcherTimer();

        // Liste fuer die Radiostaionen
        readonly List<WebStations> webStationList = new List<WebStations>();
        readonly List<WebFavs> webFavList = new List<WebFavs>();

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            // Timerintervall setzen
            timer.Interval = TimeSpan.FromMilliseconds(250);
            timerWeb.Interval = TimeSpan.FromSeconds(1);

            ViewSettings();

            firstLoad = true;
        }

        // Einstellungen fuer Ansichtsmenue
        private void ViewSettings()
        {
            if (folderSelection)
            {
                FolderListMenu.IsEnabled = true;
                FavListMenu.IsEnabled = false;
                SearchboxMenu.IsEnabled = false;
                WebListMenu.IsEnabled = false;
            }
            else
            {
                FolderListMenu.IsEnabled = false;
                FavListMenu.IsEnabled = true;
                SearchboxMenu.IsEnabled = true;
                WebListMenu.IsEnabled = true;
            }
        }


        // einzelne Datei auswaehlen
        private void ButtonOpen_Click(object sender, RoutedEventArgs e)
        {
            // den Dialog erzeugen
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.FileName = String.Empty;
            if (openDialog.ShowDialog() == true)
            {
                ListSelectionWeb.Visibility = Visibility.Collapsed;
                ListSelectionWebFav.Visibility = Visibility.Collapsed;
                GridSplitterWebLists.Visibility = Visibility.Collapsed;
                StackPanelSearch.Visibility = Visibility.Collapsed;
                TextBlockFavListHeader.Visibility = Visibility.Collapsed;
                ListSelectionFolder.Visibility = Visibility.Visible;
                FolderListMenu_Click(sender, e);
                MediaPlayer.Source = new Uri(openDialog.FileName);
                ImagePlay();
                folderSelection = true;
                MediaPlayer.Stop();
                playing = false;
                PlayRoutine();
                LabelFileName.Content = openDialog.FileName;
                ChkBoxSaveOnExit.IsEnabled = false;
                ChkBoxSaveOnExit.IsChecked = false;
                AddWebStationMenu.IsEnabled = false;
                ViewSettings();
            }
        }

        // Radiostationen laden
        private void ButtonOpenWeb_Click(object sender, RoutedEventArgs e)
        {
            if (webStationList != null)
            {
                webStationList.Clear();
            }
            if (!checkBox)
            {
                OpenFileDialog openWebDialog = new OpenFileDialog();
                openWebDialog.FileName = String.Empty;
                openWebDialog.Multiselect = false;
                //openWebDialog.Filter = "";
                if (openWebDialog.ShowDialog() == true)
                {
                    ListSelectionFolder.Visibility = Visibility.Collapsed;
                    ListSelectionWeb.Visibility = Visibility.Visible;
                    ListSelectionWebFav.Visibility = Visibility.Visible;
                    GridSplitterWebLists.Visibility = Visibility.Visible;
                    StackPanelSearch.Visibility = Visibility.Visible;
                    TextBlockFavListHeader.Visibility = Visibility.Visible;
                    if (!FolderListMenu.IsChecked)
                    {
                        CheckWebListsOn();
                    }
                    CheckListSwitch(sender, e);

                    webStationFile = openWebDialog.FileName;
                    ListSelectionWeb.Items.Clear();
                    folderSelection = false;
                    WebStationsStorage();
                    AddWebStationMenu.IsEnabled = true;
                    ViewSettings();
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
                TextBlockFavListHeader.Visibility = Visibility.Visible;
                if (!FolderListMenu.IsChecked)
                {
                    CheckWebListsOn();
                }
                CheckListSwitch(sender, e);

                webStationFile = AppDomain.CurrentDomain.BaseDirectory + @"RadioStations\RadioStation-List.csv";
                ListSelectionWeb.Items.Clear();
                ListSelectionWebFav.Items.Clear();
                folderSelection = false;
                WebStationsStorage();
                AddWebStationMenu.IsEnabled = true;
                ViewSettings();
            }

        }

        // Ausgewaehlte lokale Datei (Liste) in das Datagrid uebertragen
        private void WebStationsStorage()
        {

            var newWebStations = WebFileProcessor.WebFileProcessor.LoadFromTextFile<WebStations>(webStationFile);
            //var newWebStationsesFav = WebFileProcessor.WebFileProcessor.LoadFromTextFile<WebFavs>(webStationFile);

            foreach (var webStation in newWebStations)
            {
                ListSelectionWeb.Items.Add(new WebStations { StationFav = webStation.StationFav, StationName = webStation.StationName, BitRate = webStation.BitRate, StationUrl = webStation.StationUrl});
                webStationList.Add( new WebStations { StationName = webStation.StationName, BitRate = webStation.BitRate, StationUrl = webStation.StationUrl, StationFav = webStation.StationFav } );
                if (webStation.StationFav == true)
                {
                    ListSelectionWebFav.Items.Add(new WebStations { StationFav = webStation.StationFav, StationName = webStation.StationName, BitRate = webStation.BitRate, StationUrl = webStation.StationUrl });
                }
            }
            // Datagrid of FavoriteList adjust to height (max 200 in XAML) only at first load of List
            if (firstLoad && WebListMenu.IsChecked && FavListMenu.IsChecked)
            {
                RowFavListHeight.Height = new GridLength((ListSelectionWebFav.Items.Count * 24) + 45);
                firstLoad = false;
            }
            ChkBoxSaveOnExit.IsEnabled = true;
            ChkBoxSaveOnExit.IsChecked = true;

            
        }

        // Liste aus Datagrid in Datei speichern, wenn gewuenscht
        private void WindowMediaPLayer_Closing(object sender, CancelEventArgs e)
        {
            if (ChkBoxSaveOnExit.IsChecked == true)
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
                TextBlockFavListHeader.Visibility = Visibility.Collapsed;
                ListSelectionFolder.Visibility = Visibility.Visible;
                //CheckWebListsOn();
                FolderListMenu.IsChecked = true;
                FolderListMenu_Click(sender, e);
                //GridView vorbereiten
                //this.ListSelection.View = gridView;
                folderSelection = true;
                ChkBoxSaveOnExit.IsEnabled = false;
                ChkBoxSaveOnExit.IsChecked = false;
                AddWebStationMenu.IsEnabled = false;
                ViewSettings();

                // Pfad uebergeben
                sPath = folderDialog.SelectedPath;
                DirectoryInfo folder = new DirectoryInfo(sPath);
                int i = 1;
                if (folder.Exists)
                {
                    ListSelectionFolder.Items.Clear();
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
            PlayMenu.IsEnabled = true;
            StopMenu.IsEnabled = true;
            ForwardMenu.IsEnabled = true;
            BackwardMenu.IsEnabled = true;
            NextMenu.IsEnabled = true;
            PreviousMenu.IsEnabled = true;
            ButtonPlayPause.IsEnabled = true;
            ButtonBackwards.IsEnabled = true;
            ButtonForwards.IsEnabled = true;
            ImagePlayPic.Opacity = 0.85;
            ImageBackwardPic.Opacity = 0.85;
            ImageForwardPic.Opacity = 0.85;
            ThumbButtonPlay.IsEnabled = true;
            ThumbButtonNext.IsEnabled = true;
            ThumbButtonPrevious.IsEnabled = true;
            fileLoaded = true;
            // Timer (Ticker) starten
            timer.Tick += TimerTick;
            timer.Start();
        }

        private void PlayRoutineWeb()
        {
            PlayMenu.IsEnabled = true;
            StopMenu.IsEnabled = true;
            NextMenu.IsEnabled = true;

            PreviousMenu.IsEnabled = true;
            ButtonPlayPause.IsEnabled = true;
            ThumbButtonPlay.IsEnabled = true;
            ThumbButtonNext.IsEnabled = true;
            ThumbButtonPrevious.IsEnabled = true;
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
            if (!folderSelection)
            {
                // Photo by israel palacio(https://unsplash.com/@othentikisra?utm_source=unsplash&utm_medium=referral&utm_content=creditCopyText) on Unsplash(https://unsplash.com)
                ImageAudio.Source = new BitmapImage(new Uri("musicBackground/radio.jpg", UriKind.Relative));
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
                    // uodate Progressbar gespielte Zeit
                    if (MediaPlayer.NaturalDuration.HasTimeSpan && !sliderMoving)
                    {
                        ProgressPlayed.Minimum = 0;
                        ProgressPlayed.Maximum = MediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                        ProgressPlayed.Value = MediaPlayer.Position.TotalSeconds;
                    }
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
        private void ButtonPlayPause_Click(object sender, EventArgs e)
        {
            if (!playing)
            {
                if (folderSelection)
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
                if (folderSelection)
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
            StopMenu.IsEnabled = true;
        }

        // Mediendatei stoppen
        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            if (folderSelection)
            {
                ImagePlay();
                MediaPlayer.Stop();
                ImageStopPic.Opacity = 0.5;
                ButtonStop.IsEnabled = false;
                playing = false;
                PlayMenu.IsEnabled = true;
                StopMenu.IsEnabled = true;
                ForwardMenu.IsEnabled = true;
                BackwardMenu.IsEnabled = true;
                NextMenu.IsEnabled = true;
                PreviousMenu.IsEnabled = true;
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
                PlayMenu.IsEnabled = true;
                StopMenu.IsEnabled = true;
                NextMenu.IsEnabled = true;
                PreviousMenu.IsEnabled = true;
                timerWeb.Stop();
            }
            TaskbarItemInfo.Overlay = new BitmapImage(new Uri("pack://application:,,,/icons/stop.png"));
        }


        // Bilder tauschen (Play || Pause)
        void ImagePlay()
        {
            ImagePlayPic.Source = new BitmapImage(new Uri("icons/play.png", UriKind.Relative));
            TaskbarItemInfo.Overlay = new BitmapImage(new Uri("pack://application:,,,/icons/pause.png"));
            ThumbButtonPlay.ImageSource = new BitmapImage(new Uri("pack://application:,,,/icons/pause.png"));
            ButtonPlayPause.ToolTip = "Wiedergabe";
            PlayMenu.Header = "_Wiedergabe";
            PlayPauseMenuImage.Source = new BitmapImage(new Uri("icons/menu/menu-start.png", UriKind.Relative));
        }

        void ImagePause()
        {
            ImagePlayPic.Source = new BitmapImage(new Uri("icons/pause.png", UriKind.Relative));
            TaskbarItemInfo.Overlay = new BitmapImage(new Uri("pack://application:,,,/icons/play.png"));
            ThumbButtonPlay.ImageSource = new BitmapImage(new Uri("pack://application:,,,/icons/play.png"));
            ButtonPlayPause.ToolTip = "Pause";
            PlayMenu.Header = "_Pause";
            PlayPauseMenuImage.Source = new BitmapImage(new Uri("icons/menu/menu-pause.png", UriKind.Relative));
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

        // Bild der SoundBox der jeweiligen Lautstaerke anpassen
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
            if (!playing || !folderSelection)
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
            if (!playing || !folderSelection)
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
            SoundBoxVolume();
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

        // Wenn Internetverbindung vorhanden, Bild anpassen
        private void WindowMediaPLayer_Loaded(object sender, RoutedEventArgs e)
        {
            if (CheckForInternetConnection() == true)
            {
                InternetConPic.Source = new BitmapImage(new Uri("internetCon/network-connect-3.png", UriKind.Relative));
            }
        }

        // Je nach Liste in die geklickt wurde die korrekte Adresse laden
        // Ueberpruefung per Mouseover auf welcher Liste sich die Maus befindet
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

        // Auswahl aus der Liste in den Mediaplayer laden
        private void ListSelection_SelectionChanged(object sender, SelectionChangedEventArgs e, int identifier)
        {
            if (((ListSelectionWeb.Visibility == Visibility.Visible && ListSelectionWeb.SelectedItem == null) && (ListSelectionWebFav.Visibility == Visibility.Visible && ListSelectionWebFav.SelectedItem == null)) || (ListSelectionFolder.Visibility == Visibility.Visible && ListSelectionFolder.SelectedItem == null))
            {
                return;
            }
            else
            {
                if (folderSelection)
                {
                    dynamic selectedItemFolder = ListSelectionFolder.SelectedItems[0];
                    var selectionFolder = selectedItemFolder.FileName;

                    StringBuilder sB = new StringBuilder(sPath);
                    sB.Append(@"\");
                    sB.Append(selectionFolder);
                    MediaPlayer.Source = new Uri(sB.ToString());
                    PlayRoutine();
                    playing = false;
                    ButtonPlayPause_Click(sender, e);
                    LabelFileName.Content = sB.ToString();
                }
                else if (!folderSelection && TextBoxSearch.Text != string.Empty)
                {
                    if (identifier == 1)
                    {
                        ButtonCancelSearch_Click(sender, e);
                        selectedItemWeb = ListSelectionWebFav.SelectedItems[0];
                    }
                    else
                    {
                        selectedItemWeb = ListSelectionWeb.SelectedItems[0];
                    }
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

        // Wenn Favorit an- oder abgewaehlt wurde, Listen aktualisieren und neu laden in GUI
        // Normale WebStationen Liste
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
            RowFavListHeight.Height = new GridLength((ListSelectionWebFav.Items.Count * 24) + 45);

        }
        // Wenn Favorit an- oder abgewaehlt wurde, Listen aktualisieren und neu laden in GUI
        // Favoriten Liste
        private void CheckBoxFav_Click(object sender, RoutedEventArgs e)
        {
            dynamic selectedItemWeb = ListSelectionWebFav.SelectedItems[0];
            var selectedName = selectedItemWeb.StationName;
            var selectedFav = selectedItemWeb.StationFav;

            if (selectedFav == false)
            {
                webStationList.First(f => f.StationName == selectedName).StationFav = Convert.ToBoolean("False");
                //ListSelectionWeb.ItemsSource = webStationList;
                ListSelectionWeb.Items.Clear();
                foreach (var station in webStationList)
                {
                    ListSelectionWeb.Items.Add(new WebStations { StationName = station.StationName, BitRate = station.BitRate, StationUrl = station.StationUrl, StationFav = station.StationFav });
                }
            }
            else
            {
                webStationList.First(f => f.StationName == selectedName).StationFav = Convert.ToBoolean("True");
                //ListSelectionWeb.ItemsSource = webStationList;
                ListSelectionWeb.Items.Clear();
                foreach (var station in webStationList)
                {
                    ListSelectionWeb.Items.Add(new WebStations { StationName = station.StationName, BitRate = station.BitRate, StationUrl = station.StationUrl, StationFav = station.StationFav });
                }
            }
        }


        // Fokus auf Suche-Textbox 
        private void TextBoxSearch_GotMouseCapture(object sender, MouseEventArgs e)
        {
            TextBoxSearch.Text = string.Empty;
        }

        //private void TextBoxSearch_LostFocus(object sender, RoutedEventArgs e)
        //{
        //    TextBoxSearch.Text = "Search";
        //}

        private void ButtonCancelSearch_Click(object sender, RoutedEventArgs e)
        {
            if (TextBoxSearch.Text == "Search" || TextBoxSearch.Text == string.Empty)
            {
                return;
            }
            TextBoxSearch.Text = "Search";
            ListSelectionWeb.Items.Clear();
            foreach (var station in webStationList)
            {
                ListSelectionWeb.Items.Add(new WebStations { StationName = station.StationName, BitRate = station.BitRate, StationUrl = station.StationUrl, StationFav = station.StationFav });
            }
        }

        // Liste durchsuchen per TextBox
        private void TextBoxSearch_KeyUp(object sender, KeyEventArgs e)
        {
            var filter = webStationList.Where(webStationList => webStationList.StationName.ToLower().Contains(TextBoxSearch.Text));
            ListSelectionWeb.Items.Clear();
            foreach (var station in filter)
            {
                ListSelectionWeb.Items.Add(new WebStations { StationName = station.StationName, BitRate = station.BitRate, StationUrl = station.StationUrl, StationFav = station.StationFav });
            }
        }

        private void AddWebStation_Click(object sender, RoutedEventArgs e)
        {
            string stationName;
            string stationUrl;
            string bitRate;
            bool fav;

            AddRadiostation openDialog = new AddRadiostation();
            openDialog.ShowDialog();
            openDialog.Owner = this;

            if (openDialog.DialogResult == true)
            {
                stationName = openDialog.StationName;
                stationUrl = openDialog.StationUrl;
                bitRate = openDialog.BitRate;
                fav = openDialog.Favorite;
                AddStationToList(stationName, bitRate, stationUrl, fav);
            }
        }

        // Webradio Station hinzufuegen
        private void AddStationToList(string stationName, string bitRate, string stationUrl, bool fav)
        {
            // Radiostaion hinzu
            webStationList.Add(new WebStations { StationName = stationName, BitRate = bitRate, StationUrl = stationUrl, StationFav = fav });
            // Liste sortieren
            var orderedStationList = webStationList.OrderBy(x => x.StationName).ToList();
            // GUI und List<T> updaten
            ListSelectionWeb.Items.Clear();
            webStationList.Clear();
            foreach (var station in orderedStationList)
            {
                webStationList.Add(new WebStations { StationName = station.StationName, BitRate = station.BitRate, StationUrl = station.StationUrl, StationFav = station.StationFav });
                ListSelectionWeb.Items.Add(new WebStations { StationName = station.StationName, BitRate = station.BitRate, StationUrl = station.StationUrl, StationFav = station.StationFav });
            }
            // wenn direkt als Favorit speichern, Favoritenliste updaten
            if (fav)
            {
                ListSelectionWebFav.Items.Clear();
                foreach (var station in orderedStationList)
                {
                    if (station.StationFav)
                    {
                        ListSelectionWebFav.Items.Add(new WebStations { StationName = station.StationName, BitRate = station.BitRate, StationUrl = station.StationUrl, StationFav = station.StationFav });
                    }
                }
            }
        }

        // Track-/ Videoposition per Progressbar steuern
        private void ProgressPlayed_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!playing && !folderSelection)
            {
                return;
            }
            double mousePosition = e.GetPosition(ProgressPlayed).X;
            MediaPlayer.Position = TimeSpan.FromSeconds(SetProgressBarValuePlayed(mousePosition));
        }

        private double SetProgressBarValuePlayed(double mP)
        {
            double ratio = mP / ProgressPlayed.ActualWidth ;
            double progressBarValue = ratio * ProgressPlayed.Maximum;
            return progressBarValue;
        }

        //private void ProgressPlayed_MouseMove(object sender, MouseEventArgs e)
        //{
        //    if (e.MouseDevice.LeftButton == MouseButtonState.Pressed )
        //    {
        //        sliderMoving = true;
        //        double mousePosition = e.GetPosition(ProgressPlayed).X;
        //        ProgressPlayed.Value = SetProgressBarValuePlayed(mousePosition);
        //    }
        //}

        //private void ProgressPlayed_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        //{
        //    if (!playing)
        //    {
        //        return;
        //    }
        //    sliderMoving = false;
        //    double mousePosition = e.GetPosition(ProgressPlayed).X;
        //    MediaPlayer.Position = TimeSpan.FromSeconds(SetProgressBarValuePlayed(mousePosition));
        //}


        // Close Window
        private void CloseMenu_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }



        public void ListController()
        {

        }

        /// <summary>
        /// Ab hier "View" Bereich fuer Listen an/aus
        /// </summary>
        private void FolderListMenu_Click(object sender, RoutedEventArgs e)
        {
            if (FolderListMenu.IsChecked)
            {
                ColumnWidthLists.MinWidth = 200;
                ColumnGridSplitter.Width = columnSplitter;

                ListSelectionFolder.Visibility = Visibility.Visible;
                GridSplitterColumn.Visibility = Visibility.Visible;
                favListSelected = true;
            }
            else
            {
                columnList = ColumnWidthLists.Width;
                columnSplitter = ColumnGridSplitter.Width;

                ListSelectionFolder.Visibility = Visibility.Collapsed;
                GridSplitterColumn.Visibility = Visibility.Collapsed;
                ColumnWidthLists.MinWidth = 0;
                favListSelected = false;

                ColumnWidthLists.Width = new GridLength(0);
                ColumnGridSplitter.Width = new GridLength(0);
            }
        }

        private void FavListMenu_Click(object sender, RoutedEventArgs e)
        {
            if (FavListMenu.IsChecked)
            {
                if (rowFavList.GridUnitType == GridUnitType.Star && webListSelected || rowFavList.Value == 0 && webListSelected)
                {
                    rowFavList = new GridLength(ListSelectionWebFav.Items.Count * 24 + 45);
                    RowFavListHeight.Height = rowFavList;
                }
                else if (rowFavList.Value == 0 && !webListSelected)
                {
                    rowFavList = new GridLength(1, GridUnitType.Star);
                }
                else
                {
                    RowFavListHeight.Height = rowFavList;
                }

                RowFavListHeight.MinHeight = favListMinHeight;
                ListSelectionWebFav.Visibility = Visibility.Visible;
                TextBlockFavListHeader.Visibility = Visibility.Visible;
                if (webListSelected)
                {
                    RowFavGridSplitter.Height = new GridLength(3);
                    GridSplitterWebLists.Visibility = Visibility.Visible;
                    GridSplitterWebLists.ResizeBehavior = GridResizeBehavior.PreviousAndNext;
                }
                favListSelected = true;

                if (!WebListMenu.IsChecked)
                {
                    CheckWebListsOn();
                }

            }
            else
            {
                rowFavList = RowFavListHeight.Height;
                favListMinHeight = RowFavListHeight.MinHeight;

                ListSelectionWebFav.Visibility = Visibility.Collapsed;
                TextBlockFavListHeader.Visibility = Visibility.Collapsed;
                GridSplitterWebLists.Visibility = Visibility.Collapsed;
                favListSelected = false;

                RowFavListHeight.MinHeight = 0;
                RowFavListHeight.Height = new GridLength(0);
                RowFavGridSplitter.Height = new GridLength(0);

                CheckWebListsOff();
            }
        }

        private void SearchboxMenu_Click(object sender, RoutedEventArgs e)
        {
            if (SearchboxMenu.IsChecked)
            {
                RowSearchBoxGrid.Height = new GridLength(25);

                StackPanelSearch.Visibility = Visibility.Visible;
                GridSplitterWebLists.ResizeBehavior = GridResizeBehavior.PreviousAndNext;
            }
            else
            {
                StackPanelSearch.Visibility = Visibility.Collapsed;

                RowSearchBoxGrid.Height = new GridLength(0);
            }
        }

        private void WebListMenu_Click(object sender, RoutedEventArgs e)
        {
            if (WebListMenu.IsChecked)
            {
                RowWebListGrid.Height = new GridLength(1, GridUnitType.Star);
                RowFavListHeight.MaxHeight = 200;
                

                ListSelectionWeb.Visibility = Visibility.Visible;
                webListSelected = true;
                GridSplitterWebLists.ResizeBehavior = GridResizeBehavior.PreviousAndNext;

                SearchboxMenu.IsEnabled = true;

                // Searchbox aktiv setzen wenn checked
                if (SearchboxMenu.IsChecked)
                {
                    SearchboxMenu_Click(sender, e);
                }
                // FavoritenListe aktiv setzen wenn checked
                if (FavListMenu.IsChecked)
                {
                    FavListMenu_Click(sender,e);
                }

                if (!FavListMenu.IsChecked)
                {
                    RowFavListHeight.Height = new GridLength(0);
                    CheckWebListsOn();
                }
            }
            else
            {
                RowFavListHeight.MaxHeight = 2800;

                ListSelectionWeb.Visibility = Visibility.Collapsed;
                webListSelected = false;

                RowWebListGrid.Height = new GridLength(0);

                // Searchbox ausblenden, wenn noch eingeblendet && deaktivieren im Menu
                if (SearchboxMenu.IsChecked)
                {
                    SearchboxMenu.IsChecked = false;
                    SearchboxMenu_Click(sender, e);
                    SearchboxMenu.IsChecked = true;
                }
                SearchboxMenu.IsEnabled = false;
                // Werte nur speichern, wenn FavListe unchecked
                if (!FavListMenu.IsChecked)
                {
                    rowFavList = RowFavListHeight.Height;
                    favListMinHeight = RowFavListHeight.MinHeight;
                }
                GridSplitterWebLists.Visibility = Visibility.Collapsed;
                RowFavGridSplitter.Height = new GridLength(0);
                GridSplitterWebLists.ResizeBehavior = GridResizeBehavior.PreviousAndNext;
                RowFavListHeight.Height = new GridLength(1, GridUnitType.Star);

                CheckWebListsOff();
            }
        }

        private void CheckWebListsOff()
        {
            if (!favListSelected && !webListSelected)
            {
                //columnListMinWidth = ColumnWidthLists.MinWidth;
                columnList = ColumnWidthLists.Width;

                ColumnWidthLists.MinWidth = 0;
                ColumnWidthLists.Width = new GridLength(0);
                GridSplitterColumn.Visibility = Visibility.Collapsed;
                ColumnGridSplitter.Width = new GridLength(0);

                RowFavListHeight.Height = new GridLength(1, GridUnitType.Star);
            }
        }

        private void CheckWebListsOn()
        {
            ColumnWidthLists.MinWidth = 255;
            if (columnList.Value == 0)
            {
                ColumnWidthLists.Width = new GridLength(300);
            }
            ColumnGridSplitter.Width = new GridLength(3);
            GridSplitterColumn.Visibility = Visibility.Visible;
        }

        private void CheckListSwitch(object sender, RoutedEventArgs e)
        {
            if (!FavListMenu.IsChecked)
            {
                FavListMenu_Click(sender, e);
            }

            if (!WebListMenu.IsChecked)
            {
                WebListMenu_Click(sender, e);
            }

            if (!SearchboxMenu.IsChecked)
            {
                SearchboxMenu_Click(sender, e);
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

