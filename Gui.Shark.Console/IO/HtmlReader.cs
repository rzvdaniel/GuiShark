using System.IO;
using System.Text;

namespace Gui.Shark.Console.IO
{
    public class HtmlReader
    {
        private const string DefaultPath = "..\\Gui.Shark.Console\\Resources\\Default.html";

        public string Read(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                filePath = DefaultPath;
            }

            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                var html = streamReader.ReadToEnd();

                return html;
            }
        }
    }
}
