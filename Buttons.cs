using System;
using System.Text;

namespace SO_Mediaplayer
{
    public class Buttons
    {
        private readonly string icons;

        public Buttons()
        {
            icons = "icons/";
        }

        public Uri SetButtonSkipBackward(string folderName)
        {
            return new Uri(icons + folderName + "/" + "skip-backward.png", UriKind.Relative);
        }

        public Uri SetButtonSkipForward(string folderName)
        {
            return new Uri(icons + folderName + "/" + "skip-forward.png", UriKind.Relative);
        }

        public Uri SetButtonStop(string folderName)
        {
            return new Uri(icons + folderName + "/" + "stop.png", UriKind.Relative);
        }

        public Uri SetButtonPlay(string folderName)
        {
            return new Uri(icons + folderName + "/" + "play.png", UriKind.Relative);
        }

        public Uri SetButtonPause(string folderName)
        {
            return new Uri(icons + folderName + "/" + "pause.png", UriKind.Relative);
        }

        public Uri SetButtonBackward(string folderName)
        {
            return new Uri(icons + folderName + "/" + "backward.png", UriKind.Relative);
        }

        public Uri SetButtonForward(string folderName)
        {
            return new Uri(icons + folderName + "/" + "forward.png", UriKind.Relative);
        }
    }
}
