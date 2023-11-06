#region Copyright and License
/*
This file is part of PDFiumSharp, a wrapper around the PDFium library for the .NET framework.
Copyright (C) 2017 Tobias Meyer
License: Microsoft Reciprocal License (MS-RL)
*/
#endregion
using System;
using PDFiumSharp.Types;

namespace PDFiumSharp
{
	public sealed class PDFiumException : Exception
	{
        public PDFiumException()
        {
            PdfiumErrorNumber = PDFium.FPDF_GetLastError();
        }

        public PDFiumException(FPDF_ERR pdfiumErrorNumber)
        {
            PdfiumErrorNumber = pdfiumErrorNumber;
        }

        public FPDF_ERR PdfiumErrorNumber { get; private set; }

        public override string Message => $"PDFium Error: {PdfiumErrorNumber.GetDescription()}";
    }
}
