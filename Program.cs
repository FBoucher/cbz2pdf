
using System.IO.Compression;
using System.Text.RegularExpressions;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Drawing;

string folderpath = "/home/frank/dev/github/fboucher/cbz2pdf/data";

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
GetCBZs(folderpath);




void GetCBZs(string folderpath){
    //var files = Directory.GetFiles(folderpath, "*.cbz");
    var files = Directory.GetFiles(folderpath).Where(file => Regex.IsMatch(file, @"^.+\.(cbz|cbr)$"));
    foreach (var file in files)
    {
        Console.WriteLine($"- {file}");
        
        string destFolder = Decompress(file);
        CreatePDF(destFolder);
    }
}

string Decompress(string filePath)
{
    FileInfo fileInfo = new FileInfo(filePath);
    string destination = Path.GetFileNameWithoutExtension(filePath);
    using (FileStream fileStream = fileInfo.OpenRead())
    {
        ZipFile.ExtractToDirectory(fileStream, destination, true);
    }
    return destination;
}


 void CreatePDF(string folder)
{
    var pdfFilename = folder + ".pdf";
    if (File.Exists(pdfFilename))
        File.Delete(pdfFilename);

    var justName = Path.GetFileNameWithoutExtension(folder);
    var document = new PdfDocument();
    document.Info.Title = justName;
    

    var files = Directory.GetFiles(folder).OrderBy(f => f).ToArray<string>();
    foreach (var file in files)
    {
        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        string filePath = Path.GetFullPath(file);

        var fileInfo = new FileInfo(filePath);

        // doesn't work with jpg
        using (FileStream fileStream = fileInfo.OpenRead())
        {
            var image = XImage.FromStream(fileStream);
            gfx.DrawRectangle(XBrushes.Black, new XRect(0, 0, page.Width.Point, page.Height.Point));
            gfx.DrawImage(image, 0, 0);
        }
        //var image = XImage.FromFile(filePath);


    }

    document.Save(pdfFilename);
}