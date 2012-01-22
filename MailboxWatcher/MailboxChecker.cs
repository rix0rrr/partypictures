using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AE.Net.Mail;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace MailboxWatcher
{
    /// <summary>
    /// Background mailbox checker and attachment downloader
    /// </summary>
    class MailboxChecker : IDisposable
    {
        private readonly object lockRoot = new object();
        private readonly ImapClient imap;
        private readonly string path;
        private readonly Thread thread;

        public MailboxChecker(string path, string server, string username, string password)
        {
            this.path = path;

            imap = new ImapClient(server, username, password, ImapClient.AuthMethods.Login, 993, true);
            thread = new Thread(BackgroundScan) { IsBackground = true };
            thread.Start();

        }

        private void BackgroundScan(object state)
        {
            try 
            {
                ScanAllMessages();

                imap.NewMessage += (sender, e) => {
                    var msg = imap.GetMessage(e.MessageCount - 1);
                    HandleMessage(msg);
                };
            }
            catch (ThreadAbortException)
            {
                // ok
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void ScanAllMessages()
        {
            imap.SelectMailbox("INBOX");
            var uids = imap.Search(SearchCondition.Unseen());
            if (uids.Count() == 0) return;

            Debug.WriteLine("Found {0} new messages", uids.Count());
            foreach (var uid in uids)
            {
                Debug.WriteLine(string.Format("Fetching message {0}", uid));
                var msg = imap.GetMessage(uid);
                Debug.WriteLine("Done.");
                HandleMessage(msg);
            }
        }


        private void HandleMessage(MailMessage message)
        {
            if (message == null) return;

            try
            {
                lock (lockRoot) // One message at a time
                {
                    Debug.WriteLine("Found a message with {1} attachments: {0}", message.Subject, message.Attachments.Count);
                    foreach (var att in message.Attachments.Where(_ => _.IsAttachment))
                    {
                        var ctr = 0;

                        string fullpath;
                        do 
                        {
                            ctr++;
                            string filename =
                                "[" + message.From.DisplayName + "] "
                                + Path.GetFileNameWithoutExtension(att.Filename)
                                + (ctr > 1 ? " (" + ctr + ")" : "")
                                + Path.GetExtension(att.Filename);
                            fullpath = Path.Combine(path, filename);
                        }
                        while (File.Exists(fullpath));
        
                        Debug.WriteLine("Saving {0} to {1}", att.Filename, fullpath);
                        att.Save(fullpath);
                        Debug.WriteLine("Done.");
                    }

                    imap.SetFlags(message.Flags | Flags.Seen, message);
                    imap.DeleteMessage(message);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public void Dispose()
        {
            imap.Disconnect();
        }
    }
}
