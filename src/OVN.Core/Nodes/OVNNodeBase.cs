using Dbosoft.OVN.OSCommands;
using JetBrains.Annotations;
using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN.Nodes;

[PublicAPI]
public abstract class OVNNodeBase : OVSNodeBase
{
    private Arr<DemonProcessBase> _demons;

    public NodeStatus Status { get; private set; } = NodeStatus.Stopped;



    protected abstract IEnumerable<DemonProcessBase> SetupDemons();

    protected virtual EitherAsync<Error, Unit> BeforeProcessStarted(DemonProcessBase process,
        CancellationToken cancellationToken)
    {
        return Unit.Default;
    }

    protected virtual EitherAsync<Error, Unit> OnProcessStarted(DemonProcessBase process,
        CancellationToken cancellationToken)
    {
        return Unit.Default;
    }

    private static EitherAsync<Error, IEnumerable<TR>> RunDemonsOp<TR>(
        IEnumerable<DemonProcessBase> demons,
        Func<DemonProcessBase, EitherAsync<Error, TR>> func)
    {
        return RunDemonsOpAsync(demons, func).ToAsync();
    }

    private static async Task<Either<Error, IEnumerable<TR>>> RunDemonsOpAsync<TR>(
        IEnumerable<DemonProcessBase> demons,
        Func<DemonProcessBase, EitherAsync<Error, TR>> func)
    {
        var arr = new Arr<Either<Error, TR>>();

        // ReSharper disable once LoopCanBeConvertedToQuery
        // force sequential processing of demon tasks
        foreach (var demon in demons) arr = arr.Add(await func(demon));

        return arr.Length == 0
            ? Prelude.Right<Error, IEnumerable<TR>>(Enumerable.Empty<TR>())
            : arr.Traverse(l => l).Map(r => r.AsEnumerable());
    }

    public override EitherAsync<Error, Unit> Start(CancellationToken cancellationToken = default)
    {
        var statusBefore = Status;
        Status = NodeStatus.Starting;
        _demons = SetupDemons().ToArr();
        return RunDemonsOp(_demons, d =>
                BeforeProcessStarted(d, cancellationToken)
                    .Bind(_ => d.Start(cancellationToken))
                    .Bind(_ => OnProcessStarted(d, cancellationToken)))
            .Map(_ =>
            {
                Status = NodeStatus.Started;
                return Unit.Default;
            }).MapLeft(l =>
            {
                Status = statusBefore;
                return l;
            });
    }

    public override EitherAsync<Error, Unit> Stop(CancellationToken cancellationToken = default)
    {
        var statusBefore = Status;

        Status = NodeStatus.Stopping;
        return RunDemonsOp(_demons.Reverse(), d => d.Stop(cancellationToken))
            .Map(_ =>
            {
                Status = NodeStatus.Stopped;
                return Unit.Default;
            }).MapLeft(l =>
            {
                Status = statusBefore;
                return l;
            });
    }


    public override EitherAsync<Error, Unit> EnsureAlive(bool checkResponse, CancellationToken cancellationToken = default)
    {
        return RunDemonsOp(_demons, d => d.CheckAlive(
                checkResponse, cancellationToken: cancellationToken))
            .Map(_ => Unit.Default);
    }

    public async Task<bool> WaitForStart(CancellationToken cancellationToken = default)
    {
        if (Status != NodeStatus.Started && Status != NodeStatus.Starting)
            return false;

        while (Status == NodeStatus.Starting) await Task.Delay(1000, cancellationToken);

        return Status == NodeStatus.Started;
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing) return;

        foreach (var demon in _demons) demon.Dispose();
        _demons = Arr<DemonProcessBase>.Empty;
    }
}