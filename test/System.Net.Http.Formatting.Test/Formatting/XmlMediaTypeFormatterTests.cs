﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Formatting.DataSets;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.TestCommon;
using Moq;
using Newtonsoft.Json;

namespace System.Net.Http.Formatting
{
    public class XmlMediaTypeFormatterTests : MediaTypeFormatterTestBase<XmlMediaTypeFormatter>
    {
        // Test data which should round-trip using a media type that includes type information.  A representative
        // sample only; avoids types DataContractJsonSerializer fails to round trip (e.g. Guid, Uint16).  May require
        // known types or similar (de)serializer configuration.
        public static readonly RefTypeTestData<object> BunchOfTypedObjectsTestData = new RefTypeTestData<object>(
            () => new List<object> { null, String.Empty, "This is a string", false, true, Double.MinValue,
                Double.MaxValue, Int32.MinValue, Int32.MaxValue, Int64.MinValue, Int64.MaxValue, DBNull.Value, });

        public static IEnumerable<TestData> BunchOfTypedObjectsTestDataCollection
        {
            get { return new TestData[] { BunchOfTypedObjectsTestData, }; }
        }

        public override IEnumerable<MediaTypeHeaderValue> ExpectedSupportedMediaTypes
        {
            get { return HttpTestData.StandardXmlMediaTypes; }
        }

        public override IEnumerable<Encoding> ExpectedSupportedEncodings
        {
            get { return HttpTestData.StandardEncodings; }
        }

        public override byte[] ExpectedSampleTypeByteRepresentation
        {
            get { return ExpectedSupportedEncodings.ElementAt(0).GetBytes("<DataContractSampleType xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.datacontract.org/2004/07/System.Net.Http.Formatting\"><Number>42</Number></DataContractSampleType>"); }
        }

        [Fact]
        void CopyConstructor()
        {
            TestXmlMediaTypeFormatter formatter = new TestXmlMediaTypeFormatter()
            {
                Indent = true,
#if !NETFX_CORE // We don't support MaxDepth in the portable library
                MaxDepth = 42,
#endif
                UseXmlSerializer = true
            };

            TestXmlMediaTypeFormatter derivedFormatter = new TestXmlMediaTypeFormatter(formatter);

            Assert.Equal(formatter.Indent, derivedFormatter.Indent);
#if !NETFX_CORE // We don't support MaxDepth in the portable library
            Assert.Equal(formatter.MaxDepth, derivedFormatter.MaxDepth);
#endif
            Assert.Equal(formatter.UseXmlSerializer, derivedFormatter.UseXmlSerializer);
        }

        [Fact]
        public void DefaultMediaType_ReturnsApplicationXml()
        {
            MediaTypeHeaderValue mediaType = XmlMediaTypeFormatter.DefaultMediaType;
            Assert.NotNull(mediaType);
            Assert.Equal("application/xml", mediaType.MediaType);
        }

#if !NETFX_CORE // We don't support MaxDepth in the portable library
        [Fact]
        public void MaxDepthReturnsCorrectValue()
        {
            Assert.Reflection.IntegerProperty(
                new XmlMediaTypeFormatter(),
                f => f.MaxDepth,
                expectedDefaultValue: 256,
                minLegalValue: 1,
                illegalLowerValue: 0,
                maxLegalValue: null,
                illegalUpperValue: null,
                roundTripTestValue: 10);
        }

        [Fact]
        public void ReadDeeplyNestedObjectThrows()
        {
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter() { MaxDepth = 1 };

            MemoryStream stream = new MemoryStream();
            formatter.WriteToStreamAsync(typeof(SampleType), new SampleType() { Number = 1 }, stream, null, null).Wait();
            stream.Position = 0;
            Task task = formatter.ReadFromStreamAsync(typeof(SampleType), stream, null, null);
            Assert.Throws<SerializationException>(() => task.Wait());
        }
#endif

        [Fact]
        public void Indent_RoundTrips()
        {
            Assert.Reflection.BooleanProperty(
                new XmlMediaTypeFormatter(),
                c => c.Indent,
                expectedDefaultValue: false);
        }

