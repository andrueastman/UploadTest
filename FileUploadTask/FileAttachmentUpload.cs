using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FileUploadTask
{
    public class FileAttachmentUpload
    {
        private static async Task<IUploadSession> CreateFileAttachementUploadSession(IBaseClient graphClient, string messageId, long streamLength)
        {
            // Create upload session only works on beta endpoint for now
            // POST /beta/me/messages/{message-id}/attachments/createUploadSession
            string uri = $"https://graph.microsoft.com/beta/me/messages/{messageId}/attachments/createUploadSession";
            string attachementInfo = $"{{\"AttachmentItem\": {{\"attachmentType\": \"file\",\"name\": \"flower\",\"size\": {streamLength} }}}}";
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(attachementInfo, Encoding.UTF8, "application/json")
            };
            await graphClient.AuthenticationProvider.AuthenticateRequestAsync(httpRequestMessage);

            // Read the session info from the response
            var httpResponseMessage = await graphClient.HttpProvider.SendAsync(httpRequestMessage);
            var content = await httpResponseMessage.Content.ReadAsStringAsync();
            var uploadSession = graphClient.HttpProvider.Serializer.DeserializeObject<UploadSession>(content);

            return uploadSession;
        }
        public static async Task UploadLargeAttachmentInSlices(IBaseClient graphClient, string messageId)
        {
            using Stream stream = Program.GetFileStream();

            var uploadSession = await CreateFileAttachementUploadSession(graphClient, messageId, stream.Length);

            // Create task
            var maxSliceSize = 320 * 1024; // 320 KB - Change this to your slice size.

            LargeFileUploadTask<FileAttachment> largeFileUploadTask = new LargeFileUploadTask<FileAttachment>(uploadSession, graphClient, stream, maxSliceSize);

            // Setup the chunk request necessities
            var slicesRequests = largeFileUploadTask.GetUploadSliceRequests();
            var trackedExceptions = new List<Exception>();

            Uri attachmentLocation = null;

            //upload the chunks
            foreach (var request in slicesRequests)
            {
                // Send chunk request
                var result = await largeFileUploadTask.UploadSliceAsync(request, trackedExceptions);
                // Do your updates here: update progress bar, etc.
                Console.WriteLine($"File uploading in progress. {request.RangeEnd} of {stream.Length} bytes uploaded");

                if (result.UploadSucceeded)
                {
                    attachmentLocation = result.Location;
                    Console.WriteLine($"File uploading complete at : " + attachmentLocation.AbsoluteUri);
                }
            }

            // Check that upload succeeded
            if (attachmentLocation == null)
            {
                //Upload failed
                Console.WriteLine("Upload failed");
            }
        }

        public static async Task UploadLargeAttachmentWithCallBack(IBaseClient graphClient, string messageId)
        {

            using Stream stream = Program.GetFileStream();

            var uploadSession = await CreateFileAttachementUploadSession(graphClient, messageId,stream.Length);

            // Create task
            var maxSliceSize = 320 * 1024; // 320 KB - Change this to your slice size.
            LargeFileUploadTask<FileAttachment> largeFileUploadTask = new LargeFileUploadTask<FileAttachment>(uploadSession, graphClient, stream, maxSliceSize);

            // Setup the progress mechanism
            IProgress<long> progress = new Progress<long>(progress =>
            {
                Console.WriteLine($"Uploaded {progress} bytes of {stream.Length} bytes");
            });

            UploadResult<FileAttachment> uploadResult = null;
            try
            {
                uploadResult = await largeFileUploadTask.UploadAsync(progress);
                if (uploadResult.UploadSucceeded)
                {
                    Console.WriteLine(uploadResult.Location);
                }
            }
            catch (ServiceException e)
            {
                //try to refresh the upload info and resume the upload from where we left off.
                Console.WriteLine("Something went wrong with the upload");
                Console.WriteLine(e.Message);
            }

        }
    }
}
