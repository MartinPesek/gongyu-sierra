using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Dropbox.Api;
using Dropbox.Api.Files;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Web.Models;
using Web.Services;

namespace Web.Controllers
{
    public class UploadController : Controller
    {
        private static CloudBlobContainer _storageService;
        private static DropboxClient _dropboxClient;

        public UploadController(IConfiguration configuration)
        {
            var storageAccount = CloudStorageAccount.Parse(configuration["AZURE-STORAGE-CONNECTION-STRING"]);
            var storageClient = storageAccount.CreateCloudBlobClient();

            _storageService = storageClient.GetContainerReference("ofs");
            _storageService.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Blob, null, null);

            _dropboxClient = new DropboxClient(configuration["DROPBOX-ACCESS-KEY"], new DropboxClientConfig("gongyu-sierra/1.0"));
        }

        [Route("")]
        public IActionResult Index()
        {
            return View();
        }

        [Route("save")]
        public IActionResult Save(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return new BadRequestObjectResult("No data received.");
            }

            string resultUrl;
            if (data.StartsWith("http"))
            {
                resultUrl = SaveUrlToFile(data).Result;
            }
            else
            {
                resultUrl = SaveBase64ToFile(data);
            }

            return new OkObjectResult(resultUrl);
        }

        private string SaveBase64ToFile(string data)
        {
            var mimeTypeBase64 = data.Split(';');

            string base64Data = null;
            string extension = null;
            string mimeType = null;

            foreach (var s in mimeTypeBase64)
            {
                var colonIndex = s.IndexOf(':');
                if (colonIndex == -1)
                {
                    if (s.StartsWith("base64,", StringComparison.InvariantCultureIgnoreCase))
                    {
                        base64Data = s.Substring(7).Replace(' ', '+');
                    }

                    continue;
                }

                var header = s.Split(':');
                if (!"data".Equals(header[0], StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                mimeType = header[1];
                extension = GetExtensionFromMimeType(mimeType);
            }

            // unable to parse the data, do nothing
            if (string.IsNullOrEmpty(base64Data) || string.IsNullOrEmpty(extension))
            {
                // TODO: add error log: Unable to parse base64 data.
                return null;
            }

            var rawData = Convert.FromBase64String(base64Data);
            return SaveFile(rawData, mimeType).Result;
        }

        private async Task<string> SaveUrlToFile(string url)
        {
            var resultUrl = string.Empty;

            using (var client = new HttpClient())
            {
                // TODO: add some error handling (HttpRequestException)
                // TODO: check if downloading exceeds max. file size

                using (var result = await client.GetAsync(url))
                {
                    if (result.IsSuccessStatusCode)
                    {
                        var rawData = await result.Content.ReadAsByteArrayAsync();
                        resultUrl = SaveFile(rawData, result.Content.Headers.ContentType.ToString()).Result;
                    }
                }
            }

            return resultUrl;
        }

        private async Task<string> SaveFile(byte[] rawData, string mimeType)
        {
            var extension = GetExtensionFromMimeType(mimeType);
            var filename = StringGenerator.GetRandomString() + extension;

            var uploadToAzureStorage = UploadToAzureStorage(rawData, mimeType, filename);

            // TODO: dropbox uploads should be done in background because user does not need to wait for it (IHostedService?)
#pragma warning disable 4014
            Task.Run(async () => await UploadToDropbox(rawData, filename));
#pragma warning restore 4014

//            await Task.WhenAll(uploadToAzureStorage, uploadToDropbox);

            var resultUrl = await uploadToAzureStorage;

            return resultUrl;
        }

        private async Task<string> UploadToAzureStorage(byte[] rawData, string mimeType, string filename)
        {
            var blob = _storageService.GetBlockBlobReference(filename);
            await blob.UploadFromByteArrayAsync(rawData, 0, rawData.Length);

            blob.Properties.ContentType = mimeType;
            await blob.SetPropertiesAsync();

            return blob.Uri.ToString();
        }

        private async Task UploadToDropbox(byte[] rawData, string filename)
        {
            var dropboxFilename = DateTime.Now.ToString("yyyy-MM-dd") + "_" + filename;
            await _dropboxClient.Files.UploadAsync("/ofs/" + dropboxFilename, WriteMode.Add.Instance,
                body: new MemoryStream(rawData));
        }

        private static string GetExtensionFromMimeType(string mimeType)
        {
            switch (mimeType)
            {
                case "image/png":
                    return ".png";

                case "image/jpg":
                case "image/jpeg":
                    return ".jpg";

                case "image/gif":
                    return ".gif";

                default:
                    throw new InvalidOperationException();
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }
}