        [Fact]
        public void UseXmlSerializer_RoundTrips()
        {
            Assert.Reflection.BooleanProperty(
                new XmlMediaTypeFormatter(),
                c => c.UseXmlSerializer,
                expectedDefaultValue: false);
        }

        [Theory]
        [InlineData(typeof(IEnumerable<string>))]
        [InlineData(typeof(IQueryable<string>))]
        public void UseXmlFormatterWithNull(Type type)
        {
            XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter { UseXmlSerializer = false };
            MemoryStream memoryStream = new MemoryStream();
            HttpContent content = new StringContent(String.Empty);
            Assert.Task.Succeeds(xmlFormatter.WriteToStreamAsync(type, null, memoryStream, content, transportContext: null));
            memoryStream.Position = 0;
            string serializedString = new StreamReader(memoryStream).ReadToEnd();
            Assert.True(serializedString.Contains("nil=\"true\""),
                "Null value should be serialized as nil.");
            Assert.True(serializedString.ToLower().Contains("arrayofstring"),
                "It should be serialized out as an array of string.");
        }

        [Fact]
        public void UseXmlSerializer_False()
        {
            XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter { UseXmlSerializer = false };
            MemoryStream memoryStream = new MemoryStream();
            HttpContent content = new StringContent(String.Empty);
            Assert.Task.Succeeds(xmlFormatter.WriteToStreamAsync(typeof(SampleType), new SampleType(), memoryStream, content, transportContext: null));
            memoryStream.Position = 0;
            string serializedString = new StreamReader(memoryStream).ReadToEnd();
            Assert.True(serializedString.Contains("DataContractSampleType"),
                "SampleType should be serialized with data contract name DataContractSampleType because we're using DCS.");
            Assert.False(serializedString.Contains("version=\"1.0\" encoding=\"utf-8\""),
                    "Using DCS should not emit the xml declaration by default.");
            Assert.False(serializedString.Contains("\r\n"), "Using DCS should emit data without indentation by default.");
        }

        [Fact]
        public void UseXmlSerializer_False_Indent()
        {
            XmlMediaTypeFormatter xmlFormatter = new XmlMediaTypeFormatter { UseXmlSerializer = false, Indent = true };
            MemoryStream memoryStream = new MemoryStream();
            HttpContent content = new StringContent(String.Empty);
            Assert.Task.Succeeds(xmlFormatter.WriteToStreamAsync(typeof(SampleType), new SampleType(), memoryStream, content, transportContext: null));
            memoryStream.Position = 0;
            string serializedString = new StreamReader(memoryStream).ReadToEnd();
            Assert.True(serializedString.Contains("\r\n"), "Using DCS with indent set to true should emit data with indentation.");
        }

        [Fact]
        public void SetSerializer_ThrowsWithNullType()
        {
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(string));
            Assert.ThrowsArgumentNull(() => { formatter.SetSerializer(null, xmlSerializer); }, "type");
        }

        [Fact]
        public void SetSerializer_ThrowsWithNullSerializer()
        {
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            Assert.ThrowsArgumentNull(() => { formatter.SetSerializer(typeof(string), (XmlSerializer)null); }, "serializer");
        }

        [Fact]
        public void SetSerializer1_ThrowsWithNullSerializer()
        {
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            Assert.ThrowsArgumentNull(() => { formatter.SetSerializer<string>((XmlSerializer)null); }, "serializer");
        }

        [Fact]
        public void SetSerializer2_ThrowsWithNullType()
        {
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            XmlObjectSerializer xmlObjectSerializer = new DataContractSerializer(typeof(string));
            Assert.ThrowsArgumentNull(() => { formatter.SetSerializer(null, xmlObjectSerializer); }, "type");
        }

        [Fact]
        public void SetSerializer2_ThrowsWithNullSerializer()
        {
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            Assert.ThrowsArgumentNull(() => { formatter.SetSerializer(typeof(string), (XmlObjectSerializer)null); }, "serializer");
        }

