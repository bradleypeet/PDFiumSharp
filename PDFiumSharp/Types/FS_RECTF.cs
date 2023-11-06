#region Copyright and License
/*
This file is part of PDFiumSharp, a wrapper around the PDFium library for the .NET framework.
Copyright (C) 2017 Tobias Meyer
License: Microsoft Reciprocal License (MS-RL)
*/
#endregion
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PDFiumSharp.Types
{
	/// <summary>
	/// Rectangle area(float) in device or page coordinate system.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct FS_RECTF
    {
		/// <summary>
		/// The x-coordinate of the left-top corner.
		/// </summary>
		public float Left { get; }

		/// <summary>
		/// The y-coordinate of the left-top corner.
		/// </summary>
		public float Top { get; }

		/// <summary>
		/// The x-coordinate of the right-bottom corner.
		/// </summary>
		public float Right { get; }

		/// <summary>
		/// The y-coordinate of the right-bottom corner.
		/// </summary>
		public float Bottom { get; }

		public FS_RECTF(float left, float top, float right, float bottom)
		{
			Left = left;
			Top = top;
			Right = right;
			Bottom = bottom;
		}

        public FS_RECTF Union(FS_RECTF rectangle)
        {
            return new FS_RECTF(
                Math.Min(this.Left, rectangle.Left),
                Math.Max(this.Top, rectangle.Top),
                Math.Max(this.Right, rectangle.Right),
                Math.Min(this.Bottom, rectangle.Bottom));
        }

        public FS_RECTF UnionAll(List<FS_RECTF> rectangles)
        {
            List<FS_RECTF> rectList = new List<FS_RECTF>(rectangles);
            rectList.Add(this);
            return Union(rectList);
        }

        public static FS_RECTF Union(List<FS_RECTF> rectangles)
        {
            if (rectangles == null || rectangles.Count == 0)
            {
                return new FS_RECTF();
            }
            else
            {
                float left = rectangles[0].Left, right = rectangles[0].Right, top = rectangles[0].Top, bottom = rectangles[0].Top;
                for (int i = 1; i < rectangles.Count; i++)
                {
                    left = Math.Min(left, rectangles[i].Left);
                    top = Math.Max(top, rectangles[i].Top);
                    right = Math.Max(right, rectangles[i].Right);
                    bottom = Math.Min(bottom, rectangles[i].Bottom);
                }
                return new FS_RECTF(left, top, right, bottom);
            }
        }

        public float Height
        {
            get { return Top - Bottom; }
        }

        public float Width
        {
            get { return Right - Left; }
        }

        public bool IntersectsWith(FS_RECTF rectangle)
        {
            bool retval = (this.Left < rectangle.Right && this.Right > rectangle.Left &&
                        this.Top > rectangle.Bottom && this.Bottom < rectangle.Top);
            return retval;
        }

        public bool Contains(FS_RECTF rectangle)
        {
            return this.Left <= rectangle.Left && this.Right >= rectangle.Right
                && this.Top >= rectangle.Top && this.Bottom <= rectangle.Bottom;
        }

        public bool ContainsPartially(FS_RECTF rectangle, float minPercentContained)
        {
            if (this.IntersectsWith(rectangle))
            {
                // calculate area of intersection
                float left = Math.Max(this.Left, rectangle.Left),
                    right = Math.Min(this.Right, rectangle.Right),
                    bottom = Math.Max(this.Bottom, rectangle.Bottom),
                    top = Math.Min(this.Top, rectangle.Top);
                float areaContained = (right - left) * (top - bottom);
                // compare to area of partially contained rectangle
                bool retval = areaContained >= rectangle.Height * rectangle.Width * (minPercentContained / 100);
                //System.Diagnostics.Debug.Assert(retval == true);
                return retval;
            }
            else
            {
                return false;
            }
        }
    }
}
