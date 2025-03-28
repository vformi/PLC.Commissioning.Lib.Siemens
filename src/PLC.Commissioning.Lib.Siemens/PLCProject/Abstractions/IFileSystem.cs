namespace PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions
{
    public interface IFileSystem
    {
        string ReadAllText(string path);
        bool FileExists(string path);
        string[] ReadAllLines(string path);
        void DeleteFile(string path); 
        bool DirectoryExists(string path);
        void DeleteDirectory(string path, bool recursive); 
        string[] GetDirectoryFiles(string path, string searchPattern);
    }
}
