# Party Pictures

Party Pictures lets people quickly get pictures taken with their mobile phones
onto a large screen at an event somewhere.

It uses e-mail, the lowest common denominator communication technology
supported by every phone. People send their pictures to a special mailbox,
where they are instantly downloaded and shown on the screen.

(Requires .NET 4)

## Mail Downloader

The Mail Download application continuously monitors the mailbox of IMAP server
(for example, GMail) for new messages, and downloads any attachments it finds
to disk.

This application is designed to run in the background.

## Photo Viewer

The Photo Viewer monitors the download directory and displays any files it
finds there on the screen. The directory is watched live and new files are
picked up as soon as they are added.

This application takes up the entire screen. Use Alt-F4 to kill it ;).

Note that the only thing this application requires is photos in a directory.
If you can find any other tool to drop files in the directory, they will be
picked up and displayed as well.

Any text between square brackets is used as a caption for the image (the
Mail Downloader stores the sender's name in this field).

## Known Issues

  - Sometimes the Mailbox Watcher doesn't close properly and will have to be
    process-killed.
  - There's not a whole lot of configurability; the tool currently does exactly
    what I need it to do. You should hack the source if one of the following
    is too limited for you:
    - Both programs probably need to run on a single computer.
    - The time delay between picture changes is fixed.
    - They programs use a fixed directory.
    - The IMAP server needs to support SSL on the default port.
    - The viewer currently only watches for .jpg files.

## Libraries used

  - Andy Edinborough's AE.Net.Mail
  - Someone's grayscale pixel shader (forgot who)
