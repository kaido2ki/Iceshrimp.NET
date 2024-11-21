function getHeight(ref) {
    if (ref != null) {
        return ref.scrollHeight;
    } else {
        console.log("invalid ref")
        return 0;
    }
}

function getBoundingClientRectangle(element){
    if (element == null) return ;
    return element.getBoundingClientRect().top
}

function SetScrollY(number){
    window.scroll(window.scrollX, number);
}

function GetScrollY(){
    return window.scrollY;
}

function GetDocumentHeight(){
    return document.body.scrollHeight
}