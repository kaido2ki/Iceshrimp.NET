## v2024.1-beta3.patch1
This is a hotfix release. It's identical to v2024.1-beta3, except for a bunch of fixed frontend crashes. Upgrading is strongly recommended for all server operators.

### Blazor frontend
- Empty timelines no longer cause a frontend crash
- Unauthenticated access to some pages no longer causes a frontend crash
- The overflow behavior of several list UI elements was fixed
- An issue that caused UI elements to not fade out properly when a dialog was open has been resolved
- Code blocks now scroll horizontally
- Opening the emoji picker no longer scrolls the page to the top
- A button to copy the link to a post to the clipboard has been added

### Attribution
This release was made possible by project contributors: Lilian

## v2024.1-beta3
This release contains lots of new features & bugfixes. Upgrading is recommended for all server operators.

### Highlights
- Significant frontend UX improvements
- The backend can now load plugin assemblies. Plugins can inject new endpoints, react to events, instantiate cron tasks & store configuration data.
- There is now an easy migration assistant for existing users of Iceshrimp-JS.
- The image processing pipeline is now modular & highly configurable. The defaults match the previous hardcoded behavior, minus a couple bugs.
- The request / job id is now printed alongside every log message
- When running in an interactive terminal, logs are now color-formatted for easier readability
- WebFinger reverse discovery is now supported, fixing federation with older Pixelfed instances & other miscellaneous AP implementations
- Error responses are now returned as HTML, XML or JSON depending on the Accept header of the request
- Database connection multiplexing is now enabled by default, allowing for massive bursts in traffic without compromising stability or degrading performance beyond expectations.
- The partial cluster mode implementation has been removed to minimize development overhead. It may be added back once vertical scaling proves to not be enough for larger instances.

### Blazor frontend
- The MFM plain node is now supported
- The sidebar layout has been reworked
- A bug that prevented you from renoting of your own notes has been fixed
- Notes which have been renoted are now indicated
- Notes that cannot be renoted are now indicated more clearly
- Note state is now updated across the application on changes
- A page for managing follow requests has been added
- Follow request related notifications are now supported
- A follow button has been added
- Erroneous compile time warnings related to ILLink have been fixed
- The sidebar has been reworked in the mobile view
- A bug that made it impossible to interact with renotes has been resolved
- Extraneous debug logging has been removed
- Basic logging has been added
- A version information page has been added
- Most menus are now self-closing
- Performance has been improved significantly by only re-rendering notes when required
- The reply tree now updates when new replies are added
- The scroll state in single note view is now preserved while navigating back and forth
- Deleted notes are now removed from timeline and single note view
- The timestamp display on notes has been fixed & now live-updates
- Renote visibility is now selectable
- The single note view now loads much faster via concurrent network requests
- The styling of MFM links and URLs has been fixed
- An Error UI when an unhandled exception occurs has been introduced, displaying the stacktrace and allowing log download
- An in memory logger has been added
- A profile editing page has been added
- The send post button now reflects request state
- The top of the page now displays a sticky indicator of the current page
- Filters can now be configured in settings and are enforced
- Threads can now be muted
- A button to accept a follow request & follow the sender back has been added

