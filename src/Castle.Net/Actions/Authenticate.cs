﻿using System;
using System.Threading.Tasks;
using Castle.Config;
using Castle.Infrastructure.Exceptions;
using Castle.Messages;
using Castle.Messages.Requests;
using Castle.Messages.Responses;

namespace Castle.Actions
{
    internal static class Authenticate
    {
        public static async Task<Verdict> Execute(
            Func<ActionRequest, Task<Verdict>> send,
            ActionRequest request,
            CastleOptions options)
        {
            try
            {
                var apiRequest = request.PrepareApiCopy(options.Whitelist, options.Blacklist);

                return await send(apiRequest);
            }
            catch (Exception e)
            {
                return CreateFailoverResponse(options.FailOverStrategy, e);
            }
        }

        private static Verdict CreateFailoverResponse(ActionType strategy, Exception exception)
        {
            if (strategy == ActionType.None)
            {
                throw new CastleExternalException("Attempted failover, but no strategy was set.");
            }

            return new Verdict()
            {
                Action = strategy,
                Failover = true,
                FailoverReason = exception is CastleTimeoutException ? "timeout" : "server error"
            };
        }
    }
}