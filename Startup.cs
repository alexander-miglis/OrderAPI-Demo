using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Annotations;
using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;

namespace OrderAPI;

[LambdaStartup]
public class Startup
{
	public void ConfigureServices(IServiceCollection services)
	{
		services.AddAWSService<IAmazonS3>();

		services.AddAWSService<IAmazonDynamoDB>();
		services.AddTransient<IDynamoDBContext, DynamoDBContext>();
		
	}
}