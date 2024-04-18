using Windows.Storage.Streams;
using Windows.Storage;
using UglyToad.PdfPig;
using NativePDF = Windows.Data.Pdf;
using UglyToad.PdfPig.Graphics.Colors;
using UglyToad.PdfPig.Rendering.Skia;

namespace PdfPig.Rendering.Skia.NativeTest;

[TestClass]
public class UnitTest1
{
    private const int _scale = 2;
    private const string _outputPath = "Output";

    public static IEnumerable<object[]> GetAllDocuments()
    {
        return Directory.EnumerateFiles("Documents", "*.pdf")
            .Select(Path.GetFileName)
            .Where(p => !p.EndsWith("GHOSTSCRIPT-699178-0.pdf")) // Seems to be an issue with PdfPig
            .Select(p => new object[] { p });
    }

    [TestInitialize]
    public void Initialize()
    {
        Directory.CreateDirectory(_outputPath);
    }

    [TestMethod]
    [DynamicData(nameof(GetAllDocuments), DynamicDataSourceType.Method)]
    public void RenderToFolderSkia(string docPath)
    {
        string rootName = Path.GetFileNameWithoutExtension(docPath);
        Directory.CreateDirectory(Path.Combine(_outputPath, $"{rootName}"));

        using var document = PdfDocument.Open(Path.Combine("Documents", docPath));
        document.AddSkiaPageFactory();

        for (int p = 1; p <= document.NumberOfPages; p++)
        {
            using var fs = new FileStream(Path.Combine(_outputPath, $"{rootName}", $"{rootName}_skia_{p}.png"),
                FileMode.Create);
            using var ms = document.GetPageAsPng(p, _scale, RGBColor.White);
            if (docPath == "22060_A1_01_Plans-1.pdf")
            {
                var page = document.GetPage(p);
                var text = page.Text;
            }

            // using var skpic = document.GetPage<SKPicture>(p);
            ms.WriteTo(fs);
        }
    }


    [TestMethod]
    [DynamicData(nameof(GetAllDocuments), DynamicDataSourceType.Method)]
    public void RenderToFolderNative(string docPath)
    {
        string rootName = Path.GetFileNameWithoutExtension(docPath);

        Directory.CreateDirectory(Path.Combine(_outputPath, $"{rootName}"));


        var absolutePath = Path.GetFullPath(Path.Combine(@"Documents", docPath));
        var storageFile = StorageFile.GetFileFromPathAsync(absolutePath).AsTask().Result;
        //
        var document = NativePDF.PdfDocument.LoadFromFileAsync(storageFile).AsTask().Result;

        for (var i = 0; i < document.PageCount; ++i)
        {
            var page = document.GetPage((uint)i);

            var renderOptions = new NativePDF.PdfPageRenderOptions
            {
                DestinationHeight = (uint)page.Size.Height,
                DestinationWidth = (uint)page.Size.Width,

            };

            var outputStream = new InMemoryRandomAccessStream();
            page.RenderToStreamAsync(outputStream, renderOptions).AsTask().Wait();

            var file = File.OpenWrite(Path.Combine(_outputPath, $"{rootName}", $"{rootName}_native_{i}.png"));
            outputStream.AsStreamForRead().CopyTo(file);

        }
    }


}
