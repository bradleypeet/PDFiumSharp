﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDFiumSharp
{
	public static class RenderingExtensions
	{
		public static void Render(this PdfPage page, System.Drawing.Bitmap bitmap, int x, int y, int width, int height, RotateOptions rotate = RotateOptions.DontRotate, RenderingFlags flags = RenderingFlags.None)
		{
			if (bitmap == null)
				throw new ArgumentNullException(nameof(bitmap));

			var format = GetBitmapFormat(bitmap);
			var data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);
			using (var tmp = new Bitmap(bitmap.Width, bitmap.Height, format, data.Scan0, data.Stride))
				page.Render(tmp, x, y, width, height, rotate, flags);
			bitmap.UnlockBits(data);
		}

		public static void Render(this PdfPage page, System.Drawing.Bitmap bitmap, RotateOptions rotate = RotateOptions.DontRotate, RenderingFlags flags = RenderingFlags.None)
		{
			page.Render(bitmap, 0, 0, bitmap.Width, bitmap.Height, rotate, flags);
		}

		public static void Render(this PdfPage page, out System.Drawing.Bitmap bitmap, RenderingFlags flags = RenderingFlags.None)
		{
			bitmap = new System.Drawing.Bitmap((int)Math.Round(page.Width), (int)Math.Round(page.Height));
			page.Render(bitmap, RotateOptions.DontRotate, flags);
		}

		static BitmapFormats GetBitmapFormat(System.Drawing.Bitmap bitmap)
		{
			if (bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format24bppRgb)
				return BitmapFormats.FPDFBitmap_BGR;
			if (bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
				return BitmapFormats.FPDFBitmap_BGRA;
			if (bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppRgb)
				return BitmapFormats.FPDFBitmap_BGRx;
			throw new NotSupportedException($"Pixel format {bitmap.PixelFormat} is not supported.");
		}
	}
}