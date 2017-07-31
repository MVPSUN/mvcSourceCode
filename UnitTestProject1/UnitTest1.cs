using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Web.Mvc;
using Moq;
using AssertSpecical = Microsoft.TestCommon.Assert;
using System.Globalization;
using System.IO;
using System.Text;

namespace UnitTestProject1
{
    [TestClass]
    public class JsonValueProviderFactoryTest
    {
     




        [TestMethod]
        public void GetValueProvider_ComplexJsonObject()
        {
            // Arrange
            const string jsonString = @"
[
  { 
    ""BillingAddress"": {
      ""Street"": ""1 Microsoft Way"",
      ""City"": ""Redmond"",
      ""State"": ""WA"",
      ""ZIP"": 98052 },
    ""ShippingAddress"": { 
      ""Street"": ""123 Anywhere Ln"",
      ""City"": ""Anytown"",
      ""State"": ""ZZ"",
      ""ZIP"": 99999 }
  },
  { 
    ""Enchiladas"": [ ""Delicious"", ""Nutritious""]
  }
]
";

            ControllerContext cc = GetJsonEnabledControllerContext(jsonString);
            JsonValueProviderFactory factory = new JsonValueProviderFactory();

            // Act & assert
            IValueProvider valueProvider = factory.GetValueProvider(cc);
            //AssertSpecical.NotNull(valueProvider);

            //AssertSpecical.True(valueProvider.ContainsPrefix("[0].billingaddress"));
            //AssertSpecical.Null(valueProvider.GetValue("[0].billingaddress"));

            //AssertSpecical.True(valueProvider.ContainsPrefix("[0].billingaddress.street"));
            //AssertSpecical.NotNull(valueProvider.GetValue("[0].billingaddress.street"));

            ValueProviderResult vpResult1 = valueProvider.GetValue("[1].enchiladas[0]");
            //AssertSpecical.NotNull(vpResult1);
            //AssertSpecical.Equal("Delicious", vpResult1.AttemptedValue);
            //AssertSpecical.Equal(CultureInfo.CurrentCulture, vpResult1.Culture);
        }

        [TestMethod]
        public void GetValueProvider_NoJsonBody_ReturnsNull()
        {
            // Arrange
            Mock<ControllerContext> mockControllerContext = new Mock<ControllerContext>();
            mockControllerContext.Setup(o => o.HttpContext.Request.ContentType).Returns("application/json");
            mockControllerContext.Setup(o => o.HttpContext.Request.InputStream).Returns(new MemoryStream());

            JsonValueProviderFactory factory = new JsonValueProviderFactory();

            // Act
            IValueProvider valueProvider = factory.GetValueProvider(mockControllerContext.Object);

            // Assert
            // AssertSpecical.Null(valueProvider);
        }

        [TestMethod]
        public void GetValueProvider_NotJsonRequest_ReturnsNull()
        {
            // Arrange
            Mock<ControllerContext> mockControllerContext = new Mock<ControllerContext>();
            mockControllerContext.Setup(o => o.HttpContext.Request.ContentType).Returns("not JSON");

            JsonValueProviderFactory factory = new JsonValueProviderFactory();

            // Act
            IValueProvider valueProvider = factory.GetValueProvider(mockControllerContext.Object);

            // Assert
            // AssertSpecical.Null(valueProvider);
        }

        private static ControllerContext GetJsonEnabledControllerContext(string jsonString)
        {
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);
            MemoryStream jsonStream = new MemoryStream(jsonBytes);

            Mock<ControllerContext> mockControllerContext = new Mock<ControllerContext>();
            mockControllerContext.Setup(o => o.HttpContext.Request.ContentType).Returns("application/json");
            mockControllerContext.Setup(o => o.HttpContext.Request.InputStream).Returns(jsonStream);
            return mockControllerContext.Object;
        }
    }
}
