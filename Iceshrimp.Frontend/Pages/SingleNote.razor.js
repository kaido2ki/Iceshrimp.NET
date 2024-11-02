export function ScrollIntoView(ref) {
    ref.scrollIntoView({ behavior: "instant", block: "center"})
}

export function GetScrollY(){
    return window.scrollY;
}

export function SetScrollY(number){
    window.scroll(window.scrollX, number);
}