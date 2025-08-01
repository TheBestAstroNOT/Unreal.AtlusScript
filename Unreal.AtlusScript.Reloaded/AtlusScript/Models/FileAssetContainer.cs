﻿namespace Unreal.AtlusScript.Reloaded.AtlusScript.Models;

using Timer = System.Timers.Timer;

internal class FileAssetContainer : BaseAssetContainer
{
    private readonly string file;
    private readonly bool isUniversal;
    private readonly FileSystemWatcher watcher;
    private readonly Timer timer = new(500) { AutoReset = false };


    public FileAssetContainer(AtlusAssetCompiler compiler, string file, bool isUniversal = true)
        : base(compiler, Path.GetFileNameWithoutExtension(file), Path.GetExtension(file).Equals(".flow", StringComparison.OrdinalIgnoreCase), isUniversal)
    {
        this.isUniversal = isUniversal;
        this.file = file;
        this.watcher = new(Path.GetDirectoryName(file)!, Path.GetFileName(file))
        {
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite,
        };

        this.watcher.Changed += (_, _) => { this.timer.Stop(); this.timer.Start(); };
        this.timer.Elapsed += (_, _) => { this.Sync(); };
        this.isUniversal = isUniversal;
    }

    protected override string Source => File.ReadAllText(file);
}
