using FileSyncClient.Config;
using FSWatcher;

namespace FileSyncClient;

public class FileWatcher
{
    private Dictionary<string, FileSystemWatcher> _watchers = new();
    private FileSyncController _fileSyncController;

    public FileWatcher(FileSyncController fileSyncController)
    {
        _fileSyncController = fileSyncController;
        _fileSyncController.Watch();
    }


    public void LoadObjects(List<SynchronizedObject> synchronizedPaths)
    {
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
        watcher.NotifyFilter =  NotifyFilters.CreationTime
                                | NotifyFilters.DirectoryName
                                | NotifyFilters.FileName
                                | NotifyFilters.LastWrite
                                | NotifyFilters.Security 
                                | NotifyFilters.Size;
        watcher.InternalBufferSize = 655360;
        Console.WriteLine("BUFFER SIZE"+watcher.InternalBufferSize);
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
            _fileSyncController.AddNewChange(new FileChange(args.FullPath,FileOperation.FileCreated));
        } );
        watcher.Deleted += ((sender, args) =>
        {
            Console.WriteLine("Directory/File deleted " + args.Name);
            //TODO
        });
        watcher.Changed += (sender, args) =>
        {
            Console.WriteLine("Directory/File changed " + args.Name);
            _fileSyncController.AddNewChange(new FileChange(args.FullPath,FileOperation.FileCreated));
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
        _watchers.Add(path,watcher);
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
    FIleChanged,
    FileDeleted
}