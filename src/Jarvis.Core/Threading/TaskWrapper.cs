﻿// Licensed to Spectre Systems AB under one or more agreements.
// Spectre Systems AB licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Jarvis.Core.Diagnostics;

namespace Jarvis.Core.Threading
{
    public sealed class TaskWrapper
    {
        private readonly IJarvisLog _log;
        private readonly IBackgroundWorker _worker;
        private CancellationTokenSource _source;

        public string Name => _worker.Name;
        public Task Task { get; private set; }

        public TaskWrapper(IBackgroundWorker worker, IJarvisLog log)
        {
            _worker = worker;
            _log = log;
        }

        public Task Start(CancellationTokenSource source)
        {
            _log.Information($"Starting {Name.ToLowerInvariant()}...");
            _source = source;

            Task = Task.Factory.StartNew(
                async () =>
            {
                try
                {
                    _log.Information($"{Name} started.");
                    if (!await _worker.Run(_source.Token))
                    {
                        _source.Cancel();
                    }
                }
                catch (OperationCanceledException)
                {
                    _log.Information($"{Name} aborted.");
                }
                catch (AggregateException ex)
                {
                    var exceptions = ex.Flatten().InnerExceptions;
                    foreach (var exception in exceptions)
                    {
                        _log.Error($"{Name}: {ex.Message} ({ex.GetType().FullName})");
                    }
                    _source.Cancel();
                }
                catch (Exception ex)
                {
                    _log.Error($"{Name}: {ex.Message} ({ex.GetType().FullName})");
                    _source.Cancel();
                }
                finally
                {
                    _log.Information($"{Name} stopped.");
                }
            }, TaskCreationOptions.LongRunning);

            return Task;
        }
    }
}
