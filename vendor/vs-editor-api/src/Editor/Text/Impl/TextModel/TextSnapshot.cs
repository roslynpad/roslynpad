//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;
    using System.Collections.Generic;

    internal partial class TextSnapshot : BaseSnapshot, ITextSnapshot, ITextSnapshot2
    {
        #region Private members
        private readonly ITextBuffer textBuffer;
        #endregion

        #region Constructors
        public TextSnapshot(ITextBuffer textBuffer, ITextVersion2 version, StringRebuilder content)
          : base(version, content)
        {
            System.Diagnostics.Debug.Assert(version.Length == content.Length);
            this.textBuffer = textBuffer;
        }
        #endregion

        protected override ITextBuffer TextBufferHelper
        {
            get { return this.textBuffer; }
        }
    }
}
