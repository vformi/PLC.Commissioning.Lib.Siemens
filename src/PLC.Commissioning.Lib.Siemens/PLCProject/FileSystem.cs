using System.IO;
using PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions;

namespace PLC.Commissioning.Lib.Siemens.PLCProject
{
    public class FileSystemWrapper : IFileSystem
    {
        public string ReadAllText(string path) => File.ReadAllText(path);
        public bool FileExists(string path) => File.Exists(path);
        public string[] ReadAllLines(string path) => File.ReadAllLines(path);
        public void DeleteFile(string path) => File.Delete(path);
        public bool DirectoryExists(string path) => Directory.Exists(path);
        public void DeleteDirectory(string path, bool recursive) => Directory.Delete(path, recursive);
    }
}