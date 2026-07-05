//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Implementation
{
    using Microsoft.VisualStudio.Text.Utilities;

    internal static class TextModelUtilities
    {
        static public int ComputeLineCountDelta(LineBreakBoundaryConditions boundaryConditions, StringRebuilder oldText, StringRebuilder newText)
        {
            int delta = 0;
            delta -= oldText.LineBreakCount;
            delta += newText.LineBreakCount;
            if ((boundaryConditions & LineBreakBoundaryConditions.PrecedingReturn) != 0)
            {
                if (oldText.FirstCharacter == '\n')
                {
                    delta++;
                }
                if (newText.FirstCharacter == '\n')
                {
                    delta--;
                }
            }

            if ((boundaryConditions & LineBreakBoundaryConditions.SucceedingNewline) != 0)
            {
                if (oldText.LastCharacter == '\r')
                {
                    delta++;
                }
                if (newText.LastCharacter == '\r')
                {
                    delta--;
                }
            }

            if ((oldText.Length == 0) &&
                ((boundaryConditions & LineBreakBoundaryConditions.PrecedingReturn) != 0) &&
                ((boundaryConditions & LineBreakBoundaryConditions.SucceedingNewline) != 0))
            {
                // return and newline were adjacent before and were separated by the insertion
                delta++;
            }

            if ((newText.Length == 0) &&
                ((boundaryConditions & LineBreakBoundaryConditions.PrecedingReturn) != 0) &&
                ((boundaryConditions & LineBreakBoundaryConditions.SucceedingNewline) != 0))
            {
                // return and newline were separated before and were made adjacent by the deletion
                delta--;
            }
            return delta;
        }
    }
}
