using System;
using System.Collections.Generic;
using MetadataExtractor;
using MetadataExtractor.Formats.FileSystem;
using MetadataExtractor.Formats.Xmp;
using SIL.Extensions;
using TagLib;
using TagLib.Gif;
using TagLib.Jpeg;
using TagLib.Png;
using TagLib.IFD;
using TagLib.Xmp;

namespace TestImages
{
	internal class MetaExtract
	{
		internal static void Run(MetaOptions options)
		{
			try
			{
				Console.WriteLine("Extracting metadata from {0} using {1}", options.FileName, options.Mode);
				MemoryManagement.CheckMemory($"Before extracting metadata using {options.Mode}", true);
				if (options.Folder)
				{
					foreach (var file in System.IO.Directory.EnumerateFiles(options.FileName))
					{
						if (Program.IsImageFile(file))
							LoadMetadataAndReport(options.Mode, file);
					}
				}
				else
				{
					LoadMetadataAndReport(options.Mode, options.FileName);
				}
				MemoryManagement.CheckMemory("After metadata extracted and possibly discarded", true);
			}
			catch (Exception ex)
			{
				Console.WriteLine("EXCEPTION CAUGHT: {0}", ex);
				Console.WriteLine("{0}", ex.StackTrace);
			}
		}

		private static void LoadMetadataAndReport(MetaMode mode, string filename)
		{
			Console.WriteLine("Image metadata from {0}", filename);
			switch (mode)
			{
				case MetaMode.MetadataExtractor:
					var metaDirectories = ImageMetadataReader.ReadMetadata(filename);
					foreach (var metaDir in metaDirectories)
					{
						foreach (var tag in metaDir.Tags)
							Console.WriteLine($"{metaDir.Name}: \"{tag.Name}\" = \"{tag.Description}\"");
						if (metaDir is XmpDirectory)
						{
							var properties = (metaDir as XmpDirectory).GetXmpProperties();
							foreach (var prop in properties)
								Console.WriteLine($"    XMP \"{prop.Key}\" = \"{prop.Value}\"");
						}
					}
					MemoryManagement.CheckMemory($"With metadata extracted using {mode}", true);
					break;
				case MetaMode.Palaso:
					var meta = SIL.Windows.Forms.ClearShare.Metadata.FromFile(filename);
					Console.WriteLine("meta.AttributionUrl = {0}", meta.AttributionUrl);
					Console.WriteLine("meta.CollectionName = {0}", meta.CollectionName);
					Console.WriteLine("meta.CollectionUri = {0}", meta.CollectionUri);
					Console.WriteLine("meta.CopyrightNotice = {0}", meta.CopyrightNotice);
					Console.WriteLine("meta.Creator = {0}", meta.Creator);
					Console.WriteLine("meta.License.Token = {0}", meta.License.Token);
					Console.WriteLine("meta.License.RightsStatement = {0}", meta.License.RightsStatement);
					Console.WriteLine("meta.License.Url = {0}", meta.License.Url);
					Console.WriteLine("meta.License.Description = {0}", meta.License.GetDescription(new[] {"en"}, out string langId));
					Console.WriteLine("meta.ShortCopyrightNotice = {0}", meta.ShortCopyrightNotice);
					MemoryManagement.CheckMemory($"With metadata extracted using {mode}", true);
					break;
				case MetaMode.Taglib:
					var tagFile = TagLib.File.Create(filename);
					WriteTaglibValues(tagFile);
					MemoryManagement.CheckMemory($"With metadata extracted using {mode}", true);
					break;
			}
		}

		private static void WriteTaglibValues(TagLib.File tagFile)
		{
			Console.WriteLine("tagFile = {0}", tagFile);
			var imageTagFile = tagFile as TagLib.Image.File;
			if (imageTagFile == null)
				return;
			Console.WriteLine("Image Properties.PhotoWidth={0}", imageTagFile.Properties.PhotoWidth);
			Console.WriteLine("Image Properties.PhotoHeight={0}", imageTagFile.Properties.PhotoHeight);
			Console.WriteLine("Image Properties.Description=\"{0}\"", imageTagFile.Properties.Description);

			if ((imageTagFile.TagTypes & TagTypes.Png) == TagTypes.Png)
			{
				if (imageTagFile.GetTag(TagTypes.Png) is PngTag tag)
				{
					foreach (KeyValuePair<string,string> kvp in tag)
						Console.WriteLine("PNG tag [\"{0}\"] = \"{1}\"", kvp.Key, kvp.Value);
				}
			}
			if ((imageTagFile.TagTypes & TagTypes.GifComment) == TagTypes.GifComment)
			{
				if (imageTagFile.GetTag(TagTypes.GifComment) is GifCommentTag tag)
				{
					Console.WriteLine("GIF tag.Value=\"{0}\"; .Comment=\"{1}\"", tag.Value, tag.Comment);
				}
			}
			if ((imageTagFile.TagTypes & TagTypes.JpegComment) == TagTypes.JpegComment)
			{
				if (imageTagFile.GetTag(TagTypes.JpegComment) is JpegCommentTag tag)
				{
					Console.WriteLine("JPEG tag.Value=\"{0}\"; .Comment=\"{1}\"", tag.Value, tag.Comment);
				}
			}
			if ((imageTagFile.TagTypes & TagTypes.TiffIFD) == TagTypes.TiffIFD)
			{
				if (imageTagFile.GetTag(TagTypes.TiffIFD) is IFDTag tag)
				{
					Console.WriteLine("TIFF tag.Structure entries");
					foreach (var dir in tag.Structure.Directories)
					{
						foreach (var entry in dir)
							WriteTaglibTiffDirEntry(entry.Key, entry.Value, "    ");
					}
					Console.WriteLine("TIFF tag.GPSIFD entries");
					foreach (var dir in tag.GPSIFD.Directories)
					{
						foreach (var entry in dir)
							WriteTaglibTiffDirEntry(entry.Key, entry.Value, "    ");
					}
					Console.WriteLine("TIFF tag.ExifIFD entries");
					foreach (var dir in tag.ExifIFD.Directories)
					{
						foreach (var entry in dir)
							WriteTaglibTiffDirEntry(entry.Key, entry.Value, "    ");
					}
				}
			}
			if ((imageTagFile.TagTypes & TagTypes.XMP) == TagTypes.XMP)
			{
				if (imageTagFile.GetTag(TagTypes.XMP) is XmpTag tag)
				{
					foreach (var node in tag.NodeTree.Children)
						WriteTaglibXmpNodeTree(node, "");
				}
			}
		}

