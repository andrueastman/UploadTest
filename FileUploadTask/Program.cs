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


            var graphClient = new BaseClient("https://graph.microsoft.com/v1.0/", delegatingAuthProvider);

            // get a itemid from the /drive/root/children endpoint using graph explorer
            string driveItemId = "01WICLWWBGFKLMVC54ZJCJ6D7654VWZCLD";

            Console.WriteLine("Uploading large drive item file in slices");
            await DriveItemUpload.UploadLargeFileInSlices(graphClient, driveItemId);

            Console.WriteLine("Uploading large drive item file with callbacks");
            await DriveItemUpload.UploadLargeFileWithCallBacks(graphClient, driveItemId);

            // get a message from the /me/messages endpoint using graph explorer
            string messageId = "AAMkADcyMWRhMWZmLTFlMjUtNGNmMS1hNGMwLTgwZjc4YTM1OGJkMABGAAAAAABQGcMSxjfySL9jI-9KPCamBwCHxMu3VWSLRbuEFlqpOoZNAAAAAAEMAACHxMu3VWSLRbuEFlqpOoZNAAAdBwU1AAA=";

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
