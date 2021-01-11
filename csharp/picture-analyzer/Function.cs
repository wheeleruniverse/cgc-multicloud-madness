using System;
using System.Collections;
using System.Collections.Generic;

using Amazon.Lambda.Core;
using Amazon.S3.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]
// [assembly: LambdaSerializer(typeof(JsonSerializer))]
// [assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Wheeler.PictureAnalyzer
{
    public class Function
    {
        
        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public List<Response> FunctionHandler(JObject input, ILambdaContext context)
        {

            List<Response> response = new List<Response>();
            S3EventNotification s3Event = input.ToObject<S3EventNotification>();

            foreach(var r in s3Event.Records)
            {
                response.Add(ProcessRecord(r));
            }
            return response;
        }


        private Response ProcessRecord(S3EventNotification.S3EventNotificationRecord record)
        {
            // Validate S3 Event
            string s3Action = record.EventName;
            string xpAction = "ObjectCreated:Put";
            if (!s3Action.Equals(xpAction))
            {
                string error = $"Expected: {xpAction}; Received: {s3Action}";
                LambdaLogger.Log(error);
                return new Response(false, error);
            }

            // Extract S3 Event Details
            Response response = new Response
            {
                S3Action = s3Action,
                S3Bucket = record.S3.Bucket.Name,
                S3Object = record.S3.Object.Key
            };

            // TODO: Load S3 Object

            // TODO: Send File to GCP Vision API

            // TODO: Write GCP Vision API Results to Azure Tables

            return response;
        }
    }
}
