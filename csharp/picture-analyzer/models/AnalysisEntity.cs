using Microsoft.WindowsAzure.Storage.Table;
using System;

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
            PartitionKey = DateTime.Now.ToString("yyyy-MM-dd");
            RowKey = Guid.NewGuid().ToString();
            Success = success;
            ErrorMessage = errorMessage;
        }
        public AnalysisEntity() : this(true, null)
        {
            // Default to Success
        }
    }
}
