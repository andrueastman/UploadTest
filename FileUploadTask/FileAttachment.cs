namespace FileUploadTask
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    public partial class FileAttachment
    {
        public FileAttachment()
        {
            this.ODataType = "microsoft.graph.fileAttachment";
        }
        /// <summary>
        /// Gets or sets id.
        /// Read-only.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "id", Required = Newtonsoft.Json.Required.Default)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets @odata.type.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "@odata.type", Required = Newtonsoft.Json.Required.Default)]
        public string ODataType { get; set; }

        /// <summary>
        /// Gets or sets additional data.
        /// </summary>
        [JsonExtensionData(ReadData = true, WriteData = true)]
        public IDictionary<string, object> AdditionalData { get; set; }

        /// <summary>
        /// Gets or sets last modified date time.
        /// The Timestamp type represents date and time information using ISO 8601 format and is always in UTC time. For example, midnight UTC on Jan 1, 2014 would look like this: '2014-01-01T00:00:00Z'
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "lastModifiedDateTime", Required = Newtonsoft.Json.Required.Default)]
        public DateTimeOffset? LastModifiedDateTime { get; set; }

        /// <summary>
        /// Gets or sets name.
        /// The attachment's file name.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "name", Required = Newtonsoft.Json.Required.Default)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets content type.
        /// The MIME type.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "contentType", Required = Newtonsoft.Json.Required.Default)]
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets size.
        /// The length of the attachment in bytes.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "size", Required = Newtonsoft.Json.Required.Default)]
        public Int32? Size { get; set; }

        /// <summary>
        /// Gets or sets is inline.
        /// true if the attachment is an inline attachment; otherwise, false.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "isInline", Required = Newtonsoft.Json.Required.Default)]
        public bool? IsInline { get; set; }

        /// <summary>
        /// Gets or sets content id.
        /// The ID of the attachment in the Exchange store.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "contentId", Required = Newtonsoft.Json.Required.Default)]
        public string ContentId { get; set; }

        /// <summary>
        /// Gets or sets content location.
        /// Do not use this property as it is not supported.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "contentLocation", Required = Newtonsoft.Json.Required.Default)]
        public string ContentLocation { get; set; }

        /// <summary>
        /// Gets or sets content bytes.
        /// The base64-encoded contents of the file.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "contentBytes", Required = Newtonsoft.Json.Required.Default)]
        public byte[] ContentBytes { get; set; }
    }
}