using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using PDFiumSharp.Types;
using PDFiumSharp.Enums;

namespace PDFiumSharp
{
    public sealed class PdfText : NativeWrapper<FPDF_TEXTPAGE>
    {
        public enum BoundedTextDetailLevel
        {
            AggregateAll = 1,
            AggregateByLine = 2,
            IndividualSegments = 3
        }

        // Private constructor. See static Load method below.
        PdfText(PdfPage page, FPDF_TEXTPAGE text)
            : base(text)
        {
            if (text.IsNull)
                throw new PDFiumException();
            Page = page;
        }

        internal static PdfText Load(PdfPage page) => new PdfText(page, PDFium.FPDFText_LoadPage(page.Handle));

        public PdfPage Page { get; }

        protected override void Dispose(FPDF_TEXTPAGE handle)
        {
            PDFium.FPDFText_ClosePage(handle);
        }

        public int CountChars()
        {            
            return PDFium.FPDFText_CountChars(this.Handle);
        }

        /// <summary>
        /// Returns all the text on the page.
        /// </summary>
        /// <returns></returns>
        public string GetText()
        {
            return PDFium.FPDFText_GetText(this.Handle, 0, this.CountChars());
        }

        public string GetText(int start_index, int count)
        {
            return PDFium.FPDFText_GetText(this.Handle, start_index, count);
        }

        public string GetBoundedText(double left, double top, double right, double bottom)
        {
            return PDFium.FPDFText_GetBoundedText(this.Handle, left, top, right, bottom);
        }

