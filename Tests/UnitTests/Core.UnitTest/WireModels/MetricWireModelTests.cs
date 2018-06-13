﻿using JetBrains.Annotations;
using NewRelic.Agent.Core.Aggregators;
using NewRelic.Agent.Core.Metrics;
using NewRelic.Testing.Assertions;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Telerik.JustMock;

namespace NewRelic.Agent.Core.WireModels
{
	[TestFixture]
	public class MetricWireModelTests
	{
		[NotNull] private readonly IMetricBuilder _metricBuilder = Utilities.GetSimpleMetricBuilder();

		[NotNull] private readonly IMetricNameService _metricNameService = Mock.Create<IMetricNameService>();

		[SetUp]
		public void SetUp()
		{
			Mock.Arrange(() => _metricNameService.RenameMetric(Arg.IsAny<string>())).Returns<string>(name => name);

		}

		#region AddMetricsToEngine

		[Test]
		public void AddMetricsToEngine_OneScopedMetric()
		{
			var metric1 = MetricWireModel.BuildMetric(_metricNameService, "DotNet/name", "scope",
				MetricDataWireModel.BuildTimingData(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(2)));
			Assert.That(metric1, Is.Not.Null);
			var data = metric1.Data;
			Assert.NotNull(data);
			Assert.AreEqual(1, data.Value0);
			Assert.AreEqual(3, data.Value1);
			Assert.AreEqual(2, data.Value2);

			var engine = new MetricStatsCollection();
			metric1.AddMetricsToEngine(engine);

			var actual = engine.ConvertToJsonForSending(_metricNameService);
			var unscopedCount = 0;
			var scopedCount = 0;
			var theScope = string.Empty;
			var metricName = string.Empty;
			MetricDataWireModel scopedData = null;
			foreach (var current in actual)
			{
				if (current.MetricName.Scope == null)
				{
					unscopedCount++;
				}
				else
				{
					scopedCount++;
					theScope = current.MetricName.Scope;
					metricName = current.MetricName.Name;
					scopedData = current.Data;
				}
			}

			Assert.AreEqual(1, scopedCount);
			Assert.AreEqual(0, unscopedCount);
			Assert.AreEqual("scope", theScope);
			Assert.AreEqual("DotNet/name", metricName);
			Assert.IsNotNull(scopedData);
			Assert.AreEqual(1, scopedData.Value0);
			Assert.AreEqual(3, scopedData.Value1);
			Assert.AreEqual(2, scopedData.Value2);
		}

		[Test]
		public void AddMetricsToEngine_OneUnscopedMetricNull()
		{
			var metric1 = MetricWireModel.BuildMetric(_metricNameService, "DotNet/name", null,
				MetricDataWireModel.BuildTimingData(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(2)));
			Assert.That(metric1, Is.Not.Null);
			var data = metric1.Data;
			Assert.NotNull(data);
			Assert.AreEqual(1, data.Value0);
			Assert.AreEqual(3, data.Value1);
			Assert.AreEqual(2, data.Value2);

			var engine = new MetricStatsCollection();
			metric1.AddMetricsToEngine(engine);

			var stats = engine.ConvertToJsonForSending(_metricNameService);

			foreach (var current in stats)
			{
				Assert.AreEqual("DotNet/name", current.MetricName.Name);
				Assert.AreEqual(null, current.MetricName.Scope);
				var myData = current.Data;
				Assert.AreEqual(1, myData.Value0);
				Assert.AreEqual(3, myData.Value1);
				Assert.AreEqual(2, myData.Value2);
			}
		}

		[Test]
		public void AddMetricsToEngine_OneUnscopedMetricEmptyString()
		{
			var metric1 = MetricWireModel.BuildMetric(_metricNameService, "DotNet/name", "",
				MetricDataWireModel.BuildTimingData(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(2)));
			Assert.That(metric1, Is.Not.Null);

			var data = metric1.Data;
			Assert.NotNull(data);
			Assert.AreEqual(1, data.Value0);
			Assert.AreEqual(3, data.Value1);
			Assert.AreEqual(2, data.Value2);

			var engine = new MetricStatsCollection();
			metric1.AddMetricsToEngine(engine);

			var stats = engine.ConvertToJsonForSending(_metricNameService);

			foreach (var current in stats)
			{
				Assert.AreEqual("DotNet/name", current.MetricName.Name);
				Assert.AreEqual("", current.MetricName.Scope);
				var myData = current.Data;
				Assert.AreEqual(1, myData.Value0);
				Assert.AreEqual(3, myData.Value1);
				Assert.AreEqual(2, myData.Value2);
			}
		}

