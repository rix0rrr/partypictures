using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace PhotoViewer
{
    /// <summary>
    /// Watches a directory for new files and adds them to the playlist
    /// </summary>
    /// <remarks>
    /// File captions are taken from text between square brackets in the
    /// filename.
    /// </remarks>
    class DirectoryScanner
    {
        const string FilePattern = "*.jpg";
        private static readonly Regex CaptionPattern = new Regex(@"\[(.*)\]");

        private readonly string   path;
        private readonly Playlist playlist;
        private readonly FileSystemWatcher watcher;

        private SortedSet<string> knownFiles = new SortedSet<string>();

        public DirectoryScanner(string path, Playlist playlist)
        {
            this.path = path;
            this.playlist = playlist;

            Scan();
            playlist.ShuffleNew();

            watcher = new FileSystemWatcher(Path.GetFullPath(path), FilePattern);
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += (_, e) => Scan();
            watcher.Created += (_, e) => Scan();
            watcher.EnableRaisingEvents = true;
        }

        private void Scan()
        {
            foreach (var file in Directory.EnumerateFiles(path, FilePattern))
            {
                FoundFile(file);
            }
        }

        private void FoundFile(string file)
        {
            var canon = Path.GetFullPath(file);
            if (knownFiles.Contains(canon)) return;

            playlist.Add(new PlaylistEntry(canon, CaptionFromFilename(canon)));
            knownFiles.Add(canon);
        }

        private string CaptionFromFilename(string file)
        {
            var name = Path.GetFileNameWithoutExtension(file);

            var m = CaptionPattern.Match(name);
            if (m.Success) return m.Groups[1].ToString();
            return "";
        }
    }
}
