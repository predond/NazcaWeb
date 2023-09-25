using Microsoft.AspNetCore.Routing;
using System.Windows.Forms;
using System.Xml.Linq;
using System;
using System.Reflection;

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

        public static async Task<dynamic> RenderAccordionItem(FileSystemItem item, string rootAccordion = "", int level = 0)
        {
            var html = "";

            var guid = Guid.NewGuid();
            var id = "section_" + guid;
            var collapse = "collapse_" + guid;
            var accordion = rootAccordion != "" ? rootAccordion : "accordion_" + guid;
            var bg = level % 2 == 0 ? "even" : "even-white";

            if (item.Subfolders.Count > 0 || item.Files.Count > 1)
            {
                if (item.Name != "")
                {
                    html += "<div class=\"accordion-item rounded shadow-sm p-3 my-2 border border-light " + bg + "\">";
                        html += "<h2 class=\"accordion-header\" id=\"" + collapse + "\">";
                            html += "<button class=\"accordion-button collapsed\" type=\"button\" data-bs-toggle=\"collapse\" data-bs-target=\"#" + id + "\" aria-expanded=\"false\" aria-controls=\"" + id + "\" data-bs-parent=\"#" + accordion + "\">";
                                html += "<div class=\"d-flex justify-content-between w-100 me-3\">";
                                    html += "<div>";
                                        html += "<p>" + item.Name + "</p>";
                                    html += "</div>";
                                    html += "<div class=\"d-none d-md-block\">";
                                        html += "<p>" + item.FullPath.Replace("\\", "\\\\") + "</p>";
                                    html += "</div>";
                                html += "</div>";
                                html += "<i class=\"bi-caret-up-fill\"></i>";
                            html += "</button>";
                        html += "</h2>";
                        html += "<div id=\"" + id + "\" class=\"accordion-collapse collapse\" aria-labelledby=\"" + collapse + "\" data-bs-parent=\"#" + accordion + "\">";
                            html += "<div class=\"accordion-body\">";
                                accordion = "accordion_" + Guid.NewGuid();
                                html += "<hr />";
                                html += "<div id = \"" + accordion + "\">";
                                    var subLevel = bg == "even" ? 1 : 0;

                                    foreach (var subdir in item.Subfolders.OrderBy(v => v.Name).Select((element, index) => new { Index = index, Element = element}).ToArray())
                                    {
                                        var result = await RenderAccordionItem(subdir.Element, accordion, subLevel);
                                        subLevel = result.level;
                                        html += result.html;
                                    }
                                html += "</div>";

                                foreach(var video in item.Files.OrderBy(v => v.Title).Select((element, index) => new { Index = index, Element = element }).ToArray())
                                {
                                    var bg_color = bg == "even" ? (video.Index % 2 == 0 ? "even-white" : "even") : (video.Index % 2 == 0 ? "even" : "even-white");
                                    html += "<div class=\"rounded shadow-sm p-3 my-2 d-flex justify-content-between " + bg_color + "\">";
                                        html += $"<a href=\"/Films/Details?videoid={video.Element.ID}\" class=\"text-body link-underline link-underline-opacity-0 w-100\">" + video.Element.Title + "</a>";
                                    html += "</div>";
                                }
                            html += "</div>";
                        html += "</div>";
                    html += "</div>";
                }
                else
                {
                    var subLevel = bg == "even" ? 1 : 0;

                    foreach (var subdir in item.Subfolders.OrderBy(v => v.Name).Select((element, index) => new { Index = index, Element = element }).ToArray())
                    {
                        var result = await RenderAccordionItem(subdir.Element, accordion, subLevel);
                        subLevel = result.level;
                        html += result.html;
                    }

                    foreach (var video in item.Files.OrderBy(v => v.Title).Select((element, index) => new { Index = index, Element = element }).ToArray())
                    {
                        var bg_color = bg == "even" ? (video.Index % 2 == 0 ? "even-white" : "even") : (video.Index % 2 == 0 ? "even" : "even-white");
                        html += "<div class=\"rounded shadow-sm p-3 my-2 d-flex justify-content-between " + bg_color + "\">";
                        html += $"<a href=\"/Films/Details?videoid={video.Element.ID}\" class=\"text-body link-underline link-underline-opacity-0 w-100\">" + video.Element.Title + "</a>";
                        html += "</div>";
                    }
                }
            }
            else if (item.Files.Count == 1)
            {
                var index = item.Files.FirstOrDefault()?.ID;

                html += $"<a href=\"/Films/Details?videoid={index}\" class= \"rounded shadow-sm p-3 my-2 d-flex justify-content-between text-body link-underline link-underline-opacity-0 " + bg + "\">";
                    html += "<div>";
                        html += "<p>" + item.Name + "</p>";
                    html += "</div>";
                    html += "<div class= \"d-none d-md-block\" >";
                        html += "<p>" + item.FullPath + "</p>";
                    html += "</div>";
                html += "</a>";
            }

            level++;

            return new { level = level, html = html };
        }

        public static async Task<string> RenderNazcaAccordionItem(FileSystemItem item, string rootAccordion = "", int level = 0)
        {
            var html = "";

            var guid = Guid.NewGuid();
            var id = "section_" + guid;
            var collapse = "collapse_" + guid;
            var accordion = rootAccordion != "" ? rootAccordion : "accordion_" + guid;

            var ps = 0.8;
            if (item.Subfolders.Count > 0 || item.Files.Count > 1)
            {
                if (item.Name != "")
                {
                    html += $"<div class=\"accordion-item pt-2 ps-2\" style=\"margin-left: {ps.ToString().Replace(',', '.')}%;\">";
                        html += $"<h2 class=\"accordion-header\" id=\"{collapse}\">";
                            html += $"<button class=\"accordion-button collapsed\" type=\"button\" data-bs-toggle=\"collapse\" data-bs-target=\"#{id}\" aria-expanded=\"false\" aria-controls=\"{id}\" data-bs-parent=\"#{accordion}\">";
                                html += $"<div class=\"d-flex justify-content-between w-100 me-3\">";
                                    html += $"<div>";
                                        html += $"<p>{item.Name}</p>";
                                    html += $"</div>";
                                    html += $"<div class=\"d-none d-md-block\">";
                                        html += $"<p>{item.FullPath.Replace("\\", "\\\\")}</p>";
                                    html += $"</div>";
                                html += $"</div>";
                                html += $"<i class=\"bi-caret-up-fill\"></i>";
                            html += $"</button>";
                        html += $"</h2>";
                        html += $"<div id = \"{id}\" class=\"accordion-collapse collapse\" aria-labelledby=\"{collapse}\" data-bs-parent=\"#{accordion}\">";
                            html += $"<hr />";
                            html += $"<div class=\"accordion-body\">";
                                accordion = "accordion_" + Guid.NewGuid();
                                html += $"<div id = \"{accordion}\" >";
                                    var subLevel = ++level;
                                    foreach (var subdir in item.Subfolders.OrderBy(v => v.Name).Select((element, index) => new { Index = index, Element = element }).ToArray())
                                    {
                                        html += await RenderNazcaAccordionItem(subdir.Element, accordion, subLevel);
                                    }
                                html += $"</div>";
                                foreach (var video in item.Files.OrderBy(v => v.Title).Select((element, index) => new { Index = index, Element = element }).ToArray())
                                {
                                    html += $"<div class=\"d-flex justify-content-between\">";
                                        html += $"<a href=\"/Films/Details/Nazca?videoid={video.Element.ID}\" class=\"text-white link-underline link-underline-opacity-0 w-100 ps-2 py-1\" style=\"margin-left: {ps.ToString().Replace(',', '.')}%;\"> {video.Element.Title}</a>";
                                    html += $"</div>";
                                }
                                html += $"<span class=\"mb-1\" style=\"display: inline-block;\"></span>";
                            html += $"</div>";
                        html += $"</div>";
                    html += $"</div>";
                }
                else
                {
                    var subLevel = ++level;
                    foreach (var subdir in item.Subfolders.OrderBy(v => v.Name).Select((element, index) => new { Index = index, Element = element }).ToArray())
                    {
                        html += await RenderNazcaAccordionItem(subdir.Element, accordion, subLevel);
                    }

                    foreach (var video in item.Files.OrderBy(v => v.Title).Select((element, index) => new { Index = index, Element = element }).ToArray())
                    {
                        html += $"<div class=\"d-flex justify-content-between\">";
                        html += $"<a href=\"/Films/Details/Nazca?videoid={video.Element.ID}\" class=\"text-white link-underline link-underline-opacity-0 w-100 ps-2 py-1\" style=\"margin-left: {ps.ToString().Replace(',', '.')}%;\"> {video.Element.Title}</a>";
                        html += $"</div>";
                    }
                }
            }
            else if (item.Files.Count == 1)
            {
                var index = item.Files.FirstOrDefault()?.ID;
                html += $"<a href=\"/Films/Details?videoid={index}\" class=\"d-flex justify-content-between text-white link-underline link-underline-opacity-0 ps-2 pt-2\" style=\"margin-left: {(ps + 0.8).ToString().Replace(',', '.')}%;\">";
                    html += $"<div>";
                        html += $"<p>{item.Name}</p>";
                    html += $"</div>";
                    html += $"<div class=\"d-none d-md-block\">";
                        html += $"<p>{item.FullPath}</p>";
                    html += $"</div>";
                html += $"</a>";
            }

            return html;
        }
    }
}
