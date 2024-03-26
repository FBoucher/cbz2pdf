using System.IO.Compression;
using System.Text.RegularExpressions;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;
using Microsoft.Extensions.Configuration;

IConfigurationRoot config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();
Settings? settings = config.GetRequiredSection("Settings").Get<Settings>();

var skippedFiles = new List<string>();
ProcessFolder(settings.RootFolder);

if(skippedFiles.Count > 0){
    string sfiles = Path.Combine(settings.RootFolder, "skippedFiles.txt");
    Console.WriteLine($"\n\n{skippedFiles.Count} files were skipped.\n\nThe complete list can be found in {sfiles}");
    File.WriteAllLines(sfiles, skippedFiles);
}


void ProcessFolder(string folderpath){
    
    Console.WriteLine("- Looking for CBZs and CBRs in " + folderpath);
    GetCBZs(folderpath);
    var subFolders =  Directory.GetDirectories(folderpath);
    Console.WriteLine("_");

    foreach (var subFolder in subFolders)
    {
        ProcessFolder(subFolder);
    }
}

void GetCBZs(string folderpath)
{
    var files = Directory.GetFiles(folderpath).Where(file => Regex.IsMatch(file, @"^.+\.(cbz|cbr)$"));

    foreach (var file in files)
    {
        try{
            Console.WriteLine($".. {file}");
            string destFolder = string.Empty;   

            if (file.EndsWith(".cbz"))
            {
                destFolder = DecompressCBZ(file);
            }
            else
            {
                destFolder = DecompressCBR(file);
            }
            CreatePDF(destFolder);
        }
        catch(Exception ex){
            Console.WriteLine($"!! {file}");
            skippedFiles.Add(file);
        }
        
    }
}



string DecompressCBR(string filePath)
{
    FileInfo fileInfo = new FileInfo(filePath);
    string destination = Path.Combine(fileInfo.DirectoryName, Path.GetFileNameWithoutExtension(filePath));
    Directory.CreateDirectory(Path.Combine(fileInfo.DirectoryName, destination));

    using (var archive = RarArchive.Open(fileInfo.FullName))
    {
        foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
        {
            entry.WriteToDirectory(destination, new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
        }
    }
    return destination;

}

string DecompressCBZ(string filePath)
{
    FileInfo fileInfo = new FileInfo(filePath);
    string destination = Path.Combine(fileInfo.DirectoryName, Path.GetFileNameWithoutExtension(filePath));
    Directory.CreateDirectory(Path.Combine(fileInfo.DirectoryName, destination));

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

    var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories).OrderBy(f => f).Where(file => Regex.IsMatch(file, @"^.+\.(jpeg|jpg|png)$")).ToArray<string>();

    foreach (var file in files)
    {
        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        string filePath = Path.GetFullPath(file);

        var fileInfo = new FileInfo(filePath);

        try
        {
            using (FileStream fileStream = fileInfo.OpenRead())
            {
                var image = XImage.FromStream(fileStream);
                gfx.DrawRectangle(XBrushes.Black, new XRect(0, 0, page.Width.Point, page.Height.Point));
                gfx.DrawImage(image, 0, 0, page.Width.Point, page.Height.Point);
            }            
        }
        catch (Exception ex)
        {
            string logMsg = $"! {fileInfo.FullName} --> {ex.Message}";
            skippedFiles.Add(logMsg);
            throw;
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
    document.Save(pdfFilename);
    
}