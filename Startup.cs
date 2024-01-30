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
	}
}