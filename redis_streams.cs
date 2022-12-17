using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

public class StreamProcessor
{
    private readonly IDatabase _database;
    private readonly int _batchSize;
    private readonly int _maxConcurrency;

    public StreamProcessor(IDatabase database, int batchSize, int maxConcurrency)
    {
        _database = database;
        _batchSize = batchSize;
        _maxConcurrency = maxConcurrency;
    }

    public void ProcessStream()
    {
        // create a blocking collection to store stream entries
        var blockingCollection = new BlockingCollection<StreamEntry>();

        // start a task to process stream entries concurrently
        var processingTask = Task.Factory.StartNew(() =>
        {
            // process stream entries until the blocking collection is completed
            while (!blockingCollection.IsCompleted)
            {
                // try to take an entry from the blocking collection
                if (blockingCollection.TryTake(out var entry, Timeout.Infinite))
                {
                    var value = entry.Values[0].ToString();
                    // save value to database here
                }
            }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        // start a task to read and add entries to the blocking collection
        var readingTask = Task.Factory.StartNew(() =>
        {
            while (true)
            {
                // read a batch of entries from the stream
                var batch = _database.StreamRead("OrderDocument", "0-0", _batchSize, CommandFlags.None);
                if (batch.Length == 0)
                {
                    // if there are no more entries, complete the blocking collection
                    // and break out of the loop
                    blockingCollection.CompleteAdding();
                    break;
                }

                // add the entries to the blocking collection
                foreach (var entry in batch)
                {
                    blockingCollection.Add(entry);
                }
            }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        // wait for the reading task to complete
        readingTask.Wait();

        // wait for the processing task to complete
        processingTask.Wait();
    }
}
