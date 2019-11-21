using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.IO;
using System.Net.Http.Headers;
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

            Console.WriteLine("Uploading large drive item file in slices");
            //await DriveItemUpload.UploadLargeFileInSlices(graphClient,id);

            Console.WriteLine("Uploading large drive item file with callbacks");
            //await DriveItemUpload.UploadLargeFileWithCallBacks(graphClient,id);

            var messages = await graphClient.Me.Messages.Request().GetAsync();
            var messageId = messages.CurrentPage[5].Id;

            Console.WriteLine("Uploading large attachement file in slices");
            await FileAttachmentUpload.UploadLargeAttachmentInSlices(graphClient,messageId);

            Console.WriteLine("Uploading large attachement file with callbacks");
            await FileAttachmentUpload.UploadLargeAttachmentWithCallBack(graphClient, messageId);

        }
        
        /// <summary>
        /// Read a file present in the project for uploading
        /// </summary>
        /// <returns></returns>
        public static Stream GetFileStream()
        {
            string startupPath = Environment.CurrentDirectory;
            FileStream fileStream = new FileStream(startupPath + "\\SWEBOKv3.pdf", FileMode.Open);
            return fileStream;
        }
    }
}