		#endregion AddMetricsToEngine

		#region Merge

		[Test]
		public void Merge_MergesOneMetricCorrectly()
		{
			var metric1 = MetricWireModel.BuildMetric(_metricNameService, "DotNet/name", "scope",
				MetricDataWireModel.BuildTimingData(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1)));
			var mergedMetric = MetricWireModel.Merge(new[] {metric1});

			NrAssert.Multiple(
				() => Assert.AreEqual("DotNet/name", mergedMetric.MetricName.Name),
				() => Assert.AreEqual(1, mergedMetric.Data.Value0),
				() => Assert.AreEqual(3, mergedMetric.Data.Value1),
				() => Assert.AreEqual(1, mergedMetric.Data.Value2),
				() => Assert.AreEqual(3, mergedMetric.Data.Value3),
				() => Assert.AreEqual(3, mergedMetric.Data.Value4),
				() => Assert.AreEqual(9, mergedMetric.Data.Value5)
			);
		}

		[Test]
		public void Merge_MergesTwoMetricsCorrectly()
		{
			var metric1 = MetricWireModel.BuildMetric(_metricNameService, "DotNet/name", "scope",
				MetricDataWireModel.BuildTimingData(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1)));
			var metric2 = MetricWireModel.BuildMetric(_metricNameService, "DotNet/name", "scope",
				MetricDataWireModel.BuildTimingData(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(5)));

			var mergedMetric = MetricWireModel.Merge(new[] {metric1, metric2});

