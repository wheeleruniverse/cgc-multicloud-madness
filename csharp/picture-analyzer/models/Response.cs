
namespace Wheeler.PictureAnalyzer
{
    public class Response
    {
        public string S3ActionName { get; set; }
        public string S3BucketName { get; set; }
        public string S3ObjectName { get; set; }
        public VisionAnalysis VisionAnalysis { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }

        public Response(bool success, string errorMessage)
        {
            Success = success;
            ErrorMessage = errorMessage;
        }
        public Response() : this(true, null)
        { 
            // Default to Success
        }
    }
}
