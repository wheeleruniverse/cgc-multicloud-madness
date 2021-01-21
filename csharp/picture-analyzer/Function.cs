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
        private static readonly RegionEndpoint awsRegion = RegionEndpoint.USEast1;
        private static IAmazonS3 awsS3Client;

        // azure variables
        private static CloudStorageAccount azureStorage;
        private static CloudTableClient azureTablesClient;
        private static CloudTable azureTable;
        
        // gcp variables
        private static ImageAnnotatorClient gcpVisionClient;


        /// <summary>
        /// processes aws s3 put events by analyzing the data with the gcp vision service 
        /// and storing the results in an azure tables nosql database
        /// </summary>
        /// <param name="request">the aws event that triggered this aws lambda function</param>
        /// <param name="context">the aws lambda context</param>
        /// <returns>a list of response instances</returns>
        public List<AnalysisEntity> FunctionHandler(JObject request, ILambdaContext context)
        {
            InitAWS();
            InitAzure();
            InitGCP();

            // convert the request into an s3 event 
            S3EventNotification s3Event = request.ToObject<S3EventNotification>();

            // process each record within the s3 event
            return s3Event.Records.Select(i => ProcessRecord(i)).ToList();
        }


        /// <summary>
        /// initializes aws resources
        /// </summary>
        private void InitAWS()
        {
            awsS3Client ??= new AmazonS3Client(awsRegion);
        }

        /// <summary>
        /// initializes azure resources
        /// </summary>
        private void InitAzure()
        {
            JObject azureJson = JObject.Parse(File.ReadAllText("creds/azure-tables-svc.json"));
            string connectionString = azureJson["ConnectionString"].Value<string>();
            string tableName = azureJson["TableName"].Value<string>();
            
            azureStorage ??= CloudStorageAccount.Parse(connectionString);
            azureTablesClient ??= azureStorage.CreateCloudTableClient();
            azureTable ??= azureTablesClient.GetTableReference(tableName);
        }

        /// <summary>
        /// initializes gcp resources
        /// </summary>
        private void InitGCP()
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "creds/gcp-vision-svc.json");
            gcpVisionClient ??= ImageAnnotatorClient.Create();
        }

        /// <summary>
        /// processes a single s3 event notification record to support batching
        /// </summary>
        /// <param name="record">the s3 event notification record to process</param>
        /// <returns>an entity that matches the data saved in azure tables storage</returns> 
        private AnalysisEntity ProcessRecord(S3EventNotification.S3EventNotificationRecord record)
        {
            // extract event details
            string s3ActionName = record.EventName;
            string s3BucketName = record.S3.Bucket.Name;
            string s3ObjectName = record.S3.Object.Key;

            // form response with event details
            AnalysisEntity response = new AnalysisEntity
            {
                S3ActionName = s3ActionName,
                S3BucketName = s3BucketName,
                S3ObjectName = s3ObjectName
            };


            // validate s3 put event
            string xpActionName = "ObjectCreated:Put";
            if (!s3ActionName.Equals(xpActionName))
            {
                string error = $"ERROR : Expected: {xpActionName}; Received: {s3ActionName}";
                LambdaLogger.Log(error);
                return new AnalysisEntity(false, error);
            }

            // read object from s3
            InMemoryObject s3Object = GetObject(s3BucketName, s3ObjectName);
            LambdaLogger.Log(s3Object.ToString());

            // process the object with gcp vision
            VisionAnalysis analysis = Analyze(s3Object.Data);
            LambdaLogger.Log(analysis.ToString());

            response.VisionAnalysis = analysis;
            response.SerializedVisionAnalysis = JsonSerializer.Serialize(analysis);

            // save to azure tables
            AnalysisEntity entity = QueryEntity();
            if (entity == null)
            {
                // table is empty (load all 9 partitions)
                for (int i = 1; i <= 9; i++)
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
            Image image = Image.FromBytes(bytes);
            SafeSearchAnnotation safeSearch = gcpVisionClient.DetectSafeSearch(image);
            IReadOnlyList<EntityAnnotation> labels = gcpVisionClient.DetectLabels(image);
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
            GetObjectRequest request = new GetObjectRequest
            {
                BucketName = s3Bucket,
                Key = s3Object
            };

            try
            {
                using GetObjectResponse response = awsS3Client.GetObjectAsync(request).Result;
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
            TableOperation operation = TableOperation.Insert(entity);
            TableResult result = azureTable.ExecuteAsync(operation).Result;
            
            if(result != null && result.HttpStatusCode == 204)
            {
                LambdaLogger.Log($"Entity: Success: [{entity.PartitionKey}]{entity.RowKey}");
            }
            else
            {
                LambdaLogger.Log($"Status: {result.HttpStatusCode}");
                LambdaLogger.Log($"Result: {result.Result}");
                
                string errorMessage = $"Entity: Failure: [{entity.PartitionKey}]{entity.RowKey}";
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
            AnalysisEntity[] entities = QueryEntity(1);
            return entities.Length > 0 ? entities[0] : null;
        }

        /// <summary>
        /// queries x record(s) from azure tables storage basd on the limit provided
        /// </summary>
        /// <param name="limit">the number of records to load from azure tables storage</param>
        /// <returns>the record(s) found converted to an array</returns>
        private AnalysisEntity[] QueryEntity(int limit)
        {
            // query from the database
            TableQuery<AnalysisEntity> query = new TableQuery<AnalysisEntity>().Take(limit);
            TableQuerySegment<AnalysisEntity> segment = azureTable.ExecuteQuerySegmentedAsync(query, null).Result;
            return segment.ToArray<AnalysisEntity>();
        }
    }
}
