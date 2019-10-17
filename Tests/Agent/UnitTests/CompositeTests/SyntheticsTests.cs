﻿using NewRelic.Agent.Api;
using NewRelic.Agent.Configuration;
using NewRelic.Agent.Core.Transactions;
using NewRelic.Agent.Core.Wrapper.AgentWrapperApi.Synthetics;
using NewRelic.Agent.Extensions.Providers.Wrapper;
using NewRelic.Core;
using NewRelic.SystemExtensions.Collections.Generic;
using NewRelic.Testing.Assertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Telerik.JustMock;

namespace CompositeTests
{
	internal class SyntheticsTests
	{
		private static CompositeTestAgent _compositeTestAgent;

		private IAgent _agent;

		[SetUp]
		public void SetUp()
		{
			_compositeTestAgent = new CompositeTestAgent();
			_agent = _compositeTestAgent.GetAgent();
		}

		[TearDown]
		public static void TearDown()
		{
			_compositeTestAgent.Dispose();
		}

		[Test]
		public void synthetics_attributes_when_header_is_present()
		{
			// ARRANGE
			var encodingKey = "foo";
			var version = 1;
			var clientAccountId = 123;
			var resourceId = "resourceId";
			var jobId = "jobId";
			var monitorId = "monitorId";
			_compositeTestAgent.ServerConfiguration.TrustedIds = new Int64[] {clientAccountId};
			_compositeTestAgent.ServerConfiguration.EncodingKey = encodingKey;
			_compositeTestAgent.PushConfiguration();

			var syntheticsHeader = new KeyValuePair<string, string>("X-NewRelic-Synthetics",
				Strings.Base64Encode(
					string.Format(@"[{0}, {1}, ""{2}"", ""{3}"", ""{4}""]", version, clientAccountId, resourceId, jobId,
						monitorId), encodingKey));
			var requestHeaders = new[] {syntheticsHeader};


			// ==== ACT ====
			var tx = _agent.CreateTransaction(
				isWeb: true,
				category: EnumNameCache<WebTransactionType>.GetName(WebTransactionType.Action),
				transactionDisplayName: "name",
				doNotTrackAsUnitOfWork: true);
			_agent.ProcessInboundRequest(requestHeaders, TransportType.HTTP);
			var segment = _agent.StartTransactionSegmentOrThrow("segmentName");
			segment.End();
			tx.End();
			_compositeTestAgent.Harvest();
			// ==== ACT ====


			// ASSERT
			var unexpectedEventAttributes = new List<string>
			{
				"nr.alternatePathHashes"
			};
			var expectedEventAttributes = new List<ExpectedAttribute>
			{
				new ExpectedAttribute {Key = "nr.guid"},
				new ExpectedAttribute {Key = "nr.syntheticsResourceId", Value = resourceId},
				new ExpectedAttribute {Key = "nr.syntheticsJobId", Value = jobId},
				new ExpectedAttribute {Key = "nr.syntheticsMonitorId", Value = monitorId}
			};
			var expectedTraceAttributes = new List<ExpectedAttribute>
			{
				new ExpectedAttribute {Key = "synthetics_resource_id", Value = resourceId},
				new ExpectedAttribute {Key = "synthetics_job_id", Value = jobId},
				new ExpectedAttribute {Key = "synthetics_monitor_id", Value = monitorId}
			};
			var transactionEvent = _compositeTestAgent.TransactionEvents.First();
			var transactionTrace = _compositeTestAgent.TransactionTraces.First();
			NrAssert.Multiple(
				() =>
					TransactionEventAssertions.HasAttributes(expectedEventAttributes, AttributeClassification.Intrinsics,
						transactionEvent),
				() =>
					TransactionEventAssertions.DoesNotHaveAttributes(unexpectedEventAttributes,
						AttributeClassification.Intrinsics, transactionEvent),
				() =>
					TransactionTraceAssertions.HasAttributes(expectedTraceAttributes, AttributeClassification.Intrinsics,
						transactionTrace)
				);
		}

