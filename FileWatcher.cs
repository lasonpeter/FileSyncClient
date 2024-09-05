using System.Text;
using FileSyncClient.Config;
using FSWatcher;
using RocksDbSharp;
using TransferLib;
using XXHash3NET;

namespace FileSyncClient;

public class FileWatcher
{
    private Dictionary<string, FileSystemWatcher> _watchers = new();
    private FileSyncController _fileSyncController;
    private RocksDb _rocksDb;
    private List<SynchronizedObject> _synchronizedPaths;
    public FileWatcher(FileSyncController fileSyncController, RocksDb rocksDb)
    {
        _fileSyncController = fileSyncController;
        _rocksDb = rocksDb;
        _fileSyncController.Watch();
    }
    
    public void LoadSynchronizedObjects(List<SynchronizedObject> synchronizedPaths)
    {
        _synchronizedPaths = synchronizedPaths;
        Console.WriteLine($"COUNT: {synchronizedPaths.Count}");
        foreach (var synchronizedPath in synchronizedPaths)
        {
            Console.WriteLine($"WATCHING:{synchronizedPath.SynchronizedObjectPath}");
            if(synchronizedPath.IsSynchronized)
                AddWatcher(synchronizedPath.SynchronizedObjectPath);
        }
    }
    
    public void AddScanner()
    {
        new Thread(o =>
        {
            while (true)
            {
                /*FileInfo fileInfo = new FileInfo("/home/xenu/Documents/New Empty File (copy 2)");
                Console.WriteLine(fileInfo.LastAccessTime.ToString("yyyy-MM-dd HH:mm:ss"));
                fileInfo.LastAccessTime=fileInfo.LastAccessTime.AddMonths(1);*/
                Thread.Sleep(new TimeSpan(0,0,10));
            }
        }).Start();
    }
    
    //cells interlinked 
    private List<string> TraverseForChanges()
    {
        throw new Exception("yoikes");
    }


    private void AddWatcher(string path)
    {
        var watcher = new FileSystemWatcher(path);
        watcher.NotifyFilter = NotifyFilters.CreationTime
                               | NotifyFilters.DirectoryName
                               | NotifyFilters.FileName
                               | NotifyFilters.LastWrite
                               | NotifyFilters.Security
                               | NotifyFilters.Size;
        watcher.InternalBufferSize = 655360;
        Console.WriteLine("BUFFER SIZE" + watcher.InternalBufferSize);
        watcher.Error += (sender, args) =>
        {
            {
                Console.WriteLine("ERROR: " + args.GetException());
                //recorderTime = Environment.TickCount64;
                //_fileSyncController.AddNewChange(new FileChange(s,FileOperation.DirectoryCreated));
            }
        };
        watcher.Created += ((sender, args) =>
        {
            Console.WriteLine("Directory/File created " + args.Name);
            _fileSyncController.AddNewChange(new FileChange(args.FullPath, FileOperation.FileCreated));
        });
        watcher.Deleted += ((sender, args) =>
        {
            Console.WriteLine("Directory/File deleted " + args.Name);
            //TODO
        });
        watcher.Changed += (sender, args) =>
        {
            Console.WriteLine("Directory/File changed " + args.Name);
            _fileSyncController.AddNewChange(new FileChange(args.FullPath, FileOperation.FileCreated));
        };
        watcher.Renamed += (sender, args) =>
        {
            Console.WriteLine("Directory/File renamed ");
            Console.WriteLine($"    From:{args.OldName} to {args.Name}");
            _fileSyncController.AddNewChange(new FileChange(args.FullPath, FileOperation.FileCreated));
        };
        //watcher.Filter = "*.txt";
        watcher.IncludeSubdirectories = true;
        watcher.EnableRaisingEvents = true;
        _watchers.Add(path, watcher);
    }

