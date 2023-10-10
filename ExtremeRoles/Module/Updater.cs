﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;

using UnityEngine;
using Newtonsoft.Json.Linq;

using ExtremeRoles.Helper;

namespace ExtremeRoles.Module;

#nullable enable

public sealed class ExRRepositoryInfo : Updater.IRepositoryInfo
{
	public string Url => "https://api.github.com/repos/yukieiji/ExtremeRoles/releases/latest";

	public List<string> DllName { private set; get; } = new List<string>()
	{
		"ExtremeRoles.dll"
	};
	private JObject? releaseData = null;

	public async Task<List<Updater.ModUpdateData>> GetModUpdateData(HttpClient client)
	{
		var result = new List<Updater.ModUpdateData>();

		var releaseDataResult = await getReleaseData(client);

		if (!releaseDataResult.HasValue())
		{
			return result;
		}

		var data = releaseDataResult.Value;

		string? tagname = data["tag_name"]?.ToString();
		if (tagname == null)
		{
			return result; // Something went wrong
		}

		JToken assets = data["assets"];
		if (!assets.HasValues)
		{
			return result;
		}
		for (JToken current = assets.First; current != null; current = current.Next)
		{
			string? browser_download_url = current["browser_download_url"]?.ToString();
			if (browser_download_url != null &&
				current[Updater.IRepositoryInfo.ContentType] != null)
			{
				string content = current[Updater.IRepositoryInfo.ContentType].ToString();

				if (content.Equals("application/x-zip-compressed")) { continue; }

				foreach (string dll in this.DllName)
				{
					if (browser_download_url.EndsWith(dll))
					{
						result.Add(new(browser_download_url, dll));
					}
				}
			}
		}
		return result;
	}

	public async Task<bool> HasUpdate(HttpClient client)
	{
		var result = await getReleaseData(client);

		if (!result.HasValue())
		{
			return false;
		}

		var data = result.Value;

		string? tagname = data["tag_name"]?.ToString();
		if (tagname == null)
		{
			return false; // Something went wrong
		}
		// check version
		Version ver = Version.Parse(tagname.Replace("v", ""));
		int? diff = Assembly.GetExecutingAssembly().GetName().Version?.CompareTo(ver);
		return diff < 0;
	}

	private async Task<Expected<JObject>> getReleaseData(HttpClient client)
	{
		if (this.releaseData != null) { return this.releaseData; }

		var response = await client.GetAsync(
			Url, HttpCompletionOption.ResponseContentRead);
		if (response.StatusCode != HttpStatusCode.OK || response.Content == null)
		{
			Logging.Error($"Server returned no data: {response.StatusCode}");
			return null!;
		}

		string json = await response.Content.ReadAsStringAsync();
		JObject data = JObject.Parse(json);

		this.releaseData = data;

		return data;
	}
}


public sealed class Updater
{
	public readonly record struct ModUpdateData(string DownloadUrl, string DllName);

	public interface IRepositoryInfo
	{
		protected const string ContentType = "content_type";

		public string Url { get; }

		public List<string> DllName { get; }

		public Task<List<ModUpdateData>> GetModUpdateData(HttpClient client);

		public Task<bool> HasUpdate(HttpClient client);
	}

	public static Updater Instance = new Updater();

	public bool IsInit => InfoPopup != null;
	public GenericPopup? InfoPopup { private get; set; }
	private HttpClient client = new HttpClient();

	private readonly ConcurrentDictionary<Type, IRepositoryInfo> service = new ConcurrentDictionary<Type, IRepositoryInfo>();

	private static string pluginFolder
	{
		get
		{
			string? auPath = Path.GetDirectoryName(Application.dataPath);
			if (string.IsNullOrEmpty(auPath)) { return string.Empty; }

			return Path.Combine(auPath, "BepInEx", "plugins");
		}
	}

	public Updater()
	{
		this.client = new HttpClient();
		this.client.DefaultRequestHeaders.Add("User-Agent", "ExtremeRoles Updater");

		this.AddRepository<ExRRepositoryInfo>();
	}

	public void AddRepository<TRepoType>() where TRepoType : class, IRepositoryInfo, new()
	{
		AddRepository(new TRepoType());
	}

