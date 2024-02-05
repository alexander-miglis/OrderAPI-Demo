using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace OrderAPI;

public class Function
{
	private IDynamoDBContext DynamoDbContext { get; set; }
	IAmazonS3 S3Client { get; set; }
	public Function(IAmazonS3 s3Client, IDynamoDBContext dynamoDbContext)
	{
        S3Client = s3Client;
        DynamoDbContext = dynamoDbContext;
	}

	//private readonly IDynamoDBContext _dynamoDbContext;
	//public Function(IAmazonDynamoDB dynamoDbClient)
	//{
	//	if (dynamoDbClient == null)
	//	{
	//		// log error
	//		throw new ArgumentNullException(nameof(dynamoDbClient));
	//	}

	//	_dynamoDbContext = new DynamoDBContext(dynamoDbClient);
	//}

	[LambdaFunction(MemorySize = 256, Role = "@OrderApiLambdaExecutionRole")]
    [HttpApi(LambdaHttpMethod.Post, "/order")]
    public async Task PostOrder([FromBody]Order order, ILambdaContext context)
    {
		await DynamoDbContext.SaveAsync(order);
    }

    [LambdaFunction(MemorySize = 256, Role = "@OrderApiLambdaExecutionRole")]
    [HttpApi(LambdaHttpMethod.Get, "/order/{orderId}")]
    public async Task<Order> GetOrder(string orderId, ILambdaContext context)
    {
	    return await DynamoDbContext.LoadAsync<Order>(orderId);
    }

    [LambdaFunction(MemorySize = 256, Role = "@OrderApiLambdaExecutionRole")]
    [HttpApi(LambdaHttpMethod.Delete, "/order/{orderId}")]
    public async Task DeleteOrder(string orderId, ILambdaContext context)
    {
	    await DynamoDbContext.DeleteAsync<Order>(orderId);
    }

    [LambdaFunction(MemorySize = 256, Role = "@OrderApiLambdaExecutionRole")]
    [HttpApi(LambdaHttpMethod.Get, "/order/{a}/{b}/{c}")]
	public async Task<int> Businesslogic(int a, int b, int c, ILambdaContext context)
    {
	    return a + b + c;
    }

	[LambdaFunction(MemorySize = 256, ResourceName = "S3OrderTrigger", Role = "@S3LambdaRole")]
    public async Task OrderFromFile(S3Event s3event, ILambdaContext context)
    {
        var eventRecords = s3event.Records ?? new List<S3Event.S3EventNotificationRecord>();
        foreach (var record in eventRecords)
        {
			var s3Event = record.S3;
			var bucketName = s3Event.Bucket.Name;
			var key = s3Event.Object.Key;
			var s3Object = await S3Client.GetObjectAsync(bucketName, key);
			var order = await JsonSerializer.DeserializeAsync<Order>(s3Object.ResponseStream);

			try
			{
				Console.WriteLine($"data: {JsonSerializer.Serialize(order)}, bucket: {bucketName}, key: {key}");
				await DynamoDbContext.SaveAsync(order);
			}
			catch (Exception e)
			{
				Console.WriteLine($"Error: {e.Message}");
				// move file to error folder
				var errorKey = key.Replace("incoming", "error");
				await S3Client.CopyObjectAsync(bucketName, key, bucketName, errorKey);
				await S3Client.DeleteObjectAsync(bucketName, key);
				return;
			}

			// move the file to the processed folder
			var processedKey = key.Replace("incoming", "processed");

			Console.WriteLine($"Moving data to processed folder. key: {key}, processedKey: {processedKey}");
			// Move the file to the processed folder
			await S3Client.CopyObjectAsync(bucketName, key, bucketName, processedKey);
			await S3Client.DeleteObjectAsync(bucketName, key);
        }
    }
}
