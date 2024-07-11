namespace FileSyncClient.FileStructureIntrospection;


public class DirectoryChangeInfo
{
    //public List<DirectoryChangeInfo> DirectoryChangeInfos { get; set; } = new List<DirectoryChangeInfo>();
    public List<FileChangeInfo> FileChangeInfos { get; set; } = new List<FileChangeInfo>();
}

public class FileChangeInfo
{
    public string FilePath { get; set; }
    public long Hash { get; set; }
}