using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using SO_Mediaplayer.Models;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;
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
        private bool fileSelection = true;
        // zwischenspeichern (Stop/Play)
        private string tempSelectionWeb;
        // Maximale Spielzeit der geladenen Dateien ----noch offen
        private string playtime;
        // Spracheselektion uebergeben
        private string langSelection;

        // Progressbar Steuerung
        private bool sliderMoving;
        // Schalter fuers muten
        private bool isMuted = false;

        // Hilfsattribute fuer die Listenauswahl (click)
        private dynamic selectedItemWeb;
        private dynamic selectionWeb;

        // Bool fuer Ansichten
        private bool folderSelected = true;
        private bool favListSelected = true;
        private bool webListSelected = true;
        private bool firstLoad;

        // Loop and Random play
        private bool loop = true;
        private bool loopOne = true;
        private bool playRandom = true;

        //private double columnListMinWidth;
        private double favListMinHeight;
        private GridLength columnList;
        private GridLength columnSplitter;
        private GridLength rowFavList;

        // Spielzeit fuer WebRadio
        DateTime startTime;
        DateTime diff;

        // Ellipse fuer Time-Progressbar
        Ellipse elliTime = new Ellipse();
        public Point ElliPos { get; set; }

        // StandardListe laden oder oeffnen
        private bool checkBox = true;

        // Timer (Ticker) um aktuelle Zeit wiederzuegeben an Label
        readonly DispatcherTimer timer = new DispatcherTimer();
        readonly DispatcherTimer timerWeb = new DispatcherTimer();

        // Liste fuer die Radiostaionen
        readonly List<WebStations> webStationList = new List<WebStations>();

        private SetLanguages Sl;
        private string thumbButton;
        public BitmapImage ButtonSkipBackwardGraphic { get; set; }
        public BitmapImage ButtonSkipForwardGraphic { get; set; }
        public BitmapImage ButtonStopGraphic { get; set; }
        public BitmapImage ButtonPlayGraphic { get; set; }
        public BitmapImage ButtonPauseGraphic { get; set; }
        public BitmapImage ButtonBackwardGraphic { get; set; }
        public BitmapImage ButtonForwardGraphic { get; set; }

        private readonly Buttons buttons = new Buttons();

        #endregion


        #region Methods

        public MainWindow()
        {
            InitializeComponent();

            // Timerintervall setzen
            timer.Interval = TimeSpan.FromMilliseconds(250);
            timerWeb.Interval = TimeSpan.FromSeconds(1);

            ViewSettings();

            firstLoad = true;

            ElliTimePos();

            SetLanguageStartUp();

            // Light-/Darkmode
            LightStyle.IsChecked = true;
            // Buttonstyle
            Buttons3.IsChecked = true;
            SetButtonsStartUp();
            ButtonLoop_OnClick(new object(), new RoutedEventArgs());
            ButtonShuffle_OnClick(new object(), new RoutedEventArgs());
        }

        private void SetLanguageStartUp()
        {
            foreach (MenuItem item in MenuItemLanguages.Items)
            {
                if (item.Tag.ToString().Equals(CultureInfo.CurrentUICulture.Name))
                {
                    item.IsChecked = true;
                    Sl = new SetLanguages(item.Tag.ToString());
                    langSelection = item.Tag.ToString();
                }
            }
            TextBlockFavListHeader.Text = Sl.FavListHeader;
            SetDataGridHeadersLang();
        }

        private void SetDataGridHeadersLang()
        {
            FavsSort.Header = Sl.WebListFav;
            FavsSortFav.Header = Sl.WebListFav;
            DgcStationNameFav.Header = Sl.WebListStationName;
            DgcBitrateFav.Header = Sl.WebListBitrate;
            DgcWebUrlFav.Header = Sl.WebListWebUrl;
            DgcStationName.Header = Sl.WebListStationName;
            DgcBitrate.Header = Sl.WebListBitrate;
            DgcWebUrl.Header = Sl.WebListWebUrl;
        }

        private void SetButtonsStartUp()
        {
            foreach (MenuItem item in MenuItemButtons.Items)
            {
                if (item.IsChecked)
                {
                    thumbButton = item.Name;
                    MenuItem_Buttons_Click(item, new RoutedEventArgs());
                }
            }
        }

        private void ElliTimePos()
        {
            GradientBrush filling = new RadialGradientBrush(Colors.LightGray, Color.FromRgb(198, 198, 198));
            elliTime.Fill = filling;
            elliTime.Height = 16;
            elliTime.Width = 16;

            DropShadowEffect effBlur = new DropShadowEffect();
            effBlur.BlurRadius = 3;
            effBlur.ShadowDepth = 1;
            effBlur.Direction = -75;
            effBlur.Color = Colors.Gray;
            elliTime.Effect = effBlur;

            Canvas.SetTop(elliTime, ProgressPlayed.Height / 3 - 1);
            //CanvasPbTime.Children.Add(elliTime);
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
                ListSelectionFolder.Items.Clear();
                string playtime = "00:00:00";
                ListSelectionFolder.Items.Add(new FolderPick { Number = 1.ToString(), FileName = openDialog.FileName, PlayTime = playtime });
                fileSelection = true;
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
			fileSelection = false;
			WebStationsStorage();
			AddWebStationMenu.IsEnabled = true;
			ViewSettings();
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
                fileSelection = false;
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
            NextMenu.IsEnabled = true;
            PreviousMenu.IsEnabled = true;

            ButtonPlayPause.IsEnabled = true;
            ImagePlayPic.Opacity = 0.85;

            ButtonSkipBackward.IsEnabled = true;
            ImageSkipBackwardPic.Opacity = 0.85;

            ButtonSkipForward.IsEnabled = true;
            ImageSkipForwardPic.Opacity = 0.85;

            ThumbButtonPlay.IsEnabled = true;
            ThumbButtonNext.IsEnabled = true;
            ThumbButtonPrevious.IsEnabled = true;

            fileLoaded = true;

            if (folderSelection)
            {
                ForwardMenu.IsEnabled = true;
                BackwardMenu.IsEnabled = true;

                ButtonBackwards.IsEnabled = true;
                ImageBackwardPic.Opacity = 0.85;
                ButtonForwards.IsEnabled = true;
                ImageForwardPic.Opacity = 0.85;

                ButtonLoop.IsEnabled = true;
                ImageLoop.Opacity = 0.65;
                ButtonShuffle.IsEnabled = true;
                ImageShuffle.Opacity = 0.65;

                // Timer (Ticker) starten
                timer.Tick += TimerTick;
                timer.Start();
                ProgressPlayed.IsHitTestVisible = true;
            }
            else // web
            {
                ForwardMenu.IsEnabled = false;
                BackwardMenu.IsEnabled = false;

                ButtonBackwards.IsEnabled = false;
                ImageBackwardPic.Opacity = 0.5;
                ButtonForwards.IsEnabled = false;
                ImageForwardPic.Opacity = 0.5;

                ButtonLoop.IsEnabled = false;
                ImageLoop.Opacity = 0.3;
                ButtonShuffle.IsEnabled = false;
                ImageShuffle.Opacity = 0.3;

                startTime = DateTime.Now;
                LabelMaxTime.Content = "--:--:--";
                // Timer (Ticker) starten
                timerWeb.Tick += TimerWebTick;
                timerWeb.Start();
                ProgressTimeWeb();
            }
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
                MessageBox.Show(Sl.ErrorLoad, Sl.MsgBoxInfo, MessageBoxButton.OK, MessageBoxImage.Information);
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
                    CanvasPbTime.Children.Clear();
                    // update Progressbar gespielte Zeit
                    if (MediaPlayer.NaturalDuration.HasTimeSpan && !sliderMoving)
                    {
                        ProgressPlayed.Minimum = 0;
                        ProgressPlayed.Maximum = MediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                        ProgressPlayed.Value = MediaPlayer.Position.TotalSeconds;
                        // Timebar Ellipse aktualisieren
                        Canvas.SetLeft(elliTime, (((ProgressPlayed.ActualWidth / MediaPlayer.NaturalDuration.TimeSpan.TotalSeconds) * MediaPlayer.Position.TotalSeconds) - (elliTime.Width / 2)));
                        CanvasPbTime.Children.Add(elliTime);
                    }

                    if (MediaPlayer.Position == MediaPlayer.NaturalDuration.TimeSpan)
                    {
                        if (loopOne)
                        {
                            MediaPlayer.Position = new TimeSpan(0);
                            return;
                        }
                        if (loop)
                        {
                            ButtonSkipForward_Click(sender, e);
                        }
                        else
                        {
                            MediaPlayer.Position = new TimeSpan(0);
                            ButtonStop_Click(sender, new RoutedEventArgs());
                        }
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
                    ForwardMenu.IsEnabled = true;
                    BackwardMenu.IsEnabled = true;
                    ButtonBackwards.IsEnabled = true;
                    ImageBackwardPic.Opacity = 0.85;
                    ButtonForwards.IsEnabled = true;
                    ImageForwardPic.Opacity = 0.85;
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
            ButtonStop.IsEnabled = true;
            ImageStopPic.Opacity = 0.85;
            StopMenu.IsEnabled = true;
        }

        // Mediendatei stoppen
        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            ImagePlay();
            MediaPlayer.Stop();
            ButtonStop.IsEnabled = false;
            ImageStopPic.Opacity = 0.5;
            ButtonBackwards.IsEnabled = false;
            ImageBackwardPic.Opacity = 0.5;
            ButtonForwards.IsEnabled = false;
            ImageForwardPic.Opacity = 0.5;
            StopMenu.IsEnabled = false;
            ForwardMenu.IsEnabled = false;
            BackwardMenu.IsEnabled = false;
            NextMenu.IsEnabled = true;
            PreviousMenu.IsEnabled = true;
            playing = false;
            // WebRadio has to deload Source
            if (!folderSelection)
            {
                MediaPlayer.Source = null;
                timerWeb.Stop();
            }
        }


        // Bilder tauschen (Play || Pause)
        void ImagePlay()
        {
            ImagePlayPic.Source = ButtonPlayGraphic;
            TaskbarItemInfo.Overlay = new BitmapImage(new Uri("pack://application:,,,/icons/" + thumbButton + "/play.png"));
            ThumbButtonPlay.ImageSource = new BitmapImage(new Uri("pack://application:,,,/icons/" + thumbButton + "/play.png"));
            ThumbButtonPlay.Description = Sl.Play;
            ButtonPlayPause.ToolTip = Sl.Play;
            PlayMenu.Header = Sl.MenuPlay;
            PlayPauseMenuImage.Source = new BitmapImage(new Uri("icons/menu/menu-start.png", UriKind.Relative));
        }

        void ImagePause()
        {
            ImagePlayPic.Source = ButtonPauseGraphic;
            TaskbarItemInfo.Overlay = new BitmapImage(new Uri("pack://application:,,,/icons/" + thumbButton + "/pause.png"));
            ThumbButtonPlay.ImageSource = new BitmapImage(new Uri("pack://application:,,,/icons/" + thumbButton + "/pause.png"));
            ThumbButtonPlay.Description = Sl.Pause;
            ButtonPlayPause.ToolTip = Sl.Pause;
            PlayMenu.Header = Sl.MenuPause;
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
                if (!isMuted)
                {
                    MediaPlayer.Volume = ProgressVolume.Value;
                }
            }
            else
            {
                // Nach hinten reduziert
                ProgressVolume.Value -= 0.05;
                SoundBoxVolume();
                if (!isMuted)
                {
                    MediaPlayer.Volume = ProgressVolume.Value;
                }
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
            MediaPlayer.Volume = ProgressVolume.Value;
            isMuted = false;
            SoundBoxVolume();
        }

        private double SetProgressBarValue(double mP)
        {
            double ratio = mP / ProgressVolume.ActualWidth;
            double progressBarValue = ratio * ProgressVolume.Maximum;
            return progressBarValue;
        }


        private void ProgressVolume_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.MouseDevice.LeftButton == MouseButtonState.Pressed)
            {
                double mousePosition = e.GetPosition(ProgressVolume).X;
                ProgressVolume.Value = SetProgressBarValue(mousePosition);
                MediaPlayer.Volume = ProgressVolume.Value;

                SoundBoxVolume();
            }
        }

        //private void ProgressVolume_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        //{
        //    if (!playing)
        //    {
        //        return;
        //    }
        //    sliderMoving = false;
        //    double mousePosition = e.GetPosition(ProgressPlayed).X;
        //    MediaPlayer.Position = TimeSpan.FromSeconds(SetProgressBarValuePlayed(mousePosition));
        //}


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
                if (fileSelection)
                {
                    return;
                }
                if (folderSelection)
                {
                    dynamic selectedItemFolder = ListSelectionFolder.SelectedItems[0];
                    var selectionFolder = selectedItemFolder.FileName;

                    var sB = new StringBuilder(sPath);
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
                    if (ListSelectionWebFav.SelectedItem == null)
                    {
                        identifier = 0;
                    }
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
                        MessageBox.Show(Sl.ErrorLoad, Sl.MsgBoxInfo, MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    PlayRoutine();
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
            if (TextBoxSearch.Text == Sl.Search || TextBoxSearch.Text == string.Empty)
            {
                return;
            }
            TextBoxSearch.Text = Sl.Search;
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

            AddRadiostation openDialog = new AddRadiostation(langSelection, Top + Height / 2, Left + Width / 2);
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


        // Close Window
        private void CloseMenu_Click(object sender, RoutedEventArgs e)
        {
            Close();
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

        private void ButtonSkipBackward_Click(object sender, EventArgs e)
        {
            if (folderSelection)
            {
                var lsf = ListSelectionFolder;

                if (lsf.SelectedIndex >= 0)
                {
                    lsf.SelectedIndex--;
                }

                if (lsf.SelectedIndex < 0)
                {
                    int lastRow = lsf.Items.Count - 1;
                    lsf.SelectedIndex = lastRow;
                    lsf.ScrollIntoView(lsf.SelectedItem);
                }
            }
            else
            {
                var lsw = ListSelectionWeb;

                if (lsw.SelectedIndex >= 0)
                {
                    lsw.SelectedIndex--;
                    if (lsw.SelectedItem == null)
                    {
                        return;
                    }
                    lsw.ScrollIntoView(lsw.SelectedItem);
                }

                if (lsw.SelectedIndex < 0)
                {
                    int lastRow = lsw.Items.Count - 1;
                    lsw.SelectedIndex = lastRow;
                    lsw.ScrollIntoView(lsw.SelectedItem);
                }
            }
        }

        private void ButtonSkipForward_Click(object sender, EventArgs e)
        {
            if (folderSelection)
            {
                var lsf = ListSelectionFolder;

                if (lsf.Items.Count - 1 == lsf.SelectedIndex)
                {
                    lsf.SelectedIndex = -1;
                }

                if (lsf.Items.Count - 1 >= lsf.SelectedIndex)
                {
                    lsf.SelectedIndex++;
                    lsf.ScrollIntoView(lsf.SelectedItem);
                }
            }
            else
            {
                var lsw = ListSelectionWeb;

                if (lsw.Items.Count - 1 == lsw.SelectedIndex)
                {
                    lsw.SelectedIndex = -1;
                }

                if (lsw.Items.Count - 1 >= lsw.SelectedIndex)
                {
                    lsw.SelectedIndex++;
                    lsw.ScrollIntoView(lsw.SelectedItem);
                }
            }
        }

        // ProgressTime Ellipse hide when scaling Window
        private void SOMediaPlayer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (playing)
            {
                var animElli = new DoubleAnimation();
                animElli.To = 0;
                animElli.BeginTime = TimeSpan.FromMilliseconds(0);
                animElli.Duration = TimeSpan.FromMilliseconds(100);
                animElli.FillBehavior = FillBehavior.Stop;
                elliTime.BeginAnimation(OpacityProperty, animElli);
            }
        }

        // ProgressBarTime fill complete when Webradio is playing
        private void ProgressTimeWeb()
        {
            CanvasPbTime.Children.Clear();
            ProgressPlayed.Maximum = 1;
            ProgressPlayed.Value = 1;
            ProgressPlayed.IsHitTestVisible = false;
        }

        private void MenuItem_Lang_Click(object sender, RoutedEventArgs e)
        {
            // Uncheck each item
            foreach (MenuItem item in MenuItemLanguages.Items)
            {
                item.IsChecked = false;
            }

            MenuItem miLang = sender as MenuItem;
            miLang.IsChecked = true;
            if (CultureInfo.CurrentCulture.Name.Equals(miLang.Tag.ToString()))
            {
                return;
            }
            App.Instance.SwitchLanguage(miLang.Tag.ToString());

            Sl = new SetLanguages(miLang.Tag.ToString());
            langSelection = miLang.Tag.ToString();
            TextBlockFavListHeader.Text = Sl.FavListHeader;
            SetDataGridHeadersLang();

            if (playing)
            {
                ImagePause();
            }
            else
            {
                ImagePlay();
            }
        }

        private void MenuItem_Style_Click(object sender, RoutedEventArgs e)
        {
            foreach (MenuItem item in MenuItemStyle.Items)
            {
                item.IsChecked = false;
            }

            MenuItem miStyle = sender as MenuItem;
            miStyle.IsChecked = true;
            string stylefile = Path.Combine(App.Directory, "Styles", miStyle.Name + ".xaml");
            App.Instance.LoadButtonsDictionaryFromFile(stylefile);

            // Load language again!
            foreach (MenuItem item in MenuItemLanguages.Items)
            {
                if (item.IsChecked)
                {
                    App.Instance.SwitchLanguage(item.Tag.ToString());
                }
            }

        }
        private void MenuItem_Buttons_Click(object sender, RoutedEventArgs e)
        {
            foreach (MenuItem item in MenuItemButtons.Items)
            {
                item.IsChecked = false;
            }

            MenuItem miButton = sender as MenuItem;
            miButton.IsChecked = true;
            thumbButton = miButton.Name;

            ButtonSkipBackwardGraphic = new BitmapImage(buttons.SetButtonSkipBackward(miButton.Name));
            ButtonSkipForwardGraphic = new BitmapImage(buttons.SetButtonSkipForward(miButton.Name));
            ButtonStopGraphic = new BitmapImage(buttons.SetButtonStop(miButton.Name));
            ButtonPlayGraphic = new BitmapImage(buttons.SetButtonPlay(miButton.Name));
            ButtonPauseGraphic = new BitmapImage(buttons.SetButtonPause(miButton.Name));
            ButtonBackwardGraphic = new BitmapImage(buttons.SetButtonBackward(miButton.Name));
            ButtonForwardGraphic = new BitmapImage(buttons.SetButtonForward(miButton.Name));
            
            SetGraphicButtons(sender, e);
        }

        private void SetGraphicButtons(object sender, RoutedEventArgs e)
        {
            ImageSkipBackwardPic.Source = ButtonSkipBackwardGraphic;
            ImageSkipForwardPic.Source = ButtonSkipForwardGraphic;
            ImageStopPic.Source = ButtonStopGraphic;
            ImagePlayPic.Source = ButtonPlayGraphic;
            ImageBackwardPic.Source = ButtonBackwardGraphic;
            ImageForwardPic.Source = ButtonForwardGraphic;

            ThumbButtonPrevious.ImageSource = new BitmapImage(new Uri("pack://application:,,,/icons/" + thumbButton + "/skip-backward.png"));
            ThumbButtonNext.ImageSource = new BitmapImage(new Uri("pack://application:,,,/icons/" + thumbButton + "/skip-forward.png"));

            if (playing)
            {
                ImagePlayPic.Source = ButtonPauseGraphic;
                TaskbarItemInfo.Overlay = new BitmapImage(new Uri("pack://application:,,,/icons/" + thumbButton + "/pause.png"));
                ThumbButtonPlay.ImageSource = new BitmapImage(new Uri("pack://application:,,,/icons/" + thumbButton + "/pause.png"));
            }
            else
            {
                ImagePlayPic.Source = ButtonPlayGraphic;
                TaskbarItemInfo.Overlay = new BitmapImage(new Uri("pack://application:,,,/icons/" + thumbButton + "/play.png"));
                ThumbButtonPlay.ImageSource = new BitmapImage(new Uri("pack://application:,,,/icons/" + thumbButton + "/play.png"));
            }

        }

        private void AboutMenu_OnClick(object sender, RoutedEventArgs e)
        {
            About about = new About(Top + Height / 2, Left + Width / 2);
            about.Show();
            about.Owner = this;
        }

        // Mute-Method when SoundIcon is clicked
        private void SoundBoxPic_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            //double tempVol = MediaPlayer.Volume;
            if (!isMuted)
            {
                SoundBoxPic.Source = new BitmapImage(new Uri("soundVol/audio-volume-muted.png", UriKind.Relative));
                MediaPlayer.Volume = 0;
                isMuted = true;
            }
            else
            {
                MediaPlayer.Volume = ProgressVolume.Value;
                SoundBoxVolume();
                isMuted = false;
            }
        }

        // Loop Method activate || deactivate
        private void ButtonLoop_OnClick(object sender, RoutedEventArgs e)
        {
            if (loop && loopOne)
            {
                loop = false;
                loopOne = false;
                ButtonLoop.ToolTip = Sl.Repeat;
                ImageLoop.Source = !DarkStyle.IsChecked 
                    ? new BitmapImage(new Uri("icons/loop.png", UriKind.Relative)) 
                    : new BitmapImage(new Uri("icons/loopLight.png", UriKind.Relative));
            }
            else if (!loop && !loopOne)
            {
                loop = true;
                ButtonLoop.ToolTip = Sl.RepeatAll;
                ImageLoop.Source = !DarkStyle.IsChecked
                    ? new BitmapImage(new Uri("icons/loopActive.png", UriKind.Relative))
                    : new BitmapImage(new Uri("icons/loopLightActive.png", UriKind.Relative));
            }
            else if (loop && !loopOne)
            {
                loopOne = true;
                ButtonLoop.ToolTip = Sl.RepeatOne;
                ImageLoop.Source = !DarkStyle.IsChecked
                    ? new BitmapImage(new Uri("icons/loopActiveOne.png", UriKind.Relative))
                    : new BitmapImage(new Uri("icons/loopLightActiveOne.png", UriKind.Relative));
            }
        }

        // Random Method activate || deactivate
        private void ButtonShuffle_OnClick(object sender, RoutedEventArgs e)
        {
            if (playRandom)
            {
                playRandom = false;
                ButtonShuffle.ToolTip = Sl.ShuffleOff;
                ImageShuffle.Source = !DarkStyle.IsChecked
                    ? new BitmapImage(new Uri("icons/shuffle.png", UriKind.Relative))
                    : new BitmapImage(new Uri("icons/shuffleLight.png", UriKind.Relative));
            }
            else
            {
                playRandom = true;
                ButtonShuffle.ToolTip = Sl.ShuffleOn;
                ImageShuffle.Source = !DarkStyle.IsChecked
                    ? new BitmapImage(new Uri("icons/shuffleActive.png", UriKind.Relative))
                    : new BitmapImage(new Uri("icons/shuffleLightActive.png", UriKind.Relative));
            }
        }

        #endregion
    }
}

