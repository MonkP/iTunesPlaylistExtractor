# iTunes Playlist Extractor
### This project is current under construction.
## Instructions
I have a iTunes library with about 6,000 local music files on my PC, and the app I used to sync iTunes music to my Android phone (double twists) works poorly recently. 
I looked for alternatives but found nothing. Besides I don't like the way double twists arranges music files on the phone. So I'm gonna to make my own tool to do the job.

## Goal Features

1. Start woking by choose your local `iTunes Music Library.xml`.
2. Display playlists in a listview, showing names, music counts, and parent-sibling relations.
3. Choose the root path of your music library.
4. Choose the playlists you want to extrat in the listview.
5. Choose a target path that you want to extrat playlists to.
6. By clicking the "Extract" button, the music files in the choosen playlists will be extracted to the target path, with the exact same reletive pathes to the choosen root path.
    For example, the root path is `F:/musics`, one of extracted files is `F:/music/Jay Chou/叶惠美/晴天.mp3`, the target path is `G:/PickedMusic`. Then the file will be copyed to `G:/PickedMusic/Jay Chou/叶惠美/晴天.mp3`
7. The playlists themselfs will be extracted as `.m3u` files with their names, and the `.m3u` files will be created in the target path. Each of the `.m3u` files contians the music items of their originals, but the file path to the music items is relative. 
8. You can move the whole target path content to your device and import the `m3u` playlists to the player app you like.

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
