namespace Iceshrimp.Parsing

open System
open System.Collections.Generic
open System.Diagnostics
open System.Runtime.InteropServices
open FParsec

module MfmNodeTypes =
    [<AbstractClass>]
    type MfmNode() =
        member val Children: MfmNode list = [] with get, set

    [<AbstractClass>]
    type MfmInlineNode(c: MfmInlineNode list) =
        inherit MfmNode()
        do base.Children <- c |> List.map (fun x -> x :> MfmNode)

    [<AbstractClass>]
    type MfmBlockNode(c: MfmInlineNode list) =
        inherit MfmNode()
        do base.Children <- c |> List.map (fun x -> x :> MfmNode)

    type InlineNodeType =
        | Symbol
        | HtmlTag

    type MfmTextNode(v: string) =
        inherit MfmInlineNode([])
        member val Text = v

    type MfmTimeoutTextNode(v: string) =
        inherit MfmTextNode(v)

    type MfmItalicNode(c, t) =
        inherit MfmInlineNode(c)
        member val Type: InlineNodeType = t

    type MfmBoldNode(c, t) =
        inherit MfmInlineNode(c)
        member val Type: InlineNodeType = t

    type MfmStrikeNode(c, t) =
        inherit MfmInlineNode(c)
        member val Type: InlineNodeType = t

    type MfmInlineCodeNode(v: string) =
        inherit MfmInlineNode([])
        member val Code = v

    type MfmPlainNode(v: string) =
        inherit MfmInlineNode([ MfmTextNode(v) ])

    type MfmSmallNode(c) =
        inherit MfmInlineNode(c)

    type MfmQuoteNode(c, followedByQuote, followedByEof) =
        inherit MfmBlockNode(c)
        member val FollowedByQuote = followedByQuote
        member val FollowedByEof = followedByEof

    type MfmSearchNode(content: string, query: string) =
        inherit MfmBlockNode([])
        member val Content = content
        member val Query = query

    type MfmCodeBlockNode(code, lang: string option) =
        inherit MfmBlockNode([])
        member val Code = code
        member val Language = lang

    type MfmMathBlockNode(f) =
        inherit MfmBlockNode([])
        member val Formula = f

    type MfmCenterNode(c) =
        inherit MfmBlockNode(c)

    type MfmEmojiCodeNode(n) =
        inherit MfmInlineNode([])
        member val Name = n

    type MfmMathInlineNode(f) =
        inherit MfmInlineNode([])
        member val Formula = f

    type MfmMentionNode(acct, user, host: string option) =
        inherit MfmInlineNode([])
        member val Acct = acct
        member val Username = user
        member val Host = host

    type MfmHashtagNode(h) =
        inherit MfmInlineNode([])
        member val Hashtag = h

    type MfmUrlNode(url, brackets) =
        inherit MfmInlineNode([])
        member val Url = url
        member val Brackets = brackets

    type MfmLinkNode(url, text, silent) =
        inherit MfmInlineNode([ MfmTextNode(text) ])
        member val Url = url
        member val Silent = silent

    type MfmFnNode(name, args: IDictionary<string, string option> option, children) =
        inherit MfmInlineNode(children)
        member val Name = name
        member val Args = args

    type internal MfmCharNode(v: char) =
        inherit MfmInlineNode([])
        member val Char = v

    type internal UserState =
        { ParenthesisStack: char list
          LastLine: int64
          Depth: int
          TimeoutAt: int64 }

        member this.TimeoutReached = Stopwatch.GetTimestamp() > this.TimeoutAt

        static member Timeout =
            match RuntimeInformation.OSArchitecture with
            | Architecture.Wasm -> Stopwatch.Frequency * 2L // 2000ms
            | _ -> Stopwatch.Frequency / 2L // 500ms

        static member Default =
            fun () ->
                { ParenthesisStack = []
                  LastLine = 0
                  Depth = 0
                  TimeoutAt = Stopwatch.GetTimestamp() + UserState.Timeout }

open MfmNodeTypes

