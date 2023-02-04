using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using StackExchange.Redis;
using System.Text.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace verySlowCalculationApi;

public class Function
{
    private IDatabase _database;

    public Function()
    {
        var connectionString = Environment.GetEnvironmentVariable("REDIS_CONFIGURATION");
        var connection = ConnectionMultiplexer.Connect(connectionString);
        _database = connection.GetDatabase();
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest input, ILambdaContext context)
    {
        var id = input.PathParameters["id"];

        var value = string.Empty;

        var cacheValue = await _database.StringGetAsync(id);

        if(cacheValue.HasValue)
        {
            value = cacheValue;
        }
        else
        {
            value = await VeryExpensiveCalculation();

            await _database.StringSetAsync(id, value, TimeSpan.FromSeconds(30), When.Always, CommandFlags.PreferMaster);
        }

        return new APIGatewayProxyResponse
        {
            Body = "{\"Value\":\""+ value + "\"}",
            StatusCode = 200,
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
    }

    public async Task<string> VeryExpensiveCalculation()
    {
        await Task.Delay(5000);

        return Guid.NewGuid().ToString();
    }
}
