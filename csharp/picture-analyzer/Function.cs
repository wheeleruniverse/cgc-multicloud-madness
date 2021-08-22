using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Amazon;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Google.Cloud.Vision.V1;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json.Linq;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Wheeler.PictureAnalyzer
{
    public class Function
    {

        // aws variables
        private static readonly RegionEndpoint AwsRegion = RegionEndpoint.USEast1;
        private static IAmazonS3 _awsS3Client;

        // azure variables
        private static CloudStorageAccount _azureStorage;
        private static CloudTableClient _azureTablesClient;
        private static CloudTable _azureTable;
        
        // gcp variables
        private static ImageAnnotatorClient _gcpVisionClient;


        /// <summary>
        /// processes aws s3 put events by analyzing the data with the gcp vision service 
        /// and storing the results in an azure tables nosql database
        /// </summary>
        /// <param name="request">the aws event that triggered this aws lambda function</param>
        /// <param name="context">the aws lambda context</param>
        /// <returns>a list of response instances</returns>
        public List<AnalysisEntity> FunctionHandler(JObject request, ILambdaContext context)
        {
            InitAws();
            InitAzure();
            InitGcp();

            // convert the request into an s3 event 
            var s3Event = request.ToObject<S3EventNotification>();

            // process each record within the s3 event
            return s3Event?.Records.Select(ProcessRecord).ToList();
        }


        /// <summary>
        /// initializes aws resources
        /// </summary>
        private void InitAws()
        {
            _awsS3Client ??= new AmazonS3Client(AwsRegion);
        }

        /// <summary>
        /// initializes azure resources
        /// </summary>
        private void InitAzure()
        {
            var azureJson = JObject.Parse(File.ReadAllText("creds/azure-tables-svc.json"));
            var connectionString = azureJson["ConnectionString"]?.Value<string>();
            var tableName = azureJson["TableName"]?.Value<string>();
            
            _azureStorage ??= CloudStorageAccount.Parse(connectionString);
            _azureTablesClient ??= _azureStorage.CreateCloudTableClient();
            _azureTable ??= _azureTablesClient.GetTableReference(tableName);
        }

        /// <summary>
        /// initializes gcp resources
        /// </summary>
        private void InitGcp()
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "creds/gcp-vision-svc.json");
            _gcpVisionClient ??= ImageAnnotatorClient.Create();
        }

        /// <summary>
        /// processes a single s3 event notification record to support batching
        /// </summary>
        /// <param name="record">the s3 event notification record to process</param>
        /// <returns>an entity that matches the data saved in azure tables storage</returns> 
        private AnalysisEntity ProcessRecord(S3EventNotification.S3EventNotificationRecord record)
        {
            // extract event details
            var s3ActionName = record.EventName.ToString();
            var s3BucketName = record.S3.Bucket.Name;
            var s3ObjectName = record.S3.Object.Key;

            // form response with event details
            var response = new AnalysisEntity
            {
                S3ActionName = s3ActionName,
                S3BucketName = s3BucketName,
                S3ObjectName = s3ObjectName
            };


            // validate s3 put event
            const string xpActionName = "ObjectCreated:Put";
            if (!s3ActionName.Equals(xpActionName))
            {
                var error = $"ERROR : Expected: {xpActionName}; Received: {s3ActionName}";
                LambdaLogger.Log(error);
                return new AnalysisEntity(false, error);
            }

            // read object from s3
            var s3Object = GetObject(s3BucketName, s3ObjectName);
            LambdaLogger.Log(s3Object.ToString());

            // process the object with gcp vision
            var analysis = Analyze(s3Object.Data);
            LambdaLogger.Log(analysis.ToString());

            response.VisionAnalysis = analysis;
            response.SerializedVisionAnalysis = JsonSerializer.Serialize(analysis);

            // save to azure tables
            var entity = QueryEntity();
            if (entity == null)
            {
                // table is empty (load all 9 partitions)
                for (var i = 1; i <= 9; i++)
                {
                    response.PartitionKey = i.ToString();
                    InsertEntity(response);
                }
            }
            else
            {
                InsertEntity(response);
            }
            return response;
        }

        /// <summary>
        /// analyzes the bytes provided with the gcp vision service
        /// </summary>
        /// <param name="bytes">the bytes of an image to process</param>
        /// <returns>the gcp vision results</returns>
        private VisionAnalysis Analyze(byte[] bytes)
        {
            var image = Image.FromBytes(bytes);
            var safeSearch = _gcpVisionClient.DetectSafeSearch(image);
            var labels = _gcpVisionClient.DetectLabels(image);
            return new VisionAnalysis(safeSearch, labels);
        }

        /// <summary>
        /// loads an object from s3 into memory using the provided parameters
        /// </summary>
        /// <param name="s3Bucket">the s3 bucket to read</param>
        /// <param name="s3Object">the s3 object to read</param>
        /// <returns>an object that contains the s3 object in bytes along with information about the s3 object</returns>
        private InMemoryObject GetObject(string s3Bucket, string s3Object)
        {
            var request = new GetObjectRequest
            {
                BucketName = s3Bucket,
                Key = s3Object
            };

            try
            {
                using var response = _awsS3Client.GetObjectAsync(request).Result;
                using var stream = response.ResponseStream;
                using var memory = new MemoryStream();

                // read headers
                var headers = response.Headers.Keys.Select(i => response.Headers[i]);

                // read data
                int line;
                var buffer = new byte[16 * 1024];
                while ((line = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    memory.Write(buffer, 0, line);
                }
                var data = memory.ToArray();

                // form response
                return new InMemoryObject()
                {
                    Headers = headers,
                    Data = data
                };
            }
            catch(Exception e)
            {
                LambdaLogger.Log($"ERROR : {e}");
                throw;
            }
        }

        /// <summary>
        /// inserts the provided entity into azure tables storage
        /// </summary>
        /// <param name="entity">the entity to insert</param>
        private void InsertEntity(AnalysisEntity entity)
        {
            // insert into the database
            var operation = TableOperation.Insert(entity);
            var result = _azureTable.ExecuteAsync(operation).Result;
            
            if(result is { HttpStatusCode: 204 })
            {
                LambdaLogger.Log($"Entity: Success: [{entity.PartitionKey}]{entity.RowKey}");
            }
            else
            {
                LambdaLogger.Log($"Status: {result?.HttpStatusCode}");
                LambdaLogger.Log($"Result: {result?.Result}");
                
                var errorMessage = $"Entity: Failure: [{entity.PartitionKey}]{entity.RowKey}";
                LambdaLogger.Log(errorMessage);

                entity.ErrorMessage = errorMessage;
                entity.Success = false;
            }
        }
        
        /// <summary>
        /// queries the first record in azure tables storage
        /// </summary>
        /// <returns>the entity found or null if the table is empty</returns>
        private AnalysisEntity QueryEntity()
        {
            var entities = QueryEntity(1);
            return entities.Length > 0 ? entities[0] : null;
        }

        /// <summary>
        /// queries x record(s) from azure tables storage based on the limit provided
        /// </summary>
        /// <param name="limit">the number of records to load from azure tables storage</param>
        /// <returns>the record(s) found converted to an array</returns>
        private AnalysisEntity[] QueryEntity(int limit)
        {
            // query from the database
            var query = new TableQuery<AnalysisEntity>().Take(limit);
            var segment = _azureTable.ExecuteQuerySegmentedAsync(query, null).Result;
            return segment.ToArray();
        }
    }
}
