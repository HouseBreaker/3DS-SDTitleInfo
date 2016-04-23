namespace _3DS_SDTitleInfo
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Text;

	public static class SDTitleInfoMain
	{
		public static void Main(string[] args)
		{
			ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
			Console.OutputEncoding = Encoding.Unicode;

			string pathToSd;

			if (args.Length != 0)
			{
				pathToSd = ValidatePathToSd(args[0]);
			}
			else
			{
				Console.Write(@"Please specify where your SD card is (example: ""G:""): ");
				pathToSd = ValidatePathToSd(Console.ReadLine());
			}

			Directory.SetCurrentDirectory(pathToSd);

			var nin3DsFolder = pathToSd + "\\Nintendo 3DS";

			if (!Directory.Exists(nin3DsFolder))
			{
				Console.WriteLine("Couldn't find Nintendo 3DS folder!");
				return;
			}

			var info = new DirectoryInfo(nin3DsFolder);

			var ids = info.GetDirectories().Where(d => d.Name != "Private").ToArray();

			long totalTitleSize = 0;

			foreach (var id in ids)
			{
				var titlesFolder = new DirectoryInfo(id.GetDirectories()[0].FullName + "\\title");

				var titleTypes = titlesFolder.GetDirectories();

				var titles = new Dictionary<string, long>();

				foreach (var titleType in titleTypes)
				{
					var highId = titleType.Name;
					var lowIds = titleType.GetDirectories();

					foreach (var titleFolder in lowIds)
					{
						var lowId = titleFolder.Name;
						var titleId = highId + lowId;

						var contentFolder = new DirectoryInfo(titleFolder.FullName + "\\content");

						var fileSizeCombined = contentFolder.GetFiles().Sum(a => a.Length);
						totalTitleSize += fileSizeCombined;

						titles[titleId] = fileSizeCombined;
					}
				}

				titles = titles.OrderBy(a => a.Key.Substring(3, 4))
					.ThenByDescending(a => a.Value)
					.ToDictionary(a => a.Key, b => b.Value);

				foreach (var title in titles)
				{
					var name = CDNUtils.GetTitleName(title.Key);

					var fullWidthPadding = GetFullWidthExtraPadding(name);

					Console.WriteLine($"{title.Key} - {name.PadRight(50 - fullWidthPadding)} {HumanReadableFileSize(title.Value)}");
				}
			}
			
			var driveInfo = new DriveInfo(pathToSd);
			var usedSpace = driveInfo.TotalSize - driveInfo.AvailableFreeSpace;
			var freeSpace = driveInfo.AvailableFreeSpace;

			Console.WriteLine("Used space: {0} Free space: {1}", HumanReadableFileSize(usedSpace), HumanReadableFileSize(freeSpace));
			Console.WriteLine("Total size of titles: " + HumanReadableFileSize(totalTitleSize));
		}

		private static string ValidatePathToSd(string pathToSd)
		{
			while (pathToSd == null || !Directory.Exists(pathToSd))
			{
				Console.Write(@"Invalid path. Try again: ");
				pathToSd = Console.ReadLine();
			}

			return pathToSd;
		}

		private static int GetFullWidthExtraPadding(string name)
		{
			var fullWidthPadding = 0;

			foreach (var letter in name)
			{
				for (int i = '\u2E80'; i < '\uA48C'; i++)
				{
					if (letter == i)
					{
						fullWidthPadding++;
						break;
					}
				}

				for (int i = '\uFF00'; i < '\uFFEF'; i++)
				{
					if (letter == i)
					{
						fullWidthPadding++;
						break;
					}
				}
			}

			return fullWidthPadding;
		}

		private static string HumanReadableFileSize(long size)
		{
			string[] sizes = { "B", "KB", "MB", "GB" };

			var order = 0;

			double actualSize = size;

			while (actualSize >= 1024 && order + 1 < sizes.Length)
			{
				order++;
				actualSize /= 1024;
			}

			return $"{actualSize:0.##} {sizes[order]}";
		}
	}
}