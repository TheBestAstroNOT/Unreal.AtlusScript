using Unreal.AtlusScript.Interfaces;

namespace Unreal.AtlusScript.Reloaded.AtlusScript.Models;

internal abstract class BaseAssetContainer
{
    private readonly AtlusAssetCompiler compiler;
    private readonly bool isFlow;
    public readonly bool isUniversal;

    protected BaseAssetContainer(AtlusAssetCompiler compiler, string name, bool isFlow, bool isUniversal)
    {
        this.compiler = compiler;
        this.isFlow = isFlow;
        this.Name = name;
        this.isUniversal = isUniversal;
    }

    public string Name { get; }

    public byte[]? Data { get; private set; }

    public required AssetMode Mode { get; init; }

    protected abstract string Source { get; }

    public void Sync()
    {
        if (this.isFlow)
        {
            this.Data = this.compiler.CompileBF(this.Name, this.Source);
        }
        else
        {
            this.Data = this.compiler.CompileBMD(this.Name, this.Source);
        }

        if (this.Data != null)
        {
            Log.Debug($"{this.Name}: Updated data from source.");
        }
    }
}
