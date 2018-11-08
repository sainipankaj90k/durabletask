﻿//  ----------------------------------------------------------------------------------
//  Copyright Microsoft Corporation
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//  ----------------------------------------------------------------------------------

namespace DurableTask.AzureStorage
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    static class Utils
    {
        public static readonly Task CompletedTask = Task.FromResult(0);

        public static readonly string ExtensionVersion = FileVersionInfo.GetVersionInfo(typeof(AzureStorageOrchestrationService).Assembly.Location).FileVersion;

        public static async Task ParallelForEachAsync<TSource>(
            this IEnumerable<TSource> enumerable,
            Func<TSource, Task> action)
        {
            var tasks = new List<Task>(32);
            foreach (TSource entry in enumerable)
            {
                tasks.Add(action(entry));
            }

            await Task.WhenAll(tasks.ToArray());
        }

        public static async Task ParallelForEachAsync<T>(this IReadOnlyList<T> items, int maxConcurrency, Func<T, Task> action)
        {
            using (var semaphore = new SemaphoreSlim(maxConcurrency))
            {
                var tasks = new Task[items.Count];
                for (int i = 0; i < items.Count; i++)
                {
                    tasks[i] = InvokeThrottledAction(items[i], action, semaphore);
                }
                await Task.WhenAll(tasks);
            }
        }

        static async Task InvokeThrottledAction<T>(T item, Func<T, Task> action, SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            try
            {
                await action(item);
            }
            finally
            {
                semaphore.Release();
            }
        }
    }

    /// <summary>
    /// Class to hold statistics about this execution of purge history
    /// </summary>
    public class PurgeHistoryResult
    {
        /// <summary>
        /// Constructor for purge history statistics
        /// </summary>
        /// <param name="storageRequests">Requests sent to storage</param>
        /// <param name="instancesDeleted">Number of instances deleted</param>
        /// <param name="rowsDeleted">Number of rows deleted</param>
        public PurgeHistoryResult(int storageRequests, int instancesDeleted, int rowsDeleted)
        {
            this.StorageRequests = storageRequests;
            this.InstancesDeleted = instancesDeleted;
            this.RowsDeleted = rowsDeleted;
        }

        /// <summary>
        /// Number of requests sent to Storage during this execution of purge history
        /// </summary>
        public int StorageRequests { get; }

        /// <summary>
        /// Number of instances deleted during this execution of purge history
        /// </summary>
        public int InstancesDeleted { get; }

        /// <summary>
        /// Number of rows deleted during this execution of purge history
        /// </summary>
        public int RowsDeleted { get; }
    }
}