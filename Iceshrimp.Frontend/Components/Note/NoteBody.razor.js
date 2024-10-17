// Computes the height of an element by its font size
export function getComputedHeight(element){
    let fontSize = window.getComputedStyle(element).getPropertyValue("font-size").match(/\d+/);
    let height = element.scrollHeight;
    return height / fontSize;
}

