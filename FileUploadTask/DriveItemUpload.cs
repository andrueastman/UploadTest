using Microsoft.Graph;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace FileUploadTask
{
    public static class DriveItemUpload
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="graphClient"></param>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private static async Task<IUploadSession> CreateDriveItemUploadSession(IBaseClient graphClient, string itemId)
        {
            // POST /v1.0/drive/items/01KGPRHTV6Y2GOVW7725BZO354PWSELRRZ:/SWEBOKv3.pdf:/microsoft.graph.createUploadSession
            string uri = $"https://graph.microsoft.com/v1.0/drive/items/{itemId}:/SWEBOKv3.pdf:/microsoft.graph.createUploadSession";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
            await graphClient.AuthenticationProvider.AuthenticateRequestAsync(httpRequestMessage);

            // Read the session info from the response
            var httpResponseMessage = await graphClient.HttpProvider.SendAsync(httpRequestMessage);
            var content = await httpResponseMessage.Content.ReadAsStringAsync();
            var uploadSession = graphClient.HttpProvider.Serializer.DeserializeObject<UploadSession>(content);

            Console.WriteLine("Upload Session Created");

            return uploadSession;
        }

        /// <summary>
        /// Upload a large file using callbacks
        /// </summary>
        /// <param name="graphClient">Client for upload</param>
        /// <param name="itemId">itemId for upload</param>
        /// <returns></returns>
        public static async Task UploadLargeFileWithCallBacks(IBaseClient graphClient, string itemId)
        {
            await using Stream stream = Program.GetFileStream();

            // POST /v1.0/drive/items/01KGPRHTV6Y2GOVW7725BZO354PWSELRRZ:/SWEBOKv3.pdf:/microsoft.graph.createUploadSession
            var uploadSession = await CreateDriveItemUploadSession(graphClient,itemId);

            // Create task
            var maxSliceSize = 320 * 1024; // 320 KB - Change this to your chunk size. 5MB is the default.
            LargeFileUploadTask<DriveItem> largeFileUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, stream, maxSliceSize);

            // Setup the progress monitoring
            IProgress<long> progress = new Progress<long>(progressCallBack =>
            {
                Console.WriteLine($"Uploaded {progressCallBack} bytes of {stream.Length} bytes");
            });

            try
            {
                var uploadResult = await largeFileUploadTask.UploadAsync(progress);

                if (uploadResult.UploadSucceeded)
                {
                    Console.WriteLine($"File Uploaded {uploadResult.ItemResponse.Id}");//Successful Upload
                }
            }
            catch (ServiceException e)
            {
                Console.WriteLine("Something went wrong with the upload");
                Console.WriteLine(e.Message);
            }

        }
    }
}
