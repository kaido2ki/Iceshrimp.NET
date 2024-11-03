## v2024.1-beta4
This release contains lots of new features & bug fixes, including security fixes. Upgrading is strongly recommended for all server operators.

### Release notes
This release contains a **breaking change** regarding the configuration file. If you have configured a *natural duration* using the units for \[w\]eeks, \[m\]onths, or \[y\]ears, please update your configuration file to use one of \[s\]econds, \[m\]inutes, \[h\]ours, and \[d\]ays. This change was necessary to accommodate the newly added minute unit.

Furthermore, this release contains a migration that may take a while, as it goes through every note in the database in order to migrate to a new thread schema required for reply backfilling.

### Highlights
- Akkoma clients are now supported, including Akko-FE
- Note reply backfilling is now available as an opt-in experimental feature
- Index redirects for unauthenticated users are now configurable
- Incoming, outgoing, local-local & remote-remote account migrations are now supported
- Inbox jobs are now retried with exponential backoff
- Connecting to relays is now supported
- Reject & rewrite policies are now supported & can be arbitrarily extended via plugins
- Full text search now also searches for alt text matches
- Basic moderation actions are now supported
- A basic admin dashboard has been added
- Commands for fixing up media & pruning unreferenced files have been added
- The frontend now shows significantly more note details
- The frontend layout & stylesheet has been significantly refined
- The follow list can now be imported & exported

### Blazor frontend
- Version information is now displayed correctly
- The .NET Runtime version is now shown on the about page
- Note footer buttons now have correct accessibility labeling
- Notifications have received a visual overhaul
- Unsupported notification details are now displayed
- Bites notifications are now rendered correctly
- Buttons to bite users and notes have been added
- Note search now supports state reconstruction
- Links now open in a new tab
- Initial loads for the single note view have been reworked
- Reply count is now shown next to the reply button
- Replies are now shown inline on the timeline
- Replies to inaccessible notes are now marked with a lock
- The login page now redirects to the previous page after successful authentication
- Erroneous "note not found" messages in the single note view have been resolved
- Long notes now get truncated correctly
- Accessibility issues with the compose dialog have been resolved
- The main layout now carries accessibility landmarks
- Profile images on notes are now indicated as being links
- Various bits of missing alt text have been added
- User profiles now show the profile banner, if set
- Verified, birthday & location fields now have appropriate icons

