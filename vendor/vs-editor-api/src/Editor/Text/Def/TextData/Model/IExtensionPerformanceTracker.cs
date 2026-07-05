//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    /// Allows editor hosts to track performance of extension points.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// IExtensionPerformanceTracker errorHandler = null;
    /// </remarks>
    /// <experimental>This is an experimental interface, subject to breaking changes.</experimental>
    public interface IExtensionPerformanceTracker
    {
        /// <summary>
        /// Invoked before calling to an extension event handler.
        /// </summary>
        /// <param name="eventHandler"></param>
        void BeforeCallingEventHandler(Delegate eventHandler);
        
        /// <summary>
        /// Invoked after calling to an extension event handler.
        /// </summary>
        void AfterCallingEventHandler(Delegate eventHandler);

        /// <summary>
        /// Invoked before calling to an extensibility point.
        /// </summary>
        void BeforeCallingExtension(object extension);

        /// <summary>
        /// Invoked after calling to an extensibility point.
        /// </summary>
        void AfterCallingExtension(object extension);
    }
}
