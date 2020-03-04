using System;

namespace SO_Mediaplayer
{
    public class LoopIcon
    {
        private static readonly string loop = "loop.png";
        private static readonly string loopActive = "loopActive.png";
        private static readonly string loopActiveOne = "loopActiveOne.png";
        private static readonly string loopLight = "loopLight.png";
        private static readonly string loopLightActive = "loopLightActive.png";
        private static readonly string loopLightActiveOne = "loopLightActiveOne.png";

        private string correctLoopIcon;

        private readonly string[] getSetIcon = { icons + loop, icons + loopActive, icons + loopActiveOne };
        private readonly string[] getSetIconLight = { icons + loopLight, icons + loopLightActive, icons + loopLightActiveOne };

        private static readonly string icons = "icons/";

        public Uri SwitchLoopIcon(string currentIcon)
        {
            for (int i = 0; i < getSetIcon.Length ; i++)
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
