using Librarr.Model;

namespace Librarr.Settings;

public class ProfileSettingsData
{
    public Profile AudiobookProfile { get; set; } = new()
    {
        Formats =
        [
            new Profile.Format { Name = "FLAC", Order = 1, Enabled = true },
            new Profile.Format { Name = "M4B", Order = 2, Enabled = true },
            new Profile.Format { Name = "MP3", Order = 3, Enabled = true },
        ]
    };

    public Profile EBookProfile { get; set; } = new()
    {
        Formats =
        [
            new Profile.Format { Name = "AZW3", Order = 1, Enabled = true },
            new Profile.Format { Name = "EPUB", Order = 2, Enabled = true },
            new Profile.Format { Name = "MOBI", Order = 3, Enabled = true },
        ]
    };
    
    public string Language { get; set; } = "ENG";

    public Profile GetProfile(LibraryFile.FileType fileType)
    {
        return fileType switch
        {
            LibraryFile.FileType.Audiobook => AudiobookProfile,
            LibraryFile.FileType.Ebook => EBookProfile,
            _ => throw new ArgumentOutOfRangeException(nameof(fileType), fileType, null)
        };
    }

    public class Profile
    {
        public required List<Format> Formats { get; set; }

        public class Format
        {
            public required string Name    { get; set; }
            public int Order   { get; set; }
            public bool   Enabled { get; set; }
        }
    }
}