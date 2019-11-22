using Microsoft.Graph;
using System;
using System.Collections.Generic;

namespace FileUploadTask
{
    internal class UploadSession : IUploadSession
    {
        /// <summary>
        /// Expiration date of the upload session
        /// </summary>
        public DateTimeOffset? ExpirationDateTime { get; set; }

        /// <summary>
        /// The ranges yet to be uploaded to the server
        /// </summary>
        public IEnumerable<string> NextExpectedRanges { get; set; }

        /// <summary>
        /// The URL for upload
        /// </summary>
        public string UploadUrl { get; set; }
    }
}