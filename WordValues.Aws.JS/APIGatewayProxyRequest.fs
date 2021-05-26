namespace Amazon.Lambda.APIGatewayEvents

open Fable.Core

module rec Request =
    type [<AllowNullLiteral>] RequestHeaders =
        [<EmitIndexer>] abstract Item: name: string -> string option with get, set

    type [<AllowNullLiteral>] MultiValueHeaders =
        [<EmitIndexer>] abstract Item: name: string -> string seq option with get, set

    type [<AllowNullLiteral>] QueryStringParameters =
        [<EmitIndexer>] abstract Item: name: string -> string option with get, set

    type [<AllowNullLiteral>] MultiValueQueryStringParameters =
        [<EmitIndexer>] abstract Item: name: string -> string seq option with get, set

    type [<AllowNullLiteral>] PathParameters =
        [<EmitIndexer>] abstract Item: name: string -> string option with get, set

    type [<AllowNullLiteral>] StageVariables =
        [<EmitIndexer>] abstract Item: name: string -> string option with get, set

    type [<AllowNullLiteral>] APIGatewayCustomAuthorizerContext =
        [<EmitIndexer>] abstract Item: name: string -> obj option with get, set

    /// <summary>
    /// For request coming in from API Gateway proxy
    /// http://docs.aws.amazon.com/apigateway/latest/developerguide/api-gateway-set-up-simple-proxy.html
    /// </summary>
    type APIGatewayProxyRequest =
        {
        /// <summary>
        /// The resource path defined in API Gateway
        /// <para>
        /// This field is only set for REST API requests.
        /// </para>
        /// </summary>
        resource : string option

        /// <summary>
        /// The url path for the caller
        /// <para>
        /// This field is only set for REST API requests.
        /// </para>
        /// </summary>
        path : string option

        /// <summary>
        /// The HTTP method used
        /// <para>
        /// This field is only set for REST API requests.
        /// </para>
        /// </summary>
        httpMethod : string option

        /// <summary>
        /// The headers sent with the request. This collection will only contain a single value for a header.
        ///
        /// API Gateway will populate both the Headers and MultiValueHeaders collection for every request. If multiple values
        /// are set for a header then the Headers collection will just contain the last value.
        /// <para>
        /// This field is only set for REST API requests.
        /// </para>
        /// </summary>
        headers : RequestHeaders option

        /// <summary>
        /// The headers sent with the request. This collection supports multiple values for a single header.
        ///
        /// API Gateway will populate both the Headers and MultiValueHeaders collection for every request. If multiple values
        /// are set for a header then the Headers collection will just contain the last value.
        /// <para>
        /// This field is only set for REST API requests.
        /// </para>
        /// </summary>
        multiValueHeaders : MultiValueHeaders option

        /// <summary>
        /// The query string parameters that were part of the request. This collection will only contain a single value for a query parameter.
        ///
        /// API Gateway will populate both the QueryStringParameters and MultiValueQueryStringParameters collection for every request. If multiple values
        /// are set for a query parameter then the QueryStringParameters collection will just contain the last value.
        /// <para>
        /// This field is only set for REST API requests.
        /// </para>
        /// </summary>
        queryStringParameters : QueryStringParameters option

        /// <summary>
        /// The query string parameters that were part of the request. This collection supports multiple values for a single query parameter.
        ///
        /// API Gateway will populate both the QueryStringParameters and MultiValueQueryStringParameters collection for every request. If multiple values
        /// are set for a query parameter then the QueryStringParameters collection will just contain the last value.
        /// <para>
        /// This field is only set for REST API requests.
        /// </para>
        /// </summary>
        multiValueQueryStringParameters : MultiValueQueryStringParameters option

        /// <summary>
        /// The path parameters that were part of the request
        /// <para>
        /// This field is only set for REST API requests.
        /// </para>
        /// </summary>
        pathParameters : PathParameters option

        /// <summary>
        /// The stage variables defined for the stage in API Gateway
        /// </summary>
        stageVariables : StageVariables option

        /// <summary>
        /// The request context for the request
        /// </summary>
        requestContext : ProxyRequestContext option

        /// <summary>
        /// The HTTP request body.
        /// </summary>
        body : string option

        /// <summary>
        /// True if the body of the request is base 64 encoded.
        /// </summary>
        isBase64Encoded : bool option
        }
        /// <summary>
        /// The ProxyRequestContext contains the information to identify the AWS account and resources invoking the
        /// Lambda function. It also includes Cognito identity information for the caller.
        /// </summary>
        type ProxyRequestContext =
            {
            /// <summary>
            /// The resource full path including the API Gateway stage
            /// <para>
            /// This field is only set for REST API requests.
            /// </para>
            /// </summary>
            path : string option

            /// <summary>
            /// The account id that owns the executing Lambda function
            /// </summary>
            accountId : string option

            /// <summary>
            /// The resource id.
            /// </summary>
            resourceId : string option


            /// <summary>
            /// The API Gateway stage name
            /// </summary>
            stage : string option

            /// <summary>
            /// The unique request id
            /// </summary>
            requestId : string option

            /// <summary>
            /// The identity information for the request caller
            /// </summary>
            identity : RequestIdentity option

            /// <summary>
            /// The resource path defined in API Gateway
            /// <para>
            /// This field is only set for REST API requests.
            /// </para>
            /// </summary>
            resourcePath : string option

            /// <summary>
            /// The HTTP method used
            /// <para>
            /// This field is only set for REST API requests.
            /// </para>
            /// </summary>
            httpMethod : string option

            /// <summary>
            /// The API Gateway rest API Id.
            /// </summary>
            apiId : string option

            /// <summary>
            /// An automatically generated ID for the API call, which contains more useful information for debugging/troubleshooting.
            /// </summary>
            extendedRequestId : string option

            /// <summary>
            /// The connectionId identifies a unique client connection in a WebSocket API.
            /// <para>
            /// This field is only set for WebSocket API requests.
            /// </para>
            /// </summary>
            connectionId : string option

            /// <summary>
            /// The Epoch-formatted connection time in a WebSocket API.
            /// <para>
            /// This field is only set for WebSocket API requests.
            /// </para>
            /// </summary>
            connectionAt : int64 option

            /// <summary>
            /// A domain name for the WebSocket API. This can be used to make a callback to the client (instead of a hard-coded value).
            /// <para>
            /// This field is only set for WebSocket API requests.
            /// </para>
            /// </summary>
            domainName : string option

            /// <summary>
            /// The first label of the DomainName. This is often used as a caller/customer identifier.
            /// </summary>
            domainPrefix : string option

            /// <summary>
            /// The event type: CONNECT, MESSAGE, or DISCONNECT.
            /// <para>
            /// This field is only set for WebSocket API requests.
            /// </para>
            /// </summary>
            eventType : string option

            /// <summary>
            /// A unique server-side ID for a message. Available only when the $context.eventType is MESSAGE.
            /// <para>
            /// This field is only set for WebSocket API requests.
            /// </para>
            /// </summary>
            messageId : string option

            /// <summary>
            /// The selected route key.
            /// <para>
            /// This field is only set for WebSocket API requests.
            /// </para>
            /// </summary>
            routeKey : string option


            /// <summary>
            /// The APIGatewayCustomAuthorizerContext containing the custom properties set by a custom authorizer.
            /// </summary>
            authorizer : APIGatewayCustomAuthorizerContext option

            /// <summary>
            /// Gets and sets the operation name.
            /// </summary>
            operationName : string option

            /// <summary>
            /// Gets and sets the error.
            /// </summary>
            error : string option

            /// <summary>
            /// Gets and sets the integration latency.
            /// </summary>
            integrationLatency : string option

            /// <summary>
            /// Gets and sets the message direction.
            /// </summary>
            messageDirection : string option

            /// <summary>
            /// Gets and sets the request time.
            /// </summary>
            requestTime : string option

            /// <summary>
            /// Gets and sets the request time as an epoch.
            /// </summary>
            requestTimeEpoch : int64 option

            /// <summary>
            /// Gets and sets the status.
            /// </summary>
            status : string option

        }

        /// <summary>
        /// The RequestIdentity contains identity information for the request caller.
        /// </summary>
        type RequestIdentity =
            {

            /// <summary>
            /// The Cognito identity pool id.
            /// </summary>
            cognitoIdentityPoolId : string option

            /// <summary>
            /// The account id of the caller.
            /// </summary>
            accountId : string option

            /// <summary>
            /// The cognito identity id.
            /// </summary>
            cognitoIdentityId : string option

            /// <summary>
            /// The caller
            /// </summary>
            caller : string option

            /// <summary>
            /// The API Key
            /// </summary>
            apiKey : string option

            /// <summary>
            /// The API Key ID
            /// </summary>
            apiKeyId : string option

            /// <summary>
            /// The Access Key
            /// </summary>
            accessKey : string option

            /// <summary>
            /// The source IP of the request
            /// </summary>
            sourceIp : string option

            /// <summary>
            /// The Cognito authentication type used for authentication
            /// </summary>
            cognitoAuthenticationType : string option

            /// <summary>
            /// The Cognito authentication provider
            /// </summary>
            cognitoAuthenticationProvider : string option

            /// <summary>
            /// The user arn
            /// </summary>
            userArn : string option

            /// <summary>
            /// The user agent
            /// </summary>
            userAgent : string option

            /// <summary>
            /// The user
            /// </summary>
            user : string option


            /// <summary>
            /// Properties for a client certificate.
            /// </summary>
            clientCert : ProxyRequestClientCert option
        }

        /// <summary>
        /// Container for the properties of the client certificate.
        /// </summary>
        type ProxyRequestClientCert =
            {
            /// <summary>
            /// The PEM-encoded client certificate that the client presented during mutual TLS authentication.
            /// Present when a client accesses an API by using a custom domain name that has mutual
            /// TLS enabled. Present only in access logs if mutual TLS authentication fails.
            /// </summary>
            clientCertPem : string option

            /// <summary>
            /// The distinguished name of the subject of the certificate that a client presents.
            /// Present when a client accesses an API by using a custom domain name that has
            /// mutual TLS enabled. Present only in access logs if mutual TLS authentication fails.
            /// </summary>
            subjectDN : string option

            /// <summary>
            /// The distinguished name of the issuer of the certificate that a client presents.
            /// Present when a client accesses an API by using a custom domain name that has
            /// mutual TLS enabled. Present only in access logs if mutual TLS authentication fails.
            /// </summary>
            issuerDN : string option

            /// <summary>
            /// The serial number of the certificate. Present when a client accesses an API by
            /// using a custom domain name that has mutual TLS enabled.
            /// Present only in access logs if mutual TLS authentication fails.
            /// </summary>
            serialNumber : string option

            /// <summary>
            /// The rules for when the client cert is valid.
            /// </summary>
            validity : ClientCertValidity option
        }

        /// <summary>
        /// Container for the validation properties of a client cert.
        /// </summary>
        type ClientCertValidity =
            {
            /// <summary>
            /// The date before which the certificate is invalid. Present when a client accesses an API by using a custom domain name
            /// that has mutual TLS enabled. Present only in access logs if mutual TLS authentication fails.
            /// </summary>
            notBefore : string option

            /// <summary>
            /// The date after which the certificate is invalid. Present when a client accesses an API by using a custom domain name that
            /// has mutual TLS enabled. Present only in access logs if mutual TLS authentication fails.
            /// </summary>
            notAfter : string option
        }
