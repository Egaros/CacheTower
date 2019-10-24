﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Akavache;
using BenchmarkDotNet.Attributes;
using CacheManager.Core;
using CacheTower.AlternativesBenchmark.Utils;
using CacheTower.Providers.FileSystem.Json;
using CacheTower.Providers.Redis;
using MonkeyCache.FileStore;
using ProtoBuf;

namespace CacheTower.AlternativesBenchmark
{
	[CoreJob, MemoryDiagnoser, MaxIterationCount(200)]
	public class CacheAlternatives_JsonFile_Benchmark : BaseBenchmark
	{
		[Params(1, 100, 1000)]
		public int Iterations;

		private const string DirectoryPath = "CacheAlternatives/FileCache";

		[GlobalSetup]
		public void Setup()
		{
			BlobCache.ApplicationName = nameof(CacheAlternatives_JsonFile_Benchmark);
		}

		[IterationSetup]
		public void IterationSetup()
		{
			if (Directory.Exists(DirectoryPath))
			{
				Directory.Delete(DirectoryPath, true);
			}
		}
		[IterationSetup(Target = nameof(Akavache_LocalMachine))]
		public void AkavacheSetup()
		{
			BlobCache.LocalMachine.InvalidateAll();
		}
		[GlobalCleanup]
		public void GlobalCleanup()
		{
			if (Directory.Exists(DirectoryPath))
			{
				Directory.Delete(DirectoryPath, true);
			}
		}

		[Benchmark(Baseline = true)]
		public async Task CacheTower_JsonFileCacheLayer()
		{
			await using (var cacheStack = new CacheStack(null, new[] { new JsonFileCacheLayer(DirectoryPath) }, Array.Empty<ICacheExtension>()))
			{
				await LoopActionAsync(Iterations, async () =>
				{
					await cacheStack.SetAsync("TestKey", 123, TimeSpan.FromDays(1));
					await cacheStack.GetAsync<int>("TestKey");
					await cacheStack.GetOrSetAsync<string>("GetOrSet_TestKey", (old, context) =>
					{
						return Task.FromResult("Hello World");
					}, new CacheSettings(TimeSpan.FromDays(1)));
				});
			}
		}

		[Benchmark]
		public void MonkeyCache_FileStore()
		{
			var barrel = Barrel.Create(DirectoryPath);
			
			LoopAction(Iterations, () =>
			{
				barrel.Add("TestKey", 123, TimeSpan.FromDays(1));
				barrel.Get<int>("TestKey");
				
				var getOrSetResult = barrel.Get<string>("GetOrSet_TestKey");
				if (getOrSetResult == null)
				{
					barrel.Add("GetOrSet_TestKey", "Hello World", TimeSpan.FromDays(1));
				}
			});
		}
	}
}
