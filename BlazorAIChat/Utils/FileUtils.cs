using System.IO;
using System.Text;

namespace BlazorAIChat.Utils
{
    public static class FileUtils
    {
        public static string GetMimeTypeFromImage(Stream stream)
        {
            stream.Position = 0;

            var bmp = Encoding.ASCII.GetBytes("BM");     // BMP
            var gif = Encoding.ASCII.GetBytes("GIF");    // GIF
            var png = new byte[] { 137, 80, 78, 71 };    // PNG
            var tiff = new byte[] { 73, 73, 42 };         // TIFF
            var tiff2 = new byte[] { 77, 77, 42 };         // TIFF
            var jpeg = new byte[] { 255, 216, 255, 224 }; // jpeg
            var jpeg2 = new byte[] { 255, 216, 255, 225 }; // jpeg canon

            var buffer = new byte[4];
            stream.Read(buffer, 0, buffer.Length);

            stream.Position = 0;

            if (bmp.SequenceEqual(buffer.Take(bmp.Length)))
                return "image/bmp";

            if (gif.SequenceEqual(buffer.Take(gif.Length)))
                return "image/gif";

            if (png.SequenceEqual(buffer.Take(png.Length)))
                return "image/png";

            if (tiff.SequenceEqual(buffer.Take(tiff.Length)))
                return "image/tiff";

            if (tiff2.SequenceEqual(buffer.Take(tiff2.Length)))
                return "image/tiff";

            if (jpeg.SequenceEqual(buffer.Take(jpeg.Length)))
                return "image/jpeg";

            if (jpeg2.SequenceEqual(buffer.Take(jpeg2.Length)))
                return "image/jpeg";

            return string.Empty;
        }

        public static string GetIconForFileType(string filename)
        {


            if (filename.EndsWith(".pdf"))
                return "/images/pdf_256x256.png";

            if (filename.EndsWith(".docx"))
                return "/images/word_256x256.png";

            if (filename.EndsWith(".xlsx"))
                return "/images/excel_256x256.png";

            if (filename.EndsWith(".pptx"))
                return "/images/powerpoint_256x256.png";

            if (filename.EndsWith("txt"))
                return "/images/txt_256x256.png";

            return string.Empty;
        }
    }
}
