# jellyfin-plugin-calibre
[![GitHub release](https://img.shields.io/github/release/AshleyDeo/jellyfin-plugin-calibre.svg?style=flat-square&color=00a6ff)](https://github.com/AshleyDeo/jellyfin-plugin-calibre/releases/latest) [![Github downloads](https://img.shields.io/github/downloads/AshleyDeo/jellyfin-plugin-calibre/total.svg?style=flat-square&color=50ab50)](https://github.com/AshleyDeo/jellyfin-plugin-calibre/releases) [![License: GPL v3](https://img.shields.io/badge/License-GPLv3-9b61ff.svg?style=flat-square&)](https://raw.githubusercontent.com/AshleyDeo/jellyfin-plugin-calibre/master/LICENSE)

 This [Jellyfin](https://jellyfin.org/) plugin provides metadata from user's [Calibre](https://calibre-ebook.com/) library.
 <br>
 Based on the [Bookshelf plugin](https://github.com/jellyfin/jellyfin-plugin-bookshelf)
 <br>
 * Adds metadata from Calibre custom columns
 * Adds Google Books, ComicVine, and ISBN provider id

## How to use

1. Add the folder where your Calibre library is located to Jellyfin
1. On **Admin Dashboard**, go to **My Plugins**. It will show your folders/libraries with dropdowns. 
2. Add user custom columns seperated by semicolon ;<br>
 Example: `myColumn01;myColumn02;myColumn03`
3. Save plugin configuration
4. The library must be refreshed

### Rules
* DO NOT use columns with code in template. It breaks the plugin.
* Only add user custom columns
* ``default`` only works for Genres, Tags, and Community Rating. It uses the original Bookshelf settings to get the metadata.
* Ratings only take 1 value

## Installation

### From Repository
1. Add [link](https://raw.githubusercontent.com/AshleyDeo/jellyfin-plugin-calibre/main/manifest.json) to Plugin Repository
```
https://raw.githubusercontent.com/AshleyDeo/jellyfin-plugin-calibre/main/manifest.json
```
2. Install the "Calibre" plugin from the catalog

### From .zip file
1. Download the .zip file from release page.
2. Extract it and place the .dll file in a folder called ```plugins/Calibre``` under  the program data directory or inside the portable install directory.
3. Restart Jellyfin.
