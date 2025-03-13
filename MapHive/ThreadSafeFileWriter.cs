using System.Collections.Concurrent;

namespace Extensions
{
    public static class ThreadSafeFileWriter
    {
        private static readonly ConcurrentDictionary<string, ConcurrentQueue<string>> _queue = new();

        private static void writeFromQueue()
        {
            try
            {
                foreach (KeyValuePair<string, ConcurrentQueue<string>> fileEntries in _queue)
                {
                    if (fileEntries.Value.Count != 0)
                    {
                        using StreamWriter writer = File.AppendText(fileEntries.Key);
                        while (fileEntries.Value.TryDequeue(out string entry))
                        {
                            writer.Write(entry);
                        }
                    }
                }
            }
            catch { }
        }

        public static void Write(string fileName, string text)
        {
            ConcurrentQueue<string> entries = _queue.GetOrAdd(fileName, new ConcurrentQueue<string>());
            entries.Enqueue(text);
            writeFromQueue();
        }
    }
}
