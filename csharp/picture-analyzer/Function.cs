using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Google.Cloud.Vision.V1;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Wheeler.PictureAnalyzer
{
    public class Function
    {

        private static readonly RegionEndpoint awsRegion = RegionEndpoint.USEast1;
        private static IAmazonS3 s3Client;
        private static ImageAnnotatorClient visionClient;


        /// <summary>
        /// processes aws s3 put events by analyzing the data with the gcp vision service 
        /// and storing the results in an azure tables nosql database
        /// </summary>
        /// <param name="request">the aws event that triggered this aws lambda function</param>
        /// <param name="context">the aws lambda context</param>
        /// <returns>a list of response instances</returns>
        public List<Response> FunctionHandler(JObject request, ILambdaContext context)
        {
            // set environment variables
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "creds/gcp-vision-svc.json");

            // create cloud clients
            s3Client ??= new AmazonS3Client(awsRegion);
            visionClient ??= ImageAnnotatorClient.Create();

            // convert the request into an s3 event 
            S3EventNotification s3Event = request.ToObject<S3EventNotification>();

            // process each record within the s3 event
            return s3Event.Records.Select(i => ProcessRecord(i)).ToList();
        }

        private Response ProcessRecord(S3EventNotification.S3EventNotificationRecord record)
        {
            // extract event details
            string s3ActionName = record.EventName;
            string s3BucketName = record.S3.Bucket.Name;
            string s3ObjectName = record.S3.Object.Key;

            // form response with event details
            Response response = new Response
            {
                S3ActionName = s3ActionName,
                S3BucketName = s3BucketName,
                S3ObjectName = s3ObjectName
            };


            // validate s3 put event
            string xpActionName = "ObjectCreated:Put";
            if (!s3ActionName.Equals(xpActionName))
            {
                string error = $"Expected: {xpActionName}; Received: {s3ActionName}";
                LambdaLogger.Log(error);
                return new Response(false, error);
            }

            // read object from s3
            InMemoryObject s3Object = ReadFromS3(s3BucketName, s3ObjectName).Result;
            LambdaLogger.Log(s3Object.ToString());

            // process the object with gcp vision
            VisionAnalysis analysis = SendToVision(s3Object.Data);
            LambdaLogger.Log(analysis.ToString());

            response.VisionAnalysis = analysis;

            // TODO: Write GCP Vision API Results to Azure Tables

            return response;
        }


        private async Task<InMemoryObject> ReadFromS3(string s3Bucket, string s3Object)
        {
            GetObjectRequest request = new GetObjectRequest
            {
                BucketName = s3Bucket,
                Key = s3Object
            };

            try
            {
                using GetObjectResponse response = await s3Client.GetObjectAsync(request);
                using Stream stream = response.ResponseStream;
                using MemoryStream memory = new MemoryStream();

                // read headers
                IEnumerable<string> headers = response.Headers.Keys.Select(i => response.Headers[i]);

                // read data
                int line;
                byte[] buffer = new byte[16 * 1024];
                while ((line = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    memory.Write(buffer, 0, line);
                }
                byte[] data = memory.ToArray();

                // form response
                return new InMemoryObject()
                {
                    Headers = headers,
                    Data = data
                };
            }
            catch(Exception e)
            {
                LambdaLogger.Log($"ERROR: {e}");
                throw;
            }
        }


        private VisionAnalysis SendToVision(byte[] bytes)
        {
            Image image = Image.FromBytes(bytes);
            SafeSearchAnnotation safeSearch = visionClient.DetectSafeSearch(image);
            IReadOnlyList<EntityAnnotation> labels = visionClient.DetectLabels(image);
            return new VisionAnalysis(safeSearch, labels);
        }
    }
}
