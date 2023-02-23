using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace FileUploadTask
{
    class Program
    {
        const string clientID = "11e584da-5b80-404b-9a3c-0d510c105513"; // APP client id for app
        const string filesDirectory = "C:\\Users\\user2\\Downloads\\BigFiles\\"; // Location of a large number of large files

        static async Task Main()
        {
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

            var graphClient = new GraphServiceClient(delegatingAuthProvider);

            for (int i =0; i< 20; i++)
            {
                using var stream = GetFileStream();
                await UploadFile(graphClient, "testFile.txt", stream);
                Console.WriteLine($"Test {i+1} completed");
            }
        }

        public static async Task UploadFile(GraphServiceClient graphClient, string itemPath, Stream fileStream)
        {
            // Use properties to specify the conflict behavior
            // in this case, replace
            var uploadProps = new DriveItemUploadableProperties
            {
                ODataType = null,
                AdditionalData = new Dictionary<string, object>
                {
                    { "@microsoft.graph.conflictBehavior", "replace" }
                }
            };

            // Create the upload session
            // itemPath does not need to be a path to an existing item
            var uploadSession = await graphClient.Me.Drive.Root
                .ItemWithPath(itemPath)
                .CreateUploadSession(uploadProps)
                .Request()
                .PostAsync();

            // Max slice size must be a multiple of 320 KiB
            int maxSliceSize = 320 * 1024 * 50;
            var fileUploadTask =
                new LargeFileUploadTask<DriveItem>(uploadSession, fileStream, maxSliceSize);

            // Create a callback that is invoked after each slice is uploaded
            IProgress<long> progress = new Progress<long>(prog =>
            {
                Console.WriteLine($"Uploaded {prog} bytes of {fileStream.Length} bytes");
            });

            try
            {
                // Upload the file
                var uploadResult = await fileUploadTask.UploadAsync(progress);

                if (uploadResult.UploadSucceeded)
                {
                    // The ItemResponse object in the result represents the
                    // created item.
                    Console.WriteLine($"Upload complete, item ID: {uploadResult.ItemResponse.Id}");
                }
                else
                {
                    Console.WriteLine("Upload failed");
                }
            }
            catch (ServiceException ex)
            {
                Console.WriteLine($"Error uploading: {ex}");
            }
        }
        
        /// <summary>
        /// Read a file present in the project for uploading
        /// </summary>
        /// <returns></returns>
        public static Stream GetFileStream()
        {
            var rand = new Random(); 
            var files = System.IO.Directory.GetFiles(filesDirectory, "*.*");
            string nextFile = files[rand.Next(files.Length)];
            FileStream fileStream = new(nextFile, FileMode.Open);
            Console.WriteLine($"Picked file {nextFile} with size : {fileStream.Length}");
            return fileStream;
        }
    }
}
