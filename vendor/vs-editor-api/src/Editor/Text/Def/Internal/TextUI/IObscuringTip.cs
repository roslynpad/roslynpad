//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    public interface IObscuringTip
    {
        /// <summary>
        /// Dismiss the tip. Return true if the tip had been visible.
        /// </summary>
        bool Dismiss();

        /// <summary>
        /// Get the current opacity of the tip (should be 100% unless explicitly set otherwise).
        /// </summary>
        double Opacity { get; }

        /// <summary>
        /// Set the opacity of the tip (generally to either 100% or 10% while the control key is held down.
        /// </summary>
        void SetOpacity(double opacity);
    }

    public abstract class Tip : IObscuringTip
    {
        public abstract bool Dismiss();

        public virtual double Opacity => 1.0;

        public virtual void SetOpacity(double opacity) { }
    }
}