			NrAssert.Multiple(
				() => Assert.AreEqual("DotNet/name", mergedMetric.MetricName.Name),
				() => Assert.AreEqual(2, mergedMetric.Data.Value0),
				() => Assert.AreEqual(10, mergedMetric.Data.Value1),
				() => Assert.AreEqual(6, mergedMetric.Data.Value2),
				() => Assert.AreEqual(3, mergedMetric.Data.Value3),
				() => Assert.AreEqual(7, mergedMetric.Data.Value4),
				() => Assert.AreEqual(58, mergedMetric.Data.Value5)
			);
		}

		[Test]
		public void Merge_MergesThreeMetricsCorrectly()
		{
			var metric1 = MetricWireModel.BuildMetric(_metricNameService, "DotNet/name", "scope",
				MetricDataWireModel.BuildTimingData(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1)));
			var metric2 = MetricWireModel.BuildMetric(_metricNameService, "DotNet/name", "scope",
				MetricDataWireModel.BuildTimingData(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(5)));
			var metric3 = MetricWireModel.BuildMetric(_metricNameService, "DotNet/name", "scope",
				MetricDataWireModel.BuildTimingData(TimeSpan.FromSeconds(13), TimeSpan.FromSeconds(11)));

			var mergedMetric = MetricWireModel.Merge(new[] {metric1, metric2, metric3});

			NrAssert.Multiple(
				() => Assert.AreEqual("DotNet/name", mergedMetric.MetricName.Name),
				() => Assert.AreEqual(3, mergedMetric.Data.Value0),
				() => Assert.AreEqual(23, mergedMetric.Data.Value1),
				() => Assert.AreEqual(17, mergedMetric.Data.Value2),
				() => Assert.AreEqual(3, mergedMetric.Data.Value3),
				() => Assert.AreEqual(13, mergedMetric.Data.Value4),
				() => Assert.AreEqual(227, mergedMetric.Data.Value5)
			);
		}

		[Test]
		public void Merge_MergesThreeMetricsCorrectly_WhenMergedProgressively()
		{
			var metric1 = MetricWireModel.BuildMetric(_metricNameService, "DotNet/name", "scope",
				MetricDataWireModel.BuildTimingData(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1)));
			var metric2 = MetricWireModel.BuildMetric(_metricNameService, "DotNet/name", "scope",
				MetricDataWireModel.BuildTimingData(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(5)));
			var metric3 = MetricWireModel.BuildMetric(_metricNameService, "DotNet/name", "scope",
				MetricDataWireModel.BuildTimingData(TimeSpan.FromSeconds(13), TimeSpan.FromSeconds(11)));

			var mergedMetric = MetricWireModel.Merge(new[] {metric1, metric2});
			mergedMetric = MetricWireModel.Merge(new[] {mergedMetric, metric3});

			NrAssert.Multiple(
				() => Assert.AreEqual("DotNet/name", mergedMetric.MetricName.Name),
				() => Assert.AreEqual(3, mergedMetric.Data.Value0),
				() => Assert.AreEqual(23, mergedMetric.Data.Value1),
				() => Assert.AreEqual(17, mergedMetric.Data.Value2),
				() => Assert.AreEqual(3, mergedMetric.Data.Value3),
				() => Assert.AreEqual(13, mergedMetric.Data.Value4),
				() => Assert.AreEqual(227, mergedMetric.Data.Value5)
			);
		}

		[Test]
		public void Merge_IgnoresNullMetrics()
		{
			var metric1 = MetricWireModel.BuildMetric(_metricNameService, "DotNet/name", "scope",
				MetricDataWireModel.BuildTimingData(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1)));

			var mergedMetric = MetricWireModel.Merge(new[] {null, metric1, null});

			NrAssert.Multiple(
				() => Assert.AreEqual("DotNet/name", mergedMetric.MetricName.Name),
				() => Assert.AreEqual(1, mergedMetric.Data.Value0),
				() => Assert.AreEqual(3, mergedMetric.Data.Value1),
				() => Assert.AreEqual(1, mergedMetric.Data.Value2),
				() => Assert.AreEqual(3, mergedMetric.Data.Value3),
				() => Assert.AreEqual(3, mergedMetric.Data.Value4),
				() => Assert.AreEqual(9, mergedMetric.Data.Value5)
			);
		}

		[Test]
		public void Merge_Throws_IfGivenMetricsWithDifferentNames()
		{
			var metric1 = MetricWireModel.BuildMetric(_metricNameService, "DotNet/name", "scope",
				MetricDataWireModel.BuildTimingData(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1)));
			var metric2 = MetricWireModel.BuildMetric(_metricNameService, "DotNet/name1", "scope",
				MetricDataWireModel.BuildTimingData(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(5)));
			var metric3 = MetricWireModel.BuildMetric(_metricNameService, "DotNet/name2", "scope",
				MetricDataWireModel.BuildTimingData(TimeSpan.FromSeconds(13), TimeSpan.FromSeconds(11)));

			NrAssert.Throws<Exception>(() => MetricWireModel.Merge(new[] {metric1, metric2, metric3}));
		}

		[Test]
		public void Merge_Throws_IfGivenMetricsWithDifferentScopes()
		{
			var metric1 = MetricWireModel.BuildMetric(_metricNameService, "DotNet/name", "scope",
				MetricDataWireModel.BuildTimingData(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1)));
			var metric2 = MetricWireModel.BuildMetric(_metricNameService, "DotNet/name", "scope1",
				MetricDataWireModel.BuildTimingData(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(5)));
			var metric3 = MetricWireModel.BuildMetric(_metricNameService, "DotNet/name", "scope2",
				MetricDataWireModel.BuildTimingData(TimeSpan.FromSeconds(13), TimeSpan.FromSeconds(11)));


			NrAssert.Throws<Exception>(() => MetricWireModel.Merge(new[] {metric1, metric2, metric3}));
		}

		[Test]
		public void Merge_ThrowsIfAllNullsGiven()
		{
			NrAssert.Throws<Exception>(() => MetricWireModel.Merge(new MetricWireModel[] {null, null}));
		}

		[Test]
		public void Merge_ThrowsIfEmptyListGiven()
		{
			NrAssert.Throws<Exception>(() => MetricWireModel.Merge(new MetricWireModel[] { }));
		}

		#endregion Merge

		#region Serialization

		[Test]
		public void MetricWireModel_SerializesCorrectlyScoped()
		{
			var metric1 = MetricWireModel.BuildMetric(_metricNameService, "DotNet/name", "scope1",
				MetricDataWireModel.BuildTimingData(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1)));

			var serializedMetric = JsonConvert.SerializeObject(metric1);

			const string expectedJson = @"[{""name"":""DotNet/name"",""scope"":""scope1""},[1,3.0,1.0,3.0,3.0,9.0]]";
			Assert.AreEqual(expectedJson, serializedMetric);
		}

		[Test]
		public void MetricWireModel_SerializesCorrectlyUnscoped()
		{
			var metric1 = MetricWireModel.BuildMetric(_metricNameService, "DotNet/name", null,
				MetricDataWireModel.BuildTimingData(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1)));

			var serializedMetric = JsonConvert.SerializeObject(metric1);

			const string expectedJson = @"[{""name"":""DotNet/name""},[1,3.0,1.0,3.0,3.0,9.0]]";
			Assert.AreEqual(expectedJson, serializedMetric);
		}

		#endregion

		#region BuildAggregateData

		[Test]
		public void BuildAggregateData()
		{
			var one = MetricDataWireModel.BuildTimingData(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(4));
			var two = MetricDataWireModel.BuildTimingData(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(2));
			var actual = MetricDataWireModel.BuildAggregateData(one, two);
			Assert.AreEqual(2, actual.Value0);
			Assert.AreEqual(12, actual.Value1);
			Assert.AreEqual(6, actual.Value2);
			Assert.AreEqual(5, actual.Value3);
			Assert.AreEqual(7, actual.Value4);
			Assert.AreEqual(one.Value5 + two.Value5, actual.Value5);
		}

		#endregion BuildAggregateData

		#region DistributedTracing
		private static List<TestCaseData> GetSupportabilityDistributedTraceTestData()
		{
			return new List<TestCaseData>()
			{
				new TestCaseData( "TryBuildAcceptPayloadException", "Supportability/DistributedTrace/AcceptPayload/Exception" ),
				new TestCaseData( "TryBuildAcceptPayloadSuccess", "Supportability/DistributedTrace/AcceptPayload/Success"),
				new TestCaseData( "TryBuildAcceptPayloadException", "Supportability/DistributedTrace/AcceptPayload/Exception"),
				new TestCaseData( "TryBuildAcceptPayloadParseException", "Supportability/DistributedTrace/AcceptPayload/ParseException"),
				new TestCaseData( "TryBuildAcceptPayloadIgnoredCreateBeforeAccept", "Supportability/DistributedTrace/AcceptPayload/Ignored/CreateBeforeAccept"),
				new TestCaseData( "TryBuildAcceptPayloadIgnoredMultiple", "Supportability/DistributedTrace/AcceptPayload/Ignored/Multiple"),
				new TestCaseData( "TryBuildAcceptPayloadIgnoredMajorVersion", "Supportability/DistributedTrace/AcceptPayload/Ignored/MajorVersion"),
				new TestCaseData( "TryBuildAcceptPayloadIgnoredNull", "Supportability/DistributedTrace/AcceptPayload/Ignored/Null"),
				new TestCaseData( "TryBuildAcceptPayloadIgnoredUntrustedAccount", "Supportability/DistributedTrace/AcceptPayload/Ignored/UntrustedAccount"),
				new TestCaseData( "TryBuildCreatePayloadSuccess", "Supportability/DistributedTrace/CreatePayload/Success"),
				new TestCaseData( "TryBuildCreatePayloadException", "Supportability/DistributedTrace/CreatePayload/Exception"),
			};
		}

		[Test]
		[TestCaseSource(nameof(GetSupportabilityDistributedTraceTestData))]
		public void MetricWireModelTests_MetricBuilder_SupportabiltiyDistributedTracing(string propertyName, string name)
		{
			var propertyInfo = _metricBuilder.GetType().GetProperty(propertyName);
			Assert.That(propertyInfo, Is.Not.Null);

			var obj =  propertyInfo.GetValue(_metricBuilder);
			Assert.That(obj, Is.Not.Null);
			Assert.That(obj, Is.InstanceOf(typeof(MetricWireModel)));

			var wireModel = obj as MetricWireModel;
			Assert.That(wireModel, Is.Not.Null);

			Assert.That(wireModel.MetricName.Name, Is.EqualTo(name));
			Assert.That(wireModel.Data.Value0, Is.EqualTo(1));
			Assert.That(wireModel.Data.Value1, Is.EqualTo(0));
			Assert.That(wireModel.Data.Value2, Is.EqualTo(0));
			Assert.That(wireModel.Data.Value3, Is.EqualTo(0));
			Assert.That(wireModel.Data.Value4, Is.EqualTo(0));
			Assert.That(wireModel.Data.Value5, Is.EqualTo(0));
		}

		#endregion DistributedTracing
	}
}