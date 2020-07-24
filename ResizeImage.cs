using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using MetadataExtractor;
using MetadataExtractor.Formats.Jpeg;
using TagLib;
using TagLib.Gif;
using TagLib.IFD;
using TagLib.Jpeg;
using TagLib.Png;
using TagLib.Xmp;
using File = TagLib.File;

namespace TestImages
{
	public class ResizeImage
	{
		private const int kMaxWidth = 2550;		// 8.5 inches at 300dpi (as wide as Letter, wider than A4)
		private const int kMaxHeight = 3500;	// 11.667 inches at 300dpi (taller than Letter, 8 pixels shorter than A4)
		public static void Run(ResizeOptions options)
		{
			var originalTags = TagLib.File.Create(options.FileName);
			switch (options.Mode)
			{
				case ResizeMode.System:
					ResizeWithSystemDrawing(options, originalTags);
					break;
				case ResizeMode.ExternalGM:
					ResizeWithGraphicsMagick(options, originalTags);
					break;
			}
		}

		private static void ResizeWithSystemDrawing(ResizeOptions options, File originalTags)
		{
			var newSize = ComputeNewImageSize(options, originalTags);
			using (var originalImage = Image.FromFile(options.FileName))
			{
				using (var newImage = DrawResizedImage(newSize, originalImage, false))
				{
					newImage.Save(options.Output, originalImage.RawFormat);
				}
			}
			var newTags = TagLib.File.Create(options.Output);
			CopyTags(originalTags, newTags);
			newTags.Save();
		}

		private static void ResizeWithGraphicsMagick(ResizeOptions options, File originalTags)
		{
			var newSize = ComputeNewImageSize(options, originalTags);
			string exePath;
			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
			{
				var pathCodeBase = Assembly.GetExecutingAssembly().CodeBase.Substring(8);	// strip leading "file:///"
				Console.WriteLine("DEBUG: pathCodeBase={0}", pathCodeBase);
				exePath = Path.Combine(Path.GetDirectoryName(pathCodeBase), "gm", "gm.exe");
			}
			else
			{
				exePath = "/usr/bin/gm";
			}
			Console.WriteLine("DEBUG: exePath={0}", exePath);
			string arguments;
			if (originalTags.Properties != null && originalTags.Properties.Description == "PNG File")
			{
				// Ensure opaque white background and no transparency as well as adjusting the size of the image
				arguments =
					$"convert \"{options.FileName}\" -background white -extent 0x0 +matte -scale {newSize.Width}x{newSize.Height} \"{options.Output}\"";
			}
			else
			{
				arguments =
					$"convert \"{options.FileName}\" -scale {newSize.Width}x{newSize.Height} \"{options.Output}\"";
			}
			Console.WriteLine("DEBUG: arguments={0}", arguments);
			var proc = new Process
			{
				StartInfo =
				{
					FileName = exePath,
					Arguments = arguments,
					UseShellExecute = false, // enables CreateNoWindow
					CreateNoWindow = true, // don't need a DOS box
					RedirectStandardOutput = true,
					RedirectStandardError = true,
				}
			};
			proc.Start();
			proc.WaitForExit();
			var standardOutput = proc.StandardOutput.ReadToEnd();
			var standardError = proc.StandardError.ReadToEnd();
			Console.WriteLine("GraphicsMagic exit code = {0}", proc.ExitCode);
			Console.WriteLine("GraphicsMagic stderr");
			Console.WriteLine("--------------------");
			Console.WriteLine(standardError);
			Console.WriteLine("================================");
			Console.WriteLine("GraphicsMagic stdout");
			Console.WriteLine("--------------------");
			Console.WriteLine(standardOutput);
			Console.WriteLine("================================");
		}

		private static Size ComputeNewImageSize(ResizeOptions options, File originalTags)
		{
			if (options.Height != 0 && options.Width != 0)
				return new Size(options.Width, options.Height);

			if (originalTags.Properties == null)
				return new Size(kMaxWidth, kMaxHeight);	// if wrong orientation, file will be smaller than necessary.

			int height, width;
			double dHeight, dWidth;
			if (options.Height != 0)		// height set, but not width
			{
				if (originalTags.Properties.PhotoWidth == originalTags.Properties.PhotoHeight)
				{
					var side = Math.Min(options.Height, kMaxWidth);
					return new Size(side, side);
				}
				if (originalTags.Properties.PhotoWidth < originalTags.Properties.PhotoHeight)
					height = Math.Min(options.Height, kMaxHeight);
				else
					height = Math.Min(options.Height, kMaxWidth);
				dWidth = (double) originalTags.Properties.PhotoWidth * (double) height /
				                (double) originalTags.Properties.PhotoHeight;
				width = (int) Math.Round(dWidth);
				return new Size(width, height);
			}

			if (options.Width != 0)		// width set, but not height
			{
				if (originalTags.Properties.PhotoWidth == originalTags.Properties.PhotoHeight)
				{
					var side = Math.Min(options.Width, kMaxWidth);
					return new Size(side, side);
				}
				if (originalTags.Properties.PhotoWidth < originalTags.Properties.PhotoHeight)
					width = Math.Min(options.Width, kMaxWidth);
				else
					width = Math.Min(options.Width, kMaxHeight);
				dHeight = (double) originalTags.Properties.PhotoHeight * (double) width /
				                 (double) originalTags.Properties.PhotoWidth;
				height = (int) Math.Round(dHeight);
				return new Size(width, height);
			}

			// neither width nor height set on command line
			if (originalTags.Properties.PhotoWidth == originalTags.Properties.PhotoHeight)
				return new Size(kMaxWidth, kMaxWidth);

			if (originalTags.Properties.PhotoWidth < originalTags.Properties.PhotoHeight)
				width = kMaxWidth;
			else
				width = kMaxHeight;
			dHeight = (double) originalTags.Properties.PhotoHeight * (double) width /
			                 (double) originalTags.Properties.PhotoWidth;
			height = (int) Math.Round(dHeight);
			if (height > kMaxHeight)
			{
				height = kMaxHeight;
				dWidth = (double) originalTags.Properties.PhotoWidth * (double) height /
				                (double) originalTags.Properties.PhotoHeight;
				width = (int) Math.Round(dWidth);
			}
			return new Size(width, height);
		}

