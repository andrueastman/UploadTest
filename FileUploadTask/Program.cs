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
            // get a itemid from the /drive/root/children endpoint using graph explorer
            string driveItemId = "DRIVE ITEM ID";

            // get a message from the /me/messages endpoint using graph explorer
            string messageId = "MESSAGE ID";

            // APP client for app
            string clientID = "APP ID FOR APPlICATION";

            /* Do the auth stuff first */
            IPublicClientApplication publicClientApplication = PublicClientApplicationBuilder
                .Create(clientID)
                .WithRedirectUri("http://localhost:1234")
                .Build();

            var scopes = new[] { "User.Read", "Mail.ReadWrite", "Sites.ReadWrite.All" };

            var authResult = await publicClientApplication.AcquireTokenInteractive(scopes).ExecuteAsync();

            /* Create a DelegateAuthenticationProvider to use */
            var delegatingAuthProvider = new DelegateAuthenticationProvider((requestMessage) =>
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", authResult.AccessToken);
                return Task.FromResult(0);
            });


            var graphClient = new BaseClient("https://graph.microsoft.com/v1.0/", delegatingAuthProvider);

            Console.WriteLine("Uploading large drive item file with callbacks");
            await DriveItemUpload.UploadLargeFileWithCallBacks(graphClient, driveItemId);

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
