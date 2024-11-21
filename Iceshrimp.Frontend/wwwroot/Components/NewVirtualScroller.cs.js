export function scrollTo(element) {
    element.scrollIntoView()
}

export function getBoundingClientRectangle(element){
    if (element == null) return ;
    return element.getBoundingClientRect().top
}

export function SetScrollY(number){
    window.scroll(window.scrollX, number);
}

export function GetScrollY(){
    return window.scrollY;
}

export function GetDocumentHeight(){
    return document.body.scrollHeight
}