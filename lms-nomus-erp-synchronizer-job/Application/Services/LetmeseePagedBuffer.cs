namespace lms_nomus_erp_synchronizer_job.Application.Services;

/// <summary>
/// Acumula itens vindos do Nomus (página a página) e dispara envio ao Letmesee em lotes fixos,
/// sem manter o histórico inteiro em memória.
/// </summary>
public sealed class LetmeseePagedBuffer<T>
{
    private readonly List<T> _buffer;
    private readonly int _batchSize;
    private readonly Func<IReadOnlyList<T>, Task> _sendBatchAsync;

    public LetmeseePagedBuffer(int batchSize, Func<IReadOnlyList<T>, Task> sendBatchAsync)
    {
        if (batchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(batchSize));

        _batchSize = batchSize;
        _sendBatchAsync = sendBatchAsync;
        _buffer = new List<T>(batchSize);
    }

    public int TotalSent { get; private set; }

    public int PendingCount => _buffer.Count;

    public void AddRange(IEnumerable<T> items) => _buffer.AddRange(items);

    public async Task FlushPendingAsync(CancellationToken cancellationToken = default)
    {
        while (_buffer.Count >= _batchSize)
        {
            var batch = _buffer.Take(_batchSize).ToList();
            _buffer.RemoveRange(0, _batchSize);
            await _sendBatchAsync(batch);
            TotalSent += batch.Count;
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    public async Task FlushRemainderAsync(CancellationToken cancellationToken = default)
    {
        if (_buffer.Count == 0)
            return;

        var batch = _buffer.ToList();
        _buffer.Clear();
        await _sendBatchAsync(batch);
        TotalSent += batch.Count;
        cancellationToken.ThrowIfCancellationRequested();
    }
}