        [Fact]
        public void SetSerializer3_ThrowsWithNullSerializer()
        {
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            Assert.ThrowsArgumentNull(() => { formatter.SetSerializer<string>((XmlSerializer)null); }, "serializer");
        }

        [Fact]
        public void RemoveSerializer_ThrowsWithNullType()
        {
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            Assert.ThrowsArgumentNull(() => { formatter.RemoveSerializer(null); }, "type");
        }

        [Fact]
        public void FormatterThrowsOnWriteWhenOverridenCreateFails()
        {
            // Arrange
            TestXmlMediaTypeFormatter formatter = new TestXmlMediaTypeFormatter();

            formatter.ThrowAnExceptionOnCreate = true;

            MemoryStream memoryStream = new MemoryStream();
            HttpContent content = new StringContent(String.Empty);

            // Act & Assert
            Action action = () => formatter.WriteToStreamAsync(typeof(SampleType), new SampleType(), memoryStream, content, transportContext: null).Wait();
            Assert.Throws<InvalidOperationException>(action);

            Assert.NotNull(formatter.InnerDataContractSerializer);
            Assert.Null(formatter.InnerXmlSerializer);
        }

        [Fact]
        public void FormatterThrowsOnWriteWhenOverridenCreateReturnsNull()
        {
            // Arrange
            TestXmlMediaTypeFormatter formatter = new TestXmlMediaTypeFormatter();

            formatter.ReturnNullOnCreate = true;

            MemoryStream memoryStream = new MemoryStream();
            HttpContent content = new StringContent(String.Empty);

            // Act & Assert
            Action action = () => formatter.WriteToStreamAsync(typeof(SampleType), new SampleType(), memoryStream, content, transportContext: null).Wait();
            Assert.Throws<InvalidOperationException>(action);

            Assert.NotNull(formatter.InnerDataContractSerializer);
            Assert.Null(formatter.InnerXmlSerializer);
        }

        [Fact]
        public void FormatterThrowsOnReadWhenOverridenCreateFails()
        {
            // Arrange
            TestXmlMediaTypeFormatter formatter = new TestXmlMediaTypeFormatter();

            formatter.ThrowAnExceptionOnCreate = true;

            byte[] array = Encoding.UTF8.GetBytes("foo");
            MemoryStream memoryStream = new MemoryStream(array);

            HttpContent content = new StringContent("foo");

            // Act & Assert
            Action action = () => formatter.ReadFromStreamAsync(typeof(SampleType), memoryStream, content, null).Wait();
            Assert.Throws<InvalidOperationException>(action);

            Assert.NotNull(formatter.InnerDataContractSerializer);
            Assert.Null(formatter.InnerXmlSerializer);
        }

        [Fact]
        public void FormatterThrowsOnReadWhenOverridenCreateReturnsNull()
        {
            // Arrange
            TestXmlMediaTypeFormatter formatter = new TestXmlMediaTypeFormatter();

            formatter.ReturnNullOnCreate = true;

            byte[] array = Encoding.UTF8.GetBytes("foo");
            MemoryStream memoryStream = new MemoryStream(array);

            HttpContent content = new StringContent("foo");

            // Act & Assert
            Action action = () => formatter.ReadFromStreamAsync(typeof(SampleType), memoryStream, content, null).Wait();

            Assert.Throws<InvalidOperationException>(action);

            Assert.NotNull(formatter.InnerDataContractSerializer);
            Assert.Null(formatter.InnerXmlSerializer);
        }

        [Fact]
        public void DataContractFormatterThrowsOnWriteWhenOverridenCreateFails()
        {
            // Arrange
            TestXmlMediaTypeFormatter formatter = new TestXmlMediaTypeFormatter();

            formatter.ThrowAnExceptionOnCreate = true;
            formatter.UseXmlSerializer = true;

            MemoryStream memoryStream = new MemoryStream();
            HttpContent content = new StringContent(String.Empty);

            // Act & Assert
            Action action = () => formatter.WriteToStreamAsync(typeof(SampleType), new SampleType(), memoryStream, content, transportContext: null).Wait();
            Assert.Throws<InvalidOperationException>(action);

            Assert.Null(formatter.InnerDataContractSerializer);
            Assert.NotNull(formatter.InnerXmlSerializer);
        }

