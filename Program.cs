using System.Net;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
//using Amazon.CloudTrail.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using System;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]



public class Function
{


    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var requestBody = System.Text.Json.JsonSerializer.Deserialize<RequestModel>(request.Body);


        bool resp = ValidateInput(requestBody);

        if (resp == true)
        {
            bool respDynamo = await InsertIntoDynamoDb(requestBody,context);
            if(respDynamo == true)
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    Body = "Operation successful",
                    Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
                };
            }
            else
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = $"Error (Dynamo Operation Failed)",
                    Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
                };
            }
            
        }
        else
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Body = $"Error (Validation Failed)",
                Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
            };
        }

    }
    private bool ValidateInput(RequestModel request)
    {
        string emailPattern = @"^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$";
        string passwordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$";

        Regex regexEmail = new Regex(emailPattern);
        Regex regexPassword = new Regex(passwordPattern);


        if (string.IsNullOrEmpty(request.Name))
        {
            return false;
        }
        else if (regexEmail.IsMatch(request.Email) == false)
        {
            return false;
        }
        else if (regexPassword.IsMatch(request.Password) == false)
        {
            return false;
        }

        return true;
    }

    private async Task<bool> InsertIntoDynamoDb(RequestModel request, ILambdaContext context)
    {
        try
        {
            var awsAccessKeyId = "567";
            var awsSecretKey = "123";
            var dynamoDbConfig = new AmazonDynamoDBConfig
            {
                RegionEndpoint = Amazon.RegionEndpoint.USEast2 // Replace YourRegion with your desired AWS region
            };

            var dynamoDbClient = new AmazonDynamoDBClient(awsAccessKeyId, awsSecretKey, dynamoDbConfig);
            var dbContext = new DynamoDBContext(dynamoDbClient);

            var newItem = new YourItemClass
            {
                ID = Guid.NewGuid().ToString(), // You may adjust the hash key value based on your requirements
                name = request.Name,
                email = request.Email,
                password = request.Password
                // Set other attribute values as needed
            };

            await dbContext.SaveAsync(newItem);
            return true;


        }
        catch (Exception ex)
        {
            // Log the exception for troubleshooting
            context.Logger.LogLine($"Error inserting data into DynamoDB: {ex.Message}");
            return false;
        }
    }
}


public class RequestModel
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}

[DynamoDBTable("TestDB")]
public class YourItemClass
{
    [DynamoDBHashKey("ID")]
    public string ID { get; set; }

    [DynamoDBProperty]
    public string name { get; set; }

    [DynamoDBProperty]
    public string email { get; set; }

    [DynamoDBProperty]
    public string password { get; set; }
    // Add other attributes as needed
}