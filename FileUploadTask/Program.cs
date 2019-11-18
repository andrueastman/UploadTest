using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace FileUploadTask
{
    class Program
    {
        static async Task Main()
        {
            /* Do the auth stuff first */
            IPublicClientApplication publicClientApplication = PublicClientApplicationBuilder
                .Create("d662ac70-7482-45af-9dc3-c3cde8eeede4")
                .WithRedirectUri("http://localhost:1234")
                .Build();

            var scopes = new[] { "User.Read", "Mail.ReadWrite" };
            var authResult = await publicClientApplication.AcquireTokenInteractive(scopes).ExecuteAsync();

            /* Create a DelegateAuthenticationProvider to use */
            var delegatingAuthProvider = new DelegateAuthenticationProvider((requestMessage) =>
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", authResult.AccessToken);
                return Task.FromResult(0);
            });
            var graphClient = new GraphServiceClient(delegatingAuthProvider);

            /* Look for a valid item path to use in the drive */
            var driveItems = await graphClient.Drive.Root.Children.Request().GetAsync();
            string id = "";
            
            //find the first item that is a file.
            foreach (var item in driveItems)
            {
                if (item.File == null)
                {
                    id = item.Id;
                    break;
                }
            }

            // Upload the large file
            //await UploadLargeFileWithCallBacks(graphClient, id);
            // Upload the large file
            //await UploadLargeFileInSlices(graphClient, id);

            await UploadLargeAttachmentInSlices(graphClient, id);
        }

        /// <summary>
        /// Upload a large file using callbacks
        /// </summary>
        /// <param name="graphClient">Client for upload</param>
        /// <param name="itemId">itemId for upload</param>
        /// <returns></returns>
        public static async Task UploadLargeFileWithCallBacks(GraphServiceClient graphClient, string itemId)
        {
            try
            {
                using Stream stream = GetFileStream();

                // POST /v1.0/drive/items/01KGPRHTV6Y2GOVW7725BZO354PWSELRRZ:/SWEBOKv3.pdf:/microsoft.graph.createUploadSession
                var uploadSession = await graphClient.Drive.Items[itemId].ItemWithPath("SWEBOK1.pdf").CreateUploadSession().Request().PostAsync();
                Console.WriteLine("Upload Session Created");

                var maxSliceSize = 320 * 1024; // 320 KB - Change this to your chunk size. 5MB is the default.
                LargeFileUploadTask<DriveItem> largeFileUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, graphClient, stream, maxSliceSize);

                // Setup the chunk request necessities
                UploadResult<DriveItem> uploadResult = null;

                IProgress<long> progress = new Progress<long>(progress =>
                {
                    Console.WriteLine($"Uploaded {progress} bytes of {stream.Length} bytes");
                });

                try
                {
                    uploadResult = await largeFileUploadTask.UploadAsync(progress);
                }
                catch (TaskCanceledException)
                {
                    //try to refresh the upload info and resume the upload from where we left off.
                    Console.WriteLine("Resuming Download");
                    uploadResult = await largeFileUploadTask.ResumeAsync(progress);
                }

                //Sucessful Upload
                if (uploadResult.ItemResponse != null)
                {
                    Console.WriteLine($"File Uploaded {uploadResult.ItemResponse.Id}");
                }
                else 
                {
                    Console.WriteLine("Upload Failed");
                }
            }
            catch (ServiceException e)
            {
                Console.WriteLine(e.Message);
            }
            //Sucessful Upload
        }


        public static async Task UploadLargeFileInSlices(GraphServiceClient graphClient, string itemId)
        {
            try
            {
                using Stream stream = GetFileStream();
                // Create upload session 
                // POST /v1.0/drive/items/01KGPRHTV6Y2GOVW7725BZO354PWSELRRZ:/SWEBOKv3.pdf:/microsoft.graph.createUploadSession
                var uploadSession = await graphClient.Drive.Items[itemId].ItemWithPath("SWEBOK.pdf").CreateUploadSession().Request().PostAsync();

                // Create task
                var maxSliceSize = 320 * 1024; // 320 KB - Change this to your chunk size. 5MB is the default.
                LargeFileUploadTask<DriveItem> largeFileUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, graphClient, stream, maxSliceSize);

                // Setup the chunk request necessities
                var slicesRequests = largeFileUploadTask.GetUploadSliceRequests();
                var trackedExceptions = new List<Exception>();
                DriveItem itemResult = null;

                //upload the chunks
                foreach (var request in slicesRequests)
                {
                    // Send chunk request
                    var result = await largeFileUploadTask.UploadSliceAsync(request, trackedExceptions);
                    // Do your updates here: update progress bar, etc.
                    Console.WriteLine($"File uploading in progress. {request.RangeEnd} of {stream.Length} bytes uploaded");

                    if (result.UploadSucceeded)
                    {
                        itemResult = result.ItemResponse;
                        Console.WriteLine($"File uploading complete");
                    }
                }
                // Check that upload succeeded
                if (itemResult == null)
                {
                    //Upload failed
                    Console.WriteLine("Upload failed");
                }
            }
            catch (ServiceException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public static async Task UploadLargeAttachmentInSlices(GraphServiceClient graphClient, string itemId)
        {
            try
            {
                
                // Create upload session 
                // POST /v1.0/drive/items/01KGPRHTV6Y2GOVW7725BZO354PWSELRRZ:/SWEBOKv3.pdf:/microsoft.graph.createUploadSession
                String uri = "https://graph.microsoft.com/beta/me/messages/AAMkADcyMWRhMWZmLTFlMjUtNGNmMS1hNGMwLTgwZjc4YTM1OGJkMABGAAAAAABQGcMSxjfySL9jI-9KPCamBwCHxMu3VWSLRbuEFlqpOoZNAAAAAAEMAACHxMu3VWSLRbuEFlqpOoZNAABOcwnyAAA=/attachments/createUploadSession";
                HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
                httpRequestMessage.Content = new StringContent("{\"AttachmentItem\": {\"attachmentType\": \"file\",\"name\": \"flower\",\"size\": 3483322 }}", Encoding.UTF8, "application/json");
                await graphClient.AuthenticationProvider.AuthenticateRequestAsync(httpRequestMessage);

                var httpResponseMessage = await graphClient.HttpProvider.SendAsync(httpRequestMessage);
                var content = await httpResponseMessage.Content.ReadAsStringAsync();
                
                var uploadSession = graphClient.HttpProvider.Serializer.DeserializeObject<UploadSession>(content);

                using (Stream stream = GetFileStream())
                {
                    // Create task
                    var maxSliceSize = 320 * 1024; // 320 KB - Change this to your chunk size. 5MB is the default.
                    LargeFileUploadTask<FileAttachment> largeFileUploadTask = new LargeFileUploadTask<FileAttachment>(uploadSession, graphClient, stream, maxSliceSize);

                    // Setup the chunk request necessities
                    var slicesRequests = largeFileUploadTask.GetUploadSliceRequests();
                    var trackedExceptions = new List<Exception>();

                    FileAttachment itemResult = null;

                    //upload the chunks
                    foreach (var request in slicesRequests)
                    {
                        request.Method = "PUT";
                        byte[] readbuffer = new byte[maxSliceSize];
                        stream.Read(readbuffer, (int)request.RangeBegin, maxSliceSize);
                        var memoryStream = new MemoryStream(readbuffer);
                        var uploadRequest = request.GetHttpRequestMessage();
                        // Send chunk request
                        uploadRequest.Content = new StreamContent(memoryStream);
                        uploadRequest.Content.Headers.ContentRange = new ContentRangeHeaderValue(request.RangeBegin, request.RangeEnd, request.TotalSessionLength);
                        uploadRequest.Content.Headers.ContentLength = request.RangeLength;
                        uploadRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                        //var result = await largeFileUploadTask.UploadSliceAsync(request, trackedExceptions);
                        HttpClient httpClient = new HttpClient();
                        var streamResponseMessage = await httpClient.SendAsync(uploadRequest);
                        //var streamResponseMessage = await graphClient.HttpProvider.SendAsync(uploadRequest);
                        // Do your updates here: update progress bar, etc.
                        Console.WriteLine($"File uploading in progress. {request.RangeEnd} of {stream.Length} bytes uploaded");
                        var response = await streamResponseMessage.Content.ReadAsStringAsync();
                        Console.WriteLine(response);
                        Console.WriteLine("");
                        if (false)
                        {
                            //itemResult = result.ItemResponse;
                            Console.WriteLine($"File uploading complete");
                        }
                    }
                    // Check that upload succeeded
                    if (itemResult == null)
                    {
                        //Upload failed
                        Console.WriteLine("Upload failed");
                    }
                }
 
            }
            catch (ServiceException e)
            {
                Console.WriteLine(e.Message);
            }
        }
        /// <summary>
        /// Read a file present in the project for uploading
        /// </summary>
        /// <returns></returns>
        private static Stream GetFileStream()
        {
            string startupPath = Environment.CurrentDirectory;
            FileStream fileStream = new FileStream(startupPath + "\\SWEBOKv3.pdf", FileMode.Open);
            return fileStream;
        }
    }
}
