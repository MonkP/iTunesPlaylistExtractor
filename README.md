# iTunes Playlist Extractor
### A working tool with core features completed; MTP sync is still on the roadmap.
## Instructions
I have a iTunes library with about 6,000 local tracks on my PC, and the app I used to sync iTunes music to my Android phone (double twists) works poorly recently. 
I looked for alternatives but found nothing. Besides I don't like the way double twists arranges music files on the phone. So I'm gonna to make my own tool to do the job.

## Features & User Manual

1. Start working by choosing your local `iTunes Music Library.xml`. The library is parsed in the background, so the UI stays responsive even with a large library.
2. Playlists are displayed in a listview, showing names, music counts, and parent-sibling relations. The nesting of playlist folders is represented by two-space indentation per level, and children always appear right below their parent folder. The iTunes master library and built-in system playlists (Music, Movies, Podcasts, etc.) are hidden. The item count of a folder is the deduplicated total of the tracks in all playlists nested inside it.
3. Choose the root path of your music library.
4. Check the playlists you want to extract in the listview. Checking a playlist folder extracts it as one playlist containing the deduplicated tracks of all nested playlists.
5. Choose a target path that you want to extract playlists to.
6. By clicking the "Extract" button, the music files in the chosen playlists will be extracted to the target path, with the exact same relative paths to the chosen root path.
    For example, the root path is `F:/musics`, one of extracted files is `F:/music/Jay Chou/叶惠美/晴天.mp3`, the target path is `G:/PickedMusic`. Then the file will be copied to `G:/PickedMusic/Jay Chou/叶惠美/晴天.mp3`
    All files from the chosen playlists are collected and deduplicated before copying. Whether existing music files in the target path are overwritten depends on the **OverWrite** checkbox: when it is checked, they are overwritten; when it is unchecked, they are kept as they are and not copied again. `.m3u` files are always rewritten. The failure of a single file never aborts the extraction; it is only logged and skipped.
7. The playlists themselves will be extracted as `.m3u` files, and the `.m3u` files will be created in the target path. Each of the `.m3u` files contains the music items of its original, but the file paths to the music items are relative.
    An `.m3u` file is named with the full path from the top-level folder down to the checked item, joined by `-`. Every parent level is compressed to at most its first 6 characters while the checked item keeps its full name, and characters invalid in file names are replaced with `_`. For example, checking `周杰伦全集` inside folder `周杰伦` produces `周杰伦-周杰伦全集.m3u`.
8. After the extraction, the target path is checked for files that are not included in the playlists chosen this time, e.g. an old `.m3u` of a playlist not checked this time, or music files not covered by any checked playlist. If any are found, you are asked "Target Path contains files that not included in playlists choosen this time, would you like to delete them?". Choosing Yes deletes them (the deleted count is appended to the summary), choosing No keeps them.
9. You can move the whole target path content to your device and import the `m3u` playlists to the player app you like.
10. Your choices are remembered. The library file, root path, target path, the OverWrite option and the checked playlists of the last extraction are saved to `preference.json` in the application directory when you click "Extract", and restored automatically on the next start.
11. Progress and summary are shown in the status bar, e.g. `Copying (5/30)` and `Creating m3u (1/5)` while extracting. When the extraction finishes, an alert shows the summary (selected/created playlist counts and track counts). If anything went wrong, the log file of the day opens automatically after you close the alert.
12. Logs are written to `log/yyyy-MM-dd.log` under the application directory. The minimum recorded level (Fatal/Error/Warn/Info/Debug) is set by the `LogLevel` field in `preference.json`, defaulting to `Error`.

## AI Programming Announcement

* The idea--me
* Feasibility Studies--me with DeepSeek chat mode
* Initialize the project and arranging the main form--me
* Feature discription and critical how-to decisions--me
* All the messy stuff--QoderCN(Quest mode)
* Code Review--me and another QoderCN session
* Manual testing & debugging--me
* Documentation--me and QoderCN(Quest mode)

## Further more

1. Add MTP abilities so you can choose a path on your Android device as target path.
2. With MTP, check the existing `.m3u` and music files in target path, then perform a proper synchronization rather than adding only.

## Acknowledgements

This project uses [iTunesLibraryParser](https://github.com/asciamanna/iTunesLibraryParser), Copyright (c) 2018 Anthony Sciamanna, licensed under the [MIT License](iTunesLibraryParser-master/LICENSE):

> Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
>
> The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
>
> THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
