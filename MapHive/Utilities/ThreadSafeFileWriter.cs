namespace MapHive.Utilities
{
    using System.Collections.Concurrent;

    public static class ThreadSafeFileWriter
    {
        private static readonly ConcurrentDictionary<string, ConcurrentQueue<string>> _queue = new();
        private static readonly object _writeLock = new();

        private static void writeFromQueue()
        {
            lock (_writeLock)
            {
                try
                {
                    foreach (KeyValuePair<string, ConcurrentQueue<string>> fileEntries in _queue)
                    {
                        if (!fileEntries.Value.IsEmpty)
                        {
                            using StreamWriter writer = File.AppendText(path: fileEntries.Key);
                            while (fileEntries.Value.TryDequeue(result: out string? entry))
                            {
                                writer.Write(value: entry);
                            }
                        }
                    }
                }
                catch { }
            }
        }

        public static void Write(string fileName, string text)
        {
            ConcurrentQueue<string> entries = _queue.GetOrAdd(key: fileName, value: new ConcurrentQueue<string>());
            entries.Enqueue(item: text);
            writeFromQueue();
        }
    }
}
