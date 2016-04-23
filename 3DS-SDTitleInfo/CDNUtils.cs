namespace _3DS_SDTitleInfo
{
	using System;
	using System.IO;
	using System.Net;
	using System.Security.Cryptography;
	using System.Text;

	public static class CDNUtils
	{
		private static readonly byte[][] Hmm =
			{
				Convert.FromBase64String("SrmkDhRpdahLsbTz7O/Eew=="),
				Convert.FromBase64String("kKC7Hg6GSuh9E6agPSjJuA=="), Convert.FromBase64String("/7tXwU6Y7Gl1s4T89AeGtQ=="),
				Convert.FromBase64String("gJI3mbQfNqanX7i0jJX2bw=="), Convert.FromBase64String("pGmHrkfYK7T6irwEUChfpA=="),
			};

		public static string GetTitleName(string titleId)
		{
			titleId = titleId.ToUpper();

			string metadataUrl = $"https://idbe-ctr.cdn.nintendo.net/icondata/10/{titleId}.idbe";
			byte[] data;
			try
			{
				using (var webClient = new WebClient())
				{
					data = webClient.DownloadData(metadataUrl);
				}
			}
			catch (WebException)
			{
				return "Unknown";
			}

			var dataMinus2 = new byte[data.Length - 2];
			Array.Copy(data, 2, dataMinus2, 0, dataMinus2.Length);
			var iconData = AesDecrypt(dataMinus2, Hmm[data[1]], Hmm[4]);
			var highId = titleId.Substring(0, 4);

			const string Is3dsTitle = "0004";

			if (highId == Is3dsTitle)
			{
				Func<string, string> cleanInput = a => a.TrimEnd('\0').Replace("\n", " ");

				// var titleIdFromData = BitConverter.ToUInt64(iconData, 32).ToString("X16");
				var name = cleanInput(Encoding.Unicode.GetString(iconData, 208 + 512, 256));

				return name;
			}

			return "Unknown";
		}

		private static byte[] AesDecrypt(byte[] encrypted, byte[] key, byte[] iv)
		{
			byte[] result;

			using (var aesManaged = new AesManaged())
			{
				aesManaged.Key = key;
				aesManaged.IV = iv;
				aesManaged.Padding = PaddingMode.None;
				aesManaged.Mode = CipherMode.CBC;

				var transform = aesManaged.CreateDecryptor();

				using (var memoryStream = new MemoryStream())
				{
					using (var cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write))
					{
						cryptoStream.Write(encrypted, 0, encrypted.Length);
					}

					result = memoryStream.ToArray();
				}
			}

			return result;
		}
	}
}