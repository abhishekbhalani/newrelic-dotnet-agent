﻿using System.Threading.Tasks;
using NewRelic.Agent.Extensions.Providers.Wrapper;
using NewRelic.Agent.Extensions.Parsing;

namespace NewRelic.Providers.Wrapper.MongoDb26
{
	public class MongoCollectionBaseWrapper : IWrapper
	{
		public bool IsTransactionRequired => true;

		public CanWrapResponse CanWrap(InstrumentedMethodInfo methodInfo)
		{
			var method = methodInfo.Method;

			var canWrap = method.MatchesAny(assemblyName: "MongoDB.Driver", typeName: "MongoDB.Driver.MongoCollectionBase`1",
				methodNames: new[]
				{
					"DeleteMany",
					"DeleteOne",
					"InsertOne",
					"InsertMany",
					"ReplaceOne",
					"UpdateMany",
					"UpdateOne",

					"DeleteManyAsync",
					"DeleteOneAsync",
					"InsertOneAsync",
					"InsertManyAsync",
					"ReplaceOneAsync",
					"UpdateManyAsync",
					"UpdateOneAsync"
				});

			return new CanWrapResponse(canWrap);
		}

		public AfterWrappedMethodDelegate BeforeWrappedMethod(InstrumentedMethodCall instrumentedMethodCall, IAgentWrapperApi agentWrapperApi, ITransaction transaction)
		{
			var operation = instrumentedMethodCall.MethodCall.Method.MethodName;
			dynamic caller = instrumentedMethodCall.MethodCall.InvocationTarget;

			var model = caller.CollectionNamespace.CollectionName;

			ConnectionInfo connectionInfo = MongoDbHelper.GetConnectionInfoFromDatabase(caller.Database);

			var segment = transaction.StartDatastoreSegment(instrumentedMethodCall.MethodCall,
				new ParsedSqlStatement(DatastoreVendor.MongoDB, model, operation), isLeaf: true, connectionInfo: connectionInfo);

			if (!instrumentedMethodCall.IsAsync)
			{
				return Delegates.GetDelegateFor(segment);
			}

			return Delegates.GetDelegateFor<Task>(
				onFailure: segment.End,
				onSuccess: AfterWrapped
			);

			void AfterWrapped(Task task)
			{
				segment.RemoveSegmentFromCallStack();

				transaction.Hold();

				if (task == null)
				{
					return;
				}

				task.ContinueWith(responseTask => agentWrapperApi.HandleExceptions(() =>
				{
					segment.End();
					transaction.Release();
				}));
			}
		}

	}
}