        public List<PdfTextItem> GetBoundedTextInfo(double left, double top, double right, double bottom, BoundedTextDetailLevel detailLevel)
        {
            FS_RECTF boundedArea = new FS_RECTF((float)left, (float)top, (float)right, (float)bottom);
            List<PdfTextItem> textInfoList = new List<PdfTextItem>();            
            List<FS_RECTF> allRects = this.GetAllRects();
            // ensure char info is loaded
            this.GetAllCharInfo();
            // iterate master list of rectangles 
            foreach (FS_RECTF rect in allRects)
            {
                if (rect.IntersectsWith(boundedArea))
                {
                    // Find all the characters that fit in this rect AND the boundedArea
                    List<PdfTextItem> chars = _allCharInfo.FindAll(c => rect.Contains(c.BoundingRectangle) && boundedArea.ContainsPartially(c.BoundingRectangle, 50));
                    List<FS_RECTF> charBoxes = new List<FS_RECTF>();
                    chars.ForEach(c => charBoxes.Add(c.BoundingRectangle));
                    FS_RECTF boundingRect = FS_RECTF.Union(charBoxes);
                    //System.Diagnostics.Debug.Assert(boundingRect.Left >= boundedArea.Left);
                    // Combine chars into new PdfTextInfo element
                    PdfTextItem textSegment = new PdfTextItem(
                        string.Join<PdfTextItem>(string.Empty, chars.ToArray()),
                        -1, chars.Count, boundingRect, charBoxes);
                    textInfoList.Add(textSegment);
                }
            }
            // Sort segments by location.  Top to bottom, left to right.
            textInfoList.Sort((x, y) =>
            {
                int retval = -x.BoundingRectangle.Bottom.CompareTo(y.BoundingRectangle.Bottom);
                return retval == 0 ? x.BoundingRectangle.Left.CompareTo(y.BoundingRectangle.Left) : retval;                
            });
            if (detailLevel == BoundedTextDetailLevel.AggregateByLine || detailLevel == BoundedTextDetailLevel.AggregateAll)
            {
                // combine/group items by line
                PdfTextItem currentLine = textInfoList[0];
                List<PdfTextItem> lines = new List<PdfTextItem>() { currentLine };
                foreach (PdfTextItem textInfo in textInfoList)
                {
                    // skip first one
                    if (textInfo != currentLine)
                    {
                        // Create a new line if text fragment is completely below the bounding box of the current line
                        // or the amount of overlap between the two is less than 50%
                        if (textInfo.BoundingRectangle.Top < currentLine.BoundingRectangle.Bottom || (textInfo.BoundingRectangle.Top - currentLine.BoundingRectangle.Bottom) < (textInfo.BoundingRectangle.Top - textInfo.BoundingRectangle.Bottom) * 0.5)
                        {
                            // start a new line
                            currentLine = textInfo;
                            lines.Add(currentLine);
                        }
                        else
                        {
                            // append/merge segment into current line
                            currentLine.Text += " " + textInfo.Text;
                            currentLine.Length += textInfo.Length + 1;
                            currentLine.BoundingRectangle = currentLine.BoundingRectangle.Union(textInfo.BoundingRectangle);
                            FS_RECTF emptyRect = new FS_RECTF(currentLine.BoundingRectangle.Right, currentLine.BoundingRectangle.Top, textInfo.BoundingRectangle.Right, currentLine.BoundingRectangle.Bottom);
                            // add placeholder for the space
                            currentLine.charBoxList.Add(emptyRect);
                            currentLine.charBoxList.AddRange(textInfo.charBoxList);
                        }
                    }
                }
                if (detailLevel == BoundedTextDetailLevel.AggregateByLine)
                {
                    return lines;
                }
                else
                {
                    // combine lines into single item                    
                    StringBuilder combinedText = new StringBuilder();
                    List<FS_RECTF> lineRects = new List<FS_RECTF>();
                    List<FS_RECTF> charBoxes = new List<FS_RECTF>();
                    for (int i = 0; i < lines.Count; i++)
                    {                        
                        PdfTextItem line = lines[i];
                        combinedText.Append(line.Text);
                        lineRects.Add(line.BoundingRectangle);
                        charBoxes.AddRange(line.charBoxList);
                        // if not last item, append CRLF
                        if (i != lines.Count - 1)
                        {
                            const string CRLF = "\r\n";
                            combinedText.Append(CRLF);
                            // add fake char boxes for CRLF
                            FS_RECTF emptyRect = new FS_RECTF(line.BoundingRectangle.Right, line.BoundingRectangle.Bottom, line.BoundingRectangle.Right, line.BoundingRectangle.Bottom);
                            charBoxes.Add(emptyRect);
                            charBoxes.Add(emptyRect);
                        }                        
                    }
                    PdfTextItem aggregateTextInfo = new PdfTextItem(
                        combinedText.ToString(), -1, combinedText.Length,
                        FS_RECTF.Union(lineRects), charBoxes);
                    return new List<PdfTextItem>() { aggregateTextInfo };
                }
            }
            else if (detailLevel != BoundedTextDetailLevel.IndividualSegments)
            {
                throw new System.ArgumentException();
            }
            return textInfoList;
        }
        
        public char GetCharacter(int index)
        {
            return PDFium.FPDFText_GetUnicode(this.Handle, index);
        }

        public int GetCharIndexAtPos(double x, double y, double xTolerance, double yTolerance)
        {
            return PDFium.FPDFText_GetCharIndexAtPos(this.Handle, x, y, xTolerance, yTolerance);
        }

        public FS_RECTF GetCharBox(int index)
        {
            double left, top, right, bottom;
            PDFium.FPDFText_GetCharBox(this.Handle, index, out left, out right, out bottom, out top);
            return new FS_RECTF((float)left, (float)top, (float)right, (float)bottom); 
        }

        private List<FS_RECTF> GetCharBoxes(int char_index, int char_count)
        {
            List<FS_RECTF> charBoxes = new List<FS_RECTF>();
            for (int i = char_index; i < char_index + char_count; i++)
            {
                charBoxes.Add(this.GetCharBox(i));
            }
            return charBoxes;
        }

