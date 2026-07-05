//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Utilities
{
    using System;
    using Microsoft.VisualStudio.Utilities;

    internal class VacuousTextDataModel : ITextDataModel
    {
        private ITextBuffer documentBuffer;

        public VacuousTextDataModel(ITextBuffer documentBuffer)
        {
            this.documentBuffer = documentBuffer ?? throw new ArgumentNullException(nameof(documentBuffer));
            documentBuffer.ContentTypeChanged += OnDocumentBufferContentTypeChanged;
        }

        public event EventHandler<TextDataModelContentTypeChangedEventArgs> ContentTypeChanged;

        public IContentType ContentType
        {
            get { return documentBuffer.ContentType; }
        }

        public ITextBuffer DocumentBuffer
        {
            get { return this.documentBuffer; }
        }

        public ITextBuffer DataBuffer
        {
            get { return this.documentBuffer; }
        }

        private void OnDocumentBufferContentTypeChanged(object sender, ContentTypeChangedEventArgs e)
        {
            EventHandler<TextDataModelContentTypeChangedEventArgs> handler = ContentTypeChanged;
            if (handler != null)
            {
                handler(this, new TextDataModelContentTypeChangedEventArgs(e.BeforeContentType, e.AfterContentType));
            }
        }
    }
}