		[Test]
		public void synthetics_attributes_when_no_header_is_present()
		{
			// ARRANGE
			var encodingKey = "foo";
			_compositeTestAgent.ServerConfiguration.TrustedIds = new Int64[] {123};
			_compositeTestAgent.ServerConfiguration.EncodingKey = encodingKey;
			_compositeTestAgent.PushConfiguration();


			// ==== ACT ====
			var tx = _agent.CreateTransaction(
				isWeb: true,
				category: EnumNameCache<WebTransactionType>.GetName(WebTransactionType.Action),
				transactionDisplayName: "name",
				doNotTrackAsUnitOfWork: true);
			_agent.ProcessInboundRequest(new KeyValuePair<string, string>[0], TransportType.HTTP);
			var segment = _agent.StartTransactionSegmentOrThrow("segmentName");
			segment.End();
			tx.End();
			_compositeTestAgent.Harvest();
			// ==== ACT ====


			// ASSERT
			var unexpectedEventAttributes = new List<string>
			{
				"nr.guid",
				"nr.syntheticsResourceId",
				"nr.syntheticsJobId",
				"nr.syntheticsMonitorId"
            };
			var unexpectedTraceAttributes = new List<string>
			{
				"synthetics_resource_id",
				"synthetics_job_id",
				"synthetics_monitor_id"
			};
			var transactionEvent = _compositeTestAgent.TransactionEvents.First();
			var transactionTrace = _compositeTestAgent.TransactionTraces.First();
			NrAssert.Multiple(
				() => TransactionEventAssertions.DoesNotHaveAttributes(unexpectedEventAttributes, AttributeClassification.Intrinsics, transactionEvent),
				() => TransactionTraceAssertions.DoesNotHaveAttributes(unexpectedTraceAttributes, AttributeClassification.Intrinsics, transactionTrace)
				);
		}

		[Test]
		public void synthetics_attributes_when_no_headers_are_provided()
		{
			// ARRANGE
			var encodingKey = "foo";
			_compositeTestAgent.ServerConfiguration.TrustedIds = new Int64[] { 123 };
			_compositeTestAgent.ServerConfiguration.EncodingKey = encodingKey;
			_compositeTestAgent.PushConfiguration();


			// ==== ACT ====
			var tx = _agent.CreateTransaction(
				isWeb: true,
				category: EnumNameCache<WebTransactionType>.GetName(WebTransactionType.Action),
				transactionDisplayName: "name",
				doNotTrackAsUnitOfWork: true);
			_agent.StartTransactionSegmentOrThrow("segmentName").End();
			tx.End();
			_compositeTestAgent.Harvest();
			// ==== ACT ====


			// ASSERT
			var unexpectedEventAttributes = new List<string>
			{
				"nr.guid",
				"nr.syntheticsResourceId",
				"nr.syntheticsJobId",
				"nr.syntheticsMonitorId"
			};
			var unexpectedTraceAttributes = new List<string>
			{
				"synthetics_resource_id",
				"synthetics_job_id",
				"synthetics_monitor_id"
			};
			var transactionEvent = _compositeTestAgent.TransactionEvents.First();
			var transactionTrace = _compositeTestAgent.TransactionTraces.First();
			NrAssert.Multiple(
				() => TransactionEventAssertions.DoesNotHaveAttributes(unexpectedEventAttributes, AttributeClassification.Intrinsics, transactionEvent),
				() => TransactionTraceAssertions.DoesNotHaveAttributes(unexpectedTraceAttributes, AttributeClassification.Intrinsics, transactionTrace)
				);
		}

