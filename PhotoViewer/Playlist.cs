using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace PhotoViewer
{
    /// <summary>
    /// Playlist manager for pictures
    /// </summary>
    /// <remarks>
    /// New pictures can be added at any point; these will be picked (in order)
    /// until all "new" pictures have been picked, after which old pictures will
    /// be picked at random. The "new" queue can be made "old" at any point.
    /// 
    /// Playcounts will be equalized, no picture will be picked twice until all other
    /// pictures have been picked.
    /// </remarks>
    class Playlist
    {
        private readonly object lockRoot = new object();
        private readonly ConcurrentQueue<PlaylistEntry> head = new ConcurrentQueue<PlaylistEntry>();
        private readonly List<PlaylistEntry> recycle = new List<PlaylistEntry>();
        private readonly ConcurrentRandomQueue<PlaylistEntry> remainder;

        private bool lastWasOld;

        /// <summary>
        /// This event is raised when a new picture is added and the last picture picked
        /// was an old one.
        /// 
        /// This allows a listener to respond as soon as there is something new to
        /// show, instead of waiting for the polling schedule.
        /// </summary>
        public event Action FirstFreshPhoto = delegate {};

        public Playlist(Random random)
        {
            remainder  = new ConcurrentRandomQueue<PlaylistEntry>(random);
            lastWasOld = true;
        }

        public void Add(PlaylistEntry entry)
        {
            bool fresh;
            lock (lockRoot)
            {
                fresh = lastWasOld;
                lastWasOld = false; // Not entirely true but prevents a double-raise
                head.Enqueue(entry);
            }
            if (fresh) FirstFreshPhoto();
        }

        /// <summary>
        /// Add all pictures in the New queue to the remainder list
        /// </summary>
        public void ShuffleNew()
        {
            PlaylistEntry e;
            while (head.TryDequeue(out e))
            {
                remainder.Enqueue(e);
            }
        }

        public bool TryPick(out PlaylistEntry result)
        {
            lock (lockRoot)
            {
                var success = head.TryDequeue(out result);

                if (!success)
                {
                    success = remainder.TryDequeue(out result);
                    if (success) lastWasOld = true;
                }

                if (!success)
                {
                    // By definition, remainder is emtpy so recycle and try again
                    remainder.EnqueueRange(recycle);
                    recycle.Clear();

                    success = remainder.TryDequeue(out result);
                    lastWasOld = true;
                }

                if (success) recycle.Add(result);
                return success;
            }
        }
    }

    class PlaylistEntry
    {
        public PlaylistEntry(string filename, string caption)
        {
            Filename = filename;
            Caption  = caption;
        }

        public string Filename { get; private set; }
        public string Caption  { get; private set; }
    }
}
