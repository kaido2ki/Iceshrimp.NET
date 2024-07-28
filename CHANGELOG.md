## v2024.1-beta2.security3
This is a security hotfix release. It's identical to v2024.1-beta2.security2, except for the following security-related changes:

### Backend
- Updated dotNetRdf to `3.2.9-iceshrimp` (addressing a possible DoS attack vector)
- Limited the maximum HttpClient response size to 1MiB (up from 2GiB, addressing a possible DoS attack vector)
- Refactored DriveService to use stream processing for remote media (addressing a possible DoS attack vector)

### Attribution
This release was made possible by project contributors: Laura Hausmann

## v2024.1-beta2.security2
This is a security hotfix release. It's identical to v2024.1-beta2.security1, except for referencing an updated version of the `SixLabors.ImageSharp` dependency, fixing a Denial of Service vulnerability ([GHSA-63p8-c4ww-9cg7](https://github.com/advisories/GHSA-63p8-c4ww-9cg7)). Upgrading is strongly recommended for all server operators.

### Backend
- Updated SixLabors.ImageSharp to 3.1.5 (addressing [GHSA-63p8-c4ww-9cg7](https://github.com/advisories/GHSA-63p8-c4ww-9cg7))

### Attribution
This release was made possible by project contributors: Laura Hausmann

## v2024.1-beta2.security1
This is a security hotfix release. It's identical to v2024.1-beta2, except for referencing an updated version of the `System.Text.Json` dependency, fixing a Denial of Service vulnerability ([GHSA-hh2w-p6rv-4g7w](https://github.com/advisories/GHSA-hh2w-p6rv-4g7w)). Upgrading is strongly recommended for all server operators.

### Backend
- Updated System.Text.Json to 8.0.4 (addressing [GHSA-hh2w-p6rv-4g7w](https://github.com/advisories/GHSA-hh2w-p6rv-4g7w))

### Attribution
This release was made possible by project contributors: Laura Hausmann

## v2024.1-beta2
This release contains various features & bugfixes, including a security issue. Upgrading is strongly recommended for all server operators.

### Frontend
- Various leftover debug logging has been removed
- The MFM nodes `center`, `quote`, `hashtag`, `small` and `strike` are now rendered correctly
- Custom emoji are now rendered in a visually consistent way when compared to iceshrimp-js
- Non-image attachments are now rendered correctly
- Stacking issues with positioned elements have been fixed
- Notes in single note view now take up the entire width
- The emoji picker has been refactored for improved usability and stability
- Content warnings are now preserved when replying
- The menu button on notes is now functional, allowing for deleting, redrafting, and opening the original page
- The virtual scroller no longer loads infinitely when reaching the end
- The ability to search for notes has been implemented

### Backend
- Deleting an emoji now requires admin permissions
- The User-Agent header sent with outgoing HTTP requests is now standards compliant
- MediaCleanupTask now prints clearer log messages
- The Web API now allows for deleting of notes
- The Mastodon client API now returns the number of pending follow requests when verifying user credentials
- The Web API now allows for cloning of remote emoji
- The background task queue no longer tries to delete remote files from the storage backend
- The Web API now allow for importing of emoji packs

### Miscellaneous
- The code formatting rules have been updated
- The CI pipeline was overhauled and is now more performant and reliable
- The Dockerfile was overhauled and now builds faster & results in smaller images

### Attribution
This release was made possible by project contributors: Kopper, Laura Hausmann, Lilian & pancakes

## v2024.1-beta1
This is the first ever tagged release of Iceshrimp.NET, the successor to iceshrimp-js. While it is performant and stable, many features are still incomplete. Check the [README](README.md) for more details.

### Release notes
We've been very hard at work over the past (*checks notes*) 8 months! This release is a very big milestone, as all the "core" features of an AP-compatible social network have been implemented. To us, this means that we now consider this software "beta". This means that we'll start to push tagged releases (somewhat) regularly again. We're aiming for a stable release (with feature-parity) by the end of 2024. We really hope you enjoy!

### Highlights
- All-new backend & frontend
- So many more more changes than we could possibly list here, explore for yourself!

### Attribution
This release was made possible by project contributors: Laura Hausmann, Lilian, Thomas May & pancakes
