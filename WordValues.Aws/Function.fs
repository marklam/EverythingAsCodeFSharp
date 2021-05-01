namespace WordValues.Aws

open System
open System.Net
open System.Text.Json

open Amazon.Lambda.Core
open Amazon.Lambda.RuntimeSupport
open Amazon.Lambda.Serialization.SystemTextJson
open Amazon.Lambda.APIGatewayEvents

open WordValues

// This project specifies the serializer used to convert Lambda event into .NET classes in the project's main
// main function. This assembly register a serializer for use when the project is being debugged using the
// AWS .NET Mock Lambda Test Tool.
[<assembly: LambdaSerializer(typeof<Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer>)>]
()

module Function =

    let functionHandler (word : string) (context: ILambdaContext) =
        if not (isNull word) then
            let result = Calculate.wordValue word
            let content = JsonSerializer.Serialize<_>(result, JsonSerializerOptions(IgnoreNullValues = true))

            APIGatewayProxyResponse(
                StatusCode = int HttpStatusCode.OK,
                Body       = content,
                Headers    = dict [ ("Content-Type", "application/json") ]
                )
        else
            APIGatewayProxyResponse(
                StatusCode = int HttpStatusCode.BadRequest,
                Body       = "Required query parameter 'word' was missing",
                Headers    = dict [ ("Content-Type", "text/plain;charset=utf-8") ]
                )

    [<EntryPoint>]
    let main _args =

        let handler = Func<string, ILambdaContext, APIGatewayProxyResponse>(functionHandler)
        use handlerWrapper = HandlerWrapper.GetHandlerWrapper(handler, new DefaultLambdaJsonSerializer())
        use bootstrap = new LambdaBootstrap(handlerWrapper)

        bootstrap.RunAsync().GetAwaiter().GetResult()
        0