    /// <summary>
    /// Used for scanning the directory and checking the hashes of files with the server saved hashes
    /// </summary>
    /// <param name="directoryInfo">Directory to scan</param>
    /// <param name="hashCheckPairs"></param>
    public void CheckHashesWithServer(DirectoryInfo directoryInfo, List<HashCheckPair> hashCheckPairs)
    {
        Console.WriteLine("WE");
        if (directoryInfo.GetDirectories().Length > 0)
        {
            try
            {
                foreach (var dict in directoryInfo.GetDirectories())
                {
                    CheckHashesWithServer(dict,hashCheckPairs);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        if (directoryInfo.GetFiles().Length > 0)
        {
            foreach (var fileInfo in directoryInfo.GetFiles())
            {
                try
                {
                    Console.WriteLine(fileInfo.FullName);
                    if(_rocksDb.HasKey(Encoding.UTF8.GetBytes(fileInfo.FullName)))// Checks if there is a filepath like this in DB
                    { //Only looks up FUUID and updates the respective HASH
                        Console.WriteLine("EXISTED");
                        Guid fuuid = new Guid(_rocksDb.Get(Encoding.UTF8.GetBytes(fileInfo.FullName)));

                        var resp =BitConverter.ToUInt64(_rocksDb.Get(fuuid.ToByteArray()));
                        //Generate HASH
                        ulong hash64;
                        using var memoryStream = new MemoryStream();
                        {
                            hash64 = XXHash3.Hash64(fileInfo.OpenRead());
                            Console.WriteLine($"Hash: {hash64} fuuid: {fuuid.ToString()}");
                        }
                        if (resp != hash64)
                        {
                            Console.WriteLine("UPDATED");
                            _rocksDb.Put(fuuid.ToByteArray(),BitConverter.GetBytes(hash64));
                        }
                        hashCheckPairs.Add(new HashCheckPair(){FuuId = fuuid,Hash = resp});
                    }
                    else
                    { 
                        //Adds new FP->FUUID and FUUID->HASH
                        var fuuid = Guid.NewGuid();
                        Console.WriteLine($"CREATING NEW {fuuid.ToByteArray().Length}");
                        //Console.WriteLine(BitConverter.ToString(fuuid));

                        _rocksDb.Put(Encoding.UTF8.GetBytes(fileInfo.FullName),fuuid.ToByteArray());
                        ulong hash64;
                        using var memoryStream = new MemoryStream();
                        {
                            hash64 = XXHash3.Hash64(fileInfo.OpenRead());
                            Console.WriteLine(hash64);
                        }
                        _rocksDb.Put(fuuid.ToByteArray(),BitConverter.GetBytes(hash64));
                        hashCheckPairs.Add(new HashCheckPair(){FuuId = fuuid,Hash = hash64});

                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            if (hashCheckPairs.Count > 5)
            {
                //Don't know what to do
            }
        }
    }

    public void UpdateSyncedFilesHashes()
    {
        Parallel.ForEach(_synchronizedPaths, o =>
        {
            HashUpdate(new DirectoryInfo(o.SynchronizedObjectPath));
        });
    }
    /// <summary>
    /// Updates hashes in rocksdb for all synced files in a given directory 
    /// </summary>
    /// <param name="directoryInfo">Directory which one's file's hashes to update</param>
    /// TODO: Change it from using recursion to iteration to avoid StackOverflow exception
    private void HashUpdate(DirectoryInfo directoryInfo)
    {
        if (directoryInfo.GetDirectories().Length > 0)
        {
            try
            {
                foreach (var dict in directoryInfo.GetDirectories())
                {
                    HashUpdate(dict);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        if (directoryInfo.GetFiles().Length > 0)
        {
            foreach (var fileInfo in directoryInfo.GetFiles())
            {
                try
                {
                    Console.WriteLine(fileInfo.FullName);
                    if(_rocksDb.HasKey(Encoding.UTF8.GetBytes(fileInfo.FullName)))// Checks if there is a filepath like this in DB
                    { //Only looks up FUUID and updates the respective HASH
                        Console.WriteLine("EXISTED");
                        var fuuid = _rocksDb.Get(Encoding.UTF8.GetBytes(fileInfo.FullName));

                        var resp =_rocksDb.Get(fuuid);
                        //Generate HASH
                        ulong hash64;
                        using var memoryStream = new MemoryStream();
                        {
                            hash64 = XXHash3.Hash64(fileInfo.OpenRead());
                            Console.WriteLine($"Hash: {hash64} fuuid: {new Guid(fuuid).ToString()}");
                        }
                        if (BitConverter.ToUInt64(resp) != hash64)
                        {
                            Console.WriteLine("UPDATED");
                        }
                        _rocksDb.Put(fuuid,BitConverter.GetBytes(hash64));
                    }
                    else
                    { //Adds new FP->FUUID and FUUID->HASH
                        var fuuid = Guid.NewGuid().ToByteArray();
                        Console.WriteLine($"CREATING NEW {fuuid.Length}");
                        Console.WriteLine(BitConverter.ToString(fuuid));

                        _rocksDb.Put(Encoding.UTF8.GetBytes(fileInfo.FullName),fuuid);
                        ulong hash64;
                        using var memoryStream = new MemoryStream();
                        {
                            hash64 = XXHash3.Hash64(fileInfo.OpenRead());
                            Console.WriteLine(hash64);
                        }
                        _rocksDb.Put(fuuid,BitConverter.GetBytes(hash64));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
        return;
    }
}

public class FileChange
{
    public string FilePath;
    public FileOperation FileOperation;

    public FileChange(string filePath, FileOperation fileOperation)
    {
        FilePath = filePath;
        FileOperation = fileOperation;
    }
}

public enum FileOperation
{
    DirectoryCreated,
    DirectoryDeleted,
    FileCreated,
    FileChanged,
    FileDeleted
}