## v2025.1-beta5
This release contains lots of new features & bug fixes. Upgrading is recommended for all server operators.

### Release notes
This release contains a **breaking change** - we now require PostgreSQL version 15 or higher. If you need assistance upgrading, please reach out to the [support chat](https://chat.iceshrimp.dev).

### Highlights
- The MFM parser has been completely rewritten, improving frontend performance by several orders of magnitude, as well as fixing countless bugs, slowdowns & edge cases.
- TOTP 2FA is now supported and can be configured in the user settings
- Instance rules can now be configured and displayed
- Links in user profile fields are now verified
- Full drive file management has been added
- Federated user pronouns have been added
- Remote media is now proxied by default
- The project and all in-house libraries now target .NET 9.0

### Blazor frontend
- Custom emoji in user bios, user fields, user display names, & note content warnings are now rendered correctly
- Display names & fields now only render their respective first line
- The note display now only breaks words break when necessary
- The font used on the frontend is now downloaded if it's not available on the client system
- The Iceshrimp.NET frontend can now be installed as a PWA (Offline support is not enabled yet)
- The frontend will automatically check for and notify about new versions
- Better emoji picker with categories and search support
- Follow button will no longer show up for your own profile
- User profiles now have badges that indicate if a user is following you, as well as badges for moderators, administrators, and automated accounts
- Notes by automated accounts are tagged as such
- Improved rendering of notifications
- Accounts that require follow approval are tagged appropriately
- Notes in the profile view can now be opened correctly
- The emoji picker now works correctly when composing a note
- Composing a reply no longer adds a mention for yourself
- The host part of local mentions is now hidden
- Alt text can now easily be viewed for note attachments
- More notification types are now supported, and feature appropriate icons and emoji
- A registration page has been added
- The login page has been reworked and now features an account selector for existing sessions
- TOTP 2FA enrollment and authentication are now possible
- Buttons that have a state now reflect their state better
- Default note visiblity is respected when composing new notes
- MfM rendering now supports many many more functions and should render most MfM art correctly (flip, font, x2/3/4, blur, rotate, crop, position, scale, fg, bg, fn, jelly, tada, jump, bounce, spin, shake, twitch, rainbow, fade, ruby, unixtime, center, small)
- All popover menus are now improved
- The attachment viewer now supports keyboard navigation and displays alt text
- Single character profiles can now be opened correctly
- When composing a note, attached files are now listed and have a preview
- Improved display of note reaction details
- User profile now has a menu for contextual actions
- Look of all buttons has been improved
- Full profile customization is now possible, including changing banners, profile pictures, tags, etc.
- The follow back button now renders correctly
- The note composer now has a preview of what your note will look like
- Note composer now features character count
- Posts can be submitted with ctrl/cmd + enter
- Virtual scroller was completely rewritten to be more performant
- Fetched note data is now cached
- You can now create rules that will be displayed on the registration page and the instances about page
- Support for setting profile avatar and banner alt text
- New better looking dialog system for prompts, notices, etc.
- Button to open/close all content warnings in a thread
- The cw button now shows how long the post behind the CW is
- Removed overscroll in places where it looks bad
- Added status indicator for notification and timeline streaming
- Refetch profile option for the profile page
- Drive management has been added, including folder support, upload, and deletion, and modification
- Added a dedicated pronoun field on the profile page
- Menus take up more of the screen on the mobile UI and are easier to navigate
- Management page for local and remote emoji (Upload, modification, cloning)
- Completely reworked default theme
- Style improvements to go with the new default theme
- Support for poll rendering and voting
- Improved loading spinners
- Menu to change accounts or log out
- Settings pages no longer exceed screen height unless needed
- Notification content is limited to a reasonable size
- Improved rendering of cw and reactions in indented notes
- Admin cookie persists unless you log out the admin account
- Fixed a crash in the attachment viewer on chrome
- Content warnings now correctly hide quotes
- Added indicator when attachments are uploading
- Disabled posting note while attachments are uploading or note is empty
- Blurred images are now easier to deblur
- Many z-index issues have been fixed
- Page title now reflects instance name and current page

### Razor (public preview, admin panel, queue dashboard, etc.)
- The admin dashboard now has a responsive navigation bar
- Constructor dependency injection is now used where applicable
- Static assets are now collected, compressed & fingerprinted at build time
- The favicon is now correctly set to the project logo
- The index page now displays the Iceshrimp project wordmark
- The page footer is now more responsive
- Emoji now have their name set as alt text
- The queue dashboard index page now has a "top delayed" section
- The page footer now shows a registration link when registrations are open or invite-only
- The generate invite button on the admin panel is now accessible to screen-reader users
- The federation management page of the admin panel now has a search box
- The admin panel now supports remote user management & user search
- Polls are now displayed in public preview

### Backend
- Fork information in the version string is now handled correctly
- Version information is now only computed once
- Failed user resolutions no longer break the follow list import process
- Command line output referencing help pages now uses shortlinks, to prevent link rot
- Note backfilling now uses a stack instead of a queue
- MIME type & file extension are now being set correctly for converted images
- Locally originating create activities can now be fetched by their URI
- User responses now contain any emoji used in their display name or bio
- A DbContext race condition in UserRenderer has been fixed, resolving transient concurrency errors
- The search query parser now supports the has:media query
- User publickeys now have any extra whitespace removed before being added to the database
- Instance staff endpoints have been added
- User lookup error messages are now more specific
- User profile responses now include user roles, as well as the IsBot, IsCat and IsLocked fields
- Uploading files with long unicode names now works correctly
- Lock statements now use lock objects for improved performance
- GeneratedRegex partial methods have been converted to partial properties
- All params methods have been converted to take `IEnumerable<T>` as parameters
- The `dotnet ef database update` command now works as expected with multiplexing enabled
- An alternative OpenAPI UI - Scalar - has been added (accessible under `/scalar` & `/openapi`)
- Unauthenticated federation endpoints now cache their outputs for a short duration, easing database load during request bursts
- Release builds now use compiled EF models, reducing startup time by ~500ms
- The startup duration is now logged to console
- Entity model configuration has been moved into the respective entity classes
- The OpenAPI schema is now only generated once
- Usages of the `ConsumesHybrid` attribute have been replaced with `FromHybrid`
- `BlazorSsrHandoffMiddleware` now uses reflection instead of modifying the response
- A new exception verbosity option `Debug` has been added
- The error page title now contain the status code
- Middleware is now invoked conditionally, improving performance, simplifying stack traces and allowing plugins to add middleware to the stack
- Services are now runtime-discoverable, greatly improving readability
- Scoped services with request-specific properties have been converted to singletons using `AsyncLocal<T>`
- Unneeded compressed assets are no longer generated during build, improving build times
- The solution file now has virtual folders for build assets & project root files
- Version & web manifest endpoints have been added to support frontend PWA features
- Exceptions in StreamingConnectionAggregate no longer crash the backend
- Note creates & updates now get delivered to the author of note being replied to even if they're not mentioned
- Note recipients now get deduplicated
- Instance info endpoints have been added
- Support for note context collections has been added
- Reaction notifications now contain more information about the received reaction
- Note inline media is now supported using the `$[media <uri>]` MFM tag
- Session management endpoints have been added
- Line endings now get canonicalized during note/user ingest/update for improved frontend performance
- Empty & whitespace alt text now gets treated as no alt text
- User profile responses now contain the public URL of the user
- Endpoints related to user avatar, banner & display name have been added
- The user settings endpoints now allow for configuring the `isBot`, `isCat` and `speakAsCat` properties
- HTML markup tags are now deserialized to their corresponding MFM tag equivalent, instead of using symbol tags
- The note resolution lock now uses the fetched object `@id` property as its key
- Note lookups are now authenticated with the requesting user & don't attempt to redirect to inaccessible notes
- The batch emoji import endpoint is now excluded from the request size limit
- The emoji management endpoints now require moderator permissions instead of administrator ones
- Quotes without text no longer federate incorrectly to quote-aware implementations
- Notes from implementations sending HTML line breaks not followed by newline characters now get parsed correctly
- Quote blocks now aren't surrounded by extraneous line breaks
- The default renote visibility user setting can no longer be set to `specified`
- The user resolver now falls back to building the username/host tuple from the actor URI when it's not contained in the WebFinger response
- Reply backfill jobs now don't get scheduled for followers-only posts when authenticated user backfill is disabled
- The `w3id/identity-v1` JSON-LD context definition is now preloaded
- Outgoing unixtime MFM nodes now get converted to human-readable HTML
- Nodeinfo responses now return the configured instance name, description & admin contact email
- Support for backfilling user profiles has been added
- The exposed outbox collection is now functional
- Transient LD signature validation errors due to use of the wrong media type parser have been resolved
- Note refetches no longer wrongly mark notes as edited
- Fetching the relay actor now bypasses authorized fetch
- A startup error is now raised if the `ASPNETCORE_TEMP` is not writable
- Requests sent by suspended remote users are now rejected early during authorized fetch / inbox validation
- The unix socket permissions are now customizable
- The rewrite policy `CollapseWhitespace` was added
- Single emoji can now be given a name before uploading them
- The search query parser has been rewritten in C#, dropping the `FSharp.Core` dependency
- The UserResolver acct/uri mismatch message has been significantly improved
- Processed images now federate with the correct content type
- Negated search parameters now work with `match:words`
- The instance info response now contains the note length limit
- HTTP proxy configurations are now supported
- Hashtags are now handled more correctly, improving federation compatibility
- The home timeline heuristic now gets updated automatically for recently active users
- Hashtags now get the correct class set in when serialized to HTML
- Notes with `publishedAt`/`updatedAt` set to timestamps from the future will now get clamped to the current time
- The `Result<T>` helper type is now provided by `Iceshrimp.Utils.Common`
- User migration events now also transfer incoming and outgoing blocks to the new account
- The emoji table now correctly enforces unique names for local emoji (duplicates get fixed automatically, the newest entry is preserved)
- Like activities with `content` property now get correctly processed as reactions
- Deletion failures during media fixup are now ignored
- Avatar & banner alt text now federates bidirectionally, is returned in corresponding API responses & can be set
- `ExpressionExtensions` and `QueryableExtensions.AsChunkedAsyncEnumerable<T>` are now provided by `Iceshrimp.EntityFrameworkCore.Extensions`
- The license of assets included in the repository has been clarified to be `CC BY-SA 4.0`
- A refetch user endpoint has been added
- Remote emoji management endpoints have been added
- Polls can now be created, retrieved & voted on via the Web API
- Emoji media types now get populated & federated as appropriate
- Emoji entity names now get wrapped in colons for federation, resolving an interoperability issue with NodeBB

### Akkoma client API
- Local-only visibility is now respected

### Mastodon client API
- Admin scopes are now considered valid, allowing clients who request these to authenticate
- The confusing status context logic has been removed, matching -js & web api behavior
- The specified WebSocket protocol is now echoed back for streaming connections, fixing compatibility issues with some clients
- Attachment metadata is now returned when available
- Filter matches are now deduplicated, preventing duplicate filter match mesages
- The "reply inaccessible" marker now gets moved into the content warning (if any) and is more consistent
- Blockquotes now get rendered correctly when `supportsHtmlFormatting` is disabled
- Multiple accounts can now be fetched in one go via `/api/v1/accounts`
- Multiple statuses can now be fetched in one go via `/api/v1/statuses`
- The status response now correctly lists all hashtags
- The `/api/v1/accounts/{id}/statuses` endpoint no longer requires authentication, matching Mastodon's behavior

### Unit tests
- Tests now take less time to run due to higher parallelization
- The testing platform has been changed from `VSTest` to `Microsoft.Testing.Platform`
- The assertions library has been changed from `FluentAssertions` to `Iceshrimp.Assertions` due to a license change

### Build tasks
- Compressed razor class library assets now have corresponding static asset selector routes
- Pre-fingerprinted static assets collected from razor class libraries now get mapped correctly

### Miscellaneous
- The README has been updated
- The Dockerfile has been updated
- The security policy has been updated
- The OpenAPI documentation has been improved

### Attribution
This release was made possible by project contributors: blueb, Jeder, Kopper, Laura Hausmann, Lilian, notfire, pancakes & Tamara Schmitz

## v2024.1-beta4.security2
This is a security hotfix release. It's identical to v2024.1-beta4.security1, except for the security mitigations listed below. Upgrading is strongly recommended for all server operators.

### Backend
- Several DoS & stack overflow vulnerabilities in the MFM parser were resolved

### Miscellaneous
- Performance of the MFM parser (and by extension, the frontend) should be significantly improved, as the backport of the security fixes also contains all other performance-related changes since v2024.1-beta4.

### Attribution
This release was made possible by project contributors: Laura Hausmann

Furthermore, I want to give special thanks to Natty for helping with investigating this vulnerability.

## v2024.1-beta4.security1
This is a security hotfix release. It's identical to v2024.1-beta4, except for the security mitigations listed below. Upgrading is strongly recommended for all server operators.

### Backend
- ActivityPub actor and note validation has been improved & now protects against cross-origin identifiers in more places, resolving a database pollution vulnerability
- Cross-origin `url` properties on actor & note objects now get set to null before ingestion, resolving a clickjacking vulnerability
- User resolution when processing incoming notes is now limited

### Attribution
This release was made possible by project contributors: Laura Hausmann

Furthermore, I want to give special thanks to Hazel Koehler for the vulnerability disclosure.

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
- The frontend layout & stylesheet have been significantly refined
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
This release was made possible by project contributors: Jeder, Laura Hausmann, Lilian, Samuel Proulx, kopper, notfire & pancakes

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