		[Test]
		public void synthetics_header_is_generated_when_outbound_request_is_made()
		{
			// ARRANGE
			var encodingKey = "foo";
			var version = 1;
			var clientAccountId = 123;
			var resourceId = "resourceId";
			var jobId = "jobId";
			var monitorId = "monitorId";
			_compositeTestAgent.ServerConfiguration.TrustedIds = new Int64[] { clientAccountId };
			_compositeTestAgent.ServerConfiguration.CatId = "123#456";
            _compositeTestAgent.ServerConfiguration.EncodingKey = encodingKey;
			_compositeTestAgent.PushConfiguration();

			var syntheticsHeader = new KeyValuePair<string, string>("X-NewRelic-Synthetics",
				Strings.Base64Encode(
					string.Format(@"[{0}, {1}, ""{2}"", ""{3}"", ""{4}""]", version, clientAccountId, resourceId, jobId,
						monitorId), encodingKey));
			var requestHeaders = new[] { syntheticsHeader };


			// ==== ACT ====
			_agent.CreateTransaction(
				isWeb: true,
				category: EnumNameCache<WebTransactionType>.GetName(WebTransactionType.Action),
				transactionDisplayName: "name",
				doNotTrackAsUnitOfWork: true);
			_agent.ProcessInboundRequest(requestHeaders, TransportType.HTTP);

			var headers = _agent.CurrentTransaction.GetRequestMetadata().ToDictionary();

			Assert.NotNull(headers);
			Assert.AreEqual("PV5DV11cSk0dAxwAEx0MAyYLRENNDAANLwtNSk0CCQEGEgAdLwtNOw==", headers["X-NewRelic-Synthetics"]);
		}


		[Test]
		public void synthetics_header_can_be_decoded_from_outbound_request()
		{
			// ARRANGE
			var encodingKey = "foo";
			var version = 1;
			var clientAccountId = 123;
			var resourceId = "resourceId";
			var jobId = "jobId";
			var monitorId = "monitorId";
			_compositeTestAgent.ServerConfiguration.TrustedIds = new Int64[] { clientAccountId };
			_compositeTestAgent.ServerConfiguration.CatId = "123#456";
			_compositeTestAgent.ServerConfiguration.EncodingKey = encodingKey;
			_compositeTestAgent.PushConfiguration();

			var syntheticsHeader = new KeyValuePair<string, string>("X-NewRelic-Synthetics",
				Strings.Base64Encode(
					string.Format(@"[{0}, {1}, ""{2}"", ""{3}"", ""{4}""]", version, clientAccountId, resourceId, jobId,
						monitorId), encodingKey));
			var requestHeaders = new[] { syntheticsHeader };

			// ==== ACT ====
			_agent.CreateTransaction(
				isWeb: true,
				category: EnumNameCache<WebTransactionType>.GetName(WebTransactionType.Action),
				transactionDisplayName: "name",
				doNotTrackAsUnitOfWork: true);
			_agent.ProcessInboundRequest(requestHeaders, TransportType.HTTP);

			var headers = _agent.CurrentTransaction.GetRequestMetadata().ToDictionary();

			Assert.NotNull(headers);

			var configurationService = Mock.Create<IConfigurationService>();
			Mock.Arrange(() => configurationService.Configuration).Returns(() => _compositeTestAgent.CurrentConfiguration);

			var syntheticsHeaderHandler = new SyntheticsHeaderHandler(configurationService);
			SyntheticsHeader decodedSyntheticsHeader = syntheticsHeaderHandler.TryDecodeInboundRequestHeaders(headers);

			NrAssert.Multiple(
				() => Assert.AreEqual("PV5DV11cSk0dAxwAEx0MAyYLRENNDAANLwtNSk0CCQEGEgAdLwtNOw==", headers["X-NewRelic-Synthetics"]),
				() => Assert.AreEqual(decodedSyntheticsHeader.Version, version),
				() => Assert.AreEqual(decodedSyntheticsHeader.JobId, jobId),
				() => Assert.AreEqual(decodedSyntheticsHeader.AccountId, clientAccountId),
				() => Assert.AreEqual(decodedSyntheticsHeader.MonitorId, monitorId),
				() => Assert.AreEqual(decodedSyntheticsHeader.ResourceId, resourceId) 
			);
		}
	}	
}