        [Fact]
        public void DataContractFormatterThrowsOnWriteWhenOverridenCreateReturnsNull()
        {
            // Arrange
            TestXmlMediaTypeFormatter formatter = new TestXmlMediaTypeFormatter();

            formatter.ReturnNullOnCreate = true;
            formatter.UseXmlSerializer = true;

            MemoryStream memoryStream = new MemoryStream();
            HttpContent content = new StringContent(String.Empty);

            // Act & Assert
            Action action = () => formatter.WriteToStreamAsync(typeof(SampleType), new SampleType(), memoryStream, content, transportContext: null).Wait();
            Assert.Throws<InvalidOperationException>(action);

            Assert.Null(formatter.InnerDataContractSerializer);
            Assert.NotNull(formatter.InnerXmlSerializer);
        }

        [Fact]
        public void DataContractFormatterThrowsOnReadWhenOverridenCreateFails()
        {
            // Arrange
            TestXmlMediaTypeFormatter formatter = new TestXmlMediaTypeFormatter();

            formatter.ThrowAnExceptionOnCreate = true;
            formatter.UseXmlSerializer = true;

            byte[] array = Encoding.UTF8.GetBytes("foo");
            MemoryStream memoryStream = new MemoryStream(array);

            HttpContent content = new StringContent("foo");

            // Act & Assert
            Action action = () => formatter.ReadFromStreamAsync(typeof(SampleType), memoryStream, content, null).Wait();
            Assert.Throws<InvalidOperationException>(action);

            Assert.Null(formatter.InnerDataContractSerializer);
            Assert.NotNull(formatter.InnerXmlSerializer);
        }

        [Fact]
        public void DataContractFormatterThrowsOnReadWhenOverridenCreateReturnsNull()
        {
            // Arrange
            TestXmlMediaTypeFormatter formatter = new TestXmlMediaTypeFormatter();

            formatter.ReturnNullOnCreate = true;
            formatter.UseXmlSerializer = true;

            byte[] array = Encoding.UTF8.GetBytes("foo");
            MemoryStream memoryStream = new MemoryStream(array);

            HttpContent content = new StringContent("foo");

            // Act & Assert
            Action action = () => formatter.ReadFromStreamAsync(typeof(SampleType), memoryStream, content, null).Wait();

            Assert.Throws<InvalidOperationException>(action);

            Assert.Null(formatter.InnerDataContractSerializer);
            Assert.NotNull(formatter.InnerXmlSerializer);
        }

        [Theory]
        [TestDataSet(typeof(CommonUnitTestDataSets), "RepresentativeValueAndRefTypeTestDataCollection", RoundTripDataVariations)]
        public void ReadFromStreamAsync_RoundTripsWriteToStreamAsyncUsingXmlSerializer(Type variationType, object testData)
        {
            // Guard
            bool canSerialize = IsSerializableWithXmlSerializer(variationType, testData) && Assert.Http.CanRoundTrip(variationType);
            if (canSerialize)
            {
                // Arrange
                TestXmlMediaTypeFormatter formatter = new TestXmlMediaTypeFormatter();
                formatter.SetSerializer(variationType, new XmlSerializer(variationType));

                // Arrange & Act & Assert
                object readObj = ReadFromStreamAsync_RoundTripsWriteToStreamAsync_Helper(formatter, variationType, testData);
                Assert.Equal(testData, readObj);
            }
        }

