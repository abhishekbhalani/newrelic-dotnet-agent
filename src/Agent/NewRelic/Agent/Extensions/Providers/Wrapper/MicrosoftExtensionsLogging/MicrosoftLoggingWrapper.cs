﻿// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using NewRelic.Agent.Api;
using NewRelic.Agent.Api.Experimental;
using NewRelic.Agent.Extensions.Providers.Wrapper;
using Microsoft.Extensions.Logging;
using NewRelic.Core;

namespace NewRelic.Providers.Wrapper.MicrosoftExtensionsLogging
{
    public class MicrosoftLoggingWrapper : IWrapper
    {
        public bool IsTransactionRequired => false;

        private const string WrapperName = "MicrosoftLoggingWrapper";

        public CanWrapResponse CanWrap(InstrumentedMethodInfo methodInfo)
        {
            return new CanWrapResponse(WrapperName.Equals(methodInfo.RequestedWrapperName));
        }

        public AfterWrappedMethodDelegate BeforeWrappedMethod(InstrumentedMethodCall instrumentedMethodCall, IAgent agent, ITransaction transaction)
        {
            var logLevel = (LogLevel)instrumentedMethodCall.MethodCall.MethodArguments[0];

            var formattedLogValues = instrumentedMethodCall.MethodCall.MethodArguments[2];

            var renderedMessage = formattedLogValues.ToString();
            var xapi = agent.GetExperimentalApi();
            xapi.RecordLogMessage(DateTime.UtcNow, logLevel.ToString(), renderedMessage, agent.TraceMetadata.SpanId, agent.TraceMetadata.TraceId);


            if (!agent.Configuration.LogDecoratorEnabled)
            {
                return Delegates.NoOp;
            }

            var logger = (ILogger)instrumentedMethodCall.MethodCall.InvocationTarget;

            var metadata = agent.GetLinkingMetadata();

            if (metadata == null)
            {
                return Delegates.NoOp;
            }

            var handle = logger.BeginScope(metadata);

            return Delegates.GetDelegateFor(onComplete: () =>
            {
                handle.Dispose();
            });
        }
    }
}
