namespace NazcaWeb.Models
{
    public class VideoItem
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = "";
        public string FullPath { get; set; } = "";
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;
        public string Size { get; set; } = "N/A";
        public string MimeType { get; set; } = "";
        public bool IsDirectory { get; set; } = false;
        public enum Sizes
        {
            B = 1,
            kB,
            MB,
            GB,
            TB
        };

        public VideoItem()
        {

        }

        public VideoItem(string title) : this()
        {
            Title = title;
        }

    }

    public static class VideoModel
    {
        public static FileSystemItem GroupedFiles = new FileSystemItem();

        public static FileSystemItem GetVideos(string path = @"\\DISKSTATION\video1")
        {
            if (GroupedFiles.Subfolders.Count == 0 && GroupedFiles.Files.Count == 0)
            {
                FileSystemHelper.CreateStructure(path, GroupedFiles);
            }

            return GroupedFiles; 
            //return Directory.GetFiles(@"\\DISKSTATION\video1\Filmy Dawid\Private\Kage no Jitsuryokusha ni Naritakute!", "*", SearchOption.AllDirectories).OrderBy(video => video).Select(video => new VideoModel() { FullPath = video, Title = Path.GetFileNameWithoutExtension(video)}).ToList();
        }

        public static VideoItem? GetVideoById(Guid id)
        {
            return GetVideoById(GroupedFiles, id);
        }

        private static VideoItem? GetVideoById(FileSystemItem item, Guid id)
        {
            foreach (var file in item.Files)
            {
                if (file.ID == id)
                {
                    return file;
                }
            }

            foreach (var subfolder in item.Subfolders)
            {
                var video = GetVideoById(subfolder, id);
                if (video != null)
                {
                    return video;
                }
            }

            return null;
        }

        public static FileSystemItem? GetVideoParentItem(string path)
        {
            var targetElem = GroupedFiles;

            var processPath = path.Replace(IRC.StartPath, "").Split("\\", StringSplitOptions.RemoveEmptyEntries).ToList();
            foreach (var part in processPath)
            {
                if (targetElem == null)
                    return null;

                if (targetElem.Subfolders.Any(obj => obj.Name.Contains(part)))
                {
                    targetElem = targetElem.Subfolders.Where(obj => obj.Name.Equals(part)).FirstOrDefault();
                }
            }

            return targetElem;
        }

        public static VideoItem? GetVideoItem(string path, FileSystemItem? parent = null)
        {
            if (parent == null)
                return GetVideoParentItem(path)?.Files.Where(obj => obj.Title.Equals(Path.GetFileNameWithoutExtension(path))).FirstOrDefault();
            else
                return parent.Files.Where(obj => obj.Title.Equals(Path.GetFileNameWithoutExtension(path))).FirstOrDefault();
        }

        public static bool RemoveVideoItem(string path)
        {
            var targetElem = GetVideoParentItem(path);
            var deletionItem = GetVideoItem(path, targetElem);

            if (targetElem == null || deletionItem == null)
                return false;

            targetElem.Files.Remove(deletionItem);

            return true;
        }

        public static FileSystemItem GetVideosByName(string name)
        {
            return GetVideosByName(GroupedFiles, name);
        }

        private static FileSystemItem GetVideosByName(FileSystemItem structure, string name)
        {
            var matchingStructure = new FileSystemItem
            {
                Name = structure.Name
            };

            foreach (var file in structure.Files)
            {
                if (file.Title.Contains(name, StringComparison.OrdinalIgnoreCase))
                {
                    matchingStructure.Files.Add(file);
                }
            }

            foreach (var subfolder in structure.Subfolders)
            {
                var matchingSubfolder = GetVideosByName(subfolder, name);
                if (matchingSubfolder.Files.Count > 0 || matchingSubfolder.Subfolders.Count > 0)
                {
                    matchingStructure.Subfolders.Add(matchingSubfolder);
                }
            }

            return matchingStructure;
        }

        public static string RenderAccordionItem(FileSystemItem item, string rootAccordion = "", int level = 0)
        {
            var guid = Guid.NewGuid();
            var id = "section_" + guid;
            var collapse = "collapse_" + guid;
            var accordion = rootAccordion != "" ? rootAccordion : "accordion_" + guid;
            var path = item.Files.FirstOrDefault()?.FullPath;
            if (path != null)
            {
                path = Directory.GetParent(path)?.FullName;
            }

            var ps = level * 0.8;
            var htmlResult = string.Empty;

            if (item.Subfolders.Count > 0 || item.Files.Count > 1)
            {
                htmlResult += $"<div class=\"accordion-item pt-2 ps-2\" style=\"margin-left: {ps.ToString().Replace(',', '.')}%;\">";
                htmlResult += $"<h2 class=\"accordion-header\" id=\"{collapse}\">";
                htmlResult += $"<button class=\"accordion-button collapsed\" type=\"button\" data-bs-toggle=\"collapse\" data-bs-target=\"#{id}\" aria-expanded=\"false\" aria-controls=\"{id}\" data-bs-parent=\"#{accordion}\">";
                htmlResult += "<div class=\"d-flex justify-content-between w-100 me-3\">";
                htmlResult += "<div>";
                htmlResult += $"<p>{item.Name}</p>";
                htmlResult += "</div>";
                htmlResult += "<div class=\"d-none d-md-block\">";
                htmlResult += $"<p>{path}</p>";
                htmlResult += "</div>";
                htmlResult += "</div>";
                htmlResult += "<i class=\"bi-caret-up-fill\"></i>";
                htmlResult += "</button>";
                htmlResult += "</h2>";
                htmlResult += $"<div id=\"{id}\" class=\"accordion-collapse collapse\" aria-labelledby=\"{collapse}\" data-bs-parent=\"#{accordion}\">";
                htmlResult += "<hr />";
                htmlResult += "<div class=\"accordion-body\">";

                accordion = "accordion_" + Guid.NewGuid();
                var subLevel = level + 1;
                foreach (var subdir in item.Subfolders.OrderBy(v => v.Name))
                {
                    htmlResult += RenderAccordionItem(subdir, accordion, subLevel);
                }

                ps = ps == 0 ? 0.8 : ps;
                foreach (var video in item.Files.OrderBy(v => v.Title))
                {
                    htmlResult += "<div class=\"d-flex justify-content-between\">";
                    htmlResult += $"<a asp-action=\"VideoDetails\" asp-route-videoId=\"{video.ID}\" class=\"text-white link-underline link-underline-opacity-0 w-100 ps-2 py-1\" style=\"margin-left: {ps.ToString().Replace(',', '.')}%;\">{video.Title}</a>";
                    htmlResult += "</div>";
                }
                htmlResult += "<span class=\"mb-1\" style=\"display: inline-block;\"></span>";
                htmlResult += "</div>";
                htmlResult += "</div>";
                htmlResult += "</div>";
            }
            else if (item.Files.Count == 1)
            {
                var index = item.Files.FirstOrDefault()?.ID;
                htmlResult += $"<a asp-action=\"VideoDetails\" asp-route-videoId=\"{index}\" class=\"d-flex justify-content-between text-white link-underline link-underline-opacity-0 ps-2 pt-2\" style=\"margin-left: {ps.ToString().Replace(',', '.')}%;\">";
                htmlResult += "<div>";
                htmlResult += $"<p>{item.Name}</p>";
                htmlResult += "</div>";
                htmlResult += "<div class=\"d-none d-md-block\">";
                htmlResult += $"<p>{path}</p>";
                htmlResult += "</div>";
                htmlResult += "</a>";
            }

            return htmlResult;
        }
    }
}