        [Theory]
        [TestDataSet(typeof(XmlMediaTypeFormatterTests), "BunchOfTypedObjectsTestDataCollection", RoundTripDataVariations)]
        public void ReadFromStreamAsync_RoundTripsWriteToStreamAsyncUsingXmlSerializer_ExtraTypes(Type variationType, object testData)
        {
            // Guard
            bool canSerialize = IsSerializableWithXmlSerializer(variationType, testData) && Assert.Http.CanRoundTrip(variationType);
            if (canSerialize)
            {
                // Arrange
                TestXmlMediaTypeFormatter formatter = new TestXmlMediaTypeFormatter();
                formatter.SetSerializer(variationType, new XmlSerializer(variationType, new Type[] { typeof(DBNull), }));

                // Arrange & Act & Assert
                object readObj = ReadFromStreamAsync_RoundTripsWriteToStreamAsync_Helper(formatter, variationType, testData);
                Assert.Equal(testData, readObj);
            }
        }

        // Test alternate null value; this serializer attempts to cast DBNull to variationType so typeof(string) variation fails
        [Fact]
        public void ReadFromStreamAsync_RoundTripsWriteToStreamAsyncUsingXmlSerializer_DBNull()
        {
            // Arrange
            TestXmlMediaTypeFormatter formatter = new TestXmlMediaTypeFormatter();
            Type variationType = typeof(DBNull);
            formatter.SetSerializer(variationType, new XmlSerializer(variationType));
            object testData = DBNull.Value;

            // Arrange & Act & Assert
            object readObj = ReadFromStreamAsync_RoundTripsWriteToStreamAsync_Helper(formatter, variationType, testData);
            Assert.Equal(testData, readObj);
        }

        [Theory]
        [TestDataSet(typeof(CommonUnitTestDataSets), "RepresentativeValueAndRefTypeTestDataCollection", RoundTripDataVariations)]
        public void ReadFromStream_AsyncRoundTripsWriteToStreamUsingDataContractSerializer(Type variationType, object testData)
        {
            // Guard
            bool canSerialize = IsSerializableWithDataContractSerializer(variationType, testData) && Assert.Http.CanRoundTrip(variationType);
            if (canSerialize)
            {
                // Arrange
                TestXmlMediaTypeFormatter formatter = new TestXmlMediaTypeFormatter();
                formatter.SetSerializer(variationType, new DataContractSerializer(variationType));

                // Arrange & Act & Assert
                object readObj = ReadFromStreamAsync_RoundTripsWriteToStreamAsync_Helper(formatter, variationType, testData);
                Assert.Equal(testData, readObj);
            }
        }

        [Theory]
        [TestDataSet(typeof(XmlMediaTypeFormatterTests), "BunchOfTypedObjectsTestDataCollection", RoundTripDataVariations)]
        public void ReadFromStream_AsyncRoundTripsWriteToStreamUsingDataContractSerializer_KnownTypes(Type variationType, object testData)
        {
            // Guard
            bool canSerialize = IsSerializableWithDataContractSerializer(variationType, testData) && Assert.Http.CanRoundTrip(variationType);
            if (canSerialize)
            {
                // Arrange
                TestXmlMediaTypeFormatter formatter = new TestXmlMediaTypeFormatter();
                formatter.SetSerializer(variationType, new DataContractSerializer(variationType, new Type[] { typeof(DBNull), }));

                // Arrange & Act & Assert
                object readObj = ReadFromStreamAsync_RoundTripsWriteToStreamAsync_Helper(formatter, variationType, testData);
                Assert.Equal(testData, readObj);
            }
        }

        [Fact]
        public void ReadFromStreamAsync_RoundTripsWriteToStreamAsyncUsingDataContractSerializer_DBNull()
        {
            // Arrange
            TestXmlMediaTypeFormatter formatter = new TestXmlMediaTypeFormatter();
            Type variationType = typeof(DBNull);
            formatter.SetSerializer(variationType, new DataContractSerializer(variationType));
            object testData = DBNull.Value;

            // Arrange & Act & Assert
            object readObj = ReadFromStreamAsync_RoundTripsWriteToStreamAsync_Helper(formatter, variationType, testData);

            // DBNull.Value round-trips as either Object or DBNull because serialization includes its type
            Assert.Equal(testData, readObj);
        }

