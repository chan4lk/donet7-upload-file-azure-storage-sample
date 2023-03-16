using System.IO;
using System.Net;
using System.Threading;
using Azure.Storage.Blobs;
using HttpMultipartParser;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FunctionApp2
{
    public class Function1
    {
        private readonly ILogger _logger;
        private readonly IConfiguration configuration;

        public Function1(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<Function1>();
            this.configuration = configuration;
        }

        [Function("Function1")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            var parser = await MultipartFormDataParser.ParseAsync(req.Body).ConfigureAwait(false);

            var file = parser.Files.First();
            string filename = file.FileName;
            Stream content = file.Data;

            var parmacyName = parser.GetParameterValue("ParmacyName");
            var imagePath = $"{parmacyName}/{filename}";

            try
            {
                BlobContainerClient blobContainerClient = new BlobContainerClient(
                    configuration.GetValue<string>("storage:conn"), 
                    configuration.GetValue<string>("storage:container"));

                blobContainerClient.CreateIfNotExists(Azure.Storage.Blobs.Models.PublicAccessType.Blob);

                await blobContainerClient.GetBlobClient(imagePath).DeleteIfExistsAsync();

                var info = await blobContainerClient.UploadBlobAsync(imagePath, content);

                var url= $"{blobContainerClient.Uri.AbsoluteUri}/{imagePath}";
                response.WriteString(url);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }

            return response;
        }
    }
}
