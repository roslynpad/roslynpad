//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// A helper for managing JoinableTasks.
    /// </summary>
    public class JoinableTaskHelper
    {
#pragma warning disable CA1051 // Do not declare visible instance fields
        [SuppressMessage("Microsoft.Security", "CA2104", Justification = "By design")]
        public readonly JoinableTaskContext Context;

        [SuppressMessage("Microsoft.Security", "CA2104", Justification = "By design")]
        public readonly JoinableTaskCollection Collection;

        [SuppressMessage("Microsoft.Security", "CA2104", Justification = "By design")]
        public readonly JoinableTaskFactory Factory;
#pragma warning restore CA1051 // Do not declare visible instance fields

        public JoinableTaskHelper(JoinableTaskContext context)
        {
            this.Context = context ?? throw new ArgumentNullException(nameof(context));
            this.Collection = context.CreateCollection();
            this.Factory = context.CreateFactory(this.Collection);
        }

        public JoinableTask RunOnUIThread(Action action, bool forceTaskSwitch = true)
        {
            using (this.Context.SuppressRelevance())
            {
                return this.Factory.RunAsync(async delegate
                                    {
                                        if (forceTaskSwitch && this.Context.IsOnMainThread)
                                        {
                                            await Task.Yield();
                                        }

                                        await this.Factory.SwitchToMainThreadAsync();
                                        action();
                                    });
            }
        }

        public JoinableTask<T> RunOnUIThread<T>(Func<T> function, bool forceTaskSwitch = true)
        {
            using (this.Context.SuppressRelevance())
            {
                return this.Factory.RunAsync(async delegate
                                    {
                                        if (forceTaskSwitch && this.Context.IsOnMainThread)
                                        {
                                            await Task.Yield();
                                        }

                                        await this.Factory.SwitchToMainThreadAsync();
                                        return function();
                                    });
            }
        }

        public Task DisposeAsync()
        {
            return this.Collection.JoinTillEmptyAsync();
        }

        public void Dispose()
        {
            this.Context.Factory.Run(async delegate
            {
                // Not this.Factory
                await this.DisposeAsync().ConfigureAwait(false);
            });
        }
    }
}

