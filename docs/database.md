# WinWork Database Documentation

## Overview
WinWork uses a local SQLite database for persistent storage of all links, folders, notes, tags, and application settings. The database is managed via Entity Framework Core and is automatically created and migrated on first run.

## Database File
- **Location:** `linker.db` (in application data folder)
- **Type:** SQLite (serverless, file-based)

## Schema Summary

### Tables
- **Links**: Stores all links, folders, and notes
- **Tags**: Stores tag definitions and colors
- **LinkTags**: Many-to-many relationship between links and tags
- **AppSettings**: Stores application-level settings

---

## Table: Links
| Column         | Type      | Description                                 |
|---------------|-----------|---------------------------------------------|
| Id            | INTEGER   | Primary key, auto-increment                 |
| Name          | TEXT      | Display name                                |
| Url           | TEXT      | URL, file path, or application path         |
| Type          | INTEGER   | Enum: WebUrl, File, Folder, Application, Notes, etc. |
| ParentId      | INTEGER   | Foreign key to Links.Id (nullable)          |
| Description   | TEXT      | Optional description                        |
| Notes         | TEXT      | Notes content (for Notes type)              |
| CreatedAt     | DATETIME  | Creation timestamp                          |
| UpdatedAt     | DATETIME  | Last modified timestamp                     |

### Link Types
- **WebUrl**: Standard HTTP/HTTPS links
- **File**: Local file paths
- **Folder**: Folder containers (can have children)
- **Application**: Executable paths (with arguments)
- **Notes**: Freeform notes (no URL required)
- **System/AppStore**: Windows shell or store URIs

---

## Table: Tags
| Column   | Type    | Description                |
|----------|---------|----------------------------|
| Id       | INTEGER | Primary key, auto-increment|
| Name     | TEXT    | Tag name                   |
| Color    | TEXT    | Hex color code             |

---

## Table: LinkTags
| Column   | Type    | Description                |
|----------|---------|----------------------------|
| LinkId   | INTEGER | Foreign key to Links.Id    |
| TagId    | INTEGER | Foreign key to Tags.Id     |

---

## Table: AppSettings
| Column   | Type    | Description                |
|----------|---------|----------------------------|
| Key      | TEXT    | Setting key                |
| Value    | TEXT    | Setting value              |

---

## Relationships
- **Links** can have a parent (for folders/subfolders)
- **Links** can have multiple tags (via LinkTags)
- **Tags** can be assigned to multiple links
- **Notes** are stored as a special link type with content in the Notes column

---

## Migrations & Versioning
- All schema changes are managed via EF Core migrations
- On startup, the app applies any pending migrations automatically
- Database upgrades are non-destructive and preserve user data

---

## Example Entity Definitions (C#)
```csharp
public class Link {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public LinkType Type { get; set; }
    public int? ParentId { get; set; }
    public string Description { get; set; }
    public string Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<LinkTag> LinkTags { get; set; }
}

public class Tag {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Color { get; set; }
    public ICollection<LinkTag> LinkTags { get; set; }
}

public class LinkTag {
    public int LinkId { get; set; }
    public int TagId { get; set; }
    public Link Link { get; set; }
    public Tag Tag { get; set; }
}
```

---

## Notes
- **Notes type**: No URL required, only Name and Notes content
- **Folders**: Only Name required, can have children
- **All other types**: Require Name and URL
- **Tags**: Color-coded, many-to-many with links
- **AppSettings**: Used for user preferences, theme, etc.

---

## Troubleshooting
- If the database file is missing, it will be recreated automatically
- If migrations fail, check for schema conflicts or file permissions
- All data is stored locally; backup `linker.db` for safety
