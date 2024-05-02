namespace Iceshrimp.Parsing

open System
open System.Collections.Generic
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

    type MfmTextNode(v: string) =
        inherit MfmInlineNode([])
        member val Text = v

    type MfmItalicNode(c) =
        inherit MfmInlineNode(c)

    type MfmBoldNode(c) =
        inherit MfmInlineNode(c)

    type MfmStrikeNode(c) =
        inherit MfmInlineNode(c)

    type MfmInlineCodeNode(v: string) =
        inherit MfmInlineNode([])
        member val Code = v

    type MfmPlainNode(v: string) =
        inherit MfmInlineNode([ MfmTextNode(v) ])

    type MfmSmallNode(c) =
        inherit MfmInlineNode(c)

    type MfmQuoteNode(c) =
        inherit MfmBlockNode(c)

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

    type MfmFnNode(args: Dictionary<string, string>, name, children) =
        inherit MfmInlineNode(children)
        // (string, bool) args = (string, null as string?)
        member val Args = args
        member val Name = name

    type internal MfmCharNode(v: char) =
        inherit MfmInlineNode([])
        member val Char = v

open MfmNodeTypes

module private MfmParser =
    // Abstractions
    let str s = pstring s
    let seqAttempt s = s |> Seq.map attempt
    let isWhitespace c = Char.IsWhiteSpace c
    let isNotWhitespace c = Char.IsWhiteSpace c = false

    let isAsciiLetterOrNumber c = Char.IsAsciiLetter c || Char.IsDigit c

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

    let domainFirstComponent =
        many1Chars (satisfy isAsciiLetter <|> satisfy isDigit <|> anyOf "_-")

    let domainComponent =
        many1Chars (satisfy isAsciiLetter <|> satisfy isDigit <|> anyOf "._-")

    let domainStart = (satisfy isAsciiLetter <|> satisfy isDigit)

    let domainFull =
        domainStart .>>. domainFirstComponent .>>. pchar '.' .>>. many1 domainComponent

    let domainAggregate1 (a: char, b: string) = string a + b
    let domainAggregate2 (a: char * string, b: char) = (domainAggregate1 a) + string b

    let domainAggregate (x: (char * string) * char, y: string list) =
        domainAggregate2 x + (String.concat "" y)

    let domain = domainFull |>> domainAggregate

    let acct (user: string, host: string option) =
        match host with
        | None -> user
        | Some v -> user + "@" + v

    // References
    let node, nodeRef = createParserForwardedToRef ()
    let inlineNode, inlineNodeRef = createParserForwardedToRef ()

    let seqFlatten items =
        seq {
            for item in items do
                yield! item
        }

    // Patterns
    let italicPattern = (notFollowedBy <| str "**") >>. skipChar '*'
    let codePattern = (notFollowedBy <| str "```") >>. skipChar '`'

    // Node parsers

    let italicNode =
        italicPattern >>. manyTill inlineNode italicPattern
        |>> fun c -> MfmItalicNode(aggregateTextInline c) :> MfmNode

    //TODO: https://github.com/pzp1997/harkdown/blob/master/src/InlineParser.hs#L173-L201

    let boldNode =
        skipString "**" >>. manyTill inlineNode (skipString "**")
        |>> fun c -> MfmBoldNode(aggregateTextInline c) :> MfmNode

    let strikeNode =
        skipString "~~" >>. manyTill inlineNode (skipString "~~")
        |>> fun c -> MfmStrikeNode(aggregateTextInline c) :> MfmNode

    let codeNode =
        codePattern >>. manyCharsTill anyChar codePattern
        |>> fun v -> MfmInlineCodeNode(v) :> MfmNode

    let codeBlockNode =
        previousCharSatisfiesNot isNotWhitespace
        >>. skipString "```"
        >>. opt (many1CharsTill asciiLetter (lookAhead newline))
        .>>. (skipNewline >>. manyCharsTill anyChar (attempt (skipNewline >>. skipString "```")))
        |>> fun (lang: string option, code: string) -> MfmCodeBlockNode(code, lang) :> MfmNode

    let mathNode =
        skipString "\(" >>. manyCharsTill anyChar (skipString "\)")
        |>> fun f -> MfmMathInlineNode(f) :> MfmNode

    let mathBlockNode =
        previousCharSatisfiesNot isNotWhitespace
        >>. skipString "\["
        >>. many1CharsTill anyChar (skipString "\]")
        |>> fun f -> MfmMathBlockNode(f) :> MfmNode

    let emojiCodeNode =
        skipChar ':' >>. manyCharsTill (satisfy isAsciiLetter <|> satisfy isDigit <|> anyOf "+-_") (skipChar ':')
        |>> fun e -> MfmEmojiCodeNode(e) :> MfmNode

    let plainNode =
        skipString "<plain>" >>. manyCharsTill anyChar (skipString "</plain>")
        |>> fun v -> MfmPlainNode(v) :> MfmNode

    let smallNode =
        skipString "<small>" >>. manyTill inlineNode (skipString "</small>")
        |>> fun c -> MfmSmallNode(aggregateTextInline c) :> MfmNode

    let centerNode =
        skipString "<center>" >>. manyTill inlineNode (skipString "</center>")
        |>> fun c -> MfmSmallNode(aggregateTextInline c) :> MfmNode

    let mentionNode =
        previousCharSatisfiesNot isNotWhitespace
        >>. skipString "@"
        >>. many1Chars (satisfy isAsciiLetter <|> satisfy isDigit <|> anyOf "._-")
        .>>. opt (skipChar '@' >>. domain)
        .>> (lookAhead
             <| choice
                 [ spaces1
                   eof
                   skipChar ')'
                   skipChar ','
                   skipChar '\''
                   skipChar ':' .>> nextCharSatisfiesNot isAsciiLetterOrNumber ])
        |>> fun (user: string, host: string option) -> MfmMentionNode(acct (user, host), user, host) :> MfmNode

    let hashtagNode =
        previousCharSatisfiesNot isNotWhitespace
        >>. skipChar '#'
        >>. many1CharsTill letter (nextCharSatisfiesNot isLetter)
        |>> fun h -> MfmHashtagNode(h) :> MfmNode

    let urlNodePlain =
        lookAhead (skipString "https://" <|> skipString "http://")
        >>. manyCharsTill anyChar (nextCharSatisfies isWhitespace <|> eof) //FIXME: this needs significant improvements
        >>= fun uri ->
            match Uri.TryCreate(uri, UriKind.Absolute) with
            | true, finalUri ->
                match finalUri.Scheme with
                | "http" -> preturn (MfmUrlNode(uri, false) :> MfmNode)
                | "https" -> preturn (MfmUrlNode(uri, false) :> MfmNode)
                | _ -> fail "invalid scheme"
            | _ -> fail "invalid url"

    let urlNodeBrackets =
        skipChar '<'
        >>. lookAhead (skipString "https://" <|> skipString "http://")
        >>. manyCharsTill anyChar (skipChar '>')
        >>= fun uri ->
            match Uri.TryCreate(uri, UriKind.Absolute) with
            | true, finalUri ->
                match finalUri.Scheme with
                | "http" -> preturn (MfmUrlNode(uri, true) :> MfmNode)
                | "https" -> preturn (MfmUrlNode(uri, true) :> MfmNode)
                | _ -> fail "invalid scheme"
            | _ -> fail "invalid url"

    let urlNode = urlNodePlain <|> urlNodeBrackets

    let linkNode =
        (opt (pchar '?'))
        .>>. (pchar '[' >>. manyCharsTill anyChar (pchar ']'))
        .>>. (pchar '('
              >>. lookAhead (skipString "https://" <|> skipString "http://")
              >>. manyCharsTill anyChar (pchar ')'))
        >>= fun ((silent, text), uri) ->
            match Uri.TryCreate(uri, UriKind.Absolute) with
            | true, finalUri ->
                match finalUri.Scheme with
                | "http" -> preturn (MfmLinkNode(uri, text, silent.IsSome) :> MfmNode)
                | "https" -> preturn (MfmLinkNode(uri, text, silent.IsSome) :> MfmNode)
                | _ -> fail "invalid scheme"
            | _ -> fail "invalid url"


    let charNode = anyChar |>> fun v -> MfmCharNode(v) :> MfmNode

    // Node collection
    let inlineNodeSeq =
        [ italicNode
          boldNode
          strikeNode
          hashtagNode
          mentionNode
          codeNode
          urlNode
          linkNode
          mathNode
          emojiCodeNode
          charNode ]

    //TODO: still missing: FnNode, MfmSearchNode, MfmQuoteNode

    let blockNodeSeq =
        [ plainNode; centerNode; smallNode; codeBlockNode; mathBlockNode ]

    let nodeSeq = [ blockNodeSeq; inlineNodeSeq ]

    // Populate references
    do nodeRef.Value <- choice <| seqAttempt (seqFlatten <| nodeSeq)
    do inlineNodeRef.Value <- choice <| (seqAttempt inlineNodeSeq) |>> fun v -> v :?> MfmInlineNode

    // Final parse command
    let parse = spaces >>. manyTill node eof .>> spaces

open MfmParser

module Mfm =
    let parse str =
        match run parse str with
        | Success(result, _, _) -> aggregateText result
        | Failure(s, _, _) -> failwith $"Failed to parse MFM: {s}"