### Backend
- The content root path is now set to the assembly directory instead of the working directory
- Additional domains to permit can now be added in the configuration file
- Reply notifications are no longer generated for remote users
- User creations with database conflicts now fail early and with a better error message
- Paginated collections are now handled correctly
- Raw JSON-LD value types are now deserialized correctly
- Dead instances are no longer erroneously marked as responsive
- The program now exits when started with --migrate & no pending migrations
- Several long-running tasks now consume less memory due to improved database abstractions
- Files larger than 128MB can now be uploaded
- Non-image attachments no longer have leading dashes erroneously added to their filenames
- Drive files can now be deleted
- Links converted from HTML now get shortened if the url and text components match
- The local-only flag is now enforced for renotes & replies of local-only notes
- Invalid accept activities now have improved error logging
- Mention in parentheses are now parsed correctly
- The media cleanup task no longer causes database query warnings
- Newlines surrounding code blocks are now handled correctly
- Code blocks are now serialized correctly
- Erroneous job timeouts are no longer logged
- Job timeouts now log improved error messages
- Queue exceptions are no longer logged twice
- The prune-designer-cs-files helper script has been relicensed under MIT
- Inbox queue logs have been improved
- Creating local follow relationships no longer cause errors related to instance stats
- Delayed jobs can now be abandoned in the queue dashboard
- Renotes & quotes mentioning muted/blocked users now get filtered
- System users can no longer be followed
- Reply/renote accessibility is now indicated correctly in Web API responses
- Zero-durations in the configuration file now get treated the same regardless of their suffix
- Media cleanup can now be triggered manually
- Punycode hosts are now represented in lowercase everywhere
- Deep threads no longer cause API errors
- Emoji can now be marked as sensitive
- Erroneous inbox job failures for activities referencing deleted notes have been resolved
- System users can no longer log in or create notes
- Avatar & banner updates now set the denormalized URLs to the AccessUrl instead of the regular Url
- Files served by /files are now returned as inline attachments
- Endpoints to get all blocked/allowed instances have been added
- Log messages related to jobs that were queued for more than 10 seconds have been improved
- The background-task queue timeout has been increased to accommodate longer-running tasks
- The inbox queue timeout has been increased to accommodate longer-running jobs
- Erroneous voter counts for polls from instances that don't return a voter count value have been resolved
- Drive file expiry no longer leaves orphaned file versions in the storage backend
- MFM fn nodes now get parsed correctly
- Content warnings can now be searched for explicitly using the cw: search filter
- The replies collection is now exposed for local notes
- A bug in the drive file cleanup job related to locally stored files has been resolved
- The job queue now supports a mutex field to prevent the same job from being queued by multiple threads
- Negative voter counts are now rejected
- It's now possible to bite users, posts & other bites
- InboxValidationMiddleware error handling has been improved
- A typo causing confusing log messages in ActivityHandlerService has been fixed
- UserResolver has been fully reworked, deduplicating significant amounts of code & greatly limiting attack surface, as well as improving consistency & performance
- Endpoints for listing note likes, renotes & quotes have been added
- Web API responses now use RestPagination instead of LinkPagination
- Stripped reply data is now returned for the note ascendants & descendants Web API endpoints
- The request trace identifier is now returned as a header even when no errors have occurred
- The WebFinger JSON-LD context definition is now preloaded
- The natural duration configuration parser has been reworked to support seconds & minutes. Support for weeks, months & years has been removed.
- Lists using stars as item indicators no longer get mis-parsed by libmfm
- HTTP/2 is now preferred for outgoing connections
- The StreamingService render-only-once mutex implementation has been fixed
- DriveController is no longer serving files with possibly invalid extensions
- The thread mute endpoints no longer have incorrect rate limits
- A bug causing some followers-only renote activities to be registered as specified has been fixed
- Stricter guard clauses have been added to some federation-related methods
- ActivityPub URIs are now enforced to be https everywhere
- More efficient time & duration is now being used where applicable
- An edge case related to local mentions in profile fields & bios has been resolved
- Followers can now be removed via a new endpoint

### Razor (public preview, admin panel, queue dashboard, etc.)
- Basic user page public preview has been added
- Razor pages now carry a footer with login, instance & version information
- The RestrictedNoMedia public preview mode is now enforced
- Avatars are now replaced with identicons when public preview mode is set to RestrictedNoMedia
- Public hashtag preview now displays a placeholder instead of loading the blazor frontend
- When public preview is disabled, a better error message is now shown
- The instance name is now shown in the title of queue dashboard pages
- You can now click on avatars & display names of users on public note preview pages
- The queue dashboard now allows for batch retries of failed jobs
- Custom emoji are now displayed on public preview pages
- The error page for disabled public preview now has a login button
- Public preview pages have been rebuilt using Blazor SSR (Razor components)
- Sensitive media is now skipped for public preview embeds
- Public preview embeds with images now use the correct card type
- Delayed jobs with a retry count of zero are now marked as scheduled on the queue dashboard
- The entries in the queue dashboard overview table are now clickable
- Abandoning or descheduling jobs in the queue dashboard now requires confirmation
- CSS & JS files are now versioned on razor pages & blazor SSR

### Mastodon client API
- Blockquotes are now handled better for some clients
- Reaction notification are now shown in supported clients
- The git revision is no longer reported in the backend version string
- The bite extension is now supported, allowing bites to originate from compatible clients

### Akkoma client API
- Akkoma-specific endpoints have been implemented, adding support for Akkoma clients, including Akko-FE

### Miscellaneous
- The frontend is no longer unnecessarily rebuilt during CI runs
- SECURITY.md has been added to the repository root
- FEDERATION.md has been updated to reflect support for FEP-9fde
- Vulnerable dependency checks no longer cause build failures by default. To opt back in to the previous behavior, add the `DependencyVulnsAsError=true` build flag, or the `DEP_VULN_WERROR=true` make flag.

### Attribution
This release was made possible by project contributors: Jeder, Kopper, Laura Hausmann, Lilian, Samuel Proulx, kopper, notfire, pancakes & zotan

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
