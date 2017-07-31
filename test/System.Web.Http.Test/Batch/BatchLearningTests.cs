﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Hosting;
using System.Web.Http.Routing;
using Microsoft.TestCommon;

namespace System.Web.Http.Batch
{
    public class BatchLearningTests
    {
        [Fact]
        public void BatchRequest_SubRequestPropertiesDoNotContainRoutingContext_CopyProperties()
        {
            // Arrange
            const string baseAddress = "http://localhost/api/";
            HttpConfiguration config = new HttpConfiguration();
            HttpServer server = new HttpServer(config);
            config.Routes.MapHttpBatchRoute(
                routeName: "Batch",
                routeTemplate: "api/$batch",
                batchHandler: new CustomHttpBatchHandler(server));
            config.Routes.MapHttpRoute(
                "Default",
                "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional });

            // Act
            using (HttpClient client = new HttpClient(server))
            using (HttpRequestMessage batchRequest = new HttpRequestMessage(HttpMethod.Post, baseAddress + "$batch"))
            {
                batchRequest.Content = new MultipartContent("mixed")
                {
                    new HttpMessageContent(
                        new HttpRequestMessage(HttpMethod.Post, baseAddress + "values")
                        {
                            Content = new ObjectContent<string>("newValue", new JsonMediaTypeFormatter())
                        }),
                    new HttpMessageContent(new HttpRequestMessage(HttpMethod.Get, baseAddress + "values/newValue"))
                };

                using (HttpResponseMessage batchResponse = client.SendAsync(batchRequest, CancellationToken.None).Result)
                {
                    MultipartStreamProvider streamProvider = batchResponse.Content.ReadAsMultipartAsync().Result;
                    foreach (HttpContent content in streamProvider.Contents)
                    {
                        HttpResponseMessage response = content.ReadAsHttpResponseMessageAsync().Result;
                        string result = response.Content.ReadAsStringAsync().Result;

                        // Assert
                        Assert.Equal("\"newValue\"", result);
                    }
                }
            }
        }

        public class ValuesController : ApiController
        {
            public string Get(string id)
            {
                return id;
            }

            public string Post([FromBody] string value)
            {
                return value;
            }
        }

        public class CustomHttpBatchHandler : DefaultHttpBatchHandler
        {
            public CustomHttpBatchHandler(HttpServer httpServer)
                : base(httpServer)
            {
            }

            public override async Task<IList<HttpRequestMessage>> ParseBatchRequestsAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                IList<HttpRequestMessage> subRequests = await base.ParseBatchRequestsAsync(request, cancellationToken);

                // Assert
                Assert.NotNull(subRequests);
                foreach (HttpRequestMessage subRequest in subRequests)
                {
                    Assert.NotNull(subRequest);
                    Assert.Equal(2, subRequest.Properties.Count);
                    Assert.True(subRequest.Properties.ContainsKey(HttpPropertyKeys.RequestContextKey));
                    Assert.True(subRequest.Properties.ContainsKey(HttpPropertyKeys.IsBatchRequest));
                    Assert.False(subRequest.Properties.ContainsKey(HttpRoute.RoutingContextKey));
                }

                return subRequests;
            }
        }
    }
}
