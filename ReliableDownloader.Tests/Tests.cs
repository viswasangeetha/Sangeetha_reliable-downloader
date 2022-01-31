using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ReliableDownloader.Tests
{
    [TestFixture]
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }
        
        [Test]
        public void InitFileDL()
        {
            var fileDownloader = new FileDownloader();
            Assert.True(fileDownloader != null);
        }

        [Test]
        public void CheckSlowDownload()
        {
            var exampleUrl = "https://installerstaging.accurx.com/chain/3.55.11050.0/accuRx.Installer.Local.msi";
            var exampleFilePath = "C:/Users/Sangeetha.v/myfirstdownload.msi";
            var fileDownloader = new FileDownloader();
            fileDownloader.bytesPerSecond = 100;
            Task.Run(async () => { await fileDownloader.DownloadFile(exampleUrl, exampleFilePath, p => { Console.WriteLine("{0}", p.TotalBytesDownloaded); }); });
        }
    }
}