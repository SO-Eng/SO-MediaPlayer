using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SO_Mediaplayer.WebFileProcessor
{
    public static class LoadSavePath
    {
        public static string SetFilePath(string fileName)
        {
            string companyPath = @"\SoftwOrt\";
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            return folderPath + companyPath + fileName;
        }
    }
}