		internal static void WriteTaglibTiffDirEntry(ushort key, IFDEntry entry, string indent)
		{
			if (entry is TagLib.IFD.Entries.ShortIFDEntry ifdShort)
			{
				Console.WriteLine("{0}TIFF entry = [{1}] = {2}", indent, ifdShort.Tag, ifdShort.Value);
			}
			else if (entry is TagLib.IFD.Entries.LongIFDEntry ifdLong)
			{
				Console.WriteLine("{0}TIFF entry = [{1}] = {2}", indent, ifdLong.Tag, ifdLong.Value);
			}
			else if (entry is TagLib.IFD.Entries.StringIFDEntry ifdString)
			{
				Console.WriteLine("{0}TIFF entry = [{1}] = \"{2}\"", indent, ifdString.Tag, ifdString.Value);
			}
			else if (entry is TagLib.IFD.Entries.UserCommentIFDEntry ifdUserComment)
			{
				Console.WriteLine("{0}TIFF entry = [{1}] = \"{2}\"", indent, ifdUserComment.Tag, ifdUserComment.Value);
			}
			else if (entry is TagLib.IFD.Entries.RationalIFDEntry ifdRational)
			{
				Console.WriteLine("{0}TIFF entry = [{1}] = {2}/{3}", indent, ifdRational.Tag, ifdRational.Value.Numerator, ifdRational.Value.Denominator);
			}
			else if (entry is TagLib.IFD.Entries.SRationalIFDEntry ifdSRational)
			{
				Console.WriteLine("{0}TIFF entry = [{1}] = {2}/{3}", indent, ifdSRational.Tag, ifdSRational.Value.Numerator, ifdSRational.Value.Denominator);
			}
			else if (entry is TagLib.IFD.Entries.ShortArrayIFDEntry ifdShortArray)
			{
				Console.Write("{0}TIFF entry = [{1}] =", indent, ifdShortArray.Tag);
				foreach (var x in ifdShortArray.Values)
					Console.Write("  {0}", x);
				Console.WriteLine();
			}
			else if (entry is TagLib.IFD.Entries.RationalArrayIFDEntry ifdRationalArray)
			{
				Console.Write("{0}TIFF entry = [{1}] =", indent, ifdRationalArray.Tag);
				foreach (var x in ifdRationalArray.Values)
					Console.Write("  {0}/{1}", x.Numerator, x.Denominator);
				Console.WriteLine();
			}
			else if (entry is TagLib.IFD.Entries.ThumbnailDataIFDEntry ifdThumbnailData)
			{
				Console.WriteLine("{0}TIFF entry = [{1}] has {2} bytes of thumbnail data", indent, ifdThumbnailData.Tag, ifdThumbnailData.Data?.Count);
			}
			else if (entry is TagLib.IFD.Entries.ByteVectorIFDEntry ifdByteVector)
			{
				Console.WriteLine("{0}TIFF entry = [{1}] has {2} bytes of byte vector data", indent, ifdByteVector.Tag, ifdByteVector.Data?.Count);
			}
			else if (entry is TagLib.IFD.Entries.SubIFDEntry ifdSubEntry)
			{
				Console.WriteLine("{0}TIFF entry = [{1}] has {2} subentries in {3} directories", indent, ifdSubEntry.Tag, ifdSubEntry.ChildCount, ifdSubEntry.Count);
				foreach (var dir in ifdSubEntry.Structure.Directories)
				{
					foreach (var subentry in dir)
						WriteTaglibTiffDirEntry(subentry.Key, subentry.Value, indent+"    ");
				}
			}
			else if (entry is TagLib.IFD.Entries.UndefinedIFDEntry ifdUndefined)
			{
				Console.WriteLine("{0}TIFF entry = [{1}] has {2} bytes of undefined data", indent, ifdUndefined.Tag, ifdUndefined.Data?.Count);
			}
			else
			{
				Console.WriteLine("{0}TIFF entry = [{1}] is {2}", indent, entry.Tag, entry.GetType());
			}
		}

		internal static void WriteTaglibXmpNodeTree(XmpNode nodeTree, string indent)
		{
			Console.WriteLine("    {0}XMP \"{1}\" = \"{2}\" [ns=\"{3}\", type=\"{4}\"]", indent, nodeTree.Name, nodeTree.Value, nodeTree.Namespace, nodeTree.Type);
			foreach (var node in nodeTree.Children)
				WriteTaglibXmpNodeTree(node, indent + "    ");
		}
	}
}