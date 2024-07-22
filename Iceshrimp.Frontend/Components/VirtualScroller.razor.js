export function GetHeight(ref) {
    if (ref != null) {
        return ref.scrollHeight;
    } else {
        console.log("invalid ref")
        return 0;
    }
}

export function GetScrollTop(ref) {
    if (ref != null) {
        return ref.scrollTop;
    } else {
        console.log("invalid ref")
        return 0;
    }
}

export function GetScrollY(){
    return window.scrollY;
}

export function SetScrollY(number){
    window.scroll(window.scrollX, number);
}

export function SetScrollTop(number, ref) {
    ref.scrollTop = number;
} 