		private static void CopyTags(File originalTags, File newTags)
		{
			if ((originalTags.TagTypes & TagTypes.Png) == TagTypes.Png)
			{
				if (originalTags.GetTag(TagTypes.Png) is PngTag tag &&
				    newTags.GetTag(TagTypes.Png, true) is PngTag newTag)
				{
					foreach (KeyValuePair<string, string> kvp in tag)
						newTag.SetKeyword(kvp.Key, kvp.Value);
				}
			}
			if ((originalTags.TagTypes & TagTypes.XMP) == TagTypes.XMP)
			{
				if (originalTags.GetTag(TagTypes.XMP) is XmpTag tag &&
				    newTags.GetTag(TagTypes.XMP, true) is XmpTag newTag)
				{
					Console.WriteLine("DEBUG: new XMP Tag values");
					foreach (var node in newTag.NodeTree.Children)
						MetaExtract.WriteTaglibXmpNodeTree(node, "");
					// Don't bother copying camera/scanner related information.
					// We just want the creator/copyright/description type information.
					foreach (var node in tag.NodeTree.Children)
					{
						if (node.Namespace == "http://purl.org/dc/elements/1.1/" ||
						    node.Namespace == "http://creativecommons.org/ns#" ||
							node.Namespace == "http://www.metadataworkinggroup.com/schemas/collections/" ||
						    (node.Namespace == "http://ns.adobe.com/exif/1.0/" && node.Name == "UserComment"))
						{
							newTag.NodeTree.AddChild(node);
						}
					}
				}
			}
		}


		/// <summary>
		/// Return a possibly resized and possibly centered image.  If no change is needed,
		/// a new copy of the image is returned.
		/// </summary>
		/// <remarks>
		/// Always returning a new image simplifies keeping track of when to dispose the original
		/// image.
		/// Note that this method never returns a larger image than the original: only one the
		/// same size or smaller.
		/// </remarks>
		private static Image DrawResizedImage(Size maxSize, Image image, bool centerImage)
		{
			// adapted from https://www.c-sharpcorner.com/article/resize-image-in-c-sharp/
			var desiredHeight = maxSize.Height;
			var desiredWidth = maxSize.Width;
			if (image.Width == desiredWidth && image.Height == desiredHeight)
				return new Bitmap(image);	// exact match already
			int newHeight;
			int newWidth;
			if (image.Height <= desiredHeight && image.Width <= desiredWidth)
			{
				if (!centerImage)
					return new Bitmap(image);
				newHeight = image.Height;	// not really new...
				newWidth = image.Width;
			}
			else
			{
				// Try resizing to desired width first
				newHeight = image.Height * desiredWidth / image.Width;
				newWidth = desiredWidth;
				if (newHeight > desiredHeight)
				{
					// Resize to desired height instead
					newWidth = image.Width * desiredHeight / image.Height;
					newHeight = desiredHeight;
				}
			}
			Image newImage;
			if (centerImage)
				newImage = new Bitmap(desiredWidth, desiredHeight);
			else
				newImage = new Bitmap(newWidth, newHeight);
			using (var graphic = Graphics.FromImage(newImage))
			{
				// I tried using HighSpeed settings in here with no appreciable difference in loading speed.
				// However, the "High Quality" settings can greatly increase memory use, possibly causing "Out of Memory"
				// errors when creating thumbnail images.  So we use the default settings for drawing the image here.
				// Some thumbnails may be a bit uglier, but they're supposed to just give an idea of what the front cover
				// looks like: they're not works of art themselves.
				// See https://stackoverflow.com/questions/15438509/graphics-drawimage-throws-out-of-memory-exception?lq=1
				// (the second answer).
				graphic.DrawImage(image, (newImage.Width - newWidth)/2, (newImage.Height - newHeight)/2, newWidth, newHeight);
			}
			return newImage;
		}

	}
}
