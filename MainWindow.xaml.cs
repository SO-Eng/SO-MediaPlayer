using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
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
using Microsoft.Win32;
using NAudio.Wave;
using SO_Mediaplayer.Models;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;
using WinForms = System.Windows.Forms;

namespace SO_Mediaplayer
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields
        // Mediaplayer is playing
        bool playing = false;
        // for HotKey-safety
        private bool fileLoaded = false;
        // Path to selected folder (Open folder)
        private string sPath = String.Empty;
        // CSV file to import (Path)
        private string webStationFile = String.Empty;
        // Folder or File opened, if flase then Webradio
        private bool folderSelection = true;
        private bool fileSelection = true;
        // Tempsaving (Stop/Play)
        private string tempSelectionWeb;
        // Max. duration of loaded file
        private string playtime;
        // Set selected language
        private string langSelection;
        // Windowtilte adjustement
        private string mediaPlayerTitle;
        private string webPlayerTitle;
        // Progressbar controll
        private bool sliderMoving;
        private bool mouseMove;
        // Switch for mute
        private bool isMuted = false;
        // Drop files
        int dropCount = 1;

        // Helping attributes for selected Items in Listview/Datagrid (click)
        private dynamic selectedItemWeb;
        private dynamic selectionWeb;

        // Bools for Loop and Random play
        private bool loop;
        private bool loopOne;
        private bool playRandom;
        private bool onStart;

        // Bools for View
        private bool folderSelected = true;
        private bool favListSelected = true;
        private bool webListSelected = true;
        private bool firstLoad;
        // Bools for saved Listview for StartUp
        private bool folderSelectedLast;
        private bool favListSelectedLast;
        private bool searchListSelectedLast;
        private bool webListSelectedLast;

        //Var's for View-Settings
        private double favListMinHeight;
        private GridLength columnList;
        private GridLength columnSplitter;
        private GridLength rowFavList;

        // Spielzeit fuer WebRadio
        DateTime startTime;
        DateTime diff;

        // Ellipse fuer Time-Progressbar
        Ellipse elliTime = new Ellipse();
        GradientBrush filling;
        GradientBrush fillingPressed;

        private bool progressMoving = false;
        private bool dragging = false;

        // StandardListe laden oder oeffnen
        private bool checkBox = true;
        // Timer (Ticker) um aktuelle Zeit wiederzuegeben an Label
        readonly DispatcherTimer timer = new DispatcherTimer();
        readonly DispatcherTimer timerWeb = new DispatcherTimer();
        readonly DispatcherTimer timerProgressBar = new DispatcherTimer();

        // Liste fuer die Radiostaionen
        readonly List<WebStations> webStationList = new List<WebStations>();
		// Instanitiate language class
        private SetLanguages Sl;
		// Var's for Buttonselection
        private string thumbButton;
        public BitmapImage ButtonSkipBackwardGraphic { get; set; }
        public BitmapImage ButtonSkipForwardGraphic { get; set; }
        public BitmapImage ButtonStopGraphic { get; set; }
        public BitmapImage ButtonPlayGraphic { get; set; }
        public BitmapImage ButtonPauseGraphic { get; set; }
        public BitmapImage ButtonBackwardGraphic { get; set; }
        public BitmapImage ButtonForwardGraphic { get; set; }

        private readonly Buttons buttons = new Buttons();
        private readonly LoopIcon Li = new LoopIcon();
        private readonly ShuffleIcon Si = new ShuffleIcon();

        // Shuffle var's
        List<int> allreadyPlayed = new List<int>();
        Random rnd = new Random();
        // Path for Registry
        private static string regPath = @"Software\SoftwOrt\SO-Mediaplayer";

        #endregion


        #region Methods
        // Constructor
        public MainWindow()
        {
            InitializeComponent();

            // Timerintervall setzen
            timer.Interval = TimeSpan.FromMilliseconds(50);
            timerWeb.Interval = TimeSpan.FromSeconds(1);
            timerWeb.Interval = TimeSpan.FromMilliseconds(10);

            mediaPlayerTitle = "SoftwOrt - Mediaplayer";
            webPlayerTitle = "SoftwOrt - WebRadioPlayer";
            ViewSettings();

            folderSelection = true;
            firstLoad = true;
            mouseMove = false;

            ElliTimePos();

            // Load last Settings and WPF-Props
            LoadRegToStart();
        }

        // Load settings Methods
        private void LoadRegToStart()
        {
            using (RegistryKey regkey = Registry.CurrentUser.OpenSubKey(regPath))
            {
                if (regkey != null)
                {
                    LoadSettings(regkey);
                }
                else
                {
                    // Language to Systemlanguage
                    SetLanguageStartUp();
                    // Buttonstyle
                    Buttons1.IsChecked = true;
                    SetButtonsStartUp();
                    // Turn off Loop and Shuffle
                    LightStyle.IsChecked = true;
                    onStart = false;
                    loop = true;
                    loopOne = true;
                    playRandom = true;
                    ButtonLoop_OnClick(new object(), new RoutedEventArgs());
                    ButtonShuffle_OnClick(new object(), new RoutedEventArgs());
                    // Lightmode
                    MenuItem_Style_Click(LightStyle, new RoutedEventArgs());

                    folderSelectedLast = true;
                    favListSelectedLast = true;
                    searchListSelectedLast = true;
                    webListSelectedLast = true;
                    onStart = true;
                }
            }
        }

        // Load last settings from registry
        private void LoadSettings(RegistryKey _regKey)
        {
            // MainWindow
            this.Top = Convert.ToDouble(_regKey.GetValue("Top"));
            this.Left = Convert.ToDouble(_regKey.GetValue("Left"));
            this.Width = Convert.ToDouble(_regKey.GetValue("Width"));
            this.Height = Convert.ToDouble(_regKey.GetValue("Height"));
            // Buttons
            if (_regKey.GetValue("Buttons") == null)
            {
                Buttons1.IsChecked = true;
                SetButtonsStartUp();
            }
            else
            {
                string buttons = _regKey.GetValue("Buttons").ToString();
                foreach (MenuItem item in MenuItemButtons.Items)
                {
                    if (item.Name.Equals(buttons))
                    {
                        item.IsChecked = true;
                        MenuItem_Buttons_Click(item, new RoutedEventArgs());
                    }
                }
            }
            // Language
            if (_regKey.GetValue("Language") == null)
            {
                SetLanguageStartUp();
            }
            else
            {
                langSelection = _regKey.GetValue("Language").ToString();
                foreach (MenuItem item in MenuItemLanguages.Items)
                {
                    if (item.Tag.ToString().Equals(langSelection))
                    {
                        item.IsChecked = true;
                        Sl = new SetLanguages(item.Tag.ToString());
                        MenuItem_Lang_Click(item, new RoutedEventArgs());
                    }
                }
            }
            // Style
            string lastStyle = _regKey.GetValue("Style").ToString();
            foreach (MenuItem item in MenuItemStyle.Items)
            {
                if (item.Name.Equals(lastStyle))
                {
                    item.IsChecked = true;
                    MenuItem_Style_Click(item, new RoutedEventArgs());
                }
            }
            // Loop && ShuffleButton
            onStart = true;
            loop = Convert.ToBoolean(_regKey.GetValue("Loop"));
            loopOne = Convert.ToBoolean(_regKey.GetValue("LoopOne"));
            playRandom = Convert.ToBoolean(_regKey.GetValue("Shuffle"));
            ButtonLoop_OnClick(new object(), new RoutedEventArgs());
            ButtonShuffle_OnClick(new object(), new RoutedEventArgs());

            // Volume
            ProgressVolume.Value = Convert.ToDouble(_regKey.GetValue("Volume"));
            MediaPlayer.Volume = ProgressVolume.Value;
            // View Folder/File
            folderSelectedLast = Convert.ToBoolean(_regKey.GetValue("ListViewFolder"));
            FolderListMenu.IsChecked = folderSelectedLast;
            FolderListMenu_Click(new object(), new RoutedEventArgs());
            // View Webradio
            favListSelectedLast = Convert.ToBoolean(_regKey.GetValue("ListViewFavorites"));
            searchListSelectedLast = Convert.ToBoolean(_regKey.GetValue("ListViewSearch"));
            webListSelectedLast = Convert.ToBoolean(_regKey.GetValue("ListViewWebradio"));
            FavListMenu.IsChecked = favListSelectedLast;
            SearchboxMenu.IsChecked = searchListSelectedLast;
            WebListMenu.IsChecked = webListSelectedLast;
            // Webradio was active?
            if (!Convert.ToBoolean(_regKey.GetValue("WebradioActive")))
            {
                ButtonOpenWeb_Click(new object(), new RoutedEventArgs());
            }
        }

        // Save settings to registry
        private void SaveSettings()
        {
            using (RegistryKey regKey = Registry.CurrentUser.CreateSubKey(regPath))
            {
                // MainWindow
                regKey.SetValue("Top", this.Top);
                regKey.SetValue("Left", this.Left);
                regKey.SetValue("Width", this.Width);
                regKey.SetValue("Height", this.Height);
                // Buttons
                foreach (MenuItem item in MenuItemButtons.Items)
                {
                    if (item.IsChecked)
                    {
                        regKey.SetValue("Buttons", item.Name);
                    }
                }
                regKey.SetValue("Loop", loop);
                regKey.SetValue("LoopOne", loopOne);
                regKey.SetValue("Shuffle", playRandom);
                // Language
                regKey.SetValue("Language", langSelection);
                // Style
                foreach (MenuItem item in MenuItemStyle.Items)
                {
                    if (item.IsChecked)
                    {
                        regKey.SetValue("Style", item.Name);
                    }
                }
                // Volume
                regKey.SetValue("Volume", MediaPlayer.Volume);
                // Webbradio
                regKey.SetValue("WebradioActive", folderSelection);
                // View (Lists of Foler/File && Webradio
                regKey.SetValue("ListViewFolder", FolderListMenu.IsChecked ? "True" : "False");
                regKey.SetValue("ListViewFavorites", FavListMenu.IsChecked ? "True" : "False");
                regKey.SetValue("ListViewSearch", SearchboxMenu.IsChecked ? "True" : "False");
                regKey.SetValue("ListViewWebradio", WebListMenu.IsChecked ? "True" : "False");
            }
        }

        // Define correct language at startup
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

		// Define correct Language from SetLanguage class
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

		// Show last selected Buttons at startup
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

		// Method for PrograssBar-Duration graphical Ellipse
        private void ElliTimePos()
        {
            filling = new RadialGradientBrush(Colors.LightGray, Color.FromRgb(198, 198, 198));
            fillingPressed = new RadialGradientBrush(Colors.DarkGray, Color.FromRgb(198, 198, 198));
            elliTime.Fill = filling;
            elliTime.Height = 16;
            elliTime.Width = 16;

            DropShadowEffect effBlur = new DropShadowEffect();
            effBlur.BlurRadius = 3;
            effBlur.ShadowDepth = 1;
            effBlur.Direction = -75;
            effBlur.Color = Colors.Gray;
            elliTime.Effect = effBlur;
            elliTime.IsHitTestVisible = false;

            Canvas.SetTop(elliTime, ProgressPlayed.Height / 3 - 1);
            //CanvasPbTime.Children.Add(elliTime);
        }

        // Menu-Settings for View (IsSelectable or not, depending on situation (file or webradio))
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


        // Open single file
        private void ButtonOpen_Click(object sender, RoutedEventArgs e)
        {
            // create Dialog
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
                // Split Fileinfo
                string fileInfo = openDialog.FileName.Split('\\').Last();
                sPath = openDialog.FileName.Replace(fileInfo, "");

                ListSelectionFolder.Items.Clear();
                //string playtime = "00:00:00";
                string playtime = GetFileDuration(openDialog.FileName).ToString((@"hh\:mm\:ss"));
                ListSelectionFolder.Items.Add(new FolderPick { Number = 1.ToString(), FileName = fileInfo, PlayTime = playtime });
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
                allreadyPlayed.Clear();
                timerWeb.Stop();
                this.Title = mediaPlayerTitle;
                onStart = false;
                dropCount = 2;
            }
        }

        public static TimeSpan GetFileDuration(string fileName)
        {
            string mediaFile = fileName.Split('.').Last();

            if (mediaFile.Equals("mp3"))
            {
                Mp3FileReader mp3 = new Mp3FileReader(fileName);
                return mp3.TotalTime;
            }

            if (mediaFile.Equals("wav"))
            {
                WaveFileReader wf = new WaveFileReader(fileName);
                return wf.TotalTime;
            }

            return TimeSpan.Zero;
        }

        // Load Radiostations
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
            this.Title = webPlayerTitle;
            onStart = true;
        }

        // Selected local file (List) show in Datagrid
        private void WebStationsStorage()
        {
            var newWebStations = WebFileProcessor.WebFileProcessor.LoadFromTextFile<WebStations>(webStationFile);

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

        // Save List from Datagrid in CSV-File if Checkbox == true && start closing routine
        private void WindowMediaPLayer_Closing(object sender, CancelEventArgs e)
        {
            if (ChkBoxSaveOnExit.IsChecked == true)
            {
                WebFileProcessor.WebFileProcessor.SaveToTextFile(webStationList, webStationFile);
            }
            timer.Stop();
            timerWeb.Stop();
            timerProgressBar.Stop();
            SaveSettings();
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
                FolderListMenu.IsChecked = true;
                FolderListMenu_Click(sender, e);
                // Prepare GridView
                folderSelection = true;
                fileSelection = false;
                ChkBoxSaveOnExit.IsEnabled = false;
                ChkBoxSaveOnExit.IsChecked = false;
                AddWebStationMenu.IsEnabled = false;
                ViewSettings();

                allreadyPlayed.Clear();
                //timerWeb.Stop();
                this.Title = mediaPlayerTitle;
                // Set path
                sPath = folderDialog.SelectedPath;
                sPath += "\\";
                DirectoryInfo folder = new DirectoryInfo(sPath);
                int i = 1;
                if (folder.Exists)
                {
                    ListSelectionFolder.Items.Clear();
                    foreach (var fileInfo in folder.GetFiles())
                    {
                        string playtime = GetFileDuration(sPath + fileInfo.ToString()).ToString((@"hh\:mm\:ss"));
                        ListSelectionFolder.Items.Add(new FolderPick { Number = i.ToString(), FileName = fileInfo.ToString(), PlayTime = playtime });
                        i++;
                    }
                }
                dropCount = i;
            }
        }

        // Playroutine for File or Folder opened
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
            else // Webradio
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


        // Show duration of track if Mediafile is loaded
        private void MediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (!folderSelection)
            {
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


        // #1 Timer to show current Trackposition, adjust ProgressBar and Ellipse #1
		// #2 Decide what to do when Track reached end of duration #2
        private void TimerTick(object sender, EventArgs e)
        {
			// #1
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
					// #2
                    if (MediaPlayer.Position == MediaPlayer.NaturalDuration.TimeSpan)
                    {
                        if (playRandom && ListSelectionFolder.Items.Count > 2)
                        {
                            ShuffleTrackList();
                            return;
                        }
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

		// Timer for Webradio-Player ( show played time )
        private void TimerWebTick(object sender, EventArgs e)
        {
            diff = DateTime.Now;
            string seconds = (diff - startTime).ToString(@"hh\:mm\:ss");
            LabelCurrentTime.Content = seconds;
        }


        // Play || Pause Mediaplayer
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

        // Stop Mediaplayer
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
            timerWeb.Stop();
            // WebRadio has to deload Source
            if (!folderSelection)
            {
                MediaPlayer.Source = null;
            }
        }


        // Change picture (Play || Pause)
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


        // Mediafile per Spacekey (plaz || pause) and Arrowkeys 10sec jump or skip for-/backward
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
            else if (e.Key == Key.Up)
            {
                ButtonSkipBackward_Click(sender, e);
            }
            else if (e.Key == Key.Down)
            {
                ButtonSkipForward_Click(sender, e);
            }
        }


        // Adjust volume per per MouseWheel
        private void WindowMediaPLayer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // In which direction MouseWheel is tourned
            if (e.Delta > 0)
            {
                // Forward? More louder
                ProgressVolume.Value += 0.05;
                if (!isMuted)
                {
                    MediaPlayer.Volume = ProgressVolume.Value;
                }
                SoundBoxVolume();
            }
            else
            {
                // Backward? More quiet
                ProgressVolume.Value -= 0.05;
                if (!isMuted)
                {
                    MediaPlayer.Volume = ProgressVolume.Value;
                }
                SoundBoxVolume();
            }
        }

        // Adjust picture of SoundBox to Volume-Value
        private void SoundBoxVolume()
        {
            if (MediaPlayer.Volume <= 0.001)
            {
                SoundBoxPic.Source = new BitmapImage(new Uri("soundVol/audio-volume-muted.png", UriKind.Relative));
            }
            else if (MediaPlayer.Volume > 0 && MediaPlayer.Volume <= 0.33)
            {
                SoundBoxPic.Source = new BitmapImage(new Uri("soundVol/audio-volume-low.png", UriKind.Relative));
            }
            else if (MediaPlayer.Volume > 0.331 && MediaPlayer.Volume < 0.66)
            {
                SoundBoxPic.Source = new BitmapImage(new Uri("soundVol/audio-volume-medium.png", UriKind.Relative));
            }
            else
            {
                SoundBoxPic.Source = new BitmapImage(new Uri("soundVol/audio-volume-high.png", UriKind.Relative));
            }
        }

        // Jump 10sec. forward
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

        // Jump 10sec. back
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


		// ProgressBar-Volume
        // ProgressBar react to MousLeftButtonDown and bring Value to clicked position
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


        // Check if Internetconnection is available
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

        // If internetconnection == true, adjust picture 
        private void WindowMediaPLayer_Loaded(object sender, RoutedEventArgs e)
        {
            if (CheckForInternetConnection() == true)
            {
                InternetConPic.Source = new BitmapImage(new Uri("internetCon/network-connect-3.png", UriKind.Relative));
            }
        }

        // Specify clicked List (DataGrid) and load correct webaddress
        // check per Mouseover over which Datagrid mouse is hoovered
        private void ListIdentifier(object sender, SelectionChangedEventArgs e)
        {
            ListSelection_SelectionChanged(sender, e, ListSelectionWebFav.IsMouseOver ? 1 : 0);
        }

        // Selection from Lists load to Mediaplayer
        private void ListSelection_SelectionChanged(object sender, SelectionChangedEventArgs e, int identifier)
        {
            if (((ListSelectionWeb.Visibility == Visibility.Visible && ListSelectionWeb.SelectedItem == null) && (ListSelectionWebFav.Visibility == Visibility.Visible && ListSelectionWebFav.SelectedItem == null)) || (ListSelectionFolder.Visibility == Visibility.Visible && ListSelectionFolder.SelectedItem == null))
            {
                return;
            }
            else
            {
				// only one file? go out!
                if (fileSelection)
                {
                    return;
                }
				// ListSelectionFolder
                if (folderSelection)
                {
                    dynamic selectedItemFolder = ListSelectionFolder.SelectedItems[0];
                    var selectionFolder = selectedItemFolder.FileName;

                    var sB = new StringBuilder(sPath);
                    sB.Append(@"\");
                    sB.Append(selectionFolder);
                    MediaPlayer.Source = new Uri(sB.ToString());
                    timerWeb.Stop();
                    PlayRoutine();
                    playing = false;
                    // Add in List for allready played
                    allreadyPlayed.Add(ListSelectionFolder.SelectedIndex + 1);
                    ButtonPlayPause_Click(sender, e);
                    LabelFileName.Content = sB.ToString();
                    onStart = false;
                }
				// ListSelectionWeb || ListSelectionWebFav
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

        // Catch Keyboardentries and forward
        private void ListSelection_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            HotkeysForward(sender, e);
        }

        private void GridSplitter_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            HotkeysForward(sender, e);
        }

        private void ButtonClick_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            HotkeysForward(sender, e);
        }

		// Get catch of Keyboardentries and specialize
        private void HotkeysForward(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                case Key.Right:
                case Key.Up:
                case Key.Down:
                case Key.Space:
                    e.Handled = true;
                    WindowMediaPLayer_KeyDown(sender, e);
                    break;
                default:
                    break;
            }
        }

        // If Favorite is selected || deselected, update List and load in GUI
        // Normal WebStationen List
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
        // If Favorite is selected || deselected, update List and load in GUI
        // List of Favorites
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

		// Method to clear TextBoxSearch and get ListSelectionWeb fully showed again
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

        // Search through List per Textbox
        private void TextBoxSearch_KeyUp(object sender, KeyEventArgs e)
        {
            var filter = webStationList.Where(webStationList => webStationList.StationName.ToLower().Contains(TextBoxSearch.Text));
            ListSelectionWeb.Items.Clear();
            foreach (var station in filter)
            {
                ListSelectionWeb.Items.Add(new WebStations { StationName = station.StationName, BitRate = station.BitRate, StationUrl = station.StationUrl, StationFav = station.StationFav });
            }
        }

		// Method to open "Add Radiostation"-Window
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
				// forward to Method
                AddStationToList(stationName, bitRate, stationUrl, fav);
            }
        }

        // Add Webradio-Station to List
        private void AddStationToList(string stationName, string bitRate, string stationUrl, bool fav)
        {
            // Add Radiostaion
            webStationList.Add(new WebStations { StationName = stationName, BitRate = bitRate, StationUrl = stationUrl, StationFav = fav });
            // sort List
            var orderedStationList = webStationList.OrderBy(x => x.StationName).ToList();
            // update GUI and List<T>
            ListSelectionWeb.Items.Clear();
            webStationList.Clear();
            foreach (var station in orderedStationList)
            {
                webStationList.Add(new WebStations { StationName = station.StationName, BitRate = station.BitRate, StationUrl = station.StationUrl, StationFav = station.StationFav });
                ListSelectionWeb.Items.Add(new WebStations { StationName = station.StationName, BitRate = station.BitRate, StationUrl = station.StationUrl, StationFav = station.StationFav });
            }
            // if "Save as favorite" is checked, update list of favorites
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

        // Track-/ Videoposition controll per Progressbar
        private void ProgressPlayed_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!playing && !folderSelection)
            {
                return;
            }
            // Change Color from Ellipse when pressed
            CanvasPbTime.Children.Clear();
            fillingPressed = new RadialGradientBrush(Color.FromRgb(77, 166, 174), Color.FromRgb(62, 62, 62));
            elliTime.Fill = fillingPressed;
            CanvasPbTime.Children.Add(elliTime);

            double mousePosition = e.GetPosition(ProgressPlayed).X;
            MediaPlayer.Position = TimeSpan.FromSeconds(SetProgressBarValuePlayed(mousePosition));
        }

        private double SetProgressBarValuePlayed(double mP)
        {
            double ratio = mP / ProgressPlayed.ActualWidth ;
            double progressBarValue = ratio * ProgressPlayed.Maximum;
            return progressBarValue;
        }


        // Set Ellipseposition correct on ProgressBar Duration (drag&drop)
        private void ProgressPlayed_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.MouseDevice.LeftButton == MouseButtonState.Pressed)
            {
                if (!folderSelection || onStart)
                {
                    return;
                }
                if (MediaPlayer.HasVideo)
                {
                    if (MediaPlayer.NaturalDuration.TimeSpan.TotalSeconds <= 30)
                    {
                        return;
                    }
                }
                mouseMove = true;
                timer.Stop();
                if (!progressMoving)
                {
                    timerProgressBar.Tick += TimerPrograssBarMoving;
                    timerProgressBar.Start();
                }

                if (Mouse.GetPosition(CanvasPbTime).X >= CanvasPbTime.ActualWidth - elliTime.Width / 2)
                {
                    Canvas.SetLeft(elliTime, CanvasPbTime.ActualWidth - elliTime.Width / 2);
                }

                else if (Mouse.GetPosition(CanvasPbTime).X <= 0)
                {
                    Canvas.SetLeft(elliTime, 0 - elliTime.Width / 2);
                }
                else
                {
                    Canvas.SetLeft(elliTime, Mouse.GetPosition(CanvasPbTime).X - elliTime.Width / 2);
                }
                progressMoving = true;
            }
        }

        // Update ProgressBar Duration to Ellipse-Position
        private void TimerPrograssBarMoving(object sender, EventArgs e)
        {
            LabelCurrentTime.Content = MediaPlayer.Position.ToString(@"hh\:mm\:ss");
            // update Progressbar gespielte Zeit
            if (MediaPlayer.NaturalDuration.HasTimeSpan && !sliderMoving)
            {
                ProgressPlayed.Minimum = 0;
                ProgressPlayed.Maximum = MediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                ProgressPlayed.Value = (Mouse.GetPosition(CanvasPbTime).X / CanvasPbTime.ActualWidth * MediaPlayer.NaturalDuration.TimeSpan.TotalSeconds - elliTime.Width / 2);
                MediaPlayer.Position = TimeSpan.FromSeconds(ProgressPlayed.Value);

                if (Mouse.GetPosition(CanvasPbTime).Y >=20 || Mouse.GetPosition(CanvasPbTime).Y <= 4)
                {
                    Canvas.SetLeft(elliTime, MediaPlayer.Position.TotalSeconds / MediaPlayer.NaturalDuration.TimeSpan.TotalSeconds * CanvasPbTime.ActualWidth - elliTime.Width / 2);
                }
            }
        }

        // Stop update Timer (TimerPrograssBarMoving) and start normal timer again
        private void ProgressPlayed_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!mouseMove)
            {
                return;
            }

            CanvasPbTime.Children.Clear();
            filling = new RadialGradientBrush(Colors.LightGray, Color.FromRgb(198, 198, 198));
            elliTime.Fill = filling;

            timerProgressBar.Stop();
            timer.Start();
            progressMoving = false;
            mouseMove = false;
        }


        // Close Window
        private void CloseMenu_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


		#region ViewOptions
        /// <summary>
        /// Ab hier "View" area for switching Lists on/off
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
		#endregion


		// Method for Button skip backward
        private void ButtonSkipBackward_Click(object sender, EventArgs e)
        {
            if (folderSelection)
            {
                // Shuffle Button active?
                if (playRandom && ListSelectionFolder.Items.Count > 2)
                {
                    ShuffleTrackList();
                    return;
                }

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

		// Method for Button skip forward
        private void ButtonSkipForward_Click(object sender, EventArgs e)
        {
            if (folderSelection)
            {
				// Shuffle-Button active?
                if (playRandom && ListSelectionFolder.Items.Count > 2)
                {
                    ShuffleTrackList();
                    return;
                }

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

		// Methods for Shuffling when active
        private void ShuffleTrackList()
        {
            if (loopOne)
            {
                MediaPlayer.Position = new TimeSpan(0);
                return;
            }

            if (loop)
            {
                if (allreadyPlayed.Count + 1 == ListSelectionFolder.Items.Count)
                {
                    allreadyPlayed.Clear();
                }
            }
            else
            {
                if (allreadyPlayed.Count + 1 == ListSelectionFolder.Items.Count)
                {
                    ButtonStop_Click(new object(), new RoutedEventArgs());
                    allreadyPlayed.Clear();
                    return;
                }
            }

            int trackCount = ListSelectionFolder.Items.Count;

            int rndTrack = rnd.Next(1, trackCount + 1);

            CheckIfAllreadyPlayed(rndTrack);
        }
		// If rndTrack is allready in List<allreadyPlayed> then ShuffleTrackList() is called again,
		// else new Track will be selected over ListSelectionFolder
        private void CheckIfAllreadyPlayed(int rndTrack)
        {
            bool recursive = false;

            foreach (int item in allreadyPlayed)
            {
                if (item == rndTrack)
                {
                    recursive = true;
                }
            }
            if (recursive)
            {
                ShuffleTrackList();
            }
            else
            {
                ListSelectionFolder.SelectedIndex = rndTrack - 1;
                ListSelectionFolder.ScrollIntoView(ListSelectionFolder.SelectedItem);
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
		
		// Method for Language-selection over menu
        private void MenuItem_Lang_Click(object sender, RoutedEventArgs e)
        {
            // Uncheck each item
            foreach (MenuItem item in MenuItemLanguages.Items)
            {
                item.IsChecked = false;
            }

            MenuItem miLang = sender as MenuItem;
            miLang.IsChecked = true;
            //if (CultureInfo.CurrentCulture.Name.Equals(miLang.Tag.ToString()))
            //{
            //    return;
            //}
            App.Instance.SwitchLanguage(miLang.Tag.ToString());

            Sl = new SetLanguages(miLang.Tag.ToString());
            langSelection = miLang.Tag.ToString();
            TextBlockFavListHeader.Text = Sl.FavListHeader;
            SetDataGridHeadersLang();
            SetLoopShuffleLanguage();

            if (playing)
            {
                ImagePause();
            }
            else
            {
                ImagePlay();
            }
        }

        // Set corect language to Loop && Shuffle button
        private void SetLoopShuffleLanguage()
        {
            if (loop && loopOne)
            {
                ButtonLoop.ToolTip = Sl.RepeatOne;
            }
            else if (!loop && !loopOne)
            {
                ButtonLoop.ToolTip = Sl.Repeat;
            }
            else if (loop && !loopOne)
            {
                ButtonLoop.ToolTip = Sl.RepeatAll;

            }

            ButtonShuffle.ToolTip = playRandom ? Sl.ShuffleOn : Sl.ShuffleOff;
        }

        // Method for Style-selection over menu
        private void MenuItem_Style_Click(object sender, RoutedEventArgs e)
        {
            string currentStyle = LightStyle.IsChecked ? LightStyle.ToString() : DarkStyle.ToString();

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

            if (miStyle.ToString() == currentStyle)
            {
                return;
            }
            // String to replace
            string applicationPlace = "pack://application:,,,/SoftwOrt-Mediaplayer;component/";
            // Only send UriKind.Relative to Classes
            string uriSourceLoopAbsolute = ImageLoop.Source.ToString();
            string uriSourceLoop = uriSourceLoopAbsolute.Replace(applicationPlace, "");
            ImageLoop.Source = new BitmapImage(Li.SwitchLoopIcon(uriSourceLoop));

            var uriSourceShuffleAbsolute = ImageShuffle.Source.ToString();
            string uriSourceShuffle = uriSourceShuffleAbsolute.Replace(applicationPlace, "");
            ImageShuffle.Source = new BitmapImage(Si.SwitchShuffleIcon(uriSourceShuffle));
        }
		// Method for Button-selection over menu
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

		// Method to change Buttongraphic
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

		// MEthod to open "About"-Window
        private void AboutMenu_OnClick(object sender, RoutedEventArgs e)
        {
            About about = new About(Top + Height / 2, Left + Width / 2);
            about.Show();
            about.Owner = this;
        }

        // Mute-Method when SoundBoxIcon is clicked
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

        // Loop and Loop1 Method activate || deactivate
        private void ButtonLoop_OnClick(object sender, RoutedEventArgs e)
        {
            if (loop && loopOne)
            {
                if (onStart)
                {
                    ButtonLoop.ToolTip = Sl.RepeatOne;
                    ImageLoop.Source = !DarkStyle.IsChecked
                        ? new BitmapImage(new Uri("icons/loopActiveOne.png", UriKind.Relative))
                        : new BitmapImage(new Uri("icons/loopLightActiveOne.png", UriKind.Relative));
                }
                else
                {
                    loop = false;
                    loopOne = false;
                    ButtonLoop.ToolTip = Sl.Repeat;
                    ImageLoop.Source = !DarkStyle.IsChecked
                        ? new BitmapImage(new Uri("icons/loop.png", UriKind.Relative))
                        : new BitmapImage(new Uri("icons/loopLight.png", UriKind.Relative));
                }
            }
            else if (!loop && !loopOne)
            {
                if (onStart)
                {
                    ButtonLoop.ToolTip = Sl.Repeat;
                    ImageLoop.Source = !DarkStyle.IsChecked
                        ? new BitmapImage(new Uri("icons/loop.png", UriKind.Relative))
                        : new BitmapImage(new Uri("icons/loopLight.png", UriKind.Relative));
                }
                else
                {
                    loop = true;
                    ButtonLoop.ToolTip = Sl.RepeatAll;
                    ImageLoop.Source = !DarkStyle.IsChecked
                        ? new BitmapImage(new Uri("icons/loopActive.png", UriKind.Relative))
                        : new BitmapImage(new Uri("icons/loopLightActive.png", UriKind.Relative));
                }
            }
            else if (loop && !loopOne)
            {
                if (onStart)
                {
                    ButtonLoop.ToolTip = Sl.RepeatAll;
                    ImageLoop.Source = !DarkStyle.IsChecked
                        ? new BitmapImage(new Uri("icons/loopActive.png", UriKind.Relative))
                        : new BitmapImage(new Uri("icons/loopLightActive.png", UriKind.Relative));
                }
                else
                {
                    loopOne = true;
                    ButtonLoop.ToolTip = Sl.RepeatOne;
                    ImageLoop.Source = !DarkStyle.IsChecked
                        ? new BitmapImage(new Uri("icons/loopActiveOne.png", UriKind.Relative))
                        : new BitmapImage(new Uri("icons/loopLightActiveOne.png", UriKind.Relative));
                }
            }
        }

        // Random Method activate || deactivate
        private void ButtonShuffle_OnClick(object sender, RoutedEventArgs e)
        {
            if (playRandom)
            {
                if (onStart)
                {
                    ButtonShuffle.ToolTip = Sl.ShuffleOn;
                    ImageShuffle.Source = !DarkStyle.IsChecked
                        ? new BitmapImage(new Uri("icons/shuffleActive.png", UriKind.Relative))
                        : new BitmapImage(new Uri("icons/shuffleLightActive.png", UriKind.Relative));
                }
                else
                {
                    playRandom = false;
                    ButtonShuffle.ToolTip = Sl.ShuffleOff;
                    ImageShuffle.Source = !DarkStyle.IsChecked
                        ? new BitmapImage(new Uri("icons/shuffle.png", UriKind.Relative))
                        : new BitmapImage(new Uri("icons/shuffleLight.png", UriKind.Relative));
                }
            }
            else
            {
                if (onStart)
                {
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
            allreadyPlayed.Clear();
            onStart = false;
        }

        // If files droped in Player show in ListView
        private void MainWindow_OnDrop(object sender, DragEventArgs e)
        {
            if (!folderSelection)
            {
                if (MessageBox.Show(Sl.DragDropWeb, Sl.DragDropWebTitle, MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    ListSelectionWeb.Visibility = Visibility.Collapsed;
                    ListSelectionWebFav.Visibility = Visibility.Collapsed;
                    GridSplitterWebLists.Visibility = Visibility.Collapsed;
                    StackPanelSearch.Visibility = Visibility.Collapsed;
                    TextBlockFavListHeader.Visibility = Visibility.Collapsed;
                    ListSelectionFolder.Visibility = Visibility.Visible;
                    FolderListMenu.IsChecked = true;
                    FolderListMenu_Click(sender, e);
                    // Prepare GridView
                    folderSelection = true;
                    fileSelection = false;
                    ChkBoxSaveOnExit.IsEnabled = false;
                    ChkBoxSaveOnExit.IsChecked = false;
                    AddWebStationMenu.IsEnabled = false;
                    ViewSettings();
                    MediaPlayer.Source = null;
                }
                else
                {
                    return;
                }
            }
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                int i = 0;
                // store in Array for multidrop
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                string checkPath = files[i].Split('\\').Last();
                string sPathDrop = files[i].Replace(checkPath, "");

                if (!sPath.Equals(sPathDrop))
                {
                    ListSelectionFolder.Items.Clear();
                    dropCount = 1;
                }

                foreach (var fileInfo in files)
                {
                    string tempFileInfo = fileInfo.Split('\\').Last();
                    sPath = fileInfo.Replace(tempFileInfo, "");
                    string playtime = GetFileDuration(sPath + tempFileInfo).ToString((@"hh\:mm\:ss"));
                    ListSelectionFolder.Items.Add(new FolderPick { Number = dropCount.ToString(), FileName = tempFileInfo, PlayTime = playtime });
                    dropCount++;
                }

                fileSelection = false;
                folderSelection = true;
                allreadyPlayed.Clear();
                timerWeb.Stop();
                this.Title = mediaPlayerTitle;
            }
        }

        #endregion
    }
}

