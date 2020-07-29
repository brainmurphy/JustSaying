using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.Messaging.Channels.Context;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Middleware
{
    /// <summary>
    /// The default middleware to use for the receive pipeline.
    /// </summary>
    public class DefaultSqsMiddleware : MiddlewareBase<GetMessagesContext, IList<Message>>
    {
        private readonly ILogger<DefaultSqsMiddleware> _logger;

        /// <summary>
        /// Creates an instance of <see cref="DefaultSqsMiddleware"/>.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        public DefaultSqsMiddleware(ILogger<DefaultSqsMiddleware> logger)
        {
            _logger = logger;
        }

        protected override async Task<IList<Message>> RunInnerAsync(
            GetMessagesContext context,
            Func<CancellationToken, Task<IList<Message>>> func,
            CancellationToken stoppingToken)
        {
            try
            {
                var results = await func(stoppingToken).ConfigureAwait(false);

                _logger.LogTrace(
                    "Polled for messages on queue '{QueueName}' in region '{Region}', and received {MessageCount} messages.",
                    context.QueueName,
                    context.RegionName,
                    results.Count);

                return results;
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogTrace(
                    ex,
                    "Request to get more messages from queue was canceled for queue '{QueueName}' in region '{Region}'," +
                    "likely because there are no messages in the queue. " +
                    "This might have also been caused by the application shutting down.",
                    context.QueueName,
                    context.RegionName);
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _logger.LogError(
                    ex,
                    "Error receiving messages on queue '{QueueName}' in region '{Region}'.",
                    context.QueueName,
                    context.RegionName);
            }

            return Array.Empty<Message>();
        }
    }
}
