# jellyfin-plugin-calibre
 This [Jellyfin](https://jellyfin.org/) plugin provides metadata from user's [Calibre](https://calibre-ebook.com/) library.
 <br>
 Based on the [Bookshelf plugin](https://github.com/jellyfin/jellyfin-plugin-bookshelf)
 <br>
 * Adds all authors for multi-author books
 * Adds Google Books provider id
 * Adds metadata from Calibre custom columns

## How to use

1. Add user custom columns seperated by semicolon ;<br>
 Example: `myColumn01;myColumn02;myColumn03`
2. Save plugin configuration
3. The library must be refreshed

### Rules
* DO NOT use columns with code in template. It breaks the plugin.
* Only add user custom columns
* ``default`` only works for Genres, Tags, and Community Rating. It uses the original Bookshelf settings to get the metadata.
* Ratings only take 1 value

## Installation

### From .zip file
1. Download the .zip file from release page.
2. Extract it and place the .dll file in a folder called ```plugins/Calibre``` under  the program data directory or inside the portable install directory.
3. Restart Jellyfin.

### From Repository
1. Add #linkNotAddedYet# to Plugin Repository
2. Install the "Calibre" plugin from the catalog
