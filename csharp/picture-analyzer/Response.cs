
namespace Wheeler.PictureAnalyzer
{
    public class Response
    {
        public string S3Action { get; set; }
        public string S3Bucket { get; set; }
        public string S3Object { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }

        public Response(bool Success, string ErrorMessage)
        {
            this.Success = Success;
            this.ErrorMessage = ErrorMessage;
        }
        public Response() : this(true, null)
        { 
            // Default to Success
        }
    }
}
