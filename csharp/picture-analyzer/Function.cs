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
            // validate s3 put event
            string s3Action = record.EventName;
            string xpAction = "ObjectCreated:Put";
            if (!s3Action.Equals(xpAction))
            {
                string error = $"Expected: {xpAction}; Received: {s3Action}";
                LambdaLogger.Log(error);
                return new Response(false, error);
            }

            // form response with event details
            Response response = new Response
            {
                S3Action = s3Action,
                S3Bucket = record.S3.Bucket.Name,
                S3Object = record.S3.Object.Key
            };

            // read object from s3
            byte[] s3ObjectBytes = ReadFromS3(response.S3Bucket, response.S3Object).Result;

            // process the object with gcp vision
            SendToVision(s3ObjectBytes);

            // TODO: Write GCP Vision API Results to Azure Tables

            return response;
        }


        private async Task<byte[]> ReadFromS3(string s3Bucket, string s3Object)
        {
            GetObjectRequest request = new GetObjectRequest
            {
                BucketName = s3Bucket,
                Key = s3Object
            };

            byte[] bytes = null;

            try
            {
                using GetObjectResponse response = await s3Client.GetObjectAsync(request);
                using Stream stream = response.ResponseStream;
                using StreamReader reader = new StreamReader(stream);

                string type = response.Headers["Content-Type"];
                Console.WriteLine($"Content Type: {type}");

                string content = reader.ReadToEnd();
                bytes = Convert.FromBase64String(content);
                Console.WriteLine($"Content Size: {bytes.Length}");

                return bytes;
            }
            catch(Exception e)
            {
                Console.WriteLine($"ERROR: {e}");
            }

            return bytes ?? new byte[0];
        }


        private void SendToVision(byte[] bytes)
        {
            Image image = Image.FromBytes(bytes);

            IReadOnlyList<EntityAnnotation> labels = visionClient.DetectLabels(image);
            foreach(EntityAnnotation a in labels)
            {
                Console.WriteLine($"Description: {a.Description}");
                Console.WriteLine($"Locale: {a.Locale}");
                Console.WriteLine($"Score: {a.Score}");
                Console.WriteLine($"Topicality: {a.Topicality}");
            }
        }
    }
}
