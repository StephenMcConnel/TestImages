using System;

namespace TestImages
{
	public class ImageLoader
	{
		public static void Run(LoadOptions options)
		{
			try
			{
				Console.WriteLine("Loading {0}", options.FileName);
				MemoryManagement.CheckMemory($"Before loading image(s) using {options.Mode}", true);
				if (options.Folder)
				{
					foreach (var file in System.IO.Directory.EnumerateFiles(options.FileName))
					{
						if (Program.IsImageFile(file))
							LoadImageAndReport(options.Mode, file);								
					}
				}
				else
				{
					LoadImageAndReport(options.Mode, options.FileName);
				}
				MemoryManagement.CheckMemory("After disposing image(s)", true);
			}
			catch (Exception ex)
			{
				Console.WriteLine("EXCEPTION CAUGHT: {0}", ex);
				Console.WriteLine("{0}", ex.StackTrace);
			}
		}

		private static void LoadImageAndReport(LoadMode mode, string filename)
		{
			switch (mode)
			{
				case LoadMode.System:
					using (var image = System.Drawing.Image.FromFile(filename))
					{
						Console.WriteLine("{0} image size = {1}w by {2}h", System.IO.Path.GetFileName(filename), image.Width, image.Height);
						MemoryManagement.CheckMemory("With image loaded", true);
					}
					break;
				case LoadMode.Palaso:
					using (var image = SIL.Windows.Forms.ImageToolbox.PalasoImage.FromFile(filename))
					{
						Console.WriteLine("{0} image size = {1}w by {2}h", System.IO.Path.GetFileName(filename), image.Image.Width, image.Image.Height);
						MemoryManagement.CheckMemory("With image loaded", true);
					}
					break;
				case LoadMode.ImageProcessor:
					using (var factory = new ImageProcessor.ImageFactory(true))
					{
						factory.Load(filename);
						Console.WriteLine("{0} image size = {1}w by {2}h", System.IO.Path.GetFileName(filename), factory.Image.Width, factory.Image.Height);
						MemoryManagement.CheckMemory("With image loaded", true);
					}
					break;
				case LoadMode.ImageSharp:
					using (var image = SixLabors.ImageSharp.Image.Load(filename))
					{
						Console.WriteLine("{0} image size = {1}w by {2}h", System.IO.Path.GetFileName(filename), image.Width, image.Height);
						MemoryManagement.CheckMemory("With image loaded", true);
					}
					break;
			}
		}
	}
}