        [Fact]
        public void ReadFromStreamAsync_RoundTripsWriteToStreamAsyncUsingDataContractSerializer_DBNullAsEmptyString()
        {
            // Arrange
            TestXmlMediaTypeFormatter formatter = new TestXmlMediaTypeFormatter();
            Type variationType = typeof(string);
            formatter.SetSerializer(variationType, new DataContractSerializer(variationType, new Type[] { typeof(DBNull), }));
            object testData = DBNull.Value;

            // Arrange & Act & Assert
            object readObj = ReadFromStreamAsync_RoundTripsWriteToStreamAsync_Helper(formatter, variationType, testData);

            // Lower levels convert DBNull.Value to empty string on read
            Assert.Equal(String.Empty, readObj);
        }

        [Theory]
        [TestDataSet(typeof(HttpTestData), "ReadAndWriteCorrectCharacterEncoding")]
        public override Task ReadFromStreamAsync_UsesCorrectCharacterEncoding(string content, string encoding, bool isDefaultEncoding)
        {
            if (!isDefaultEncoding)
            {
                // XmlDictionaryReader/Writer only supports utf-8 and 16
                return TaskHelpers.Completed();
            }

            // Arrange
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();

            string formattedContent = "<string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">" + content + "</string>";
#if NETFX_CORE
            // We need to supply the xml declaration when compiled in portable library for non utf-8 content
            if (String.Equals("utf-16", encoding, StringComparison.OrdinalIgnoreCase))
            {
                formattedContent = "<?xml version=\"1.0\" encoding=\"UTF-16\"?>" + formattedContent;
            }
#endif
            string mediaType = string.Format("application/xml; charset={0}", encoding);

            // Act & assert
            return ReadFromStreamAsync_UsesCorrectCharacterEncodingHelper(formatter, content, formattedContent, mediaType, encoding, isDefaultEncoding);
        }

        [Fact]
        public void ReadFromStreamAsync_UsesGetDeserializerAndCreateXmlReader()
        {
            Type type = typeof(string);
            string xml = "<string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">x</string>";
            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            var serializer = new Mock<XmlObjectSerializer>() { CallBase = true };
            var reader = new Mock<XmlReader>() { CallBase = true };
            var formatter = new Mock<XmlMediaTypeFormatter>() { CallBase = true };
            formatter.Setup(f => f.GetDeserializer(type, null)).Returns(serializer.Object);
            formatter.Setup(f => f.CreateXmlReader(stream, null)).Returns(reader.Object);

            formatter.Object.ReadFromStreamAsync(type, stream, content: null, formatterLogger: null).Wait();

            serializer.Verify(s => s.ReadObject(reader.Object));
        }

        [Fact]
        public void ReadFromStreamAsync_ThrowsException_WhenGetDeserializerReturnsNull()
        {
            Type type = typeof(string);
            string xml = "<string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">x</string>";
            var formatter = new Mock<XmlMediaTypeFormatter>() { CallBase = true };
            formatter.Setup(f => f.GetDeserializer(type, null)).Returns(null);

            Assert.Throws<InvalidOperationException>(
                () => formatter.Object.ReadFromStreamAsync(type, new MemoryStream(Encoding.UTF8.GetBytes(xml)), content: null, formatterLogger: null).Wait(),
                "The object returned by GetDeserializer must not be a null value.");
        }

        [Fact]
        public void ReadFromStreamAsync_ThrowsException_WhenGetDeserializerReturnsInvalidType()
        {
            Type type = typeof(string);
            string xml = "<string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">x</string>";
            var formatter = new Mock<XmlMediaTypeFormatter>() { CallBase = true };
            formatter.Setup(f => f.GetDeserializer(type, null)).Returns(new JsonSerializer());

            Assert.Throws<InvalidOperationException>(
                () => formatter.Object.ReadFromStreamAsync(type, new MemoryStream(Encoding.UTF8.GetBytes(xml)), content: null, formatterLogger: null).Wait(),
                "The object of type 'JsonSerializer' returned by GetDeserializer must be an instance of either XmlObjectSerializer or XmlSerializer.");
        }

