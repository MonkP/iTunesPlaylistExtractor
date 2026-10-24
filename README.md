# iTunes Playlist Extractor
### A working tool with core features completed, including MTP sync to Android devices.
## Instructions
I have a iTunes library with about 6,000 local tracks on my PC, and the app I used to sync iTunes music to my Android phone (double twists) works poorly recently. 
I looked for alternatives but found nothing. Besides I don't like the way double twists arranges music files on the phone. So I'm gonna to make my own tool to do the job.

## Features & User Manual

1. Start working by choosing your local `iTunes Music Library.xml`. The library is parsed in the background, so the UI stays responsive even with a large library.
2. Playlists are displayed in a listview, showing names, music counts, and parent-sibling relations. The nesting of playlist folders is represented by two-space indentation per level, and children always appear right below their parent folder. The iTunes master library and built-in system playlists (Music, Movies, Podcasts, etc.) are hidden. The item count of a folder is the deduplicated total of the tracks in all playlists nested inside it.
3. Choose the root path of your music library.
4. Check the playlists you want to extract in the listview. Checking a playlist folder extracts it as one playlist containing the deduplicated tracks of all nested playlists.
5. Choose a target path that you want to extract playlists to. Use **Choose Local Path** for a folder on this PC, or **Choose Path on Device** to browse a connected Android device (via MTP) in a device picker and select a folder on it. Both buttons write the chosen path into the same target path box, and the remembered source decides which target is used when you click Extract. While the box holds a device path it is locked against manual editing. Keep the device connected, unlocked and in file-transfer (MTP) mode.
6. By clicking the "Extract" button, the music files in the chosen playlists will be extracted to the target path, with the exact same relative paths to the chosen root path.
    For example, the root path is `F:/musics`, one of extracted files is `F:/musics/Jay Chou/叶惠美/晴天.mp3`, the target path is `G:/PickedMusic`. Then the file will be copied to `G:/PickedMusic/Jay Chou/叶惠美/晴天.mp3`
    All files from the chosen playlists are collected and deduplicated before copying. A track is skipped (and counted in the summary as `Skipped`) when it has no local file location, points to a non-local resource, or lies outside the chosen root path. Whether existing music files in the target path are overwritten depends on the **OverWrite** checkbox: when it is checked, they are overwritten; when it is unchecked, they are kept as they are and not copied again. For a device target, a file is considered up to date when a file with the same relative path and the same size already exists, so re-extracting only transfers what changed. `.m3u` files are always rewritten. The failure of a single file never aborts the extraction; it is only logged and the next file is tried. However, once the number of failures reaches the larger of 10% of the files to sync and 10, the remaining files are skipped and the abort is reported in the summary (the two thresholds are stored in `preference.json` as `MaxErrorRatio`/`MaxErrorCount` and have no UI option).
7. The playlists themselves will be extracted as `.m3u` files, and the `.m3u` files will be created in the target path. Each of the `.m3u` files contains the music items of its original, but the file paths to the music items are relative.
    An `.m3u` file is named with the full path from the top-level folder down to the checked item, joined by `-`. Every parent level is compressed to at most its first 6 characters while the checked item keeps its full name, and characters invalid in file names are replaced with `_`. For example, checking `周杰伦全集` inside folder `周杰伦` produces `周杰伦-周杰伦全集.m3u`.
8. After the extraction, the target path is checked for files that are not included in the playlists chosen this time, e.g. an old `.m3u` of a playlist not checked this time, or music files not covered by any checked playlist. Only files whose extension matches one of the files being synced this time are considered, so unrelated files are never touched. If any are found, you are asked "Target Path contains files that not included in playlists choosen this time, would you like to delete them?". Choosing Yes deletes them (the deleted count is appended to the summary), choosing No keeps them.
9. You can move the whole target path content to your device (If you've choosen a local path), and import the `m3u` playlists to the player app you like.
10. Your choices are remembered. The library file, root path, target path (including whether it is a local path or a device path, and which device), the OverWrite option and the checked playlists of the last extraction are saved to `preference.json` in the application directory when you click "Extract", and restored automatically on the next start.
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

MTP support (choosing a path on your Android device as the target and syncing playlists to it) has been implemented. No further plans at the moment.

## Acknowledgements

This project uses [iTunesLibraryParser](https://github.com/asciamanna/iTunesLibraryParser), Copyright (c) 2018 Anthony Sciamanna, licensed under the [MIT License](iTunesLibraryParser-master/LICENSE):

> Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
>
> The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
>
> THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

This project also uses [MediaDevices](https://github.com/Bassman2/MediaDevices), Copyright (c) 2018 Ralf Beckers, licensed under the [MIT License](https://github.com/Bassman2/MediaDevices/blob/master/LICENSE):
> MIT License

> Copyright (c) 2017 - 2023 Ralf Beckers

> Permission is hereby granted, free of charge, to any person obtaining a copy
> of this software and associated documentation files (the "Software"), to deal
> in the Software without restriction, including without limitation the rights
> to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
> copies of the Software, and to permit persons to whom the Software is
> furnished to do so, subject to the following conditions:
> 
> The above copyright notice and this permission notice shall be included in all
> copies or substantial portions of the Software.
> 
> THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
> IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
> FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
> AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
> LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
> OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
> SOFTWARE.
