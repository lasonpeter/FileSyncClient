using FileSyncClient.Config;
using FSWatcher;

namespace FileSyncClient;

public class FileWatcher
{
    private Dictionary<string, FileSystemWatcher> _watchers = new();
    private long recorderTime=0;
    private FileSyncController _fileSyncController;

    public FileWatcher(FileSyncController fileSyncController)
    {
        _fileSyncController = fileSyncController;
    }

    public void Watch()
    {
        //If there are 5 secs of inactivity then sync the changes
        long timeElapsed = 0;
        new Thread(o =>
        {
            while(true){
                timeElapsed = Environment.TickCount64 - recorderTime;
                Console.WriteLine(timeElapsed);
                if (timeElapsed > 5_000 && _fileSyncController.Queue.Count > 0 )
                {
                    Console.WriteLine("WEEEEEEEEEEEEEEEEEE");
                    _fileSyncController.Sync();
                    recorderTime = Environment.TickCount64;
                }

                Thread.Sleep(1_000);
            }
        }).Start();
    }

    public void LoadObjects(List<SynchronizedObject> objects)
    {
        Console.WriteLine($"COUNT: {objects.Count}");
        foreach (var synchronizedObject in objects)
        {
            Console.WriteLine($"WATCHING:{synchronizedObject.SynchronizedObjectPath}");
            if(synchronizedObject.IsSynchronized)
                AddWatcher(synchronizedObject.SynchronizedObjectPath);
        }
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
            recorderTime = Environment.TickCount64;
        } );
        watcher.Deleted += ((sender, args) =>
        {
            Console.WriteLine("Directory/File deleted " + args.Name);
            recorderTime = Environment.TickCount64;
        });
        watcher.Changed += (sender, args) =>
        {
            Console.WriteLine("Directory/File changed " + args.Name);
            _fileSyncController.AddNewChange(new FileChange(args.FullPath,FileOperation.FileCreated));
            recorderTime = Environment.TickCount64;
        };
        watcher.Renamed += (sender, args) =>
        {
            Console.WriteLine("Directory/File renamed ");
            Console.WriteLine($"    From:{args.OldName} to {args.Name}");
            _fileSyncController.AddNewChange(new FileChange(args.FullPath, FileOperation.FileCreated));
            recorderTime = Environment.TickCount64;
        }; 
        //watcher.Filter = "*.txt";
        watcher.IncludeSubdirectories = true;
        watcher.EnableRaisingEvents = true;
        /*var watcher =
            new Watcher(
                path,
                (s) =>
                {
                    Console.WriteLine("Directory created " + s);
                    recorderTime = Environment.TickCount64;
                    //_fileSyncController.AddNewChange(new FileChange(s,FileOperation.DirectoryCreated));
                },
                (s) => {
                    Console.WriteLine("Directory deleted " + s);
                    recorderTime = Environment.TickCount64;
                    //_fileSyncController.AddNewChange(new FileChange(s,FileOperation.DirectoryDeleted));
                },
                (s) => {
                    Console.WriteLine("File created " + s);
                    recorderTime = Environment.TickCount64;
                    _fileSyncController.AddNewChange(new FileChange(s,FileOperation.FileCreated));
                },
                (s) => {
                    Console.WriteLine("File changed " + s);
                    recorderTime = Environment.TickCount64;
                    //_fileSyncController.AddNewChange(new FileChange(s,FileOperation.FIleChanged));
                },
                (s) => {
                    recorderTime = Environment.TickCount64;
                    Console.WriteLine("File deleted " + s);
                    //_fileSyncController.AddNewChange(new FileChange(s,FileOperation.FileDeleted));
                });
        //watcher.Settings.SetPollFrequencyTo(100);
        Console.WriteLine($"is continuous {watcher.Settings.ContinuousPolling}");
        watcher.Watch();*/
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