        [Theory]
        [TestDataSet(typeof(HttpTestData), "ReadAndWriteCorrectCharacterEncoding")]
        public override Task WriteToStreamAsync_UsesCorrectCharacterEncoding(string content, string encoding, bool isDefaultEncoding)
        {
            if (!isDefaultEncoding)
            {
                // XmlDictionaryReader/Writer only supports utf-8 and 16
                return TaskHelpers.Completed();
            }

            // Arrange
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            string formattedContent = "<string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">" + content +
                                      "</string>";
            string mediaType = string.Format("application/xml; charset={0}", encoding);

            // Act & assert
            return WriteToStreamAsync_UsesCorrectCharacterEncodingHelper(formatter, content, formattedContent, mediaType, encoding, isDefaultEncoding);
        }

        [Fact]
        public void WriteToStreamAsync_UsesGetSerializerAndCreateXmlWriter()
        {
            Type type = typeof(string);
            object value = "x";
            Stream stream = new MemoryStream();
            var serializer = new Mock<XmlObjectSerializer>() { CallBase = true };
            var writer = new Mock<XmlWriter>() { CallBase = true };
            var formatter = new Mock<XmlMediaTypeFormatter>() { CallBase = true };
            formatter.Setup(f => f.GetSerializer(type, value, null)).Returns(serializer.Object);
            formatter.Setup(f => f.CreateXmlWriter(stream, null)).Returns(writer.Object);

            formatter.Object.WriteToStreamAsync(type, value, stream, content: null, transportContext: null).Wait();

            serializer.Verify(s => s.WriteObject(writer.Object, value));
        }

        [Fact]
        public void WriteToStreamAsync_ThrowsException_WhenGetSerializerReturnsNull()
        {
            Type type = typeof(string);
            object value = "x";
            var formatter = new Mock<XmlMediaTypeFormatter>() { CallBase = true };
            formatter.Setup(f => f.GetSerializer(type, value, null)).Returns(null);

            Assert.Throws<InvalidOperationException>(
                () => formatter.Object.WriteToStreamAsync(type, value, new MemoryStream(), content: null, transportContext: null).Wait(),
                "The object returned by GetSerializer must not be a null value.");
        }

        [Fact]
        public void WriteToStreamAsync_ThrowsException_WhenGetSerializerReturnsInvalidType()
        {
            Type type = typeof(string);
            object value = "x";
            var formatter = new Mock<XmlMediaTypeFormatter>() { CallBase = true };
            formatter.Setup(f => f.GetSerializer(type, value, null)).Returns(new JsonSerializer());

            Assert.Throws<InvalidOperationException>(
                () => formatter.Object.WriteToStreamAsync(type, value, new MemoryStream(), content: null, transportContext: null).Wait(),
                "The object of type 'JsonSerializer' returned by GetSerializer must be an instance of either XmlObjectSerializer or XmlSerializer.");
        }

        [Fact]
        public void CreateXmlWriter_Uses_WriterSettings()
        {
            // Arrange
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            formatter.WriterSettings.ConformanceLevel = ConformanceLevel.Fragment;
            Stream stream = new MemoryStream();
            HttpContent content = new StreamContent(stream);

            // Act
            XmlWriter writer = formatter.CreateXmlWriter(stream, content);

            // Assert
            Assert.Equal(writer.Settings.ConformanceLevel, formatter.WriterSettings.ConformanceLevel);
        }

        [Fact]
        public void Property_WriterSettings_DefaultValues()
        {
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();

            Assert.NotNull(formatter.WriterSettings);
            Assert.False(formatter.WriterSettings.Indent);
            Assert.False(formatter.WriterSettings.CloseOutput);
            Assert.True(formatter.WriterSettings.OmitXmlDeclaration);
            Assert.False(formatter.WriterSettings.CheckCharacters);
        }

        [Fact]
        public void InvalidXmlCharacters_CanBeSerialized_Default()
        {
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            Stream stream = new MemoryStream();
            HttpContent content = new StreamContent(stream);

            formatter.WriteToStreamAsync(typeof(string), "\x16", stream, content, null).Wait();
        }

