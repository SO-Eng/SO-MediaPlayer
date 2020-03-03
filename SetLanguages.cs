namespace SO_Mediaplayer
{
    public class SetLanguages
    {
        #region Fields
        public string Play { get; set; }
        public string MenuPlay { get; set; }
        public string Pause { get; set; }
        public string MenuPause { get; set; }
        public string Stop { get; set; }
        public string Prelisten { get; set; }
        public string ErrorLoad { get; set; }
        public string MsgBoxInfo { get; set; }
        public string ErrorPlay { get; set; }
        public string AddStationInfo { get; set; }
        public string Search { get; set; }
        public string FavListHeader { get; set; }
        public string WebListFav { get; set; }
        public string WebListStationName { get; set; }
        public string WebListBitrate { get; set; }
        public string WebListWebUrl { get; set; }
        public string Repeat { get; set; }
        public string RepeatAll { get; set; }
        public string RepeatOne { get; set; }
        public string ShuffleOff { get; set; }
        public string ShuffleOn { get; set; }

        #endregion


        #region Methods
        public SetLanguages(string lang)
        {
            GetLanguageInformation(lang);
        }

        private void GetLanguageInformation(string lang)
        {
            switch (lang)
            {
                case "en-US":
                    SetEnglish();
                    break;
                case "de-DE":
                    SetGerman();
                    break;
                case "es-ES":
                    SetSpanish();
                    break;
                default:
                    SetEnglish();
                    break;
            }
        }

        private void SetEnglish()
        {
            Play = "Play";
            MenuPlay = "_Play";
            Pause = "Pause";
            MenuPause = "_Pause";
            Stop = "Stop playback";
            Prelisten = "Prelisten";
            ErrorLoad = "Try only load Audio or Video files";
            MsgBoxInfo = "Information";
            ErrorPlay = "Unfortunately, this Radiostation does not seem to be available...";
            AddStationInfo = "Add your favourite station to the list.\nIf you save the list, the station will also be available to you in the future!";
            Search = "Search";
            FavListHeader = "List of favourites";
            WebListFav = "Favs.";
            WebListStationName = "Station Name";
            WebListBitrate = "Bitrate";
            WebListWebUrl = "Web-URL";
            Repeat = "Repeat off";
            RepeatAll = "Repeat all";
            RepeatOne = "Repeat one";
            ShuffleOff = "Shuffle off";
            ShuffleOn = "Shuffle on";
        }


        private void SetGerman()
        {
            Play = "Wiedergeben";
            MenuPlay = "_Wiedergeben";
            Pause = "Pause";
            MenuPause = "_Pause";
            Stop = "Wiedergabe stoppen";
            Prelisten = "Vorhören";
            ErrorLoad = "Bitte versuchen Sie nur Audio- oder Videodateien zu laden";
            MsgBoxInfo = "Information";
            ErrorPlay = "Diese WebStation scheint leider nicht erreichbar zu sein...";
            AddStationInfo = "Fügen Sie Ihren Lieblingssender zur Liste hinzu.\nWenn Sie die Liste speichern, wird der Sender Ihnen auch in Zukuft zur verfügung stehen!";
            Search = "Suchen";
            FavListHeader = "Favoriten-Liste";
            WebListFav = "Fav.";
            WebListStationName = "Stations Name";
            WebListBitrate = "Bitrate";
            WebListWebUrl = "Web-URL";
            Repeat = "Wiederholen aus";
            RepeatAll = "Alle wiederholen";
            RepeatOne = "Einzelstück wiederholen";
            ShuffleOff = "Zufällig aus";
            ShuffleOn = "Zufällig an";
        }

        private void SetSpanish()
        {
            Play = "Reproducir";
            MenuPlay = "_Reproducir";
            Pause = "Pausar";
            MenuPause = "_Pausar";
            Stop = "Detener la reproducción";
            Prelisten = "Vista previa";
            ErrorLoad = "Por favor, intenta cargar sólo archivos de audio o vídeo";
            MsgBoxInfo = "Información";
            ErrorPlay = "Esta estación de radio no parece estar disponible...";
            AddStationInfo = "Añade tu estación favorita a la lista.\n¡Si guardas la lista, la estación también estará disponible en el futuro!";
            Search = "Busca en";
            FavListHeader = "Lista de favoritos";
            WebListFav = "Favs.";
            WebListStationName = "Nombre de la estación";
            WebListBitrate = "Bitrate";
            WebListWebUrl = "URL de la web";
            Repeat = "Repetir apagar";
            RepeatAll = "Repita todo";
            RepeatOne = "Repita la pieza única";
            ShuffleOff = "Al azar de";
            ShuffleOn = "Al azar en";
        }

        #endregion
    }
}