	public void AddRepository<TRepoType>(TRepoType repository) where TRepoType : class, IRepositoryInfo, new()
	{
		if (!this.service.TryAdd(typeof(TRepoType), repository))
		{
			ExtremeRolesPlugin.Logger.LogError("This instance already added!!");
		}
	}

	public void AddMod<TRepoType>(string dllName) where TRepoType : class, IRepositoryInfo, new()
	{
		TRepoType? repo;

		if (this.service.TryGetValue(typeof(TRepoType), out var instance) &&
			instance is TRepoType castedInstance)
		{
			repo = castedInstance;
		}
		else
		{
			repo = new TRepoType();
			AddRepository(repo);
		}

		repo.DllName.Add(dllName);
	}

	public async Task CheckAndUpdate()
	{
		// TODO: 二重アプデを防ぐ
		if (this.InfoPopup == null) { return; }

		this.InfoPopup.Show(Translation.GetString("chekUpdateWait"));

		try
		{
			List<ModUpdateData> updatingData = new List<ModUpdateData>();

			foreach (var repo in this.service.Values)
			{
				bool hasUpdate = await repo.HasUpdate(this.client);
				if (!hasUpdate) { continue; }

				List<ModUpdateData> updateData = repo.GetModUpdateData(
					this.client).GetAwaiter().GetResult();
				updatingData.AddRange(updateData);
			}

			if (updatingData.Count == 0)
			{
				setPopupText(Translation.GetString("latestNow"));
				return;
			}

			setPopupText(Translation.GetString("updateNow"));
			clearOldVersions();

			this.InfoPopup.StartCoroutine(
				Effects.Lerp(0.01f, new Action<float>(
					(p) => { setPopupText(Translation.GetString("updateInProgress")); })));

			foreach (ModUpdateData data in updatingData)
			{
				var result = getStreamFromUrl(data.DownloadUrl).GetAwaiter().GetResult();

				if (!result.HasValue()) { continue; }

				using (var stream = result.Value)
				{
					installModFromStream(stream, data.DllName);
				}
			}
			this.InfoPopup.StartCoroutine(
				Effects.Lerp(0.01f, new Action<float>(
					(p) => { this.showPopup(Translation.GetString("updateRestart")); })));
		}
		catch (Exception ex)
		{
			Logging.Error(ex.ToString());
			this.showPopup(Translation.GetString("updateManually"));
		}
	}

	private void installModFromStream(Stream stream, string dllName)
	{
		string installDir = pluginFolder;
		if (string.IsNullOrEmpty(installDir)) { return; }

		string installModPath = Path.Combine(installDir, dllName);
		string oldModPath = $"{installModPath}.old";
		if (File.Exists(oldModPath))
		{
			File.Delete(oldModPath);
		}

		File.Move(installModPath, oldModPath);

		using (var fileStream = File.Create(installModPath))
		{
			stream.CopyTo(fileStream);
		}
	}

	private async Task<Expected<Stream>> getStreamFromUrl(string url)
	{
		var response = await this.client.GetAsync(
			new Uri(url),
			HttpCompletionOption.ResponseContentRead);
		if (response.StatusCode != HttpStatusCode.OK || response.Content == null)
		{
			Logging.Error("Server returned no data: " + response.StatusCode.ToString());
			return null!;
		}

		var responseStream = await response.Content.ReadAsStreamAsync();

		return responseStream;
	}

	private void showPopup(string message)
	{
		setPopupText(message);
		if (this.InfoPopup != null)
		{
			this.InfoPopup.gameObject.SetActive(true);
		}
	}

	private void setPopupText(string message)
	{
		if (this.InfoPopup == null)
		{
			return;
		}

		if (this.InfoPopup.TextAreaTMP != null)
		{
			this.InfoPopup.TextAreaTMP.text = message;
		}
	}

	private static void clearOldVersions()
	{
		try
		{
			string installDir = pluginFolder;
			if (string.IsNullOrEmpty(installDir)) { return; }

			DirectoryInfo d = new DirectoryInfo(installDir);
			var files = d.GetFiles("*.old").Select(x => x.FullName); // Getting old versions
			foreach (string f in files)
			{
				File.Delete(f);
			}
		}
		catch (Exception e)
		{
			Logging.Error("Exception occured when clearing old versions:\n" + e);
		}
	}
}