        [Fact]
        public void InvalidXmlCharacters_CannotBeSerialized_IfCheckCharactersIsTrue()
        {
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            formatter.WriterSettings.CheckCharacters = true;
            Stream stream = new MemoryStream();
            HttpContent content = new StreamContent(stream);

            Assert.Throws<ArgumentException>(
                () => formatter.WriteToStreamAsync(typeof(string), "\x16", stream, content, null).Wait(),
                "'\x16', hexadecimal value 0x16, is an invalid character.");
        }

#if !NETFX_CORE // Different behavior in portable libraries due to no DataContract validation
        [Fact]
        public void CanReadType_ReturnsFalse_ForInvalidDataContracts()
        {
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();

            Assert.False(formatter.CanReadType(typeof(InvalidDataContract)));
        }

        [Fact]
        public void CanWriteType_ReturnsFalse_ForInvalidDataContracts()
        {
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();

            Assert.False(formatter.CanWriteType(typeof(InvalidDataContract)));
        }
#else
        [Fact]
        public void CanReadType_InPortableLibrary_ReturnsFalse_ForInvalidDataContracts()
        {
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();

            // The formatter is unable to positively identify non readable types, so true is always returned
            Assert.True(formatter.CanReadType(typeof(InvalidDataContract)));
        }

        [Fact]
        public void CanWriteType_InPortableLibrary_ReturnsTrue_ForInvalidDataContracts()
        {
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();

            // The formatter is unable to positively identify non readable types, so true is always returned
            Assert.True(formatter.CanWriteType(typeof(InvalidDataContract)));
        }
#endif

        public class InvalidDataContract
        {
            // removing the default ctor makes this invalid
            public InvalidDataContract(string s)
            {
            }
        }

        public class TestXmlMediaTypeFormatter : XmlMediaTypeFormatter
        {
            public TestXmlMediaTypeFormatter()
            {
            }

            public TestXmlMediaTypeFormatter(TestXmlMediaTypeFormatter formatter)
                : base(formatter)
            {
            }

            public bool ThrowAnExceptionOnCreate { get; set; }
            public bool ReturnNullOnCreate { get; set; }
            public XmlSerializer InnerXmlSerializer { get; private set; }
            public DataContractSerializer InnerDataContractSerializer { get; private set; }

            public bool CanReadTypeCaller(Type type)
            {
                return CanReadType(type);
            }

            public bool CanWriteTypeCaller(Type type)
            {
                return CanWriteType(type);
            }

            public override XmlSerializer CreateXmlSerializer(Type type)
            {
                InnerXmlSerializer = base.CreateXmlSerializer(type);

                if (ReturnNullOnCreate)
                {
                    return null;
                }

                if (ThrowAnExceptionOnCreate)
                {
                    throw new Exception("Throwing exception directly, since it needs to get caught by a catch all");
                }

                return InnerXmlSerializer;
            }

            public override DataContractSerializer CreateDataContractSerializer(Type type)
            {
                InnerDataContractSerializer = base.CreateDataContractSerializer(type);

                if (ReturnNullOnCreate)
                {
                    return null;
                }

                if (ThrowAnExceptionOnCreate)
                {
                    throw new Exception("Throwing exception directly, since it needs to get caught by a catch all");
                }

                return InnerDataContractSerializer;
            }
        }

        private bool IsSerializableWithXmlSerializer(Type type, object obj)
        {
            if (Assert.Http.IsKnownUnserializable(type, obj))
            {
                return false;
            }

            try
            {
                new XmlSerializer(type);
                if (obj != null && obj.GetType() != type)
                {
                    new XmlSerializer(obj.GetType());
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        private bool IsSerializableWithDataContractSerializer(Type type, object obj)
        {
            if (Assert.Http.IsKnownUnserializable(type, obj))
            {
                return false;
            }

            try
            {
                new DataContractSerializer(type);
                if (obj != null && obj.GetType() != type)
                {
                    new DataContractSerializer(obj.GetType());
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