module private MfmParser =
    // Override - prevents O(n!) complexity for recursive grammars where endp applies immediately
    let many1Till p endp = notFollowedBy endp >>. many1Till p endp

    let skipMany1Till p endp =
        notFollowedBy endp >>. skipMany1Till p endp

    // Abstractions
    let str s = pstring s
    let seqAttempt s = s |> Seq.map attempt
    let isWhitespace c = Char.IsWhiteSpace c
    let isNotWhitespace c = Char.IsWhiteSpace c = false

    let isAsciiLetterOrNumber c = Char.IsAsciiLetter c || Char.IsDigit c
    let isLetterOrNumber c = Char.IsLetterOrDigit c
    let isNewline c = '\n'.Equals(c)
    let isNotNewline c = not (isNewline c)

    let followedByChar c = nextCharSatisfies <| fun ch -> c = ch

    let (|CharNode|MfmNode|) (x: MfmNode) =
        if x :? MfmCharNode then
            CharNode(x :?> MfmCharNode)
        else
            MfmNode x

    let folder (current, result) node =
        match (node: MfmNode), (current: char list) with
        | CharNode node, _ -> node.Char :: current, result
        | MfmNode node, [] -> current, node :: result
        | MfmNode node, _ -> [], node :: (MfmTextNode(current |> List.toArray |> String) :: result)

    let aggregateText nodes =
        nodes
        |> List.rev
        |> List.fold folder ([], [])
        |> function
            | [], result -> result
            | current, result -> MfmTextNode(current |> List.toArray |> String) :: result

    let aggregateTextInline nodes =
        nodes |> aggregateText |> List.map (fun x -> x :?> MfmInlineNode)

    let domainComponent =
        many1Chars (
            satisfy isAsciiLetterOrNumber
            <|> pchar '_'
            <|> attempt (
                previousCharSatisfies isAsciiLetterOrNumber >>. pchar '-'
                .>> lookAhead (satisfy isAsciiLetterOrNumber)
            )
            <|> attempt (
                (previousCharSatisfies '-'.Equals <|> previousCharSatisfies isAsciiLetterOrNumber)
                >>. pchar '-'
                .>> lookAhead (satisfy '-'.Equals <|> satisfy isAsciiLetterOrNumber)
            )
        )

    let domain =
        domainComponent .>>. (many <| attempt (skipChar '.' >>. domainComponent))
        |>> fun (a, b) -> String.concat "." <| Seq.append [ a ] b

    let acct (user: string, host: string option) =
        match host with
        | None -> user
        | Some v -> user + "@" + v

    let fnArg =
        many1Chars asciiLetter
        .>>. opt (
            pchar '='
            >>. manyCharsTill anyChar (nextCharSatisfies <| fun p -> p = ',' || isWhitespace p)
        )

    let fnDict (input: (string * string option) list option) : IDictionary<string, string option> option =
        match input with
        | None -> None
        | Some items -> items |> dict |> Some

    let pushLine: Parser<unit, UserState> =
        fun stream ->
            stream.UserState <-
                { stream.UserState with
                    LastLine = stream.Line }

            Reply(())

    let assertLine: Parser<unit, UserState> =
        let error = messageError "Line changed"

        fun stream ->
            match stream.UserState.LastLine = stream.Line with
            | true -> Reply(())
            | false -> Reply(Error, error)

    let assertParen = userStateSatisfies <| fun u -> u.ParenthesisStack.Length > 0
    let assertNoParen = userStateSatisfies <| fun u -> u.ParenthesisStack.Length = 0

    let pushParen =
        updateUserState
        <| fun u ->
            { u with
                ParenthesisStack = '(' :: u.ParenthesisStack }

    let popParen =
        assertParen
        >>. updateUserState (fun u ->
            { u with
                ParenthesisStack = List.tail u.ParenthesisStack })

    let clearParen = updateUserState <| fun u -> { u with ParenthesisStack = [] }

    let (|GreaterEqualThan|_|) k value = if value >= k then Some() else None

    let restOfLineContains (s: string) : Parser<unit, UserState> =
        let error = messageError "No match found"

        fun (stream: CharStream<_>) ->
            let pos = stream.Position
            let rest = stream.ReadRestOfLine false
            do stream.Seek pos.Index

            match rest with
            | c when c.Contains(s) -> Reply(())
            | _ -> Reply(Error, error)

    let restOfLineContainsChar (c: char) : Parser<unit, UserState> =
        let error = messageError "No match found"
        let func ch = ch <> c && isNotNewline ch

        fun (stream: CharStream<_>) ->
            let pos = stream.Position
            let _ = stream.SkipCharsOrNewlinesWhile(func)
            let ch = stream.Read()
            do stream.Seek pos.Index

            match ch with
            | m when m = c -> Reply(())
            | _ -> Reply(Error, error)

    let restOfSegmentContains (s: string, segment: char -> bool) : Parser<unit, UserState> =
        let error = messageError "No match found"

        fun (stream: CharStream<_>) ->
            let pos = stream.Position
            let rest = stream.ReadCharsOrNewlinesWhile(segment, false)
            do stream.Seek pos.Index

            match rest with
            | c when c.Contains s -> Reply(())
            | _ -> Reply(Error, error)

    let restOfStreamContains (s: string) : Parser<unit, UserState> =
        restOfSegmentContains (s, (fun _ -> true))

    let streamMatches (s: string) : Parser<unit, UserState> =
        fun stream ->
            match stream.Match s with
            | true -> Reply(())
            | false -> Reply(Error, NoErrorMessages)

    let streamMatchesOrEof (s: string) : Parser<unit, UserState> =
        fun stream ->
            match (stream.Match s, stream.IsEndOfStream) with
            | false, false -> Reply(Error, NoErrorMessages)
            | _ -> Reply(())

    let anyCharExceptNewline: Parser<char, UserState> =
        let error = messageError "anyCharExceptNewline"

        fun stream ->
            let c = stream.ReadCharOrNewline()

            if c <> EOS && isNotNewline c then
                Reply(c)
            else
                Reply(Error, error)

    let createParameterizedParserRef () =
        let dummyParser _ =
            fun _ -> failwith "a parser created with createParameterizedParserRef was not initialized"

        let r = ref dummyParser
        (fun endp -> (fun stream -> r.Value endp stream)), r

    // References
    let manyInlineNodesTill, manyInlineNodesTillRef = createParameterizedParserRef ()
    let many1InlineNodesTill, many1InlineNodesTillRef = createParameterizedParserRef ()

    let seqFlatten items =
        seq {
            for item in items do
                yield! item
        }

    // Patterns
    let italicPatternAsterisk = notFollowedByString "**" >>. skipChar '*'
    let italicPatternUnderscore = notFollowedByString "__" >>. skipChar '_'
    let codePattern = notFollowedByString "```" >>. skipChar '`'

    // Matchers
    let hashtagMatcher = letter <|> digit <|> anyOf "-_"
    let hashtagSatisfier = attempt hashtagMatcher

    // Node parsers
    let italicAsteriskNode =
        previousCharSatisfiesNot isNotWhitespace
        >>. italicPatternAsterisk
        >>. restOfLineContainsChar '*' // TODO: this doesn't cover the case where a bold node ends before the italic one
        >>. pushLine // Remove when above TODO is resolved
        >>. manyInlineNodesTill italicPatternAsterisk
        .>> assertLine // Remove when above TODO is resolved
        |>> fun c -> MfmItalicNode(aggregateTextInline c, Symbol) :> MfmNode

    let italicUnderscoreNode =
        previousCharSatisfiesNot isNotWhitespace
        >>. italicPatternUnderscore
        >>. restOfLineContainsChar '_' // TODO: this doesn't cover the case where a bold node ends before the italic one
        >>. pushLine // Remove when above TODO is resolved
        >>. manyInlineNodesTill italicPatternUnderscore
        .>> assertLine // Remove when above TODO is resolved
        |>> fun c -> MfmItalicNode(aggregateTextInline c, Symbol) :> MfmNode

    let italicTagNode =
        skipString "<i>"
        >>. restOfStreamContains "</i>"
        >>. manyInlineNodesTill (skipString "</i>")
        |>> fun c -> MfmItalicNode(aggregateTextInline c, HtmlTag) :> MfmNode

    let boldAsteriskNode =
        previousCharSatisfiesNot isNotWhitespace
        >>. skipString "**"
        >>. restOfLineContains "**"
        >>. manyInlineNodesTill (skipString "**")
        |>> fun c -> MfmBoldNode(aggregateTextInline c, Symbol) :> MfmNode

    let boldUnderscoreNode =
        previousCharSatisfiesNot isNotWhitespace
        >>. skipString "__"
        >>. restOfLineContains "__"
        >>. manyInlineNodesTill (skipString "__")
        |>> fun c -> MfmBoldNode(aggregateTextInline c, Symbol) :> MfmNode

    let boldTagNode =
        skipString "<b>"
        >>. restOfStreamContains "</b>"
        >>. manyInlineNodesTill (skipString "</b>")
        |>> fun c -> MfmBoldNode(aggregateTextInline c, HtmlTag) :> MfmNode

    let strikeNode =
        skipString "~~"
        >>. restOfLineContains "~~"
        >>. manyInlineNodesTill (skipString "~~")
        |>> fun c -> MfmStrikeNode(aggregateTextInline c, Symbol) :> MfmNode

    let strikeTagNode =
        skipString "<s>"
        >>. restOfStreamContains "</s>"
        >>. manyInlineNodesTill (skipString "</s>")
        |>> fun c -> MfmStrikeNode(aggregateTextInline c, HtmlTag) :> MfmNode

    let codeNode =
        codePattern
        >>. restOfLineContainsChar '`' // TODO: this doesn't cover the case where a code block node ends before the inline one
        >>. pushLine // Remove when above TODO is resolved
        >>. manyCharsTill anyChar codePattern
        .>> assertLine // Remove when above TODO is resolved
        |>> fun v -> MfmInlineCodeNode(v) :> MfmNode

    let codeBlockNode =
        opt skipNewline
        >>. opt skipNewline
        >>. followedByString "```"
        >>. restOfStreamContains "```"
        >>. previousCharSatisfiesNot isNotNewline
        >>. skipString "```"
        >>. opt (many1CharsTill asciiLetter (lookAhead newline))
        .>>. (skipNewline
              >>. manyCharsTill anyChar (attempt (skipNewline >>. skipString "```")))
        .>> (skipNewline <|> eof)
        .>> opt skipNewline
        |>> fun (lang: string option, code: string) -> MfmCodeBlockNode(code, lang) :> MfmNode

    let mathNode =
        skipString "\("
        >>. restOfLineContains "\)"
        >>. manyCharsTill anyChar (skipString "\)")
        |>> fun f -> MfmMathInlineNode(f) :> MfmNode

    let mathBlockNode =
        previousCharSatisfiesNot isNotWhitespace
        >>. skipString "\["
        >>. restOfStreamContains "\]"
        >>. many1CharsTill anyChar (skipString "\]")
        |>> fun f -> MfmMathBlockNode(f) :> MfmNode

    let emojiCodeNode =
        skipChar ':'
        >>. restOfLineContains ":"
        >>. manyCharsTill (satisfy isAsciiLetter <|> satisfy isDigit <|> anyOf "+-_") (skipChar ':')
        |>> fun e -> MfmEmojiCodeNode(e) :> MfmNode

    let fnNode =
        skipString "$["
        >>. restOfStreamContains "]"
        >>. many1Chars (asciiLower <|> digit)
        .>>. opt (skipChar '.' >>. sepBy1 fnArg (skipChar ','))
        .>> skipChar ' '
        .>>. many1InlineNodesTill (skipChar ']')
        |>> fun ((n, o), c) -> MfmFnNode(n, fnDict o, aggregateTextInline c) :> MfmNode

    let plainNode =
        skipString "<plain>"
        >>. restOfStreamContains "</plain>"
        >>. manyCharsTill anyChar (skipString "</plain>")
        |>> fun v -> MfmPlainNode(v) :> MfmNode

    let smallNode =
        skipString "<small>"
        >>. restOfStreamContains "</small>"
        >>. manyInlineNodesTill (skipString "</small>")
        |>> fun c -> MfmSmallNode(aggregateTextInline c) :> MfmNode

    let centerNode =
        skipString "<center>"
        >>. restOfStreamContains "</center>"
        >>. manyInlineNodesTill (skipString "</center>")
        |>> fun c -> MfmCenterNode(aggregateTextInline c) :> MfmNode

    let mentionNode =
        (previousCharSatisfiesNot isNotWhitespace
         <|> previousCharSatisfies (isAnyOf <| "()"))
        >>. skipString "@"
        >>. many1Chars (
            satisfy isLetterOrNumber
            <|> pchar '_'
            <|> attempt (anyOf ".-" .>> nextCharSatisfies isLetterOrNumber)
        )
        .>>. opt (skipChar '@' >>. domain)
        .>> (lookAhead
             <| choice
                 [ eof
                   skipNoneOf ":"
                   skipChar ':' .>> nextCharSatisfiesNot isAsciiLetterOrNumber ])
        |>> fun (user: string, host: string option) -> MfmMentionNode(acct (user, host), user, host) :> MfmNode

    let hashtagNode =
        previousCharSatisfiesNot isNotWhitespace
        >>. skipChar '#'
        >>. many1CharsTill hashtagMatcher (notFollowedBy hashtagSatisfier)
        |>> fun h -> MfmHashtagNode(h) :> MfmNode

    let urlNode =
        lookAhead (skipString "https://" <|> skipString "http://")
        >>. manyCharsTill
                ((pchar '(' .>> pushParen) <|> (pchar ')' .>> popParen) <|> anyChar)
                (nextCharSatisfies isWhitespace
                 <|> (assertNoParen >>. followedByChar ')')
                 <|> eof)
        .>> clearParen
        >>= fun uri ->
            match Uri.TryCreate(uri, UriKind.Absolute) with
            | true, NonNullQuick finalUri ->
                match finalUri.Scheme with
                | "http" -> preturn (MfmUrlNode(uri, false) :> MfmNode)
                | "https" -> preturn (MfmUrlNode(uri, false) :> MfmNode)
                | _ -> fail "invalid scheme"
            | _ -> fail "invalid url"

    let urlNodeBrackets =
        skipChar '<'
        >>. lookAhead (skipString "https://" <|> skipString "http://")
        >>. restOfLineContains ">" // This intentionally breaks compatibility with mfm-js, as there's no reason to allow newlines in urls
        >>. manyCharsTill anyChar (skipChar '>')
        >>= fun uri ->
            match Uri.TryCreate(uri, UriKind.Absolute) with
            | true, NonNullQuick finalUri ->
                match finalUri.Scheme with
                | "http" -> preturn (MfmUrlNode(uri, true) :> MfmNode)
                | "https" -> preturn (MfmUrlNode(uri, true) :> MfmNode)
                | _ -> fail "invalid scheme"
            | _ -> fail "invalid url"

    let linkNode =
        (opt (pchar '?'))
        .>>. (pchar '[' >>. restOfLineContains "]" >>. manyCharsTill anyChar (pchar ']'))
        .>>. (pchar '('
              >>. restOfLineContains ")"
              >>. lookAhead (skipString "https://" <|> skipString "http://")
              >>. manyCharsTill
                      ((pchar '(' .>> pushParen) <|> (pchar ')' .>> popParen) <|> anyCharExceptNewline)
                      (assertNoParen >>. skipChar ')'))
        .>> clearParen
        >>= fun ((silent, text), uri) ->
            match Uri.TryCreate(uri, UriKind.Absolute) with
            | true, NonNullQuick finalUri ->
                match finalUri.Scheme with
                | "http" -> preturn (MfmLinkNode(uri, text, silent.IsSome) :> MfmNode)
                | "https" -> preturn (MfmLinkNode(uri, text, silent.IsSome) :> MfmNode)
                | _ -> fail "invalid scheme"
            | _ -> fail "invalid url"

    let quoteNode =
        previousCharSatisfiesNot isNotNewline
        >>. many1 (
            pchar '>'
            >>. (opt <| pchar ' ')
            >>. (many1InlineNodesTill (skipNewline <|> eof))
        )
        .>> (opt <| attempt (skipNewline >>. (notFollowedBy <| pchar '>')))
        .>>. (opt <| attempt (skipNewline >>. (followedBy <| pchar '>')) .>>. opt eof)
        |>> fun (q, (followedByQuote, followedByEof)) ->
            MfmQuoteNode(
                List.collect (fun e -> e @ [ (MfmCharNode('\n') :> MfmInlineNode) ]) q
                |> fun q -> List.take (q.Length - 1) q
                |> aggregateTextInline,
                followedByQuote.IsSome,
                followedByEof.IsSome
            )
            :> MfmNode

    let charNode = anyChar |>> fun v -> MfmCharNode(v) :> MfmNode

    // Custom parser for higher throughput
    type ParseMode =
        | Full
        | Inline
        | Simple

    let parseNode (m: ParseMode) =
        let inlineTagNodes =
            [ plainNode
              smallNode
              italicTagNode
              boldTagNode
              strikeTagNode
              urlNodeBrackets ]

        let failIfTimeout: Parser<unit, UserState> =
            let error = messageError "Timeout exceeded"

            fun (stream: CharStream<_>) ->
                match stream.UserState.TimeoutReached with
                | true -> Reply(FatalError, error)
                | _ -> Reply(())

        let prefixedNode (m: ParseMode) : Parser<MfmNode, UserState> =
            fun (stream: CharStream<_>) ->
                match stream.UserState.Depth with
                | GreaterEqualThan 20 -> stream |> charNode
                | _ ->
                    match (stream.Peek(), m) with
                    // Block nodes, ordered by expected frequency
                    | '`', Full -> codeBlockNode <|> codeNode
                    | '\n', Full when stream.Match("\n```") -> codeBlockNode
                    | '\n', Full when stream.Match("\n\n```") -> codeBlockNode
                    | '>', Full -> quoteNode
                    | '<', Full when stream.Match "<center>" -> centerNode
                    | '\\', Full when stream.Match "\\[" -> mathBlockNode
                    // Inline nodes, ordered by expected frequency
                    | '*', (Full | Inline) -> italicAsteriskNode <|> boldAsteriskNode
                    | '_', (Full | Inline) -> italicUnderscoreNode <|> boldUnderscoreNode
                    | '@', (Full | Inline) -> mentionNode
                    | '#', (Full | Inline) -> hashtagNode
                    | '`', Inline -> codeNode
                    | 'h', (Full | Inline) when stream.Match "http" -> urlNode
                    | ':', (Full | Inline | Simple) -> emojiCodeNode
                    | '~', (Full | Inline) when stream.Match "~~" -> strikeNode
                    | '[', (Full | Inline) -> linkNode
                    | '<', (Full | Inline) -> choice inlineTagNodes
                    | '<', Simple when stream.Match "<plain>" -> plainNode
                    | '\\', (Full | Inline) when stream.Match "\\(" -> mathNode
                    | '$', (Full | Inline) when stream.Match "$[" -> fnNode
                    | '?', (Full | Inline) when stream.Match "?[" -> linkNode
                    // Fallback to char node
                    | _ -> charNode
                    <| stream

        failIfTimeout >>. (attempt <| prefixedNode m <|> charNode)

    // Parser abstractions
    let node = parseNode Full
    let simple = parseNode Simple
    let inlinep = parseNode Inline |>> fun v -> v :?> MfmInlineNode

    // Populate references
    let pushDepth = updateUserState (fun u -> { u with Depth = (u.Depth + 1) })
    let popDepth = updateUserState (fun u -> { u with Depth = (u.Depth - 1) })
    do manyInlineNodesTillRef.Value <- fun endp -> pushDepth >>. manyTill inlinep endp .>> popDepth
    do many1InlineNodesTillRef.Value <- fun endp -> pushDepth >>. many1Till inlinep endp .>> popDepth

    // Final parse command
    let parse = spaces >>. manyTill node eof .>> spaces
    let parseSimple = spaces >>. manyTill simple eof .>> spaces

open MfmParser

module Mfm =
    let internal runParser p str =
        let state = UserState.Default()
        let result = runParserOnString p state "" str

        match (result, state.TimeoutReached) with
        | Success(result, _, _), _ -> aggregateText result
        | Failure _, true -> [ MfmTimeoutTextNode(str) ]
        | Failure(s, _, _), false -> failwith $"Failed to parse MFM: {s}"

    let parse str = runParser parse str
    let parseSimple str = runParser parseSimple str
