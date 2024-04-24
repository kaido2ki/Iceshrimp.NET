export function openDialog(element){
    element.showModal()
}
export function closeDialog(element){
    element.close()
}

export function scrollTo(element) {
    element.scrollIntoView()
}

export function getScrollLeft(element){
    return element.scrollLeft
}

export function getScrollWidth(element){
    return element.scrollWidth
}