### Backend
- The emoji import endpoints are now named consistently
- Custom emoji in user display names & bios now federate correctly
- Local users can now be resolved by their fully qualified names
- The emoji reaction detection regex now detects unicode emoji more accurately
- Sporadic GetOrCreateSystemUserAndKeypairAsync failures no longer occur
- SystemUserService now has significantly improved logging
- A new endpoint to refetch a note & its relations has been added
- The rate limiting rules have been adjusted
- Endpoints for managing follow requests have been added
- The clone emoji endpoint now functions correctly
- All MVC controllers have been refactored for better readability of endpoint attributes
- Preloaded JSON-LD contexts are now embedded into the compiled .dll instead of being shipped alongside the binaries
- The nodeinfo response now includes release codename and edition
- Media file names are now preserved for S3 & local storage
- User profile properties that only concern local users have been moved into a user settings table
- Several unused user profile properties have been removed
- Validation errors are now returned in a more sensible format
- Endpoints concerning the user profile of the authenticated user have been added
- The application no longer crashes on startup when running on IIS on Windows Server
- The deliver queue now handles client errors (4xx) more gracefully
- HTTP signatures now validate correctly on systems that use CRLF line endings
- The user resolver now correctly updates the last fetched date before proceeding
- The user settings attribute "auto accept followed" and "always mark sensitive" are now respected
- Extraneous user table columns have been removed
- Constraint/index differences between migrated and .net-native instances have been resolved
- Endpoints concerning management of filters have been added
- The entire note is now marked as sensitive if any attachments are tagged as such, improving federation compatibility
- Streaming connection handlers no longer randomly crash due to prematurely disposed DbContext instances
- The streaming service now supports the note deleted event
- Endpoints related to muted threads have been added
- Hashtags contained in incoming notes are now fixed up to be hashtags instead of links
- Location and birthday profile fields now federate correctly
- RazorRuntimeCompilation has been removed, as it doesn't behave correctly with CSS isolation
- HTTP timeout exceptions no longer cause an excessively long stack trace to be logged
- Animated PNG images are now being processed correctly
- Added emoji now bypass the image processing pipeline
- The ImageSharp dependency was swapped for an in-house fork that supports detection of animated images properly
- The queue dashboard now has an overview page with live updates
- A race condition in the queue system causing transient queue stalls has been fixed
- The queue dashboard now uses local time instead of UTC when rendering timestamps
- Unordered ASCollection(Page) objects now serialize & deserialize correctly
- The light theme has been removed from the WaterCSS derivative stylesheets, allowing for significantly smaller file sizes & a less visually broken experience for light theme users. It may be added again in the future, after the display issues have been resolved.
- The queue overview page now uses consistent ordering of queue names
- Poll expiry jobs for polls that expire more than a year into the future aren't queued
- The queue & application shutdown process has been significantly improved
- The ImageSharp maximum memory allocation is now computed from the resolution limits instead of being hardcoded
- The queue system algorithm is now explained in detail in code comments
- Drive deduplication handling was improved
- The OpenGraph embed preview no longer shows "0 Attachments" if there are no attachments
- HTTP responses to outgoing requests are now limited to 1MiB (except for drive)
- Remote media is now being processed using safe stream processing with enforced allocation limits
- The default configuration is now embedded into the .dll, instead of being bundled alongside
- MFM italic and bold nodes are now also parsed when using their alternative representation
- Exception logging in DriveService has been improved
- HEIC images are now allowed for inline viewing
- A federation issue with a GoToSocial beta build was resolved
- Requests from suspended users are now rejected on the middleware layer
- Activities from suspended remote users are now rejected early
- The preloaded JSON-LD contexts have been updated
- Streaming connections now store their data in WriteLockingHashSets, significantly improving lookup performance
- The WebFinger algorithm now reuses responses more efficiently, cutting down on unnecessary requests to remote instances
- Local usernames are now validated more strictly
- The WebFinger & host-meta serializers now support the XRD/XML host-meta format for both serialization and deserialization, fixing federation with Hubzilla
- Hubzilla-style hashtags are now fixed up automatically
- WebFinger responses now contain the user aliases
- Host-meta responses are now returned with proper content negotiation
- A possibly unbounded UserResolver recursion was fixed
- The systemd logger now supports colored output, though journalctl strips color by default
- The console logger now supports logging timestamps. This feature can be enabled by setting the `LOG_TIMESTAMPS=1` environment variable.
- Untrusted XML input is now deserialized using a XmlReader instead of a XmlSerializer
- The public preview lockdown message has been improved to be easier to understand by regular users
- There are now unit tests for the unicode emoji detection code
- Invalid misskey heart emoji reactions now get canonicalized to their correct representation
- The razor error page now uses the same request ID in use in every other component
- Federation with Friendica now works correctly
- A bug causing sporadic "unique constraint violation error" logs has been resolved
- ASLike activities with `content` property instead of `_misskey_reaction` are now being ingested correctly
- Quotes are now being tagged as object links (FEP-e232)
- Note updates with nonsensical timestamps are now discarded automatically
- The deliver queue no longer caches `the userPrivateKey` property, as a cache lookup is equally expensive as a lookup in the `user_keypair` table, and we're lowering the chance of a cache hit by duplicating this data in memory.
- Files larger than 28MB no longer fail to upload
- An endpoint to get drive files by their hash has been added, only searching through the requesting users' files to make sure no metadata is being leaked.
- The MFM quote node type is now supported
- The note replies collection is now being exposed over ActivityPub
- The canonical WebFinger address of local users is now exposed over ActivityPub (FEP-2c59)
- The JSON-LD roundtrip unit test now validates that deserialization works as well
- Blurhash performance & memory efficiency has been significantly improved for both LibVips and ImageSharp

### Razor (public preview, queue dashboard, etc.)
- Code blocks that are wider than the viewport can now be scrolled through horizontally

### Mastodon client API
- Inaccessible quotes & replies now carry a lock indicator even when the note has no text, and are rendered more consistently
- Feature flags are now exposed as toggles on the OAuth authorization screen
- The client_credentials grant type is now supported
- Threads can now be muted
- The user setting hideInaccessible is now respected in streaming connections
- Custom emoji no longer render with double colons on either side in some clients
- The OAuth authorization screen now allows 1-click login for users that are already authenticated in the blazor frontend
- The user statuses endpoint now filters unresolved replies/renotes when exclude_replies or exclude_reblogs are set
- Clients that support this are now informed that the server supports both polls and media in the same post
- Editing filter keywords no longer results in API exceptions

### Miscellaneous
- The dockerfiles & docker build scripts have been updated
- The CI workflows have been updated
- A global.json file has been added to configure the dotnet SDK version in a more precise manner
- A Makefile has been added for easier build and deployment
- Source Link now works correctly for docker builds, allowing the resulting application to know which git commit it has been built from
- Docker & make publish builds now use deterministic source paths
- Common build options were extracted into Directory.Build.props
- The dotnet environment information is now printed in CI runs, allowing for easier auditing of security patch statuses
- The README has been updated
- All dependencies have been updated, with some manual overrides for transitive dependencies to patch vulnerabilities
- Compile warnings are now being treated as errors, with limited exceptions
- Transitive dependencies are now being audited for vulnerabilities as well
- Docker builds based on musl no longer result in glibc binaries
- FEDERATION.md (FEP-67ff) has been added to the project root
- 

### Attribution
This release was made possible by project contributors: Kopper, Laura Hausmann, Lilian, pancakes & zotan

## v2024.1-beta2.security3
This is a security hotfix release. It's identical to v2024.1-beta2.security2, except for the security mitigations listed below. Upgrading is strongly recommended for all server operators.

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
