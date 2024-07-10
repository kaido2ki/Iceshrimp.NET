export function GetHeight(ref) {
    if (ref != null) {
        return ref.scrollHeight;
    } else {
        return 0;
    }
}

export function GetScrollTop(ref) {
    return ref.scrollTop;
}

export function SetScrollTop(number, ref) {
    ref.scrollTop = number;
} 