        public ReadOnlyCollection<PdfTextItem> GetAllCharInfo(bool refresh = false)
        {
            if (_allCharInfo == null || refresh)
            {
                string allText = this.GetText(0, this.CountChars());
                _allCharInfo = new List<PdfTextItem>();
                int charCount = this.CountChars();
                for (int i = 0; i < charCount; i++)
                {
                    FS_RECTF charBox = this.GetCharBox(i);
                    _allCharInfo.Add(new PdfTextItem( allText.Substring(i, 1), i, 1, charBox, new List<FS_RECTF>() { charBox } ));
                }
            }
            return new ReadOnlyCollection<PdfTextItem>(_allCharInfo);
        }
        private List<PdfTextItem> _allCharInfo;

        public FS_RECTF GetRect(int rect_index)
        {
            double left, top, right, bottom;
            PDFium.FPDFText_GetRect(this.Handle, rect_index, out left, out top, out right, out bottom);
            return new FS_RECTF((float)left, (float)top, (float)right, (float)bottom);
        }

        public FS_RECTF GetRect(int char_index, int char_count)
        {
            List<FS_RECTF> charBoxes;
            return GetRect(char_index, char_count, out charBoxes);
        }

        public FS_RECTF GetRect(int char_index, int char_count, out List<FS_RECTF> charBoxes)
        {
            charBoxes = GetCharBoxes(char_index, char_count);
            return FS_RECTF.Union(charBoxes);
        }
                
        public List<FS_RECTF> GetAllRects()
        {
            List<FS_RECTF> rects = new List<FS_RECTF>();
            int rectCount = PDFium.FPDFText_CountRects(this.Handle, 0, this.CountChars());
            for (int i = 0; i < rectCount; i++)
            {
                rects.Add(this.GetRect(i));
            }
            return rects;
        }

        public float GetFontSize(int index)
        {
            return (float)PDFium.FPDFText_GetFontSize(this.Handle, index);
        }

        public int FindText(string searchText, SearchFlags searchFlags, int start_index = 0)
        {
            FPDF_SCHHANDLE hSearch = PDFium.FPDFText_FindStart(this.Handle, searchText, searchFlags, start_index);
            try
            {
                int countCharsFound = PDFium.FPDFText_GetSchCount(hSearch);
                int charIndex = PDFium.FPDFText_GetSchResultIndex(hSearch);
                return charIndex;
            }
            finally
            {
                PDFium.FPDFText_FindClose(hSearch);
            }
        }

        public List<PdfTextItem> FindTextAll(string searchText, SearchFlags searchFlags, int start_index = 0)
        {
            List<PdfTextItem> results = new List<PdfTextItem>();
            FPDF_SCHHANDLE hSearch = PDFium.FPDFText_FindStart(this.Handle, searchText, searchFlags, start_index);
            try
            {
                
                while (PDFium.FPDFText_FindNext(hSearch))
                {
                    int countCharsFound = PDFium.FPDFText_GetSchCount(hSearch);
                    int charIndex = PDFium.FPDFText_GetSchResultIndex(hSearch);                    
                    List<FS_RECTF> charBoxes;
                    FS_RECTF boundingRect = GetRect(charIndex, countCharsFound, out charBoxes);
                    results.Add(new PdfTextItem(                    
                        this.GetText(charIndex, countCharsFound),
                        charIndex,
                        countCharsFound,
                        boundingRect,
                        charBoxes));
                }
            }
            finally
            {
                PDFium.FPDFText_FindClose(hSearch);
            }
            return results;
        }
    }

    public class PdfTextItem
    {
        internal PdfTextItem(string text, int startIndex, int length, FS_RECTF boundingRect, List<FS_RECTF> charBoxes)
        {
            Text = text;
            StartIndex = startIndex;
            Length = length;
            BoundingRectangle = boundingRect;
            charBoxList = charBoxes;
        }
        internal List<FS_RECTF> charBoxList;

        public string Text { get; internal set; }
        public int StartIndex { get; internal set; }
        public int Length { get; internal set; }
        public FS_RECTF BoundingRectangle { get; internal set; }
        public ReadOnlyCollection<FS_RECTF> CharBoxes { get { return new ReadOnlyCollection<FS_RECTF>(charBoxList); } }
        
        public override string ToString()
        {
            return this.Text;
        }
    }
}
