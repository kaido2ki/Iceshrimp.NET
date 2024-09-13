namespace Iceshrimp.Parsing

open System
open FParsec

module SearchQueryFilters =
    type Filter() = class end

    type WordFilter(neg: bool, value: string) =
        inherit Filter()
        member val Negated = neg
        member val Value = value

    type MultiWordFilter(values: string list) =
        inherit Filter()
        member val Values = values

    type FromFilter(neg: bool, value: string) =
        inherit Filter()
        member val Negated = neg
        member val Value = value

    type MentionFilter(neg: bool, value: string) =
        inherit Filter()
        member val Negated = neg
        member val Value = value

    type ReplyFilter(neg: bool, value: string) =
        inherit Filter()
        member val Negated = neg
        member val Value = value

    type InstanceFilter(neg: bool, value: string) =
        inherit Filter()
        member val Negated = neg
        member val Value = value

    type MiscFilterType =
        | Followers
        | Following
        | Replies
        | Renotes

    type MiscFilter(neg: bool, value: string) =
        inherit Filter()
        member val Negated = neg

        member val Value =
            match value with
            | "followers" -> Followers
            | "following" -> Following
            | "replies" -> Replies
            | "reply" -> Replies
            | "boosts" -> Renotes
            | "boost" -> Renotes
            | "renote" -> Renotes
            | "renotes" -> Renotes
            | _ -> failwith $"Invalid type: {value}"

    type InFilterType =
        | Bookmarks
        | Likes
        | Reactions
        | Interactions

    type InFilter(neg: bool, value: string) =
        inherit Filter()
        member val Negated = neg

        member val Value =
            match value with
            | "bookmarks" -> Bookmarks
            | "likes" -> Likes
            | "favorites" -> Likes
            | "favourites" -> Likes
            | "reactions" -> Reactions
            | "interactions" -> Interactions
            | _ -> failwith $"Invalid type: {value}"


    type AttachmentFilterType =
        | Any
        | Image
        | Video
        | Audio
        | File
        | Poll

    type AttachmentFilter(neg: bool, value: string) =
        inherit Filter()
        member val Negated = neg

        member val Value =
            match value with
            | "any" -> Any
            | "image" -> Image
            | "video" -> Video
            | "audio" -> Audio
            | "file" -> File
            | "poll" -> Poll
            | _ -> failwith $"Invalid type: {value}"

    type AfterFilter(d: DateOnly) =
        inherit Filter()
        member val Value = d

    type BeforeFilter(d: DateOnly) =
        inherit Filter()
        member val Value = d

    type CaseFilterType =
        | Sensitive
        | Insensitive

    type CaseFilter(v: string) =
        inherit Filter()

        member val Value =
            match v with
            | "sensitive" -> Sensitive
            | "insensitive" -> Insensitive
            | _ -> failwith $"Invalid type: {v}"

    type MatchFilterType =
        | Words
        | Substring

    type MatchFilter(v: string) =
        inherit Filter()

        member val Value =
            match v with
            | "word" -> Words
            | "words" -> Words
            | "substr" -> Substring
            | "substring" -> Substring
            | _ -> failwith $"Invalid type: {v}"

module private SearchQueryParser =
    open SearchQueryFilters

    // Abstractions
    let str s = pstring s
    let tokenEnd = (skipChar ' ' <|> eof)
    let token = anyChar |> manyCharsTill <| tokenEnd
    let orTokenEnd = (skipChar ' ' <|> lookAhead (skipChar ')') <|> eof)
    let orToken = spaces >>. anyChar |> manyCharsTill <| orTokenEnd
    let key s = str s .>>? pchar ':'
    let strEnd s = str s .>>? tokenEnd
    let anyStr s = choice (s |> Seq.map strEnd)
    let anyKey k = choice (k |> Seq.map key)
    let seqAttempt s = s |> Seq.map attempt
    let neg = opt <| pchar '-'
    let negFilter k = pipe2 neg (anyKey k >>. token)
    let negKeyFilter k v = pipe2 neg (anyKey k >>. anyStr v)
    let keyFilter k v = anyKey k >>. anyStr v
    let strSepByOr = sepBy orToken (str "OR ")

    let parseDate (s: string) =
        match DateOnly.TryParseExact(s, "O") with
        | true, result -> preturn result
        | false, _ -> fail $"Invalid date: {s}"

    let dateFilter k = anyKey k >>. token >>= parseDate

    // Filters
    let wordFilter = pipe2 neg token <| fun a b -> WordFilter(a.IsSome, b) :> Filter

    let multiWordFilter =
        skipChar '(' >>. strSepByOr .>> skipChar ')'
        |>> fun v -> MultiWordFilter(v) :> Filter

    let literalStringFilter =
        skipChar '"' >>. manyCharsTill anyChar (skipChar '"')
        |>> fun v -> WordFilter(false, v) :> Filter

    let fromFilter =
        negFilter [ "from"; "author"; "by"; "user" ]
        <| fun n v -> FromFilter(n.IsSome, v) :> Filter

    let mentionFilter =
        negFilter [ "mention"; "mentions"; "mentioning" ]
        <| fun n v -> MentionFilter(n.IsSome, v) :> Filter

    let replyFilter =
        negFilter [ "reply"; "replying"; "to" ]
        <| fun n v -> ReplyFilter(n.IsSome, v) :> Filter

    let instanceFilter =
        negFilter [ "instance"; "domain"; "host" ]
        <| fun n v -> InstanceFilter(n.IsSome, v) :> Filter

    let miscFilter =
        negKeyFilter
            [ "filter" ]
            [ "followers"
              "following"
              "replies"
              "reply"
              "renote"
              "renotes"
              "boosts"
              "boost" ]
        <| fun n v -> MiscFilter(n.IsSome, v) :> Filter

    let inFilter =
        negKeyFilter [ "in" ] [ "bookmarks"; "favorites"; "favourites"; "reactions"; "likes"; "interactions" ]
        <| fun n v -> InFilter(n.IsSome, v) :> Filter

    let attachmentFilter =
        negKeyFilter [ "has"; "attachment"; "attached" ] [ "any"; "image"; "video"; "audio"; "file"; "poll" ]
        <| fun n v -> AttachmentFilter(n.IsSome, v) :> Filter

    let afterFilter =
        dateFilter [ "after"; "since" ] |>> fun v -> AfterFilter(v) :> Filter

    let beforeFilter =
        dateFilter [ "before"; "until" ] |>> fun v -> BeforeFilter(v) :> Filter

    let caseFilter =
        keyFilter [ "case" ] [ "sensitive"; "insensitive" ]
        |>> fun v -> CaseFilter(v) :> Filter

    let matchFilter =
        keyFilter [ "match" ] [ "words"; "word"; "substr"; "substring" ]
        |>> fun v -> MatchFilter(v) :> Filter

    // Filter collection
    let filterSeq =
        [ literalStringFilter
          fromFilter
          mentionFilter
          replyFilter
          instanceFilter
          miscFilter
          inFilter
          attachmentFilter
          afterFilter
          beforeFilter
          caseFilter
          matchFilter
          multiWordFilter
          wordFilter ]

    // Final parse commands
    let filters = choice <| seqAttempt filterSeq
    let parse = manyTill (spaces >>. filters .>> spaces) eof

module SearchQuery =
    open SearchQueryParser

    let parse str =
        match run parse str with
        | Success(result, _, _) -> result
        | Failure(s, _, _) -> failwith $"Failed to parse query: {s}"
