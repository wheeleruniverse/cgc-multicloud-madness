using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Text.Json;

namespace Wheeler.PictureAnalyzer
{
    public class AnalysisEntity : TableEntity
    {
        // _______________________________________________________________
        // Database Fields

        public string S3BucketName { get; set; }
        
        public string S3ObjectName { get; set; }

        public string SerializedVisionAnalysis { get; set; }


        // _______________________________________________________________
        // Ignored Fields

        [IgnoreProperty]
        public string S3ActionName { get; set; }

        [IgnoreProperty]
        public string ErrorMessage { get; set; }

        [IgnoreProperty]
        public bool Success { get; set; }

        [IgnoreProperty]
        public VisionAnalysis VisionAnalysis { get; set; }


        // _______________________________________________________________
        // Constructors

        public AnalysisEntity(bool success, string errorMessage)
        {
            RowKey = Guid.NewGuid().ToString();
            Success = success;
            ErrorMessage = errorMessage;
        }
        public AnalysisEntity() : this(true, null)
        {
            // Default to Success
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
