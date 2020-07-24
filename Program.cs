using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Gtk;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SIL.Extensions;

namespace TestImages
{
	public class Program
	{
		static void Main(string[] args)
		{
			var parser = new Parser((settings) =>
			{
				settings.CaseInsensitiveEnumValues = true;
				settings.CaseSensitive = false;
				settings.HelpWriter = Console.Error;
			});
			parser
				.ParseArguments<LoadOptions,MetaOptions,ResizeOptions>(args)
				.WithParsed<LoadOptions>(options =>
				{
					options.ValidateOptions();
					ImageLoader.Run(options);
				})
				.WithParsed<MetaOptions>(options =>
				{
					options.ValidateOptions();
					MetaExtract.Run(options);
				})
				.WithParsed<ResizeOptions>(options =>
				{
					options.ValidateOptions();
					ResizeImage.Run(options);
				})
				;
			Console.Write("FINISHED: hit Return (but not too hard!)");
			Console.ReadLine();
		}

		static readonly HashSet<string> _imageExtensions = new HashSet<string>() {".png", ".jpg", ".jpeg", ".tif", ".tiff", ".gif" };

		public static bool IsImageFile(string filename)
		{
			var extension = System.IO.Path.GetExtension(filename)?.ToLowerInvariant();
			return _imageExtensions.Contains(extension);
		}
	}

	public enum LoadMode
	{
		System,
		Palaso,
		ImageProcessor,
		ImageSharp
	}

	[Verb("load", HelpText = "Just load the image file into memory.")]
	public class LoadOptions
	{
		[Option('f', "folder", Required=false, HelpText="Operate on all image files in the folder, not on a single file.")]
		public bool Folder { get; set; }

		[Option('m', "mode", Required=false, HelpText="How to open the image: System, Palaso, ImageProcessor, ImageSharp")]
		public LoadMode Mode { get; set; }

		//[Option('i', "input", Required = true, HelpText = "Path to the image file to try loading")]
		[Value(0, MetaName= "filename", Required=true, HelpText="Path to the image file to try loading")]
		public string FileName { get; set; }

		public void ValidateOptions()
		{
			// Nothing to validate yet.
		}
	}

	public enum MetaMode
	{
		Palaso,
		Taglib,
		MetadataExtractor
	}

	[Verb("meta", HelpText = "Load the metadata from the image file.")]
	public class MetaOptions
	{
		[Option('f', "folder", Required=false, HelpText="Operate on all image files in the folder, not on a single file.")]
		public bool Folder { get; set; }

		[Option('m', "mode", Required=false, HelpText="How to get the metadata from the image: Palaso, Taglib, MetadataExtractor")]
		public MetaMode Mode { get; set; }

		//[Option('i', "input", Required=true, HelpText="Path to the image file we want the metadata from")]
		[Value(0, MetaName="filename", Required=true, HelpText="Path to the image file we want the metadata from")]
		public string FileName { get; set; }

		public void ValidateOptions()
		{
			// nothing yet.
		}
	}

	public enum ResizeMode
	{
		System,
		ExternalGM
	}

	[Verb("resize", HelpText = "Resize the image, creating a new file.")]
	public class ResizeOptions
	{
		[Option('m', "mode", Required=false, HelpText = "How to resize the image: System, ExternalGM")]
		public ResizeMode Mode { get; set; }

		[Option('w', "width", Required = false, HelpText = "Width of the resized image.")]
		public int Width { get; set; }

		[Option('h', "height", Required = false, HelpText = "Height of the resized image.")]
		public int Height { get; set; }

		[Option('o', "output", Required = true, HelpText = "Path to the new resized image.")]
		public string Output { get; set; }

		[Value(0, MetaName="filename", Required=true, HelpText="Path to the image file we want to resize.")]
		public string FileName { get; set; }

		internal void ValidateOptions()
		{
			if (Output == FileName)
			{
				Console.WriteLine("resize output file cannot be the same as the input file!");
				Environment.Exit(1);
			}
		}
	}
}
