using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wheeler.PictureAnalyzer
{
    public class AnalysisEntity : TableEntity
    {
        public string S3ActionName { get; set; }
        public string S3BucketName { get; set; }
        public string S3ObjectName { get; set; }
        public VisionAnalysis VisionAnalysis { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        

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
