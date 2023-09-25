using TagLib;

namespace NazcaWeb.Models
{
    public class FileSystemItem
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public List<FileSystemItem> Subfolders { get; set; } = new List<FileSystemItem>();
        public List<VideoItem> Files { get; set; } = new List<VideoItem>();
    }

    public class FileSystemHelper
    {
        public static void CreateStructure(string path, FileSystemItem files)
        {
            CreateStructure(new DirectoryInfo(path), files);
        }

        private static FileSystemItem CreateStructure(DirectoryInfo directoryInfo, FileSystemItem? files = null)
        {
            var structure = files ?? new FileSystemItem
            {
                Name = directoryInfo.Name,
                FullPath = directoryInfo.FullName,
            };
            
            foreach (var directory in directoryInfo.GetDirectories())
            {
                var substructure = CreateStructure(directory);
                structure.Subfolders.Add(substructure);
            }

            foreach (var file in directoryInfo.GetFiles())
            {
                try
                {
                    var counter = 1;
                    double size = file.Length;
                    while (size > 1024)
                    {
                        size /= 1024;
                        counter++;
                    }

                    structure.Files.Add(new VideoItem()
                    {
                        Title = Path.GetFileNameWithoutExtension(file.FullName),
                        FullPath = file.FullName.Replace("\\", "\\\\"),
                        Size = Math.Round(size, 2) + " " + Enum.GetName(typeof(VideoItem.Sizes), counter),
                        IsDirectory = System.IO.File.GetAttributes(file.FullName) == FileAttributes.Directory
                    });
                }
                catch (UnsupportedFormatException) { }
            }

            structure.Subfolders.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            structure.Files.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase));

            return structure;
        }
    }
}
