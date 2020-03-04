using System;

namespace SO_Mediaplayer
{
    class ShuffleIcon
    {
        private static string shuffle = "shuffle.png";
        private static string shaffleActive = "shuffleActive.png";
        private static string shuffleLight = "shuffleLight.png";
        private static string shuffleLightActive = "shuffleLightActive.png";

        private string correctLoopIcon;

        private readonly string[] getSetIcon = { icons + shuffle, icons + shaffleActive };
        private readonly string[] getSetIconLight = { icons + shuffleLight, icons + shuffleLightActive };

        private static readonly string icons = "icons/";

        public Uri SwitchShuffleIcon(string currentIcon)
        {
            for (int i = 0; i < getSetIcon.Length; i++)
            {
                if (getSetIcon[i] == currentIcon)
                {
                    correctLoopIcon = getSetIconLight[i];
                }
            }

            for (int i = 0; i < getSetIconLight.Length; i++)
            {
                if (getSetIconLight[i] == currentIcon)
                {
                    correctLoopIcon = getSetIcon[i];

                }
            }

            return new Uri(correctLoopIcon, UriKind.Relative);
        }

    }
}
