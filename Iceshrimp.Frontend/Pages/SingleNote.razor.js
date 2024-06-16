export function GetScrollTop(ref) {
    return ref.scrollTop;
}

export function SetScrollTop(ref, number) {
    ref.scrollTop = number;
}

export function ScrollIntoView(ref) {
    ref.scrollIntoView(true)
}