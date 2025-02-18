# Federation

## Terminology

The key words "**MUST**", "**MUST NOT**", "**REQUIRED**", "**SHALL**", "**SHALL NOT**", "**SHOULD**", "**SHOULD NOT**", "**RECOMMENDED**", "**MAY**", and "**OPTIONAL**" in this document are to be interpreted as described in [RFC-2119](https://datatracker.ietf.org/doc/html/rfc2119).

This document **MAY** alias JSON-LD namespace IRIs to their well-known aliases. Specifically, the following aliases are used:
- `as:` - `https://www.w3.org/ns/activitystreams#`
- `toot:` - `http://joinmastodon.org/ns#`
- `fedibird:` - `http://fedibird.com/ns#`
- `misskey:` - `https://misskey-hub.net/ns#`
- `litepub:` - `http://litepub.social/ns#`

## Supported federation protocols and standards

- [ActivityPub](https://www.w3.org/TR/activitypub/) (Server-to-Server)
  - We perform full JSON-LD processing.
  - Incoming activities (whether sent to the shared/actor inbox or fetched via federation requests) **MUST** carry a valid JSON-LD context for successful federation with Iceshrimp.NET instances.
  - Regardless, we attempt to make sense of activities carrying some known invalid LD contexts. Specifically:
    + We resolve the nonexistent `http://joinmastodon.org/ns` context ([toot.json](https://iceshrimp.dev/iceshrimp/Iceshrimp.NET/src/branch/dev/Iceshrimp.Backend/Core/Federation/ActivityStreams/Contexts/toot.json)) for federation with GTS (which references it by link)
    + We resolve some unofficial ActivityStreams context extensions ([as-extensions.json](https://iceshrimp.dev/iceshrimp/Iceshrimp.NET/src/branch/dev/Iceshrimp.Backend/Core/Federation/ActivityStreams/Contexts/as-extensions.json)), since some implementors incorrectly reference it by link.
    + See [here](https://iceshrimp.dev/iceshrimp/Iceshrimp.NET/src/branch/dev/Iceshrimp.Backend/Core/Federation/ActivityStreams/LdHelpers.cs#L16-L24) and [here](https://iceshrimp.dev/iceshrimp/Iceshrimp.NET/src/branch/dev/Iceshrimp.Backend/Core/Federation/ActivityStreams/Contexts) to see all preloaded LD contexts we ship.
  - Outgoing activities are compacted against our well-known LD context ([iceshrimp.json](https://iceshrimp.dev/iceshrimp/Iceshrimp.NET/src/branch/dev/Iceshrimp.Backend/Core/Federation/ActivityStreams/Contexts/iceshrimp.json)).
    + For compatibility with implementors that are not doing full LD processing, we force some attributes to be an array:
      * `tag`, `attachment`, `to`, `cc`, `bcc`, `bto`, `alsoKnownAs` (all in the `https://www.w3.org/ns/activitystreams` namespace)
    + For the same reason, we forcibly keep `https://www.w3.org/ns/activitystreams#Public` as the full IRI, instead of compacting it to `as:Public`.
    + We trim unused inline properties from the context. For technical reasons, unused namespace aliases are currently not trimmed, but this is subject to change.
- [WebFinger](https://webfinger.net/)
  - Any actors referenced in activities **MUST** be queryable via WebFinger for federation with Iceshrimp.NET instances.
  - Actor `@id` URIs **SHOULD** be directly queryable via WebFinger, but [reverse discovery](https://www.w3.org/community/reports/socialcg/CG-FINAL-apwf-20240608/#reverse-discovery) is performed as a fallback.
  - Split domain configurations are supported (for local and remote actors).
    + Implementors **MUST NOT** have multiple actors with the same `preferredUsername` on each web or account domain.
    + Mentions referencing a user by their non-canonical `acct` (`@user@web.domain.tld`) get canonicalized on note ingestion.
  - We support WebFinger over `application/jrd+json` as well as `application/xrd+xml` (both incoming and outgoing).
    + However, we do not ask for `xrd+xml` in our `Accept` header for outgoing WebFinger requests due to [compatibility issues](https://github.com/friendica/friendica/issues/14370) with Friendica.
    + Responses **MUST** have their `Content-Type` set to `application/jrd+json`, `application/xrd+xml`, `application/json`, or `application/xml`.
    + Responses **MUST** contain a link with the attributes `rel='self'` and `type='application/activity+json'`.
        * `application/ld+json; profile="https://www.w3.org/ns/activitystreams"` is treated interchangably with `application/activity+json`.
    + Responses **SHOULD** contain the `acct:` URI of the actor in the `subject` or `aliases` fields.
      * If no such URI is found, we attempt to fetch the actor via ActivityPub and assemble the link from the actor's `preferredUsername` and `@id` host.
  - We support host-meta over `application/jrd+json` as well as `application/xrd+xml` (both incoming and outgoing).
    + The json representation is also accessible under `/.well-known/host-meta.json`.
    + Implementors **SHOULD** advertise the WebFinger `Content-Type` in the `type` attribute of the WebFinger template in the host-meta response.
      * However, since major implementors either omit the attribute, or incorrectly advertise `jrd+json` as `xrd+xml`, we presently ignore this property. 
- [HTTP Signatures](https://datatracker.ietf.org/doc/html/draft-cavage-http-signatures)
  - Incoming activities sent to the shared inbox or actor inbox **MUST** carry a valid HTTP signature, unless LD Signatures are explicitly enabled in the configuration, and the activity carries a valid LD signature.
  - Incoming federation requests **MUST** carry a valid HTTP signature, unless authorized fetch is explicitly disabled in the configuration.
- [LD Signatures](https://web.archive.org/web/20170923124140/https://w3c-dvcg.github.io/ld-signatures/)
  + Both LD-signing outgoing activities and accepting LD signatures are disabled by default due to privacy concerns, but instance operators can choose to enable them.
  + `as:Delete` activities, which don't come with any of the privacy concerns mentioned above, are however accepted regardless of the configuration.
- [NodeInfo](https://nodeinfo.diaspora.software/) (versions 2.0 and 2.1)

## Supported FEPs

- [FEP-f1d5: NodeInfo in Fediverse Software](https://codeberg.org/fediverse/fep/src/branch/main/fep/f1d5/fep-f1d5.md)
- [FEP-c0e0: Emoji reactions](https://codeberg.org/fediverse/fep/src/branch/main/fep/c0e0/fep-c0e0.md)
  + `litepub:EmojiReact` activities are processed as emoji reactions.
  + `as:Like` activities with `as:content` or `misskey:_misskey_reaction` properties get canonicalized to `litepub:EmojiReact`.
  + Multiple emoji reactions are supported.
  + Remote custom emoji reactions are supported if two conditions are met:
    * The `as:content` property is set to `:emoji@remoteinstance.tld:` or `emoji@remoteinstance.tld`
    * The emoji `emoji` is already known from a post or reaction by a user on `remoteinstance.tld`
- [FEP-e232: Object Links](https://codeberg.org/fediverse/fep/src/branch/main/fep/e232/fep-e232.md)
    + Specifically, inline quotes with the following `rel` attributes are supported:
        * `misskey:_misskey_quote`
        * `fedibird:quoteUri`
        * `as:quoteUrl`
- [FEP-67ff: FEDERATION.md](https://codeberg.org/fediverse/fep/src/branch/main/fep/67ff/fep-67ff.md)
- [FEP-2c59: Discovery of a Webfinger address from an ActivityPub actor](https://codeberg.org/fediverse/fep/src/branch/main/fep/2c59/fep-2c59.md)
- [FEP-9fde: Mechanism for servers to expose supported operations](https://codeberg.org/fediverse/fep/src/branch/main/fep/9fde/fep-9fde.md)
- [FEP-7888: Demystifying the context property](https://codeberg.org/fediverse/fep/src/branch/main/fep/7888/fep-7888.md)
    + Specifically, we use it in a "conversational context" sense, where each note has an attached context, which maps to an internal "thread".
    + We currently do not use the context for anything other than grouping.
    + Our context collections contain objects, not activities.

## FEPs we intend to support in the future
- [FEP-8fcf: Followers collection synchronization across servers](https://codeberg.org/fediverse/fep/src/branch/main/fep/8fcf/fep-8fcf.md)
- [FEP-1b12: Group federation](https://codeberg.org/fediverse/fep/src/branch/main/fep/1b12/fep-1b12.md)
- [FEP-96ff: Explicit signalling of ActivityPub Semantics](https://codeberg.org/fediverse/fep/src/branch/main/fep/96ff/fep-96ff.md)
- [FEP-d556: Server-Level Actor Discovery Using WebFinger](https://codeberg.org/fediverse/fep/src/branch/main/fep/d556/fep-d556.md)
- [FEP-2677: Identifying the Application Actor](https://codeberg.org/fediverse/fep/src/branch/main/fep/2677/fep-2677.md)

## Supported non-FEP extensions
- `Bite` activities (as specified in [mia:Bite](https://ns.mia.jetzt/as/#Bite))
